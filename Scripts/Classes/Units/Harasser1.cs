using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Harasser1 : Unit
{
    public override void Spawned()
    {
        base.Spawned();

        objectName = "Harasser";
        objectType = "Harasser";
        maxHP = 8;
        HP = maxHP;
        visionRange = 6;
        moveRange = 8;
        attack = 6;
        attackMinRange = 0;
        attackMaxRange = 1;
        attackType = "Melee";
        description = "Attacking ends this unit's turn";
        ResetHPDisplayInfo();

        activeAbilities.Add(new MoveAbility());
        activeAbilities.Add(new AttackAbility());
        actionsLimited.Add(1);
        setupAbilitiesReady = true;
    }
}
