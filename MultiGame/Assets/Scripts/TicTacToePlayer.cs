using Unity.Netcode;
using UnityEngine;

namespace TicTacToeMultiplayer
{
    public class TicTacToePlayer : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
         
            if (IsOwner)
            {
                var gameState = FindFirstObjectByType<TicTacToeGameState>();
                if (gameState != null)
                    gameState.RegisterPlayer(OwnerClientId);
            }
        }
    }
}
