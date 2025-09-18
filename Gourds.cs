using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.UIElements.UxmlAttributeDescription;
using System.Linq;
using System;

public enum DrinkRestoreType {HP, SP, HP_SP}
public enum DrinkCalculationType {Flat, PercentMaximumHealth}
[CreateAssetMenu(fileName = "New Jug", menuName = "Gourds/Create New Jug")]

[System.Serializable]
public class ClayJug : ScriptableObject
{
    public string name;
    public int currentMix;
    public string description;
    public GameObject model;
    public List<GameObject> adornmentPoints;
    public GameObject passive;
    public GameObject passiveCopy;
    public List<string> harmonyItemNames;

    [Header("Gourd Stats")]
    public int Uses;
    public int Potency;
    public float Duration;
    public int MaxMixes;

    [Header("Buffs/Debuffs")]
    public List<StatusEffect> activeStatusEffects = new();

    public ClayJug(string name, string description, int uses, int potency, float duration, int maxMixes)
    {
        this.name = name;
        this.description = description;
        this.Uses = uses;
        this.Potency = potency;
        this.Duration = duration;
        this.MaxMixes = maxMixes;

        adornmentPoints = new List<GameObject>();
        harmonyItemNames = new List<string>();
        activeStatusEffects = new List<StatusEffect>();
    }
}

[CreateAssetMenu(fileName = "New Adornment", menuName = "Gourds/Create New Adornment")]
[System.Serializable]
public class Adornment : ScriptableObject
{
    public string name;
    public string description;
    public GameObject model;
    public GameObject passive;
    public GameObject passiveCopy;

    [Header("Gourd Stats")]
    public int Uses;
    public int Potency;
    public float Duration;
    public int MaxMixes;



    public List<StatEntry> Stats = new List<StatEntry>();

    [Header("Buffs/Debuffs")]
    public List<StatusEffect> activeStatusEffects = new();

    public Adornment(string name, string description, int uses, int potency, float duration, int maxMixes)
    {
        this.name = name;
        this.description = description;
        this.Uses = uses;
        this.Potency = potency;
        this.Duration = duration;
        this.MaxMixes = maxMixes;

        Stats = new List<StatEntry>();
        activeStatusEffects = new List<StatusEffect>();
    }
}

[Serializable]
public class DrinkRestoration
{
    public DrinkRestoreType RestoreType;
    public DrinkCalculationType CalcType;
    public float RestoreValue;

    // New fields for over-time effects
    public bool IsOverTime = false;
    public float Duration = 0f;      // Total duration in seconds
    public float TickInterval = 1f;  // How often to apply (e.g., every 1 sec)

    public float FlatHealAmount;
    public float FlatScarletRestore;

    public float OverTimeHealAmount;
    public float OverTimeScarletAmount;

    public bool ApplyOverTime => Duration > 0;
}


[CreateAssetMenu(fileName = "New Drink", menuName = "Gourds/Create New Drink")]
[System.Serializable]
public class Drink : ScriptableObject
{
    public string name;
    public string description;
    public GameObject passive;
    public GameObject passiveCopy;

    [Header("Healing/Scarlet Point Restore")]
    public List<DrinkRestoration> restortations;

    [Header("Gourd Mix Value")]
    public int MixValue;

    public List<StatEntry> Stats = new List<StatEntry>();

    [Header("Buffs/Debuffs")]
    public List<StatusEffect> activeStatusEffects = new();

    public Drink(string name, string description, int mixValue)
    {
        this.name = name;
        this.description = description;
        this.MixValue = mixValue;

        Stats = new List<StatEntry>();
        activeStatusEffects = new List<StatusEffect>();
    }
}

[System.Serializable]
public class Gourd
{
    [Header("Uses")]
    public int currentUses;

    [Header("Totaled Gourd Stats")]
    public int MaxUses;
    public int Potency;
    public float Duration;
    public int RemainingMixes;
    public int MaxMixes;


    [Header("Gourd")]
    public ClayJug jug;
    public List<Adornment> adornments;
    public List<Drink> drinks;

    public Gourd(ClayJug jug)
    {
        this.jug = jug;
        this.currentUses = jug.Uses;
        this.adornments = new List<Adornment>();
        this.drinks = new List<Drink>();
    }
}


public class Gourds : MonoBehaviour
{
    public bool canEquipJug;
    public bool canEquipDrinks;
    public bool canEquipAdornments;
    public Gourd playerGourd;
    private CharacterStats stats;
    private EntityEventManager eventMgr;

    void Awake()
    {
        stats = GetComponent<CharacterStats>();
        eventMgr = GetComponent<EntityEventManager>();
    }

    private void Start()
    {
        RecalculateStats();
    }

    // Update is called once per frame
    void Update()
    {
        var scheme = InputManager.Instance.PlayerInputScheme;

        if(scheme.WasPressedThisFrame(KeyActions.Gourd))
        {
            UseGourd(stats, 1);
        }
    }


    public void ApplyDrinkEffects()
    {
        if (playerGourd.drinks == null || playerGourd.drinks.Count == 0)
        {
            Debug.Log("No Drinks Inside The Gourd To Apply Any Effect.");
            return;
        }

        foreach (var drink in playerGourd.drinks)
        {
            float totalPotency = playerGourd.jug.Potency;

            foreach (Adornment adornment in playerGourd.adornments)
            {
                totalPotency += adornment.Potency;
            }

            foreach (var effect in drink.restortations)
            {
                float hpMax = stats.GetStatValue(StatType.Health);
                float spMax = stats.GetStatValue(StatType.Scarlet);

                float calcBaseHP = 0f;
                float calcBaseSP = 0f;

                // --- CALCULATE BASE RESTORE VALUES BASED ON TYPE ---
                if (effect.CalcType == DrinkCalculationType.Flat)
                {
                    calcBaseHP = effect.RestoreValue + (totalPotency * effect.RestoreValue);
                    calcBaseSP = effect.RestoreValue + (totalPotency * effect.RestoreValue);
                }
                else if (effect.CalcType == DrinkCalculationType.PercentMaximumHealth)
                {
                    float percentHP = hpMax * (effect.RestoreValue / 100f);
                    float percentSP = spMax * (effect.RestoreValue / 100f);

                    calcBaseHP = percentHP + (totalPotency * percentHP);
                    calcBaseSP = percentSP + (totalPotency * percentSP);
                }

                // --- APPLY EFFECT (INSTANT OR OVER TIME) ---
                if (effect.IsOverTime && effect.Duration > 0f)
                {
                    switch (effect.RestoreType)
                    {
                        case DrinkRestoreType.HP:
                            effect.OverTimeHealAmount = calcBaseHP;
                            stats.ApplyHealingOverTime(effect.OverTimeHealAmount, effect.Duration, effect.TickInterval);
                            break;

                        case DrinkRestoreType.SP:
                            effect.OverTimeScarletAmount = calcBaseSP;
                            stats.GainScarletOverTime(effect.OverTimeScarletAmount, effect.Duration, effect.TickInterval);
                            break;

                        case DrinkRestoreType.HP_SP:
                            effect.OverTimeHealAmount = calcBaseHP;
                            effect.OverTimeScarletAmount = calcBaseSP;

                            stats.ApplyHealingOverTime(effect.OverTimeHealAmount, effect.Duration, effect.TickInterval);
                            stats.GainScarletOverTime(effect.OverTimeScarletAmount, effect.Duration, effect.TickInterval);
                            break;
                    }
                }
                else // --- INSTANT RESTORE ---
                {
                    switch (effect.RestoreType)
                    {
                        case DrinkRestoreType.HP:
                            effect.FlatHealAmount = calcBaseHP;
                            stats.Heal(effect.FlatHealAmount);
                            break;

                        case DrinkRestoreType.SP:
                            effect.FlatScarletRestore = calcBaseSP;
                            stats.GainScarlet(effect.FlatScarletRestore);
                            break;

                        case DrinkRestoreType.HP_SP:
                            effect.FlatHealAmount = calcBaseHP;
                            effect.FlatScarletRestore = calcBaseSP;

                            stats.Heal(effect.FlatHealAmount);
                            stats.GainScarlet(effect.FlatScarletRestore);
                            break;
                    }
                }
            }
        }
    }

    public void UseGourd(CharacterStats Status, int consumedCharges)
    {
        if (playerGourd.currentUses <= 0)
        {
            Debug.Log("No uses left!");
            return;
        }

        playerGourd.currentUses -= consumedCharges;

        eventMgr.OnGourdUseTrigger();

        // Jug Effects
        if (playerGourd.jug != null)
        {
            if (playerGourd.jug.activeStatusEffects != null)
            {
                foreach (var effect in playerGourd.jug.activeStatusEffects)
                {
                    if (effect != null)
                        Status.AddStatusEffect(effect);
                }
            }

            // --- APPLY PASSIVE EFFECT ---
            if (playerGourd.jug.passive != null)
                playerGourd.jug.passiveCopy = Instantiate(playerGourd.jug.passive);
        }

        // Adornment Effects
        if (playerGourd.adornments != null)
        {
            foreach (var adornment in playerGourd.adornments)
            {
                if (adornment == null) continue;

                if (adornment.activeStatusEffects != null)
                {
                    foreach (var effect in adornment.activeStatusEffects)
                    {
                        if (effect != null)
                            Status.AddStatusEffect(effect);
                    }
                }

                // --- APPLY PASSIVE EFFECT ---
                if (adornment.passive != null)
                    adornment.passiveCopy = Instantiate(adornment.passive);
            }
        }

        // Drink Effects
        if (playerGourd.drinks != null)
        {
            foreach (var drink in playerGourd.drinks)
            {
                if (drink == null) continue;

                if (drink.activeStatusEffects != null)
                {
                    foreach (var effect in drink.activeStatusEffects)
                    {
                        if (effect != null)
                            Status.AddStatusEffect(effect); // Apply buffs (but not heals or Scarlet points)
                    }
                }

                // --- APPLY PASSIVE EFFECT ---
                if (drink.passive != null)
                    drink.passiveCopy = Instantiate(drink.passive);
            }
        }

        ApplyDrinkEffects(); // Apply healing, SP restoration, and over-time effects
        Debug.Log("Consumed Gourd Charge!");
    }


    public void DrainGourdCharge(int count)
    {
        playerGourd.currentUses -= count;
    }

    public void RefillGourd()
    {
        playerGourd.currentUses = Mathf.RoundToInt(playerGourd.jug.Uses + GetAdornmentUseCount().Uses + stats.GetStatValue(StatType.GourdCharge));
    }

    public Adornment GetAdornmentUseCount()
    {
        Adornment allStats = new Adornment("results", "", 0, 0,0,0);

        if (playerGourd.adornments == null)
        {
            Debug.LogWarning("Not equipped.");
            return allStats;
        }


        float adornmentUses = 0;
        float adornmentDuration = 0;
        float adornmentPotency = 0;
        float adornmentMixes = 0;

        // Adornment Effects
        foreach (var adornment in playerGourd.adornments)
        {
            adornmentUses += adornment.Uses;
            adornmentDuration += adornment.Duration;
            adornmentPotency += adornment.Potency;
            adornmentMixes += adornment.MaxMixes;
        }

        return allStats;
    }

    public List<StatEntry> GetAllCombinedStats()
    {
        List<StatEntry> combined = new();

        combined.AddRange(playerGourd.jug.activeStatusEffects);
        foreach (var adornment in playerGourd.adornments)
            combined.AddRange(adornment.Stats);
        foreach (var drink in playerGourd.drinks)
            combined.AddRange(drink.Stats);

        return combined;
    }

    public bool EquipAdornment(Adornment newAdornment)
    {
        if (newAdornment == null)
            return false;

        playerGourd.adornments.Add(newAdornment);
        RecalculateStats();
        return true;
    }

    public bool UnequipAdornment(Adornment adornmentToRemove)
    {
        bool result = playerGourd.adornments.Remove(adornmentToRemove);
        if (result)
            RecalculateStats();
        return result;
    }

    public void SetEquippedJug(ClayJug newJug)
    {
        if (newJug == null)
            return;

        playerGourd.jug = newJug;
        playerGourd.currentUses = newJug.Uses;
    }

    public int GetCurrentTotalMix()
    {
        int totalMix = 0;
        foreach (var drink in playerGourd.drinks)
        {
            totalMix += drink.MixValue;
        }
        return totalMix;
    }

    public bool AddDrink(Drink drinkToAdd)
    {
        if (drinkToAdd == null)
            return false;

        int currentMix = playerGourd.currentUses;
        int mixCapacity = playerGourd.MaxMixes;

        if (currentMix + drinkToAdd.MixValue > mixCapacity)
        {
            Debug.Log("Too much mix! Cannot add drink.");
            return false;
        }

        playerGourd.drinks.Add(drinkToAdd);
        playerGourd.RemainingMixes += drinkToAdd.MixValue;
        RecalculateStats();
        return true;
    }


    public bool RemoveDrink(Drink drinkToRemove)
    {
        bool result = playerGourd.drinks.Remove(drinkToRemove);
        if (result)
        {
            playerGourd.RemainingMixes -= drinkToRemove.MixValue;
            RecalculateStats();
        }
        return result;
    }

    public void RecalculateStats()
    {
        playerGourd.MaxUses = Mathf.RoundToInt(playerGourd.jug.Uses + stats.GetStatValue(StatType.GourdCharge) + GetAdornmentUseCount().Uses);
        playerGourd.Potency = playerGourd.jug.Potency + GetAdornmentUseCount().Potency;
        playerGourd.Duration = playerGourd.jug.Duration + GetAdornmentUseCount().Duration;
        playerGourd.MaxMixes = playerGourd.jug.MaxMixes + GetAdornmentUseCount().MaxMixes;

        foreach (var adornment in playerGourd.adornments)
        {
            playerGourd.MaxUses += adornment.Uses;
            playerGourd.Potency += adornment.Potency;
            playerGourd.Duration += adornment.Duration;
            playerGourd.MaxMixes += adornment.MaxMixes;
        }

        playerGourd.RemainingMixes = playerGourd.MaxMixes - playerGourd.drinks.Sum(d => d.MixValue);
    }
}