using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Spell : MonoBehaviour
{
    [Header("Spell")]
    public string spellName;
    public float spellRange;
    public float spellDuration;
    
    [HideInInspector]public float currlifetime = 10f;

    public CharacterStats source;
    public ComboManager comboManager;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.isKinematic = true; // not using physics movement
    }

    public void Initialize(ComboManager comboMgr, CharacterStats stats, float lifetime, float range, string name)
    {
        comboManager = comboMgr;
        source = stats;
        spellName = name;
        spellDuration = lifetime;
        spellRange = range;
    }

    public float GetTotalDuration()
    {
       return spellDuration + (spellDuration * source.GetStatValue(StatType.SpellDurationBonus));
    }

    public float GetTotalRange()
    {
        return spellRange + (spellRange * source.GetStatValue(StatType.AttackRangeBonus));
    }

    public string GetName()
    {
        return spellName;
    }

    public void OnSpellEnd()
    {
        if (comboManager != null)
        {
            comboManager.ClearPersistentSpell(spellName);

        }
    }
}