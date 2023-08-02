using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestStructure : Structure
{
    public override void Spawned()
    {
        base.Spawned();

        objectName = "Test structure";
        objectType = "TEST";
        maxHP = 6;
        HP = maxHP;
        visionRange = 2;
        moveRange = 0;
        attack = 3;
        attackMinRange = 0;
        attackMaxRange = 2;
        attackType = "Ranged";
        description = "THIS STRUCTURE IS ONLY FOR TESTING";
        ResetHPDisplayInfo();

        activeAbilities.Add(new AttackAbility());
        setupAbilitiesReady = true;
    }
}
