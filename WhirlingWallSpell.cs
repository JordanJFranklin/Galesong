using UnityEngine;

public class WhirlingWallSpell : MonoBehaviour
{
    [Header("Whirling Wall")]
    public bool drawRayGizmo = true;
    public Attack WhirlingWallImpactDamage;
    public float speed = 3f;
    public float scalefactor = 0f;
    public float totalLifeTime;
    [HideInInspector] public float currentLifeTime;
    public float blastForce = 10f;
    public float blastRadius = 10f;
    public float groundSnapRange = 2f;
    public LayerMask groundLayer;

    [Header("Wind Bolt Settings")]
    public int numberOfBolts = 3;
    public float angleSpread = 30f; // Degrees total between leftmost and rightmost bolt
    public GameObject windBoltPrefab;
    public Attack windBoltAttack;
    public LayerMask enemyMask;

    private bool isMoving = true;

    private Spell spell;
    private Collider fieldCollider;

    private Vector3 baseScaleField;

    void Start()
    {
        spell = GetComponent<Spell>();

        baseScaleField = transform.localScale;

        GetSpellRange();

        totalLifeTime = spell.GetTotalDuration();
        currentLifeTime = totalLifeTime;

        fieldCollider = GetComponent<Collider>();

        SetScale();
    }

    void FixedUpdate()
    {
        if (isMoving)
        {
            MoveAndAdjustHeight();
        }
            
    }

    private void Update()
    {
        currentLifeTime -= Time.deltaTime;

        if (currentLifeTime <= 0f)
        {
            DestroyWall();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (enemyMask == (enemyMask | (1 << other.gameObject.layer)))
        {
            StopMovement();

            if (other.TryGetComponent(out Rigidbody rb))
            {
                // Use wall's forward direction to blast enemies in front of it
                Vector3 blastDirection = transform.forward;
                rb.AddForce(blastDirection * blastForce, ForceMode.Impulse);

                if (other.TryGetComponent(out CharacterStats enemy))
                {
                    enemy.DealDamage(WhirlingWallImpactDamage);
                }
            }
        }

        if (other.TryGetComponent(out Projectile proj) && !proj.tags.Contains("WindBolt"))
        {
            if (proj.source.currEntity.Equals(EntityType.Player) || proj.source.currEntity.Equals(EntityType.Summon))
            {
                OnAllyProjectilePassThrough(transform.forward);
            }
        }
    }

    public void OnAllyProjectilePassThrough(Vector3 direction)
    {
        if (fieldCollider == null) return;

        Bounds bounds = fieldCollider.bounds;
        float minY = bounds.center.y + (bounds.extents.y * 0.25f); 
        float maxY = bounds.max.y;

        for (int i = 0; i < numberOfBolts; i++)
        {
            Vector3 randomPosition = new Vector3(
                Random.Range(bounds.min.x, bounds.max.x),
                Random.Range(minY, maxY),
                Random.Range(bounds.min.z, bounds.max.z)
            );

            Vector3 dir = direction.normalized;
            GameObject projGO = Instantiate(windBoltPrefab, randomPosition, Quaternion.LookRotation(dir));

            Projectile proj = projGO.GetComponent<Projectile>();
            if (proj != null)
            {
                proj.Launch(windBoltAttack, spell.source, spell.comboManager, Track:true, trackDist: 50);
            }
        }
    }
    private void SetScale()
    {
        transform.localScale = new Vector3(baseScaleField.x + (baseScaleField.x * scalefactor), baseScaleField.y + (baseScaleField.y * scalefactor), baseScaleField.z);
    }
    public void GetSpellRange()
    {
        scalefactor = spell.source.GetStatValue(StatType.AttackRangeBonus);
    }
    void DestroyWall()
    {
        Destroy(gameObject);
    }
    public void OnRecast()
    {
        if (isMoving)
        {
            StopMovement();
        }
    }
    public void StopMovement()
    {
        isMoving = false;

        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
        }

        currentLifeTime = totalLifeTime;
    }
    private void OnDestroy()
    {
        spell.OnSpellEnd();
    }

    private void MoveAndAdjustHeight()
    {
        // Move forward
        transform.position += transform.forward * speed * Time.deltaTime;

        // Cast ray downward from current position
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundSnapRange + (groundSnapRange * spell.source.GetStatValue(StatType.AttackRangeBonus)), groundLayer))
        {
            Vector3 pos = transform.position;
            float objectHeight = GetObjectHeight() / 2f;
            transform.position = new Vector3(pos.x, hit.point.y + objectHeight, pos.z);
        }
        else
        {
            // Optional: fallback to renderer/collider bounds
            AdjustHeightToOwnBounds();
        }
    }

    private float GetObjectHeight()
    {
        if (TryGetComponent(out MeshRenderer renderer))
        {
            return renderer.bounds.size.y;
        }
        else if (TryGetComponent(out CapsuleCollider capsule))
        {
            return capsule.height * transform.localScale.y;
        }
        else if (TryGetComponent(out BoxCollider box))
        {
            return box.size.y * transform.localScale.y;
        }

        Debug.LogWarning("No suitable component found for height. Using fallback height of 1.");
        return 1f;
    }

    private void AdjustHeightToOwnBounds()
    {
        float height = GetObjectHeight();
        Vector3 pos = transform.position;
        transform.position = new Vector3(pos.x, pos.y + height / 2f, pos.z);
    }

    private void OnDrawGizmos()
    {
        if (!drawRayGizmo) return;

        Gizmos.color = Color.green;
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        Vector3 end = origin + Vector3.down * groundSnapRange;
        Gizmos.DrawLine(origin, end);
        Gizmos.DrawSphere(end, 0.05f);
    }
}
