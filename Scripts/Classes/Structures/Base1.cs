using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Base1 : Structure
{
    // Variables
    private int gatherers;
    private int maxGatherers;

    /* Functions
     * Spawned()
     * Setup()
     * Despawned()
     * StartTurn()
     * EndTurn()
     */

    public override void Spawned()
    {
        base.Spawned();

        objectName = "Base";
        objectType = "Base";
        maxHP = 20;
        HP = maxHP;
        visionRange = 3;
        moveRange = 0;
        attack = 0;
        attackMinRange = 0;
        attackMaxRange = 0;
        attackType = "N/A";
        description = "Major economic advancement structure.  Builds gatherers and builders.  Advances tier rank.";
        ResetHPDisplayInfo();

        activeAbilities.Add(new TrainBuilderAbility());
        setupAbilitiesReady = true;

        // Non-Inherited variables
        gatherers = 0;
        maxGatherers = 10;
    }

    protected override IEnumerator Setup()
    {
        StartCoroutine(base.Setup());

        while (playerNum <= 0) { yield return new WaitForSeconds(0.1f); }

        bool gmReady = false;
        GameManager gm = null;
        while (!gmReady)
        {
            gm = FindObjectOfType<GameManager>();
            if (gm != null)
            {
                gmReady = true;
            }

            yield return null;
        }

        while (!gm.SetupIsComplete()) { yield return null; }

        gm.GetPlayers()[playerNum].AddBase();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        base.Despawned(runner, hasState);

        if (hasState)
        {
            if (Runner.IsServer)
            {
                PlayerController pc = FindObjectOfType<GameManager>().GetPlayers()[playerNum];
                pc.RemoveBase();
            }
        }
    }

    public override void StartTurn()
    {
        base.StartTurn();

        // First, gather resources according to current worker count
        int res = curTile.GetResources();
        int resGathered = 0;
        for (int i = 0; i < gatherers; i++)
        {
            if (res >= 2)
            {
                resGathered += 2;
                res -= 2;
            }
            else
            {
                resGathered += 1;
                res = 0;
            }
        }

        playerController.AddResources(resGathered);
        curTile.Rpc_SetResources(res);

        // Then, increment gatherer count
        if (gatherers < maxGatherers)
        {
            gatherers++;
        }

        Rpc_SetGatherers(gatherers);
    }

    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        // If this is already dead
        if (HP <= 0)
        {
            return;
        }

        // Damage workers by only half the damage taken
        gatherers -= (damage / 2);

        // Gatherers can not go below 0
        if (gatherers < 0)
        {
            gatherers = 0;
        }

        Rpc_SetGatherers(gatherers);
    }

    public override bool IsSaturated()
    {
        if (gatherers == maxGatherers)
        {
            return true;
        }

        return false;
    }

    // Rpcs
    [Rpc(RpcSources.All, RpcTargets.All)]
    public override void Rpc_SetGatherers(int g)
    {
        gatherers = g;
        description = "Gatherers: " + g;
    }
}
