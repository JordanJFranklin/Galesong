using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine;
using System;
using System.IO;
using UnityEngine.Audio;

public enum TargetFrameRate {Thirty,Sixty,Unlimited}
public enum CameraMode {UnTargetedMode, AimMode, TargetMode, FPSMode, DialogueMode, GrappleMode}
public enum ZTargetMode {Toggle,Hold}
public enum CrouchMode {Toggle, Hold}
public enum AudioMode {Stereo, Mono}
public enum ShadowResolution {Off, Low, Medium, High, Ultra}
public enum WindowSize {SD, HD, FullHD, QHD, UHD, FullUltraHD, NativeResolution}
public enum ParticleEffectGraphics {Off,Low,Medium,High,Ultra}
public enum WindowType {Windowed, Borderless, Fullscreen}

namespace GameSettings
{
    [System.Serializable]
    public class GameplaySettings
    {
        [Header("Camera Modes")]
        public Camera WorldCamera;
        public CameraMode Mode = CameraMode.UnTargetedMode;
        public ZTargetMode ZTargetType = ZTargetMode.Toggle;
        public CrouchMode CrouchType = CrouchMode.Toggle;

        [Header("View Distance")]
        public float FieldOfView = 90;
        public float maxViewDistance = 1000;

        [Header("Sensitivity")]
        public float sensitivity = 1;
        public float zoomSensitivity = 1;

        [Header("Controller Sensitivity")]
        public float controllerSensitivity = 100;
        public float controllerZoomSensitivity = 65;
    }

    [System.Serializable]
    public class GameAudio
    {
        [Header("Audio")]
        public AudioMode Mode = AudioMode.Stereo;
        public AudioMixer Mixer;
        public float MasterVolume = 1;
        public float MusicVolume = 1;
        public float SoundEffectsVolume = 1;
        public float AmbientSoundVolume = 1;
    }

    [System.Serializable]
    public class GameGraphics
    {
        [Header("Window")]
        public Volume GlobalVolume;
        public TargetFrameRate FrameRate;
        public WindowSize WindowScale = WindowSize.NativeResolution;
        public WindowType WindowStyle = WindowType.Fullscreen;

        [Header("Shadows")]
        public bool useDynamicShadows = true;
        public ShadowResolution ShadowQuality = ShadowResolution.Ultra;

        [Header("Particles")]
        public bool useDynamicParticleCollision = true;
        public ParticleEffectGraphics ParticleQuality = ParticleEffectGraphics.Ultra;

        [Header("Post Processing Effects")]
        public bool useBloom = true;
        public bool useVignette = true;
        public bool useBlur = true;
    }

    public class SettingsEvents
    {
        //Notes
        //Remember to create try and catch statements for exception handling in the future!
        
        public static void SaveSettings(string path, PlayerSettings Settings)
        {
            //Save To Existing File
            if (File.Exists(path))
            {
                //Clear File
                File.WriteAllText(path, string.Empty);

                WriteData(path, Settings);
                Debug.Log("File Found. Successfully Saved Game Settings");
            }
            //Save To Non Existing File
            else
            {
                //Create Text File To Path
                File.WriteAllText(path, string.Empty);

                WriteData(path, Settings);
                Debug.Log("File Not Found. Successfully Saved Game Settings");
            }
        }

        private static string t;
        private static void WriteData(string path, PlayerSettings Settings)
        {
            //Write All The Data Into The Inventory
            var scheme = InputManager.Instance.PlayerInputScheme;

            //Camera Data
            t = "MaxViewDistance " + Settings.gameplaySettings.maxViewDistance.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "FieldOfView " + Settings.gameplaySettings.FieldOfView.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "Sensitivity " + Settings.gameplaySettings.sensitivity.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "ZoomSensitivity " + Settings.gameplaySettings.zoomSensitivity.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "Invert Axis X " + scheme.invertX.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "Invert Axis Y " + scheme.invertY.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "ZTargetMode " + Settings.gameplaySettings.CrouchType.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "CrouchMode " + Settings.gameplaySettings.ZTargetType.ToString() + "\n";
            File.AppendAllText(path, t);

            //Audio Data
            t = "MasterVolume " + Settings.AudioSet.MasterVolume.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "MusicVolume " + Settings.AudioSet.MusicVolume.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "SoundEffectsVolume " + Settings.AudioSet.SoundEffectsVolume.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "AmbientSoundVolume " + Settings.AudioSet.AmbientSoundVolume.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "AudioMode " + Settings.AudioSet.Mode.ToString() + "\n";
            File.AppendAllText(path, t);

            //Graphics Data
            t = "WindowScale " + Settings.GraphicsSet.WindowScale.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "WindowStyle " + Settings.GraphicsSet.WindowStyle.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "ShadowQuality " + Settings.GraphicsSet.ShadowQuality.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "UseDynamicShadows " + Settings.GraphicsSet.useDynamicShadows.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "ParticleQuality " + Settings.GraphicsSet.ParticleQuality.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "UseDynamicParticleCollision " + Settings.GraphicsSet.useDynamicParticleCollision.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "UseBloom " + Settings.GraphicsSet.useBloom.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "UseBlur " + Settings.GraphicsSet.useBlur.ToString() + "\n";
            File.AppendAllText(path, t);
            t = "UseVignette " + Settings.GraphicsSet.useVignette.ToString() + "\n";
            File.AppendAllText(path, t);
        }

        private static void LoadData(string path, PlayerSettings Settings)
        {
            var scheme = InputManager.Instance.PlayerInputScheme;

            //Save To Existing File
            if (File.Exists(path))
            {
                // Open the file to read from.
                string[] readText = File.ReadAllLines(path);

                //Load In Basic Items From File
                foreach (string s in readText)
                {
                    if (s != null)
                    {
                        string[] separatingStrings = { " " };

                        string[] itemString = s.Split(separatingStrings, System.StringSplitOptions.RemoveEmptyEntries);

                        for (int i = 0; i < readText.Length; i++)
                        {
                            if (itemString[0] == "MaxViewDistance")
                            {
                                int savedCount = Int32.Parse(itemString[1]);
                                Settings.gameplaySettings.maxViewDistance = savedCount;
                            }

                            if (itemString[0] == "FieldOfView")
                            {
                                int savedCount = Int32.Parse(itemString[1]);
                                Settings.gameplaySettings.FieldOfView = savedCount;
                            }

                            if (itemString[0] == "ZoomSensitivity")
                            {
                                int savedCount = Int32.Parse(itemString[1]);
                                Settings.gameplaySettings.zoomSensitivity = savedCount;
                            }

                            if (itemString[0] == "Invert Axis X")
                            {
                                bool axisMode = bool.Parse(itemString[1]);
                                scheme.invertX = axisMode;
                            }

                            if (itemString[0] == "Invert Axis Y")
                            {
                                bool axisMode = bool.Parse(itemString[1]);
                                scheme.invertY = axisMode;
                            }

                            if (itemString[0] == "ZTargetMode")
                            {
                                if (Enum.TryParse<ZTargetMode>(itemString[1], out ZTargetMode yourEnum))
                                {
                                    ZTargetMode parsed_enum = (ZTargetMode)System.Enum.Parse(typeof(ZTargetMode), itemString[1]);
                                    Settings.gameplaySettings.ZTargetType = parsed_enum;
                                }
                            }

                            if (itemString[0] == "CrouchMode")
                            {
                                if (Enum.TryParse<CrouchMode>(itemString[1], out CrouchMode yourEnum))
                                {
                                    CrouchMode parsed_enum = (CrouchMode)System.Enum.Parse(typeof(CrouchMode), itemString[1]);
                                    Settings.gameplaySettings.CrouchType = parsed_enum;
                                }
                            }

                            if (itemString[0] == "MasterVolume")
                            {
                                float savedCount = float.Parse(itemString[1]);
                                Settings.AudioSet.MasterVolume = savedCount;
                            }

                            if (itemString[0] == "MusicVolume")
                            {
                                float savedCount = float.Parse(itemString[1]);
                                Settings.AudioSet.MusicVolume = savedCount;
                            }

                            if (itemString[0] == "SoundEffectsVolume")
                            {
                                float savedCount = float.Parse(itemString[1]);
                                Settings.AudioSet.SoundEffectsVolume = savedCount;
                            }

                            if (itemString[0] == "AmbientSoundVolume")
                            {
                                float savedCount = float.Parse(itemString[1]);
                                Settings.AudioSet.AmbientSoundVolume = savedCount;
                            }

                            if (itemString[0] == "AudioMode")
                            {
                                AudioMode savedType = (AudioMode)System.Enum.Parse(typeof(AudioMode), itemString[1]);
                                Settings.AudioSet.Mode = savedType;
                            }

                            if (itemString[0] == "WindowScale")
                            {
                                WindowSize savedType = (WindowSize)System.Enum.Parse(typeof(WindowSize), itemString[1]);
                                Settings.GraphicsSet.WindowScale = savedType;
                            }

                            if (itemString[0] == "WindowStyle")
                            {
                                WindowType savedType = (WindowType)System.Enum.Parse(typeof(WindowType), itemString[1]);
                                Settings.GraphicsSet.WindowStyle = savedType;
                            }

                            if (itemString[0] == "ShadowQuality")
                            {
                                ShadowResolution savedType = (ShadowResolution)System.Enum.Parse(typeof(ShadowResolution), itemString[1]);
                                Settings.GraphicsSet.ShadowQuality = savedType;
                            }

                            if (itemString[0] == "UseDynamicShadows")
                            {
                                bool savedCount = Boolean.Parse(itemString[1]);
                                Settings.GraphicsSet.useDynamicShadows = savedCount;
                            }

                            if (itemString[0] == "ParticleQuality")
                            {
                                ParticleEffectGraphics savedType = (ParticleEffectGraphics)System.Enum.Parse(typeof(ParticleEffectGraphics), itemString[1]);
                                Settings.GraphicsSet.ParticleQuality = savedType;
                            }

                            if (itemString[0] == "UseDynamicParticleCollision")
                            {
                                bool savedCount = Boolean.Parse(itemString[1]);
                                Settings.GraphicsSet.useDynamicParticleCollision = savedCount;
                            }

                            if (itemString[0] == "UseBloom")
                            {
                                bool savedCount = Boolean.Parse(itemString[1]);
                                Settings.GraphicsSet.useBloom = savedCount;
                            }

                            if (itemString[0] == "UseBlur")
                            {
                                bool savedCount = Boolean.Parse(itemString[1]);
                                Settings.GraphicsSet.useBlur = savedCount;
                            }

                            if (itemString[0] == "UseVignette")
                            {
                                bool savedCount = Boolean.Parse(itemString[1]);
                                Settings.GraphicsSet.useVignette = savedCount;
                            }


                        }
                    }
                }

                Debug.Log("File Found. Successfully Loaded Game Settings.");
            }
            else
            {
                //Clear File
                File.WriteAllText(path, string.Empty);

                WriteData(path, Settings);

                Debug.Log("File Not Found. Game Settings set to Default Value. Preloaded Default Values. Successfully Saved Game Settings");
            }
        }

        public static void LoadSettings(string path, PlayerSettings Settings)
        {
            //Save To Existing File
            if (File.Exists(path))
            {
                if(new FileInfo(path).Length == 0)
                {
                    //Create Text File To Path
                    File.WriteAllText(path, string.Empty);

                    WriteData(path, Settings);
                    LoadData(path, Settings);
                    PlayerSettings.Instance.UpdateSettings();
                }
                else
                {
                    LoadData(path, Settings);
                }
            }
            //Save To Non Existing File
            else
            {
                //Create Text File To Path
                File.WriteAllText(path, string.Empty);

                WriteData(path, Settings);
                LoadData(path, Settings);
            }
        }
    }
}
