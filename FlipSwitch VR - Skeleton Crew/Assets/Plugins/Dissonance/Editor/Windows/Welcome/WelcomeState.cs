using System;
using UnityEngine;

namespace Dissonance.Editor.Windows.Welcome
{
    [Serializable]
    internal class WelcomeState
    {
        [SerializeField] private string _shownForVersion;

        public string ShownForVersion
        {
            get { return _shownForVersion; }
        }

        public WelcomeState(string version)
        {
            _shownForVersion = version;
        }

        public override string ToString()
        {
            return _shownForVersion;
        }
    }
}
