using UnityEngine;

using System.Collections;

using System.Collections.Generic;
using UnityEngine.UIElements;

public class HaliDrop : MonoBehaviour
{
    [Header("Hali Settings")]
    public DropType dropType;
    public int value;

    [Header("Hali Sprite Settings")]
    public bool isMovingHaliSprite;
    public GameObject wispDropPrefab;
    public float dropInterval = 1f;
    private float dropTimer;
    public float currSpriteMoveDuration;
    public float maxSpriteMoveDuration = 10;
    public float turnSpeed = 360f;
    public float haliSpriteSpeed = 25f;
    public float spriteDetectionDist = 30f;
    public int maxWispsToDrop = 5;
    private int wispsDropped = 0;
    private bool fleeingAfterMaxDrop = false;
    private float fleeDestroyTimer = 0f;
    public float destroyAfterNoDropsTime = 7f;

    [Header("Hali Soul Settings")]
    [SerializeField] private float baseFleeSpeed = 5f;
    [SerializeField] private float extraSpeedPerMissingHealth = 2f; // how much faster at low health (e.g., 2 means up to 3x)
    [SerializeField] private int initialSoulHitCount = 5; // the starting soulHitCount
    public int soulHitCount = 0;
    public float fleeSpeed = 8f;
    public float expireAfterHitTime = 10f;
    public float burstSpriteLifetime = 7f;
    public GameObject soulWispDropPrefab;
    public GameObject soulBurstSpritePrefab;
    private bool hitOnce = false;
    private float soulExpireTimer = 0f;

    [Header("Hali Celeste Settings")]
    public int maxHP = 5;
    public int currentHP;
    public float celesteMoveSpeed = 0f;
    public int celesteHealth = 5;
    public float orbitRadius = 2.5f;
    public float orbitSpeed = 30f;
    public GameObject burstCurrencyPrefab;
    public GameObject haliMoonPrefab;  // Ensure this is assigned in inspector
    public int burstCurrencyCount = 15;
    public float baseHoverHeight = 1.0f;
    public float moonFloatHeight = 0.3f;
    public float moonFloatSpeed = 2f;
    private float fireCooldown = 3f;
    private float fireTimer = 0f;
    public float hoverHeight = 5f;          // Distance above ground
    public float groundCheckDistance = 10f; // How far to look down
    public float wallCheckDistance = 2f;    // How close before stopping


    private Queue<int> moonsToRespawn = new();
    [SerializeField] public LayerMask playerLayerMask;
    private bool moonsInitialized = false;
    [HideInInspector] public bool pendingDeath = false;
    [HideInInspector] public bool burstTriggered = false;
    [HideInInspector] public HaliMoon finalReturningMoon;

    public List<HaliMoon> orbitingMoons = new();
    public bool celesteBurstTriggered = false;
    private float orbitAngleOffset = 0f;
    public float celesteBurstTimer = 0f;

    [Header("Seeking Settings")]
    public bool isSeekingPlayer = false;
    private float seekTimer = 0f;
    public float baseSeekSpeed = 5f;
    public float maxSeekSpeed = 75f;
    public float seekAcceleration = 10f;

    [Header("Model Prefabs")]
    public GameObject wispModelPrefab;
    public GameObject spriteModelPrefab;
    public GameObject soulModelPrefab;
    public GameObject celesteModelPrefab;

    [Header("Floating Animation")]
    public float floatSpeed = 2f;
    public float floatHeight = 0.2f;

    [Header("Idle Rotation")]
    public float rotationSpeed = 30f;
    public Vector3 rotationAxis = Vector3.up;

    private Transform visualModel;
    private LockOnTargetHelper lockonpoint;
    private Rigidbody rb;
    public CharacterStats stats;
    public float GetOrbitAngleOffset() => orbitAngleOffset;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        SpawnModel();

        switch (dropType)
        {
            case DropType.HaliSoul:
                soulHitCount = UnityEngine.Random.Range(3, 10);
                initialSoulHitCount = soulHitCount;
                break;

            case DropType.HaliCeleste:
                currentHP = celesteHealth;
                ResetMoonsToHealth();  // Spawn all moons initially here
                break;
        }
    }


    private void Update()
    {
        var player = PlayerDriver.Instance;
        if (player == null) return;

        if (isSeekingPlayer)
        {
            seekTimer += Time.deltaTime;
            float currentSpeed = Mathf.Min(baseSeekSpeed + seekAcceleration * seekTimer, maxSeekSpeed);

            Vector3 direction = (player.transform.position - transform.position).normalized;
            transform.position += direction * currentSpeed * Time.deltaTime;
            transform.rotation = Quaternion.LookRotation(direction);

            float distToPlayer = Vector3.Distance(transform.position, player.transform.position);
            if (distToPlayer <= 3f)
            {
                Collect();
            }
        }

        HaliWisp();
        HaliSoul();
        HaliCeleste();

        if (dropType == DropType.HaliCeleste)
        {
            // Handle moon firing cooldown
            fireTimer += Time.deltaTime;
            if (fireTimer >= fireCooldown && orbitingMoons.Count > 0)
            {
                FireMoonAtPlayer();
                fireTimer = 0f;
            }
        }
    }



    private void FixedUpdate()
    {
        HaliSprite();
    }

    public void Collect()
    {
        ProgressionInv.Instance.GainCurrency(value);
        Destroy(gameObject);
    }

    public void StartSeeking()
    {
        GetComponent<DropPhysicsController>().applyGravity = false;
        isSeekingPlayer = true;
        seekTimer = 0f;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = true;
            var collider = GetComponent<CapsuleCollider>();
            if (collider != null)
                collider.isTrigger = true;
        }
    }

    public void SetHali(DropType type)
    {
        int randomValue = 0;

        switch (type)
        {
            case DropType.HaliWisp:
                randomValue = UnityEngine.Random.Range(1, 5);
                break;
            case DropType.HaliSprite:
                currentHP = maxHP;
                randomValue = UnityEngine.Random.Range(10, 50);
                break;
            case DropType.HaliSoul:
                randomValue = UnityEngine.Random.Range(75, 150);
                break;
            case DropType.HaliCeleste:
                randomValue = UnityEngine.Random.Range(200, 300);
                break;
            case DropType.HaliMoon:
                randomValue = 1;
                break;
        }

        dropType = type;
        value = randomValue;
    }

    private void SpawnModel()
    {
        // Destroy any existing child models
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }

        // Pick model prefab
        GameObject prefabToUse = dropType switch
        {
            DropType.HaliWisp => wispModelPrefab,
            DropType.HaliSprite => spriteModelPrefab,
            DropType.HaliSoul => soulModelPrefab,
            DropType.HaliCeleste => celesteModelPrefab,
            DropType.HaliMoon => celesteModelPrefab,
            _ => null
        };

        // Spawn main visual model
        if (prefabToUse != null)
        {
            GameObject model = Instantiate(prefabToUse, transform);
            model.transform.localPosition = Vector3.zero;
            visualModel = model.transform;
            Destroy(gameObject, 100); // maybe optional?
        }

        // Only Celeste uses moons
        if (dropType == DropType.HaliCeleste && !moonsInitialized)
        {
            moonsInitialized = true;
            GetComponent<Rigidbody>().useGravity = false;
            GetComponent<DropPhysicsController>().enabled = false;

            if (!TryGetComponent<LockOnTargetHelper>(out lockonpoint))
                lockonpoint = gameObject.AddComponent<LockOnTargetHelper>();

            orbitingMoons.Clear();

            for (int i = 0; i < currentHP; i++)
            {
                SpawnMoon(i);
            }
        }

        if (dropType == DropType.HaliSoul && !TryGetComponent<LockOnTargetHelper>(out lockonpoint))
        {
            lockonpoint = gameObject.AddComponent<LockOnTargetHelper>();
            soulHitCount = UnityEngine.Random.Range(3, 6);
        }

    }


    void HaliWisp()
    {
        if (visualModel != null && dropType == DropType.HaliWisp)
        {
            float offsetY = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            visualModel.localPosition = new Vector3(0f, offsetY, 0f);
            visualModel.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
        }
    }

    private bool canDropWisp = true;

    void HaliSprite()
    {
        if (dropType != DropType.HaliSprite) return;

        PlayerDriver player = PlayerDriver.Instance;
        if (player == null) return;

        Vector3 toPlayer = player.transform.position - transform.position;
        Vector3 fleeDirection = -toPlayer.normalized;

        currSpriteMoveDuration = Mathf.Clamp(currSpriteMoveDuration, 0, maxSpriteMoveDuration);

        if (!isMovingHaliSprite)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            if (distance <= spriteDetectionDist)
            {
                StartHaliSprite();
            }
        }

        if (isMovingHaliSprite)
        {
            currSpriteMoveDuration -= Time.deltaTime;

            if (fleeDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(fleeDirection);
                targetRotation.x = 0;
                targetRotation.z = 0;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
            }

            float moveSpeed = fleeingAfterMaxDrop ? haliSpriteSpeed * 2f : haliSpriteSpeed;
            transform.position += transform.forward * moveSpeed * Time.deltaTime;

            if (!fleeingAfterMaxDrop)
            {
                dropTimer += Time.deltaTime;
                if (dropTimer >= dropInterval && canDropWisp)
                {
                    DropWisp();
                    dropTimer = 0f;
                    canDropWisp = false;
                    StartCoroutine(ResetDropCooldown());
                }
            }

            if (wispsDropped >= maxWispsToDrop && !fleeingAfterMaxDrop)
            {
                fleeingAfterMaxDrop = true;
                value *= 2;
            }

            if (fleeingAfterMaxDrop)
            {
                fleeDestroyTimer += Time.deltaTime;
                if (fleeDestroyTimer >= destroyAfterNoDropsTime)
                {
                    Destroy(gameObject);
                }
            }

            if (currSpriteMoveDuration <= 0)
            {
                isMovingHaliSprite = false;
            }
        }
    }

    private IEnumerator ResetDropCooldown()
    {
        yield return new WaitForSeconds(dropInterval);
        canDropWisp = true;
    }

    void DropWisp()
    {
        if (wispDropPrefab == null) return;

        Vector3 dropOffset = UnityEngine.Random.insideUnitSphere * 0.3f;
        dropOffset.y = Mathf.Abs(dropOffset.y);
        Vector3 dropPosition = transform.position + dropOffset;

        GameObject wisp = Instantiate(wispDropPrefab, dropPosition, Quaternion.identity);

        Rigidbody rb = wisp.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = wisp.AddComponent<Rigidbody>();
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        if (wisp.TryGetComponent<HaliDrop>(out HaliDrop drop))
        {
            drop.SetHali(DropType.HaliWisp);
            drop.gameObject.GetComponent<Rigidbody>().isKinematic = false;
            wispsDropped++;
        }
        else
        {
            Debug.LogWarning("Spawned wispDropPrefab does not have a HaliDrop component!");
        }
    }

    public void StartHaliSprite()
    {
        currSpriteMoveDuration = maxSpriteMoveDuration;
        isMovingHaliSprite = true;
    }

    void HaliSoul()
    {
        if (dropType != DropType.HaliSoul) return;

        if (!TryGetComponent<LockOnTargetHelper>(out LockOnTargetHelper lockOn))
        {
            visualModel.gameObject.AddComponent<LockOnTargetHelper>();
        }

        PlayerDriver player = PlayerDriver.Instance;
        if (player == null) return;

        // Ensure we have a Rigidbody for physics-aware movement
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.isKinematic = false;
        rb.linearDamping = 1f; // tweak to taste
        rb.angularDamping = 1f;

        // Compute health fraction based on hit counts (lower health -> higher speed)
        float healthFraction = Mathf.Clamp01((float)soulHitCount / Mathf.Max(1, initialSoulHitCount));
        float speedMultiplier = 1f + (1f - healthFraction) * extraSpeedPerMissingHealth;
        float scaledFleeSpeed = baseFleeSpeed * speedMultiplier;

        Vector3 toPlayer = player.transform.position - transform.position;
        Vector3 fleeDir = -toPlayer.normalized;
        fleeDir.z = 0; // flatten on Z if needed (note: Unity uses Y-up; if you meant remove vertical, you'd zero y)

        // Rotate toward flee direction (only yaw)
        Quaternion targetRotation = Quaternion.LookRotation(fleeDir, Vector3.up);
        Vector3 targetEuler = targetRotation.eulerAngles;
        targetRotation = Quaternion.Euler(0f, targetEuler.y, 0f);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);

        // Apply movement via Rigidbody so collisions work
        Vector3 desiredVelocity = fleeDir * scaledFleeSpeed;
        rb.linearVelocity = new Vector3(desiredVelocity.x, rb.linearVelocity.y, desiredVelocity.z);
    }

    public void DamageHali(int amount, Vector3 hitDirection)
    {
        if (dropType != DropType.HaliSoul) return;

        soulHitCount -= 1;

        int wispsToDrop = UnityEngine.Random.Range(3, 5);
        for (int i = 0; i < wispsToDrop; i++)
        {
            // Random offset spawn position
            Vector3 offset = UnityEngine.Random.insideUnitSphere * 0.3f;
            GameObject wisp = Instantiate(soulWispDropPrefab, transform.position + offset, Quaternion.identity);

            if (wisp.TryGetComponent<HaliDrop>(out HaliDrop w))
            {
                w.SetHali(DropType.HaliWisp);
            }

            Rigidbody rb = wisp.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = wisp.AddComponent<Rigidbody>();
            }

            rb.isKinematic = false;
            rb.useGravity = true;
            rb.mass = 0.5f;
            rb.linearDamping = 0.5f;
            rb.angularDamping = 0.8f;

            // Push in varied directions around the soul: mostly radial with a slight upward bias
            Vector3 randomDirection = UnityEngine.Random.onUnitSphere;
            randomDirection.y = Mathf.Clamp(randomDirection.y, 0.1f, 0.4f); // give slight upward tilt
            randomDirection = randomDirection.normalized;

            float forceMagnitude = UnityEngine.Random.Range(10f, 30f);
            rb.AddForce(randomDirection * forceMagnitude, ForceMode.Impulse);

            // Add a little random spin for visual variety
            Vector3 randomTorque = UnityEngine.Random.insideUnitSphere * 5f;
            rb.AddTorque(randomTorque, ForceMode.Impulse);
        }

        if (soulHitCount <= 0)
        {
            BurstSoul();
        }
    }

    void BurstSoul()
    {
        int numSprites = 8;
        float angleStep = 360f / numSprites;

        for (int i = 0; i < numSprites; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));

            GameObject sprite = Instantiate(soulBurstSpritePrefab, transform.position, Quaternion.LookRotation(dir));
            if (sprite.TryGetComponent<HaliDrop>(out HaliDrop h))
            {
                h.SetHali(DropType.HaliWisp);

                // Slight offset so it appears to emit
                h.transform.position += dir * 0.5f;

                // Ensure it has a Rigidbody and fling it outward
                Rigidbody rb = sprite.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    rb = sprite.AddComponent<Rigidbody>();
                }

                rb.isKinematic = false;
                rb.useGravity = false; // optional: set true if you want it to arc
                rb.mass = 0.3f;
                rb.linearDamping = 0.2f;

                float burstForce = 20f;
                rb.AddForce(dir.normalized * burstForce, ForceMode.Impulse);

                Destroy(sprite, burstSpriteLifetime);
            }
            else
            {
                // Still destroy if no HaliDrop component
                Destroy(sprite, burstSpriteLifetime);
            }
        }

        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (dropType == DropType.HaliSprite && fleeingAfterMaxDrop && !isSeekingPlayer)
        {
            if (other.CompareTag("Wall"))
            {
                Destroy(gameObject);
            }
        }
    }

    public void ResetMoonsToHealth()
    {
        foreach (var moon in orbitingMoons)
            if (moon != null)
                Destroy(moon.gameObject);

        orbitingMoons.Clear();

        for (int i = 0; i < currentHP; i++)
            SpawnMoon(i);
    }


    private void HaliCeleste()
    {
        if (dropType != DropType.HaliCeleste) return;

        PlayerDriver player = PlayerDriver.Instance;
        if (player == null) return;

        // Ensure lock-on helper exists
        if (!TryGetComponent<LockOnTargetHelper>(out _))
        {
            visualModel?.gameObject.AddComponent<LockOnTargetHelper>();
        }

        // Handle burst countdown
        if (celesteBurstTriggered)
        {
            celesteBurstTimer += Time.deltaTime;
            if (celesteBurstTimer >= 1f)
            {
                BurstCeleste();
            }
            return; // Skip movement if bursting
        }

        // Floating animation for visual model
        if (visualModel != null)
        {
            float offsetY = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            visualModel.localPosition = new Vector3(0f, offsetY, 0f);
            visualModel.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
        }

        // Flee from player
        Vector3 toPlayer = player.transform.position - transform.position;
        Vector3 fleeDirection = -toPlayer.normalized;

        if (fleeDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(fleeDirection, Vector3.up);
            Vector3 euler = targetRotation.eulerAngles;
            Quaternion yOnlyRotation = Quaternion.Euler(0, euler.y, 0);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, yOnlyRotation, turnSpeed * Time.deltaTime);
        }

        // Movement check using OverlapSphere instead of raycast
        Vector3 forward = transform.forward;
        float moveDistance = celesteMoveSpeed * Time.deltaTime;
        Vector3 nextPosition = transform.position + forward * moveDistance;

        float collisionRadius = orbitRadius + 1f; // Includes moon orbit radius
        Collider[] hits = Physics.OverlapSphere(nextPosition, collisionRadius, LayerMask.GetMask("Wall", "Default"));

        if (hits.Length == 0)
        {
            transform.position = nextPosition;
        }

        // Maintain high float over ground
        float groundRayDistance = 50f;
        float desiredHeightAboveGround = 6f;

        if (Physics.Raycast(transform.position + Vector3.up * 2f, Vector3.down, out RaycastHit groundHit, groundRayDistance, LayerMask.GetMask("Default", "Ground")))
        {
            Vector3 pos = transform.position;
            pos.y = groundHit.point.y + desiredHeightAboveGround;
            transform.position = Vector3.Lerp(transform.position, pos, 5f * Time.deltaTime);
        }

        // Orbiting logic
        orbitAngleOffset = Mathf.Repeat(orbitAngleOffset + orbitSpeed * Time.deltaTime, 360f);

        int moonCount = orbitingMoons.Count;
        int divisor = Mathf.Max(currentHP, 1); 

        for (int i = 0; i < moonCount; i++)
        {
            HaliMoon moon = orbitingMoons[i];
            if (moon == null) continue;

            if (!moon.isRearranging)
            {
                int moonIndex = moon.GetMoonIndex();
                float angle = orbitAngleOffset + (moonIndex * 360f / divisor);
                Vector3 offset = Quaternion.Euler(0, angle, 0) * Vector3.forward * orbitRadius;

                float yOffset = Mathf.Sin(Time.time * moonFloatSpeed + moonIndex) * moonFloatHeight;
                Vector3 targetPosition = transform.position + offset + new Vector3(0, yOffset, 0);

                moon.transform.position = Vector3.Lerp(
                    moon.transform.position,
                    targetPosition,
                    5f * Time.deltaTime
                );
            }

            moon.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
        }

        orbitingMoons.RemoveAll(m => m == null);

        if (orbitingMoons.Count == 0 && pendingDeath && !celesteBurstTriggered)
        {
            celesteBurstTriggered = true;
            celesteBurstTimer = 0f;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (dropType != DropType.HaliCeleste) return;

        // Show the collision check radius for next movement
        Gizmos.color = Color.cyan;
        Vector3 forward = transform.forward;
        float moveDistance = celesteMoveSpeed * Time.deltaTime;
        Vector3 nextPos = Application.isPlaying ? transform.position + forward * moveDistance : transform.position + forward;

        float radius = orbitRadius + 1f;
        Gizmos.DrawWireSphere(nextPos, radius);
    }



    public void BurstCeleste()
    {
        foreach (var moon in orbitingMoons)
        {
            if (moon != null)
            {
                Destroy(moon.gameObject);
            }
        }

        orbitingMoons.Clear();

        for (int i = 0; i < burstCurrencyCount; i++)
        {
            Vector3 offset = Random.insideUnitSphere * 2f;
            offset.y = Mathf.Abs(offset.y);
            GameObject currency = Instantiate(burstCurrencyPrefab, transform.position + offset, Quaternion.identity);
            currency.GetComponent<Rigidbody>().useGravity = true;
            currency.GetComponent<Rigidbody>().isKinematic = false;

            if (currency.TryGetComponent<HaliDrop>(out HaliDrop drop))
            {
                drop.SetHali(DropType.HaliWisp);
            }
        }

        int numSprites = Random.Range(4, 8);
        for (int i = 0; i < numSprites; i++)
        {
            Vector3 dir = Random.onUnitSphere;
            dir.y = Mathf.Abs(dir.y);
            GameObject sprite = Instantiate(soulBurstSpritePrefab, transform.position, Quaternion.LookRotation(dir));
            sprite.GetComponent<Rigidbody>().useGravity = true;

            if (sprite.TryGetComponent<HaliDrop>(out HaliDrop h))
            {
                h.SetHali(DropType.HaliSprite);
                h.StartHaliSprite();
                Destroy(sprite, burstSpriteLifetime);
            }
        }

        Destroy(gameObject);
    }

    public void RearrangeMoons()
    {
        StartCoroutine(RearrangeMoonsCoroutine());
    }

    private IEnumerator RearrangeMoonsCoroutine()
    {
        orbitingMoons.RemoveAll(m => m == null);
        int moonCount = orbitingMoons.Count;
        if (moonCount == 0) yield break;

        // Make a copy to avoid issues if orbitingMoons changes
        List<HaliMoon> moonsCopy = new List<HaliMoon>(orbitingMoons);

        // Store current positions
        Dictionary<HaliMoon, Vector3> startPositions = new();
        foreach (var moon in moonsCopy)
        {
            if (moon != null && !startPositions.ContainsKey(moon))
                startPositions.Add(moon, moon.transform.position);
        }

        // Calculate target positions
        Dictionary<HaliMoon, Vector3> targetPositions = new();
        float angleStep = 360f / moonCount;

        for (int i = 0; i < moonCount; i++)
        {
            HaliMoon moon = moonsCopy[i];
            if (moon == null) continue;

            Vector3 offset = new Vector3(
                Mathf.Cos(angleStep * i * Mathf.Deg2Rad),
                0,
                Mathf.Sin(angleStep * i * Mathf.Deg2Rad)
            ) * orbitRadius;

            Vector3 targetPos = transform.position + offset;

            if (!targetPositions.ContainsKey(moon))
                targetPositions.Add(moon, targetPos);
        }

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

            foreach (var moon in moonsCopy)
            {
                if (moon == null) continue;

                moon.isRearranging = true;

                if (startPositions.TryGetValue(moon, out Vector3 start) &&
                    targetPositions.TryGetValue(moon, out Vector3 target))
                {
                    moon.transform.position = Vector3.Lerp(start, target, t);
                }
                else
                {
                    Debug.LogWarning($"[RearrangeMoons] Missing position data for moon: {moon.name}");
                }
            }

            yield return null;
        }

        // Finalize positions and assign new orbit indexes
        for (int i = 0; i < moonCount; i++)
        {
            HaliMoon moon = moonsCopy[i];
            if (moon == null) continue;

            if (targetPositions.TryGetValue(moon, out Vector3 finalTarget))
            {
                moon.transform.position = finalTarget;
            }

            moon.SetMoonIndex(i);
            moon.isRearranging = false;
        }
    }





    public void DamageCeleste(GameObject attacker = null)
    {
        if (attacker == null || attacker.GetComponent<Projectile>() == null) return;

        currentHP = Mathf.Max(currentHP - 1, 0);

        if (orbitingMoons.Count > 0)
        {
            RemoveMoon(orbitingMoons[0]);
            OrbitCountMatchesHealth();
        }

        if (orbitingMoons.Count == 0)
        {
            celesteBurstTriggered = true;
            celesteBurstTimer = 0f;
        }
        else
        {
            FireMoonAtPosition(attacker.transform.position);
        }
    }

    private void FireMoonAtPlayer()
    {
        Debug.Log("Firing moon at player!");

        CharacterStats targetPlayer = FindClosestPlayer(transform.position, 60, playerLayerMask);

        if (targetPlayer == null || orbitingMoons.Count == 0) return;

        Vector3 direction = (targetPlayer.transform.position - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);

        // Line of sight check
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance, LayerMask.GetMask("Wall", "Default")))
        {
            Debug.Log("Blocked by wall, not firing.");
            return; // Wall in the way
        }

        HaliMoon moon = orbitingMoons[0];
        orbitingMoons.RemoveAt(0);

        if (moon == null) return;

        moon.GetComponent<SphereCollider>().enabled = true;
        moon.FireAt(targetPlayer.transform.position);
    }

    private void OnDestroy()
    {
        foreach(HaliMoon moon in orbitingMoons)
        {
            if (moon != null)
            {
                Destroy(moon);
            }
        }
    }


    private void FireMoonAtPosition(Vector3 targetPos)
    {
        if (orbitingMoons.Count == 0) return;

        Vector3 direction = (targetPos - transform.position).normalized;
        float distance = Vector3.Distance(transform.position, targetPos);

        // Line of sight check
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit, distance, LayerMask.GetMask("Wall", "Default")))
        {
            Debug.Log("Blocked by wall, not firing.");
            return; // Blocked by something
        }

        HaliMoon moon = orbitingMoons[0];
        orbitingMoons.RemoveAt(0);

        if (moon == null) return;

        moon.GetComponent<LockOnTargetHelper>().enabled = true;
        moon.FireAt(targetPos);
    }


    private CharacterStats FindClosestPlayer(Vector3 position, float maxDistance, LayerMask playerLayerMask)
    {
        Collider[] hits = Physics.OverlapSphere(position, maxDistance, playerLayerMask);

        CharacterStats closest = null;
        float minDist = maxDistance;

        foreach (Collider hit in hits)
        {
            CharacterStats playerStats = hit.GetComponent<CharacterStats>();
            if (playerStats != null && playerStats.currEntity == EntityType.Player)
            {
                float dist = Vector3.Distance(position, playerStats.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = playerStats;
                }
            }
        }
        return closest;
    }

    public void NotifyMoonUsed(int moonIndex)
    {
        currentHP = Mathf.Max(currentHP - 1, 0);

        HaliMoon moonToRemove = orbitingMoons.Find(m => m.GetMoonIndex() == moonIndex);
        if (moonToRemove != null)
        {
            orbitingMoons.Remove(moonToRemove);
            Destroy(moonToRemove.gameObject);
        }

        OrbitCountMatchesHealth();
    }

    public void RemoveMoon(HaliMoon moon)
    {
        if (orbitingMoons.Contains(moon))
            orbitingMoons.Remove(moon);

        RearrangeMoons();
    }

    public void OrbitCountMatchesHealth()
    {
        while (orbitingMoons.Count > currentHP)
        {
            HaliMoon extraMoon = orbitingMoons[orbitingMoons.Count - 1];
            orbitingMoons.RemoveAt(orbitingMoons.Count - 1);
            if (extraMoon != null)
                Destroy(extraMoon.gameObject);
        }
    }

    public void SpawnMoon(int index)
    {
        GameObject moonObj = Instantiate(haliMoonPrefab, transform.position, Quaternion.identity);
        HaliMoon moon = moonObj.GetComponent<HaliMoon>();
        moon.Initialize(this, index, currentHP, orbitRadius);
        orbitingMoons.Add(moon);

        RearrangeMoons();  // Call this after spawning to update positions & indexes
    }

    public void NotifyMoonHitByPlayer(HaliMoon moon)
    {
        if (orbitingMoons.Contains(moon) && !moon.wasHitByProjectile)
        {
            moon.GetComponent<LockOnTargetHelper>().enabled = false;
            moon.wasHitByProjectile = true;
            moon.isFired = false;

            // Optionally disable its collider so it can’t be hit again mid-return
            Collider col = moon.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    public void NotifyMoonReturned(HaliMoon moon)
    {
        if (moon == null) return;

        if (moon.wasHitByProjectile)
        {
            // CHANGED: Always reduce health when hit moon returns
            currentHP = Mathf.Max(currentHP - 1, 0);

            if (currentHP <= 0)
            {
                pendingDeath = true;
                finalReturningMoon = moon;
            }

            orbitingMoons.Remove(moon);
            Destroy(moon.gameObject);
            RearrangeMoons();

            // CHANGED: Trigger burst if no moons left
            if (orbitingMoons.Count == 0 && pendingDeath && !celesteBurstTriggered)
            {
                celesteBurstTriggered = true;
                celesteBurstTimer = 0f;
            }
        }
        else
        {
            moon.ResetMoon();
            if (!orbitingMoons.Contains(moon))
                orbitingMoons.Add(moon);
            RearrangeMoons();
        }
    }
}