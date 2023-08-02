using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Structure : PlayerPossessedObject
{
    // Variables
    protected bool isPathable;
    protected Unit builder;  // Reference for construction sites
    protected string structureUnderConstruction;

    // Functions
    // Start
    public override void Spawned()
    {
        structureUnderConstruction = "None";
        base.Spawned();
        isPathable = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        isDestroyed = true;

        if (hasState)
        {
            if (Runner.IsServer)
            {
                curTile.Rpc_SetStructure(null);
                PlayerController pc = FindObjectOfType<GameManager>().GetPlayers()[playerNum];
                pc.Rpc_ResetStructureList();
                pc.Rpc_ResetVision();
            }
        }
    }

    // Coroutines
    protected override IEnumerator Setup()
    {
        StartCoroutine(base.Setup());

        while (!ppoSetupComplete) { yield return null; }  // Wait for parent routine to finish

        bool structureAssigned = false;
        while (!structureAssigned)
        {
            if (Runner.LocalPlayer == player)
            {
                if (FindObjectOfType<GameManager>().GetPlayers()[playerNum].SetupIsComplete())
                {
                    FindObjectOfType<GameManager>().GetPlayers()[playerNum].AddStructure(this);
                    structureAssigned = true;
                }
            }
            else
            {
                structureAssigned = true;
            }

            yield return null;
        }

        setupComplete = true;
    }

    /* Various Functions
     * IsPathable()
     * TakeDamage(int damage)
     * HasMaxGatherers()
     */
    // Receive effects from other sources
    public bool IsPathable() { return isPathable; }
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        if (HP <= 0)
        {
            Rpc_DestroySelf();
        }
    }

    // For use by bases only
    public virtual bool IsSaturated() { return false; }
    [Rpc(RpcSources.All, RpcTargets.All)]
    public virtual void Rpc_SetGatherers(int g) { }

    // For use by construction sites
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetCSName(string name) { structureUnderConstruction = name; }
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetCSBuilder(Unit b) { builder = b; }
    public void StopBuildingCS()
    {
        if (builder != null)
        {
            builder.Rpc_SetCS(null);
        }
        
        Rpc_SetCSBuilder(null);
    }
}
