using System.Collections.Generic;
using System;
using UnityEngine;
using System.Collections;
using UnityEditor;
using Ink.Runtime;
using System.Linq;
using Ink;

/// <summary>
/// Attack Types that damages can scale on. They can scale on multiple tags
/// </summary>
public enum AttackType
{
    LightAttack,
    HeavyAttack,
    Spell,
    Symphum,
    Fan,
    Summon,
    Enemy,
    Status
}


/// <summary>
/// Represents the category/type of an entity in the game world.
/// </summary>
public enum EntityType
{
    Obstacle,
    Enemy,
    Summon,
    Player
}

/// <summary>
/// Type of modifier applied to a stat (e.g., bonus or penalty, flat or percent-based).
/// </summary>
public enum StatModifierType
{
    FlatBonus,
    FlatDebuff,
    PercentBonus,
    PercentDebuff
}

/// <summary>
/// Represents all the stats that can be affected by cards, abilities, or effects.
/// </summary>
public enum StatType
{
    OverHealth,
    Health,
    Defense,
    Scarlet,
    SPBonus,
    SPRegen,
    SPFocus,
    MovementSpeed,
    BoostSpeed,
    JumpHeight,
    GravityScale,
    DashDuration,
    EvasionChance,
    CastSpeed,
    AttackSpeed,
    AttackDamage,
    CriticalChance,
    CriticalDamage,
    AttackRangeBonus,
    SpellDamageBonus,
    SpellDurationBonus,
    LightAttackBonus,
    HeavyAttackBonus,
    FanDamageBonus,
    SymphumDamageBonus,
    SummonDamage,
    SummonDuration,
    SummonResistance,
    SummonHealth,
    HealingBonus,
    HealingReduction,
    CooldownReduction,
    CollectionRange,
    HaliBonus,
    BlockPower,
    GourdCharge,
    IncreasedDamageTaken,

    // Duration Resistance Stats
    GlimmerResist,
    StunResist,
    PoisonResist,
    FireResist,
    WeakenResist,
    SluggishResist,
    SlowedResist,
    ScarletDrainResist,
    CurseboundResist,
    BrokenBonesResist,
    ShatteredArmorResist,
    BleedingResist,
    SealedResist,
    GalelockResist,
    LesserStrikesResist
}

/// <summary>
/// Classification of status effects applied to a target.
/// </summary>
public enum StatusEffectType
{
    // Buffs / Debuffs
    StatIncrease,
    StatDecrease,
    Glimmer,
    Stun,
    Poison,
    Burn,
    Weakened,
    Sluggish,
    Slowed,
    ScarletDrain,
    Cursebound,
    BrokenBones,
    ShatteredArmor,
    Bleeding,
    Sealed,
    Galelock,
    LesserStrikes,
    Petrified,
    IncreasedDamageTaken,

    // Temporary boosts
    MovementBoost,
    JumpBoost,
    OverhealthBoost,
    ScarletRegenBoost,

    // Resistances
    GeneralResistanceBoost,
    SpecificResistanceBoost,

    // Immunities
    TemporaryImmunity,
    BurnImmunity,
    PoisonImmunity,
    StunImmunity,
    PetrifyImmunity,
    WeakenedImmunity,
    SluggishImmunity,
    SlowedImmunity,
    ScarletDrainImmunity,
    CurseboundImmunity,
    BrokenBonesImmunity,
    ShatteredArmorImmunity,
    BleedingImmunity,
    SealedImmunity,
    GalelockImmunity,
    LesserStrikesImmunity,
    GlimmerImmunity,
    HealBlockImmunity,
    AllImmunity,

    // Combat / Spell Buffs
    SpellEmpowerment,
    SpellDuplication,
    EmpoweredAttack,
    AutoCritical,
    ConditionalCritical,
    CooldownReductionBoost,

    // Conditional Modifiers
    BonusVsUnderground,
    BonusVsLifted,
    BonusVsKnockedDown,
    BonusVsMarked,

    // Mobility
    AirJump,
    SlowFall,
    Hover,
    Ethereal,
    Cloaked,

    // Shields / Clones
    ReflectShield,
    WindShield,
    Decoy,

    // Card Modifiers
    CardTriggerBoost,
    CardRefund,
    CardAmplification,

    // Revival / Healing
    ReviveOnDeath,
    SecondWind,
    HealingOverTime,
    ScarletRegenOverTime,
    HealBlock,
    HealingBonus,

    // Special stacking logic
    SkillRepetitionBuff,

    // Misc fallback
    Unique
}

/// <summary>
/// Determines how a status effect stacks with itself.
/// </summary>
public enum StatusEffectStacking
{
    None,
    Stackable,
    RefreshOnly
}

[System.Serializable]
public class Attack
{
    public float BaseDamage = 0;
    public float FinalDamage = 0;
    public float DamageMultiplier = 0;
    public bool IgnoreDefense = false;
    public EntityType Sender = EntityType.Player;
    public List<AttackType> AttackTypes = new List<AttackType>();
    public List<StatusEffect> StatusEffects = new List<StatusEffect>();
    public float CritChance = 0f;
    public float CritMultiplier = 1.5f;
    public bool IsCriticalHit = false;

    public void ApplyStatScaling(CharacterStats stats, StatType statType)
    {
        float statValue = stats.GetStatValue(statType);
        BaseDamage = statValue * DamageMultiplier;
        FinalDamage = CalculateFinalDamage();
    }

    public float CalculateFinalDamage()
    {
        IsCriticalHit = UnityEngine.Random.value < CritChance;
        float damage = BaseDamage;

        if (IsCriticalHit)
        {
            damage *= CritMultiplier;
        }

        return damage;
    }
}

[System.Serializable]
public struct StatModifier
{
    public StatModifierType Type;
    public float Value;
    public string SourceUID; // Unique ID of the source that added this modifier

    public StatModifier(StatModifierType type, float value, string sourceUID)
    {
        Type = type;
        Value = value;
        SourceUID = sourceUID;
    }
}

[System.Serializable]
public class StatEntry
{
    [Tooltip("The type of stat affected.")]
    public StatType Type;

    [Tooltip("Base value for the stat.")]
    public float BaseValue;

    [Tooltip("Active modifiers for this stat.")]
    public List<StatModifier> ActiveModifiers = new List<StatModifier>();
}

[Serializable]
public class Stat
{
    public event Action OnValueChanged;
    public StatEntry Entry;

    private readonly Dictionary<string, List<StatModifier>> modifierGroups = new();

    public void AddModifier(StatModifier modifier, string sourceUID)
    {
        if (!modifierGroups.ContainsKey(sourceUID))
        {
            modifierGroups[sourceUID] = new List<StatModifier>();
        }

        // Create new modifier with source UID
        StatModifier modWithUID = new StatModifier(modifier.Type, modifier.Value, sourceUID);

        modifierGroups[sourceUID].Add(modWithUID);
        Entry.ActiveModifiers.Add(modWithUID);
        NotifyValueChanged();
    }

    public void RemoveModifier(string sourceUID)
    {
        if (!modifierGroups.ContainsKey(sourceUID)) return;

        // Remove all modifiers with matching source UID
        Entry.ActiveModifiers.RemoveAll(mod => mod.SourceUID == sourceUID);
        modifierGroups.Remove(sourceUID);
        NotifyValueChanged();
    }

    public float GetFinalValue()
    {
        float flatBonus = 0f, flatDebuff = 0f, percentBonus = 0f, percentDebuff = 0f;
        float baseVal = Entry.BaseValue;

        foreach (var modifier in Entry.ActiveModifiers)
        {
            switch (modifier.Type)
            {
                case StatModifierType.FlatBonus: flatBonus += modifier.Value; break;
                case StatModifierType.FlatDebuff: flatDebuff += modifier.Value; break;
                case StatModifierType.PercentBonus: percentBonus += modifier.Value; break;
                case StatModifierType.PercentDebuff: percentDebuff += modifier.Value; break;
            }
        }

        float final = (baseVal + (flatBonus - flatDebuff)) *
                     (1 + (percentBonus - percentDebuff));

        return Mathf.Max(0f, final);
    }

    public void ClearModifiers()
    {
        if (modifierGroups.Count > 0)
        {
            modifierGroups.Clear();
            Entry.ActiveModifiers.Clear(); 
            NotifyValueChanged();
        }
    }

    // Helper method to safely invoke the event
    public void NotifyValueChanged()
    {
        if (OnValueChanged != null)
        {
            OnValueChanged.Invoke();
        }
    }
}

[System.Serializable]
public class StatusEffect
{
    // Add a unique ID for each status effect instance
    public string uid = System.Guid.NewGuid().ToString();

    [Tooltip("Name of the effect (for display/logging).")]
    public string Name;

    [Tooltip("Type of status effect (buff/debuff/immunity/etc).")]
    public StatusEffectType EffectType;

    [Tooltip("Time remaining for this effect.")]
    public float RemainingDuration;

    [Tooltip("Original duration of this effect.")]
    public float TotalDuration;

    [Tooltip("How this status effect stacks.")]
    public StatusEffectStacking StackingBehavior = StatusEffectStacking.None;

    [Tooltip("Current number of stacks (if applicable).")]
    public int StackCount = 1;

    [Tooltip("Maximum allowed stacks.")]
    public int MaxStacks = 1;

    [Tooltip("Optional scriptable object defining unique behavior.")]
    public GameObject UniqueScriptableObject;

    [Tooltip("List of stat modifiers applied by this effect.")]
    public List<ModifierEntry> Modifiers = new();

    [Tooltip("Value per stack for each modifier (if stackable)")]
    public Dictionary<StatType, float> StackValues = new();

    public void InitializeStackValues()
    {
        foreach (var mod in Modifiers)
        {
            if (!StackValues.ContainsKey(mod.StatType))
            {
                StackValues[mod.StatType] = mod.Modifier.Value;
            }
        }
    }

    public StatusEffect(string name, StatusEffectType effectType, float duration, List<ModifierEntry> modifiers,
                        StatusEffectStacking stackingBehavior = StatusEffectStacking.None, int maxStacks = 1)
    {
        uid = System.Guid.NewGuid().ToString(); // Generate unique ID
        Name = name;
        EffectType = effectType;
        RemainingDuration = duration;
        TotalDuration = duration;
        Modifiers = modifiers;
        StackingBehavior = stackingBehavior;
        MaxStacks = maxStacks;
        StackCount = 1;
    }

    [System.Serializable]
    public struct ModifierEntry
    {
        public StatType StatType;
        public StatModifier Modifier;
    }

    public void Tick(float deltaTime, float durationResistance = 1f)
    {
        if (RemainingDuration > 0)
            RemainingDuration -= deltaTime * durationResistance;
    }

    public bool IsExpired => RemainingDuration <= 0f && RemainingDuration != -1f;
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(StatusEffect))]
public class StatusEffectDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = 2f;
        Rect rect = new Rect(position.x, position.y, position.width, lineHeight);

        SerializedProperty nameProp = property.FindPropertyRelative("Name");
        SerializedProperty effectTypeProp = property.FindPropertyRelative("EffectType");
        SerializedProperty uniqueObjectProp = property.FindPropertyRelative("UniqueScriptableObject");
        SerializedProperty modifiersProp = property.FindPropertyRelative("Modifiers");
        SerializedProperty remainingDurationProp = property.FindPropertyRelative("RemainingDuration");
        SerializedProperty totalDurationProp = property.FindPropertyRelative("TotalDuration");
        SerializedProperty stackingProp = property.FindPropertyRelative("StackingBehavior");
        SerializedProperty stackCountProp = property.FindPropertyRelative("StackCount");
        SerializedProperty maxStacksProp = property.FindPropertyRelative("MaxStacks");

        StatusEffectType effectType = (StatusEffectType)effectTypeProp.enumValueIndex;

        EditorGUI.PropertyField(rect, nameProp);
        rect.y += lineHeight + spacing;

        EditorGUI.PropertyField(rect, effectTypeProp);
        rect.y += lineHeight + spacing;

        EditorGUI.PropertyField(rect, remainingDurationProp);
        rect.y += lineHeight + spacing;

        EditorGUI.PropertyField(rect, totalDurationProp);
        rect.y += lineHeight + spacing;

        if (effectType == StatusEffectType.Unique)
        {
            EditorGUI.PropertyField(rect, uniqueObjectProp);
            rect.y += lineHeight + spacing;

            EditorGUI.PropertyField(rect, modifiersProp, true);
            rect.y += EditorGUI.GetPropertyHeight(modifiersProp, true) + spacing;
        }
        else
        {
            EditorGUI.PropertyField(rect, stackingProp);
            rect.y += lineHeight + spacing;

            if ((StatusEffectStacking)stackingProp.enumValueIndex == StatusEffectStacking.Stackable)
            {
                EditorGUI.PropertyField(rect, stackCountProp);
                rect.y += lineHeight + spacing;

                EditorGUI.PropertyField(rect, maxStacksProp);
                rect.y += lineHeight + spacing;
            }

            EditorGUI.PropertyField(rect, modifiersProp, true);
            rect.y += EditorGUI.GetPropertyHeight(modifiersProp, true) + spacing;
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight + 2f;

        SerializedProperty effectTypeProp = property.FindPropertyRelative("EffectType");
        SerializedProperty modifiersProp = property.FindPropertyRelative("Modifiers");
        SerializedProperty stackingProp = property.FindPropertyRelative("StackingBehavior");

        StatusEffectType effectType = (StatusEffectType)effectTypeProp.enumValueIndex;
        float height = lineHeight * 4f;

        if (effectType == StatusEffectType.Unique)
        {
            height += lineHeight;
            height += EditorGUI.GetPropertyHeight(modifiersProp, true) + 2f;
        }
        else
        {
            height += lineHeight;

            if ((StatusEffectStacking)stackingProp.enumValueIndex == StatusEffectStacking.Stackable)
            {
                height += lineHeight * 2;
            }

            height += EditorGUI.GetPropertyHeight(modifiersProp, true) + 2f;
        }

        return height;
    }
}
#endif

public class CharacterStats : MonoBehaviour
{
    [Header("Object Type")]
    public EntityType currEntity;

    [Header("Current Values")]
    public float currentOverHealth = 0;
    public float currentHealth = 0;
    public float maxHealth = 0;
    public float currentScarlet = 0;
    public float maxScarlet = 0;
    public float currentBlockPower = 0;
    public float maxBlockPower = 0;

    [Tooltip("Serialized list of base stat definitions.")]
    public List<StatEntry> statEntries = new();

    [NonSerialized] private Dictionary<StatType, Stat> runtimeStats = new();

    [Header("Status Effects")]
    public List<StatusEffect> activeStatusEffects = new();
    private Dictionary<StatType, float> durationResistances = new();
    private Dictionary<StatusEffectType, int> activeStateCounters = new();

    [Header("Status State Flags")]
    public bool IsStunned = false;
    public bool IsPoisoned = false;
    public bool IsBurning = false;
    public bool IsPetrified = false;
    public bool IsBleeding = false;
    public bool IsScarletDrained = false;
    public bool IsSlowed = false;
    public bool IsWeakened = false;
    public bool IsSluggish = false;
    public bool HasBrokenBones = false;
    public bool IsShatteredArmor = false;
    public bool IsCursebound = false;
    public bool IsSealed = false;
    public bool HasGlimmer = false;
    public bool IsGalelocked = false;
    public bool HasLesserStrikes = false;

    public EntityEventManager eventMgr;

    private void Awake()
    {
        eventMgr = GetComponent<EntityEventManager>();

        InitializeStats();

        if (currEntity == EntityType.Player)
        {
            SetPlayerDefaultBaseValues();
        }

        if (currEntity == EntityType.Enemy)
        {
            eventMgr.OnComboUpdated += HandleComboUpdated;
            eventMgr.OnComboExpired += RemoveCombo;
        }

        foreach (var stat in runtimeStats)
        {
            var type = stat.Value.Entry.Type;
            stat.Value.OnValueChanged += () => HandleStatChanged(type);
        }
    }

    private void Start()
    {
        currentScarlet = Mathf.RoundToInt(GetStatValue(StatType.Scarlet));
        currentHealth = Mathf.RoundToInt(GetStatValue(StatType.Health));
        currentBlockPower = Mathf.RoundToInt(GetStatValue(StatType.BlockPower));
    }

    void HandleComboUpdated(ComboManager combo)
    {
        if (TryGetComponent<CardDropper>(out CardDropper dropper))
        {
            dropper.comboCount = combo.comboCount;
        }
    }

    void RemoveCombo()
    {
        if (TryGetComponent<CardDropper>(out CardDropper dropper))
        {
            dropper.comboCount = 0;
        }
    }

    public void ApplyStatusEffect(StatusEffect newEffect)
    {
        string effectUID = newEffect.uid; // Use status effect's UID

        foreach (var modEntry in newEffect.Modifiers)
        {
            // Create modifier with source UID
            StatModifier modWithUID = new StatModifier(
                modEntry.Modifier.Type,
                modEntry.Modifier.Value,
                effectUID
            );

            AddModifierToStat(modEntry.StatType, modWithUID, effectUID);
        }
    }

    /// <summary>
    /// Removes a status effect by its name, including its stat modifiers and related flags.
    /// </summary>
    /// <param name="name">The name of the status effect to remove.</param>
    public void RemoveStatusEffect(string name)
    {
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeStatusEffects[i];
            if (effect.Name == name)
            {
                string effectUID = effect.uid;

                foreach (var entry in effect.Modifiers)
                {
                    Stat stat = GetStat(entry.StatType);
                    if (stat != null)
                    {
                        stat.RemoveModifier(effectUID);
                    }
                }

                SetFlag(effect.EffectType, false);
                activeStatusEffects.RemoveAt(i);
            }
        }
    }

    private void Update()
    {
        TickStatusEffects(Time.deltaTime);
        SetPlayerDefaultBaseValues();
    }

    private void TickStatusEffects(float deltaTime)
    {
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            StatusEffect effect = activeStatusEffects[i];
            effect.Tick(deltaTime, 1f); // optionally apply duration resist here

            if (effect.IsExpired)
            {
                RemoveStatusEffect(effect.Name);
            }
        }
    }

    private void ApplyModifier(StatType type, StatModifier mod, string sourceKey)
    {
        AddModifierToStat(type, mod, sourceKey);
    }

    private void RemoveModifier(StatType type, string sourceKey)
    {
        RemoveModifierFromStat(type, sourceKey);
    }

    // Update the CreateKey method
    public string CreateKey(StatusEffect effect)
    {
        return effect.uid;
    }

    private void OnDestroy()
    {
        eventMgr.OnComboUpdated -= HandleComboUpdated;
        eventMgr.OnComboExpired -= RemoveCombo;
    }

    /// <summary>
    /// Initializes the stat library and duration resistances from serialized entries.
    /// Applies default player base values if this is a Player entity.
    /// </summary>
    private void InitializeStats()
    {
        runtimeStats = new Dictionary<StatType, Stat>();

        foreach (var entry in statEntries)
        {
            var stat = new Stat { Entry = entry };
            runtimeStats[entry.Type] = stat;
            stat.OnValueChanged += () => HandleStatChanged(entry.Type);

            // Initialize with notification
            stat.NotifyValueChanged();
        }

        if (currEntity == EntityType.Player)
        {
            SetPlayerDefaultBaseValues();
        }
    }

    // Helper to create and add stat entries
    private void AddStatEntry(StatType type)
    {
        if (!statEntries.Any(e => e.Type == type))
        {
            statEntries.Add(new StatEntry
            {
                Type = type,
                BaseValue = GetDefaultBaseValue(type)
            });
        }
    }

    // Default base values setup
    private float GetDefaultBaseValue(StatType type, StatModifierType modType)
    {
        // Set reasonable defaults
        if (modType == StatModifierType.FlatBonus)
        {
            return type switch
            {
                StatType.Health => 25f,
                StatType.Scarlet => 50f,
                StatType.BlockPower => 3f,
                StatType.GravityScale => 2f,
                _ => 0f
            };
        }
        return 0f;
    }

    /// <summary>
    /// Gets Base Values of a given stat
    /// </summary>
    public void SetBaseValue(StatType statType, float newValue)
    {
        var stat = GetStat(statType);
        if (stat != null)
        {
            stat.Entry.BaseValue = newValue;
            stat.NotifyValueChanged();  // Use helper method
        }
        else
        {
            var newEntry = new StatEntry
            {
                Type = statType,
                BaseValue = newValue
            };
            statEntries.Add(newEntry);
            var newStat = new Stat { Entry = newEntry };
            runtimeStats[statType] = newStat;
            newStat.OnValueChanged += () => HandleStatChanged(statType);

            // Initialize with notification
            newStat.NotifyValueChanged();
        }
    }

    public void SetPlayerDefaultBaseValues()
    {
        foreach (StatType type in Enum.GetValues(typeof(StatType)))
        {
            // Special handling for gravity scale
            if (type == StatType.GravityScale && TryGetComponent<PlayerDriver>(out PlayerDriver playerDriver) && playerDriver != null)
            {
                SetBaseValue(type, playerDriver.physicsProperties.defaultGravityScale);
            }
            else
            {
                SetBaseValue(type, GetDefaultBaseValue(type));
            }
        }

        maxHealth = GetStatValue(StatType.Health);
        maxScarlet = GetStatValue(StatType.Scarlet);
        maxBlockPower = GetStatValue(StatType.BlockPower);
    }


    private float GetDefaultBaseValue(StatType type)
    {
        return type switch
        {
            // Core stats
            StatType.Health => 25f,
            StatType.Scarlet => 50f,
            StatType.BlockPower => 3f,
            StatType.GravityScale => 1f,

            // Movement stats
            StatType.MovementSpeed => 0,
            StatType.BoostSpeed => 0,
            StatType.JumpHeight => 0,
            StatType.DashDuration => 0f,

            // Combat stats
            StatType.AttackDamage => 0f,
            StatType.CriticalChance => 0.05f,
            StatType.CriticalDamage => 0.5f,
            StatType.EvasionChance => 0f,
            StatType.CastSpeed => 0f,
            StatType.AttackSpeed => 0f,

            // Defense stats
            StatType.Defense => 0f,
            StatType.OverHealth => 0f,

            // Magic stats
            StatType.SpellDamageBonus => 0f,
            StatType.SpellDurationBonus => 0f,
            StatType.LightAttackBonus => 0f,
            StatType.HeavyAttackBonus => 0f,
            StatType.FanDamageBonus => 0f,
            StatType.SymphumDamageBonus => 0f,

            // Summon stats
            StatType.SummonDamage => 0f,
            StatType.SummonDuration => 0f,
            StatType.SummonResistance => 0f,
            StatType.SummonHealth => 0f,

            // Resource stats
            StatType.SPBonus => 0f,
            StatType.SPRegen => 1f,
            StatType.SPFocus => 0f,
            StatType.GourdCharge => 0f,

            // Healing stats
            StatType.HealingBonus => 0f,
            StatType.HealingReduction => 0f,

            // Utility stats
            StatType.CooldownReduction => 0f,
            StatType.CollectionRange => 0f,
            StatType.HaliBonus => 0f,
            StatType.AttackRangeBonus => 0f,
            StatType.IncreasedDamageTaken => 0f,

            // Resistance stats
            StatType.GlimmerResist => 0f,
            StatType.StunResist => 0f,
            StatType.PoisonResist => 0f,
            StatType.FireResist => 0f,
            StatType.WeakenResist => 0f,
            StatType.SluggishResist => 0f,
            StatType.SlowedResist => 0f,
            StatType.ScarletDrainResist => 0f,
            StatType.CurseboundResist => 0f,
            StatType.BrokenBonesResist => 0f,
            StatType.ShatteredArmorResist => 0f,
            StatType.BleedingResist => 0f,
            StatType.SealedResist => 0f,
            StatType.GalelockResist => 0f,
            StatType.LesserStrikesResist => 0f,

            // Default for any unhandled stats
            _ => 0f
        };
    }


    // Get base value without modifiers
    public float GetBaseValue(StatType type)
    {
        var stat = GetStat(type);
        return stat?.Entry.BaseValue ?? 0f;
    }


    private void HandleStatChanged(StatType type)
    {
        switch (type)
        {
            case StatType.Health: maxHealth = GetStatValue(StatType.Health); break;
            case StatType.Scarlet: maxScarlet = GetStatValue(StatType.Scarlet); break;
            case StatType.BlockPower: maxBlockPower = GetStatValue(StatType.BlockPower); break;
        }
    }

    /// <summary>
    /// Applies a StatEntry as a modifier (+1 stack) to the stat library.
    /// </summary>

    public void ApplyStatEntry(StatEntry entry, StatusEffect effect = null)
    {
        // Find or create the stat entry
        var targetEntry = statEntries.FirstOrDefault(e => e.Type == entry.Type);
        if (targetEntry == null)
        {
            targetEntry = new StatEntry { Type = entry.Type };
            statEntries.Add(targetEntry);
        }

        // Apply the modifier directly
        var modifier = new StatModifier(StatModifierType.FlatBonus, entry.BaseValue, effect.uid);
        AddModifierToStat(entry.Type, modifier, CreateKey(effect));

        // Update max stat if relevant
        HandleStatChanged(entry.Type);
    }

    /// <summary>
    /// Removes a StatEntry modifier (-1 stack or full remove if unstacked).
    /// </summary>

    public void RemoveStatEntry(StatEntry entry, StatusEffect effect = null)
    {
        // Remove the modifier from the stat
        var modifier = new StatModifier(StatModifierType.FlatBonus, entry.BaseValue, effect.uid);
        RemoveModifierFromStat(entry.Type, CreateKey(effect));

        HandleStatChanged(entry.Type);
    }

    /// <summary>
    /// Removes all modifiers from a stat added by the specified sourceKey.
    /// </summary>
    public void RemoveModifierFromStat(StatType statType, string sourceKey)
    {
        Stat stat = GetStat(statType);
        if (stat != null)
        {
            stat.RemoveModifier(sourceKey);
        }
    }

    /// <summary>
    /// Adds a modifier to the specified stat, creating it if needed.
    /// </summary>
    public void AddModifierToStat(StatType statType, StatModifier modifier, string sourceKey)
    {
        Stat stat = GetStat(statType);
        if (stat == null)
        {
            // Create new stat if it doesn't exist
            StatEntry newEntry = new StatEntry
            {
                Type = statType,
                BaseValue = 0f
            };
            statEntries.Add(newEntry);
            stat = new Stat { Entry = newEntry };
            runtimeStats[statType] = stat;
            stat.OnValueChanged += () => HandleStatChanged(statType);
        }

        stat.AddModifier(modifier, sourceKey);
    }

    /// <summary>
    /// Helper to get final stat value by combining flat and percent bonus types. Returns 0 if neither stat exists.
    /// </summary>
    public float GetStatValue(StatType statType)
    {
        Stat stat = GetStat(statType);
        return stat != null ? stat.GetFinalValue() : 0f;
    }

    #region Scarlet

    /// <summary>
    /// Sets the current Scarlet (SP) value, clamping it between zero and the maximum Scarlet allowed by stats.
    /// </summary>
    /// <param name="value">The desired Scarlet value to set.</param>
    public void SetScarlet(float value)
    {
        float max = GetStatValue(StatType.Scarlet);
        float clamped = Mathf.Clamp(value, 0f, max);
        currentHealth = clamped;
    }

    /// <summary>
    /// Gain Scarlet points, with optional percent-based calculation and stat-based scaling.
    /// </summary>
    /// <param name="baseAmount">The base amount to gain. Interpreted as flat or percent depending on the flag.</param>
    /// <param name="isPercent">If true, baseAmount is treated as a percentage (0.1 = 10%) of max Scarlet.</param>
    public void GainScarlet(float baseAmount, bool isPercent = false)
    {
        float maxScarlet = GetStatValue(StatType.Scarlet);
        float spBonus = GetStatValue(StatType.SPBonus);    // e.g. 0.2 for +20%
        float spFocus = GetStatValue(StatType.SPFocus);    // e.g. 1.1 for +10% gain scaling

        // If percent-based, convert to flat based on max Scarlet
        float actualBaseAmount = isPercent ? maxScarlet * baseAmount : baseAmount;

        // Apply bonuses
        float gainAmount = actualBaseAmount * (1f + spBonus) * spFocus;

        currentScarlet += Mathf.RoundToInt(gainAmount);
        currentScarlet = Mathf.Clamp(currentScarlet, 0, Mathf.RoundToInt(maxScarlet));

        eventMgr.OnScarletGainTrigger();
    }

    /// <summary>
    /// Applies a percentage bonus to Scarlet (SP) regeneration for the duration. Supports stacking.
    /// </summary>
    public void ApplyScarletRegen(float percent, float duration, string buffname, StatusEffect status)
    {
        var mod = new StatModifier(StatModifierType.PercentBonus, percent, CreateKey(status));
        var modifiers = new List<StatusEffect.ModifierEntry>
{
    new StatusEffect.ModifierEntry { StatType = StatType.SPRegen, Modifier = mod }
};

        var effect = new StatusEffect(
            buffname,
            StatusEffectType.ScarletRegenBoost,
            duration,
            modifiers,
            StatusEffectStacking.Stackable,  // Allow stacking here
            3                               // Max 3 stacks, for example
        );

        AddStatusEffect(effect);
    }

    /// <summary>
    /// Drain Scarlet (SP). Considers SPFocus to reduce effective cost.
    /// </summary>
    /// <param name="baseAmount">Base drain amount before modifiers.</param>
    /// <returns>True if drain succeeded (enough Scarlet), false if insufficient Scarlet.</returns>
    public bool DrainScarlet(float baseAmount)
    {
        float spFocus = GetStatValue(StatType.SPFocus);
        float reduction = (baseAmount * spFocus);

        reduction = Mathf.Clamp(reduction, 0, Mathf.Infinity);

        // Drain cost reduced by SPFocus efficiency. Protect against zero or negative.
        float drainAmount = baseAmount - reduction;

        if (currentScarlet >= drainAmount)
        {
            currentScarlet -= drainAmount;
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Regenerate Scarlet (SP) per frame or tick. Uses deltaTime.
    /// </summary>
    /// <param name="deltaTime">Time passed since last regen call.</param>
    public void RegenerateScarlet(float deltaTime)
    {
        float spRegen = GetStatValue(StatType.SPRegen);    // base regen per second
        float spBonus = GetStatValue(StatType.SPBonus);

        // Regen formula: spRegen * (1 + SPBonus) * SPFocus * deltaTime
        float regenAmount = 1 + spRegen * spBonus * deltaTime;

        currentScarlet = Mathf.Clamp(currentScarlet, 0, GetStatValue(StatType.Scarlet));

        currentScarlet += regenAmount;
    }

    public void GainScarletOverTime(float totalAmount, float duration, float tickRate)
    {
        StartCoroutine(ScarletOverTimeCoroutine(totalAmount, duration, tickRate));
    }

    private IEnumerator ScarletOverTimeCoroutine(float totalAmount, float duration, float tickRate)
    {
        float elapsed = 0f;
        int ticks = Mathf.CeilToInt(duration / tickRate);
        float amountPerTick = totalAmount / ticks;

        while (elapsed < duration)
        {
            GainScarlet(amountPerTick);
            yield return new WaitForSeconds(tickRate);
            elapsed += tickRate;
        }
    }

    #endregion

    #region Damage/Healing/Overhealth
    /// <summary>
    /// Applies incoming damage to the character, optionally ignoring defense and scaling with a damage multiplier.
    /// Checks for reflection, applies defense mitigation, and factors in IncreasedDamageTaken.
    /// </summary>
    public void DealDamage(Attack attack)
    {
        if (HasReflectShield())
        {
            Debug.Log("Damage Reflected");
            return;
        }

        // Scale the base damage based on attack type(s)
        float scaledDamage = attack.BaseDamage;

        foreach (var type in attack.AttackTypes)
        {
            scaledDamage += GetAttackTypeBonus(type);
        }

        // Final damage includes multiplier + crit
        float finalDamage = scaledDamage * attack.DamageMultiplier;

        if (UnityEngine.Random.value < attack.CritChance)
        {
            attack.IsCriticalHit = true;
            finalDamage *= attack.CritMultiplier;
        }

        if (!attack.IgnoreDefense)
        {
            float defense = GetStatValue(StatType.Defense);
            finalDamage = Mathf.Max(finalDamage - defense, 1f); // Ensure minimum 1 damage
        }

        float extraTaken = GetStatValue(StatType.IncreasedDamageTaken);
        finalDamage += finalDamage * extraTaken;

        currentHealth = Mathf.Clamp(currentHealth - finalDamage, 0, GetStatValue(StatType.Health));

        // Event Triggers
        if (currEntity.Equals(attack.Sender))
        {
            eventMgr.OnSelfDamageTrigger();
        }

        if (currEntity.Equals(EntityType.Player) && attack.Sender == EntityType.Enemy || currEntity.Equals(EntityType.Enemy) && attack.Sender == EntityType.Player)
        {
            eventMgr.OnTakeDamageTrigger();
        }

        if (attack.IsCriticalHit)
        {
            switch (currEntity)
            {
                case EntityType.Player:
                    eventMgr.OnCriticalHitTrigger();
                    break;

                case EntityType.Summon:
                    eventMgr.OnSummonCritTrigger();
                    break;
            }
        }

        if (currEntity.Equals(EntityType.Summon) && attack.Sender == EntityType.Enemy)
        {
            eventMgr.OnSummonTakeDamageTrigger();
        }
    }

    private float GetAttackTypeBonus(AttackType type)
    {
        switch (type)
        {
            case AttackType.LightAttack:
                return GetStatValue(StatType.LightAttackBonus);
            case AttackType.HeavyAttack:
                return GetStatValue(StatType.HeavyAttackBonus);
            case AttackType.Spell:
                return GetStatValue(StatType.SpellDamageBonus);
            case AttackType.Summon:
                return GetStatValue(StatType.SummonDamage);
            case AttackType.Symphum:
                return GetStatValue(StatType.SymphumDamageBonus);
            case AttackType.Fan:
                return GetStatValue(StatType.FanDamageBonus);
            case AttackType.Enemy:
                return 0f; // enemy attacks don't scale on attack types
            default:
                return 0f;
        }
    }

    /// <summary>
    /// Heals the character unless healing is blocked. Modifies base heal based on HealingBonus and HealingReduction stats.
    /// </summary>
    public void Heal(float baseAmount)
    {
        if (!IsStatusActive(StatusEffectType.HealBlock))
            return;

        float bonus = GetStatValue(StatType.HealingBonus);
        float reduction = GetStatValue(StatType.HealingReduction);

        float modifiedHeal = baseAmount + (baseAmount * bonus) - (baseAmount * reduction);

        currentHealth += modifiedHeal;

        eventMgr.OnHealReceivedTrigger();
    }

    /// <summary>
    /// Coroutine that performs periodic healing over the effect's lifetime. Scales healing per tick based on number of stacks.
    /// </summary>
    public void ApplyHealingOverTime(float baseTotalHeal, float duration, float tickInterval = 1f, float stackMultiplier = 0.5f, string name = "")
    {
        if (!IsStatusActive(StatusEffectType.HealBlock) || duration <= 0f || tickInterval <= 0f)
            return;

        // Find existing HealingOverTime effect by name
        var existingHoT = activeStatusEffects.Find(e => e.EffectType == StatusEffectType.HealingOverTime && e.Name == name);

        if (existingHoT != null)
        {
            // Increase stack count up to max (optional max, say 5)
            existingHoT.StackCount = Mathf.Min(existingHoT.StackCount + 1, 5);

            // Refresh duration
            existingHoT.RemainingDuration = duration;
            existingHoT.TotalDuration = duration;

            // Update the coroutine healing amount by restarting it:
            StopCoroutine("DoHealingOverTime_" + existingHoT.GetHashCode());
            StartCoroutine(DoHealingOverTime(existingHoT, tickInterval, baseTotalHeal, stackMultiplier));
        }
        else
        {
            StatusEffect hotEffect = new StatusEffect(name, StatusEffectType.HealingOverTime, duration, new());
            hotEffect.TotalDuration = duration;
            hotEffect.StackCount = 1;

            activeStatusEffects.Add(hotEffect);

            StartCoroutine(DoHealingOverTime(hotEffect, tickInterval, baseTotalHeal, stackMultiplier));
        }
    }

    /// <summary>
    /// Coroutine that performs periodic healing over the effect's lifetime. Scales healing per tick based on number of stacks.
    /// </summary>
    private IEnumerator DoHealingOverTime(StatusEffect hotEffect, float tickInterval, float baseTotalHeal, float stackMultiplier)
    {
        float timePassed = 0f;
        float numTicks = Mathf.Ceil(hotEffect.TotalDuration / tickInterval);

        while (timePassed < hotEffect.TotalDuration)
        {
            // Calculate healing per tick with stacking:
            int stacks = hotEffect.StackCount;
            float healPerTickBase = baseTotalHeal / numTicks;
            float healThisTick = healPerTickBase * (1f + (stacks - 1) * stackMultiplier);

            if (!IsStatusActive(StatusEffectType.HealBlock))
            {
                Heal(healThisTick);
            }

            yield return new WaitForSeconds(tickInterval);
            timePassed += tickInterval;

            hotEffect.RemainingDuration = Mathf.Max(0f, hotEffect.RemainingDuration - tickInterval);
        }

        // Remove effect when finished
        activeStatusEffects.Remove(hotEffect);
    }

    /// <summary>
    /// Applies a buff that increases HealingBonus stat by a percentage for a limited duration.
    /// </summary>
    public void ApplyHealBonusBuff(float percent, float duration, string buffName, StatusEffect status)
    {
        var mod = new StatModifier(StatModifierType.PercentBonus, percent, CreateKey(status));
        var modifiers = new List<StatusEffect.ModifierEntry>
{
    new StatusEffect.ModifierEntry { StatType = StatType.HealingBonus, Modifier = mod }
};

        var effect = new StatusEffect(
            buffName,
            StatusEffectType.StatIncrease,
            duration,
            modifiers,
            StatusEffectStacking.Stackable,  // Stackable if you want
            3                                // Max stacks, tweak as needed
        );

        AddStatusEffect(effect);
    }

    /// <summary>
    /// Applies a temporary OverHealth stat boost. This increases max effective health for the duration.
    /// </summary>
    public void ApplyOverhealth(float bonus, float duration, string buffname, StatusEffect status)
    {
        var mod = new StatModifier(StatModifierType.FlatBonus, bonus, CreateKey(status));
        var modifiers = new List<StatusEffect.ModifierEntry>
{
    new StatusEffect.ModifierEntry { StatType = StatType.OverHealth, Modifier = mod }
};

        var effect = new StatusEffect(
            buffname,
            StatusEffectType.OverhealthBoost,
            duration,
            modifiers,
            StatusEffectStacking.None,  // No stacking by default, adjust if needed
            1
        );

        AddStatusEffect(effect);
        eventMgr.OnOverhealReceivedTrigger();
    }

    /// <summary>
    /// Returns whether the character currently has any OverHealth value above zero.
    /// </summary>
    public void AddOverHealth(float value)
    {
        float health = currentOverHealth;
        float max = GetStatValue(StatType.Health);
        float maxOver = Mathf.Max(0f, max - health);
        float clamped = Mathf.Clamp(value, 0f, maxOver);
        currentHealth = clamped;
        eventMgr.OnOverhealReceivedTrigger();
    }

    /// <summary>
    /// Returns whether the character currently has any OverHealth value above zero.
    /// </summary>
    public bool HasOverHealth()
    {
        return currentOverHealth > 0f;
    }
    #endregion

    #region AttackSpeed
    public float GetAttackCooldown(float baseAttackSpeed)
    {
        return 1f / baseAttackSpeed + GetStatValue(StatType.AttackSpeed); // Cooldown in seconds
    }
    #endregion

    /// <summary>
    /// Applies a flat bonus to a specific stat for the duration. Supports stacking effects.
    /// </summary>
    public void ApplyStatBuff(StatType stat, float value, float duration, string buffname, StatusEffect status)
    {
        var mod = new StatModifier(StatModifierType.FlatBonus, value, CreateKey(status));
        var modifiers = new List<StatusEffect.ModifierEntry>
{
    new StatusEffect.ModifierEntry { StatType = stat, Modifier = mod }
};

        var effect = new StatusEffect(
            buffname,
            StatusEffectType.StatIncrease,
            duration,
            modifiers,
            StatusEffectStacking.RefreshOnly,
            5  // example max stacks
        );

        AddStatusEffect(effect);
    }

    /// <summary>
    /// Applies a flat debuff (negative modifier) to a specific stat for the duration. Supports stacking.
    /// </summary>
    public void ApplyStatDebuff(StatType stat, float value, float duration, string buffname, StatusEffect status)
    {
        var mod = new StatModifier(StatModifierType.FlatDebuff, value, CreateKey(status));
        var modifiers = new List<StatusEffect.ModifierEntry>
{
    new StatusEffect.ModifierEntry { StatType = stat, Modifier = mod }
};

        var effect = new StatusEffect(
            buffname,
            StatusEffectType.StatDecrease,
            duration,
            modifiers,
            StatusEffectStacking.RefreshOnly,
            5
        );

        AddStatusEffect(effect);
    }

    /// <summary>
    /// Reduces duration of a specific debuff stat type by a given percent for the specified duration.
    /// </summary>
    public void ApplyGeneralResistance(float percentReduction, float duration, string buffname)
    {
        foreach (var stat in durationResistances.Keys)
            durationResistances[stat] *= (1f - percentReduction);

        var effect = new StatusEffect(
            buffname,
            StatusEffectType.GeneralResistanceBoost,
            duration,
            new List<StatusEffect.ModifierEntry>(),  // no modifiers here
            StatusEffectStacking.RefreshOnly,
            1
        );

        AddStatusEffect(effect);
    }

    /// <summary>
    /// Reduces duration of a specific debuff stat type by a given percent for the specified duration.
    /// </summary>
    public void ApplySpecificResistance(StatType affectedStat, float percentReduction, float duration, string buffname)
    {
        if (durationResistances.ContainsKey(affectedStat))
            durationResistances[affectedStat] *= (1f - percentReduction);

        var effect = new StatusEffect(
            buffname,
            StatusEffectType.SpecificResistanceBoost,
            duration,
            new List<StatusEffect.ModifierEntry>(),  // no modifiers here
            StatusEffectStacking.RefreshOnly,
            1
        );

        AddStatusEffect(effect);
    }

    /// <summary>
    /// Applies a temporary percentage-based MovementSpeed increase. Can stack up to 3 times.
    /// </summary>
    public void ApplyMovementBoost(float percentIncrease, float duration, string buffname)
    {
        var mod = new StatModifier(StatModifierType.PercentBonus, percentIncrease, System.Guid.NewGuid().ToString());
        var modifiers = new List<StatusEffect.ModifierEntry>
{
    new StatusEffect.ModifierEntry { StatType = StatType.MovementSpeed, Modifier = mod }
};

        var effect = new StatusEffect(
            buffname,
            StatusEffectType.MovementBoost,
            duration,
            modifiers,
            StatusEffectStacking.RefreshOnly,
            3
        );

        AddStatusEffect(effect);
    }

    /// <summary>
    /// Applies a temporary percentage-based GravityScale decrease (slow fall). Can stack up to 3 times.
    /// </summary>
    /// <param name="percentDecrease">Use positive value for reduction. For example, 0.5 means reduce gravity by 50%</param>
    public void ApplySlowFall(float percentDecrease, float duration, string buffname)
    {
        // Create modifier (percent debuff)
        var mod = new StatModifier(StatModifierType.PercentDebuff, percentDecrease, System.Guid.NewGuid().ToString());

        var modifiers = new List<StatusEffect.ModifierEntry>
    {
        new() { StatType = StatType.GravityScale, Modifier = mod }
    };

        var effect = new StatusEffect(
            buffname,
            StatusEffectType.SlowFall,
            duration,
            modifiers,
            StatusEffectStacking.RefreshOnly,
            3
        );

        AddStatusEffect(effect);
    }

    /// <summary>
    /// Applies a temporary percentage-based JumpHeight increase. Can stack up to 3 times.
    /// </summary>
    public void ApplyJumpBoost(float percentIncrease, float duration, string buffname)
    {
        var mod = new StatModifier(StatModifierType.PercentBonus, percentIncrease, System.Guid.NewGuid().ToString());
        var modifiers = new List<StatusEffect.ModifierEntry>
{
    new StatusEffect.ModifierEntry { StatType = StatType.JumpHeight, Modifier = mod }
};

        var effect = new StatusEffect(
            buffname,
            StatusEffectType.JumpBoost,
            duration,
            modifiers,
            StatusEffectStacking.RefreshOnly,
            3
        );

        AddStatusEffect(effect);
    }

    /// <summary>
    /// Applies a movement speed slow debuff for the specified duration. Can stack.
    /// </summary>
    public void ApplySlowed(float slowPercent, float duration, string buffname)
    {
        var mod = new StatModifier(StatModifierType.PercentDebuff, slowPercent, System.Guid.NewGuid().ToString());
        var modifiers = new List<StatusEffect.ModifierEntry>
{
    new StatusEffect.ModifierEntry { StatType = StatType.MovementSpeed, Modifier = mod }
};

        var effect = new StatusEffect(
            buffname,
            StatusEffectType.Slowed,          // Assuming you have a StatusEffectType.Slowed enum
            duration,
            modifiers,
            StatusEffectStacking.RefreshOnly,  // Usually slows can stack (or refresh), your choice
            3                                // Example max stacks
        );

        AddStatusEffect(effect);
    }

    /// <summary>
    /// Applies a stun effect that disables character actions for the duration. Refreshes if reapplied.
    /// </summary>
    public void ApplyStun(float duration, string debuffname)
    {

        var effect = new StatusEffect(
            debuffname,
            StatusEffectType.Stun,          // Assuming you have a StatusEffectType.Slowed enum
            duration,
            null,
            StatusEffectStacking.RefreshOnly // Example max stacks
        );

        AddStatusEffect(effect);
    }

    /// <summary>
    /// Checks if the character currently has the Spell Empowerment status effect active.
    /// </summary>
    public bool HasSpellEmpowerment() => IsStatusActive(StatusEffectType.SpellEmpowerment);

    /// <summary>
    /// Checks if the character currently has the Spell Duplication status effect active.
    /// </summary>
    public bool HasSpellDuplication() => IsStatusActive(StatusEffectType.SpellDuplication);

    /// <summary>
    /// Checks if the character currently has the Air Jump status effect active.
    /// </summary>
    public bool HasAirJump() => IsStatusActive(StatusEffectType.AirJump);

    /// <summary>
    /// Checks if the character currently has the Hover status effect active.
    /// </summary>
    public bool CanHover() => IsStatusActive(StatusEffectType.Hover);

    /// <summary>
    /// Checks if the character currently has the Empowered Attack status effect active.
    /// </summary>
    public bool HasEmpoweredAttack() => IsStatusActive(StatusEffectType.EmpoweredAttack);

    /// <summary>
    /// Checks if the character currently has the Auto Critical status effect active.
    /// </summary>
    public bool HasAutoCrit() => IsStatusActive(StatusEffectType.AutoCritical);

    /// <summary>
    /// Checks if the character currently has the Conditional Critical status effect active.
    /// </summary>
    public bool HasConditionalCrit() => IsStatusActive(StatusEffectType.ConditionalCritical);

    /// <summary>
    /// Checks if the character currently has the Card Trigger Boost status effect active.
    /// </summary>
    public bool HasCardTriggerBoost() => IsStatusActive(StatusEffectType.CardTriggerBoost);

    /// <summary>
    /// Checks if the character currently has the Card Refund status effect active.
    /// </summary>
    public bool HasCardRefund() => IsStatusActive(StatusEffectType.CardRefund);

    /// <summary>
    /// Checks if the character currently has the Card Amplification status effect active.
    /// </summary>
    public bool HasCardAmplification() => IsStatusActive(StatusEffectType.CardAmplification);

    /// <summary>
    /// Calculates the effective cooldown time after applying cooldown reduction and any active cooldown reduction boosts.
    /// </summary>
    /// <param name="baseCooldown">The base cooldown duration before reductions.</param>
    /// <returns>The cooldown duration after applying reductions.</returns>
    public float GetEffectiveCooldown(float baseCooldown)
    {
        float reduction = GetStatValue(StatType.CooldownReduction);
        if (IsStatusActive(StatusEffectType.CooldownReductionBoost))
            reduction += 0.2f; // or whatever % boost is granted
        return baseCooldown * (1f - reduction);
    }

    /// <summary>
    /// Checks if the character is dead (health is zero or below).
    /// </summary>
    public bool IsDead()
    {
        if (currentHealth <= 0)
        {
            switch (currEntity)
            {
                case EntityType.Player:
                    eventMgr.OnDeathTrigger();
                    break;

                case EntityType.Summon:
                    eventMgr.OnSummonDeathTrigger();
                    break;
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Revives the character with a percentage of max health, resets OverHealth and Scarlet, and clears negative status effects.
    /// </summary>
    /// <param name="percent">Percent of max health to restore on revive (default 50%).</param>
    public void Revive(float percent = 0.5f)
    {
        if (!IsDead())
            return;

        float max = GetStatValue(StatType.Health);
        float revivedHealth = Mathf.Clamp(max * percent, 1f, max); // Ensure minimum 1 HP

        currentBlockPower = GetStatValue(StatType.BlockPower);
        currentOverHealth = 0f;
        currentHealth = revivedHealth;
        currentScarlet = GetStatValue(StatType.Scarlet);
        Cleanse(true);

        // Remove negative status effects (optional: keep buffs like Glimmer)
        for (int i = activeStatusEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeStatusEffects[i];
        }

        eventMgr.OnReviveTrigger();
    }

    /// <summary>
    /// Checks if the character can revive upon death due to a status effect.
    /// </summary>
    public bool CanReviveOnDeath() => IsStatusActive(StatusEffectType.ReviveOnDeath);

    /// <summary>
    /// Gets the Stat object for a given stat type. Throws if the stat does not exist.
    /// </summary>
    /// <param name="type">The StatType to retrieve.</param>
    /// <returns>The Stat object.</returns>
    public Stat GetStat(StatType type)
    {
        runtimeStats.TryGetValue(type, out var stat);
        return stat;
    }

    /// <summary>
    /// Gets the current stack count of a specified status effect, or zero if not active.
    /// </summary>
    /// <param name="effectType">The status effect type.</param>
    /// <returns>The stack count.</returns>
    public int GetStackCount(StatusEffectType effectType)
    {
        var effect = activeStatusEffects.Find(e => e.EffectType == effectType);
        return effect != null ? effect.StackCount : 0;
    }

    /// <summary>
    /// Sets the resistance multiplier for duration-based effects on a given stat type.
    /// </summary>
    /// <param name="type">Stat type to apply resistance to.</param>
    /// <param name="resistanceMultiplier">Multiplier for resistance (e.g., 0.8 to reduce duration by 20%).</param>
    public void SetDurationResistance(StatType type, float resistanceMultiplier)
    {
        durationResistances[type] = resistanceMultiplier;
    }

    /// <summary>
    /// Applies a state effect, increasing its counter and setting the related flag.
    /// </summary>
    /// <param name="type">Type of state effect to apply.</param>
    public void ApplyStateEffect(StatusEffectType type)
    {
        if (!activeStateCounters.ContainsKey(type))
            activeStateCounters[type] = 0;

        activeStateCounters[type]++;
        SetFlag(type, true);
    }

    /// <summary>
    /// Removes a state effect, decrementing its counter and clearing the flag if no counters remain.
    /// </summary>
    /// <param name="type">Type of state effect to remove.</param>
    public void RemoveStateEffect(StatusEffectType type)
    {
        if (!activeStateCounters.ContainsKey(type))
            return;

        activeStateCounters[type] = Mathf.Max(0, activeStateCounters[type] - 1);

        if (activeStateCounters[type] == 0)
            SetFlag(type, false);
    }

    /// <summary>
    /// Sets the boolean flag corresponding to a status effect type (used for quick checks).
    /// </summary>
    /// <param name="type">The status effect type.</param>
    /// <param name="value">Flag value to set.</param>
    private void SetFlag(StatusEffectType type, bool value)
    {
        switch (type)
        {
            case StatusEffectType.Stun: IsStunned = value; break;
            case StatusEffectType.Poison: IsPoisoned = value; break;
            case StatusEffectType.Burn: IsBurning = value; break;
            case StatusEffectType.Petrified: IsPetrified = value; break;
            case StatusEffectType.Bleeding: IsBleeding = value; break;
            case StatusEffectType.ScarletDrain: IsScarletDrained = value; break;
            case StatusEffectType.Slowed: IsSlowed = value; break;
            case StatusEffectType.Weakened: IsWeakened = value; break;
            case StatusEffectType.Sluggish: IsSluggish = value; break;
            case StatusEffectType.BrokenBones: HasBrokenBones = value; break;
            case StatusEffectType.ShatteredArmor: IsShatteredArmor = value; break;
            case StatusEffectType.Cursebound: IsCursebound = value; break;
            case StatusEffectType.Sealed: IsSealed = value; break;
            case StatusEffectType.Glimmer: HasGlimmer = value; break;
            case StatusEffectType.Galelock: IsGalelocked = value; break;
            case StatusEffectType.LesserStrikes: HasLesserStrikes = value; break;
        }
    }

    /// <summary>
    /// Checks whether a status effect flag is currently active (counter > 0).
    /// </summary>
    /// <param name="type">The status effect type to check.</param>
    /// <returns>True if active, false otherwise.</returns>
    public bool GetFlag(StatusEffectType type)
    {
        return activeStateCounters.ContainsKey(type) && activeStateCounters[type] > 0;
    }

    public StatusEffect GetStatusEffect(StatusEffectType effectType)
    {
        return activeStatusEffects.Find(e => e.EffectType == effectType);
    }

    /// <summary>
    /// Checks whether a status effect of the specified type is currently active.
    /// </summary>
    /// <param name="effectType">Type of status effect to check.</param>
    /// <returns>True if active, false if not.</returns>
    public bool IsStatusActive(StatusEffectType effectType)
    {
        foreach (var effect in activeStatusEffects)
        {
            if (effect.EffectType == effectType)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Adds a new status effect or merges it with an existing one based on stacking rules.
    /// </summary>
    public void AddStatusEffect(StatusEffect newEffect)
    {
        // Initialize stack values if not done
        if (newEffect.StackValues.Count == 0)
        {
            newEffect.InitializeStackValues();
        }

        var existing = activeStatusEffects.Find(e => e.EffectType == newEffect.EffectType);
        string sourceUID = newEffect.uid;

        if (existing != null)
        {
            switch (existing.StackingBehavior)
            {
                case StatusEffectStacking.None:
                    return;

                case StatusEffectStacking.RefreshOnly:
                    existing.RemainingDuration = newEffect.TotalDuration;
                    return;

                case StatusEffectStacking.Stackable:
                    int oldStacks = existing.StackCount;
                    int newStacks = Mathf.Min(existing.StackCount + 1, existing.MaxStacks);

                    // Only process if we're actually adding stacks
                    if (newStacks > oldStacks)
                    {
                        // Update each modifier with accumulated value
                        foreach (var mod in existing.Modifiers)
                        {
                            Stat stat = GetStat(mod.StatType);
                            if (stat != null)
                            {
                                // Remove old modifier
                                stat.RemoveModifier(sourceUID);

                                // Calculate new value: stack value * stack count
                                float stackValue = existing.StackValues[mod.StatType];
                                float newValue = stackValue * newStacks;

                                // Create updated modifier
                                StatModifier updatedMod = new StatModifier(
                                    mod.Modifier.Type,
                                    newValue,
                                    sourceUID
                                );

                                // Add back with accumulated value
                                stat.AddModifier(updatedMod, sourceUID);
                            }
                        }

                        existing.StackCount = newStacks;
                    }

                    // Always refresh duration
                    existing.RemainingDuration = newEffect.TotalDuration;
                    return;
            }
        }
        else
        {
            // Apply modifiers with stack value calculation
            foreach (var mod in newEffect.Modifiers)
            {
                float stackValue = newEffect.StackValues[mod.StatType];
                float effectiveValue = stackValue * newEffect.StackCount;

                StatModifier modWithUID = new StatModifier(
                    mod.Modifier.Type,
                    effectiveValue,
                    sourceUID
                );

                AddModifierToStat(mod.StatType, modWithUID, sourceUID);
            }

            activeStatusEffects.Add(newEffect);
            SetFlag(newEffect.EffectType, true);
        }

        eventMgr.OnStatusAppliedTrigger(newEffect.EffectType);
    }

    /// <summary>
    /// Returns the list of stat modifiers and their types that correspond to a given status effect type.
    /// </summary>
    /// <param name="effectType">Status effect type.</param>
    /// <returns>List of (StatType, StatModifierType) tuples indicating which stats are affected.</returns>
    private List<(StatType stat, StatModifierType modType)> GetStatEffectsForStatus(StatusEffectType effectType)
    {
        List<(StatType, StatModifierType)> result = new();

        switch (effectType)
        {
            case StatusEffectType.Weakened:
                result.Add((StatType.AttackDamage, StatModifierType.PercentDebuff));
                result.Add((StatType.IncreasedDamageTaken, StatModifierType.PercentBonus));
                result.Add((StatType.CriticalDamage, StatModifierType.PercentDebuff));
                break;

            case StatusEffectType.Sluggish:
                result.Add((StatType.JumpHeight, StatModifierType.PercentDebuff));
                result.Add((StatType.AttackSpeed, StatModifierType.PercentDebuff));
                result.Add((StatType.CastSpeed, StatModifierType.PercentDebuff));
                break;

            case StatusEffectType.Slowed:
                result.Add((StatType.MovementSpeed, StatModifierType.PercentDebuff));
                break;

            case StatusEffectType.ScarletDrain:
                result.Add((StatType.SPRegen, StatModifierType.PercentDebuff));
                break;

            case StatusEffectType.BrokenBones:
                result.Add((StatType.JumpHeight, StatModifierType.PercentDebuff));
                result.Add((StatType.AttackSpeed, StatModifierType.PercentDebuff));
                result.Add((StatType.CastSpeed, StatModifierType.PercentDebuff));
                result.Add((StatType.MovementSpeed, StatModifierType.PercentDebuff));
                break;

            case StatusEffectType.ShatteredArmor:
                result.Add((StatType.Defense, StatModifierType.PercentDebuff));
                break;

            case StatusEffectType.Bleeding:
                result.Add((StatType.Health, StatModifierType.PercentDebuff));
                result.Add((StatType.HealingBonus, StatModifierType.PercentDebuff));
                break;

            case StatusEffectType.Galelock:
                result.Add((StatType.SpellDamageBonus, StatModifierType.PercentDebuff));
                result.Add((StatType.SummonDamage, StatModifierType.PercentDebuff));
                break;

            case StatusEffectType.LesserStrikes:
                result.Add((StatType.SymphumDamageBonus, StatModifierType.PercentDebuff));
                break;

            case StatusEffectType.Petrified:
                result.Add((StatType.MovementSpeed, StatModifierType.PercentDebuff));
                break;

            case StatusEffectType.HealingOverTime:
                // No stat modifier; healing is applied manually in a coroutine
                break;

            case StatusEffectType.OverhealthBoost:
                result.Add((StatType.OverHealth, StatModifierType.FlatBonus));
                break;

            case StatusEffectType.ScarletRegenBoost:
                result.Add((StatType.SPRegen, StatModifierType.PercentBonus));
                break;

            case StatusEffectType.StatIncrease:
                // Applied dynamically through ApplyStatBuff
                break;

            case StatusEffectType.StatDecrease:
                // Applied dynamically through ApplyStatDebuff
                break;

            case StatusEffectType.MovementBoost:
                result.Add((StatType.MovementSpeed, StatModifierType.PercentBonus));
                break;

            case StatusEffectType.JumpBoost:
                result.Add((StatType.JumpHeight, StatModifierType.PercentBonus));
                break;

            case StatusEffectType.GeneralResistanceBoost:
            case StatusEffectType.SpecificResistanceBoost:
                // These are handled through duration resistance, not stat modifiers
                break;
        }

        return result;
    }

    /// <summary>
    /// Removes debuffs or all status effects from the character depending on the parameter.
    /// Also removes associated stat modifiers and status flags.
    /// </summary>
    /// <param name="removeAll">If true, removes all status effects, otherwise only debuffs.</param>
    /// <summary>
    /// Removes debuffs or all status effects from the character.
    /// </summary>
    public void Cleanse(bool removeAll = false)
    {
        List<StatusEffect> effectsToRemove = new();

        foreach (var effect in activeStatusEffects)
        {
            bool isDebuff = IsDebuff(effect.EffectType);

            if (removeAll || isDebuff)
            {
                effectsToRemove.Add(effect);
                RemoveStatusFlags(effect.EffectType);
            }
        }

        foreach (var effect in effectsToRemove)
        {
            string sourceKey = CreateKey(effect);
            // Remove modifiers
            foreach (var mod in effect.Modifiers)
            {
                Stat stat = GetStat(mod.StatType);
                if (stat != null)
                {
                    stat.RemoveModifier(sourceKey);
                }
            }
            activeStatusEffects.Remove(effect);
        }

        Debug.Log($"Cleanse complete. Removed {effectsToRemove.Count} status effect(s).");
    }

    /// <summary>
    /// Determines if a given StatusEffectType is considered a debuff.
    /// </summary>
    /// <param name="type">The status effect type to check.</param>
    /// <returns>True if the effect is a debuff, false otherwise.</returns>
    private bool IsDebuff(StatusEffectType type)
    {
        switch (type)
        {
            case StatusEffectType.StatDecrease:
            case StatusEffectType.Poison:
            case StatusEffectType.Burn:
            case StatusEffectType.Stun:
            case StatusEffectType.Slowed:
            case StatusEffectType.Weakened:
            case StatusEffectType.Sluggish:
            case StatusEffectType.ScarletDrain:
            case StatusEffectType.Cursebound:
            case StatusEffectType.BrokenBones:
            case StatusEffectType.ShatteredArmor:
            case StatusEffectType.Bleeding:
            case StatusEffectType.Sealed:
            case StatusEffectType.Galelock:
            case StatusEffectType.LesserStrikes:
            case StatusEffectType.Glimmer:
            case StatusEffectType.HealBlock:
            case StatusEffectType.IncreasedDamageTaken:
                return true;

            // You can add more here if needed
            default:
                return false;
        }
    }

    /// <summary>
    /// Clears the boolean flags related to the given status effect type.
    /// </summary>
    /// <param name="effectType">The status effect type whose flags to remove.</param>
    private void RemoveStatusFlags(StatusEffectType effectType)
    {
        switch (effectType)
        {
            case StatusEffectType.Stun: IsStunned = false; break;
            case StatusEffectType.Poison: IsPoisoned = false; break;
            case StatusEffectType.Burn: IsBurning = false; break;
            case StatusEffectType.Petrified: IsPetrified = false; break;
            case StatusEffectType.Bleeding: IsBleeding = false; break;
            case StatusEffectType.ScarletDrain: IsScarletDrained = false; break;
            case StatusEffectType.Slowed: IsSlowed = false; break;
            case StatusEffectType.Weakened: IsWeakened = false; break;
            case StatusEffectType.Sluggish: IsSluggish = false; break;
            case StatusEffectType.BrokenBones: HasBrokenBones = false; break;
            case StatusEffectType.ShatteredArmor: IsShatteredArmor = false; break;
            case StatusEffectType.Cursebound: IsCursebound = false; break;
            case StatusEffectType.Sealed: IsSealed = false; break;
            case StatusEffectType.Glimmer: HasGlimmer = false; break;
            case StatusEffectType.Galelock: IsGalelocked = false; break;
            case StatusEffectType.LesserStrikes: HasLesserStrikes = false; break;
        }
    }

    /// <summary>
    /// Checks whether the character has immunity against the specified status effect type.
    /// </summary>
    /// <param name="effectType">The status effect type to check immunity for.</param>
    /// <returns>True if immune, false otherwise.</returns>
    public bool HasImmunity(StatusEffectType effectType)
    {
        if (IsStatusActive(StatusEffectType.AllImmunity))
            return true;

        return (effectType == StatusEffectType.Burn && IsStatusActive(StatusEffectType.BurnImmunity))
            || (effectType == StatusEffectType.Stun && IsStatusActive(StatusEffectType.StunImmunity))
            || (effectType == StatusEffectType.Poison && IsStatusActive(StatusEffectType.PoisonImmunity))
            || (effectType == StatusEffectType.Petrified && IsStatusActive(StatusEffectType.PetrifyImmunity))
            || (effectType == StatusEffectType.Weakened && IsStatusActive(StatusEffectType.WeakenedImmunity))
            || (effectType == StatusEffectType.Sluggish && IsStatusActive(StatusEffectType.SluggishImmunity))
            || (effectType == StatusEffectType.Slowed && IsStatusActive(StatusEffectType.SlowedImmunity))
            || (effectType == StatusEffectType.ScarletDrain && IsStatusActive(StatusEffectType.ScarletDrainImmunity))
            || (effectType == StatusEffectType.Cursebound && IsStatusActive(StatusEffectType.CurseboundImmunity))
            || (effectType == StatusEffectType.BrokenBones && IsStatusActive(StatusEffectType.BrokenBonesImmunity))
            || (effectType == StatusEffectType.ShatteredArmor && IsStatusActive(StatusEffectType.ShatteredArmorImmunity))
            || (effectType == StatusEffectType.Bleeding && IsStatusActive(StatusEffectType.BleedingImmunity))
            || (effectType == StatusEffectType.Sealed && IsStatusActive(StatusEffectType.SealedImmunity))
            || (effectType == StatusEffectType.Galelock && IsStatusActive(StatusEffectType.GalelockImmunity))
            || (effectType == StatusEffectType.LesserStrikes && IsStatusActive(StatusEffectType.LesserStrikesImmunity))
            || (effectType == StatusEffectType.HealBlock && IsStatusActive(StatusEffectType.HealBlockImmunity));
    }

    /// <summary>
    /// Returns whether the character currently has a Reflect Shield active.
    /// </summary>
    public bool HasReflectShield() => IsStatusActive(StatusEffectType.ReflectShield);

    /// <summary>
    /// Returns whether the character currently has a Wind Shield active.
    /// </summary>
    public bool HasWindShield() => IsStatusActive(StatusEffectType.WindShield);

    /// <summary>
    /// Returns whether the character currently has a Decoy active.
    /// </summary>
    public bool HasDecoy() => IsStatusActive(StatusEffectType.Decoy);

    /// <summary>
    /// Calculates the movement speed multiplier while petrified, interpolating from normal speed to zero over the effect duration.
    /// </summary>
    /// <returns>A float multiplier between 1 (normal speed) and 0 (fully petrified).</returns>
    public float GetPetrifiedMovementSpeedMultiplier()
    {
        if (!GetFlag(StatusEffectType.Petrified))
            return 1f;

        var petrifyEffect = activeStatusEffects.Find(e => e.EffectType == StatusEffectType.Petrified);
        if (petrifyEffect == null || petrifyEffect.RemainingDuration <= 0f || petrifyEffect.TotalDuration <= 0f)
            return 1f;

        float remaining = petrifyEffect.RemainingDuration;
        float total = petrifyEffect.TotalDuration;
        float progress = 1f - (remaining / total);

        return Mathf.Lerp(1f, 0f, progress);
    }

    /// <summary>
    /// Checks if the character is effectively dead due to petrification (petrify effect expired but still flagged).
    /// </summary>
    /// <returns>True if petrified and dead, false otherwise.</returns>
    public bool IsPetrifiedDead()
    {
        if (GetFlag(StatusEffectType.Petrified))
        {
            var petrifyEffect = activeStatusEffects.Find(e => e.EffectType == StatusEffectType.Petrified);
            if (petrifyEffect != null && petrifyEffect.RemainingDuration <= 0f)
                eventMgr.OnPetrifyExpiredTrigger();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Attempts a dodge chance based on Glimmer status effect, with a 50% chance to succeed.
    /// </summary>
    /// <returns>True if dodge succeeds, false otherwise.</returns>
    public bool TryGlimmerDodge()
    {
        if (!GetFlag(StatusEffectType.Glimmer))
            return false;

        return UnityEngine.Random.value < 0.5f;
    }

    /// <summary>
    /// Returns the critical hit multiplier modifier provided by the Glimmer status effect.
    /// </summary>
    /// <returns>0.5 if Glimmer is active, otherwise 1.</returns>
    public float GetGlimmerCritMultiplier()
    {
        if (GetFlag(StatusEffectType.Glimmer))
            return 0.5f;
        else
            return 1f;
    }

    /// <summary>
    /// Applies poison damage tick if the character is poisoned, ignoring defense.
    /// </summary>
    /// <param name="baseTickDamage">Flat damage per tick.</param>
    /// <param name="maxHpPercentDamage">Percent max health damage per tick.</param>
    public void ApplyPoisonTick(float baseTickDamage, float maxHpPercentDamage)
    {
        if (!GetFlag(StatusEffectType.Poison))
            return;

        float damage = GetPoisonDamagePerTick(baseTickDamage, maxHpPercentDamage);

        Attack poisonAttack = new Attack
        {
            BaseDamage = damage,
            DamageMultiplier = 1f,
            CritChance = 0f,
            CritMultiplier = 1f,
            IgnoreDefense = true,
            Sender = EntityType.Enemy, // or whoever applied the poison
            AttackTypes = new List<AttackType> { AttackType.Spell }
        };

        DealDamage(poisonAttack);
        eventMgr.OnStatusAppliedTrigger(StatusEffectType.Poison);
    }

    /// <summary>
    /// Calculates the total poison damage per tick combining flat and percent max health damage.
    /// </summary>
    /// <param name="baseTickDamage">Flat damage amount.</param>
    /// <param name="maxHpPercentDamage">Percentage of max health to apply as damage.</param>
    /// <returns>The total poison damage per tick.</returns>
    public float GetPoisonDamagePerTick(float baseTickDamage, float maxHpPercentDamage)
    {
        float maxHealth = GetStatValue(StatType.Health);
        float flatDamage = baseTickDamage;
        float percentDamage = maxHealth * maxHpPercentDamage;
        return flatDamage + percentDamage;
    }

    /// <summary>
    /// Applies burn damage tick if the character is burning, ignoring defense.
    /// </summary>
    /// <param name="burnDamage">Amount of burn damage to apply.</param>
    public void ApplyBurnTick(float burnDamage)
    {
        if (!GetFlag(StatusEffectType.Burn))
            return;

        Attack burnAttack = new Attack
        {
            BaseDamage = burnDamage,
            DamageMultiplier = 1f,
            CritChance = 0f,
            CritMultiplier = 1f,
            IgnoreDefense = true,
            Sender = EntityType.Enemy, // or whoever applied the burn
            AttackTypes = new List<AttackType> { AttackType.Spell }
        };

        DealDamage(burnAttack);
        eventMgr.OnStatusAppliedTrigger(StatusEffectType.Burn);
    }

    /// <summary>
    /// Refreshes the duration of the Burn status effect if it is active.
    /// </summary>
    /// <param name="duration">New duration to set for the burn effect.</param>
    public void RefreshBurnDuration(float duration)
    {
        if (GetFlag(StatusEffectType.Burn))
        {
            var burnEffect = activeStatusEffects.Find(e => e.EffectType == StatusEffectType.Burn);
            if (burnEffect != null)
            {
                burnEffect.RemainingDuration = duration;
                int idx = activeStatusEffects.IndexOf(burnEffect);
                if (idx >= 0)
                    activeStatusEffects[idx] = burnEffect;
            }
        }
    }

}