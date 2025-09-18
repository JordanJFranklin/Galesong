using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Ink.Runtime;
using TMPro;
using UnityEngine.EventSystems;
using System;

public class DialogueManager : MonoBehaviour
{
    [Header("NPC")]
    public bool isDialoguePlaying = false;
    public bool makeDecision = false;
    public Story currentStory;
    public float typingSpeed = 0.05f;
    public NPC Character;
    [Header("Objects")]
    public GameObject DialogueWindow;
    public TextMeshProUGUI DialogueText;
    public TextMeshProUGUI SpeakerText;
    public List<GameObject> Choices;
    public List<TextMeshProUGUI> ChoiceTexts;
    [Header("Global Ink File")]
    [SerializeField] public TextAsset globalsInkFile;

    public DialogueVariables dialogueVariables;
    private static DialogueManager _instance;
    private bool canContinueToNextLine = false;
    private const string SPEAKER_TAG = "speaker";
    private const string POSITION_TAG = "pos";
    private const string lOAD_SCENE_TAG = "scene";
    private const string CAMERA_VIEW_TAG = "camera";
    private const string GIVE_INVENTORY_ITEM = "invItem";
    private const string GIVE_MOVEMENT_POWER = "grantmovepow";
    private const string LOCALIZED_CAMERA_PARENT_TAG = "localizedCamera"; //Set Localized Speaker
    private const string LOCALIZED_CAMERA_PARENTTYPE_TAG = "speakerType";
    private const string SPEAKER_TYPE_ADDITIVE_TAG = "add";
    private const string SPEAKER_TYPE_REPLACE_TAG = "replace";
    //This will expand based on the amount of other npc's added to the game
    private const string LOCALIZED_CAMERA_PLAYER_TAG = "player";

    static bool _destroyed;

    public static DialogueManager Instance
    {
        get
        {
            // Prevent re-creation of the singleton during play mode exit.
            if (_destroyed) return null;

            // If the instance is already valid, return it. Needed if called from a
            // derived class that wishes to ensure the instance is initialized.
            if (_instance != null) return _instance;

            // Find the existing instance (across domain reloads).
            if ((_instance = FindObjectOfType<DialogueManager>()) != null) return _instance;

            // Create a new GameObject instance to hold the singleton component.
            var gameObject = new GameObject(typeof(DialogueManager).Name);

            // Move the instance to the DontDestroyOnLoad scene to prevent it from
            // being destroyed when the current scene is unloaded.
            DontDestroyOnLoad(gameObject);

            // Create the MonoBehavior component. Awake() will assign _instance.
            return gameObject.AddComponent<DialogueManager>();
        }

    }

    void Awake()
    {
        Debug.Assert(_instance == null || _instance == this, "More than one singleton instance instantiated!", this);

        if (_instance == null || _instance == this)
        {
            _instance = this;
        }

        dialogueVariables = new DialogueVariables(globalsInkFile);
    }


    // Start is called before the first frame update
    void Start()
    {
        if (DialogueWindow != null)
        {
            DialogueWindow.SetActive(false);
        }

        isDialoguePlaying = false;
    }

    // Update is called once per frame
    void Update()
    {
        ContinueStory();

        if (DialogueText != null && DialogueText.text == "\n")
        {
            StopAllCoroutines();
            StartCoroutine(TypeSentence(currentStory.Continue()));
            HandleTags(currentStory.currentTags);
            Character._player.GetComponent<PlayerDriver>().MyCamera.AdvanceDialougeCamera(Character.DialogueCameraShots.DialoguePoints[Character.chosenCameraShotIndex]);
        }
    }

    //Check Input Manager to see how to change properites/attritubutes in the game through Ink file VAR properties
    public void BeginDialogue(TextAsset inkJSON)
    {
        DialogueWindow.SetActive(true);
        currentStory = new Story(inkJSON.text);
        isDialoguePlaying = true;
        dialogueVariables.StartListening(currentStory);

        //This supports signposts
        if(currentStory.variablesState["SignIndex"] != null)
        {
            currentStory.variablesState["SignIndex"] = Character.optionalSignPostIndex;
        }

        StopAllCoroutines();
        StartCoroutine(TypeSentence(currentStory.Continue()));
        HandleTags(currentStory.currentTags);

        if (Character.customCameraPostioning)
        {
            Character._player.GetComponent<PlayerDriver>().MyCamera.AdvanceDialougeCamera(Character.DialogueCameraShots.DialoguePoints[Character.chosenCameraShotIndex]);
        }
    }
    private string currentScene;
    private void HandleTags(List<string> currentTags)
    {
        foreach (string tag in currentTags)
        {
            //Parse seperators
            string[] splitTag = tag.Split(':', ',');

            if(splitTag.Length != 2)
            {
                Debug.LogError("Tag could not be appropriately parsed: " + tag);
            }

            string tagKey = splitTag[0].Trim();
            string tagValue = splitTag[1].Trim();
            string parenttype = "";

            switch(tagKey)
            {
                case SPEAKER_TAG:
                    Debug.Log(SPEAKER_TAG + tagValue);
                    SpeakerText.text = tagValue;
                    break;
                case CAMERA_VIEW_TAG:
                    Debug.Log(CAMERA_VIEW_TAG + tagValue);
                    Character.chosenCameraShotIndex = Int32.Parse(tagValue);
                    break;
                case LOCALIZED_CAMERA_PARENT_TAG:
                    Debug.Log(LOCALIZED_CAMERA_PARENT_TAG + tagValue);
                    
                    //Add Player If it is not in
                    if(tagValue == LOCALIZED_CAMERA_PLAYER_TAG && !Character.DialogueCameraShots.DialoguePoints[Character.chosenCameraShotIndex].Speakers.Contains(gameObject))
                    {
                        Character.DialogueCameraShots.DialoguePoints[Character.chosenCameraShotIndex].LocalizedParent = gameObject.transform;
                        parenttype = LOCALIZED_CAMERA_PLAYER_TAG;
                    }

                    break;
                case LOCALIZED_CAMERA_PARENTTYPE_TAG:
                    Debug.Log(LOCALIZED_CAMERA_PARENTTYPE_TAG + tagValue);
                    if (parenttype == LOCALIZED_CAMERA_PLAYER_TAG && tagValue == SPEAKER_TYPE_ADDITIVE_TAG)
                    {
                        Character.DialogueCameraShots.DialoguePoints[Character.chosenCameraShotIndex].Speakers.Add(gameObject);
                    }

                    if(parenttype == LOCALIZED_CAMERA_PLAYER_TAG && tagValue == SPEAKER_TYPE_REPLACE_TAG)
                    {
                        Character.DialogueCameraShots.DialoguePoints[Character.chosenCameraShotIndex].Speakers[0] = gameObject;
                    }
                    break;
                case GIVE_INVENTORY_ITEM:
                    
                    break;

                case lOAD_SCENE_TAG:
                    DontDestroyOnLoad(gameObject);

                    GetComponent<PlayerDriver>().MyCamera.Rotater.SetParent(transform);
                    GetComponent<PlayerDriver>().MyCamera.transform.SetParent(GetComponent<PlayerDriver>().MyCamera.Rotater);
                    GetComponent<PlayerDriver>().MyCamera.GeneralLook.SetParent(transform);

                    DontDestroyOnLoad(GetComponent<PlayerDriver>().MyCamera.Rotater);
                    DontDestroyOnLoad(GetComponent<PlayerDriver>().MyCamera.GeneralLook);
                    DontDestroyOnLoad(GetComponent<PlayerDriver>().MyCamera.GameCamera);

                    SceneManager.LoadScene(tagValue, LoadSceneMode.Single);

                    if(SceneManager.GetActiveScene() == SceneManager.GetSceneByName(tagValue))
                    {
                        Debug.Log("New Scene Loaded.");
                        currentScene = tagValue;
                        ExitDialogueMode();
                    }

                    break;

                case POSITION_TAG:
                    string x = splitTag[1].Trim();
                    string y = splitTag[2].Trim();
                    string z = splitTag[3].Trim();

                    transform.position = new Vector3(Int32.Parse(x), Int32.Parse(y), Int32.Parse(z));
                    break;

                default:
                    Debug.LogError("Tag came in but is not currently being handled: " + tag);
                    break;
            }
        }
    }

    public void SelectChoice(int index)
    {
        if (canContinueToNextLine)
        {
            currentStory.ChooseChoiceIndex(index);
            makeDecision = false;
            StopAllCoroutines();
            StartCoroutine(TypeSentence(currentStory.Continue()));
            HandleTags(currentStory.currentTags);
            Character._player.GetComponent<PlayerDriver>().MyCamera.AdvanceDialougeCamera(Character.DialogueCameraShots.DialoguePoints[Character.chosenCameraShotIndex]);
            moveStoryOnce = true;
        }
    }

    private bool moveStoryOnce = false;
    public void ContinueStory()
    {
        if (currentStory != null && currentStory.currentChoices.Count == 0 && canContinueToNextLine && isDialoguePlaying && currentStory.canContinue && !makeDecision && Input.GetKeyDown(InputManager.Instance.PlayerInputScheme.Inputs[InputManager.Instance.PlayerInputScheme.InputKeyIndexLib[KeyActions.LightAttack]].key))
        {
            StopAllCoroutines();
            StartCoroutine(TypeSentence(currentStory.Continue()));
            HandleTags(currentStory.currentTags);

            if (Character.customCameraPostioning)
            {
                Character._player.GetComponent<PlayerDriver>().MyCamera.AdvanceDialougeCamera(Character.DialogueCameraShots.DialoguePoints[Character.chosenCameraShotIndex]);
            }

            moveStoryOnce = true;
        }

        if (Input.GetKeyUp(InputManager.Instance.PlayerInputScheme.Inputs[InputManager.Instance.PlayerInputScheme.InputKeyIndexLib[KeyActions.LightAttack]].key))
        {
            moveStoryOnce = false;
        }

        if (currentStory != null && currentStory.currentChoices.Count == 0 && !moveStoryOnce && canContinueToNextLine && isDialoguePlaying && !currentStory.canContinue && !makeDecision && Input.GetKeyDown(InputManager.Instance.PlayerInputScheme.Inputs[InputManager.Instance.PlayerInputScheme.InputKeyIndexLib[KeyActions.LightAttack]].key))
        {
            //Story End
            ExitDialogueMode();
        }
    }

    IEnumerator TypeSentence(string sentence)
    {
        DialogueText.text = sentence;
        DialogueText.maxVisibleCharacters = 0;

        canContinueToNextLine = false;

        HideChoices();

        bool isAddingRichTextTag = false;


        foreach (char letter in sentence.ToCharArray())
        {
            if(Input.GetKeyDown(InputManager.Instance.PlayerInputScheme.Inputs[InputManager.Instance.PlayerInputScheme.InputKeyIndexLib[KeyActions.LightAttack]].key))
            {
                DialogueText.maxVisibleCharacters = sentence.Length;
                break;
            }

            if (letter == '<' || isAddingRichTextTag)
            {
                isAddingRichTextTag = true;
                DialogueText.text += letter;

                if (letter == '>')
                {
                    isAddingRichTextTag = false;
                }
            }
            else
            {
                DialogueText.maxVisibleCharacters++;
                yield return new WaitForSeconds(typingSpeed);
            }
        }

        DisplayChoices();

        canContinueToNextLine = true;
    }

    private void HideChoices()
    {
        foreach(GameObject choice in Choices)
        {
            choice.SetActive(false);
        }
    }

    public void ExitDialogueMode()
    {
        DialogueWindow.SetActive(false);
        dialogueVariables.SaveGlobalVariables();

        if (Character != null)
        {
            Character.EndConversation();
        }

        dialogueVariables.StopListening(currentStory);
        isDialoguePlaying = false;
        PlayerSettings.Instance.gameplaySettings.Mode = CameraMode.UnTargetedMode;

        GetComponent<PlayerDriver>().physicsProperties.movementLock = false;
        GetComponent<PlayerDriver>().physicsProperties.turnLock = false;

        print("The Dialogue Has Concluded");
    }

    public void DisplayChoices()
    {
        List<Choice> currentChoices = currentStory.currentChoices;

        if(currentChoices.Count > Choices.Count)
        {
            Debug.LogError("Too Many Choices For UI To Support.");
        }

        int index = 0;

        foreach(Choice choice in currentChoices)
        {
            makeDecision = true;
            Choices[index].gameObject.SetActive(true);
            ChoiceTexts[index].text = choice.text;
            index++;
        }

        for(int i = index; i < Choices.Count; i++)
        {
            Choices[i].gameObject.SetActive(false);
        }
    }

    public Ink.Runtime.Object GetVariableState(string variableName)
    {
        Ink.Runtime.Object variableValue = null;
        dialogueVariables.variables.TryGetValue(variableName, out variableValue);

        if(variableValue == null)
        {
            Debug.LogWarning("Ink Variable was found to be null: " + variableName);
        }
        else
        {
            Debug.Log(variableName + " - has returned " + variableValue);
        }
        return variableValue;
    }

    // Called when the singleton is created *or* after a domain reload in the editor.
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

