#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

namespace TicTacToeMultiplayer.Editor
{
    /// <summary>
    /// Editor script to set up the Multiplayer Tic-Tac-Toe scene.
    /// Run via menu: TicTacToe > Setup Multiplayer Scene
    /// </summary>
    public static class SetupMultiplayerScene
    {
        [MenuItem("TicTacToe/Setup Multiplayer Scene")]
        public static void Setup()
        {
            if (Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Exit Play Mode First", "Setup Multiplayer Scene cannot run while the game is playing. Stop Play mode, then run TicTacToe > Setup Multiplayer Scene again.", "OK");
                return;
            }
            // Load or create main scene
            UnityEngine.SceneManagement.Scene scene;
            if (System.IO.File.Exists("Assets/Scenes/MainScene.unity"))
                scene = EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity", OpenSceneMode.Single);
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
                EditorSceneManager.SaveScene(scene, "Assets/Scenes/MainScene.unity");
            }
            if (!scene.IsValid())
                return;

            // Create NetworkManager GameObject
            var nmGo = GameObject.Find("NetworkManager");
            if (nmGo == null)
            {
                nmGo = new GameObject("NetworkManager");
                nmGo.AddComponent<NetworkManagerUI>();
            }

            var nm = nmGo.GetComponent<NetworkManager>();
            if (nm == null)
            {
                nm = nmGo.AddComponent<NetworkManager>();
            }

            // Ensure UnityTransport exists and is assigned to NetworkConfig (required by NGO)
            var transport = nmGo.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            if (transport == null)
                transport = nmGo.AddComponent<Unity.Netcode.Transports.UTP.UnityTransport>();
            nm.NetworkConfig.NetworkTransport = transport;
            // Use port 7778 (7777 often conflicts with Multiplayer Play Mode or orphaned instances)
            transport.SetConnectionData("127.0.0.1", 7778);

            if (nmGo.GetComponent<NetworkManagerUI>() == null)
                nmGo.AddComponent<NetworkManagerUI>();

            // Create GameState GameObject (NetworkBehaviour with NetworkObject)
            var gsGo = GameObject.Find("TicTacToeGameState");
            if (gsGo == null)
            {
                gsGo = new GameObject("TicTacToeGameState");
                gsGo.AddComponent<NetworkObject>();
                gsGo.AddComponent<TicTacToeGameState>();
                gsGo.AddComponent<TicTacToeUI>();
            }

            // Create or overwrite Player prefab (empty GameObject, no visible mesh)
            var prefabPath = "Assets/Prefabs/Player.prefab";
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(prefabPath));
            var playerGo = new GameObject("Player");
            playerGo.AddComponent<NetworkObject>();
            playerGo.AddComponent<TicTacToePlayer>();
            var playerPrefab = PrefabUtility.SaveAsPrefabAsset(playerGo, prefabPath);
            Object.DestroyImmediate(playerGo);

            nm.NetworkConfig.PlayerPrefab = playerPrefab;

            // In-scene NetworkObjects (e.g. GameState) are spawned automatically by NGO
            // when the scene loads; no need to add them to the prefabs list.

            // Add to build settings
            var buildScenes = UnityEditor.EditorBuildSettings.scenes;
            bool hasScene = false;
            foreach (var s in buildScenes)
                if (s.path == "Assets/Scenes/MainScene.unity") { hasScene = true; break; }
            if (!hasScene)
            {
                var newList = new System.Collections.Generic.List<EditorBuildSettingsScene>(buildScenes);
                newList.Add(new EditorBuildSettingsScene("Assets/Scenes/MainScene.unity", true));
                EditorBuildSettings.scenes = newList.ToArray();
            }

            EditorSceneManager.SaveScene(scene);
            EditorUtility.DisplayDialog("Setup Complete", "Multiplayer Tic-Tac-Toe scene has been set up. Press Play and use Host/Client buttons to test.", "OK");
        }
    }
}
#endif
