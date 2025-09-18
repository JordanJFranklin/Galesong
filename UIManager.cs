using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MouseMoveType { Centered, Free, FreeLimitedToWindow }

public class UIManager : MonoBehaviour
{
    [Header("General Pause Settings")]
    public bool isPaused;
    public List<GameObject> AllUIElements;

    [Header("Mouse Settings")]
    public bool isMouseVisible;
    public MouseMoveType MouseState;

    [Header("Key Settings")]
    public bool ListenKey;
    public bool isListeningForKey;
    public GameObject InputGrid;
    public GameObject InputBindingUI;
    public List<GameObject> InputUI = new List<GameObject>();

    //Convert Manager To Singleton
    private static UIManager _instance;
    static bool _destroyed;
    public static UIManager Instance
    {
        get
        {
            // Prevent re-creation of the singleton during play mode exit.
            if (_destroyed) return null;

            // If the instance is already valid, return it. Needed if called from a
            // derived class that wishes to ensure the instance is initialized.
            if (_instance != null) return _instance;

            // Find the existing instance (across domain reloads).
            if ((_instance = FindObjectOfType<UIManager>()) != null) return _instance;

            // Create a new GameObject instance to hold the singleton component.
            var gameObject = new GameObject(typeof(UIManager).Name);

            // Move the instance to the DontDestroyOnLoad scene to prevent it from
            // being destroyed when the current scene is unloaded.
            DontDestroyOnLoad(gameObject);

            // Create the MonoBehavior component. Awake() will assign _instance.
            return gameObject.AddComponent<UIManager>();
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
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SetWindowVisabillity(GameObject Window, bool state)
    {
        Window.SetActive(state);
    }

    public void ClearConflictKey(int index)
    {
        foreach (GameObject UI in InputUI)
        {
            if (UI.GetComponent<SettingsKeyUI>().KeyIndex == index)
            {
                UI.GetComponent<SettingsKeyUI>().KeyText.text = KeyCode.None.ToString();
                return;
            }
        }
    }

    private bool buiidKeys = true;
    private bool defaultKeys = false;
    public void BuildKeyBindings()
    {
        if (buiidKeys && isPaused)
        {
            if (InputUI.Count > 0)
            {
                foreach (GameObject UI in InputUI)
                {
                    Destroy(UI);
                }

                InputUI.Clear();
            }

            for (int i = 0; i < InputManager.Instance.PlayerInputScheme.Inputs.Count; i++)
            {
                GameObject Binding = Instantiate(InputBindingUI, InputGrid.transform);

                RegisterBinding(Binding);

                if (Binding.GetComponent<SettingsKeyUI>() != null && !defaultKeys)
                {
                    Binding.GetComponent<SettingsKeyUI>().KeyIndex = i;
                    Binding.GetComponent<SettingsKeyUI>().Text.text = InputManager.Instance.PlayerInputScheme.Inputs[i].ActionName.ToString();
                    Binding.GetComponent<SettingsKeyUI>().KeyText.text = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();
                    Binding.name = InputManager.Instance.PlayerInputScheme.Inputs[i].ActionName.ToString();
                }

                if (Binding.GetComponent<SettingsKeyUI>() != null && defaultKeys)
                {
                    Binding.GetComponent<SettingsKeyUI>().KeyIndex = i;
                    Binding.GetComponent<SettingsKeyUI>().Text.text = InputManager.Instance.PlayerInputScheme.DefaultInputs[i].ActionName.ToString();
                    Binding.GetComponent<SettingsKeyUI>().KeyText.text = InputManager.Instance.PlayerInputScheme.DefaultInputs[i].key.ToString();
                    Binding.name = InputManager.Instance.PlayerInputScheme.Inputs[i].ActionName.ToString();
                }
            }

            buiidKeys = false;
            defaultKeys = false;
        }
    }
    public void RegisterUIElement(GameObject UIElement)
    {
        if (!AllUIElements.Contains(UIElement))
        {
            AllUIElements.Add(UIElement);
        }
    }
    public void RegisterBinding(GameObject UIElement)
    {
        if (!InputUI.Contains(UIElement))
        {
            InputUI.Add(UIElement);
        }
    }

    protected virtual void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif
    }

#if UNITY_EDITOR
    // Called when entering or exiting play mode.
    static void OnPlayModeStateChanged(UnityEditor.PlayModeStateChange stateChange)
    {
        // Reset static _destroyed field. Required when domain reloads are disabled.
        // Note: ExitingPlayMode is called too early.
        if (stateChange == UnityEditor.PlayModeStateChange.EnteredEditMode)
        {
            UnityEditor.EditorApplication.playModeStateChanged -=
                OnPlayModeStateChanged;
            _destroyed = false;
        }
    }
#endif
}
