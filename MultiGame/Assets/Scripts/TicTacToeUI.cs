using Unity.Netcode;
using UnityEngine;

namespace TicTacToeMultiplayer
{
    public class TicTacToeUI : MonoBehaviour
    {
        [SerializeField] private TicTacToeGameState _gameState;
        private const float CellSize = 80f;
        private const float StartX = 120f;
        private const float StartY = 120f;

        void Start()
        {
            if (_gameState == null)
                _gameState = FindFirstObjectByType<TicTacToeGameState>();
        }

        void OnGUI()
        {
            if (_gameState == null || NetworkManager.Singleton == null)
                return;
            if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
                return;

            ulong myId = NetworkManager.Singleton.LocalClientId;
            // read board/scores from NetworkVars (synced from server)
            string board = _gameState.Board.Value.ToString();
            int mySymbol = _gameState.GetSymbolForClient(myId);
            bool myTurn = _gameState.IsMyTurn(myId);

            const float StatusY = 34f;
            const float ScoresY = 56f;
            string role = mySymbol == 1 ? "You are: X" : mySymbol == 2 ? "You are: O" : "Connecting...";
            string turn = _gameState.GameOver.Value ? "" : myTurn ? " — Your turn!" : " — Waiting for opponent";
            GUI.Label(new Rect(10f, StatusY, 320, 22), role + turn);
            if (mySymbol == 0 && NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer && Time.time > 3f)
                GUI.Label(new Rect(10f, 78, 420, 20), "If this is the BUILT game: File > Build and Run again, then join as Client.");

            GUI.Label(new Rect(10f, ScoresY, 200, 22), $"X: {_gameState.ScoreX.Value}  |  O: {_gameState.ScoreO.Value}");

            for (int i = 0; i < 9; i++)
            {
                int row = i / 3;
                int col = i % 3;
                float x = StartX + col * CellSize;
                float y = StartY + row * CellSize;

                string label = board[i] == '0' ? "" : board[i] == '1' ? "X" : "O";
                bool clickable = board[i] == '0' && _gameState.IsMyTurn(myId) && !_gameState.GameOver.Value;

                var rect = new Rect(x, y, CellSize - 4, CellSize - 4);
                if (clickable)
                {
                    if (GUI.Button(rect, label))
                        _gameState.RequestMoveRpc(i);  // RPC to server
                }
                else
                {
                    GUI.Box(rect, label);
                }
            }

            if (_gameState.GameOver.Value)
            {
                if (GUI.Button(new Rect(StartX, StartY + 3 * CellSize + 10, 240, 40), "New Game"))
                {
                    _gameState.ResetBoardRpc();  // RPC to server
                }
            }
        }
    }
}
