using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameSettings;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.ParticleSystemJobs;
using UnityEngine.SceneManagement;

public class PlayerSettings : MonoBehaviour
{
    [Header("Universal Settings")]
    public bool isToUpdateSettings = false;
    public GameplaySettings gameplaySettings;
    public GameAudio AudioSet;
    public GameGraphics GraphicsSet;
    public AudioSource MusicPlayer;
    public string path;

    //Convert Manager To Singleton
    private static PlayerSettings _instance;
    static bool _destroyed;

    public static PlayerSettings Instance
    {
        get
        {
            // Prevent re-creation of the singleton during play mode exit.
            if (_destroyed) return null;

            // If the instance is already valid, return it. Needed if called from a
            // derived class that wishes to ensure the instance is initialized.
            if (_instance != null) return _instance;

            // Find the existing instance (across domain reloads).
            if ((_instance = FindObjectOfType<PlayerSettings>()) != null) return _instance;

            // Create a new GameObject instance to hold the singleton component.
            var gameObject = new GameObject(typeof(PlayerSettings).Name);

            // Move the instance to the DontDestroyOnLoad scene to prevent it from
            // being destroyed when the current scene is unloaded.
            DontDestroyOnLoad(gameObject);

            // Create the MonoBehavior component. Awake() will assign _instance.
            return gameObject.AddComponent<PlayerSettings>();
        }
    }

    // Start is called before the first frame update
    protected virtual void Awake()
    {
        //DontDestroyOnLoad(gameObject);
        Debug.Assert(_instance == null || _instance == this, "More than one singleton instance instantiated!", this);

        if (_instance == null || _instance == this)
        {
            _instance = this;
        }
    }

    void Start()
    {
        path = Application.dataPath + "/GameSettings.txt";

        //SettingsEvents.LoadSettings(path, GetComponent<PlayerSettings>());
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Grab Spawners
        foreach (PlayerSpawner spawner in FindObjectsOfType<PlayerSpawner>())
        {
            spawner.isActiveSpawner = false;
        }

        Debug.Log("OnSceneLoaded: " + scene.name);
        Debug.Log(mode);
    }

    // Update is called once per frame
    void Update()
    {
        //PitchShiftBattleMusic();

        if(isToUpdateSettings)
        {
            SetAllSettings();
            isToUpdateSettings = false;
        }
    }

    public void UpdateSettings()
    {
        isToUpdateSettings = true;
    }

    private void SetAllSettings()
    {
        //SetVolumeSettings();
        //SetViewDistance();
        //SetShadowing(GraphicsSet.ShadowQuality);
        //PostProcessing();
        //SetScreenSettings(GraphicsSet.WindowScale, GraphicsSet.WindowStyle);
        //Debug.Log("Set All New Settings.");
    }

    public void SetFrameRate()
    {
        if(GraphicsSet.FrameRate.Equals(TargetFrameRate.Unlimited))
        {
            Application.targetFrameRate = -1;
        }

        if (GraphicsSet.FrameRate.Equals(TargetFrameRate.Sixty))
        {
            Application.targetFrameRate = 60;
        }

        if (GraphicsSet.FrameRate.Equals(TargetFrameRate.Thirty))
        {
            Application.targetFrameRate = 30;
        }

    }

    public void SetVolumeSettings()
    {
        if(AudioSet.Mode.Equals(AudioMode.Stereo))
        {
            MusicPlayer.spatialBlend = 0;
        }

        if (AudioSet.Mode.Equals(AudioMode.Mono))
        {
            MusicPlayer.spatialBlend = 1;
        }
        
        AudioSet.Mixer.SetFloat("MasterVolume", AudioSet.MasterVolume);
        AudioSet.Mixer.SetFloat("AmbientVolume", AudioSet.AmbientSoundVolume);
        AudioSet.Mixer.SetFloat("SoundEffectVolume", AudioSet.SoundEffectsVolume);
        AudioSet.Mixer.SetFloat("MusicVolume", AudioSet.MusicVolume);
    }

    public void SetScreenSettings(WindowSize Size, WindowType Type)
    {
        Vector2 scale = new Vector2(0, 0);
        
        if(Size.Equals(WindowSize.NativeResolution))
        {
            scale.x = Screen.currentResolution.width;
            scale.y = Screen.currentResolution.height;
        }

        if (Size.Equals(WindowSize.HD))
        {
            scale.x = 1280;
            scale.y = 720;
        }

        if (Size.Equals(WindowSize.FullHD))
        {
            scale.x = 1920;
            scale.y = 1080;
        }
        
        if (Size.Equals(WindowSize.QHD))
        {
            scale.x = 2560;
            scale.y = 1440;
        }

        if (Size.Equals(WindowSize.UHD))
        {
            scale.x = 3840;
            scale.y = 2160;
        }

        if (Size.Equals(WindowSize.SD))
        {
            scale.x = 640;
            scale.y = 480;
        }

        if (Size.Equals(WindowSize.FullUltraHD))
        {
            scale.x = 7680;
            scale.y = 4320;
        }

        if(Type.Equals(WindowType.Windowed))
        {
            Screen.SetResolution(Mathf.RoundToInt(scale.x), Mathf.RoundToInt(scale.y), FullScreenMode.Windowed);
        }

        if (Type.Equals(WindowType.Fullscreen))
        {
            Screen.SetResolution(Mathf.RoundToInt(scale.x), Mathf.RoundToInt(scale.y), FullScreenMode.ExclusiveFullScreen);
        }

        if (Type.Equals(WindowType.Borderless))
        {
            Screen.SetResolution(Mathf.RoundToInt(scale.x), Mathf.RoundToInt(scale.y), FullScreenMode.FullScreenWindow);
        }
    }

    public void SetViewDistance()
    {
        gameplaySettings.WorldCamera.fieldOfView = gameplaySettings.FieldOfView;
        gameplaySettings.WorldCamera.farClipPlane = gameplaySettings.maxViewDistance;
    }

    public void SetShadowing(ShadowResolution QualityType)
    {
        //Use Dyanmic Shadows is currently not doing anything. It could be used for contact shadows. Need Further testing......
        
        if(GraphicsSet.GlobalVolume.profile.TryGet<ContactShadows>(out var ContactShadows))
        {
            ContactShadows.active = GraphicsSet.useDynamicShadows;
        }

        if (GraphicsSet.GlobalVolume.profile.TryGet<ScreenSpaceAmbientOcclusion>(out var AmbientOcclusion))
        {
            AmbientOcclusion.active = GraphicsSet.useDynamicShadows;
        }

        HDAdditionalLightData[] Lights = FindObjectsOfType(typeof(HDAdditionalLightData)) as HDAdditionalLightData[];

        foreach (HDAdditionalLightData light in Lights)
        {
            if (GraphicsSet.ShadowQuality.Equals(ShadowResolution.Off))
            {
                light.gameObject.GetComponent<Light>().shadows = LightShadows.None;
                light.SetShadowResolution(0);
            }

            if (GraphicsSet.ShadowQuality.Equals(ShadowResolution.Low))
            {
                light.gameObject.GetComponent<Light>().shadows = LightShadows.Soft;
                light.SetShadowResolution(256);
            }

            if (GraphicsSet.ShadowQuality.Equals(ShadowResolution.Medium))
            {
                light.gameObject.GetComponent<Light>().shadows = LightShadows.Soft;
                light.SetShadowResolution(512);
            }

            if (GraphicsSet.ShadowQuality.Equals(ShadowResolution.High))
            {
                light.gameObject.GetComponent<Light>().shadows = LightShadows.Soft;
                light.SetShadowResolution(1024);
            }

            if (GraphicsSet.ShadowQuality.Equals(ShadowResolution.Ultra))
            {
                light.gameObject.GetComponent<Light>().shadows = LightShadows.Soft;
                light.SetShadowResolution(2048);
            }
        }

        Volume[] Volumes = FindObjectsOfType(typeof(Volume)) as Volume[];

        foreach (Volume Volume in Volumes)
        {
            //Disable Fog To Prevent Flickering Fog Visual Error
            if (GraphicsSet.ShadowQuality.Equals(ShadowResolution.Off) && Volume.profile.TryGet<Fog>(out var FogOn))
            {
                FogOn.active = false;
            }
            //Enable Fog If Shadows Are On With Any Quality Above Off Starting From Low To High
            if (!GraphicsSet.ShadowQuality.Equals(ShadowResolution.Off) && Volume.profile.TryGet<Fog>(out var FogOff))
            {
                FogOff.active = true;
            }
        }
            
    }

    public void PostProcessing()
    {
        Volume[] Volumes = FindObjectsOfType(typeof(Volume)) as Volume[];

        foreach (Volume Volume in Volumes)
        {
            //Disable Fog To Prevent Flickering Fog Visual Error
            if (Volume.profile.TryGet<Bloom>(out var Bloom))
            {
                Bloom.active = GraphicsSet.useBloom;
            }

            if (Volume.profile.TryGet<MotionBlur>(out var Blur))
            {
                Blur.active = GraphicsSet.useBlur;
            }

            if (Volume.profile.TryGet<Vignette>(out var Vignette))
            {
                Vignette.active = GraphicsSet.useVignette;
            }
        }
    }

    public void SetParticles()
    {
        ParticleSystem[] ParticleEffects = FindObjectsOfType(typeof(ParticleSystem)) as ParticleSystem[];

        foreach (ParticleSystem ParticleSys in ParticleEffects)
        {

        }
    }

    // Called when the singleton is created *or* after a domain reload in the editor.
    protected virtual void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

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

