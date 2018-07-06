using System;
using UnityEngine;

namespace Dissonance.Editor.Windows
{
    [Serializable]
    internal class SemanticVersion
        : IComparable<SemanticVersion>
    {
        // ReSharper disable InconsistentNaming (Justification: That's the serialization format)
        [SerializeField] private int _major;
        [SerializeField] private int _minor;
        [SerializeField] private int _patch;
        // ReSharper restore InconsistentNaming

        public int Major { get { return _major; } }
        public int Minor { get { return _minor; } }
        public int Patch { get { return _patch; } }

        public SemanticVersion()
        {
            //Need a blank constructor for deserialization
        }

        public SemanticVersion(int major, int minor, int patch)
        {
            _major = major;
            _minor = minor;
            _patch = patch;
        }

        public int CompareTo([CanBeNull] SemanticVersion other)
        {
            if (other == null)
                return 1;

            //Compare to the most significant part which is different

            if (!Major.Equals(other.Major))
                return Major.CompareTo(other.Major);

            if (!Minor.Equals(other.Minor))
                return Minor.CompareTo(other.Minor);

            if (!Patch.Equals(other.Patch))
                return Patch.CompareTo(other.Patch);

            return 0;
        }

        public override string ToString()
        {
            return string.Format("{0}.{1}.{2}", Major, Minor, Patch);
        }
    }
}
