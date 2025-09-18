using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ink.Runtime;
using DialogueClass;

public class NPC : MonoBehaviour
{
    public bool isCommunicating = false;
    public bool shouldFaceObjects = true;
    public bool shouldLimitMovement = true;
    public bool customCameraPostioning = true;
    public string NPCName;
    public int optionalSignPostIndex = 0;
    public TextAsset DialogueFile;
    public int chosenCameraShotIndex = -1;
    public CameraDialogueData DialogueCameraShots;
    public GameObject _player;

    public bool intialize = true;

    void Start()
    {
        baseRotation = transform.rotation;
    }

    
    void Update()
    {
        Localize();
        FaceSpeaker();
    }

    private void Localize()
    {
        //Runs Once Intializes the loop and all of its dialogue Points
        if (intialize)
        {
            foreach (CameraDialoguePositions Position in DialogueCameraShots.DialoguePoints)
            {
                Position.Speakers.Clear();
            }
            
            SetSimpleCameraShotData();

            if (DialogueCameraShots.DialoguePoints != null)
            {
                for (int i = 0; i < DialogueCameraShots.DialoguePoints.Count; i++)
                {
                    if (DialogueCameraShots.DialoguePoints[i].PositionType.Equals(DialogueCameraPositionType.Local))
                    {
                        DialogueCameraShots.DialoguePoints[i].LocalizedParent = transform;
                    }
                }
            }
        }

        intialize = false;
    }
    private Quaternion baseRotation;
    private Quaternion targetRotation;

    private void FaceSpeaker()
    {
        if (_player != null && isCommunicating && shouldFaceObjects)
        {
            Vector3 lookDir = -transform.position - -_player.transform.position;
            lookDir.y = 0;

            Quaternion q = Quaternion.LookRotation(lookDir);

            if (Quaternion.Angle(q, baseRotation) <= 180)
            {
                targetRotation = q;
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 2 * Time.deltaTime);
        }
    }

    public void StartConversation(GameObject Player)
    {
        isCommunicating = true;
        _player = Player;

        DialogueManager.Instance.Character = GetComponent<NPC>();

        if(shouldLimitMovement)
        {
            _player.GetComponent<PlayerDriver>().physicsProperties.movementLock = true;
            _player.GetComponent<PlayerDriver>().physicsProperties.turnLock = true;
        }

        DialogueManager.Instance.BeginDialogue(DialogueFile);

        if (customCameraPostioning)
        {
            _player.GetComponent<PlayerDriver>().MyCamera.AdvanceDialougeCamera(DialogueCameraShots.DialoguePoints[chosenCameraShotIndex]);
        }
    }

    public void EndConversation()
    {
        isCommunicating = false;

        if (shouldLimitMovement)
        {
            _player.GetComponent<PlayerDriver>().physicsProperties.movementLock = false;
            _player.GetComponent<PlayerDriver>().physicsProperties.turnLock = false;
        }

        if (customCameraPostioning)
        {
            _player.GetComponent<PlayerDriver>().MyCamera.EndDialogueCamera();
        }

        intialize = true;
    }

    private void SetSimpleCameraShotData()
    {
        if(DialogueCameraShots.DialoguePoints != null)
        {
            foreach (CameraDialoguePositions Position in DialogueCameraShots.DialoguePoints)
            {
                Position.Speakers.Add(gameObject);
            }
        }
    }
}
