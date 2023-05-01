using Fusion;
using Fusion.Sockets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkRunnerSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    // Variables
    // Initialized in the inspector
    [SerializeField] private GameManager gameManagerPrefab;
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private MapInfo mapPrefab;
    // Initialized at the start
    private Dictionary<PlayerRef, NetworkObject> players = new Dictionary<PlayerRef, NetworkObject>();
    private Dictionary<int, PlayerRef> playerNums = new Dictionary<int, PlayerRef>();
    // Initialized at run time
    private NetworkRunner _runner;
    private bool leftMouse;

    // Functions
    // INetworkRunnerCallbacks must include these functions
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, new Vector3(0, 0, 0), Quaternion.identity, player);  // Create the player's controller (where the input goes)
            players.Add(player, networkPlayerObject);
            playerNums.Add(players.Count, player);
            networkPlayerObject.GetComponent<PlayerController>().SetPlayerNum(players.Count);  // Set which player it is (player 1 for host or 2 for the client)
            networkPlayerObject.GetComponent<PlayerController>().SetPlayerRef(player);  // A reference to help with local differentiation using Networked on changed
            // When all players are present, spawn the map and game manager to begin the game.
            if (players.Count == runner.SessionInfo.MaxPlayers)
            {
                runner.Spawn(gameManagerPrefab);
                runner.Spawn(mapPrefab);
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {        
        var data = new InputData(false);

        data.leftMouse = leftMouse;
        leftMouse = false;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            data.up = true;
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            data.left = true;
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            data.right = true;
        }

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            data.down = true;
        }

        if (Input.GetKey(KeyCode.Return))
        {
            data.enter = true;
        }

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            data.shift = true;
        }
        
        input.Set(data);
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ArraySegment<byte> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }

    // Other functions
    private void OnGUI()
    {
        if (_runner == null)
        {
            int buttonWidth = Screen.width / 7;
            int buttonHeight = Screen.height / 20;
            if (GUI.Button(new Rect(Screen.width / 2 - buttonWidth / 2, Screen.height / 2 - buttonHeight / 2 - buttonHeight, buttonWidth, buttonHeight), "Host"))
            {
                StartGame(GameMode.Host);
            }

            if (GUI.Button(new Rect(Screen.width / 2 - buttonWidth / 2, Screen.height / 2 - buttonHeight / 2 + buttonHeight, buttonWidth, buttonHeight), "Join"))
            {
                StartGame(GameMode.Client);
            }
        }
    }
    async private void StartGame(GameMode mode)
    {
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = "High Ground",
            Scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            PlayerCount = 2
        });
    }

    private void Update()
    {
        leftMouse = leftMouse || Input.GetMouseButton(0);
    }

    public NetworkRunner GetRunner() { return _runner; }
}
