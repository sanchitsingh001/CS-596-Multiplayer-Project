using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace TicTacToeMultiplayer
{
    public class NetworkManagerUI : MonoBehaviour
    {
        private NetworkManager _networkManager;
        private UnityTransport _transport;
        private readonly float _btnW = 120f;
        private readonly float _btnH = 30f;
        private readonly float _pad = 10f;

        [SerializeField] private string _serverAddress = "127.0.0.1";
        [SerializeField] private ushort _serverPort = 7778;

        void Awake()
        {
            _networkManager = GetComponent<NetworkManager>();
            _transport = GetComponent<UnityTransport>();
        }

        void OnGUI()
        {
            if (_networkManager == null) return;

            if (!_networkManager.IsClient && !_networkManager.IsServer)
            {
                float y = _pad;
                if (GUI.Button(new Rect(_pad, y, _btnW, _btnH), "Host"))
                {
                    _networkManager.StartHost();
                }
                y += _btnH + 5f;

                // client connection settings
                GUI.Label(new Rect(_pad, y, 100, 22), "Server Address:");
                _serverAddress = GUI.TextField(new Rect(_pad + 105, y, 140, 22), _serverAddress);
                y += 22 + 2;
                GUI.Label(new Rect(_pad, y, 100, 22), "Port:");
                string portStr = GUI.TextField(new Rect(_pad + 105, y, 60, 22), _serverPort.ToString());
                if (ushort.TryParse(portStr, out ushort p))
                    _serverPort = p;
                y += 22 + 5f;

                if (GUI.Button(new Rect(_pad, y, _btnW, _btnH), "Client"))
                {
                    if (_transport != null)
                        _transport.SetConnectionData(_serverAddress, _serverPort);
                    _networkManager.StartClient();
                }
                y += _btnH + 5f;
                if (GUI.Button(new Rect(_pad, y, _btnW, _btnH), "Server"))
                {
                    _networkManager.StartServer();
                }
            }
            else
            {
                var mode = _networkManager.IsHost
                    ? "Host"
                    : _networkManager.IsServer ? "Server" : "Client";

                string transportName = "—";
                try
                {
                    var config = _networkManager.NetworkConfig;
                    if (config != null && config.NetworkTransport != null)
                        transportName = config.NetworkTransport.GetType().Name;
                }
                catch { /* ignore */ }

                // status bar
                GUI.Label(new Rect(_pad, _pad, 380, 22), $"Transport: {transportName}  |  Mode: {mode}");
            }
        }
    }
}
