using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using InputKeys;
using FileManagement;
using System.IO;
using System;
using DialogueClass;
using Ink.Runtime;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public InputScheme PlayerInputScheme;
    public bool updateKeys = true;
    public bool debugKeys;
    public string path;
    
    //Convert Manager To Singleton
    private static InputManager _instance;
    static bool _destroyed;
    public static InputManager Instance
    {
        get
        {
            // Prevent re-creation of the singleton during play mode exit.
            if (_destroyed) return null;

            // If the instance is already valid, return it. Needed if called from a
            // derived class that wishes to ensure the instance is initialized.
            if (_instance != null) return _instance;

            // Find the existing instance (across domain reloads).
            if ((_instance = FindAnyObjectByType<InputManager>()) != null) return _instance;

            // Create a new GameObject instance to hold the singleton component.
            var gameObject = new GameObject(typeof(InputManager).Name);

            // Move the instance to the DontDestroyOnLoad scene to prevent it from
            // being destroyed when the current scene is unloaded.
            DontDestroyOnLoad(gameObject);

            // Create the MonoBehavior component. Awake() will assign _instance.
            return gameObject.GetComponent<InputManager>();
        }
    }

    protected virtual void Awake()
    {
        Debug.Assert(_instance == null || _instance == this, "More than one singleton instance instantiated! You have a duplicate script running!", this);

        if (_instance == null || _instance == this)
        {
            _instance = this;
        }

        path = Application.dataPath + "/ArbiterInputs.txt";

        PlayerInputScheme.centralinputs = new CentralizedInputs();
        BuildUniqueKeyIndexing();
        PlayerInputScheme.InitializeDoubleTapTracking();
    }


    void Start()
    {
        LoadKeyScheme();            // Now Inputs list is populated
        UpdateIndexes();            // Keep input lookup synced

        PlayerInputScheme.NintendoSwitchProController = new NintendoSwitchPro();

        //Initialize centralized inputs now
        PlayerInputScheme.centralinputs.InitializeKeyboardInputs();
        PlayerInputScheme.centralinputs.InitializeNintendoSwitchControllerInputs();
    }


    // Update is called once per frame
    void Update()
    {
        DebugKeys();
        KeyMovement();
        UpdateIndexes();

        if (searchForConflict)
        {
            CheckForInvalidInputs(conflictKey, conflictIndex);
        }

        if(PlayerInputScheme.enableController)
        {
            PlayerInputScheme.NintendoSwitchProController.Enable();
            PlayerInputScheme.NintendoSwitchProController.Player.Enable();
        }
        else
        {
            PlayerInputScheme.NintendoSwitchProController.Disable();
            PlayerInputScheme.NintendoSwitchProController.Player.Disable();
        }
    }

    public void BuildUniqueKeyIndexing()
    {
        PlayerInputScheme.InputKeyIndexLib.Clear();

        PlayerInputScheme.InputKeyIndexLib = new Dictionary<KeyActions, int>();

        for (int i = 0; i < PlayerInputScheme.DefaultInputs.Count; i++)
        {
            PlayerInputScheme.InputKeyIndexLib.Add(PlayerInputScheme.DefaultInputs[i].BoundAction, i);
        }
    }

    private void TryRebuildIndexing()
    {
        BuildUniqueKeyIndexing();
        Debug.Log("InputKeyIndexLib rebuilt automatically after reload.");
    }

    void LoadKeyScheme()
    {
        //Check For File
        if (File.Exists(path))
        {
            print("Input File Found. Loading In KeyInputs From File");

            // Open the file to read from.
            string[] readText = File.ReadAllLines(path);

            //Check If the document Empty
            if (new FileInfo(path).Length == 0)
            {
                //File Not Found
                print("WARNING: Input File Was Determined To Be Empty. Resetting File With Default Inputs...");

                //Create Text File To Path
                File.WriteAllText(path, string.Empty);

                //Add Text To it
                foreach (InputKey Key in PlayerInputScheme.DefaultInputs)
                {
                    string keycode = Key.BoundAction.ToString() + "  [ " + Key.key.ToString() + " ] " + " \n";

                    KeyActions parsed_enum;

                    File.AppendAllText(path, keycode);

                    if (debugKeys)
                    {
                        if (Enum.TryParse<KeyActions>(Key.BoundAction.ToString(), out parsed_enum))
                        {
                            print("The Key " + Key.BoundAction.ToString() + " has successfully parsed");
                        }
                        else
                        {
                            print("The Key " + Key.BoundAction.ToString() + " is an invalid or empty action type. Please change the enum type. This may happen due to removing an enum parameter in the Inputclass.cs script.");
                        }

                        print(keycode);
                    }
                }

                foreach (string s in readText)
                {
                    if (s != null)
                    {
                        string boundActionStringName = s.Split(new char[] { ' ' })[0];

                        if (System.Enum.TryParse<KeyActions>(boundActionStringName, out KeyActions yourEnum))
                        {
                            KeyActions parsed_enum = (KeyActions)System.Enum.Parse(typeof(KeyActions), boundActionStringName);

                            //Loop Through Default Inputs
                            for (int i = 0; i < PlayerInputScheme.DefaultInputs.Count; i++)
                            {
                                //Found A Match To Read From And Add To PlayerInputScheme.Input
                                if (PlayerInputScheme.DefaultInputs[i].BoundAction.Equals(parsed_enum))
                                {
                                    InputKey NewKey = new InputKey();

                                    string[] separatingStrings = { " ", "  [ ", " ] " };

                                    string[] keystring = s.Split(separatingStrings, System.StringSplitOptions.RemoveEmptyEntries);

                                    KeyCode newKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), keystring[2]);

                                    NewKey.ActionName = parsed_enum.ToString();
                                    NewKey.BoundAction = parsed_enum;
                                    NewKey.key = newKey;

                                    //Created New Key Profile
                                    PlayerInputScheme.Inputs.Add(NewKey);

                                    if (debugKeys)
                                    {
                                        print(keystring[0] + ' ' + keystring[1] + ' ' + keystring[2]);
                                    }
                                    break;
                                }
                            }
                        }

                        //print(s);
                    }
                }
            }
            //File Not Found To Be Empty
            else
            {
                //Load In Data...
                foreach (string s in readText)
                {
                    if (s != null)
                    {
                        string boundActionStringName = s.Split(new char[] { ' ' })[0];

                        if (System.Enum.TryParse<KeyActions>(boundActionStringName, out KeyActions yourEnum))
                        {
                            KeyActions parsed_enum = (KeyActions)System.Enum.Parse(typeof(KeyActions), boundActionStringName);

                            //Loop Through Default Inputs
                            for (int i = 0; i < PlayerInputScheme.DefaultInputs.Count; i++)
                            {
                                //Found A Match To Read From And Add To PlayerInputScheme.Input
                                if (PlayerInputScheme.DefaultInputs[i].BoundAction.Equals(parsed_enum))
                                {
                                    string[] separatingStrings = { " ", "  [ ", " ] " };

                                    string[] keystring = s.Split(separatingStrings, System.StringSplitOptions.RemoveEmptyEntries);

                                    KeyCode newKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), keystring[2]);

                                    InputKey NewKey = new InputKey();

                                    NewKey.ActionName = parsed_enum.ToString();
                                    NewKey.BoundAction = parsed_enum;
                                    NewKey.key = newKey;

                                    //Created New Key Profile
                                    PlayerInputScheme.Inputs.Add(NewKey);

                                    if (debugKeys)
                                    {
                                        print("Loaded In And Set Key Binding To The " + PlayerInputScheme.DefaultInputs[i].key.ToString() + " - " + PlayerInputScheme.DefaultInputs[i].BoundAction.ToString());
                                    }

                                }
                            }

                        }
                    }
                }
            }
        }
        //No Such File Exists
        else
        {
            //File Not Found
            print("Input File Not Found. Creating Keybinding File...");

            //Create Text File To Path
            File.WriteAllText(path, string.Empty);

            //Add Text To it
            foreach (InputKey Key in PlayerInputScheme.DefaultInputs)
            {
                string keycode = Key.BoundAction.ToString() + "  [ " + Key.key.ToString() + " ] " + " \n";

                KeyActions parsed_enum;
                
                File.AppendAllText(path, keycode);

                if (debugKeys)
                {
                    if (Enum.TryParse<KeyActions>(Key.BoundAction.ToString(), out parsed_enum))
                    {
                        print("The Key " + Key.BoundAction.ToString() + " has successfully parsed");
                    }
                    else
                    {
                        print("The Key " + Key.BoundAction.ToString() + " is an invalid or empty action type. Please change the enum type. This may happen due to removing an enum parameter in the Inputclass.cs script.");
                    }

                    print(keycode);
                }
            }

            // Open the file to read from.
            string[] readText = File.ReadAllLines(path);

            foreach (string s in readText)
            {
                if (s != null)
                {
                    string boundActionStringName = s.Split(new char[] { ' ' })[0];

                    if (System.Enum.TryParse<KeyActions>(boundActionStringName, out KeyActions yourEnum))
                    {
                        KeyActions parsed_enum = (KeyActions)System.Enum.Parse(typeof(KeyActions), boundActionStringName);

                        //Loop Through Default Inputs
                        for (int i = 0; i < PlayerInputScheme.DefaultInputs.Count; i++)
                        {
                            //Found A Match To Read From And Add To PlayerInputScheme.Input
                            if (PlayerInputScheme.DefaultInputs[i].BoundAction.Equals(parsed_enum))
                            {
                                InputKey NewKey = new InputKey();

                                string[] separatingStrings = { " ", "  [ ", " ] " };

                                string[] keystring = s.Split(separatingStrings, System.StringSplitOptions.RemoveEmptyEntries);

                                KeyCode newKey = (KeyCode)System.Enum.Parse(typeof(KeyCode), keystring[2]);

                                NewKey.ActionName = parsed_enum.ToString();
                                NewKey.BoundAction = parsed_enum;
                                NewKey.key = newKey;

                                //Created New Key Profile
                                PlayerInputScheme.Inputs.Add(NewKey);

                                if (debugKeys)
                                {
                                    print(keystring[0] + ' ' + keystring[1] + ' ' + keystring[2]);
                                }

                                break;
                            }
                        }
                    }

                    //print(s);
                }
            }
        }

        print("Completed Populated List For Key Bindings.");
    }

    public void SaveKeyScheme()
    {
        //Check For File
        if (File.Exists(path))
        {
            File.WriteAllText(path, "Key Bindings" + "\n \n");

            //Add Text To it
            foreach (InputKey Key in PlayerInputScheme.Inputs)
            {
                string keycode = Key.BoundAction.ToString() + "  [ " + Key.key.ToString() + " ] " + " \n";

                KeyActions parsed_enum;

                File.AppendAllText(path, keycode);

                if (debugKeys)
                {
                    if (Enum.TryParse<KeyActions>(Key.BoundAction.ToString(), out parsed_enum))
                    {
                        print("The Key " + Key.BoundAction.ToString() + " has successfully parsed");
                    }
                    else
                    {
                        print("The Key " + Key.BoundAction.ToString() + " is an invalid or empty action type. Please change the enum type. This may happen due to removing an enum parameter in the Inputclass.cs script.");
                    }

                    print(keycode);
                }
            }

            updateKeys = true;
        }
        else
        {
            //Create File Fall Back And Then Save
            print("Fall Back Method. File Not Found On Save. Creating new file and saving new data set.");
            //Create Text File To Path
            File.WriteAllText(path, "Key Bindings" + "\n \n");

            //Add Text To it
            foreach (InputKey Key in PlayerInputScheme.Inputs)
            {
                string keycode = Key.BoundAction.ToString() + "  [ " + Key.key.ToString() + " ] " + " \n";
                print(keycode);
                File.AppendAllText(path, keycode);
            }
            updateKeys = true;
        }
    }
    public void UpdateIndexes()
    {
        if (updateKeys)
        {
            Story story = new Story(DialogueManager.Instance.globalsInkFile.text);
            print(story.currentText);
            DialogueManager.Instance.dialogueVariables.StartListening(story);

            for (int i = 0; i < PlayerInputScheme.Inputs.Count; i++)
            {
                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.WalkForward))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["ForwardKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.WalkBackwards))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["BackwardKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.WalkRight))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["RightKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();


                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.WalkLeft))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["LeftKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.SkillA))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["SkillAKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.SkillB))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["SkillBKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.SkillC))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["SkillCKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.SkillD))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["SkillDKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.Dodge))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["DodgeKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.Crouch))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["SlideKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.TargetEnemy))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["TargetEnemyKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.Jump))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["JumpKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.Grapple))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["GrappleKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.LightAttack))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["LightAttackKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.HeavyAttack))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["HeavyAttackKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();


                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.Interact))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["InteractKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.EscapeMenu))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["EscapeKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.Guard))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["GuardKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.ResetCamera))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["ResetCameraKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }

                if (PlayerInputScheme.Inputs[i].BoundAction.Equals(KeyActions.Nimbus))
                {
                    PlayerInputScheme.Inputs[i].ActionName = PlayerInputScheme.Inputs[i].BoundAction.ToString();
                    story.variablesState["NimbusKey"] = InputManager.Instance.PlayerInputScheme.Inputs[i].key.ToString();

                    PlayerInputScheme.DefaultInputs[i].ActionName = PlayerInputScheme.DefaultInputs[i].BoundAction.ToString();
                }
            }

            DialogueManager.Instance.dialogueVariables.StopListening(story);
            print("Completed Index Update.");
            updateKeys = false;
        }
    }
    void KeyMovement()
    {
        Vector3 inputDir = PlayerInputScheme.GetInputMovementDirection(PlayerInputScheme.reverseInput);

        // Smooth acceleration & deceleration for non-controller
        if (!PlayerInputScheme.NintendoSwitchProController.Player.enabled)
        {
            PlayerInputScheme.Horizontal = Mathf.MoveTowards(PlayerInputScheme.Horizontal, inputDir.x, Time.deltaTime * (inputDir.x == 0 ? PlayerInputScheme.deacceleration : PlayerInputScheme.acceleration));
            PlayerInputScheme.Vertical = Mathf.MoveTowards(PlayerInputScheme.Vertical, inputDir.z, Time.deltaTime * (inputDir.z == 0 ? PlayerInputScheme.deacceleration : PlayerInputScheme.acceleration));
            PlayerInputScheme.Elevation = Mathf.MoveTowards(PlayerInputScheme.Elevation, inputDir.y, Time.deltaTime * (inputDir.y == 0 ? PlayerInputScheme.deacceleration : PlayerInputScheme.acceleration));
        }
        else
        {
            PlayerInputScheme.Horizontal = inputDir.x;
            PlayerInputScheme.Vertical = inputDir.z;
            PlayerInputScheme.Elevation = inputDir.y;
        }

        PlayerInputScheme.MovementVector = new Vector3(PlayerInputScheme.Horizontal, PlayerInputScheme.Elevation, PlayerInputScheme.Vertical);
    }


    void DebugKeys()
    {
        if (debugKeys)
        {
            foreach (KeyCode vKey in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKey(vKey))
                {
                    //your code here
                    print(vKey.ToString());
                }
            }
        }
    }

    private int conflictIndex = 0;
    private InputKey conflictKey = new InputKey();
    private bool searchForConflict = false;
    public void SetNewInput(int newkeyIndex, InputKey Newkey)
    {
        foreach (KeyCode newKey in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKeyDown(newKey))
            {
                //Add New Key To Action Binding
                PlayerInputScheme.Inputs[newkeyIndex].key = newKey;
                
                UIManager.Instance.isListeningForKey = false;
            }
        }

        //Clear Conflicts
        conflictIndex = newkeyIndex;
        conflictKey.ActionName = PlayerInputScheme.Inputs[newkeyIndex].ActionName;
        conflictKey.key = Newkey.key;
        searchForConflict = true;
    }

    private void CheckForInvalidInputs(InputKey NewKey, int newkeyIndex)
    {
        for(int i = 0; i < PlayerInputScheme.Inputs.Count; i++)
        {
            //This catches duplicate uses of the same key and prevents them by deleting the duplicate found and keybinding this one instead of leaving it null
            if(PlayerInputScheme.Inputs[i].key == NewKey.key && PlayerInputScheme.Inputs[i].ActionName != NewKey.ActionName)
            {
                PlayerInputScheme.Inputs[i].key = KeyCode.None;
                UIManager.Instance.ClearConflictKey(i);
                searchForConflict = false;
                return;

            }
        }

        searchForConflict = false;
    }

    // Called when the singleton is created *or* after a domain reload in the editor.
    protected virtual void OnEnable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        TryRebuildIndexing();
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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void OnAfterDomainReload()
    {
        if (Instance != null && Instance.PlayerInputScheme != null)
        {
            InputManager.Instance.BuildUniqueKeyIndexing();
            Debug.Log("Rebuilt InputKeyIndexLib dictionary after domain reload.");
        }
    }

    
}

