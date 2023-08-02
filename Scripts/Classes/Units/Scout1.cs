using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scout1 : Unit
{
    public override void Spawned()
    {
        base.Spawned();

        objectName = "Scout";
        objectType = "Scout";
        maxHP = 6;
        HP = maxHP;
        visionRange = 6;
        moveRange = 6;
        attack = 3;
        attackMinRange = 0;
        attackMaxRange = 2;
        attackType = "Ranged";
        description = "Attacking ends this unit's turn";
        ResetHPDisplayInfo();

        activeAbilities.Add(new MoveAbility());
        activeAbilities.Add(new AttackAbility());
        actionsLimited.Add(1);
        setupAbilitiesReady = true;
    }
}
