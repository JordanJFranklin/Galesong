using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(Collider))]
public class PressureFieldSpell : MonoBehaviour
{
    public float tickInterval = 1f; // time between each damage tick
    private float tickTimer;

    [HideInInspector] public float scalefactor;
    public float totalLifeTime;
    [HideInInspector]public float currentLifeTime;
    public Attack pressureDamage;

    private Spell spell;
    private Collider fieldCollider;

    private Vector3 baseScaleField;

    void Start()
    {
        spell = GetComponent<Spell>();

        GetSpellRange();

        baseScaleField = transform.localScale;

        totalLifeTime = spell.GetTotalDuration();
        currentLifeTime = totalLifeTime;

        fieldCollider = GetComponent<Collider>();

        tickTimer = tickInterval;

        AdjustHeightToGround();
    }

    void Update()
    {
        currentLifeTime -= Time.deltaTime;
        tickTimer -= Time.deltaTime;

        if (currentLifeTime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (tickTimer > 0f) return;

        if (!other.TryGetComponent(out CharacterStats targetStats)) return;
        if (targetStats == spell.source) return;

        targetStats.DealDamage(pressureDamage);
        targetStats.ApplyStatusEffect(pressureDamage.StatusEffects[0]);
        spell.comboManager.OnSuccessfulHit();
    }


    private void SetScale()
    {
        transform.localScale = new Vector3(baseScaleField.x + (baseScaleField.x * scalefactor), baseScaleField.y + (baseScaleField.y * scalefactor), baseScaleField.z + (baseScaleField.z * scalefactor));
    }

    /// <summary>
    /// Adjusts the position of the spell so the bottom of the cylinder touches the ground.
    /// </summary>
    private void AdjustHeightToGround()
    {
        SetScale(); // First scale

        if (TryGetComponent(out MeshRenderer renderer))
        {
            float height = renderer.bounds.size.y;
            Vector3 pos = transform.position;
            transform.position = new Vector3(pos.x, pos.y + height / 2f, pos.z);
        }
        else if (TryGetComponent(out CapsuleCollider capsule))
        {
            float height = capsule.height * transform.localScale.y;
            Vector3 pos = transform.position;
            transform.position = new Vector3(pos.x, pos.y + height / 2f, pos.z);
        }
        else
        {
            Debug.LogWarning("PressureFieldSpell: No MeshRenderer or CapsuleCollider found for height adjustment.");
        }
    }

    public void GetSpellRange()
    {
        scalefactor = spell.source.GetStatValue(StatType.AttackRangeBonus);
    }

}
