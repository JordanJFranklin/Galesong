using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CardDropEntry
{
    public float dropChance;
    public Cards card;
}

[System.Serializable]
public class CardDropPool
{
    [Header("Drop Settings")]
    public float basePoolChance = 25f;
    public int minCardDropAmount = 1;
    public int maxCardDropAmount = 3;
    public List<CardDropEntry> cards;
}

[System.Serializable]
public class GuaranteedCardDrop
{
    public Cards card;
}

[CreateAssetMenu(menuName = "Cards/World Card Drop Pool")]
public class WorldDropPool : ScriptableObject
{
    public string islandName;
    public List<CardDropEntry> globalCards;
}

public class CardDropper : MonoBehaviour
{
    [Header("Debug Options")]
    public bool rollCards = false;
    public int comboCount;

    [Header("Card Drops")]
    public List<GuaranteedCardDrop> guaranteedCardDrops;
    public WorldDropPool WorldPool;
    public CardDropPool UniqueEntityPool;

    public GameObject dropPrefab;

    private void Start()
    {
        if (LevelCardDropPool.Instance != null)
        {
            WorldPool = LevelCardDropPool.Instance.LevelPool;
        }
        else
        {
            Debug.LogWarning("[CardDropper] No LevelCardDropPool.Instance found!");
        }
    }

    private void Update()
    {
        if (rollCards)
        {
            RollCards();
            rollCards = false;
        }
    }

    public void RollCards()
    {
        int comboTier = GetComboTier(comboCount);

        // Bonus chance scales with combo tier
        float bonusChance = comboCount * 0.25f;

        // Increase max card drops per tier (+1 max card per tier)
        int tierBonusMaxDrops = comboTier;

        List<Cards> droppedCards = new();

        DropGuaranteedCards(droppedCards);

        TryDropFromWorldPool(WorldPool, bonusChance, tierBonusMaxDrops, droppedCards);
        TryDropFromPool(UniqueEntityPool, bonusChance, tierBonusMaxDrops, droppedCards);

        if (droppedCards.Count == 0)
        {
            Debug.Log("[CardDropper] No cards dropped this roll.");
            return;
        }

        // Summary Log
        Dictionary<string, int> dropSummary = new();
        foreach (var card in droppedCards)
        {
            string name = card.name;
            dropSummary[name] = dropSummary.ContainsKey(name) ? dropSummary[name] + 1 : 1;
        }

        Debug.Log("[CardDropper] Dropped cards:");
        foreach (var kvp in dropSummary)
        {
            Debug.Log($"  {kvp.Key} x{kvp.Value}");
        }

        // Find highest tier from dropped cards
        CardTiers highestTier = CardTiers.Tactic;
        foreach (var card in droppedCards)
        {
            if ((int)card.GetCardTier() > (int)highestTier)
                highestTier = card.GetCardTier();
        }

        // Spawn visual drop bundle
        if (dropPrefab == null)
        {
            Debug.LogWarning("[CardDropper] Drop prefab not assigned.");
            return;
        }

        GameObject bundleDrop = Instantiate(dropPrefab, transform.position, Quaternion.identity);
        if (bundleDrop.TryGetComponent<CardDropBundle>(out var bundle))
        {
            bundle.Initialize(droppedCards);
            bundle.SetVisualEffect(highestTier);
        }
    }

    private int GetComboTier(int comboCount)
    {
        // Define combo tiers by combo count thresholds, adjust as needed
        if (comboCount >= 20) return 3;
        if (comboCount >= 10) return 2;
        if (comboCount >= 5) return 1;
        return 0;
    }

    private void DropGuaranteedCards(List<Cards> result)
    {
        foreach (var drop in guaranteedCardDrops)
        {
            if (drop.card != null)
                result.Add(drop.card);
        }
    }

    // Modified to accept tier bonus to max drops
    private void TryDropFromPool(CardDropPool pool, float bonusChance, int tierBonusMaxDrops, List<Cards> result)
    {
        if (pool == null || pool.cards == null || pool.cards.Count == 0)
            return;

        float roll = Random.Range(0f, 100f);
        if (roll > pool.basePoolChance + bonusChance)
            return;

        // Increase maxCardDropAmount by tierBonusMaxDrops
        int maxDrops = pool.maxCardDropAmount + tierBonusMaxDrops;
        int amount = Random.Range(pool.minCardDropAmount, maxDrops + 1);

        for (int i = 0; i < amount; i++)
        {
            var card = RollSingleCard(pool.cards, bonusChance);
            if (card != null)
                result.Add(card);
        }
    }

    // Same as above but for world pool
    private void TryDropFromWorldPool(WorldDropPool worldPool, float bonusChance, int tierBonusMaxDrops, List<Cards> result)
    {
        if (worldPool == null || worldPool.globalCards == null || worldPool.globalCards.Count == 0)
            return;

        float roll = Random.Range(0f, 100f);
        float baseChance = 30f;

        if (roll > baseChance + bonusChance)
            return;

        // Increase max drops for world pool by tier bonus
        int amount = Random.Range(1, 3 + tierBonusMaxDrops);

        for (int i = 0; i < amount; i++)
        {
            var card = RollSingleCard(worldPool.globalCards, bonusChance);
            if (card != null)
                result.Add(card);
        }
    }

    private Cards RollSingleCard(List<CardDropEntry> entries, float bonusChance)
    {
        foreach (var entry in entries)
        {
            if (entry.card == null) continue;

            float chance = entry.dropChance + bonusChance;
            if (Random.Range(0f, 100f) <= chance)
                return entry.card;
        }
        return null;
    }
}

