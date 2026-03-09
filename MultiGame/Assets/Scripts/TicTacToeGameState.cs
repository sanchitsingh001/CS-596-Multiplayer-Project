using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace TicTacToeMultiplayer
{
    public class TicTacToeGameState : NetworkBehaviour
    {
        // NetworkVariables: server writes, synced to all clients (board, scores, turn)
        private NetworkVariable<FixedString64Bytes> _board = new NetworkVariable<FixedString64Bytes>(
            "000000000", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<int> _currentTurn = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<int> _scoreX = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<int> _scoreO = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private NetworkVariable<bool> _gameOver = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private const ulong UnsetClientId = ulong.MaxValue;
        private NetworkVariable<ulong> _playerXClientId = new NetworkVariable<ulong>(
            UnsetClientId, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private NetworkVariable<ulong> _playerOClientId = new NetworkVariable<ulong>(
            UnsetClientId, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        // public accessors so clients can read synced state
        public NetworkVariable<FixedString64Bytes> Board => _board;
        public NetworkVariable<int> CurrentTurn => _currentTurn;
        public NetworkVariable<int> ScoreX => _scoreX;
        public NetworkVariable<int> ScoreO => _scoreO;
        public NetworkVariable<bool> GameOver => _gameOver;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer && NetworkManager.Singleton != null)
            {
                RegisterConnectedClients();
                NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            }
        }

        void Update()
        {
            if (IsServer && NetworkManager.Singleton != null)
            {
                int count = NetworkManager.Singleton.ConnectedClientsList.Count;
                if (count >= 1 && _playerXClientId.Value == UnsetClientId)
                {
                    RegisterPlayer(NetworkManager.Singleton.ConnectedClientsList[0].ClientId);
                }
                if (count >= 2 && _playerOClientId.Value == UnsetClientId)
                {
                    RegisterPlayer(NetworkManager.Singleton.ConnectedClientsList[1].ClientId);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            base.OnNetworkDespawn();
        }

        private void OnClientConnected(ulong clientId)
        {
            if (IsServer && GetSymbolForClient(clientId) == 0)
                RegisterPlayer(clientId);
        }

        private void RegisterConnectedClients()
        {
            if (NetworkManager.Singleton == null) return;
            foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
            {
                if (GetSymbolForClient(client.ClientId) == 0)
                    RegisterPlayer(client.ClientId);
            }
        }

        public void RegisterPlayer(ulong clientId)
        {
            if (!IsServer) return;
            if (_playerXClientId.Value == UnsetClientId)
                _playerXClientId.Value = clientId;
            else if (_playerOClientId.Value == UnsetClientId)
                _playerOClientId.Value = clientId;
        }
        public int GetSymbolForClient(ulong clientId)
        {
            if (_playerXClientId.Value == clientId) return 1;
            if (_playerOClientId.Value == clientId) return 2;
            return 0;
        }

        public bool IsMyTurn(ulong clientId)
        {
            int symbol = GetSymbolForClient(clientId);
            if (symbol == 0) return false;

            if (_playerOClientId.Value == UnsetClientId && _playerXClientId.Value == clientId)
                return true;
            if (_playerXClientId.Value == clientId && _playerOClientId.Value == clientId)
                return true;
            int turnIndex = symbol - 1;
            return _currentTurn.Value == turnIndex;
        }

        // RPC: client sends move request, server validates and updates board
        [Rpc(SendTo.Server)]
        public void RequestMoveRpc(int cellIndex, RpcParams rpcParams = default)
        {
            if (_gameOver.Value) return;
            ulong senderId = rpcParams.Receive.SenderClientId;
            int symbol = GetSymbolForClient(senderId);
            if (symbol == 0) return;

            int symbolToPlace = symbol;
            if ((_playerOClientId.Value == UnsetClientId && _playerXClientId.Value == senderId) ||
                (_playerXClientId.Value == senderId && _playerOClientId.Value == senderId))
                symbolToPlace = _currentTurn.Value + 1; 
            else
            {
                int turnIndex = symbol - 1;
                if (_currentTurn.Value != turnIndex) return; 
            }

            var boardStr = _board.Value.ToString();
            if (cellIndex < 0 || cellIndex >= 9 || boardStr[cellIndex] != '0')
                return; 

            char c = symbolToPlace == 1 ? '1' : '2';
            var arr = boardStr.ToCharArray();
            arr[cellIndex] = c;
            string newBoard = new string(arr);
            _board.Value = new FixedString64Bytes(newBoard);

            int winResult = CheckWin(newBoard);
            if (winResult == 1) { _scoreX.Value++; _gameOver.Value = true; return; }
            if (winResult == 2) { _scoreO.Value++; _gameOver.Value = true; return; }

            bool draw = true;
            foreach (char ch in _board.Value.ToString())
                if (ch == '0') { draw = false; break; }
            if (draw) { _gameOver.Value = true; return; }

            _currentTurn.Value = 1 - _currentTurn.Value;
        }

        // RPC: client requests new round, server resets state
        [Rpc(SendTo.Server)]
        public void ResetBoardRpc(RpcParams rpcParams = default)
        {
            _board.Value = new FixedString64Bytes("000000000");
            _currentTurn.Value = 0;
            _gameOver.Value = false;
        }

        private static int CheckWin(string s)
        {
            // Rows
            for (int r = 0; r < 3; r++)
            {
                char c = s[r * 3];
                if (c != '0' && c == s[r * 3 + 1] && c == s[r * 3 + 2]) return c == '1' ? 1 : 2;
            }
            // Cols
            for (int c = 0; c < 3; c++)
            {
                char ch = s[c];
                if (ch != '0' && ch == s[3 + c] && ch == s[6 + c]) return ch == '1' ? 1 : 2;
            }
            // Diagonals
            char d1 = s[0];
            if (d1 != '0' && d1 == s[4] && d1 == s[8]) return d1 == '1' ? 1 : 2;
            char d2 = s[2];
            if (d2 != '0' && d2 == s[4] && d2 == s[6]) return d2 == '1' ? 1 : 2;
            return 0;
        }
    }
}
