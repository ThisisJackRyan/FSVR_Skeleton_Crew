﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Dissonance
{
    /// <summary>
    /// Calculates the median packet loss over a group of decoder pipelines
    /// </summary>
    internal class PacketLossMonitor
    {
        private readonly ReadOnlyCollection<VoicePlayerState> _players;

        private DateTime _lastUpdatedPacketLoss = DateTime.MinValue;
        private int _lastUpdatedPlayerCount = -1;

        private readonly List<float> _tmpLossValues = new List<float>();

        public float PacketLoss { get; private set; }

        public PacketLossMonitor(ReadOnlyCollection<VoicePlayerState> players)
        {
            _players = players;
        }

        public void Update()
        {
            if (CheckTime() || CheckCount())
            {
                _lastUpdatedPacketLoss = DateTime.UtcNow;
                _lastUpdatedPlayerCount = _players.Count;

                //Calculate packet loss, or use the current value if we can't calculate a new one
                PacketLoss = CalculatePacketLoss() ?? PacketLoss;
            }
        }

        private bool CheckTime()
        {
            return DateTime.UtcNow - _lastUpdatedPacketLoss > TimeSpan.FromSeconds(0.5f);
        }

        private bool CheckCount()
        {
            return _lastUpdatedPlayerCount != _players.Count;
        }

        private float? CalculatePacketLoss()
        {
            //Accumulate a list of packet loss percentages
            _tmpLossValues.Clear();
            for (var i = 0; i < _players.Count; i++)
            {
                var loss = _players[i].PacketLoss;
                if (loss.HasValue)
                    _tmpLossValues.Add(loss.Value);
            }

            //Can't calculate packet loss if there are no readings...
            if (_tmpLossValues.Count == 0)
                return null;

            //Sort it so we can find the middle value (i.e. median)
            _tmpLossValues.Sort();

            //Calculate mid index (rounded up, not down, so we bias slightly towards worse values)
            int remainder;
            var midIndex = Math.DivRem(_tmpLossValues.Count - 1, 2, out remainder) + remainder;

            //Now pick out the median.
            return Math.Min(1, Math.Max(0, _tmpLossValues[Math.Min(_tmpLossValues.Count - 1, midIndex)]));
        }
    }
}
