using UnityEngine;

public class PrismaDrop : MonoBehaviour
{
    public DropType dropType;
    public float restoreAmount = 5f;

    [Header("Model Prefabs")]
    public GameObject prismaPrefab;
    public GameObject prismaUltimaPrefab;

    [Header("Floating Animation")]
    public float floatSpeed = 2f;
    public float floatHeight = 0.2f;

    [Header("Seeking Settings")]
    public bool isSeekingPlayer = false;
    public float seekSpeed = 15f;

    [Header("Idle Rotation")]
    public float rotationSpeed = 30f;
    public Vector3 rotationAxis = Vector3.up;


    private Transform visualModel;
    private Vector3 visualStartPos;

    public CharacterStats stats;

    private void Start()
    {
        SpawnModel();
    }

    private void Update()
    {
        if (visualModel != null)
        {
            float offsetY = Mathf.Sin(Time.time * floatSpeed) * floatHeight;
            visualModel.localPosition = new Vector3(0f, offsetY, 0f);

            visualModel.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
        }

        if (isSeekingPlayer)
        {
            Vector3 direction = (PlayerDriver.Instance.transform.position - transform.position).normalized;
            transform.position += direction * seekSpeed * Time.deltaTime;

            Transform player = PlayerDriver.Instance.transform;

            float distToPlayer = Vector3.Distance(transform.position, player.position);

            if (distToPlayer <= 3f)
            {
                Collect();
            }
        }
    }

    public void Collect()
    {
        switch (dropType)
        {
            case DropType.Prisma:
                PlayerDriver.Instance.GetComponent<CharacterStats>().GainScarlet(restoreAmount);
                break;
            case DropType.PrismaUltima:
                PlayerDriver.Instance.GetComponent<CharacterStats>().GainScarlet(
                    PlayerDriver.Instance.GetComponent<CharacterStats>().GetStatValue(StatType.Scarlet)
                );
                break;
        }

        Destroy(gameObject);
    }




    public void StartSeeking()
    {
        isSeekingPlayer = true;
        GetComponent<DropPhysicsController>().enabled = false;
    }

    public void SetPrisma(DropType type, float customAmount = 0f)
    {
        dropType = type;

        if (type == DropType.Prisma)
        {
            restoreAmount = customAmount; 
        }
        else if (type == DropType.PrismaUltima)
        {
            restoreAmount = PlayerDriver.Instance.transform.gameObject.GetComponent<CharacterStats>().maxScarlet;
        }
    }

    private void SpawnModel()
    {
        GameObject prefabToUse = dropType == DropType.PrismaUltima ? prismaUltimaPrefab : prismaPrefab;

        if (prefabToUse != null)
        {
            GameObject model = Instantiate(prefabToUse, transform);
            model.transform.localPosition = Vector3.zero;
            visualModel = model.transform;
            Destroy(gameObject, 100);
        }
    }
}
