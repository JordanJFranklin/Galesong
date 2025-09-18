using UnityEngine;
using System.Collections.Generic;
using DamageClass;

//Enums For Each Health Type
public enum HealthTypes {Base, Armor, Guard, DreamMagic, NightmareMagic}

namespace HealthClasses
{
    //Property Used To Send Healing Values To Health Bars
    [System.Serializable]
    public class HealingProperty
    {
        [Header("Heal Applications")]
        //Restricted Entity Type To Recieve Healing
        public EntityTarget Target;
        //Restricted Health Type To Recieve Healing
        public HealthTypes Health;
        //Amount Of Health Given
        public int HealingValue;

        //Constructor
        public HealingProperty(EntityTarget target, HealthTypes health, int healamount)
        {
            this.Target = target;
            this.Health = health;
            this.HealingValue = healamount;
        }
    }

    //Health Variable That All Entities Use
    [System.Serializable]
    public class HealthProperty
    {
        //Health Bar Type
        public HealthTypes HealthType;
        //Maximum Health That Cannot Be Exceeded 
        public int HealthCap;
        //Current Health
        public int HealthValue;

        //Constructor
        public HealthProperty(HealthTypes healthtype, int healthcap, int healthvalue)
        {
            HealthType = healthtype;
            HealthCap = healthcap;
            HealthValue = healthvalue;
        }
    }
}