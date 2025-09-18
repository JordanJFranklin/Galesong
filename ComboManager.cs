using UnityEngine;
using System;
using System.Collections.Generic;
using InputKeys;
using System.Collections;
using UnityEditor;
using Ink;
using Unity.Burst.Intrinsics;
using System.Linq;
using System.ComponentModel.Design;

public enum Stance { FanStance, SymphumStance }
public enum CastType { Instant, Hold, Channel }

public enum ComboConditionState
{
    Grounded,
    Aerial,
    Wall,
    Special,
    None
}

public enum SpellChannelMode
{
    TargetReticle,      // Cast at reticle position (mouse or crosshair)
    TargetLockedEnemy,  // Cast on currently locked-on enemy
    TargetGroundPoint,  // Cast at ground point under reticle
    SpawnAsProjectile,    // Create in front of player (e.g. Whirling Wall)
    TargetAroundEnemy,  // Surround enemy (e.g. Imprisoning Winds)
    TargetObject,       // For object levitation, etc.
    None
}


public enum ProjectileSpawnType
{
    Straight,
    Fan,
    Burst
}

public enum SpellCastState
{
    Idle,
    Activation, // Placing aiming
    Channel,    // Charging
    Cast,       // Spell executes
    Cancelled
}



[System.Serializable]
public class Combo
{
    public string ComboName;
    public float baseAttackSpeed;
    public Stance stance;
    public ComboConditionState requiredState = ComboConditionState.Grounded;
    public List<ComboStep> attackStrings;
    public List<SpellCastStep> attackSpells;
    public bool IsUnlocked = true;
}


[System.Serializable]
public class ComboBranch
{
    public List<KeyActions> validInputs; // Support multiple inputs
    public ComboStep nextStep;
}

[Serializable]
public class ComboStep
{
    public string animationEventName;
    public List<Stance> allowedStances;
    public List<KeyActions> validInputs;

    public int currComboStep;
    public Attack attack;
    public GameObject projectilePrefab;
    public Transform projectileSpawnPoint;

    [Header("Motion Settings")]
    [HideInInspector] public float attackDurationTimer;
    public float AttackDuration;
    public bool useAttackMotion = false;
    public Vector3 attackMotionDirection = Vector3.forward; // Local direction (forward, left, etc.)
    public float attackMotionDistance = 3f;
    public AnimationCurve attackMotionCurve = AnimationCurve.Linear(0, 0, 1, 1);

    public CastType castType = CastType.Instant;
    public float requiredHoldTime = 0f;

    public ProjectileSpawnType spawnType = ProjectileSpawnType.Straight;
    public bool disableGravityOnCast = false;
    public float gravityDisableDuration = 0.5f;

    public float projectileSpeed = 20f;

    // Spread (Fan) settings
    public int numberOfProjectiles = 1;
    public float spreadAngle = 30f;

    // Burst settings
    public int burstCount = 1;
    public float burstRate = 0.1f;

    public List<ComboBranch> branches = new();

    [HideInInspector] public bool isInitialAttack = false;
    [HideInInspector] public bool isFinisherAttack = false;

    public Action OnStepStart;
    public Action OnStepEnd;
}

[System.Serializable]
public class SpellCastStep
{
    [Header("Essentials")]
    public bool forceLockOnTargetPosition; // If true, override and spawn at locked target when locked on
    public bool hasUnlimitedInstances = true;
    public string spellName;
    public CastType castType;
    public SpellChannelMode channelMode = SpellChannelMode.None;
    public List<KeyActions> validInputs;
    public GameObject spellPrefab;
    public Transform spellSpawnPoint;

    [Header("Motion Settings")]
    [HideInInspector] public float attackDurationTimer;
    public float AttackDuration;
    public bool disableGravityOnCast = false;
    public bool useAttackMotion = false;
    public Vector3 attackMotionDirection = Vector3.forward; // Local direction (forward, left, etc.)
    public float attackMotionDistance = 3f;
    public AnimationCurve attackMotionCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Spell")]
    public float minHoldTime;
    public float maxHoldTime;
    public float minMultiplier = 1f;
    public float maxMultiplier = 2.5f;
    public float baseChargeTime;
    public float baseScarletCost = 0f;
    public float baseCastRange = 5f;
    public float baseDuration = 3f;
    [HideInInspector]public float spellDuration;
    [HideInInspector]public GameObject spellSelectedObject;
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(ComboStep), true)]
public class ComboStepDrawer : PropertyDrawer
{
    private const float Padding = 2f;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        float totalHeight = EditorGUIUtility.singleLineHeight + Padding; // foldout
        SerializedProperty iterator = property.Copy();
        SerializedProperty endProperty = property.GetEndProperty();

        iterator.NextVisible(true); // enter object

        while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, endProperty))
        {
            string name = iterator.name;

            // Always show projectileSpeed
            if (name == "projectileSpeed")
            {
                totalHeight += EditorGUI.GetPropertyHeight(iterator, true) + Padding;
                continue;
            }

            // Skip conditional fields
            if (name == "spreadAngle" || name == "numberOfProjectiles" || name == "burstCount" || name == "burstRate")
                continue;

            totalHeight += EditorGUI.GetPropertyHeight(iterator, true) + Padding;
        }

        SerializedProperty spawnTypeProp = property.FindPropertyRelative("spawnType");
        ProjectileSpawnType spawnType = (ProjectileSpawnType)spawnTypeProp.enumValueIndex;

        if (spawnType == ProjectileSpawnType.Fan)
        {
            totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("numberOfProjectiles")) + Padding;
            totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("spreadAngle")) + Padding;
        }
        else if (spawnType == ProjectileSpawnType.Burst)
        {
            totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("burstCount")) + Padding;
            totalHeight += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("burstRate")) + Padding;
        }

        return totalHeight;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        property.isExpanded = EditorGUI.Foldout(
            new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight),
            property.isExpanded, label, true);

        if (!property.isExpanded)
            return;

        EditorGUI.indentLevel++;
        Rect fieldRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + Padding, position.width, EditorGUIUtility.singleLineHeight);

        SerializedProperty iterator = property.Copy();
        SerializedProperty endProperty = property.GetEndProperty();

        iterator.NextVisible(true); // enter object

        while (iterator.NextVisible(false) && !SerializedProperty.EqualContents(iterator, endProperty))
        {
            string name = iterator.name;

            // Always show projectileSpeed first
            if (name == "projectileSpeed")
            {
                EditorGUI.PropertyField(fieldRect, iterator, true);
                fieldRect.y += EditorGUI.GetPropertyHeight(iterator, true) + Padding;
                continue;
            }

            // Skip conditional fields
            if (name == "spreadAngle" || name == "numberOfProjectiles" || name == "burstCount" || name == "burstRate")
                continue;

            EditorGUI.PropertyField(fieldRect, iterator, true);
            fieldRect.y += EditorGUI.GetPropertyHeight(iterator, true) + Padding;
        }

        SerializedProperty spawnTypeProp = property.FindPropertyRelative("spawnType");
        ProjectileSpawnType spawnType = (ProjectileSpawnType)spawnTypeProp.enumValueIndex;

        if (spawnType == ProjectileSpawnType.Fan)
        {
            DrawField(property, "numberOfProjectiles", ref fieldRect);
            DrawField(property, "spreadAngle", ref fieldRect);
        }
        else if (spawnType == ProjectileSpawnType.Burst)
        {
            DrawField(property, "burstCount", ref fieldRect);
            DrawField(property, "burstRate", ref fieldRect);
        }

        EditorGUI.indentLevel--;
    }

    private void DrawField(SerializedProperty root, string name, ref Rect rect)
    {
        SerializedProperty prop = root.FindPropertyRelative(name);
        EditorGUI.PropertyField(rect, prop);
        rect.y += EditorGUI.GetPropertyHeight(prop) + Padding;
    }
}

#endif
public class ComboManager : MonoBehaviour
{
    [Header("Combo Counter")]
    public int comboCount;
    public float tempoDuration = 3f;
    public float tempoDecayRate = 1f;
    public float currentTempo;
    public int maxTempoTier => GetTier(comboCount);
    public float totalBonusDropChance => comboCount * 0.25f;

    [Header("Combo Settings")]
    public bool isPlaying;
    public bool enableDebugLogs = false;
    public Stance currentStance = Stance.FanStance;
    public int currComboStep = 0;
    public float comboResetTimer = 1.5f;
    public float comboTimer = 0f;
    public int currSpellStep = 0;

    [Header("Spell Settings")]
    public LayerMask spellReticleFloorLayer;
    public bool isChargingSpell;
    public bool isCastingSpell;

    [Header("Spells/Attack Strings")]
    public List<Combo> AllAttacks;

    private enum ComboState { Idle, InputBuffered, Attacking, Canceling }
    private ComboState comboState = ComboState.Idle;

    private bool bufferedInput = false;
    private string bufferedEventName;
    private SpellCastState spellCastState = SpellCastState.Idle;
    private SpellCastStep currentSpell;
    private GameObject placementIndicator;
    private Dictionary<KeyActions, float> heldInputTimers = new();
    private Dictionary<KeyActions, bool> stepInputBuffered = new Dictionary<KeyActions, bool>();
    private Dictionary<SpellCastStep, SpellCastState> spellState = new Dictionary<SpellCastStep, SpellCastState>();
    private Dictionary<string, GameObject> activePersistentSpells = new Dictionary<string, GameObject>();


    private KeyActions? currentHeldInput = null;
    private ComboStep currentHeldStep = null;
    private float nextAttackAllowedTime = 0f;
    private EntityEventManager eventMgr;
    private CharacterStats stats;


    void Start()
    {
        eventMgr = GetComponent<EntityEventManager>();
        stats = GetComponent<CharacterStats>();
        
        TagInitialAndFinisherAttacks();
    }

    void Update()
    {
        if(PrototypeUI.Instance.isPaused) { return; }
        
        UpdateComboTimers();

        var scheme = InputManager.Instance.PlayerInputScheme;

        // 1) Check for heavy attack cancel of spells
        if (IsAnySpellChargingOrCasting())
        {
            if (scheme.IsHeld(KeyActions.HeavyAttack))
            {
                if (enableDebugLogs) Debug.Log("[Spell] Heavy attack pressed: cancelling all spells.");
                CancelAllSpells();
            }
        }

        // 2) Always handle spell input if no combo step currently playing
        foreach (var combo in AllAttacks)
        {
            foreach (var spell in combo.attackSpells)
            {
                HandleSpellInputForSpell(combo, spell, scheme);
            }
        }

        // 3) Only process combos if no spell is charging/casting
        if (!IsAnySpellChargingOrCasting())
        {
            if (comboState == ComboState.Idle || comboState == ComboState.InputBuffered)
            {
                TryInstantComboStep();
            }
        }
    }

    private void UpdateComboTimers()
    {
        if (comboTimer > 0f)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f) ResetCombo();
        }

        if (comboCount > 0)
        {
            currentTempo -= tempoDecayRate * Time.deltaTime;
            if (currentTempo <= 0)
            {
                comboCount = 0;
                currentTempo = 0;
                eventMgr.OnComboExpiredTrigger();
            }
        }
    }
    private void StartSpellCasting(SpellCastStep spell)
    {
        CancelCombo();
        isChargingSpell = true;
        currentSpell = spell;
        spellCastState = SpellCastState.Channel;
        // Show charging animation, indicator, etc.
    }

    private void FinishSpellCasting(float power)
    {
        CastSpell(currentSpell, power);
        isChargingSpell = false;
        spellCastState = SpellCastState.Idle;
        currentSpell = null;
    }

    private void UpdateSpellCasting()
    {
        if (spellCastState == SpellCastState.Channel || spellCastState == SpellCastState.Cast)
        {
            Vector3 castPos = GetSpellCastPosition(currentSpell);

            if (IsInCastRange(currentSpell, castPos))
            {
                // Show animations, prepare spell, etc.
            }
            else
            {
                // Handle invalid cast position
            }
        }
    }


    private void HandleSpellInputForSpell(Combo combo, SpellCastStep spell, InputScheme scheme)
    {
        ComboConditionState currentState = GetCurrentComboConditionState();

        // Use the passed-in combo instead of GetActiveCombo()
        if (combo.requiredState != ComboConditionState.None && combo.requiredState != currentState)
        {
            if (enableDebugLogs)
                Debug.Log($"[Spell] Cannot cast '{spell.spellName}' - required state {combo.requiredState}, current state {currentState}");
            return;
        }

        foreach (var input in spell.validInputs)
        {
            var (keyCode, inputAction) = scheme.GetKeyFromAction(input);

            bool pressedThisFrame = (keyCode.HasValue && Input.GetKeyDown(keyCode.Value)) ||
                                    (inputAction != null && inputAction.WasPressedThisFrame());
            bool isHeld = (keyCode.HasValue && Input.GetKey(keyCode.Value)) ||
                          (inputAction != null && inputAction.IsPressed());
            bool releasedThisFrame = (keyCode.HasValue && Input.GetKeyUp(keyCode.Value)) ||
                                     (inputAction != null && inputAction.WasReleasedThisFrame());

            switch (spell.castType)
            {
                case CastType.Instant:
                    if (pressedThisFrame)
                    {
                        CastSpell(spell, 1f); // Instant = default power
                        if (enableDebugLogs)
                            Debug.Log($"[Spell] Instantly casted {spell.spellName}");

                        // Reset cast state for instant spells
                        spellCastState = SpellCastState.Idle;
                        currentSpell = null;
                        isChargingSpell = false;
                    }
                    break;

                case CastType.Hold:
                    if (pressedThisFrame)
                    {
                        currentHeldInput = input;
                        heldInputTimers[input] = 0f;

                        spellCastState = SpellCastState.Channel;
                        currentSpell = spell;
                        isChargingSpell = true;

                        if (enableDebugLogs)
                            Debug.Log($"[Spell] Started charging (Hold) {spell.spellName}");
                    }
                    else if (isHeld && heldInputTimers.ContainsKey(input))
                    {
                        heldInputTimers[input] += Time.deltaTime;
                    }
                    else if (releasedThisFrame && heldInputTimers.ContainsKey(input))
                    {
                        float heldTime = heldInputTimers[input];
                        if (heldTime >= spell.maxHoldTime)
                        {
                            float power = spell.maxMultiplier;
                            CastSpell(spell, power);

                            // Reset cast state here
                            spellCastState = SpellCastState.Idle;
                            currentSpell = null;
                            isChargingSpell = false;

                            if (enableDebugLogs)
                                Debug.Log($"[Spell] Cast (Hold) {spell.spellName} after full charge ({heldTime:F2}s)");
                        }
                        else
                        {
                            if (enableDebugLogs)
                                Debug.Log($"[Spell] Release too early for {spell.spellName} (Held {heldTime:F2}s, need {spell.maxHoldTime:F2}s)");

                            // Reset on early release
                            spellCastState = SpellCastState.Idle;
                            currentSpell = null;
                            isChargingSpell = false;
                        }

                        // Reset inputs and timers
                        heldInputTimers.Remove(input);
                        stepInputBuffered.Clear();
                        currentHeldInput = null;
                        currentHeldStep = null;
                    }
                    break;

                case CastType.Channel:
                    if (pressedThisFrame)
                    {
                        currentHeldInput = input;
                        heldInputTimers[input] = 0f;

                        spellCastState = SpellCastState.Channel;
                        currentSpell = spell;
                        isChargingSpell = true;

                        if (enableDebugLogs)
                            Debug.Log($"[Spell] Started charging (Channel) {spell.spellName}");
                    }
                    else if (isHeld && heldInputTimers.ContainsKey(input))
                    {
                        heldInputTimers[input] += Time.deltaTime;
                    }
                    else if (releasedThisFrame && heldInputTimers.ContainsKey(input))
                    {
                        float heldTime = heldInputTimers[input];
                        if (heldTime >= spell.minHoldTime)
                        {
                            float normalized = Mathf.Clamp01((heldTime - spell.minHoldTime) / (spell.maxHoldTime - spell.minHoldTime));
                            float power = Mathf.Lerp(spell.minMultiplier, spell.maxMultiplier, normalized);
                            CastSpell(spell, power);

                            // Reset cast state here
                            spellCastState = SpellCastState.Idle;
                            currentSpell = null;
                            isChargingSpell = false;

                            if (enableDebugLogs)
                                Debug.Log($"[Spell] Cast (Channel) {spell.spellName} after {heldTime:F2}s (Power: {power:F2})");
                        }
                        else
                        {
                            if (enableDebugLogs)
                                Debug.Log($"[Spell] Channel too short for {spell.spellName} (Held {heldTime:F2}s, need {spell.minHoldTime:F2}s)");

                            // Reset on too-short hold
                            spellCastState = SpellCastState.Idle;
                            currentSpell = null;
                            isChargingSpell = false;
                        }

                        // Reset inputs and timers
                        heldInputTimers.Remove(input);
                        stepInputBuffered.Clear();
                        currentHeldInput = null;
                        currentHeldStep = null;
                    }
                    break;
            }
        }
    }

    private Vector3 ReticleVect = Vector3.zero;

    private Vector3 GetSpellCastPosition(SpellCastStep spell)
    {
        // Force spawn at locked target if the flag is set
        if (spell.forceLockOnTargetPosition && PlayerCamera.Instance.isLockedOn && PlayerCamera.Instance.LockOnTarget != null)
        {
            return PlayerCamera.Instance.LockOnTarget.transform.position;
        }

        switch (spell.channelMode)
        {
            case SpellChannelMode.TargetReticle:
                return GetReticleWorldPosition(spell);

            case SpellChannelMode.TargetLockedEnemy:
                if (PlayerCamera.Instance.isLockedOn && PlayerCamera.Instance.LockOnTarget != null)
                    return PlayerCamera.Instance.LockOnTarget.transform.position;
                return GetReticleWorldPosition(spell);

            case SpellChannelMode.TargetGroundPoint:
                {
                    Transform hitTransform = GetGroundPointUnderReticle(
                        PlayerCamera.Instance.transform.position,
                        PlayerCamera.Instance.transform.forward,
                        spell.baseCastRange,
                        out ReticleVect,
                        spellReticleFloorLayer
                    );
                    return ReticleVect;
                }

            case SpellChannelMode.SpawnAsProjectile:
                return spell.spellSpawnPoint.transform.position;

            case SpellChannelMode.TargetAroundEnemy:
                if (PlayerCamera.Instance.isLockedOn && PlayerCamera.Instance.LockOnTarget != null)
                    return PlayerCamera.Instance.LockOnTarget.transform.position;
                return Vector3.zero;

            case SpellChannelMode.TargetObject:
                return GetSelectedObjectPosition(spell);

            default:
                return Vector3.zero;
        }
    }



    private bool IsInCastRange(SpellCastStep spell, Vector3 castPos)
    {
        float dist = Vector3.Distance(PlayerDriver.Instance.transform.position, castPos);
        return dist <= spell.baseCastRange;
    }

    // Return world position of player's reticle/crosshair or mouse
    private Vector3 GetReticleWorldPosition(SpellCastStep spell)
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, spell.baseCastRange, spellReticleFloorLayer))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);

            // Only accept surfaces that are less than 45 degrees from 'up' (adjust threshold if needed)
            if (angle <= 75f)
            {
                return hit.point;
            }
        }

        return Vector3.zero;
    }


    /// <summary>
    /// Casts a line forward from origin, then raycasts down at the line's endpoint to find a ground point.
    /// </summary>
    /// <param name="origin">Start position of the linecast (e.g. camera or player).</param>
    /// <param name="direction">Forward direction to cast from origin.</param>
    /// <param name="distance">Distance of the forward line cast.</param>
    /// <param name="groundPoint">Resulting ground hit point.</param>
    /// <param name="floorLayer">LayerMask to hit only valid floor surfaces.</param>
    /// <returns>The Transform of the hit ground object if any; otherwise null.</returns>
    private Transform GetGroundPointUnderReticle(Vector3 origin, Vector3 direction, float distance, out Vector3 groundPoint, LayerMask floorLayer)
    {
        groundPoint = Vector3.zero;

        // Step 1: Project forward
        Vector3 forwardEndPoint = origin + direction.normalized * distance;

        // Step 2: Raycast downward from above that forward point
        if (Physics.Raycast(forwardEndPoint + Vector3.up * 5f, Vector3.down, out RaycastHit hit, distance, floorLayer))
        {
            float angle = Vector3.Angle(hit.normal, Vector3.up);

            // Only accept ground-like surfaces
            if (angle <= 45f)
            {
                groundPoint = hit.point;
                return hit.transform;
            }
        }

        return null;
    }



    // Return position of the currently selected object (e.g., for Whispering Winds)
    private Vector3 GetSelectedObjectPosition(SpellCastStep spell)
    {
        if (PlayerSettings.Instance.gameplaySettings.Mode != CameraMode.TargetMode)
        {
            var scheme = InputManager.Instance.PlayerInputScheme;

            // Get all LockOnTargetHelper enemies within range and line of sight
            var hits = Physics.OverlapSphere(transform.position, spell.baseCastRange);
            List<Transform> targets = new List<Transform>();

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out LockOnTargetHelper helper))
                {
                    Vector3 dir = (hit.transform.position - transform.position).normalized;
                    float dot = Vector3.Dot(transform.forward, dir);

                    if (dot > 0.5f)
                    {
                        // Check for line of sight
                        if (!Physics.Linecast(transform.position + Vector3.up * 1.25f, hit.transform.position + Vector3.up * 1.25f, spellReticleFloorLayer))
                        {
                            targets.Add(hit.transform);
                        }
                    }
                }
            }

            // Sort targets by distance
            targets = targets.OrderBy(t => Vector3.Distance(transform.position, t.position)).ToList();

            if (targets.Count > 0)
            {
                spell.spellSelectedObject = targets[0].transform.gameObject;
            }
        }

        // Your object selection logic here (e.g., via raycast or stored reference)
        if (spell.spellSelectedObject != null)
            return spell.spellSelectedObject.transform.position;

        return Vector3.zero;
    }

    private bool IsAnySpellChargingOrCasting()
    {
        foreach (var kvp in spellState)
        {
            if (kvp.Value == SpellCastState.Channel || kvp.Value == SpellCastState.Cast)
                return true;
        }
        return false;
    }

    private void CancelAllSpells()
    {
        var keys = new List<SpellCastStep>(spellState.Keys);
        foreach (var spell in keys)
        {
            if (spellState[spell] == SpellCastState.Channel || spellState[spell] == SpellCastState.Cast)
            {
                CancelCurrentSpell(spell);
            }
        }
    }

    public void CastSpell(SpellCastStep spell, float power)
    {
        PlayerDriver.Instance.FaceEnemy();
        
        if (enableDebugLogs) Debug.Log($"[CastSpell] Casting {spell.spellName} with power: {power:F2}");

        if (spell.spellPrefab == null)
        {
            if (enableDebugLogs) Debug.LogWarning("[CastSpell] No spell prefab assigned.");
            return;
        }

        // If the spell is persistent (like Whirling Wall), handle recast logic
        if (activePersistentSpells.TryGetValue(spell.spellName, out GameObject existingInstance))
        {
            // Recast behavior — e.g., stop movement
            if (existingInstance != null && !spell.hasUnlimitedInstances)
            {
                if (enableDebugLogs) Debug.Log($"[CastSpell] {spell.spellName} is already active — performing recast behavior.");

                Spell spellComponent = existingInstance.GetComponent<Spell>();

                if (spellComponent != null && existingInstance.TryGetComponent<WhirlingWallSpell>(out WhirlingWallSpell wallspell))
                    wallspell.OnRecast();

                return;
            }
            else
            {
                activePersistentSpells.Remove(spell.spellName);
            }
        }

        Vector3 spawnPos = GetSpellCastPosition(spell);
        Quaternion spawnRot = transform.rotation; // Default facing forward

        // If locked on target exists, face that target horizontally (Y only)
        var target = PlayerCamera.Instance.LockOnTarget;
        if (target != null && PlayerCamera.Instance.isLockedOn)
        {
            Vector3 directionToTarget = target.transform.position - spawnPos;
            directionToTarget.y = 0f; // Remove vertical component to avoid tilt
            if (directionToTarget.sqrMagnitude > 0.001f)
            {
                spawnRot = Quaternion.LookRotation(directionToTarget.normalized);
            }
        }


        if (spawnPos == Vector3.zero)
        {
            if (enableDebugLogs) Debug.LogWarning($"[CastSpell] Cast position was Zero. Position: {spawnPos}, Range: {spell.baseCastRange}");
            return;
        }

        if (!IsInCastRange(spell, spawnPos))
        {
            if (enableDebugLogs) Debug.LogWarning($"[CastSpell] Cast position out of range. Position: {spawnPos}, Range: {spell.baseCastRange}");
            return;
        }

        GameObject proj = Instantiate(spell.spellPrefab, spawnPos, spawnRot);
        Spell spellObj = proj.GetComponent<Spell>();
        if (spellObj == null)
        {
            if (enableDebugLogs) Debug.LogWarning("[CastSpell] Spell prefab missing Spell component.");
            return;
        }

        CharacterStats stats = GetComponent<CharacterStats>();
        spellObj.Initialize(GetComponent<ComboManager>(), stats, spell.baseDuration, spell.baseCastRange, spell.spellName);

        // Track this persistent spell if needed
        activePersistentSpells[spell.spellName] = proj;

        // Cleanup inputs
        heldInputTimers.Clear();
        stepInputBuffered.Clear();
        currentHeldInput = null;
        currentHeldStep = null;
        spellState[spell] = SpellCastState.Idle;

        // Reset spell cast state here to Idle after casting
        spellCastState = SpellCastState.Idle;
        currentSpell = null;
        isChargingSpell = false;

        if (enableDebugLogs) Debug.Log($"[CastSpell] {spell.spellName} launched successfully at {spawnPos}.");
    }

    public void CancelCurrentSpell(SpellCastStep spell)
    {
        if (spellState.ContainsKey(spell))
        {
            spellState[spell] = SpellCastState.Cancelled;

            if (enableDebugLogs)
                Debug.Log($"[Spell] Casting of {spell.spellName} cancelled.");

            // Reset timers and input states related to this spell
            heldInputTimers.Clear();
            stepInputBuffered.Clear();
            currentHeldInput = null;
            currentHeldStep = null;
        }
    }

    public void ClearPersistentSpell(string spellName)
    {
        if (activePersistentSpells.ContainsKey(spellName))
            activePersistentSpells.Remove(spellName);
    }


    public ComboConditionState GetCurrentComboConditionState()
    {
        var playerState = PlayerDriver.Instance.physicsProperties;

        if (playerState.isWallKicking || playerState.isWallHanging)
            return ComboConditionState.Wall;

        if (playerState.isTempestKicking || playerState.isSkywardAscending || playerState.hovering || !playerState.Grounded)
            return ComboConditionState.Aerial;

        if (playerState.Grounded)
            return ComboConditionState.Grounded;

        return ComboConditionState.None;
    }

    private void UpdatePlacementIndicatorPosition()
    {
        Vector3 aimPosition;

        if (PlayerCamera.Instance.isLockedOn && PlayerCamera.Instance.LockOnTarget != null)
        {
            // Auto place on enemy location or in front
            var target = PlayerCamera.Instance.LockOnTarget.transform;
            aimPosition = target.position;
            // Or target.position + target.forward * offset
        }
        else
        {
            // Raycast from camera or player forward to get ground hit position within cast range
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, currentSpell.baseCastRange))
            {
                aimPosition = hit.point;
            }
            else
            {
                aimPosition = PlayerDriver.Instance.transform.position + PlayerDriver.Instance.transform.forward * currentSpell.baseCastRange;
            }
        }

        placementIndicator.transform.position = aimPosition;
    }

    private void TagInitialAndFinisherAttacks()
    {
        foreach (var combo in AllAttacks)
        {
            if (combo.attackStrings == null || combo.attackStrings.Count == 0)
                continue;

            foreach (var step in combo.attackStrings)
            {
                step.isInitialAttack = false;
                step.isFinisherAttack = false;
            }

            combo.attackStrings[0].isInitialAttack = true;
            combo.attackStrings[^1].isFinisherAttack = true;
        }
    }

    public void OnSuccessfulHit()
    {
        comboCount += 1;
        currentTempo = GetTempoThreshold(comboCount);
        GetComponent<EntityEventManager>().OnComboUpdatedTrigger(GetComponent<ComboManager>());
    }

    public void OnDamageTaken(float penalty)
    {
        currentTempo = Mathf.Max(0, currentTempo - penalty);
    }

    private float GetTempoThreshold(int combo)
    {
        if (combo >= 150) return 5f;
        if (combo >= 100) return 4f;
        if (combo >= 50) return 3f;
        return 2f;
    }

    private int GetTier(int combo)
    {
        if (combo >= 150) return 3;
        if (combo >= 100) return 2;
        if (combo >= 50) return 1;
        return 0;
    }

    private void TryInstantComboStep()
    {
        KeyActions? input = DetectComboInputHoldable(out bool isReleased);
        if (input == null)
        {
            if (enableDebugLogs) Debug.Log("TryInstantComboStep: No input detected.");
            return;
        }

        if (enableDebugLogs) Debug.Log($"TryInstantComboStep: Input detected: {input.Value}, Released: {isReleased}");

        // Only allow execution once per press for Instant-type
        if (stepInputBuffered.TryGetValue(input.Value, out bool buffered) && buffered && !isReleased)
        {
            if (enableDebugLogs) Debug.Log($"TryInstantComboStep: Input {input.Value} is buffered and not released yet. Skipping.");
            return;
        }

        // Only allow execution once per press for Instant-type
        if (TryGetComponent<PlayerInteractor>(out PlayerInteractor interact) && interact.holdingObject)
        {
            if (enableDebugLogs) Debug.Log($"Prevented Attack Due To Holding Object");
            return;
        }

        Combo activeCombo = GetActiveCombo();
        if (activeCombo == null)
        {
            if (enableDebugLogs) Debug.Log("TryInstantComboStep: No active combo found for current stance.");
            return;
        }

        if (enableDebugLogs) Debug.Log($"TryInstantComboStep: Active combo: {activeCombo.ComboName}, RequiredState: {activeCombo.requiredState}");

        if (currComboStep >= activeCombo.attackStrings.Count)
            return;
        
        ComboStep step = activeCombo.attackStrings[currComboStep];

        if (step == null)
        {
            if (enableDebugLogs) Debug.Log("TryInstantComboStep: No combo step found at current combo step index.");
            return;
        }

        if (enableDebugLogs) Debug.Log($"TryInstantComboStep: Current combo step: {currComboStep}, Animation Event: {step.animationEventName}");

        if (step.allowedStances != null && !step.allowedStances.Contains(currentStance))
        {
            if (enableDebugLogs) Debug.Log($"TryInstantComboStep: Current stance {currentStance} NOT allowed for this step.");
            return;
        }

        // Check required ComboConditionState before allowing step
        ComboConditionState currentState = GetCurrentComboConditionState();
        if (enableDebugLogs) Debug.Log($"TryInstantComboStep: Current player state: {currentState}");

        if (activeCombo.requiredState != ComboConditionState.None && activeCombo.requiredState != currentState)
        {
            if (enableDebugLogs) Debug.Log($"TryInstantComboStep: Combo requires state {activeCombo.requiredState} but current state is {currentState}. Aborting.");
            return;
        }

        if (step.validInputs.Contains(input.Value))
        {
            if (enableDebugLogs) Debug.Log($"TryInstantComboStep: Input {input.Value} is valid for this combo step.");

            if (step.castType == CastType.Instant)
            {
                if (!stepInputBuffered.ContainsKey(input.Value) || !stepInputBuffered[input.Value])
                {
                    if (enableDebugLogs) Debug.Log($"TryInstantComboStep: Executing combo step {step.animationEventName} for Instant cast.");

                    float attackCooldown = GetComponent<CharacterStats>().GetAttackCooldown(GetActiveCombo().baseAttackSpeed);

                    if (Time.time < nextAttackAllowedTime)
                    {
                        if (enableDebugLogs)
                            Debug.Log($"Too soon to attack again. Wait {(nextAttackAllowedTime - Time.time):F2} seconds.");
                        return;
                    }

                    ExecuteComboStep(step);
                    ProceedCombo(activeCombo, step, input.Value);
                    nextAttackAllowedTime = Time.time + attackCooldown; 
                    stepInputBuffered[input.Value] = true;
                }
                else
                {
                    if (enableDebugLogs) Debug.Log($"TryInstantComboStep: Input {input.Value} already buffered, skipping.");
                }
            }
            else if (step.castType == CastType.Hold || step.castType == CastType.Channel)
            {
                if (!currentHeldInput.HasValue)
                {
                    if (enableDebugLogs) Debug.Log($"TryInstantComboStep: Starting hold/channel cast with input {input.Value}.");
                    currentHeldInput = input;
                    currentHeldStep = step;
                    heldInputTimers[input.Value] = 0f;
                }
                else if (isReleased && currentHeldInput == input)
                {
                    float heldTime = heldInputTimers[input.Value];
                    if (enableDebugLogs) Debug.Log($"TryInstantComboStep: Released hold input {input.Value} after {heldTime} seconds. Required hold: {step.requiredHoldTime}");
                    if (heldTime >= step.requiredHoldTime)
                    {
                        if (enableDebugLogs) Debug.Log($"TryInstantComboStep: Hold time sufficient, executing combo step.");

                        float attackCooldown = GetComponent<CharacterStats>().GetAttackCooldown(GetActiveCombo().baseAttackSpeed);

                        if (Time.time < nextAttackAllowedTime)
                        {
                            if (enableDebugLogs)
                                Debug.Log($"Too soon to attack again. Wait {(nextAttackAllowedTime - Time.time):F2} seconds.");
                            return;
                        }

                        ExecuteComboStep(step);
                        ProceedCombo(activeCombo, step, input.Value);
                        nextAttackAllowedTime = Time.time + attackCooldown; 
                    }
                    else
                    {
                        if (enableDebugLogs) Debug.Log($"TryInstantComboStep: Hold time insufficient, skipping combo step.");
                    }
                    heldInputTimers.Remove(input.Value);
                    currentHeldInput = null;
                    currentHeldStep = null;
                }
            }
        }
        else
        {
            Debug.Log($"TryInstantComboStep: Input {input.Value} is NOT valid for this combo step.");
        }

        // Reset input buffer when key is released
        if (isReleased && stepInputBuffered.ContainsKey(input.Value))
        {
            Debug.Log($"TryInstantComboStep: Input {input.Value} released, resetting buffer.");
            stepInputBuffered[input.Value] = false;
        }
    }

    /// <summary>
    /// Disable gravity during the attack duration.
    /// </summary>
    private IEnumerator DisableGravityTemporarily(float duration)
    {
        var props = PlayerDriver.Instance.physicsProperties;

        props.temporarilyDisableGravity = true;

        float timer = 0f;

        while (timer < duration)
        {
            // Disable Unity gravity
            props.rb.useGravity = false;
            props.ApplyGravity = false;

            // Force vertical velocity to zero every frame
            Vector3 lv = props.rb.linearVelocity;
            lv.y = 0f;
            props.rb.linearVelocity = lv;

            props.gravityMultiplier = 0f;
            props.velocity.y = 0f;

            timer += Time.deltaTime;
            yield return null;
        }

        // Re-enable gravity after duration
        props.temporarilyDisableGravity = false;
        props.rb.useGravity = true;
        props.ApplyGravity = true;
    }

    private KeyActions? DetectComboInputHoldable(out bool released)
    {
        released = false;

        foreach (InputKey key in InputManager.Instance.PlayerInputScheme.Inputs)
        {
            if (Input.GetKeyDown(key.key)) return key.BoundAction;
            if (Input.GetKey(key.key) && heldInputTimers.ContainsKey(key.BoundAction))
                heldInputTimers[key.BoundAction] += Time.deltaTime;
            if (Input.GetKeyUp(key.key))
            {
                released = true;
                return key.BoundAction;
            }
        }
        return null;
    }

    private void ProceedCombo(Combo combo, ComboStep step, KeyActions input)
    {
        foreach (var branch in step.branches)
        {
            if (branch.validInputs.Contains(input))
            {
                currComboStep = combo.attackStrings.IndexOf(branch.nextStep);
                comboState = ComboState.Idle;
                comboTimer = comboResetTimer;
                return;
            }
        }

        currComboStep++;
        if (currComboStep >= combo.attackStrings.Count) ResetCombo();
        else comboState = ComboState.Idle;

        comboTimer = comboResetTimer;
    }

    public void OnComboAnimationEvent(string eventName)
    {
        Combo activeCombo = GetActiveCombo();
        if (activeCombo == null) return;

        KeyActions? detectedInput = DetectComboInput();
        if (detectedInput == null) { ResetCombo(); return; }

        if (currComboStep == 0)
        {
            ComboStep firstStep = activeCombo.attackStrings[0];
            if (firstStep.animationEventName == eventName)
            {
                ExecuteComboStep(firstStep);
                currComboStep++;
                comboTimer = comboResetTimer;
                comboState = ComboState.Idle;
                return;
            }
            return;
        }

        if (currComboStep >= activeCombo.attackStrings.Count) { ResetCombo(); return; }

        ComboStep currentStep = activeCombo.attackStrings[currComboStep];
        if (currentStep.animationEventName != eventName) return;
        if (currentStep.allowedStances.Count > 0 && !currentStep.allowedStances.Contains(currentStance))
        {
            ResetCombo();
            return;
        }

        // Check required ComboConditionState before executing animation event step
        if (activeCombo.requiredState != ComboConditionState.None && activeCombo.requiredState != GetCurrentComboConditionState())
        {
            ResetCombo();
            return;
        }

        comboState = ComboState.Attacking;
        ExecuteComboStep(currentStep);
        comboTimer = comboResetTimer;

        foreach (var branch in currentStep.branches)
        {
            if (branch.validInputs.Contains(detectedInput.Value))
            {
                currComboStep = activeCombo.attackStrings.IndexOf(branch.nextStep);
                comboState = ComboState.Idle;
                return;
            }
        }

        currComboStep++;
        if (currComboStep >= activeCombo.attackStrings.Count) ResetCombo();
        else comboState = ComboState.Idle;
    }



    private KeyActions? DetectComboInput()
    {
        foreach (InputKey key in InputManager.Instance.PlayerInputScheme.Inputs)
            if (Input.GetKeyDown(key.key)) return key.BoundAction;
        return null;
    }

    public Combo GetActiveCombo()
    {
        var currentState = GetCurrentComboConditionState();

        // 1. Try to find combo that matches current stance and required state
        Combo combo = AllAttacks.Find(c =>
            c.stance == currentStance &&
            c.IsUnlocked &&
            c.requiredState == currentState);

        // 2. Fallback: Try to find combo that requires no specific state
        if (combo == null)
        {
            combo = AllAttacks.Find(c =>
                c.stance == currentStance &&
                c.IsUnlocked &&
                c.requiredState == ComboConditionState.None);
        }

        // 3. If still null, return null
        if (combo == null)
            return null;

        // 4. If no usable steps or spells, treat combo as inactive
        bool hasSteps = combo.attackStrings != null && combo.attackStrings.Count > 0;
        bool hasSpells = combo.attackSpells != null && combo.attackSpells.Count > 0;

        if (!hasSteps && !hasSpells)
            return null;

        return combo;
    }



    private Combo currentCombo;
    private Coroutine gravityCoroutine;

    public void ExecuteComboStep(ComboStep step)
    {
        step.attackDurationTimer = step.AttackDuration;

        if (step == null)
        {
            Debug.LogWarning("Trying to execute a null ComboStep.");
            return;
        }

        // Also reset isPlaying to false just in case
        isPlaying = false;

        StartCoroutine(HandleComboStepRoutine(step));
    }

    private IEnumerator HandleComboStepRoutine(ComboStep step)
    {
        PlayerDriver.Instance.FaceEnemy();

        float duration = step.AttackDuration;
        step.attackDurationTimer = 0f;
        isPlaying = true;

        bool isLockedOn = PlayerCamera.Instance.isLockedOn;
        Transform currentZTarget = isLockedOn ? PlayerCamera.Instance.LockOnTarget?.transform : null;

        step.OnStepStart?.Invoke();

        // === Disable gravity if flagged ===
        if (step.disableGravityOnCast)
        {
            if (gravityCoroutine != null)
                StopCoroutine(gravityCoroutine);
            gravityCoroutine = StartCoroutine(DisableGravityTemporarily(step.gravityDisableDuration));
        }

        // === Begin attack movement ===
        if (step.useAttackMotion && step.attackMotionDistance > 0f && step.attackMotionCurve != null)
        {
            Vector3 motionDir = step.projectileSpawnPoint.TransformDirection(step.attackMotionDirection.normalized);
            StartCoroutine(PerformAttackMovement(step, motionDir));
        }

        // === Handle projectile logic ===
        if (step.projectilePrefab != null && step.projectileSpawnPoint != null)
        {
            Vector3 fireDirection;

            if (isLockedOn && currentZTarget != null)
            {
                fireDirection = (currentZTarget.position - step.projectileSpawnPoint.position).normalized;

            }
            else if(PlayerDriver.Instance.physicsProperties.MoveType.Equals(MovementType.FPSMove))
            {
                fireDirection = Camera.main.transform.forward;
            }
            else
            {
                fireDirection = step.projectileSpawnPoint.forward;
            }

            step.projectileSpawnPoint.forward = fireDirection;

            switch (step.spawnType)
            {
                case ProjectileSpawnType.Straight:
                    FireProjectile(step, fireDirection);
                    break;
                case ProjectileSpawnType.Fan:
                    FireFanProjectiles(step);
                    break;
                case ProjectileSpawnType.Burst:
                    yield return StartCoroutine(FireBurstProjectiles(step));
                    break;
            }
        }
        else
        {
            Debug.Log("Melee attack: " + step.attack.BaseDamage);
        }

        if (step.isInitialAttack) Debug.Log("Initial Attack!");
        if (step.isFinisherAttack) Debug.Log("Finisher Attack!");

        Debug.Log($"Combo step executed: {step.currComboStep} - Crit: {step.attack?.IsCriticalHit ?? false}");

        // === Wait for duration ===
        while (step.attackDurationTimer < duration)
        {
            step.attackDurationTimer += Time.deltaTime;
            yield return null;
        }

        // === Reset Projectile Spawn Point After An Attack ===
        step.projectileSpawnPoint.transform.rotation = transform.rotation;

        isPlaying = false;
        step.OnStepEnd?.Invoke();
    }

    private IEnumerator PerformAttackMovement(ComboStep step, Vector3 direction)
    {
        float elapsed = 0f;
        float duration = 0.3f; // Time over which movement happens
        float distance = step.attackMotionDistance;
        Vector3 startPos = PlayerDriver.Instance.transform.position;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            float curveValue = step.attackMotionCurve.Evaluate(t); // 0 → 1
            float currentDistance = curveValue * distance;

            Vector3 targetPos = startPos + direction * currentDistance;
            Vector3 motionDelta = (targetPos - PlayerDriver.Instance.transform.position);

            // Optional: Clamp motionDelta to prevent tunneling
            if (motionDelta.magnitude > 0.1f)
                motionDelta = motionDelta.normalized * 0.1f;

            // This uses MovePosition so collisions are respected
            PlayerDriver.Instance.physicsProperties.rb.MovePosition(PlayerDriver.Instance.transform.position + motionDelta);

            elapsed += Time.deltaTime;
            yield return new WaitForFixedUpdate(); // Use FixedUpdate for physics sync
        }
    }

    private void FireProjectile(ComboStep step, Vector3 direction)
    {
        GameObject proj = Instantiate(
            step.projectilePrefab,
            step.projectileSpawnPoint.position,
            Quaternion.LookRotation(direction)
        );

        if (proj.TryGetComponent(out Projectile projectile))
        {
            projectile.projectileSpeed = step.projectileSpeed;

            // Apply launch with correct source and direction
            projectile.SetDirection(direction);
            projectile.Launch(step.attack, GetComponent<CharacterStats>(), GetComponent<ComboManager>());
        }
        else
        {
            Debug.LogWarning("Projectile prefab missing 'Projectile' component.");
        }
    }

    private IEnumerator FireBurstProjectiles(ComboStep step)
    {
        for (int i = 0; i < step.burstCount; i++)
        {
            FireProjectile(step, step.projectileSpawnPoint.forward);
            yield return new WaitForSeconds(step.burstRate);
        }
    }

    private void FireFanProjectiles(ComboStep step)
    {
        int count = Mathf.Max(1, step.numberOfProjectiles);
        float totalAngle = Mathf.Min(180f, step.spreadAngle); // Clamp max spread
        float angleStep = count > 1 ? totalAngle / (count - 1) : 0;
        float startAngle = -totalAngle / 2;

        for (int i = 0; i < count; i++)
        {
            float angle = startAngle + angleStep * i;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * step.projectileSpawnPoint.forward;
            FireProjectile(step, dir);
        }
    }

    public void ResetCombo()
    {
        currComboStep = 0;
        comboTimer = 0f;
        comboState = ComboState.Idle;
        bufferedInput = false;
    }

    public void CancelCombo(bool reset = true)
    {
        comboState = ComboState.Canceling;
        if (reset) ResetCombo();
    }

    public void SetStance(Stance newStance)
    {
        ResetCombo();
        currentStance = newStance;
    }

    public void UnlockCombo(string comboName)
    {
        Combo combo = AllAttacks.Find(c => c.ComboName == comboName);
        if (combo != null) combo.IsUnlocked = true;
    }

    public void LockCombo(string comboName)
    {
        Combo combo = AllAttacks.Find(c => c.ComboName == comboName);
        if (combo != null) combo.IsUnlocked = false;
    }

    void CancelCombo()
    {
        if (enableDebugLogs) Debug.Log("Combo canceled due to spell casting");

        isPlaying = false;
        comboState = ComboState.Idle;
        currComboStep = 0;
        comboTimer = 0f;
        bufferedInput = false;
        currentHeldStep = null;
        // Reset or clear any other combo-related states here
    }

    public bool IsCurrentComboStepPlaying(out ComboStep step)
    {
        step = null;

        Combo activeCombo = GetActiveCombo();

        if (activeCombo == null || currComboStep >= activeCombo.attackStrings.Count)
            return false;

        ComboStep currentStep = activeCombo.attackStrings[currComboStep];

        if (currentStep != null && isPlaying)
        {
            step = currentStep;
            return true;
        }

        return false;
    }

    public bool IsSpellBeingCast()
    {
        // Returns true if any spell is in Channel or Cast state
        foreach (var kvp in spellState)
        {
            if (kvp.Value == SpellCastState.Channel || kvp.Value == SpellCastState.Cast)
                return true;
        }

        // Also check the general spellCastState if you use it globally
        if (spellCastState == SpellCastState.Channel || spellCastState == SpellCastState.Cast)
            return true;

        return false;
    }

    private void OnDrawGizmos()
    {
        if (spellCastState == SpellCastState.Channel || spellCastState == SpellCastState.Cast)
        {
            if (currentSpell != null)
            {
                Vector3 castPos = GetSpellCastPosition(currentSpell);

                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(castPos, 1f); // radius depends on spell size

                // Optionally draw an arrow showing direction
                Vector3 forward = PlayerDriver.Instance.transform.forward;
                Gizmos.DrawLine(castPos, castPos + forward * 2f);
            }
        }
    }

}