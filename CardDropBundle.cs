using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Represents a bundle of cards dropped in the world. This object holds a list of dropped cards
/// and displays a visual effect based on the highest card tier it contains. When the player collides with it,
/// all cards are added to their inventory and the drop is destroyed.
/// </summary>
public class CardDropBundle : MonoBehaviour
{
    [Header("Tier Effect")]

    [Tooltip("Effect shown for Tier 1 (Tactic) cards.")]
    public GameObject Tier1Effect;

    [Tooltip("Effect shown for Tier 2 (Monster) cards.")]
    public GameObject Tier2Effect;

    [Tooltip("Effect shown for Tier 3 (Holographic) cards.")]
    public GameObject Tier3Effect;

    [Header("Cards")]

    [Tooltip("The list of cards contained in this drop bundle.")]
    public List<Cards> containedCards = new();

    /// <summary>
    /// Activates the appropriate visual effect based on the tier of the highest card in the bundle.
    /// </summary>
    /// <param name="Tier">The highest card tier to visually represent.</param>
    public void SetVisualEffect(CardTiers Tier)
    {
        if (Tier1Effect != null) Tier1Effect.SetActive(false);
        if (Tier2Effect != null) Tier2Effect.SetActive(false);
        if (Tier3Effect != null) Tier3Effect.SetActive(false);

        switch (Tier)
        {
            case CardTiers.Tactic:
                if (Tier1Effect != null)
                    Tier1Effect.SetActive(true);
                break;

            case CardTiers.Monster:
                if (Tier2Effect != null)
                    Tier2Effect.SetActive(true);
                break;

            case CardTiers.Holographic:
                if (Tier3Effect != null)
                    Tier3Effect.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// Initializes the bundle with the list of cards it contains.
    /// </summary>
    /// <param name="cards">The list of cards to assign to this drop bundle.</param>
    public void Initialize(List<Cards> cards)
    {
        containedCards = cards;
    }
}