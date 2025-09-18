using UnityEngine;
using System.Collections;
using TMPro;
using static UnityEngine.GraphicsBuffer;

public class HaliMoon : MonoBehaviour
{
    private HaliDrop owner;
    private Vector3 targetPos;
    public bool isFired = false;
    public bool isReturning = false;
    public bool wasHitByProjectile = false;
    private float lifeTimer = 0f;
    public float lifeDuration = 5f;
    public float fireSpeed = 20f;
    public float returnSpeed = 30f;
    public float returnThreshold = 0.5f;
    private int moonIndex = -1;
    public bool isRearranging;
    private bool destroyOnReturn;

    public void Initialize(HaliDrop owner, int index, int total, float radius)
    {
        this.owner = owner;
        isFired = false;
        isReturning = false;
        wasHitByProjectile = false;
        lifeTimer = 0f;

        float angle = index * 360f / total;
        Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * radius;
        transform.position = owner.transform.position + offset;

        // Make sure collider is enabled when initialized
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    private void FixedUpdate()
    {
        if (owner == null) return;

        if (isRearranging) return; // Skip movement during rearrangement

        if (isFired)
        {
            lifeTimer += Time.deltaTime;
            transform.position += transform.forward * fireSpeed * Time.deltaTime;

            if (lifeTimer > lifeDuration)
            {
                TriggerReturn(); // Return unharmed after lifetime
            }
        }
        else if (isReturning)
        {
            Vector3 toOwner = owner.transform.position - transform.position;
            transform.position += toOwner.normalized * returnSpeed * Time.deltaTime;

            // CHANGED: Added return distance check and moon handling
            if (toOwner.magnitude < returnThreshold)
            {
                // Final death sequence first
                if (owner.pendingDeath && owner.finalReturningMoon == this && !owner.burstTriggered)
                {
                    owner.BurstCeleste();
                    return;
                }

                // CHANGED: Always notify owner when moon returns
                owner.NotifyMoonReturned(this);
            }
        }
    }




    public void FireAt(Vector3 position)
    {
        if (isFired || isReturning) return;

        GetComponent<LockOnTargetHelper>().enabled = true;

        targetPos = position;
        transform.LookAt(position);
        isFired = true;
        lifeTimer = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isFired || isReturning) return;

        // Ignore collisions with other moons or the owner itself
        if (other.GetComponent<HaliMoon>() || other.GetComponent<HaliDrop>())
        {
            return;
        }

        // Check for walls, floors, or projectiles
        bool hitProjectile = other.TryGetComponent<Projectile>(out Projectile proj);
        if (other.CompareTag("Wall") || other.CompareTag("Floor") || hitProjectile)
        {
            if (hitProjectile && proj.source.currEntity.Equals(EntityTarget.Player))
            {
                Debug.Log("Hit Object");
                wasHitByProjectile = true;
            }

            TriggerReturn();
        }
    }

    private void TriggerReturn()
    {
        isFired = false;
        isReturning = true;
        GetComponent<LockOnTargetHelper>().enabled = false;

        // Enable collider on return phase to allow owner to detect moon arrival
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    public void TriggerMoonReturn(bool destroyOnReturn = false)
    {
        if (isReturning) return;

        isReturning = true;
        wasHitByProjectile = destroyOnReturn; // Only reduce health if triggered by projectile

        // Make sure collider is enabled for return detection
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        StartCoroutine(ReturnToCeleste());
    }

    // Coroutine to handle smooth return, optional delay or animation here
    public IEnumerator ReturnToCeleste()
    {
        Transform target = owner?.transform;
        if (target == null)
            yield break;

        // Disable physics during return
        var rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; rb.isKinematic = true; }

        while (Vector3.Distance(transform.position, target.position) > returnThreshold)
        {
            Vector3 direction = (target.position - transform.position).normalized;
            transform.position += direction * returnSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction);
            yield return null;
        }

        // Snap to avoid jitter
        transform.position = target.position;
        isReturning = false;

        if (destroyOnReturn)
        {
            if (owner != null)
                owner.NotifyMoonReturned(this);

            yield break;
        }

        // If not destroyed, re-enable physics/colliders
        if (rb != null) rb.isKinematic = false;
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }


    public void ResetMoon()
    {
        wasHitByProjectile = false;
        isRearranging = false;

        isFired = false;
        isReturning = false;

        if (TryGetComponent<Collider>(out Collider col))
            col.enabled = true;

        if (TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }



    public bool IsInFirePhase()
    {
        // True if moon is currently fired and flying outwards
        return isFired && !isReturning;
    }

    public int GetMoonIndex()
    {
        return moonIndex;
    }

    public void SetMoonIndex(int index)
    {
        moonIndex = index;
    }

}
