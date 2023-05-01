using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    // Variables
    // Set at start
    private Dictionary<int, PlayerController> players;
    [Networked] private bool gameStarted { get; set; }
    // Set at run time
    [Networked] private int playerTurn { get; set; }

    // Functions
    // Networked Start
    public override void Spawned()
    {
        players = new Dictionary<int, PlayerController>();
        gameStarted = false;
    }

    // Networked Update
    public override void FixedUpdateNetwork()
    {
        if (players.Count == 0)
        {
            FindPlayers();
        }
        else if (!gameStarted)
        {
            gameStarted = true;
            StartGame();
        }
    }

    // Public
    // Getters
    public Dictionary<int, PlayerController> GetPlayers() { return players; }
    public int GetPlayerTurn() { return playerTurn; }

    // Starts the game and sets starting values for the game
    public void NextPlayerTurn()
    {
        // TODO Send end turn notification out to current player
        players[playerTurn].EndTurn();

        if (++playerTurn > players.Count)
        {
            playerTurn = 1;
        }

        // Move to the next player's turn
        Rpc_ChangeTurn(playerTurn);

        // TODO Send new turn notification out to players
        players[playerTurn].StartTurn();
    }

    // Private Functions
    // Call once at the start of the game
    private void StartGame()
    {
        playerTurn = 1;

        // TODO Send start of game notification out to players
        players[playerTurn].StartTurn();
    }

    // Find and Set Players
    private void FindPlayers()
    {
        PlayerController[] players_ = FindObjectsOfType<PlayerController>();
        if (players_.Length == Runner.SessionInfo.MaxPlayers)
        {
            foreach (PlayerController p in players_)
            {
                players[p.GetPlayerNum()] = p;
            }
        }
    }

    // Rpc stuff
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_ChangeTurn(int playerTurn_)
    {
        playerTurn = playerTurn_;
    }
}
