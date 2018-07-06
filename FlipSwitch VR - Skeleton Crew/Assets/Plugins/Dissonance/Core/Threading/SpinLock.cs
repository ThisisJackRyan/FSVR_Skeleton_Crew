using System;
using System.Threading;

namespace Dissonance.Threading
{
    /// <summary>
    /// A lock suitable for very very very small critical sections. Faster to enter and exit than a full mutex but extremely costly under contention!
    /// </summary>
    internal class SpinLock
    {
        private readonly Unlocker _unlocker;

#if NETFX_CORE
        private readonly object _locked = new object();
#else
        private int _lockedBy;
        private readonly bool _alwaysYield;
#endif

        public SpinLock()
        {
            _unlocker = new Unlocker(this);

#if !NETFX_CORE
            //We should never spin wait on a single threaded machine!
            _alwaysYield = Environment.ProcessorCount == 1;
#endif
        }

        private static int UniqueThreadId()
        {
            var id = Thread.CurrentThread.ManagedThreadId;
            return id == 0 ? 1 : id;
        }

        public IDisposable Lock()
        {
#if NETFX_CORE
            Monitor.Enter(_locked);
#else
            var id = UniqueThreadId();

            //we only store one ID, and it's cleared as soon as you call Unlock. This system cannot support recursion.
            if (id == _lockedBy)
                throw new LockRecursionException("Attempted to take a lock which is already held by this thread");

            //There's a lot going on here! Let's break it down bit by bit.
            // - The fundamental algorithm keeps spinning while the compare/exchange fails. This will keep happening while someone holds the lock.
            //
            // - We want to spin, but not always:
            //   - if it's a single processor machine we never want to spin!
            //   - SpinWait is really great. It issues YIELD instructions to the CPU so it knows to re-order these instructions intelligently (NOP on a single core machine,
            //     much more useful with e.g. hyperthreading). Length of spinwait is an exponential backoff, capping at 1024
            //   - Sleep(0) prods the OS into scheduling other threads waiting. However the OS may not reschedule work depending on core affinity of threads.
            //   - Sleep(1) tells the OS to unconditionally unschedule this thread until the next scheduler run (~15ms on windows).
            //     However this is important to do occasionally because it will cause the OS to *definitely* give a time slice to other threads which may
            //     be holding the lock even if they're on another core or of a lower priority.

            var spins = 0;
            while (Interlocked.CompareExchange(ref _lockedBy, id, 0) != 0)
            {
                unchecked
                {
                    spins++;
                    if (spins < 0)
                        spins = 0;
                }

                if (_alwaysYield || spins > 20)
                {
                    //Frequently; poke the OS into scheduling other waiting threads.
                    if (spins % 19 == 0)
                        Thread.Sleep(0);

                    //Infrequently; unconditionally give up our time slice
                    else if (spins % 83 == 0)
                        Thread.Sleep(1);

                    //All the other times; spin
                    else
                    {
                        var wait = 10;
                        if (spins < wait)
                            wait = spins;
                        Thread.SpinWait(1 << wait);
                    }
                }
            }
#endif

            return _unlocker;
        }

        private void Unlock()
        {
#if NETFX_CORE
            Monitor.Exit(_locked);
#else
            var id = UniqueThreadId();

            if (Interlocked.CompareExchange(ref _lockedBy, 0, id) != id)
                throw new InvalidOperationException("A thread which does not own the lock attempted to released the lock");
#endif
        }

        private class Unlocker
            : IDisposable
        {
            private readonly SpinLock _parent;

            public Unlocker(SpinLock parent)
            {
                _parent = parent;
            }

            public void Dispose()
            {
                _parent.Unlock();
            }
        }
    }
}
