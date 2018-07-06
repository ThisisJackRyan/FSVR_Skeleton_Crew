using System;
using System.Threading;

#if NETFX_CORE
using System.Threading.Tasks;
#endif

namespace Dissonance.Threading
{
    internal interface IThread
    {
        void Start();

        void Join();
    }

#if NETFX_CORE
    internal class DThread
        : IThread
    {
        private readonly System.Threading.Tasks.Task _task;

        private readonly ManualResetEvent _finishedEvent = new ManualResetEvent(false);

        public DThread(Action action)
        {
            _task = new System.Threading.Tasks.Task(action, System.Threading.Tasks.TaskCreationOptions.LongRunning);
            _task.ContinueWith(_ => {
                _finishedEvent.Set();
            });
        }

        public void Start()
        {
            _task.Start();
        }

        public void Join()
        {
            _finishedEvent.WaitOne();
        }
    }
#else
    internal class DThread
        : IThread
    {
        private readonly Thread _thread;

        public DThread(Action action)
        {
            _thread = new Thread(new ThreadStart(action));
        }

        public void Start()
        {
            _thread.Start();
        }

        public void Join()
        {
            _thread.Join();
        }
    }
#endif
}
