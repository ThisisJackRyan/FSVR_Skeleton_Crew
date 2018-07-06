using UnityEngine;

namespace Dissonance.Integrations.UNet_LLAPI.Demo
{
    public class StateManager : MonoBehaviour
    {
        private interface IState
        {
            void Awake();
            IState Update();        
        }

        private class LoadWorld : IState
        {
            private readonly IState _nextState;

            public LoadWorld(IState nextState)
            {
                _nextState = nextState;
            }

            public void Awake()
            {
                var prefab = Resources.Load<GameObject>("DissonanceDemoSetup");
                Instantiate(prefab);
            }

            public IState Update()
            {
                return _nextState;
            }
        }

        private class UnloadWorld : IState
        {
            private readonly IState _nextState;

            public UnloadWorld(IState nextState)
            {
                _nextState = nextState;
            }

            public void Awake()
            {
                var dissonance = FindObjectOfType<DissonanceComms>();
                if (dissonance != null)
                    Destroy(dissonance.gameObject);
            }

            public IState Update()
            {
                return _nextState;
            }
        }

        private class InMenu : IState
        {
            private string _serverIp = "localhost";

            public void Awake() { }

            public IState Update()
            {
                using (new GUILayout.AreaScope(new Rect(20, 20, 200, 200)))
                {
                    if (GUILayout.Button("Create Server"))
                        return new LoadWorld(new Server());

                    if (GUILayout.Button("Create Dedicated Server"))
                        return new LoadWorld(new DedicatedServer());

                    GUILayout.Space(20);

                    _serverIp = GUILayout.TextField(_serverIp);
                    if (GUILayout.Button("Connect to Server"))
                        return new LoadWorld(new Client(_serverIp));
                }

                return this;
            }
        }

        private class DedicatedServer : IState
        {
            private UNetCommsNetwork _net;

            public void Awake()
            {
                _net = FindObjectOfType<UNetCommsNetwork>();
                _net.InitializeAsDedicatedServer();
            }

            public IState Update()
            {
                using (new GUILayout.AreaScope(new Rect(20, 20, 200, 200)))
                {
                    GUILayout.Label("Running Dedicated Server");

                    if (GUILayout.Button("Disconnect"))
                    {
                        _net.Stop();
                        return new UnloadWorld(new InMenu());
                    }

                    return this;
                }
            }
        }

        private class Server : IState
        {
            private UNetCommsNetwork _net;

            public void Awake()
            {
                _net = FindObjectOfType<UNetCommsNetwork>();
                _net.InitializeAsServer();
            }

            public IState Update()
            {
                using (new GUILayout.AreaScope(new Rect(20, 20, 200, 200)))
                {
                    GUILayout.Label("Running server");

                    if (GUILayout.Button("Disconnect"))
                    {
                        _net.Stop();
                        return new UnloadWorld(new InMenu());
                    }

                    return this;
                }
            }
        }

        private class Client : IState
        {
            private UNetCommsNetwork _net;
            private readonly string _serverIp;

            public Client(string serverIp)
            {
                _serverIp = serverIp;
            }        

            public void Awake()
            {
                _net = FindObjectOfType<UNetCommsNetwork>();
                _net.InitializeAsClient(_serverIp);
            }

            public IState Update()
            {
                using (new GUILayout.AreaScope(new Rect(20, 20, 200, 200)))
                {
                    GUILayout.Label("Connected to server at " + _net.ServerAddress);

                    if (GUILayout.Button("Disconnect"))
                    {
                        _net.Stop();
                        return new UnloadWorld(new InMenu());
                    }

                    return this;
                }
            }
        }

        private IState _state;
        private IState _nextState;

        private void Awake()
        {
            _state = new InMenu();
            _nextState = _state;
            DontDestroyOnLoad(gameObject);
        }

        private void OnGUI()
        {
            _nextState = _state.Update();
        }

        private void Update()
        {
            if (_state != _nextState)
            {
                _nextState.Awake();
                _state = _nextState;
            }
        }
    }
}
