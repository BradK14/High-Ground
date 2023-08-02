using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Destroyer1 : Unit
{
    public override void Spawned()
    {
        base.Spawned();

        objectName = "Destroyer";
        objectType = "Destroyer";
        maxHP = 5;
        HP = maxHP;
        visionRange = 3;
        moveRange = 4;
        attack = 4;
        attackMinRange = 0;
        attackMaxRange = 2;
        attackType = "Ranged";
        description = "Glass cannon";
        ResetHPDisplayInfo();

        activeAbilities.Add(new MoveAbility());
        // TODO: Add more abilities here
        setupAbilitiesReady = true;
    }
}
