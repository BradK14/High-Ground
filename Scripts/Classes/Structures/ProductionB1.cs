using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProductionB1 : Structure
{
    public override void Spawned()
    {
        base.Spawned();

        objectName = "Advanced Military Production";
        objectType = "Advanced Military Production";
        maxHP = 10;
        HP = maxHP;
        visionRange = 1;
        moveRange = 0;
        attack = 0;
        attackMinRange = 0;
        attackMaxRange = 0;
        attackType = "N/A";
        description = "Advanced military production structure, made for building advanced military units.";
        ResetHPDisplayInfo();

        activeAbilities.Add(new TrainAdvancedAbility());
        setupAbilitiesReady = true;
    }
}
