using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstructionSite1 : Structure
{
    public override void Spawned()
    {
        base.Spawned();

        // These values are to be replaced by the setup coroutine
        objectName = "Construction site";
        maxHP = 12;
        // These values are shared by all construction sites
        objectType = "Construction site";
        HP = 3;  // Health starts low, then works its way up by being constructed by a builder
        visionRange = 0;
        moveRange = 0;
        attack = 0;
        attackMinRange = 0;
        attackMaxRange = 0;
        attackType = "N/A";
        description = "A building under construction.";
        ResetHPDisplayInfo();

        setupAbilitiesReady = true;
    }
    
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        isDestroyed = true;

        if (hasState)
        {
            if (Runner.IsServer)
            {
                curTile.Rpc_SetStructure(null);
                playerController.Rpc_ResetStructureList();

                if (HP >= maxHP)  // Correctly reached its goal to become a structure
                {
                    builder.Rpc_SetCS(null);
                    SetUpSpawnStructure();
                }
                else  // Only reset vision if it isn't already going to be done due to the start turn function
                {
                    playerController.Rpc_ResetVision();
                }
            }
        }
    }

    protected override IEnumerator Setup()
    {
        StartCoroutine(base.Setup());

        while (!setupComplete) { yield return null; }  // Wait for parent routine to finish

        while (structureUnderConstruction == "None") { yield return null; }

        // Set name and max health according to structureUnderConstruction variable
        switch (structureUnderConstruction)
        {
            case "Base":
                objectName = "Base";
                maxHP = 9;
                break;
            case "ProductionA":
                objectName = "Military Production";
                maxHP = 6;
                break;
            case "Wall":
                objectName = "Wall";
                maxHP = 9;
                break;
            case "ProductionB":
                objectName = "Advanced Military Production";
                maxHP = 6;
                break;
            case "StaticDefense":
                objectName = "Static Defense";
                maxHP = 9;
                break;
        }
    }

    // Since this object is part of a list, it needs a delay when being despawned while going through that list
    protected IEnumerator StartTurnDelayedDespawn()
    {
        while (!playerController.GetIsMyTurn()) { yield return null; }

        Rpc_DestroySelf();
    }

    // Other Functions
    public override void StartTurn()  // This function is called in an Rpc
    {
        base.StartTurn();

        if (builder != null && curTile.GetUnit() == null)
        {
            if (!builder.IsDestroyed())
            {
                HP += 3;

                ResetHPDisplayInfo();

                if (Runner.IsServer)
                {
                    if (HP >= maxHP)
                    {
                        StartCoroutine(StartTurnDelayedDespawn());  // Delayed despawn to avoid messing up the list while it is being gone through
                    }
                }
            }
        }
    }

    public void SetUpSpawnStructure()
    {
        curTile.Rpc_SetUpSpawnStructure(structureUnderConstruction, playerNum);
    }
}
