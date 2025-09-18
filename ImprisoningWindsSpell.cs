using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Collider))]
public class ImprisoningWindsSpell : MonoBehaviour
{
    [Header("Jump Boost")]
    public float jumpStrength = 0.50f;
    public float jumpDuration = 3f;
    [Header("Movement")]
    public float moveSpeedBoost= 0.50f;
    public float moveSpeedBoostDuration = 3f;
    [Header("Slow Fall")]
    public float slowFallStrength = 0.50f;
    public float slowFallDuration = 3f;
    [Header("Slow")]
    public float slowStrength = 0.50f;
    public float slowDuration = 3f;
    [Header("Damage")]
    public float damageTickRate = 0.5f; // How often damage is applied in seconds
    public Attack imprisonDamage;

    private Spell spell;
    private Collider imprisonCollider;

    // Track players already buffed to prevent constant reapplication
    private CharacterStats lastBuffTarget;

    private float currentLifetime;
    private float damageTickTimer;

    void Start()
    {
        spell = GetComponent<Spell>();
        currentLifetime = spell.GetTotalDuration();
        imprisonCollider = GetComponent<Collider>();
        imprisonCollider.isTrigger = true;
        damageTickTimer = 0f;
        lastBuffTarget = null;

        AdjustHeightToGround();
    }

    void Update()
    {
        currentLifetime -= Time.deltaTime;
        damageTickTimer += Time.deltaTime;

        if (currentLifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Only use Stay for damage ticks, buffs are handled in Enter
        if (damageTickTimer < damageTickRate) return;

        if (other.gameObject.TryGetComponent(out CharacterStats targetStats))
        {
            ApplyDamage(targetStats);
            damageTickTimer = 0f;
        }

        HandleTriggerEffect(other, true);
    }

    private void HandleTriggerEffect(Collider other, bool isEnter)
    {
        if (!other.gameObject.TryGetComponent(out CharacterStats targetStats))
            return;

        switch (targetStats.currEntity)
        {
            case EntityType.Player:
                if (isEnter)
                {
                    // Apply buffs to the player who entered, not the caster
                    ApplyPlayerBuffs(targetStats);
                    lastBuffTarget = targetStats;
                }
                break;

            case EntityType.Enemy:
                // Apply slow effect immediately
                targetStats.ApplySlowed(slowStrength, slowDuration, "Imprisoning Winds Slowed");
                break;
        }
    }

    private void ApplyPlayerBuffs(CharacterStats playerStats)
    {
        playerStats.ApplyJumpBoost(jumpStrength, jumpDuration, "Imprisoning Winds Jump Boost");
        playerStats.ApplySlowFall(slowFallStrength, slowFallDuration, "Imprisoning Winds Slow Fall");
        playerStats.ApplyMovementBoost(moveSpeedBoost, moveSpeedBoostDuration, "Imprisoning Winds Move Speed");
        Debug.Log("Buffs applied to player: " + playerStats.gameObject.name);
    }

    private void ApplyDamage(CharacterStats targetStats)
    {
        if (targetStats.currEntity == EntityType.Enemy)
        {
            targetStats.DealDamage(imprisonDamage);
            spell.comboManager.OnSuccessfulHit();
        }
    }

    private void AdjustHeightToGround()
    {
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
            Debug.LogWarning("ImprisoningWindsSpell: No MeshRenderer or CapsuleCollider found for height adjustment.");
        }
    }
}
