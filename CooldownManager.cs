using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single ability’s cooldown data.
/// </summary>
[System.Serializable]
public class CooldownEntity
{
    [Tooltip("The unique name of the ability or action.")]
    public string AbillityName;

    [Tooltip("Current remaining time (in seconds) before the ability is ready again.")]
    public float currentCooldown;

    [Tooltip("The total cooldown duration (in seconds) when the ability is triggered.")]
    public float maximumCooldown;

    [Tooltip("Whether the ability is ready to be used (true) or still cooling down (false).")]
    public bool ready = true;

    /// <summary>
    /// Creates a new cooldown entity.
    /// </summary>
    /// <param name="name">Ability name used as the key.</param>
    /// <param name="cooldown">Cooldown duration in seconds.</param>
    public CooldownEntity(string name, float cooldown)
    {
        AbillityName = name;
        maximumCooldown = cooldown;
        currentCooldown = cooldown;
        ready = true;
    }
}

/// <summary>
/// Central manager for all ability cooldowns.
/// Attach this to a GameObject (e.g. your Player) and call
/// CooldownManagerTick() each Update() to drive all timers.
/// </summary>
public class CooldownManager : MonoBehaviour
{
    [Header("Cooldown List")]
    public List<CooldownEntity> Cooldowns = new List<CooldownEntity>();

    // Singleton instance for the PlayerDriver — allows global static access to this specific instance
    private static CooldownManager _instance;

    // Flag indicating whether the singleton was destroyed (prevents duplicate instantiation)
    static bool _destroyed;

    public static CooldownManager Instance
    {
        get
        {
            // Prevent re-creation of the singleton during play mode exit.
            if (_destroyed) return null;

            // If the instance is already valid, return it. Needed if called from a
            // derived class that wishes to ensure the instance is initialized.
            if (_instance != null) return _instance;

            // Find the existing instance (across domain reloads).
            if ((_instance = FindObjectOfType<CooldownManager>()) != null) return _instance;

            // Create a new GameObject instance to hold the singleton component.
            var gameObject = new GameObject(typeof(CooldownManager).Name);

            // Move the instance to the DontDestroyOnLoad scene to prevent it from
            // being destroyed when the current scene is unloaded.
            DontDestroyOnLoad(gameObject);

            // Create the MonoBehavior component. Awake() will assign _instance.
            return gameObject.AddComponent<CooldownManager>();
        }
    }

    // Called when the script instance is being loaded (before Start or any frame updates)
    protected virtual void Awake()
    {
        // Ensure only one instance of this class exists (singleton pattern)
        Debug.Assert(_instance == null || _instance == this, "More than one singleton instance instantiated!", this);

        // Assign this instance if it's the first or correct one
        if (_instance == null || _instance == this)
        {
            _instance = this;
        }
    }

    private void Update()
    {
        CooldownManagerTick();
    }

    #region Cooldowns
    /// <summary>
    /// Call this each frame (e.g. from Update) to decrement timers,
    /// clamp values, and flip ready flags when complete.
    /// </summary>
    public void CooldownManagerTick()
    {
        for (int i = 0; i < Cooldowns.Count; i++)
        {
            var cd = Cooldowns[i];

            cd.currentCooldown = Mathf.Clamp(cd.currentCooldown, 0f, 999f);

            if (!cd.ready)
                cd.currentCooldown -= Time.deltaTime;

            if (cd.currentCooldown <= 0f && !cd.ready)
            {
                cd.currentCooldown = 0f;
                cd.ready = true;
            }
        }
    }

    /// <summary>Adds a new cooldown to the list.</summary>
    public void CreateNewCooldown(CooldownEntity cooldown)
    {
        if (Cooldowns.Exists(c => c.AbillityName == cooldown.AbillityName))
        {
            Debug.LogWarning($"Cooldown '{cooldown.AbillityName}' already exists.");
            return;
        }
        Cooldowns.Add(cooldown);
    }

    /// <summary>Removes a cooldown from the list by ability name.</summary>
    public void DestroyCooldown(string name)
    {
        for (int i = 0; i < Cooldowns.Count; i++)
        {
            if (Cooldowns[i].AbillityName == name)
            {
                Debug.Log($"Removed cooldown: {name}");
                Cooldowns.RemoveAt(i);
                return;
            }
        }
        Debug.LogError($"DestroyCooldown: '{name}' not found.");
    }

    /// <summary>
    /// Checks if a cooldown by name is ready (i.e., finished).
    /// Returns true if not found (with an error).
    /// </summary>
    public bool CooldownState(string cooldownName)
    {
        foreach (var cd in Cooldowns)
            if (cd.AbillityName == cooldownName)
                return cd.ready;

        Debug.LogError($"CooldownState: '{cooldownName}' not found. Returning true.");
        return true;
    }

    /// <summary>
    /// Initiates a cooldown by name, resetting its timer and marking not ready.
    /// </summary>
    public void IntiateCooldown(string cooldownName)
    {
        foreach (var cd in Cooldowns)
        {
            if (cd.AbillityName == cooldownName)
            {
                cd.ready = false;
                cd.currentCooldown = cd.maximumCooldown;
                Debug.Log($"Started '{cd.AbillityName}' cooldown.");
                return;
            }
        }
        Debug.LogError($"IntiateCooldown: '{cooldownName}' not found.");
    }

    /// <summary>Sets a cooldown's duration and resets it to that duration.</summary>
    public void SetCooldown(string cooldownName, float newduration)
    {
        foreach (var cd in Cooldowns)
        {
            if (cd.AbillityName == cooldownName)
            {
                cd.maximumCooldown = newduration;
                cd.currentCooldown = newduration;
                cd.ready = false;
                Debug.Log($"Set '{cd.AbillityName}' cooldown to {newduration}s.");
                return;
            }
        }
        Debug.LogError($"SetCooldown: '{cooldownName}' not found.");
    }

    /// <summary>
    /// Reduces the remaining cooldown time by a specified amount.
    /// Marks it not ready.
    /// </summary>
    public void ReduceCooldown(string cooldownName, int amount)
    {
        foreach (var cd in Cooldowns)
        {
            if (cd.AbillityName == cooldownName)
            {
                cd.ready = false;
                cd.currentCooldown -= amount;
                Debug.Log($"Reduced '{cooldownName}' by {amount}s.");
                return;
            }
        }
        Debug.LogError($"ReduceCooldown: '{cooldownName}' not found.");
    }

    /// <summary>
    /// Resets a specific cooldown to its maximum and marks it ready.
    /// </summary>
    public void ResetCooldownName(string cooldownName)
    {
        foreach (var cd in Cooldowns)
        {
            if (cd.AbillityName == cooldownName)
            {
                cd.ready = true;
                cd.currentCooldown = cd.maximumCooldown;
                Debug.Log($"Reset cooldown: {cooldownName}");
                return;
            }
        }
        Debug.LogError($"ResetCooldownName: '{cooldownName}' not found.");
    }

    /// <summary>Resets all cooldowns, marking them ready and zeroing timers.</summary>
    public void ResetAllCooldowns()
    {
        foreach (var cd in Cooldowns)
        {
            cd.ready = true;
            cd.currentCooldown = 0f;
        }
        if (Cooldowns.Count > 0)
            Debug.Log($"All {Cooldowns.Count} cooldown(s) reset.");
        else
            Debug.LogWarning("ResetAllCooldowns: no cooldowns to reset.");
    }
    #endregion

}
