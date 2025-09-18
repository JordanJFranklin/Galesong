using UnityEngine;
using System.Collections.Generic;
using HealthClasses;
using System;

public enum EntityTarget {Player, Enemy}

[System.Serializable]
public class DamageProperty
{
    [Header("Base Properties")]
    public int BaseDamage;
    public GameObject Sender;
    public EntityTarget Target;
    public float DamageScaleFactor;
    public int CalculatedDamage;

    [Header("Damage Bonuses")]
    public int BonusAttackFlat;
    public float BonusAttackPercentage;
    
    public DamageProperty (int basedamage, int damagescale, EntityTarget target, GameObject sender)
    {
        this.BaseDamage = basedamage;
        this.DamageScaleFactor = damagescale;
        this.Target = target;
        this.Sender = sender;
    }

    public void ApplyAttackFlatDamage(int flatDamage)
    {
        this.BonusAttackFlat += flatDamage;
    }

    public void ApplyAttackPercentageDamage(float percentDamage)
    {
        this.BonusAttackPercentage += percentDamage;
    }
}


namespace DamageClass
{
    
}