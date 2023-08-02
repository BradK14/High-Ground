using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StaticDefense1 : Structure
{
    public override void Spawned()
    {
        base.Spawned();

        objectName = "Static Defense";
        objectType = "Static Defense";
        maxHP = 12;
        HP = maxHP;
        visionRange = 3;
        moveRange = 0;
        attack = 5;
        attackMinRange = 0;
        attackMaxRange = 3;
        attackType = "Ranged";
        description = "Defensive structure that attacks enemies from decent range";
        ResetHPDisplayInfo();

        activeAbilities.Add(new AttackAbility());
        setupAbilitiesReady = true;
    }
}
