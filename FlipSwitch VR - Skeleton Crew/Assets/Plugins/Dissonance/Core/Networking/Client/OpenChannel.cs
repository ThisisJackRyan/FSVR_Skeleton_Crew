namespace Dissonance.Networking.Client
{
    internal struct OpenChannel
    {
        private static readonly Log Log = Logs.Create(LogCategory.Network, typeof(OpenChannel).Name);

        private readonly ChannelProperties _config;

        private readonly ChannelType _type;
        private readonly ushort _recipient;
        private readonly string _name;
        private readonly bool _isClosing;
        private readonly ushort _sessionId;

        [NotNull] public ChannelProperties Config
        {
            get { return _config; }
        }

        public ushort Bitfield
        {
            get
            {
                return new ChannelBitField(
                    _type,
                    _sessionId,
                    Priority,
                    AmplitudeMultiplier,
                    IsPositional,
                    _isClosing
                ).Bitfield;
            }
        }

        public ushort Recipient
        {
            get { return _recipient; }
        }

        public ChannelType Type
        {
            get { return _type; }
        }

        public bool IsClosing
        {
            get { return _isClosing; }
        }

        public bool IsPositional
        {
            get { return _config.Positional; }
        }

        public ChannelPriority Priority
        {
            get { return _config.TransmitPriority; }
        }

        public float AmplitudeMultiplier
        {
            get { return _config.AmplitudeMultiplier; }
        }

        public ushort SessionId
        {
            get { return _sessionId; }
        }

        [NotNull] public string Name
        {
            get { return _name; }
        }

        public OpenChannel(ChannelType type, ushort sessionId, ChannelProperties config, bool closing, ushort recipient, string name)
        {
            _type = type;
            _sessionId = sessionId;
            _config = config;
            _isClosing = closing;
            _recipient = recipient;
            _name = name;
        }

        [Pure] public OpenChannel AsClosing()
        {
            if (IsClosing)
                throw Log.CreatePossibleBugException("Attempted to close a channel which is already closed", "94ED6728-F8D7-4926-9058-E23A5870BF31");

            return new OpenChannel(_type, _sessionId, _config, true, _recipient, _name);
        }

        [Pure] public OpenChannel AsOpen()
        {
            if (!IsClosing)
                throw Log.CreatePossibleBugException("Attempted to open a channel which is already open", "F1880EDD-D222-4358-9C2C-4F1C72114B62");

            return new OpenChannel(_type, (ushort)(_sessionId + 1), _config, false, _recipient, _name);
        }
    }
}
