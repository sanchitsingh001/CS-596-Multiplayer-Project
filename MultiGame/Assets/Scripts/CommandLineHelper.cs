using Unity.Netcode;
using UnityEngine;

namespace TicTacToeMultiplayer
{
    public class CommandLineHelper : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnBeforeSceneLoad()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-host")
                {
                    var go = new GameObject("CommandLineHelper");
                    go.AddComponent<CommandLineStartHost>();
                    DontDestroyOnLoad(go);
                    return;
                }
                if (args[i] == "-client")
                {
                    var go = new GameObject("CommandLineHelper");
                    go.AddComponent<CommandLineStartClient>();
                    DontDestroyOnLoad(go);
                    return;
                }
            }
        }
    }

    class CommandLineStartHost : MonoBehaviour
    {
        void Update()
        {
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.StartHost();
                Destroy(this);
            }
        }
    }

    class CommandLineStartClient : MonoBehaviour
    {
        void Update()
        {
            if (NetworkManager.Singleton != null && !NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.StartClient();
                Destroy(this);
            }
        }
    }
}
