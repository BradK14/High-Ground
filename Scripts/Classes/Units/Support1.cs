using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Support1 : Unit
{
    public override void Spawned()
    {
        base.Spawned();

        objectName = "Support";
        objectType = "Support";
        maxHP = 5;
        HP = maxHP;
        visionRange = 3;
        moveRange = 4;
        attack = 0;
        attackMinRange = 0;
        attackMaxRange = 0;
        attackType = "N/A";
        description = "Support unit that gives aid to allies.";
        ResetHPDisplayInfo();

        activeAbilities.Add(new MoveAbility());
        // TODO: Add abilities to this unit
        setupAbilitiesReady = true;
    }
}
