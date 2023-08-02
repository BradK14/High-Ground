using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionA1 : Structure
{
    public override void Spawned()
    {
        base.Spawned();

        objectName = "Military Production";
        objectType = "Military Production";
        maxHP = 10;
        HP = maxHP;
        visionRange = 1;
        moveRange = 0;
        attack = 0;
        attackMinRange = 0;
        attackMaxRange = 0;
        attackType = "N/A";
        description = "Basic military production structure, made for producing basic army units.";
        ResetHPDisplayInfo();

        activeAbilities.Add(new TrainBasicAbility());
        setupAbilitiesReady = true;
    }
}
