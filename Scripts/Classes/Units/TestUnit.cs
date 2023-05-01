using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestUnit : Unit
{
    // Functions
    // Start
    public override void Spawned()
    {
        unitName = "Test unit";
        unitType = "TEST";
        maxHP = 5;
        HP = maxHP;
        visionRange = 4;
        moveRange = 6;
        attack = 2;
        attackMinRange = 1;
        attackMaxRange = 2;
        attackType = "Ranged";
        description = "THIS IS ONLY A TEST UNIT, MEANT FOR TESTING STUFF.";
        moved = false;
        allActionsUsed = false;
    }
}
