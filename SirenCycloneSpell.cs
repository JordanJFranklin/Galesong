using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SirenCycloneSpell : MonoBehaviour
{
    public bool drawDebugGizmos;

    public Attack CycloneFinalBurstAttack;
    public Attack CycloneDamagePerTick;

    [Header("Ground Alignment")]
    public float groundCheckDistance = 10f;
    public float alignHeightOffset = 0.25f;
    public float groundAlignSpeed = 10f;
    public float rotationAlignSpeed = 5f;
    public LayerMask groundLayer;

    [Header("Wall Detection")]
    public float wallStopCheckDistance = 1.5f;
    public LayerMask wallLayer;

    [Header("Cyclone Settings")]
    public bool hasStopped = false;
    public float moveSpeed = 5f;
    public float duration = 4f;
    public float tickInterval = 0.5f;
    public float orbitStrength = 15f;
    public float maxVelocity = 25f;
    public float pullStrength = 20f;
    public float floatStrength = 10f;
    public float colradius = 7f;
    public float radius = 7f;
    public Vector3 OffsetSuctionOrigin;

    public float explosionRadius = 15f;
    public float explosionForce = 700f;

    public LayerMask stopLayer;
    public LayerMask damageableLayer;
    public GameObject impactEffect;

    private Vector3 moveDirection;
    private float tickTimer;
    private float elapsedTime;
    private bool isGrounded = true;

    private HashSet<GameObject> damagedEntities = new HashSet<GameObject>();
    private HashSet<Rigidbody> floatingBodies = new HashSet<Rigidbody>();
    private Spell spell;

    private void Start()
    {
        spell = GetComponent<Spell>();
        moveDirection = transform.forward;
    }

    private void Update()
    {
        elapsedTime += Time.deltaTime;
        tickTimer += Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if(!hasStopped)
        {
            transform.position += moveDirection * moveSpeed * Time.deltaTime;
        }
        
        ApplyPull();

        if (tickTimer >= tickInterval)
        {
            Collider[] targets = Physics.OverlapSphere(transform.position, radius + (radius * spell.source.GetStatValue(StatType.AttackRangeBonus)), damageableLayer);

            foreach (var target in targets)
            {
                if (target.TryGetComponent<CharacterStats>(out CharacterStats entity) && entity.currEntity.Equals(EntityTarget.Enemy))
                {
                    entity.DealDamage(CycloneDamagePerTick);
                }

                tickTimer = 0f;
            }
        }

        AlignToGround();
        CheckForCollision();

        if (elapsedTime >= duration)
        {
            EndCyclone();
        }
    }

    private void AlignToGround()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * 2f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayer))
        {
            // Position: stay just above ground
            Vector3 targetPosition = new Vector3(transform.position.x, hit.point.y + alignHeightOffset, transform.position.z);
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * groundAlignSpeed);

            // Rotation: align to terrain normal
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationAlignSpeed);

            // Optional: reorient moveDirection to remain aligned to forward
            moveDirection = transform.forward;
        }
    }

    private void CheckForCollision()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position + OffsetSuctionOrigin, colradius, stopLayer);

        foreach (Collider hit in hits)
        {
            // Wall detection — check for penetration
            if (Physics.ComputePenetration(GetComponent<Collider>(), transform.position, transform.rotation, hit, hit.transform.position, hit.transform.rotation, out _, out float distance) && distance > 0f)
            {
                Debug.Log("[Cyclone] Hit wall: " + hit.name);
                StopCyclone();
                return;
            }
        }
    }

    private void StopCyclone()
    {
        hasStopped = true;
    }

    private void ApplyPull()
    {
        Collider[] targets = Physics.OverlapSphere(transform.position, radius + (radius * spell.source.GetStatValue(StatType.AttackRangeBonus)), damageableLayer);
        HashSet<Rigidbody> currentFrameBodies = new HashSet<Rigidbody>();

        foreach (var target in targets)
        {
            if (target.TryGetComponent<Rigidbody>(out Rigidbody col))
            {
                Rigidbody rb = target.attachedRigidbody;

                currentFrameBodies.Add(rb);
                if (!floatingBodies.Contains(rb))
                {
                    rb.useGravity = false;
                    floatingBodies.Add(rb);
                }

                ApplyForce(rb);
            }

            if (target.TryGetComponent<CharacterStats>(out CharacterStats stats))
            {
                stats.DealDamage(CycloneDamagePerTick);
            }
        }

        List<Rigidbody> toRemove = new List<Rigidbody>();
        foreach (var rb in floatingBodies)
        {
            if (!currentFrameBodies.Contains(rb))
            {
                rb.useGravity = true;
                toRemove.Add(rb);
            }
        }
        foreach (var rb in toRemove)
        {
            floatingBodies.Remove(rb);
        }
    }

    private void ApplyForce(Rigidbody rb)
    {
        Vector3 suctionCenter = transform.position + OffsetSuctionOrigin;
        Vector3 toCenter = suctionCenter - rb.position;
        Vector3 pullDir = toCenter.normalized;

        Vector3 orbitDir = Vector3.Cross(Vector3.up, pullDir).normalized;
        Vector3 pullForce = pullDir * pullStrength;
        Vector3 orbitForce = orbitDir * orbitStrength;
        Vector3 floatForce = Vector3.up * floatStrength;

        rb.AddForce(pullForce + orbitForce + floatForce, ForceMode.Acceleration);
        rb.linearVelocity = Vector3.ClampMagnitude(rb.linearVelocity, maxVelocity);
    }

    private void ApplyExplosionForce()
    {
        Collider[] affected = Physics.OverlapSphere(transform.position, explosionRadius + (explosionRadius * spell.source.GetStatValue(StatType.AttackRangeBonus)), damageableLayer);
        foreach (var target in affected)
        {
            if (target.attachedRigidbody != null)
            {
                target.attachedRigidbody.useGravity = true;
                target.attachedRigidbody.AddExplosionForce(explosionForce, transform.position, explosionRadius, 1f, ForceMode.Impulse);
            }

            if (target.TryGetComponent<CharacterStats>(out CharacterStats stats))
            {
                if (!stats.currEntity.Equals(EntityType.Player) && !stats.currEntity.Equals(EntityType.Summon))
                {
                    stats.DealDamage(CycloneDamagePerTick);
                }
            }
        }

        floatingBodies.Clear();
    }

    private void EndCyclone()
    {
        Vector3 cyclonePosition = transform.position;
        Collider[] targets = Physics.OverlapSphere(cyclonePosition, radius + (radius * spell.source.GetStatValue(StatType.AttackRangeBonus)), damageableLayer);

        foreach (var rb in floatingBodies)
        {
            if (rb != null)
            {
                rb.useGravity = true;
                //rb.linearVelocity = Vector3.zero;
                //rb.angularVelocity = Vector3.zero;
            }
        }

        foreach (var col in targets)
        {
            if (col == null || !col.gameObject.activeInHierarchy) continue;

            if (col.TryGetComponent<CharacterStats>(out CharacterStats stats))
            {
                if (!stats.currEntity.Equals(EntityType.Player) && !stats.currEntity.Equals(EntityType.Summon))
                {
                    stats.DealDamage(CycloneFinalBurstAttack);
                }
            }
        }

        floatingBodies.Clear();

        ApplyExplosionForce();

        if (impactEffect != null)
        {
            Instantiate(impactEffect, cyclonePosition, Quaternion.identity);
        }

        Destroy(gameObject, 1f);
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugGizmos) return;

        Gizmos.color = new Color(0.4f, 0.7f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position + OffsetSuctionOrigin, radius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + OffsetSuctionOrigin, colradius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);

        Gizmos.color = Color.green;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        Gizmos.DrawLine(rayOrigin, rayOrigin + Vector3.down * groundCheckDistance);
        Gizmos.DrawWireSphere(rayOrigin + Vector3.down * groundCheckDistance, 0.05f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.position, transform.position + moveDirection.normalized * wallStopCheckDistance);
    }
}
