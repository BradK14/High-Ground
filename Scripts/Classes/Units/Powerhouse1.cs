using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Powerhouse1 : Unit
{
    public override void Spawned()
    {
        base.Spawned();

        objectName = "Powerhouse";
        objectType = "Powerhouse";
        maxHP = 20;
        HP = maxHP;
        visionRange = 5;
        moveRange = 6;
        attack = 12;
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
