using UnityEngine;

public class ProgressionInv : MonoBehaviour
{
    [Header("Wallet")]
    public int Hali = 0;

    [Header("Magical Treasures")]
    public bool hasJetstreamHeels;
    public bool hasBeastClaws;
    public bool hasMistBreaker;
    public bool hasCloudBurstCowl;
    public bool hasTheGreatNimbus;
    public bool hasPhantomEarrings;
    public bool hasMoonGoddessCrown;
    public bool hasJadeSerpentBangle;
    public bool hasRegalMoonstoneBangle;
    public bool hasDragonOrbSash;
    [Header("Windborne Graces")]
    public bool hasGrace_Zen;
    public bool hasGrace_Galeforce;
    public bool hasGrace_TempestKick;
    public bool hasGrace_VayudaBreathwork;
    public bool hasGrace_TradewindsSlash;
    public bool hasGrace_HurricaneDance;
    public bool hasGrace_Exhilus;
    public bool hasGrace_Aspiria;
    public bool hasGrace_LeviNamiya;
    public bool hasGrace_Vasperia;
    public bool hasGrace_WakingWyvern;
    public bool hasGrace_WyvernSoul;
    public bool hasGrace_Breath;
    public bool hasGrace_Cannon;
    public bool hasGrace_Cyclone;

    //Convert Manager To Singleton
    private static ProgressionInv _instance;
    private CharacterStats stats;
    static bool _destroyed;
    public static ProgressionInv Instance
    {
        get
        {
            // Prevent re-creation of the singleton during play mode exit.
            if (_destroyed) return null;

            // If the instance is already valid, return it. Needed if called from a
            // derived class that wishes to ensure the instance is initialized.
            if (_instance != null) return _instance;

            // Find the existing instance (across domain reloads).
            if ((_instance = FindObjectOfType<ProgressionInv>()) != null) return _instance;

            // Create a new GameObject instance to hold the singleton component.
            var gameObject = new GameObject(typeof(ProgressionInv).Name);

            // Move the instance to the DontDestroyOnLoad scene to prevent it from
            // being destroyed when the current scene is unloaded.
            DontDestroyOnLoad(gameObject);

            // Create the MonoBehavior component. Awake() will assign _instance.
            return gameObject.AddComponent<ProgressionInv>();
        }
    }


    protected virtual void Awake()
    {
        Debug.Assert(_instance == null || _instance == this, "More than one singleton instance instantiated!", this);

        if (_instance == null || _instance == this)
        {
            _instance = this;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        stats = GetComponent<CharacterStats>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GainCurrency(int hali)
    {
        Hali += Mathf.RoundToInt(hali + (hali * stats.GetStatValue(StatType.HaliBonus)));
    }

    public void SubtractCurrency(int hali)
    {
        Hali -= hali;
    }
}
