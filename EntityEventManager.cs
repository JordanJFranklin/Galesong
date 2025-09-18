using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using static UnityEngine.Rendering.LineRendering;

public class EntityEventManager : MonoBehaviour
{
    public event Action OnTakeDamage;
    public event Action OnSelfDamage;
    public event Action OnScarletGained;

    public event Action OnWalk;
    public event Action OnRun;
    public event Action OnGrind;
    public event Action OnGrindExit;
    public event Action OnJump;
    public event Action OnDoubleJump;
    public event Action OnNimbus;
    public event Action OnFreefall;
    public event Action OnHover;
    public event Action OnWallJump;
    public event Action OnEvade;
    public event Action OnBoost;
    public event Action OnDodge;
    public event Action OnDodgeExit;
    public event Action OnPerfectDodge;
    public event Action OnStandStill;
    public event Action OnGrapple;

    public event Action OnDamage;
    public event Action OnCriticalHit;
    public event Action OnLightAttackHit;
    public event Action OnHeavyAttackHit;
    public event Action OnMeleeHit;
    public event Action OnRangedHit;
    public event Action OnJumpAttackHit;
    public event Action OnBlock;

    public event Action OnSpellCast;
    public event Action OnSpellEnd;

    public event Action OnSummonDealDamage;
    public event Action OnSummonTakeDamage;
    public event Action OnSummonSpawned; // Summon spawn
    public event Action OnSummonDeath;   // Summon dies
    public event Action OnSummonCrit;    // Summon crits

    public event Action OnHealReceived;
    public event Action OnOverhealRecieved;
    public event Action OnGourdUse;
    public event Action OnLowHealth; // e.g., below 50%
    public event Action OnRevive;
    public event Action OnDeath;

    public event Action<StatusEffectType> OnStatusApplied;
    public event Action OnStatusCleansed;
    public event Action OnPetrifyExpired;
    public event Action OnStatusReflected;

    public event Action<ComboManager> OnComboUpdated;
    public event Action OnComboExpired;

    private CharacterStats statSource;

    public bool debugMode = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        statSource = GetComponent<CharacterStats>();
    }

    #region Movement 
    public void OnGrappleTrigger()
    {
        Action currAction = OnGrapple;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnGrapple");
    }

    public void OnStandStillTrigger()
    {
        Action currAction = OnStandStill;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnStandStill");
    }

    public void OnWalkTrigger()
    {
        Action currAction = OnWalk;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnWalk");
    }

    public void OnEvadeTrigger()
    {
        Action currAction = OnEvade;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnEvade");
    }

    public void OnPerfectDodgeTrigger()
    {
        Action currAction = OnPerfectDodge;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnPerfectDodge");
    }

    public void OnDodgeTrigger()
    {
        Action currAction = OnDodge;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnDash");
    }

    public void OnDodgeExitTrigger()
    {
        Action currAction = OnDodgeExit;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnDashExit");
    }

    public void OnRunTrigger()
    {
        Action currAction = OnRun;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnRun");
    }

    public void OnBoostTrigger()
    {
        Action currAction = OnBoost;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnBoost");
    }

    public void OnJumpTrigger()
    {
        Action currAction = OnJump;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnJump");
    }

    public void OnFreefallTrigger()
    {
        Action currAction = OnFreefall;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnJump");
    }

    public void OnDoubleJumpTrigger()
    {
        Action currAction = OnDoubleJump;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnDoubleJump");
    }

    public void OnHoverTrigger()
    {
        Action currAction = OnHover;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnHover");
    }

    public void OnNimbusFlightTrigger()
    {
        Action currAction = OnNimbus;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnNimbus");
    }

    public void OnWallJumpTrigger()
    {
        Action currAction = OnWallJump;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnWallJump");
    }

    public void OnGrindEnterTrigger()
    {
        Action currAction = OnGrind;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnGrind");
    }

    public void OnGrindExitTrigger()
    {
        Action currAction = OnGrindExit;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnGrindExit");
    }


    #endregion

    #region Vitality
    public void OnTakeDamageTrigger()
    {
        Action currAction = OnTakeDamage;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnTakeDamage");
    }

    public void OnSelfDamageTrigger()
    {
        Action currAction = OnSelfDamage;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnSelfDamage");
    }

    public void OnHealthHealedTrigger()
    {
        Action currAction = OnHealReceived;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnHealReceived");
    }

    public void OnScarletGainTrigger()
    {
        Action currAction = OnScarletGained;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnScarletGained");
    }


    public void OnHealReceivedTrigger()
    {
        Action currAction = OnHealReceived;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnHealReceived");
    }

    public void OnOverhealReceivedTrigger()
    {
        Action currAction = OnOverhealRecieved;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnOverhealRecieved");
    }

    public void OnGourdUseTrigger()
    {
        Action currAction = OnGourdUse;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnGourdUse");
    }

    public void OnLowHealthTrigger()
    {
        Action currAction = OnLowHealth;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnLowHealth");
    }

    public void OnReviveTrigger()
    {
        Action currAction = OnRevive;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnRevive");
    }

    public void OnDeathTrigger()
    {
        Action currAction = OnDeath;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnDeath");
    }

    #endregion

    #region Combat
    public void OnDamageTrigger()
    {
        Action currAction = OnDamage;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnDamage");
    }

    public void OnCriticalHitTrigger()
    {
        Action currAction = OnCriticalHit;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnCriticalHit");
    }

    public void OnLightAttackHitTrigger()
    {
        Action currAction = OnLightAttackHit;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnLightAttackHit");
    }

    public void OnHeavyAttackHitTrigger()
    {
        Action currAction = OnHeavyAttackHit;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnHeavyAttackHit");
    }

    public void OnMeleeHitTrigger()
    {
        Action currAction = OnMeleeHit;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnMeleeHit");
    }

    public void OnRangedHitTrigger()
    {
        Action currAction = OnRangedHit;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnRangedHit");
    }

    public void OnJumpAttackHitTrigger()
    {
        Action currAction = OnJumpAttackHit;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnJumpAttackHit");
    }

    public void OnBlockTrigger()
    {
        Action currAction = OnBlock;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnBlock");
    }

    public void OnSpellCastTrigger()
    {
        Action currAction = OnSpellCast;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnSpellCast");
    }

    public void OnSpellEndTrigger()
    {
        Action currAction = OnSpellEnd;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnSpellEnd");
    }

    public void OnStatusAppliedTrigger(StatusEffectType type)
    {
        var currAction = OnStatusApplied;
        if (currAction == null) return;
        currAction.Invoke(type);
        if (debugMode)
        {
            Debug.Log($"OnStatusApplied invoked with: {type}");
            foreach (Delegate subscriber in currAction.GetInvocationList())
            {
                string targetName = subscriber.Target != null ? subscriber.Target.ToString() : "Static Method";
                string methodName = subscriber.Method.Name;
                Debug.Log($"OnStatusApplied subscriber: {targetName} - Method: {methodName}");
            }
        }
    }

    public void OnStatusCleansedTrigger()
    {
        var currAction = OnStatusCleansed;
        if (currAction == null) return;
        currAction.Invoke();

        if (debugMode)
        {
            foreach (Delegate subscriber in currAction.GetInvocationList())
            {
                string targetName = subscriber.Target != null ? subscriber.Target.ToString() : "Static Method";
                string methodName = subscriber.Method.Name;
                Debug.Log($"OnStatusCleansed subscriber: {targetName} - Method: {methodName}");
            }
        }
    }

    public void OnPetrifyExpiredTrigger()
    {
        Action currAction = OnPetrifyExpired;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnPetrifyExpired");
    }

    public void OnStatusReflectedTrigger()
    {
        Action currAction = OnStatusReflected;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnStatusReflected");
    }


    #endregion

    #region Summons

    public void OnSummonDealDamageTrigger()
    {
        Action currAction = OnSummonDealDamage;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnSummonDealDamage");
    }

    public void OnSummonTakeDamageTrigger()
    {
        Action currAction = OnSummonTakeDamage;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnSummonTakeDamage");
    }

    public void OnSummonSpawnedTrigger()
    {
        Action currAction = OnSummonSpawned;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnSummonSpawned");
    }

    public void OnSummonDeathTrigger()
    {
        Action currAction = OnSummonDeath;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnSummonDeath");
    }

    public void OnSummonCritTrigger()
    {
        Action currAction = OnSummonCrit;
        if (currAction == null) return;
        currAction.Invoke();
        LogSubscribers(currAction, "OnSummonCrit");
    }

    #endregion

    public void OnComboUpdatedTrigger(ComboManager combo)
    {
        var currAction = OnComboUpdated;
        if (currAction == null) return;
        currAction.Invoke(combo);

        if (debugMode)
            Debug.Log($"OnComboUpdated invoked with combo: {combo}");
    }

    public void OnComboExpiredTrigger()
    {
        var currAction = OnComboExpired;
        if (currAction == null) return;
        currAction.Invoke();

        if (debugMode)
            Debug.Log($"OnComoboExpired invoked with combo reset back to 0");
    }


    private void LogSubscribers(Action action, string eventName)
    {
        if (!debugMode) return;

        foreach (Delegate subscriber in action.GetInvocationList())
        {
            string targetName = subscriber.Target != null ? subscriber.Target.GetType().Name : "Static Method";
            string methodName = subscriber.Method.Name;

            Debug.Log($"{eventName} subscriber: {targetName} - Method: {methodName}");
        }
    }
}