using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RisingGaleSpell : MonoBehaviour
{
    public float scalefactor = 1f;
    public float knockUpSpeed = 25f;          // Speed to lift enemies (units/second)
    public float HoverDuration = 1f;          // Flexible hover duration (set in Inspector)
    public Attack risingGaleDamage;
    public LayerMask enemyMask;

    [Header("Visual Feedback")]
    public GameObject hoverEffectPrefab;      // Visual effect during hover phase
    public float hoverEffectYOffset = 0.2f;   // Position above enemy

    private Spell spell;
    private Collider fieldCollider;
    private Vector3 baseScaleField;
    private float cylinderTopY;               // Top position of the cylinder
    private float cylinderHeight;             // Height of the cylinder

    // Track affected enemies
    private HashSet<Rigidbody> affectedBodies = new HashSet<Rigidbody>();
    private Dictionary<Rigidbody, bool> originalGravityStates = new Dictionary<Rigidbody, bool>();
    private Dictionary<Rigidbody, GameObject> hoverEffects = new Dictionary<Rigidbody, GameObject>();

    void Start()
    {
        spell = GetComponent<Spell>();
        GetSpellRange();
        baseScaleField = transform.localScale;
        fieldCollider = GetComponent<Collider>();

        AdjustHeightToGround(); // Sets cylinderHeight and cylinderTopY

        float maxLiftTime = cylinderHeight / knockUpSpeed;
        Destroy(gameObject, maxLiftTime + HoverDuration + 1f);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Handle physics
        if (other.TryGetComponent(out Rigidbody rb) && !affectedBodies.Contains(rb))
        {
            affectedBodies.Add(rb);
            originalGravityStates[rb] = rb.useGravity;
            StartCoroutine(LiftToTopAndHover(rb));
        }

        // Handle damage
        if (other.TryGetComponent(out CharacterStats target))
        {
            if (target.currEntity == EntityType.Enemy || target.currEntity == EntityType.Obstacle)
            {
                target.DealDamage(risingGaleDamage);

                if (target.currEntity == EntityType.Enemy &&
                    risingGaleDamage.StatusEffects != null &&
                    risingGaleDamage.StatusEffects.Count > 0)
                {
                    target.ApplyStatusEffect(risingGaleDamage.StatusEffects[0]);
                }
            }
        }
    }

    private IEnumerator LiftToTopAndHover(Rigidbody rb)
    {
        rb.useGravity = false;
        Vector3 startPos = rb.position;

        // Calculate target position at cylinder top
        float targetY = cylinderTopY;
        Vector3 targetPos = new Vector3(startPos.x, targetY, startPos.z);

        // Lift to top
        float distance = Mathf.Abs(targetY - startPos.y);
        float liftDuration = distance / knockUpSpeed;

        float elapsed = 0f;
        while (elapsed < liftDuration)
        {
            rb.MovePosition(Vector3.Lerp(startPos, targetPos, elapsed / liftDuration));
            elapsed += Time.deltaTime;
            yield return null;
        }
        rb.MovePosition(targetPos);

        // START HOVER PHASE (with visual effect)
        if (hoverEffectPrefab)
        {
            Vector3 effectPos = targetPos + Vector3.up * hoverEffectYOffset;
            GameObject effect = Instantiate(hoverEffectPrefab, effectPos, Quaternion.identity);
            effect.transform.SetParent(rb.transform); // Move with enemy
            hoverEffects[rb] = effect;
        }

        // Hover for configured duration
        yield return new WaitForSeconds(HoverDuration);

        // Clean up hover effect
        if (hoverEffects.TryGetValue(rb, out GameObject fx))
        {
            Destroy(fx);
            hoverEffects.Remove(rb);
        }

        // End hover state
        if (affectedBodies.Contains(rb))
            RestoreGravity(rb);
    }

    private void AdjustHeightToGround()
    {
        SetScale();

        if (TryGetComponent(out MeshRenderer renderer))
        {
            cylinderHeight = renderer.bounds.size.y;
            Vector3 pos = transform.position;
            transform.position = new Vector3(pos.x, pos.y + cylinderHeight / 2f, pos.z);
            cylinderTopY = transform.position.y + cylinderHeight / 2f;
        }
        else if (TryGetComponent(out CapsuleCollider capsule))
        {
            cylinderHeight = capsule.height * transform.localScale.y;
            Vector3 pos = transform.position;
            transform.position = new Vector3(pos.x, pos.y + cylinderHeight / 2f, pos.z);
            cylinderTopY = transform.position.y + cylinderHeight / 2f;
        }
        else
        {
            Debug.LogWarning("Height adjustment failed - using default height");
            cylinderHeight = 5f;
            cylinderTopY = transform.position.y + 2.5f;
        }
    }

    private void RestoreGravity(Rigidbody rb)
    {
        if (affectedBodies.Contains(rb))
        {
            rb.useGravity = originalGravityStates.TryGetValue(rb, out bool gravity) ? gravity : true;
            affectedBodies.Remove(rb);
            originalGravityStates.Remove(rb);

            // Cleanup any lingering hover effect
            if (hoverEffects.TryGetValue(rb, out GameObject fx))
            {
                Destroy(fx);
                hoverEffects.Remove(rb);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out Rigidbody rb) && affectedBodies.Contains(rb))
        {
            RestoreGravity(rb);
        }
    }


    private void OnDestroy()
    {
        foreach (Rigidbody rb in affectedBodies)
        {
            if (rb != null)
            {
                rb.useGravity = originalGravityStates.TryGetValue(rb, out bool gravity) ? gravity : true;

                if (hoverEffects.TryGetValue(rb, out GameObject fx))
                    Destroy(fx);
            }
        }

        affectedBodies.Clear();
        originalGravityStates.Clear();
        hoverEffects.Clear();
    }

    private void SetScale()
    {
        transform.localScale = baseScaleField * (1 + scalefactor);
    }

    public void GetSpellRange()
    {
        scalefactor = spell.source.GetStatValue(StatType.AttackRangeBonus);
    }
}