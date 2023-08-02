using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall1 : Structure
{
    public override void Spawned()
    {
        base.Spawned();

        objectName = "Wall";
        objectType = "Wall";
        maxHP = 12;
        HP = maxHP;
        visionRange = 1;
        moveRange = 0;
        attack = 0;
        attackMinRange = 0;
        attackMaxRange = 0;
        attackType = "N/A";
        description = "A wall that blocks vision from enemies, and grants high ground bonuses to friendly units";
        ResetHPDisplayInfo();

        setupAbilitiesReady = true;
    }
}
