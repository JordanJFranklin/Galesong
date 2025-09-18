using System.Collections.Generic;
using UnityEngine;

public enum DropType
{
    Prisma,
    PrismaUltima,
    HaliWisp,
    HaliSprite,
    HaliSoul,
    HaliCeleste, 
    HaliMoon //<<< This Is Not A Drop That Is Ever Put Into The Pool <<<
}

[System.Serializable]
public class DropTypePrefabEntry
{
    public DropType dropType;
    public GameObject prefab;
}

[System.Serializable]
public class GuaranteedDrop
{
    public DropType dropType;
    public int quantity = 1;
}

[System.Serializable]
public class RandomDrop
{
    public DropType dropType;
    [Range(0f, 1f)] public float dropChance = 0.5f; // 0 = never, 1 = always
    public int minQuantity = 1;
    public int maxQuantity = 3;
}


public class ConsumableDropper : MonoBehaviour
{
    [Tooltip("Rolls Every Necessary And Drops Items")]
    public bool rollConsumables = false;

    [Header("Drop Prefab Setup")]
    public List<DropTypePrefabEntry> dropPrefabs = new();

    [Header("Guaranteed Drops")]
    public List<GuaranteedDrop> guaranteedDrops = new();

    [Header("Random Drops")]
    public List<RandomDrop> randomDrops = new();

    [Header("Drop Behavior")]
    public bool useBurstPhysics = true;
    public float burstForceMin = 2f;
    public float burstForceMax = 5f;

    private Dictionary<DropType, GameObject> _prefabLookup;

    private void Awake()
    {
        BuildPrefabDictionary();
    }

    void Update()
    {
        if (rollConsumables)
        {
            DropGuaranteedItems();
            DropRandomItems();
            rollConsumables = false;
        }
    }

    private void BuildPrefabDictionary()
    {
        _prefabLookup = new Dictionary<DropType, GameObject>();
        foreach (var entry in dropPrefabs)
        {
            if (!_prefabLookup.ContainsKey(entry.dropType))
                _prefabLookup.Add(entry.dropType, entry.prefab);
        }
    }

    /// <summary>
    /// Attempts to drop each random drop based on its chance.
    /// </summary>
    public void DropRandomItems()
    {
        foreach (var drop in randomDrops)
        {
            if (Random.value <= drop.dropChance)
            {
                int quantity = Random.Range(drop.minQuantity, drop.maxQuantity + 1);

                for (int i = 0; i < quantity; i++)
                {
                    SpawnDrop(drop.dropType, transform.position);
                }
            }
        }
    }


    /// <summary>
    /// Call this to drop all guaranteed items at the dropper’s position
    /// </summary>
    public void DropGuaranteedItems()
    {
        foreach (var drop in guaranteedDrops)
        {
            for (int i = 0; i < drop.quantity; i++)
            {
                SpawnDrop(drop.dropType, transform.position);
            }
        }
    }

    /// <summary>
    /// Spawn a specific DropType at a position with optional random burst physics
    /// </summary>
    public void SpawnDrop(DropType type, Vector3 position)
    {
        if (!_prefabLookup.TryGetValue(type, out GameObject prefab) || prefab == null)
        {
            Debug.LogWarning($"[ConsumableDropper] No prefab found for {type}");
            return;
        }

        GameObject drop = Instantiate(prefab, position, Quaternion.identity);

        // Add force
        if (useBurstPhysics && drop.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            Vector3 randomForce = Random.onUnitSphere * Random.Range(burstForceMin, burstForceMax);
            rb.AddForce(randomForce, ForceMode.Impulse);
        }

        // If it's a Hali-type drop, assign value
        if (drop.TryGetComponent<HaliDrop>(out HaliDrop hali))
        {
            int randomValue = 0;

            switch (type)
            {
                case DropType.HaliWisp:
                    hali.SetHali(DropType.HaliWisp);
                    break;
                case DropType.HaliSprite:
                    hali.SetHali(DropType.HaliSprite);
                    break;
                case DropType.HaliSoul:
                    hali.SetHali(DropType.HaliSoul);
                    break;
                case DropType.HaliCeleste:
                    hali.SetHali(DropType.HaliCeleste);
                    break;
            }
            hali.dropType = type;
        }

        if (drop.TryGetComponent<PrismaDrop>(out PrismaDrop prisma))
        {
            prisma.dropType = type;

            if (type == DropType.Prisma)
            {
                float randomSP = Random.Range(15f, 25f);
                prisma.SetPrisma(type, Mathf.RoundToInt(randomSP));
            }
            else if (type == DropType.PrismaUltima)
            {
                prisma.SetPrisma(type); // This will use the max/default amount
            }
        }


    }


    /// <summary>
    /// drop item Directly with an external method forcibly with no chance calculations 
    /// </summary>
    public static void DropItem(DropType type, Vector3 position, ConsumableDropper dropperRef)
    {
        dropperRef?.SpawnDrop(type, position);
    }
}
