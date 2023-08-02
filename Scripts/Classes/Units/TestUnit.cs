using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestUnit : Unit
{
    public override void Spawned()
    {
        base.Spawned();

        objectName = "Test unit";
        objectType = "TEST";
        maxHP = 5;
        HP = maxHP;
        visionRange = 4;
        moveRange = 6;
        attack = 2;
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
