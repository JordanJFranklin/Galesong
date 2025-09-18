using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using InputKeys;

public class PrototypeUI : MonoBehaviour
{
    public bool isPaused;
    public Canvas canvas;

    [Header("UI")]
    public GameObject PauseScreenMain;
    public TextMeshProUGUI mousesensitivityText;
    public TextMeshProUGUI controllersensitivityText;
    public Slider mousesensitivitySlider;
    public Slider controllersensitivitySlider;
    public Toggle enableController;
    public Toggle enableInvertX;
    public Toggle enableInvertY;

    // Singleton instance for the PlayerDriver — allows global static access to this specific instance
    private static PrototypeUI _instance;

    // Flag indicating whether the singleton was destroyed (prevents duplicate instantiation)
    static bool _destroyed;

    public static PrototypeUI Instance
    {
        get
        {
            // Prevent re-creation of the singleton during play mode exit.
            if (_destroyed) return null;

            // If the instance is already valid, return it. Needed if called from a
            // derived class that wishes to ensure the instance is initialized.
            if (_instance != null) return _instance;

            // Find the existing instance (across domain reloads).
            if ((_instance = FindAnyObjectByType<PrototypeUI>()) != null) return _instance;

            // Create a new GameObject instance to hold the singleton component.
            var gameObject = new GameObject(typeof(PrototypeUI).Name);

            // Move the instance to the DontDestroyOnLoad scene to prevent it from
            // being destroyed when the current scene is unloaded.
            DontDestroyOnLoad(gameObject);

            // Create the MonoBehavior component. Awake() will assign _instance.
            return gameObject.AddComponent<PrototypeUI>();
        }
    }


    // Called when the script instance is being loaded (before Start or any frame updates)
    protected virtual void Awake()
    {
        // Ensure only one instance of this class exists (singleton pattern)
        Debug.Assert(_instance == null || _instance == this, "More than one singleton instance instantiated!", this);

        // Assign this instance if it's the first or correct one
        if (_instance == null || _instance == this)
        {
            _instance = this;
        }
    }

    private void Start()
    {
        ApplyCursorLock(true);
        Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.Windowed);
    }

    private void Update()
    {
        var scheme = InputManager.Instance.PlayerInputScheme;

        if (scheme.WasPressedThisFrame(KeyActions.EscapeMenu))
        {
            TogglePause();
        }

        if (isPaused)
        {
            UpdateSensitivityDisplay();
        }
    }

    private void TogglePause()
    {
        isPaused = !isPaused;
        Debug.Log($"Paused: {isPaused}");
        if (isPaused)
        {
            Time.timeScale = 0f;   // Pauses the game
            LoadSettingsToUI();
            PauseScreenMain.SetActive(true);
            ApplyCursorLock(false);
        }
        else
        {
            Time.timeScale = 1f;   // Resume the game
            SaveSettingsFromUI();
            PauseScreenMain.SetActive(false);
            ApplyCursorLock(true);
        }
    }

    private void ApplyCursorLock(bool locked)
    {
        Cursor.visible = !locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private void LoadSettingsToUI()
    {
        var settings = PlayerSettings.Instance.gameplaySettings;
        var inputScheme = InputManager.Instance.PlayerInputScheme;

        mousesensitivitySlider.value = settings.sensitivity;
        controllersensitivitySlider.value = settings.controllerSensitivity;

        enableInvertX.isOn = inputScheme.invertX;
        enableInvertY.isOn = inputScheme.invertY;
        enableController.isOn = inputScheme.enableController;

        UpdateSensitivityDisplay();
    }

    private void SaveSettingsFromUI()
    {
        var settings = PlayerSettings.Instance.gameplaySettings;
        settings.sensitivity = mousesensitivitySlider.value;
        settings.controllerSensitivity = controllersensitivitySlider.value;

        var inputScheme = InputManager.Instance.PlayerInputScheme;
        inputScheme.invertX = enableInvertX.isOn;
        inputScheme.invertY = enableInvertY.isOn;
        inputScheme.enableController = enableController.isOn;
    }

    private void UpdateSensitivityDisplay()
    {
        mousesensitivityText.text = mousesensitivitySlider.value.ToString("0.00");
        controllersensitivityText.text = controllersensitivitySlider.value.ToString("0.00");
    }

    public void ResetLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        Time.timeScale = 1;
    }
}
