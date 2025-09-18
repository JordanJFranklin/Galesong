using UnityEngine;
using static UnityEngine.Rendering.GPUSort;

public class Shrine : MonoBehaviour
{
    public Transform Checkpoint;
    
    private void OnTriggerEnter(Collider other)
    {
        if(other.GetComponent<PlayerDriver>() != null)
        {
            other.GetComponent<PlayerDriver>().physicsProperties.lastCheckpoint = GetComponent<Shrine>();

            CharacterStats stats = other.GetComponent<CharacterStats>();
            Gourds gourd = other.GetComponent<Gourds>();

            ShrineEffect(stats, gourd);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<PlayerDriver>() != null)
        {
            CharacterStats stats = other.GetComponent<CharacterStats>();
            
            ChangeCards(stats.GetComponent<CardManager>(), false);
        }
    }

    public void ShrineEffect(CharacterStats stats, Gourds gourd)
    {
        stats.currentHealth = stats.GetStatValue(StatType.Health);
        stats.currentScarlet = stats.GetStatValue(StatType.Scarlet);
        stats.currentBlockPower = stats.GetStatValue(StatType.BlockPower);

        gourd.RefillGourd();
        stats.Cleanse();

        ChangeCards(stats.GetComponent<CardManager>(), true);

        Debug.Log($"Gourd Charges, CC Cleansed, Health, Block And Scarlet Restored! Set Checkpoint.");
    }

    public void ChangeCards(CardManager cards, bool canequip)
    {
        cards.canEquipCards = canequip;
    }
}
