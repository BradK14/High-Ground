using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterA1 : Unit
{
    public override void Spawned()
    {
        base.Spawned();

        objectName = "Fighter";
        objectType = "Fighter";
        maxHP = 10;
        HP = maxHP;
        visionRange = 3;
        moveRange = 4;
        attack = 5;
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
