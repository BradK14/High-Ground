using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FighterB1 : Unit
{
    public override void Spawned()
    {
        base.Spawned();

        objectName = "Fighter B";
        objectType = "Supplementary Fighter";
        maxHP = 8;
        HP = maxHP;
        visionRange = 3;
        moveRange = 4;
        attack = 4;
        attackMinRange = 0;
        attackMaxRange = 3;
        attackType = "Ranged";
        description = "Attacking ends this unit's turn";
        ResetHPDisplayInfo();

        activeAbilities.Add(new MoveAbility());
        activeAbilities.Add(new AttackAbility());
        actionsLimited.Add(1);
        setupAbilitiesReady = true;
    }
}
