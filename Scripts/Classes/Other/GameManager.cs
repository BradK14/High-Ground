using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    // Variables
    // Set at start
    private bool setupComplete;
    private Dictionary<int, PlayerController> players;
    // Set at run time
    [Networked] private int playerTurn { get; set; }
    private BaseUI ui;

    // Functions
    // Networked Start
    public override void Spawned()
    {
        setupComplete = false;
        players = new Dictionary<int, PlayerController>();
        ui = FindObjectOfType<BaseUI>();
        StartCoroutine(Setup());
    }

    // Coroutines
    // Setup
    private IEnumerator Setup()
    {
        // Get a reference to the players
        while (players.Count == 0)
        {
            PlayerController[] players_ = FindObjectsOfType<PlayerController>();
            if (players_.Length == Runner.SessionInfo.MaxPlayers)
            {
                foreach (PlayerController p in players_)
                {
                    players[p.GetPlayerNum()] = p;
                }
            }

            yield return null;
        }

        // Add checks to see that nothing is trying to work (mapinfo, tileinfo, playerpossessedobjects, playercontrollers)
        bool playerControllersNotSet = true;
        while (playerControllersNotSet)
        {
            bool pcSetUpDone = true;
            for (int i = 1; i <= Runner.SessionInfo.MaxPlayers; i++)
            {
                if (!players[i].SetupIsComplete())
                {
                    pcSetUpDone = false;
                    break;
                }
            }

            if (pcSetUpDone)
            {
                playerControllersNotSet = false;
            }

            yield return null;
        }

        StartGame(1);

        setupComplete = true;
    }

    // Public
    // Getters
    public bool SetupIsComplete() { return setupComplete; }
    public Dictionary<int, PlayerController> GetPlayers() { return players; }
    public int GetPlayerTurn() { return playerTurn; }

    // Starts the game and sets starting values for the game
    public void NextPlayerTurn()
    {
        players[playerTurn].EndTurn();

        if (++playerTurn > players.Count)
        {
            playerTurn = 1;
        }

        // Move to the next player's turn
        Rpc_ChangeTurn(playerTurn);

        players[playerTurn].Rpc_StartTurn();
    }

    public void PlayerWon(int playerNum)
    {
        if (playerTurn != -1)
        {
            players[playerTurn].EndOfGame();
            playerTurn = -1;  // No one's turn anymore

            Rpc_PlayerWon(playerNum);
        }
    }

    public void PlayerLost(int playerNum)
    {
        if (playerTurn != -1)  // This function could be called in many different ways, but if the game is over it's over, so don't perform this more than once
        {
            players[playerTurn].EndOfGame();
            playerTurn = -1;

            if (playerNum == 1) { Rpc_PlayerWon(2); }
            else { Rpc_PlayerWon(1); }
        }
    }

    // Private Functions
    // Call once at the start of the game
    private void StartGame(int firstPlayer)
    {
        // Set the first turn for player 1
        playerTurn = firstPlayer;

        players[firstPlayer].Rpc_StartTurn();
    }

    // Rpcs
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_ChangeTurn(int playerTurn_)
    {
        playerTurn = playerTurn_;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_PlayerWon(int pNum)
    {
        ui.DisplayVictoryDefeat(players[pNum].GetPlayerRef() == Runner.LocalPlayer);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_CloseGame()
    {
        Application.Quit();
    }
}
