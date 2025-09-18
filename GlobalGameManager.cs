using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalGameManager : MonoBehaviour
{
    //Convert Manager To Singleton
    private static GlobalGameManager _instance;
    static bool _destroyed;
    public static GlobalGameManager Instance
    {
        get
        {
            // Prevent re-creation of the singleton during play mode exit.
            if (_destroyed) return null;

            // If the instance is already valid, return it. Needed if called from a
            // derived class that wishes to ensure the instance is initialized.
            if (_instance != null) return _instance;

            // Find the existing instance (across domain reloads).
            if ((_instance = FindObjectOfType<GlobalGameManager>()) != null) return _instance;

            // Create a new GameObject instance to hold the singleton component.
            var gameObject = new GameObject(typeof(ProgressionInv).Name);

            // Move the instance to the DontDestroyOnLoad scene to prevent it from
            // being destroyed when the current scene is unloaded.
            DontDestroyOnLoad(gameObject);

            // Create the MonoBehavior component. Awake() will assign _instance.
            return gameObject.AddComponent<GlobalGameManager>();
        }
    }

    // Start is called before the first frame update
    private void Awake()
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
