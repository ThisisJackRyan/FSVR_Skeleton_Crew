using System;
using Dissonance.Config;
using Random = System.Random;

namespace Dissonance.Networking.Client
{
    internal class PacketDelaySimulator
    {
        #region fields and properties
        private readonly Random _rnd = new Random();
        #endregion

        private static bool IsOrdered(MessageTypes header)
        {
            return header != MessageTypes.VoiceData;
        }

        private static bool IsReliable(MessageTypes header)
        {
            return header != MessageTypes.VoiceData;
        }

        public bool ShouldLose(ArraySegment<byte> packet)
        {
            if (DebugSettings.Instance.EnableNetworkSimulation)
            {
                var reader = new PacketReader(packet);
                if (reader.ReadUInt16() == PacketWriter.Magic && !IsReliable((MessageTypes)reader.ReadByte()))
                {
                    var lossRoll = _rnd.NextDouble();
                    if (lossRoll < DebugSettings.Instance.PacketLoss)
                        return true;
                }
            }

            return false;
        }
    }
}
