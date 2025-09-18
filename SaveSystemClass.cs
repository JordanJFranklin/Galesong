using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Ink.Runtime;
using GameSettings;

namespace FileManagement
{
    public class FileRead
    {
        public string ReadSpecificLine(string filePath, int line)
        {
            using (var sr = new StreamReader(filePath))
            {
                for (int i = 1; i < line; i++)
                    sr.ReadLine();

                return sr.ReadLine();
            }
        }
    }

    public class PersistentStory
    {
        public static void SaveStoryProgress(string path, Story GlobalStory)
        {
            //Check For File
            if (File.Exists(path))
            {
                //Clear File
                File.WriteAllText(path, string.Empty);

                //Add Text To it
                File.AppendAllText(path, GlobalStory.state.ToJson());

                Debug.Log("Story Progression Saved Successfully");
            }
            else
            {
                //Create Text File To Path
                File.WriteAllText(path, string.Empty);

                //Add Text To it
                File.AppendAllText(path, GlobalStory.state.ToJson());

                //Create File Fall Back And Then Save
                Debug.Log("Fall Back Method Catch Exception. Story Progression File Not Found On Save. Creating new file and saving new data set.");
                Debug.Log("Story Progression Saved Successfully");
            }
        }

        public static void LoadStoryProgress(string path, Story GlobalStory)
        {
            //Check For File
            if (File.Exists(path))
            {
                string text = File.ReadAllText(path);
                
                GlobalStory.state.LoadJson(text);

                Debug.Log("Story Progression Loaded Successfully");
            }
            else
            {
                //Fall Back Warning
                Debug.LogWarning("Fall Back Method Catch Exception. Story Progress File Not Found On Load. Creating new file and saving new data set.");
                Debug.LogWarning("Story Progression Saved Successfully");

                SaveStoryProgress(path, GlobalStory);

                return;
            }
        }

    }

    public class NativeSaveSystem
    {
        public static void PerformFullGameSave()
        {
            PlayerSettings.Instance.UpdateSettings();

            if (PlayerSettings.Instance != null && PlayerSettings.Instance.path != null)
            {
                SettingsEvents.SaveSettings(PlayerSettings.Instance.path, PlayerSettings.Instance);
            }
            else
            {
                Debug.LogError("Save will not continue. GameSettings could not be saved.");
                return;
            }

            if (DialogueManager.Instance != null)
            {
                DialogueManager.Instance.dialogueVariables.SaveGlobalVariables();
            }
            else
            {
                Debug.LogError("Save will not continue. Story could not be saved. GameSettings was saved.");
                return;
            }

            if(InputManager.Instance != null && InputManager.Instance.path != null)
            {
                InputManager.Instance.SaveKeyScheme();
            }
            else
            {
                Debug.LogError("Save will not continue. Inputs could not be saved. Inventory, GameSettings and Story was saved.");
                return;
            }

            Debug.Log("Game Fully Saved Successfully.");
        }

        public static void PerformPartialGameSave()
        {
            PlayerSettings.Instance.UpdateSettings();

            if (PlayerSettings.Instance != null && PlayerSettings.Instance.path != null)
            {
                SettingsEvents.SaveSettings(PlayerSettings.Instance.path, PlayerSettings.Instance);
            }
            else
            {
                Debug.LogError("Save will not continue. GameSettings could not be saved.");
                return;
            }

            if (InputManager.Instance != null && InputManager.Instance.path != null)
            {
                InputManager.Instance.SaveKeyScheme();
            }
            else
            {
                Debug.LogError("Save will not continue. Inputs could not be saved. Inventory, GameSettings and Story was saved.");
                return;
            }

            Debug.Log("Game Settings Saved Only. Saved Successfully.");
        }
    }
}
