using System;
using System.Collections.Generic;

namespace Dissonance.Networking
{
    /// <summary>
    /// Information about a client in a network session
    /// </summary>
    public class ClientInfo<TPeer>
        : IEquatable<ClientInfo<TPeer>>
    {
        private readonly string _playerName;
        private readonly ushort _playerId;

        private readonly List<ushort> _rooms = new List<ushort>();

        /// <summary>
        /// Name of this client (as specified by the DissonanceComms component for the client)
        /// </summary>
        [NotNull] public string PlayerName
        {
            get { return _playerName; }
        }

        /// <summary>
        /// Unique ID of this client
        /// </summary>
        public ushort PlayerId
        {
            get { return _playerId; }
        }

        /// <summary>
        /// List of rooms this client is listening to
        /// </summary>
        [NotNull] internal List<ushort> Rooms
        {
            get { return _rooms; }
        }

        [CanBeNull] public TPeer Connection { get; internal set; }

        public bool IsConnected { get; internal set; }

        public ClientInfo(string playerName, ushort playerId, [CanBeNull] TPeer connection)
        {
            _playerName = playerName;
            _playerId = playerId;
            Connection = connection;

            IsConnected = true;
        }

        #region equality
        public bool Equals([CanBeNull] ClientInfo<TPeer> other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return string.Equals(_playerName, other._playerName) && _playerId == other._playerId;
        }

        public override bool Equals([CanBeNull] object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((ClientInfo<TPeer>)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_playerName.GetHashCode() * 397) ^ _playerId.GetHashCode();
            }
        }

        public static bool operator ==(ClientInfo<TPeer> left, ClientInfo<TPeer> right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ClientInfo<TPeer> left, ClientInfo<TPeer> right)
        {
            return !Equals(left, right);
        }
        #endregion
    }
}
