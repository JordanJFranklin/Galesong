using Ink;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Enum representing the rarity tier of a card.
/// </summary>
public enum CardTiers { Tactic, Monster, Holographic }

[CreateAssetMenu(fileName = "CardLibrary", menuName = "Cards/Card Library")]
public class CardLibrary : ScriptableObject
{
    public List<Cards> AllCards;
}

[System.Serializable]
public class CardStatEffect
{
    public StatType Type;
    public StatModifier Modifier;

    public CardStatEffect(StatType type, StatModifier modifier)
    {
        Type = type;
        Modifier = modifier;
    }
}


/// <summary>
/// Represents an individual card with its stats, cost, and tier information.
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/New Card")]
[System.Serializable]
public class Cards : ScriptableObject
{
    [SerializeField] private string cardName = "";
    [SerializeField] private int count = 1;
    [SerializeField] private int cardPointValue = 0;
    [SerializeField] private int buyValue = 1;
    [SerializeField] private int sellValue = 0;
    [SerializeField] private CardTiers CardTier;
    [SerializeField] private List<CardStatEffect> cardStatEffects = new List<CardStatEffect>();
    [SerializeField] private GameObject UniqueEffect;

    /// <summary>
    /// Constructs a new card with the provided attributes.
    /// </summary>
    public Cards(string name, int count, int cost, CardTiers tier, List<CardStatEffect> stats, GameObject effect)
    {
        cardName = name;
        cardPointValue = count;
        buyValue = cost;
        sellValue = cost / 2;
        CardTier = tier;
        cardStatEffects = stats;
        UniqueEffect = effect;
    }

    

    public bool IsCardInInv(Cards card, List<Cards> cardInv)
    {
        for(int i = 0; i < cardInv.Count; i++)
        {
            if (cardInv[i].GetCardName().Equals(card.GetCardName()))
            {
                return true;
            }
        }

        return false;
    }

    public Cards GetCard(Cards card, List<Cards> cardInv)
    {
        for(int i = 0; i < cardInv.Count;i++)
        {

            if(card.GetCardName() ==  cardInv[i].GetCardName())
            {
                return cardInv[i];
            }
        }

        Debug.Log($"{card} as not found in the inventory");
        return null;
    }

    public void AddCard(Cards card)
    {
        count += 1;
        Debug.Log($"Added (1) {cardName} card(s)");
    }

    public void SubtractCard(Cards card, int amountofCardsToSubtract)
    {
        count -= amountofCardsToSubtract;
        Debug.Log($"Subtracted ({amountofCardsToSubtract}) {cardName} card(s)");
    }

    public int GetCardCount()
    {
        return count;
    }

    /// <summary>
    /// Intiate Card
    /// </summary>
    public void OnValidate()
    {
        sellValue = buyValue / 2;
    }

    /// <summary>
    /// Calculates and sets the card's sell value to half of the buy value.
    /// </summary>
    public void SetSellValue()
    {
        sellValue = buyValue / 2;
    }

    /// <summary>Returns the name of the card.</summary>
    public string GetCardName() => cardName;

    /// <summary>Returns the card's purchase cost.</summary>
    public int GetBuyValue() => buyValue;

    /// <summary>Returns the card's sell value.</summary>
    public int GetSellValue() => sellValue;

    /// <summary>Returns how many points this card contributes to a deck.</summary>
    public int GetCardPointValue() => cardPointValue;

    /// <summary>Returns the card's tier classification.</summary>
    public CardTiers GetCardTier() => CardTier;

    /// <summary>Returns the stat modifiers or effects this card grants.</summary>
    public List<CardStatEffect> GetCardEffects() => cardStatEffects;

    /// <summary>Returns the unique visual or gameplay effect associated with the card.</summary>
    public GameObject GetUnique() => UniqueEffect;
}

/// <summary>
/// Represents a player's deck data, including available points and owned/equipped cards.
/// </summary>
[System.Serializable]
public class CardDeck
{
    [Header("Card Settings")]
    [Tooltip("Current total used card points.")]
    public int CurrentDeckPoints = 0;

    [Tooltip("Maximum allowed card points (base + bonus).")]
    public int TotalDeckPoints = 0;

    [Tooltip("Base points available before any bonuses.")]
    public int BaseDeckPoints = 10;

    [Tooltip("Number of Leviathan Pearls earned.")]
    public int LeviathanPearl = 0;

    [Tooltip("Points granted per Leviathan Pearl.")]
    public int LeviathanPearlPointBonus = 10;

    [Header("Card Inventory")]
    [Tooltip("All cards owned by the player.")]
    public List<Cards> DeckInventory = new List<Cards>();

    [Tooltip("Currently equipped cards in use.")]
    public List<Cards> EquippedCards = new List<Cards>();
}



/// <summary>
/// Manages the player's card inventory, including buying, selling, and equipping cards.
/// Also handles deck point calculations and progression interactions.
/// </summary>
public class CardManager : MonoBehaviour
{

    [Tooltip("If true, allows cards to be equipped.")]
    public bool canEquipCards;

    [Tooltip("Container for inventory and deck point information.")]
    public CardDeck CardDeck;

    public CardLibrary cardLibrary;
    private CharacterStats stats;
    private ProgressionInv progression;

    void Start()
    {
        stats = GetComponent<CharacterStats>();
        progression = GetComponent<ProgressionInv>();
        SetAllSellValues();

        CardDeck.TotalDeckPoints = CardDeck.BaseDeckPoints + (CardDeck.LeviathanPearl * CardDeck.LeviathanPearlPointBonus);
        CardDeck.CurrentDeckPoints = CardDeck.TotalDeckPoints;        
    }

    /// <summary>
    /// Updates the sell value of every card in inventory and equipped list.
    /// </summary>
    public void SetAllSellValues()
    {
        foreach (Cards card in CardDeck.DeckInventory)
        {
            card.SetSellValue();
        }

        foreach (Cards card in CardDeck.EquippedCards)
        {
            card.SetSellValue();
        }
    }

    /// <summary>
    /// Increases the number of Leviathan Pearls owned.
    /// </summary>
    public void AddLeviathenPearl(int amount)
    {
        CardDeck.LeviathanPearl += amount;
    }

    /// <summary>
    /// Decreases the number of Leviathan Pearls owned.
    /// </summary>
    public void SubtractLeviathenPearl(int amount)
    {
        CardDeck.LeviathanPearl -= amount;
    }

    /// <summary>
    /// Recalculates total available deck points and current point usage.
    /// Warns if an equipped card exceeds available points.
    /// </summary>
    public void CalculateCurrentDeckPoints()
    {
        CardDeck.TotalDeckPoints = CardDeck.BaseDeckPoints + (CardDeck.LeviathanPearl * CardDeck.LeviathanPearlPointBonus);

        int usedPoints = 0;
        foreach (var card in CardDeck.EquippedCards)
        {
            usedPoints += card.GetCardPointValue();
        }

        CardDeck.CurrentDeckPoints = CardDeck.TotalDeckPoints - usedPoints;

        if (CardDeck.CurrentDeckPoints < 0)
        {
            Debug.LogWarning("You have exceeded your total deck points. Some cards may need to be unequipped.");
        }
    }


    /// <summary>
    /// Attempts to purchase a card using Hali currency. Adds it to inventory if successful.
    /// </summary>
    public void EquipCard(Cards card)
    {
        Cards ownedCard = CardDeck.DeckInventory.Find(c => c.GetCardName() == card.GetCardName());
        if (ownedCard == null)
        {
            Debug.Log($"{card.GetCardName()} is not in your inventory.");
            return;
        }

        if (CardDeck.EquippedCards.Contains(ownedCard))
        {
            Debug.Log($"{card.GetCardName()} is already Equipped.");
            return;
        }

        int cardCost = card.GetCardPointValue();
        if (cardCost > CardDeck.CurrentDeckPoints)
        {
            Debug.Log($"{card.GetCardName()} [{cardCost}] exceeds your remaining deck points [{CardDeck.CurrentDeckPoints}].");
            return;
        }

        CardDeck.EquippedCards.Add(ownedCard);
        CalculateCurrentDeckPoints();

        // Apply card stats to player
        string cardKey = $"card_{card.GetCardName()}";
        foreach (var effect in ownedCard.GetCardEffects())
        {
            stats.AddModifierToStat(effect.Type, effect.Modifier, cardKey);
        }

        Debug.Log($"{card.GetCardName()} [{cardCost}] Equipped. Remaining: {CardDeck.CurrentDeckPoints}");
    }

    /// <summary>
    /// Unequips the specified card and recalculates deck points.
    /// </summary>
    public void UnequipCard(Cards card)
    {
        if (!CardDeck.EquippedCards.Contains(card))
        {
            Debug.Log($"{card.GetCardName()} is not equipped.");
            return;
        }

        CardDeck.EquippedCards.Remove(card);
        CalculateCurrentDeckPoints();

        // Remove card stats from player
        string cardKey = $"card_{card.GetCardName()}";
        foreach (var effect in card.GetCardEffects())
        {
            stats.RemoveModifierFromStat(effect.Type, cardKey);
        }

        Debug.Log($"{card.GetCardName()} unequipped. Remaining points: {CardDeck.CurrentDeckPoints}");
    }



    /// <summary>
    /// Adds a dropped card to the player’s inventory.
    /// If already owned, increases its count instead of duplicating it.
    /// </summary>
    public void GainDropCard(Cards card)
    {
        // Look for an existing card in inventory by name
        Cards existing = CardDeck.DeckInventory.Find(c => c.GetCardName() == card.GetCardName());

        if (existing != null)
        {
            // Add to existing stack
            existing.AddCard(existing);
            Debug.Log($"You Obtained a Duplicate [{card.GetCardTier()}] {card.GetCardName()}! (+1 stack)");
        }
        else
        {
            // Add new entry to inventory
            CardDeck.DeckInventory.Add(card);
            Debug.Log($"You Obtained a New [{card.GetCardTier()}] {card.GetCardName()}!");
        }
    }


    /// <summary>
    /// Sells a card and grants the player currency.
    /// </summary>
    public void SellCard(Cards card, int currency, int amountToSell)
    {
        Cards existing = CardDeck.DeckInventory.Find(c => c.GetCardName() == card.GetCardName());

        if (existing == null)
        {
            Debug.Log($"You do not own {card.GetCardName()}.");
            return;
        }

        if (CardDeck.EquippedCards.Contains(card) && existing.GetCardCount() <= amountToSell)
        {
            Debug.Log($"You cannot sell {card.GetCardName()} — it is equipped or you are selling too many.");
            return;
        }

        if (amountToSell <= 0 || existing.GetCardCount() < amountToSell)
        {
            Debug.Log($"Invalid amount to sell for {card.GetCardName()}.");
            return;
        }

        progression.GainCurrency(currency * amountToSell);
        existing.SubtractCard(existing, amountToSell);

        if (existing.GetCardCount() <= 0)
        {
            CardDeck.DeckInventory.Remove(existing);
        }

        Debug.Log($"You Sold {amountToSell}x {card.GetCardName()} for {currency * amountToSell} Hali.");
    }
}
