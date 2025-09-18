using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum DialogueCameraMovementType {Instant, Interpolated}
public enum DialogueCameraPositionType { World, Local }
public enum DialogueCameraRotationType {Instant, Interpolated}
public enum DialogueCameraRotationStyle {Given, LookToSpeakers}
public enum DialogueCameraFOVType{Default, Static, Dynamic}

namespace DialogueClass
{
    [System.Serializable]
    public class CameraDialogueData
    {
        public List<CameraDialoguePositions> DialoguePoints;
    }

    [System.Serializable]
    public class CameraDialoguePositions
    {
        public string PositionName;        
        [Header("Speakers")]
        public List<GameObject> Speakers;
        [Header("Field Of View")]
        public DialogueCameraFOVType FOVType;
        public float CameraFOV = 0;
        public float CameraFOVZoomSpeed = 1;
        [Header("Position")]
        public Transform LocalizedParent;
        public DialogueCameraPositionType PositionType;
        public DialogueCameraMovementType MovementType;
        public float PosLerpSpeed;
        public Vector3 Position;
        [Header("Rotation")]
        public DialogueCameraRotationType RotationType;
        public DialogueCameraRotationStyle RotationStyle;
        public float RotLerpSpeed;
        public Quaternion GivenLookDirection;
        [Header("Animations")]
        public AnimationClip Animation;
    }
}
