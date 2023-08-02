using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Builder1 : Unit
{
    public override void Spawned()
    {
        base.Spawned();

        objectName = "Builder";
        objectType = "Builder";
        maxHP = 5;
        HP = maxHP;
        visionRange = 3;
        moveRange = 4;
        attack = 2;
        attackMinRange = 0;
        attackMaxRange = 2;
        attackType = "Ranged";
        description = "Attacking or Building ends this unit's turn";
        ResetHPDisplayInfo();

        activeAbilities.Add(new MoveAbility());
        activeAbilities.Add(new AttackAbility());
        activeAbilities.Add(new BuildAbility());
        activeAbilities.Add(new RepairAbility());
        actionsLimited.Add(1);
        actionsLimited.Add(2);
        actionsLimited.Add(3);
        setupAbilitiesReady = true;
    }

    public override void StartTurn()
    {
        base.StartTurn();

        // Apply repairs if currently repairing a structure
        if (constructionSite != null)
        {
            if (constructionSite.GetTile().GetUnit() == null)  // Cannot repair while units stand on the building
            {
                if (constructionSite.GetObjectType() != "Construction site")  // Construction sites recieve health in their own function
                {
                    constructionSite.RecieveHP(2);
                }
            }
        }
    }

    // When a builder uses an ability, they may have to stop construction of their current construction site
    public override bool UseAbility(TileInfo tile)
    {
        // First save information that may normally be changed
        Structure cs = constructionSite;
        int sa = selectedAbility;

        // Run the base function
        bool abilityUsed = base.UseAbility(tile);

        // Only change values when an ability was successfully used, and we are currently building something
        if (abilityUsed && constructionSite != null)
        {
            if (sa == 0 || sa == 1)  // Move or Attack
            {
                StopBuildingCS();
            }
            else if (sa == 2)  // Build
            {
                cs.Rpc_SetCSBuilder(null);
            }
            else if (sa == 3)  // Repair
            {
                //if (cs != constructionSite)
                //{
                //    cs.Rpc_SetCSBuilder(null);
                //}
            }
        }

        return abilityUsed;
    }
}
