using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Base Properties")]
    public float projectileSpeed = 15f;
    public float lifetime = 5f;
    public bool canTrack = false;
    public float trackingRange = 10f;

    [Header("Explode Settings")]
    public bool canExplode = false;
    public float explosionRadius = 5f;
    public LayerMask enemyLayer;

    [Header("Behavior")]
    public bool canPierce = false;
    public int maxPierceCount = 1;
    public bool explodesOnImpact = false;
    public List<string> tags = new List<string>();

    [Header("Effects")]
    public List<StatusEffect> additionalEffects = new List<StatusEffect>();

    private int piercedTargets = 0;
    private float lifeTimer = 0f;
    private Attack attackData;
    private Transform currentTarget;
    public CharacterStats source;
    private ComboManager comboManager;
    private Vector3 lastDirection;

    private bool hasLockedOnOnce = false;
    private Rigidbody rb;

    // Rotation speed in degrees per second for smooth turning toward target
    private float rotationSpeed = 720f;

    private Vector3 storedVelocity;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.isKinematic = true; // not using physics movement

        lastDirection = transform.forward;
    }

    public void Launch(Attack attack, CharacterStats sourceEntity, ComboManager comboM, bool Track = false, float trackDist = 0, bool Pierce = false, int pierceMax = 1, bool Explode = false, int explodeRad = 5)
    {
        attackData = attack;
        comboManager = comboM;
        source = sourceEntity;
        lastDirection = transform.forward; // Direction at launch

        // Set tracking parameters
        canTrack = Track;
        trackingRange = trackDist;

        // Set piercing parameters
        canPierce = Pierce;
        piercedTargets = 0; // Reset pierce count on launch
        maxPierceCount = pierceMax;

        // Set explosion parameters
        canExplode = Explode;
        explosionRadius = explodeRad;

        Vector3 playerVelocity = PlayerDriver.Instance.physicsProperties.vel;

        float playerSpeedInFacingDirection = Vector3.Dot(playerVelocity, transform.forward);
        float totalSpeed = projectileSpeed + playerSpeedInFacingDirection;

        storedVelocity = transform.forward * totalSpeed;

        Debug.Log($"[Projectile] Launch | Speed: {totalSpeed} | Velocity: {storedVelocity} | Player: {playerVelocity}");

        Destroy(gameObject, lifetime);
    }

    void MoveProjectile()
    {
        transform.position += storedVelocity * Time.fixedDeltaTime;
    }

    private void FixedUpdate()
    {
        MoveProjectile();

        if (canTrack)
        {
            if (currentTarget == null || !IsTargetValid(currentTarget))
            {
                currentTarget = FindClosestEnemy();
                hasLockedOnOnce = currentTarget != null;
            }

            if (currentTarget != null)
            {
                // Smoothly rotate towards the target
                Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
                Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                // Update stored velocity toward current forward direction
                lastDirection = transform.forward;
            }

            storedVelocity = lastDirection * projectileSpeed;
        }
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }
    }

    private bool IsTargetValid(Transform target)
    {
        if (target == null) return false;

        // Optional: check if target is alive (you can customize this as needed)
        CharacterStats cs = target.GetComponent<CharacterStats>();
        if (cs == null || cs.IsDead()) return false;

        // Check if target is still in tracking range
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist > trackingRange) return false;

        return true;
    }

    private Transform FindClosestEnemy()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, trackingRange);
        float closestDist = float.MaxValue;
        Transform closest = null;

        foreach (var hit in hits)
        {
            CharacterStats target = hit.GetComponent<CharacterStats>();

            if (target != null && target != source && target.currEntity != source.currEntity)
            {
                float dist = Vector3.Distance(transform.position, target.transform.position);
                if (dist < closestDist)
                {
                    closest = target.transform;
                    closestDist = dist;
                }
            }
        }

        return closest;
    }

    public void SetDirection(Vector3 direction)
    {
        lastDirection = direction.normalized;
        transform.forward = lastDirection;
    }

    /// <summary>
    /// Launch the projectile with power scaling and damage scaling.
    /// </summary>
    /// <param name="power">Normalized power [0..1]</param>
    /// <param name="stats">Reference to character stats</param>
    /// <param name="attack">Attack data</param>
    /// <param name="spell">Spell cast step</param>
    public void LaunchWithPower(float power, CharacterStats stats, Attack attack, SpellCastStep spell)
    {
        float normalizedPower = Mathf.Clamp01(power);

        float baseAttack = stats.GetStatValue(StatType.AttackDamage);
        float spellBonus = stats.GetStatValue(StatType.SpellDamageBonus);

        float baseDamage = baseAttack + (baseAttack * spellBonus); // Apply bonus as multiplier
        float scaledMultiplier = Mathf.Lerp(spell.minMultiplier, spell.maxMultiplier, normalizedPower);

        // Setup the attack data with proper scaling
        attackData = new Attack
        {
            BaseDamage = baseDamage,
            DamageMultiplier = scaledMultiplier,
            CritChance = attack.CritChance,
            CritMultiplier = attack.CritMultiplier,
            StatusEffects = new List<StatusEffect>(attack.StatusEffects),
            AttackTypes = new List<AttackType>(attack.AttackTypes),
            IgnoreDefense = attack.IgnoreDefense,
            Sender = attack.Sender
        };

        Launch(attackData, stats, GetComponent<ComboManager>());
    }

    void OnTriggerEnter(Collider other)
    {
        // Check for wall or default layer and destroy immediately
        int layer = other.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Wall") || layer == LayerMask.NameToLayer("Default"))
        {
            Destroy(gameObject);
            return;
        }

        // === A. Handle HaliMoon Collision ===
        if (other.TryGetComponent<HaliMoon>(out HaliMoon moon))
        {
            moon.gameObject.GetComponent<LockOnTargetHelper>().enabled = false;
            
            if (moon.wasHitByProjectile)
                return;

            moon.wasHitByProjectile = true;
            moon.TriggerMoonReturn(destroyOnReturn: true);
            Destroy(gameObject);
            return;
        }

        // === B. Handle HaliDrop Hit ===
        if (other.TryGetComponent<HaliDrop>(out HaliDrop drop))
        {
            Vector3 hitDir = (other.transform.position - transform.position).normalized;
            drop.DamageHali(1, hitDir);
            Destroy(gameObject);
            return;
        }

        // === C. Character Damage ===
        CharacterStats hitEntity = other.GetComponent<CharacterStats>();

        if (hitEntity == null || source == null)
            return;

        if (hitEntity == source)
            return;

        if ((hitEntity.currEntity == EntityType.Player && source.currEntity == EntityType.Summon) ||
            (hitEntity.currEntity == EntityType.Summon && source.currEntity == EntityType.Player))
            return;

        hitEntity.DealDamage(attackData);
        comboManager.OnSuccessfulHit();

        foreach (var effect in additionalEffects)
        {
            hitEntity.ApplyStatusEffect(effect);
        }

        if (explodesOnImpact)
        {
            Explode();
            return;
        }

        if (canPierce)
        {
            piercedTargets++;
            if (piercedTargets >= maxPierceCount)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }



    void OnCollisionEnter(Collision col)
    {
        // Destroy if hitting Default or Wall layer, regardless of damage logic
        int layer = col.gameObject.layer;
        if (layer == LayerMask.NameToLayer("Wall") || layer == LayerMask.NameToLayer("Default"))
        {
            Destroy(gameObject);
            return;
        }

        CharacterStats hitEntity = col.transform.GetComponent<CharacterStats>();

        // Prevent friendly fire
        if (hitEntity == null || hitEntity == source ||
            (hitEntity.currEntity == EntityType.Player && source.currEntity == EntityType.Summon) ||
            (hitEntity.currEntity == EntityType.Summon && source.currEntity == EntityType.Player))
            return;

        hitEntity.DealDamage(attackData);
        TryGetComponent<ComboManager>(out ComboManager combomgr);
        combomgr.OnSuccessfulHit();

        foreach (var effect in additionalEffects)
        {
            hitEntity.ApplyStatusEffect(effect);
        }

        if (explodesOnImpact)
        {
            Explode();
            return;
        }

        if (canPierce)
        {
            piercedTargets++;
            if (piercedTargets >= maxPierceCount)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Explode()
    {
        Debug.Log("Projectile exploded");

        Collider[] hits = Physics.OverlapSphere(transform.position, explosionRadius, enemyLayer);

        foreach (var hit in hits)
        {
            CharacterStats enemy = hit.GetComponent<CharacterStats>();

            if (enemy != null && enemy != source && enemy.currEntity != source.currEntity && TryGetComponent<ComboManager>(out ComboManager combomgr))
            {
                enemy.DealDamage(attackData);
                combomgr.OnSuccessfulHit();
            }
        }

        Destroy(gameObject);
    }
}
