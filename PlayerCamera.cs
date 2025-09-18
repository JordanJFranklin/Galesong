using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameSettings;
using InputKeys;
using DialogueClass;
using Unity.VisualScripting;
using UnityEngine.InputSystem;
using TMPro;
using UnityEditor;
using System.Linq;
using static UnityEngine.GraphicsBuffer;

public enum GrappleCameraState
{
    GrappleShot,   // Lock-on to grapple point
    ReelObject     // Reel a target to you
}

public class PlayerCamera : MonoBehaviour
{
    #region Basic Settings
    [Header("Basic Settings")]

    [Tooltip("LayerMask used for wall collision detection.")]
    public LayerMask Wall;

    [Tooltip("Current vertical angle of the camera.")]
    public float YHeight;

    [Tooltip("Minimum vertical angle allowed during platforming.")]
    public float PlatformingMinHeight;

    [Tooltip("Maximum vertical angle allowed during platforming.")]
    public float PlatformingMaxHeight;

    [Tooltip("Current horizontal angle of the camera.")]
    public float XAngle;

    [Tooltip("Minimum clamp for vertical angle.")]
    public float MinYAngle = -180;

    [Tooltip("Maximum clamp for vertical angle.")]
    public float MaxYAngle = 180;

    [Tooltip("Current zoom offset (negative = zoom out, positive = zoom in).")]
    public float CurrZoom = 0;

    [Tooltip("Additional zoom applied when grinding.")]
    public float GrindZoomOutValue = 10;

    [Tooltip("Maximum zoom distance.")]
    public float ZoomMax = 5;

    [Tooltip("Minimum zoom distance.")]
    public float ZoomMin = -5;

    [Tooltip("Speed at which the zoom changes.")]
    public float ZoomSpeed = 2f;

    [Tooltip("How fast the camera interpolates during platforming.")]
    public float PlatformingCameraSpeed = 1.5f;

    [Tooltip("How fast the rotater interpolates during platforming.")]
    public float PlatformingRotaterSpeed = 2f;

    [Tooltip("Smoothing for direction changes during platforming.")]
    public float PlatformingDirectionSmoothing = 2f;

    [Tooltip("Smoothing for look changes during platforming.")]
    public float PlatformingLookSmoothing = 2f;

    [Tooltip("Minimum viewport X/Y bounds for camera movement.")]
    public Vector2 cameraBoundsMin;

    [Tooltip("Maximum viewport X/Y bounds for camera movement.")]
    public Vector2 cameraBoundsMax;

    [Tooltip("Horizontal offset amount when strafing left/right.")]
    public float LeftOffsetAmount = 3.5f;

    [Tooltip("Speed at which offset blends in/out.")]
    public float OffsetBlendSpeed = 2f;

    [Tooltip("Blend factor for offset (0=no offset, 1=full offset).")]
    [Range(0, 1)]
    public float OffsetBlendValue = 1f;

    [Tooltip("Last Y position when the camera was grounded.")]
    public float lastGroundedY;

    [Tooltip("Actual Y offset currently applied to the camera.")]
    public float cameraYOffset;

    [Tooltip("Height difference threshold before the camera follows vertically.")]
    public float yDeadzoneThreshold = 2f;

    [Tooltip("Speed at which the camera follows vertically.")]
    public float yFollowSpeed = 3f;

    [Tooltip("Difference between desired and current height.")]
    public float heightDiff;

    [Tooltip("Scale of the transition zone (0=no transition, 1=full).")]
    [Range(0, 1)]
    public float transitionZoneScale = 1f;

    [Tooltip("Distance ahead of the player to look when moving.")]
    public float lookAheadDistance = 2f;

    [Tooltip("Smoothing factor for look-ahead movement.")]
    public float lookAheadSmoothing = 5f;

    [Tooltip("Current runtime look-ahead offset.")]
    public Vector3 currentLookOffset;

    [Tooltip("Speed of recovery for vertical follow when coming back in range.")]
    public float yFollowRecoverySpeed = 5f;

    [Tooltip("Duration over which the camera drains vertical offset.")]
    public float yFollowRecoveryDuration = 0.5f;
    #endregion

    #region Grapple Camera
    [Header("Grapple Camera")]

    [Tooltip("State of the grapple camera.")]
    public GrappleCameraState grappleCamState = GrappleCameraState.GrappleShot;

    [Tooltip("World position of the grapple target.")]
    public Vector3 GrapplePos;

    [Tooltip("Horizontal rotation angle during grapple.")]
    public float grappleXAngle;

    [Tooltip("Maximum distance for grapple camera effect.")]
    public float maxGrappleDist = 2.5f;
    #endregion

    #region Lock-On Settings
    [Header("Lock-On Debug")]
    public bool showTargetBoundsGizmo = true; // Toggle for Gizmo visualization

    [Header("Lock On Settings")]
    public float LockOnDistance = 100f;
    [SerializeField] float baseDistanceBehind = 4f;
    [SerializeField] float maxDistanceBehind = 9f;
    [SerializeField] float minDistanceBehind = 9f;
    public float maxZoomDistance;
    [SerializeField] public float distanceBehind = 6f;
    [SerializeField] public float heightOffset = 2.5f;
    [SerializeField] public float sideOffsetAmount = 1.5f; // New: how far left/right to offset camera
    [SerializeField] public float cameraSmoothSpeed = 6f;  // New: for Lerp smoothing
    public GameObject topBar;
    public GameObject bottomBar;
    public float widescreenHeight = 0.6f; // Smaller viewport height (0-1), e.g. 0.6 for letterbox
    public float normalHeight = 1f;       // Full height for normal view
    [SerializeField][Range(0f, 0.4f)] private float barHeightRatio = 0.1f; // 10% of screen
    [SerializeField] private AnimationCurve heightFollowCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float ySmoothTime = 0.25f;

    [Tooltip("Whether the lock-on key is currently pressed.")]
    public bool keyPressed = false;

    [Tooltip("Are we currently locked on to a target?")]
    public bool isLockedOn;

    [Tooltip("The main camera GameObject for lock-on transitions.")]
    public GameObject GameCamera;

    [Tooltip("Current lock-on target GameObject.")]
    public GameObject LockOnTarget;

    [Tooltip("LayerMask of valid lock-on targets.")]
    public LayerMask TargetLayers;



    private List<Transform> validTargets = new List<Transform>();
    #endregion

    #region Dialogue Settings
    [Header("Dialogue Settings")]

    [Tooltip("Transform used as the pivot for general look in dialogue.")]
    public Transform GeneralLook;

    [Tooltip("Data asset containing camera overrides for dialogues.")]
    public CameraDialogueData CameraData;
    #endregion

    #region Position Offset Settings
    [Header("Position Offset Settings")]

    [Tooltip("Local offset for platforming mode.")]
    public Vector3 PlatformingPos;

    [Tooltip("Local offset when aiming.")]
    public Vector3 AimCameraPos;

    [Tooltip("Offset applied when looking up.")]
    public Vector3 upLookCameraOffset;

    [Tooltip("Offset applied when looking down.")]
    public Vector3 downLookCameraOffset;

    [Tooltip("Local rotater position for platforming.")]
    public Vector3 PlatformingRotaterPos;

    [Tooltip("Offset for the camera during grapple mode.")]
    public Vector3 grappleCameraOffset;

    [Tooltip("Base offset for lock-on camera.")]
    public Vector3 LockOnCameraPos;

    [Tooltip("Additional adjustment to lock-on camera offset.")]
    public Vector3 LockOnAdjustmentPos;

    [Tooltip("First-person camera position offset.")]
    public Vector3 CameraFPSPos;

    [Tooltip("Offset for Tempest Kick start.")]
    public Vector3 TempestPos;

    [Tooltip("Follow-up offset during Tempest Kick.")]
    public Vector3 TempestFollowOffset;

    [Tooltip("End offset when Tempest Kick finishes.")]
    public Vector3 TempestEndOffset;

    [Tooltip("Offset for Skyward Ascent start.")]
    public Vector3 SkywardAscentPos;

    [Tooltip("Follow-up offset during Skyward Ascent.")]
    public Vector3 SkywardAscentFollowOffset;

    [Tooltip("End offset when Skyward Ascent completes.")]
    public Vector3 SkywardAscentEndOffset;
    #endregion

    #region Internals & References
    // Used internally to calculate height and rotations
    private Vector3 heightVector;
    private float pivotHeight;
    private float timeAdjust = 0;

    [Tooltip("Reference to the PlayerDriver for stats and movement.")]
    public PlayerDriver Player;

    [Tooltip("Transform around which the camera orbits.")]
    public Transform Rotater;

    [Tooltip("Flag to force camera reset on next frame.")]
    public bool AdjustCamera = false;

    [Tooltip("Flag to cancel grapple hook.")]
    public bool CancelHook = false;

    private bool forceCameraReset = false;
    private bool cancelLockOn = false;
    #endregion

    //Convert Manager To Singleton
    private static PlayerCamera _instance;
    static bool _destroyed;
    public static PlayerCamera Instance
    {
        get
        {
            // Prevent re-creation of the singleton during play mode exit.
            if (_destroyed) return null;

            // If the instance is already valid, return it. Needed if called from a
            // derived class that wishes to ensure the instance is initialized.
            if (_instance != null) return _instance;

            // Find the existing instance (across domain reloads).
            if ((_instance = FindObjectOfType<PlayerCamera>()) != null) return _instance;

            // Create a new GameObject instance to hold the singleton component.
            var gameObject = new GameObject(typeof(PlayerCamera).Name);

            // Move the instance to the DontDestroyOnLoad scene to prevent it from
            // being destroyed when the current scene is unloaded.
            DontDestroyOnLoad(gameObject);

            // Create the MonoBehavior component. Awake() will assign _instance.
            return gameObject.AddComponent<PlayerCamera>();
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

    void Start()
    {
        baseRotation = transform.rotation;
    }

    private void Update()
    {
        if (isLockedOn)
        {
            EnableLetterbox();
        }
        else
        {
            DisableLetterbox();
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        CameraModes();
    }

    //Camera Motor
    private void CameraModes()
    {
        var gameplayMode = PlayerSettings.Instance.gameplaySettings.Mode;

        if (gameplayMode == CameraMode.UnTargetedMode)
        {
            if (PlayerDriver.Instance.physicsProperties.preparingTempestKick || PlayerDriver.Instance.physicsProperties.isTempestKicking)
            {
                TempestKickCamera();
            }

            if (PlayerDriver.Instance.physicsProperties.preparingSkywardAscent || PlayerDriver.Instance.physicsProperties.isSkywardAscending)
            {
                SkywardAscentCamera();
            }
            if (!PlayerDriver.Instance.physicsProperties.preparingSkywardAscent && !PlayerDriver.Instance.physicsProperties.isSkywardAscending && !PlayerDriver.Instance.physicsProperties.isTempestKicking && !PlayerDriver.Instance.physicsProperties.preparingTempestKick)
            {
                UnTargetedMode();
                ScreenInfluencedTargeting();
            }
        }

        if (gameplayMode == CameraMode.TargetMode)
        {

            TargetMode();
            HandleTargetSwitching();
        }

        if (gameplayMode == CameraMode.GrappleMode)
        {
            GrappleCamera();
        }

        if (gameplayMode == CameraMode.FPSMode)
        {
            FirstPersonMode();
        }

        if (gameplayMode == CameraMode.DialogueMode)
        {
            DialougeCamera();
        }
    }

    private void GrappleCamera()
    {
        if (Player.physicsProperties.GrappleObject)
        {
            forceCameraReset = false;

            PlayerDriver.Instance.SetMoveType(MovementType.FPSMove);

            // Parent setup
            Rotater.SetParent(null);
            transform.SetParent(Rotater);
            GameCamera.transform.SetParent(Rotater);

            // Optional: Lock X/Y input during grapple
            float dist = -DistanceCalculation(Player.transform, Player.physicsProperties.GrappleObject) * 0.2f;
            dist = Mathf.Clamp(dist, -maxGrappleDist, -2f);

            // Define your offset relative to the Rotater
            Vector3 offset = new Vector3(grappleCameraOffset.x, grappleCameraOffset.y, grappleCameraOffset.z + dist);
            Vector3 targetCameraLocalPos = offset + LockOnCameraPos;

            // Set the Rotater position between player and grapple point
            Rotater.position = (Player.transform.position - Player.physicsProperties.GrappleObject.position) / 2f;


            // Smoothly rotate Rotater
            Quaternion targetAngle = Quaternion.Euler(YHeight, grappleXAngle, 0f);
            Rotater.rotation = Quaternion.Lerp(Rotater.rotation, targetAngle, PlatformingRotaterSpeed * Time.deltaTime * 2f);

            // Move main camera transform to the offset position
            transform.localPosition = Vector3.Lerp(transform.localPosition, targetCameraLocalPos, PlatformingCameraSpeed * Time.deltaTime * 2f);
            transform.rotation = Quaternion.LookRotation(Player.physicsProperties.GrappleVector - transform.position);

            // Collision Handling
            if (Physics.Linecast(Rotater.position, transform.position, out hit, Wall))
            {
                if (hit.collider.CompareTag("Wall"))
                {
                    Vector3 adjustedCollisionOffset = transform.TransformDirection(new Vector3(0, 0, -0.4f * DistanceCalculation(Player.transform, Rotater)));
                    GameCamera.transform.position = Vector3.Lerp(GameCamera.transform.position, hit.point + adjustedCollisionOffset, PlatformingCameraSpeed * Time.deltaTime * 2f);
                }
            }
            else
            {
                // Snap camera to its intended local position
                GameCamera.transform.localPosition = Vector3.Lerp(GameCamera.transform.localPosition, transform.localPosition, PlatformingCameraSpeed * Time.deltaTime * 2f);
            }

            GameCamera.transform.LookAt(Player.physicsProperties.GrappleObject);
        }
    }

    //Create Dialogue Camera 
    //Look Into Camera Transitions experimental
    public void AdvanceDialougeCamera(CameraDialoguePositions CameraShot)
    {
        if (CameraData.DialoguePoints != null)
        {
            CameraData.DialoguePoints.Clear();
            CameraData.DialoguePoints.Add(CameraShot);
            Player.GetComponent<PlayerDriver>().physicsProperties.movementLock = true;
            CameraData.DialoguePoints[0] = CameraShot;
            PlayerSettings.Instance.gameplaySettings.Mode = CameraMode.DialogueMode;
        }
        else
        {
            Debug.LogError("Dialogue Camera Shot is empty.");
        }
    }

    public void EndDialogueCamera()
    {
        Player.GetComponent<PlayerDriver>().physicsProperties.movementLock = false;
        isLockedOn = false;
        GameCamera.GetComponent<Animation>().enabled = false;
        CameraData.DialoguePoints.Clear();
        PlayerSettings.Instance.gameplaySettings.Mode = CameraMode.UnTargetedMode;
        print("Returning Camera To Normal. Conversation Ended");
    }

    private Quaternion baseRotation;
    private Quaternion targetRotation;
    private void DialougeCamera()
    {
        //Look Point Parenting
        GeneralLook.SetParent(null);

        GameCamera.transform.rotation = transform.rotation;
        GameCamera.transform.position = transform.position;

        if (CameraData.DialoguePoints.Count > 0 && CameraData.DialoguePoints[0] != null)
        {
            if (CameraData.DialoguePoints[0].LocalizedParent != null && CameraData.DialoguePoints[0].PositionType.Equals(DialogueCameraPositionType.Local))
            {
                Rotater.SetParent(CameraData.DialoguePoints[0].LocalizedParent);
                transform.SetParent(Rotater);
            }
            else
            {
                Rotater.SetParent(Player.transform);
                transform.SetParent(null);
            }

            //Animation
            if (GameCamera.GetComponent<Animation>() != null)
            {
                GameCamera.GetComponent<Animation>().enabled = true;

                if (CameraData.DialoguePoints[0].Animation != null)
                {
                    CameraData.DialoguePoints[0].Animation.legacy = true;
                    GameCamera.GetComponent<Animation>().AddClip(CameraData.DialoguePoints[0].Animation, "Camera Animation");
                    GameCamera.GetComponent<Animation>().Play("Camera Animation");
                }
            }

            //FOV Handling
            if (CameraData.DialoguePoints[0].FOVType.Equals(DialogueCameraFOVType.Default))
            {
                GameCamera.GetComponent<Camera>().fieldOfView = PlayerSettings.Instance.gameplaySettings.FieldOfView;
            }

            if (CameraData.DialoguePoints[0].FOVType.Equals(DialogueCameraFOVType.Static))
            {
                GameCamera.GetComponent<Camera>().fieldOfView = CameraData.DialoguePoints[0].CameraFOV;
            }

            if (CameraData.DialoguePoints[0].FOVType.Equals(DialogueCameraFOVType.Dynamic))
            {
                GameCamera.GetComponent<Camera>().fieldOfView = Mathf.Lerp(GameCamera.GetComponent<Camera>().fieldOfView, CameraData.DialoguePoints[0].CameraFOV, Time.deltaTime * CameraData.DialoguePoints[0].CameraFOVZoomSpeed);
            }

            //Face Player To Speaker Or Direction
            //Player Look
            if (CameraData.DialoguePoints[0].Speakers[0] != null && CameraData.DialoguePoints[0].Speakers.Count == 1)
            {
                Vector3 lookDir = -Player.transform.position - -CameraData.DialoguePoints[0].Speakers[0].transform.position;
                lookDir.y = 0;

                Quaternion q = Quaternion.LookRotation(lookDir);

                if (Quaternion.Angle(q, baseRotation) <= 180)
                {
                    targetRotation = q;
                }

                Player.transform.rotation = Quaternion.Slerp(Player.transform.rotation, targetRotation, 2 * Time.deltaTime);
            }

            if (CameraData.DialoguePoints[0].Speakers != null && CameraData.DialoguePoints[0].Speakers.Count > 1)
            {
                Vector3 lookDir = -Player.transform.position - -GeneralLook.transform.position;
                lookDir.y = 0;

                Quaternion q = Quaternion.LookRotation(lookDir);

                if (Quaternion.Angle(q, baseRotation) <= 180)
                {
                    targetRotation = q;
                }

                Player.transform.rotation = Quaternion.Slerp(Player.transform.rotation, targetRotation, 2 * Time.deltaTime);
            }

            //Set Camera Position
            #region Position
            if (CameraData.DialoguePoints[0].MovementType.Equals(DialogueCameraMovementType.Instant))
            {
                if (CameraData.DialoguePoints[0].PositionType.Equals(DialogueCameraPositionType.World))
                {
                    transform.position = CameraData.DialoguePoints[0].Position;
                    transform.SetParent(null);
                }

                if (CameraData.DialoguePoints[0].PositionType.Equals(DialogueCameraPositionType.Local) && CameraData.DialoguePoints[0].LocalizedParent != null)
                {
                    transform.SetParent(CameraData.DialoguePoints[0].LocalizedParent);
                    transform.position = CameraData.DialoguePoints[0].LocalizedParent.position + CameraData.DialoguePoints[0].Position;
                }
            }

            if (CameraData.DialoguePoints[0].MovementType.Equals(DialogueCameraMovementType.Interpolated))
            {
                if (CameraData.DialoguePoints[0].PositionType.Equals(DialogueCameraPositionType.World))
                {
                    transform.position = Vector3.Lerp(transform.position, CameraData.DialoguePoints[0].Position, Time.deltaTime * CameraData.DialoguePoints[0].PosLerpSpeed);
                    transform.SetParent(null);
                }

                if (CameraData.DialoguePoints[0].PositionType.Equals(DialogueCameraPositionType.Local) && CameraData.DialoguePoints[0].LocalizedParent != null)
                {
                    transform.position = Vector3.Lerp(transform.position, CameraData.DialoguePoints[0].LocalizedParent.position + CameraData.DialoguePoints[0].Position, Time.deltaTime * CameraData.DialoguePoints[0].PosLerpSpeed);
                    transform.SetParent(CameraData.DialoguePoints[0].LocalizedParent);
                }
            }
            #endregion
            //Set Camera Rotation
            #region Rotation
            if (CameraData.DialoguePoints[0].RotationStyle.Equals(DialogueCameraRotationStyle.LookToSpeakers))
            {
                if (CameraData.DialoguePoints[0].RotationType.Equals(DialogueCameraRotationType.Instant))
                {
                    //If There is only one speaker look at that one person
                    if (CameraData.DialoguePoints[0].Speakers[0] != null && CameraData.DialoguePoints[0].Speakers.Count == 1)
                    {
                        transform.LookAt(CameraData.DialoguePoints[0].Speakers[0].transform);
                    }

                    //if There is more then one speaker to look at then get the midpoint of all the speakers and look at the center of them all instead
                    if (CameraData.DialoguePoints[0].Speakers != null && CameraData.DialoguePoints[0].Speakers.Count > 1)
                    {
                        Vector3 MidPoint = Vector3.zero;

                        foreach (GameObject Speaker in CameraData.DialoguePoints[0].Speakers)
                        {
                            MidPoint += Speaker.transform.position;
                        }

                        transform.LookAt(GeneralLook);
                    }
                }

                if (CameraData.DialoguePoints[0].RotationType.Equals(DialogueCameraRotationType.Interpolated))
                {
                    //If There is only one speaker look at that one person
                    if (CameraData.DialoguePoints[0].Speakers[0] != null && CameraData.DialoguePoints[0].Speakers.Count == 1)
                    {
                        //Goal Angle To Reset Too
                        Quaternion angle = Quaternion.LookRotation(CameraData.DialoguePoints[0].Speakers[0].transform.position - transform.position);
                        transform.rotation = Quaternion.Slerp(transform.rotation, angle, Time.deltaTime * CameraData.DialoguePoints[0].RotLerpSpeed);
                    }

                    //if There is more then one speaker to look at then get the midpoint of all the speakers and look at the center of them all instead
                    if (CameraData.DialoguePoints[0].Speakers != null && CameraData.DialoguePoints[0].Speakers.Count > 1)
                    {
                        Vector3 MidPoint = Vector3.zero;

                        foreach (GameObject Speaker in CameraData.DialoguePoints[0].Speakers)
                        {
                            MidPoint += Speaker.transform.position;
                        }

                        GeneralLook.position = (Player.transform.position + MidPoint) / 2f;

                        Quaternion angle = Quaternion.LookRotation(GeneralLook.position - transform.position);
                        transform.rotation = Quaternion.Slerp(transform.rotation, angle, Time.deltaTime * CameraData.DialoguePoints[0].RotLerpSpeed);
                    }
                }
            }

            if (CameraData.DialoguePoints[0].RotationStyle.Equals(DialogueCameraRotationStyle.Given))
            {
                if (CameraData.DialoguePoints[0].RotationType.Equals(DialogueCameraRotationType.Instant))
                {
                    transform.rotation = CameraData.DialoguePoints[0].GivenLookDirection;
                }

                if (CameraData.DialoguePoints[0].RotationType.Equals(DialogueCameraRotationType.Interpolated))
                {
                    //Look In Direction
                    transform.rotation = Quaternion.Slerp(transform.rotation, CameraData.DialoguePoints[0].GivenLookDirection, Time.deltaTime * CameraData.DialoguePoints[0].RotLerpSpeed);
                }
            }
            #endregion
        }
    }

    private void FirstPersonMode()
    {
        XAngle = 0;

        //if (!UIManager.Instance.isPaused)
        //{
        //Player Movement Mode
        Player.GetComponent<PlayerDriver>().SetMoveType(MovementType.FPSMove);

        //Parenting
        Rotater.SetParent(Player.transform);
        transform.SetParent(Rotater);

        //Rotater Position
        Rotater.position = Player.transform.position + Rotater.TransformDirection(CameraFPSPos);

        //Mouse Movement Axis
        if (InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.enabled)
        {
            YHeight += (InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.RightStick.ReadValue<Vector2>().y * PlayerSettings.Instance.gameplaySettings.controllerSensitivity) * Time.deltaTime;
        }
        else
        {
            YHeight += -(Input.GetAxis("Mouse Y") * PlayerSettings.Instance.gameplaySettings.sensitivity) * Time.deltaTime;
        }

        //Clamp Up/Down Angle
        YHeight = Mathf.Clamp(YHeight, MinYAngle, MaxYAngle);

        //Rotater Rotation
        Quaternion angle = Quaternion.Euler(new Vector3(YHeight, Player.transform.eulerAngles.y, Player.transform.eulerAngles.z));
        Rotater.rotation = Quaternion.Lerp(Rotater.rotation, angle, (2 * (Time.deltaTime / Time.timeScale)) * 10);

        //Rotating Player In DirectionalTurning() See In Player Driver Script

        //Camera Position
        transform.position = Rotater.position;
        transform.rotation = Rotater.rotation;

        //Set GameCamera Position
        GameCamera.transform.localPosition = transform.localPosition;
        GameCamera.transform.rotation = Rotater.rotation;

        var zoombutton = InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.FPSCamera;

        if (Input.mouseScrollDelta.y < 0 || InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.RightStick.ReadValue<Vector2>().y > 0 && zoombutton.IsPressed())
        {
            CurrZoom = 0;
            PlayerSettings.Instance.gameplaySettings.Mode = CameraMode.UnTargetedMode;
        }
    }
    //LightWeight Distance Calculation Method
    private float DistanceCalculation(Transform origin, Transform target)
    {
        float dist = (origin.position - target.position).magnitude;

        return dist;
    }
    private float DistanceCalculation(Vector3 origin, Vector3 target)
    {
        float dist = (origin - target).magnitude;

        return dist;
    }

    //Grab Targets
    private int currentTargetIndex = 0;

    private void ScreenInfluencedTargeting()
    {
        if (PlayerSettings.Instance.gameplaySettings.Mode != CameraMode.TargetMode)
        {
            LockOnTarget = null;
            validTargets.Clear();
            currentTargetIndex = 0;

            var scheme = InputManager.Instance.PlayerInputScheme;
            Camera cam = Camera.main;

            var hits = Physics.OverlapSphere(transform.position, LockOnDistance);
            List<(Transform target, float screenDist, float worldDist)> candidates = new();

            foreach (var hit in hits)
            {
                if (hit.TryGetComponent(out LockOnTargetHelper helper) && helper.isActiveAndEnabled)
                {
                    Vector3 toTarget = (hit.transform.position - transform.position).normalized;
                    float dot = Vector3.Dot(transform.forward, toTarget);

                    if (dot > 0.25f) // Broader view cone
                    {
                        Vector3 screenPoint = cam.WorldToViewportPoint(hit.transform.position);
                        if (screenPoint.z > 0 && screenPoint.x >= 0 && screenPoint.x <= 1 && screenPoint.y >= 0 && screenPoint.y <= 1)
                        {
                            float screenDist = Vector2.Distance(new Vector2(screenPoint.x, screenPoint.y), new Vector2(0.5f, 0.5f));

                            if (!Physics.Linecast(transform.position + Vector3.up * 1.25f, hit.transform.position + Vector3.up * 1.25f, Wall))
                            {
                                float worldDist = Vector3.Distance(transform.position, hit.transform.position);
                                candidates.Add((hit.transform, screenDist, worldDist));
                            }
                        }
                    }
                }
            }

            if (candidates.Count > 0)
            {
                // Sort primarily by screen center distance, then fallback to world distance
                var sorted = candidates.OrderBy(c => c.screenDist).ThenBy(c => c.worldDist).ToList();

                validTargets = sorted.Select(c => c.target).ToList();
                LockOnTarget = validTargets[0].gameObject;

                if (scheme.WasPressedThisFrame(KeyActions.TargetEnemy) && !keyPressed && CooldownManager.Instance.CooldownState("Target Enemy"))
                {
                    EnableLetterbox();
                    keyPressed = true;
                    PlayerSettings.Instance.gameplaySettings.Mode = CameraMode.TargetMode;
                    isLockedOn = true;

                    currentTargetIndex = 0;
                    PlayerDriver.Instance.currentZTarget = validTargets[currentTargetIndex];

                    Debug.Log("Locked onto target: " + LockOnTarget.name);

                    CooldownManager.Instance.IntiateCooldown("Target Enemy");
                }
            }
            else
            {
                if (scheme.WasPressedThisFrame(KeyActions.TargetEnemy))
                {
                    Debug.Log("No valid target found to lock onto.");
                }
            }
        }
        else
        {
            HandleTargetSwitching();
        }
    }
    private void HandleTargetSwitching()
    {
        float scroll = Input.mouseScrollDelta.y;

        if (validTargets.Count <= 1) return;

        if (scroll > 0f) // Scroll up: next farthest
        {
            currentTargetIndex = Mathf.Clamp(currentTargetIndex + 1, 0, validTargets.Count - 1);
            SetNewLockTarget();
        }
        else if (scroll < 0f) // Scroll down: next closest
        {
            currentTargetIndex = Mathf.Clamp(currentTargetIndex - 1, 0, validTargets.Count - 1);
            SetNewLockTarget();
        }
    }
    private void SetNewLockTarget()
    {
        EnableLetterbox();

        LockOnTarget = validTargets[currentTargetIndex].gameObject;
        PlayerDriver.Instance.currentZTarget = validTargets[currentTargetIndex];
        Debug.Log("Switched target to: " + LockOnTarget.name);

        Vector3 camPos = transform.position;
        Vector3 targetPos = LockOnTarget.transform.position;
        Vector3 directionToNewTarget = (targetPos - camPos).normalized;

        desiredLookRotation = Quaternion.LookRotation(directionToNewTarget);
        isRotatingToNewTarget = true;
    }

    RaycastHit hit;
    //Z Targeting
    // Add this field to your class somewhere (adjust default as needed)
    private float zoomOutOffset = 5f;  // Controls max push-back distance when player is close
    private Quaternion desiredLookRotation;
    private bool isRotatingToNewTarget = false;
    private float rotationSmoothSpeed = 5f;  // tweak as needed

    private float fixedCameraY = float.NaN; // store locked Y position when jump window starts

    private float currentY; // Keep this as a field to retain across frames

    // Add these fields somewhere at the top of your script
    private float yVelocity = 0f;
    private float ySmoothTimer = 0f;
    // Add these fields at the top of your class:
    private float rotationLerpTimer = 0f;
    private const float rotationDuration = 0.25f;
    private float smoothedMidpointY;
    private float cameraDistance;
    private void TargetMode()
    {
        if (!isLockedOn || LockOnTarget == null)
        {
            CancelLock("Target was null or no longer locked");
            return;
        }

        Vector3 playerPos = PlayerDriver.Instance.transform.position;
        Vector3 targetPos = LockOnTarget.transform.position;
        Vector3 directionToTarget = (targetPos - playerPos).normalized;
        Vector3 behindDirection = -directionToTarget;

        float targetDistance = Vector3.Distance(playerPos, targetPos);
        float cancelThreshold = LockOnDistance + 2f;
        if (targetDistance > cancelThreshold)
        {
            CancelLock("Target too far or lost");
            return;
        }

        // === Y-Follow Logic matching UnTargetedMode ===
        bool isGrounded = PlayerDriver.Instance.physicsProperties.Grounded;
        bool isWallKicking = PlayerDriver.Instance.physicsProperties.isWallKicking;
        bool isNimbusFlying = PlayerDriver.Instance.physicsProperties.isNimbusInUse;

        float currentPlayerY = playerPos.y;

        if (isGrounded)
            groundNeutralY = currentPlayerY;

        float verticalDisplacement = currentPlayerY - groundNeutralY;
        bool shouldTrackY = isGrounded || isWallKicking || isNimbusFlying || Mathf.Abs(verticalDisplacement) > 6f + heightOffset;

        float verticalDeadzone = yDeadzoneThreshold;

        if (shouldTrackY)
        {
            float targetY = currentPlayerY;
            float heightDiff = targetY - currentY;
            float verticalOffsetFromCenter = Mathf.Abs(heightDiff);

            float distanceToTarget = Vector3.Distance(Player.transform.position, LockOnTarget.transform.position);
            float distanceFactor = Mathf.Clamp01(distanceToTarget / 6f);
            float distanceAdjustedFollowSpeed = Mathf.Lerp(1f, yFollowSpeed, distanceFactor);

            if (yFollowRecoveryActive)
            {
                float t = Mathf.Clamp01(1f - (yFollowRecoveryTimer / yFollowRecoveryDuration));
                float curveMultiplier = heightFollowCurve.Evaluate(t);

                yFollowRecoveryTimer -= Time.deltaTime;
                cameraYOffset = Mathf.Lerp(cameraYOffset, 0f, Time.deltaTime * yFollowRecoverySpeed * curveMultiplier);

                if (yFollowRecoveryTimer <= 0.01f && Mathf.Abs(cameraYOffset) < 0.05f)
                {
                    yFollowRecoveryActive = false;
                    cameraYOffset = 0f;
                }
            }
            else
            {
                float adjustedYFollowSpeed = yFollowBoostActive
                    ? distanceAdjustedFollowSpeed * Mathf.Lerp(1f, 4f, Mathf.Clamp01(verticalOffsetFromCenter * 2f))
                    : distanceAdjustedFollowSpeed;

                // Only lerp cameraYOffset if difference exceeds deadzone OR boost is active
                if (verticalOffsetFromCenter > verticalDeadzone || yFollowBoostActive)
                {
                    cameraYOffset = Mathf.Lerp(cameraYOffset, heightDiff, Time.deltaTime * adjustedYFollowSpeed);
                }
                // When close to center, gently lerp cameraYOffset toward zero, but never below zero
                else if (isGrounded)
                {
                    float curveMult = heightFollowCurve.Evaluate(1f - Mathf.Clamp01(Mathf.Abs(cameraYOffset) / verticalDeadzone));
                    cameraYOffset = Mathf.Lerp(cameraYOffset, 0f, Time.deltaTime * yFollowRecoverySpeed * curveMult);
                    cameraYOffset = Mathf.Max(cameraYOffset, 0f);
                }
            }

            // Smoothly interpolate currentY to playerY + your offsets + cameraYOffset
            currentY = Mathf.Lerp(currentY, currentPlayerY + cameraYOffset, Time.deltaTime * 3f);
        }
        else
        {
            float targetY = groundNeutralY;
            float heightDiff = targetY - currentY;

            float curveMult = heightFollowCurve.Evaluate(1f - Mathf.Clamp01(Mathf.Abs(cameraYOffset) / verticalDeadzone));
            cameraYOffset = Mathf.Lerp(cameraYOffset, 0f, Time.deltaTime * yFollowRecoverySpeed * curveMult);
            cameraYOffset = Mathf.Max(cameraYOffset, 0f);

            currentY = Mathf.Lerp(currentY, currentPlayerY + cameraYOffset, Time.deltaTime * 3f);
        }

        // === Camera Positioning ===
        Vector3 camRight = Vector3.Cross(Vector3.up, directionToTarget);
        Vector3 sideOffset = camRight * sideOffsetAmount;
        Vector3 cameraOffset = behindDirection * distanceBehind + sideOffset;
        Vector3 desiredCameraPos = new Vector3(
            playerPos.x + cameraOffset.x,
            currentY + heightOffset,
            playerPos.z + cameraOffset.z
        );

        if (Physics.Linecast(playerPos, desiredCameraPos, out RaycastHit hitInfo, Wall))
        {
            desiredCameraPos = hitInfo.point + hitInfo.normal * 0.2f;
            transform.position = Vector3.Lerp(transform.position, desiredCameraPos, Time.deltaTime * cameraSmoothSpeed);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, desiredCameraPos, Time.deltaTime * cameraSmoothSpeed);
        }

        // === Rotation Logic ===
        if (isRotatingToNewTarget)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredLookRotation, Time.deltaTime * 2f);
            if (Quaternion.Angle(transform.rotation, desiredLookRotation) < 0.5f)
            {
                transform.rotation = desiredLookRotation;
                isRotatingToNewTarget = false;
            }
        }
        else
        {
            Vector3 camForward = (targetPos - transform.position).normalized;
            Vector3 camRightLook = Vector3.Cross(Vector3.up, camForward);
            Vector3 camUp = Vector3.Cross(camForward, camRightLook);

            float thirdsX = 1.5f;
            float thirdsY = -0.5f;

            // --- Midpoint vertical smoothing to reduce jumpiness when close ---
            Vector3 midpoint = (playerPos + targetPos) * 0.5f;

            float horizontalDist = Vector3.Distance(
                new Vector3(playerPos.x, 0, playerPos.z),
                new Vector3(targetPos.x, 0, targetPos.z)
            );

            float verticalInfluence;

            if (horizontalDist < 7f)
            {
                verticalInfluence = Mathf.Lerp(0f, 0.1f, horizontalDist / 2f);
            }
            else
            {
                verticalInfluence = Mathf.Clamp01(horizontalDist / 5f);
            }

            float smoothedY = Mathf.Lerp(Mathf.Min(playerPos.y, targetPos.y) + 1f, midpoint.y, verticalInfluence);
            midpoint.y = smoothedY;

            Vector3 offsetTarget = midpoint + camRightLook * -thirdsX + camUp * thirdsY;
            Vector3 lookDirection = offsetTarget - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            // --- Distance-based smooth rotation speed ---
            float minSpeed = 0.1f;
            float maxSpeed = 8f;
            float closeDistance = 7f;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(horizontalDist / closeDistance));
            float rotationSpeed = Mathf.Lerp(minSpeed, maxSpeed, t);

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            Vector3 euler = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(euler.x, euler.y, 0f);
        }

        // Ensure Rotater faces the target
        Rotater.rotation = Quaternion.LookRotation(LockOnTarget.transform.position - playerPos);

        var scheme = InputManager.Instance.PlayerInputScheme;
        if (!scheme.IsHeld(KeyActions.TargetEnemy) &&
            PlayerSettings.Instance.gameplaySettings.ZTargetType == ZTargetMode.Hold)
        {
            CancelLock("Cancelled Target Mode. [Hold]");
            DisableLetterbox();
        }

        if (scheme.WasPressedThisFrame(KeyActions.TargetEnemy) &&
            PlayerSettings.Instance.gameplaySettings.ZTargetType == ZTargetMode.Toggle &&
            CooldownManager.Instance.CooldownState("Target Enemy"))
        {
            CancelLock("Cancelled Target Mode. [Toggle]");
            CooldownManager.Instance.IntiateCooldown("Target Enemy");
            DisableLetterbox();
        }

        if (!LockOnTarget.GetComponent<LockOnTargetHelper>().enabled)
        {
            CancelLock("Cancelled Target Mode. [Direct Cancel]");
            DisableLetterbox();
        }
    }



    private float groundNeutralY;
    private void UnTargetedMode()
{
    if (InputManager.Instance != null && !InputManager.Instance.updateKeys && !PrototypeUI.Instance.isPaused)
    {
        XAngle = 0;

        var scheme = InputManager.Instance.PlayerInputScheme;

        // Force camera Z rotation to 0 to avoid skew
        Vector3 fixedEuler = transform.eulerAngles;
        fixedEuler.z = 0f;
        GameCamera.transform.rotation = Quaternion.Euler(fixedEuler);

        if (!CancelHook && scheme.IsHeld(KeyActions.Grapple) && scheme.IsHeld(KeyActions.Guard))
            CancelHook = true;
        if (CancelHook && !scheme.IsHeld(KeyActions.Grapple) && !scheme.IsHeld(KeyActions.Guard))
            CancelHook = false;

        bool isGrounded = PlayerDriver.Instance.physicsProperties.Grounded;
        bool isWallKicking = PlayerDriver.Instance.physicsProperties.isWallKicking;
        bool isNimbusFlying = PlayerDriver.Instance.physicsProperties.isNimbusInUse;

        float currentPlayerY = Player.transform.position.y;

        // Reset ground neutral Y on landing
        if (isGrounded)
        {
            groundNeutralY = currentPlayerY;
        }

        float verticalDisplacement = currentPlayerY - groundNeutralY;

        // Conditions to actively track player Y
        bool shouldTrackY = isGrounded || isWallKicking || isNimbusFlying || Mathf.Abs(verticalDisplacement) > 6f;

        // Y-follow parameters
        float heightThreshold = heightOffset;
        float verticalDeadzone = yDeadzoneThreshold; // e.g., 0.25f

        if (shouldTrackY)
        {
            float targetY = currentPlayerY;
            float heightDiff = targetY - currentY;
            float verticalOffsetFromCenter = Mathf.Abs(heightDiff);

            if (yFollowRecoveryActive)
            {
                float t = Mathf.Clamp01(1f - (yFollowRecoveryTimer / yFollowRecoveryDuration));
                float curveMultiplier = heightFollowCurve.Evaluate(t);

                yFollowRecoveryTimer -= Time.deltaTime;
                cameraYOffset = Mathf.Lerp(cameraYOffset, 0f, Time.deltaTime * yFollowRecoverySpeed * curveMultiplier);

                if (yFollowRecoveryTimer <= 0.01f && Mathf.Abs(cameraYOffset) < 0.05f)
                {
                    yFollowRecoveryActive = false;
                    cameraYOffset = 0f;
                }
            }
            else
            {
                float adjustedYFollowSpeed = yFollowBoostActive
                    ? yFollowSpeed * Mathf.Lerp(1f, 4f, Mathf.Clamp01(verticalOffsetFromCenter * 2f))
                    : yFollowSpeed;

                if (Mathf.Abs(heightDiff) > verticalDeadzone || yFollowBoostActive)
                {
                    cameraYOffset = Mathf.Lerp(cameraYOffset, heightDiff, Time.deltaTime * adjustedYFollowSpeed);
                }
                else if (isGrounded)
                {
                    float curveMult = heightFollowCurve.Evaluate(1f - Mathf.Clamp01(Mathf.Abs(cameraYOffset) / verticalDeadzone));
                    cameraYOffset = Mathf.Lerp(cameraYOffset, 0f, Time.deltaTime * yFollowRecoverySpeed * curveMult);
                    cameraYOffset = Mathf.Max(cameraYOffset, 0f);
                }
            }

            // Faster lerp with multiplier (tweak 5f as needed)
            currentY = Mathf.Lerp(currentY, targetY + cameraYOffset, Time.deltaTime * 3f);
        }
        else
        {
            float targetY = groundNeutralY;
            float heightDiff = targetY - currentY;

            float curveMult = heightFollowCurve.Evaluate(1f - Mathf.Clamp01(Mathf.Abs(cameraYOffset) / verticalDeadzone));
            cameraYOffset = Mathf.Lerp(cameraYOffset, 0f, Time.deltaTime * yFollowRecoverySpeed * curveMult);
            cameraYOffset = Mathf.Max(cameraYOffset, 0f);

            currentY = Mathf.Lerp(currentY, heightOffset + targetY + cameraYOffset, Time.deltaTime * ySmoothTime * 2f);
        }

        // Existing camera and input logic below — unchanged except removing redundant currentY lerp
        if (!scheme.WasPressedThisFrame(KeyActions.Grapple) && !CancelHook || CancelHook)
        {
            PlayerDriver.Instance.SetMoveType(MovementType.FreeMove);

            if (!PlayerDriver.Instance.physicsProperties.isGrinding)
            {
                CurrZoom = Mathf.Clamp(CurrZoom, ZoomMin, ZoomMax);

                if (InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.enabled)
                {
                    if (InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.RightStick.ReadValue<Vector2>().y != 0 && scheme.WasPressedThisFrame(KeyActions.ResetCamera))
                        CurrZoom -= InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.RightStick.ReadValue<Vector2>().y * ZoomSpeed * Time.deltaTime;
                }
                else
                {
                    if (Input.mouseScrollDelta.y > 0 && CurrZoom < ZoomMax)
                        CurrZoom += 1.5f * ZoomSpeed * Time.deltaTime;
                    if (Input.mouseScrollDelta.y < 0 && CurrZoom > ZoomMin)
                        CurrZoom -= 1.5f * ZoomSpeed * Time.deltaTime;
                }

                if (CurrZoom == ZoomMax && Input.mouseScrollDelta.y > 0 || InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.RightStick.ReadValue<Vector2>().y > 0 && scheme.WasPressedThisFrame(KeyActions.ResetCamera))
                {
                    PlayerSettings.Instance.gameplaySettings.Mode = CameraMode.FPSMode;
                }
            }
            else
            {
                CurrZoom = Mathf.Clamp(CurrZoom, ZoomMin, GrindZoomOutValue);
                CurrZoom = -GrindZoomOutValue;
            }

            Vector3 zoomOffset = new Vector3(0, 0, CurrZoom);
            Vector3 lateralOffset;
            Vector3 desiredOffset = Vector3.zero;

            Quaternion targetRot;

            Vector3 moveDir = PlayerDriver.Instance.physicsProperties.move;
            moveDir.z = 0;
            Vector3 worldMoveDir = PlayerDriver.Instance.transform.TransformDirection(moveDir.normalized);

            Vector3 localMove = PlayerDriver.Instance.transform.InverseTransformDirection(moveDir);

            float strafeThreshold = 0.1f;
            float strafeDir = 0f;

            if (Mathf.Abs(moveDir.x) > strafeThreshold)
            {
                strafeDir = Mathf.Sign(moveDir.x);
            }

            Vector3 camRight = transform.right;
            desiredOffset = camRight * strafeDir * Mathf.Abs(lookAheadDistance);
            currentLookOffset = Vector3.Lerp(currentLookOffset, desiredOffset, Time.deltaTime * lookAheadSmoothing);

            Vector3 baseLookTarget = Rotater.transform.position + Vector3.up * 3f;
            Vector3 dynamicLookTarget = baseLookTarget + currentLookOffset;
            targetRot = Quaternion.LookRotation(dynamicLookTarget - transform.position);
            transform.rotation = targetRot;

            lateralOffset = -Player.transform.right * LeftOffsetAmount;

            Vector3 fullOffset = PlatformingPos + lateralOffset + zoomOffset;
            Vector3 blendedOffset = Vector3.Lerp(PlatformingPos + zoomOffset, fullOffset, OffsetBlendValue);

            Vector3 goalPos = ClampToBounds(blendedOffset);
            transform.localPosition = Vector3.Lerp(transform.localPosition, goalPos, Time.deltaTime * PlatformingCameraSpeed);

            if (Physics.Linecast(Rotater.position, transform.position, out RaycastHit hit, Wall))
            {
                if (hit.collider.CompareTag("Wall") || hit.collider.CompareTag("Floor"))
                    GameCamera.transform.position = Vector3.Lerp(GameCamera.transform.position, hit.point, Time.deltaTime * 2f * PlatformingCameraSpeed);
            }
            else
            {
                GameCamera.transform.position = Vector3.Lerp(GameCamera.transform.position, transform.position, Time.deltaTime * 2f * PlatformingCameraSpeed);
            }

            float inputX = 0;
            float inputY = 0;

            Rotater.SetParent(null);
            transform.SetParent(Rotater);
            GeneralLook.SetParent(transform);
            GeneralLook.position = transform.position;

            PlayerDriver.Instance.SetMoveType(MovementType.FreeMove);

            if (InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.enabled && scheme.WasPressedThisFrame(KeyActions.ResetCamera))
            {
                inputY = InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.RightStick.ReadValue<Vector2>().y;
            }
            else
            {
                inputY = Input.GetAxis("Mouse Y");
            }
            if (scheme.invertY)
            {
                YHeight -= inputY * PlayerSettings.Instance.gameplaySettings.sensitivity * Time.deltaTime;
            }
            else
            {
                YHeight += inputY * PlayerSettings.Instance.gameplaySettings.sensitivity * Time.deltaTime;
            }

            YHeight = Mathf.Clamp(YHeight, MinYAngle, MaxYAngle);

            if (InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.enabled)
            {
                inputX = InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.RightStick.ReadValue<Vector2>().x;
            }
            else
            {
                inputX = Input.GetAxis("Mouse X");
            }

            if (scheme.invertX)
            {
                Rotater.Rotate(Vector3.up, -inputX * PlayerSettings.Instance.gameplaySettings.sensitivity * Time.deltaTime);
            }
            else
            {
                Rotater.Rotate(Vector3.up, inputX * PlayerSettings.Instance.gameplaySettings.sensitivity * Time.deltaTime);
            }
        }

        // Apply Y-follow to Rotater position
        Vector3 goalRotPos = new Vector3(Player.transform.position.x, currentY, Player.transform.position.z);
        Rotater.position = Vector3.Lerp(Rotater.position, goalRotPos, Time.deltaTime * 2f * PlatformingRotaterSpeed);
        Rotater.eulerAngles = new Vector3(YHeight, Rotater.eulerAngles.y, 0);

        isLockedOn = false;
        keyPressed = false;
    }
}



    void SetupBar(RectTransform barRect, bool isTop)
    {
        barRect.anchorMin = new Vector2(0f, isTop ? 1f : 0f);
        barRect.anchorMax = new Vector2(1f, isTop ? 1f : 0f);
        barRect.pivot = new Vector2(0.5f, isTop ? 1f : 0f);
        barRect.anchoredPosition = Vector2.zero;
    }

    public void EnableLetterbox()
    {
        float barHeightPixels = Screen.height * barHeightRatio;

        RectTransform topRect = topBar.GetComponent<RectTransform>();
        RectTransform bottomRect = bottomBar.GetComponent<RectTransform>();

        SetupBar(topRect, true);
        SetupBar(bottomRect, false);

        topRect.sizeDelta = new Vector2(0, barHeightPixels);
        bottomRect.sizeDelta = new Vector2(0, barHeightPixels);

        topBar.SetActive(true);
        bottomBar.SetActive(true);

        Camera.main.rect = new Rect(0f, barHeightRatio, 1f, 1f - 2 * barHeightRatio);
    }

    public void DisableLetterbox()
    {
        topBar.SetActive(false);
        bottomBar.SetActive(false);
        Camera.main.rect = new Rect(0f, 0f, 1f, 1f);
    }

    private void DebugDrawBounds(Bounds bounds, Color color)
    {
        Vector3 center = transform.TransformPoint(bounds.center);
        Vector3 size = bounds.size;

        Vector3 v3FrontTopLeft = center + transform.rotation * new Vector3(-size.x, size.y, size.z) * 0.5f;
        Vector3 v3FrontTopRight = center + transform.rotation * new Vector3(size.x, size.y, size.z) * 0.5f;
        Vector3 v3FrontBottomLeft = center + transform.rotation * new Vector3(-size.x, -size.y, size.z) * 0.5f;
        Vector3 v3FrontBottomRight = center + transform.rotation * new Vector3(size.x, -size.y, size.z) * 0.5f;

        Vector3 v3BackTopLeft = center + transform.rotation * new Vector3(-size.x, size.y, -size.z) * 0.5f;
        Vector3 v3BackTopRight = center + transform.rotation * new Vector3(size.x, size.y, -size.z) * 0.5f;
        Vector3 v3BackBottomLeft = center + transform.rotation * new Vector3(-size.x, -size.y, -size.z) * 0.5f;
        Vector3 v3BackBottomRight = center + transform.rotation * new Vector3(size.x, -size.y, -size.z) * 0.5f;

        Debug.DrawLine(v3FrontTopLeft, v3FrontTopRight, color);
        Debug.DrawLine(v3FrontTopRight, v3FrontBottomRight, color);
        Debug.DrawLine(v3FrontBottomRight, v3FrontBottomLeft, color);
        Debug.DrawLine(v3FrontBottomLeft, v3FrontTopLeft, color);

        Debug.DrawLine(v3BackTopLeft, v3BackTopRight, color);
        Debug.DrawLine(v3BackTopRight, v3BackBottomRight, color);
        Debug.DrawLine(v3BackBottomRight, v3BackBottomLeft, color);
        Debug.DrawLine(v3BackBottomLeft, v3BackTopLeft, color);

        Debug.DrawLine(v3FrontTopLeft, v3BackTopLeft, color);
        Debug.DrawLine(v3FrontTopRight, v3BackTopRight, color);
        Debug.DrawLine(v3FrontBottomRight, v3BackBottomRight, color);
        Debug.DrawLine(v3FrontBottomLeft, v3BackBottomLeft, color);
    }

    private void RebuildTargetList()
    {
        var hits = Physics.OverlapSphere(transform.position, LockOnDistance);
        validTargets.Clear();

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out LockOnTargetHelper helper) && helper.isActiveAndEnabled)
            {
                Vector3 dir = (hit.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, dir);

                if (dot > 0.25f)
                {
                    if (!Physics.Linecast(transform.position + Vector3.up * 1.25f, hit.transform.position + Vector3.up * 1.25f, Wall))
                    {
                        validTargets.Add(hit.transform);
                    }
                }
            }
        }

        // Sort based on distance
        validTargets = validTargets.OrderBy(t => Vector3.Distance(transform.position, t.position)).ToList();
    }

    private void SelectTargetByScreenProximity()
    {
        Camera cam = Camera.main;
        float closestDistance = float.MaxValue;

        foreach (var target in validTargets)
        {
            Vector3 screenPoint = cam.WorldToViewportPoint(target.position);
            float distanceFromCenter = Vector2.Distance(screenPoint, new Vector2(0.5f, 0.5f));

            if (distanceFromCenter < closestDistance && screenPoint.z > 0)
            {
                closestDistance = distanceFromCenter;
                LockOnTarget = target.gameObject;
            }
        }

        if (LockOnTarget != null)
            PlayerDriver.Instance.currentZTarget = LockOnTarget.transform;
    }

    private void SelectNextTargetCloser()
    {
        if (validTargets.Count == 0) return;

        int currentIndex = validTargets.IndexOf(LockOnTarget.transform);
        currentIndex = (currentIndex - 1 + validTargets.Count) % validTargets.Count;
        LockOnTarget = validTargets[currentIndex].gameObject;
        PlayerDriver.Instance.currentZTarget = LockOnTarget.transform;
    }

    private void SelectNextTargetFarther()
    {
        if (validTargets.Count == 0) return;

        int currentIndex = validTargets.IndexOf(LockOnTarget.transform);
        currentIndex = (currentIndex + 1) % validTargets.Count;
        LockOnTarget = validTargets[currentIndex].gameObject;
        PlayerDriver.Instance.currentZTarget = LockOnTarget.transform;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || !LockOnTarget) return;

        Vector3 playerPos = PlayerDriver.Instance.transform.position;
        Vector3 targetPos = LockOnTarget.transform.position;

        Vector3 directionToTarget = (targetPos - playerPos).normalized;
        Vector3 midpoint = (playerPos + targetPos) * 0.5f;
        Vector3 offsetBack = -directionToTarget * distanceBehind;
        Vector3 offsetUp = Vector3.up * heightOffset;

        Vector3 camPos = midpoint + offsetBack + offsetUp;

        // Line from midpoint to camera position
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(midpoint, camPos);

        // Draw camera target position
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(camPos, 0.35f);
    }

    private void DebugDrawBounds(Bounds bounds, Color color, bool drawDiagonals = false)
    {
        Vector3 center = bounds.center;
        Vector3 extents = bounds.extents;

        Vector3[] corners = new Vector3[8];

        // Top 4 corners
        corners[0] = center + new Vector3(-extents.x, extents.y, -extents.z);
        corners[1] = center + new Vector3(extents.x, extents.y, -extents.z);
        corners[2] = center + new Vector3(extents.x, extents.y, extents.z);
        corners[3] = center + new Vector3(-extents.x, extents.y, extents.z);

        // Bottom 4 corners
        corners[4] = center + new Vector3(-extents.x, -extents.y, -extents.z);
        corners[5] = center + new Vector3(extents.x, -extents.y, -extents.z);
        corners[6] = center + new Vector3(extents.x, -extents.y, extents.z);
        corners[7] = center + new Vector3(-extents.x, -extents.y, extents.z);

        // Top square
        Debug.DrawLine(corners[0], corners[1], color);
        Debug.DrawLine(corners[1], corners[2], color);
        Debug.DrawLine(corners[2], corners[3], color);
        Debug.DrawLine(corners[3], corners[0], color);

        // Bottom square
        Debug.DrawLine(corners[4], corners[5], color);
        Debug.DrawLine(corners[5], corners[6], color);
        Debug.DrawLine(corners[6], corners[7], color);
        Debug.DrawLine(corners[7], corners[4], color);

        // Vertical lines
        Debug.DrawLine(corners[0], corners[4], color);
        Debug.DrawLine(corners[1], corners[5], color);
        Debug.DrawLine(corners[2], corners[6], color);
        Debug.DrawLine(corners[3], corners[7], color);

        // Optional diagonals for clarity
        if (drawDiagonals)
        {
            Debug.DrawLine(center, corners[0], color);
            Debug.DrawLine(center, corners[2], color);
            Debug.DrawLine(center, corners[5], color);
            Debug.DrawLine(center, corners[7], color);
        }
    }

    private void CancelLock(string reason)
    {
        cancelLockOn = true;
        isLockedOn = false;
        PlayerSettings.Instance.gameplaySettings.Mode = CameraMode.UnTargetedMode;
        Debug.Log(reason);
    }

    //Normal Camera
    private float lastStrafeInput = 0f;
    private float strafeDampSpeed = 5f;
    private bool yFollowBoostActive = false;
    private bool yFollowRecoveryActive = false;
    private float yFollowRecoveryTimer = 0f;
    private float cameraYOffsetVelocity = 0f;
    private float yFollowRecoverySmoothTime = 0.15f; // tweakable
    private float _preLandYOffset;
    private bool _wasAirborne;
    private float _landingTransitionTimer;

    private bool IsPlayerVisibleOnScreen(Camera cam, Transform player)
    {
        Vector3 viewportPos = cam.WorldToViewportPoint(player.position);

        // On-screen if inside the normalized viewport rectangle (0 to 1 in X and Y) and in front of the camera (Z > 0)
        return viewportPos.z > 0 &&
               viewportPos.x >= 0f && viewportPos.x <= 1f &&
               viewportPos.y >= 0f && viewportPos.y <= 1f;
    }
    Vector3 ClampToBounds(Vector3 position)
    {
        return new Vector3(Mathf.Clamp(position.x, cameraBoundsMin.x, cameraBoundsMax.x),
            position.y,
            Mathf.Clamp(position.z + CurrZoom, cameraBoundsMin.y + CurrZoom, cameraBoundsMax.y + CurrZoom)
        );
    }
    private bool IsPlayerInCameraBounds()
    {
        Vector3 playerPos = transform.position;

        bool withinX = playerPos.x >= cameraBoundsMin.x && playerPos.x <= cameraBoundsMax.x;
        bool withinZ = playerPos.z >= cameraBoundsMin.y && playerPos.z <= cameraBoundsMax.y;

        return withinX && withinZ;
    }
    private void OnDrawGizmosSelected()
    {
        if (PlayerDriver.Instance == null)
            return;

        Vector3 playerPos = PlayerDriver.Instance.transform.position;
        float y = playerPos.y;

        // Define red bounds (camera's current bounds)
        Vector3 camMin = transform.position + new Vector3(cameraBoundsMin.x, 0, cameraBoundsMin.y);
        Vector3 camMax = transform.position + new Vector3(cameraBoundsMax.x, 0, cameraBoundsMax.y);

        // Define green bounds (player-centered camera region)
        Vector3 worldMin = playerPos + new Vector3(cameraBoundsMin.x, 0, cameraBoundsMin.y);
        Vector3 worldMax = playerPos + new Vector3(cameraBoundsMax.x, 0, cameraBoundsMax.y);

        // Draw red camera region
        Gizmos.color = Color.red;
        DrawRectGizmo(camMin, camMax, y);

        // Draw green player region
        Gizmos.color = Color.green;
        DrawRectGizmo(worldMin, worldMax, y);

        // Calculate intersection (transition zone), scaled by multiplier
        Vector3 scaledWorldMin = playerPos + new Vector3(cameraBoundsMin.x, 0, cameraBoundsMin.y) * transitionZoneScale;
        Vector3 scaledWorldMax = playerPos + new Vector3(cameraBoundsMax.x, 0, cameraBoundsMax.y) * transitionZoneScale;

        Vector3 intersectMin = Vector3.Max(camMin, scaledWorldMin);
        Vector3 intersectMax = Vector3.Min(camMax, scaledWorldMax);

        if (intersectMin.x < intersectMax.x && intersectMin.z < intersectMax.z)
        {
            Gizmos.color = Color.yellow;
            DrawRectGizmo(intersectMin, intersectMax, y);
        }

        if (Player == null || Rotater == null || transform == null) return;


        // Define offsets
        Vector3 centerOffset = AimCameraPos;

        // Transform offsets into world space
        Vector3 upPos = transform.TransformPoint(upLookCameraOffset);
        Vector3 centerPos = transform.TransformPoint(centerOffset);
        Vector3 downPos = transform.TransformPoint(downLookCameraOffset);

        // Draw spheres for positions
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(upPos, 0.1f);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(centerPos, 0.1f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(downPos, 0.1f);

        // Draw blend path
        Gizmos.color = Color.yellow;
        const int steps = 20;
        for (int i = 0; i < steps; i++)
        {
            float t = i / (float)(steps - 1);
            Vector3 blendedPos;
            if (t < 0.5f)
            {
                float subT = Mathf.InverseLerp(0f, 0.5f, t);
                blendedPos = Vector3.Lerp(upPos, centerPos, subT);
            }
            else
            {
                float subT = Mathf.InverseLerp(0.5f, 1f, t);
                blendedPos = Vector3.Lerp(centerPos, downPos, subT);
            }

            Gizmos.DrawSphere(blendedPos, 0.025f);

            if (i > 0)
            {
                Vector3 prevPos;
                float prevT = (i - 1) / (float)(steps - 1);
                if (prevT < 0.5f)
                {
                    float subPrevT = Mathf.InverseLerp(0f, 0.5f, prevT);
                    prevPos = Vector3.Lerp(upPos, centerPos, subPrevT);
                }
                else
                {
                    float subPrevT = Mathf.InverseLerp(0.5f, 1f, prevT);
                    prevPos = Vector3.Lerp(centerPos, downPos, subPrevT);
                }

                Gizmos.DrawLine(prevPos, blendedPos);
            }
        }

        // Draw current active target if in play mode
        if (Application.isPlaying)
        {
            float t = Mathf.InverseLerp(MinYAngle, MaxYAngle, YHeight);
            Vector3 activeTarget;
            if (t < 0.5f)
            {
                float subT = Mathf.InverseLerp(0f, 0.5f, t);
                activeTarget = Vector3.Lerp(upPos, centerPos, subT);
            }
            else
            {
                float subT = Mathf.InverseLerp(0.5f, 1f, t);
                activeTarget = Vector3.Lerp(centerPos, downPos, subT);
            }

            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(activeTarget, 0.12f);
        }
    }
    private void DrawRectGizmo(Vector3 min, Vector3 max, float y)
    {
        Vector3 p1 = new Vector3(min.x, y, min.z);
        Vector3 p2 = new Vector3(max.x, y, min.z);
        Vector3 p3 = new Vector3(max.x, y, max.z);
        Vector3 p4 = new Vector3(min.x, y, max.z);

        Gizmos.DrawLine(p1, p2);
        Gizmos.DrawLine(p2, p3);
        Gizmos.DrawLine(p3, p4);
        Gizmos.DrawLine(p4, p1);
    }
    private bool IsPlayerInTransitionZone()
    {
        Vector3 playerPos = PlayerDriver.Instance.transform.position;

        float minX = Mathf.Max(cameraBoundsMin.x, playerPos.x + cameraBoundsMin.x * transitionZoneScale);
        float maxX = Mathf.Min(cameraBoundsMax.x, playerPos.x + cameraBoundsMax.x * transitionZoneScale);
        float minZ = Mathf.Max(cameraBoundsMin.y, playerPos.z + cameraBoundsMin.y * transitionZoneScale);
        float maxZ = Mathf.Min(cameraBoundsMax.y, playerPos.z + cameraBoundsMax.y * transitionZoneScale);

        return playerPos.x >= minX && playerPos.x <= maxX && playerPos.z >= minZ && playerPos.z <= maxZ;
    }
    private Vector3 velocity = Vector3.zero; // Persistent velocity for SmoothDamp
    private float targetFOV = 45f; // Dramatic zoom during Tempest Kick
    private float defaultFOV = 60f;
    private float FOVSpeed = 5f;
    private void TempestKickCamera()
    {
        // Detach + set hierarchy
        Rotater.SetParent(null);
        transform.SetParent(Rotater);
        GeneralLook.SetParent(transform);
        GeneralLook.position = transform.position;

        // Start and end positions
        Vector3 startPos = PlayerDriver.Instance.physicsProperties.TempestKickStartPosition;
        Vector3 endPos = PlayerDriver.Instance.physicsProperties.TempestKickEndPosition;

        // Adjust end position if ray hits something
        Vector3 direction = (endPos - startPos).normalized;
        float maxDistance = Vector3.Distance(startPos, endPos);
        RaycastHit rayHit;
        if (Physics.Raycast(startPos, direction, out rayHit, maxDistance, Wall))
        {
            endPos = rayHit.point;
        }

        // Update progress
        float progress = Mathf.Clamp01(Vector3.Distance(PlayerDriver.Instance.transform.position, startPos) / maxDistance);

        // ROTATER midpoint between start and end
        Rotater.position = (startPos + endPos) / 2f;

        // Prep cinematic look
        if (PlayerDriver.Instance.physicsProperties.preparingTempestKick)
        {
            // Assume TempestPos is something like (x, y, z) offset relative to the player
            Vector3 rotatedOffset = Rotater.rotation * TempestPos;

            // Final position relative to the player, rotated with their facing direction
            Vector3 desiredPosition = Rotater.position + rotatedOffset;

            // Use this wherever you want to position something relative to the player with rotation
            transform.position = desiredPosition;

            // Look at end point while keeping player left-aligned
            Vector3 playerToEnd = endPos - PlayerDriver.Instance.transform.position;
            Vector3 cinematicLookAt = PlayerDriver.Instance.transform.position + playerToEnd.normalized * 10f;
            transform.LookAt(cinematicLookAt);

            // Optional: Reduce FOV for more cinematic effect
            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, 45f, Time.deltaTime * FOVSpeed);
        }
        else
        {
            // Distance offset for mid-air kick (first half)
            Vector3 distanceOffset = new Vector3(0, 0, -Vector3.Distance(startPos, endPos) * 0.25f);
            Vector3 desiredLocalPos = distanceOffset + TempestPos;

            // Once past halfway, follow behind player
            if (progress > 0.2f)
            {
                Vector3 followOffset = -PlayerDriver.Instance.transform.forward * 5f + Vector3.up * 2f;
                desiredLocalPos = PlayerDriver.Instance.transform.position + followOffset + TempestFollowOffset;

                // Lower FOV further for a dramatic chase feel
                Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, 40f, Time.deltaTime * FOVSpeed);

                //CameraShake.Instance.Shake(0.3f, 0.3f);
            }

            // Lerp transform to new position
            transform.position = Vector3.Lerp(transform.position, desiredLocalPos, Time.deltaTime * 5);

            // Smooth rotation toward player
            Vector3 lookDirection = PlayerDriver.Instance.transform.position - transform.position;
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection.normalized);
            transform.rotation = targetRotation;
        }

        // Obstruction ray from camera to player
        Vector3 obstructionDir = PlayerDriver.Instance.transform.position - transform.position;
        float obstructionDistance = obstructionDir.magnitude;

        if (Physics.Raycast(transform.position, obstructionDir.normalized, out RaycastHit obstructionHit, obstructionDistance, Wall))
        {
            // Pull in front of obstruction
            Vector3 adjustedPosition = obstructionHit.point - obstructionDir.normalized * 0.5f;
            GameCamera.transform.position = Vector3.Lerp(GameCamera.transform.position, adjustedPosition, Time.deltaTime * 5);
        }
        else
        {
            // Move to desired position with added shake offset
            Vector3 finalPosition = transform.position + CameraShake.Instance.GetShakeOffset();
            GameCamera.transform.position = Vector3.Lerp(GameCamera.transform.position, finalPosition, Time.deltaTime * 5);
        }

        // When progress is >= 0.6, hold at the end position for cinematic viewing
        if (progress >= 0.6f)
        {
            Vector3 holdOffset = Vector3.zero;  // Stay at a fixed position during the end phase
            Vector3 finalHoldPosition = endPos + holdOffset;

            // Camera stays fixed at the end of the Tempest Kick to watch the player
            transform.position = Vector3.Lerp(transform.position, finalHoldPosition, Time.deltaTime * 3);

            // Slightly zoom back in
            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, 60f, Time.deltaTime * FOVSpeed);
        }
    }
    private void SkywardAscentCamera()
    {
        // Detach + set hierarchy
        Rotater.SetParent(null);
        transform.SetParent(Rotater);
        GeneralLook.SetParent(transform);
        GeneralLook.position = transform.position;

        // Start and end positions
        Vector3 startPos = PlayerDriver.Instance.physicsProperties.SkywardAscentStartPosition;
        Vector3 endPos = PlayerDriver.Instance.physicsProperties.SkywardAscentEndPosition;

        // Adjust end position if ray hits something
        float maxDistance = Vector3.Distance(startPos, endPos);
        RaycastHit rayHit;

        if (Physics.Raycast(startPos, Vector3.up, out rayHit, maxDistance, Wall))
        {
            endPos = rayHit.point;
        }

        // Update progress
        float progress = Mathf.Clamp01(Vector3.Distance(PlayerDriver.Instance.transform.position, endPos) / maxDistance);

        // ROTATER midpoint between start and end
        Rotater.position = (startPos + endPos) / 2f;

        // Prep cinematic look
        if (PlayerDriver.Instance.physicsProperties.preparingSkywardAscent)
        {
            //CameraShake.Instance.Shake(0.3f, 0.1f);

            // Camera position to the side of the player
            transform.position = PlayerDriver.Instance.physicsProperties.SkywardAscentStartPosition + SkywardAscentPos;

            // Final camera application with shake offset (apply here only!)
            GameCamera.transform.position = transform.position + CameraShake.Instance.GetShakeOffset();
            GameCamera.transform.rotation = transform.rotation;
        }
        else
        {
            if (progress <= 0.5)
            {
                // Distance offset for mid-air kick (first half)
                Vector3 distanceOffset = PlayerDriver.Instance.physicsProperties.SkywardAscentStartPosition;
                Vector3 desiredLocalPos = distanceOffset + SkywardAscentFollowOffset;

                // Lerp transform to new position
                transform.position = Vector3.Lerp(transform.position, desiredLocalPos, Time.deltaTime * 3);
            }

            if (progress > 0.5f)
            {
                // Follow behind the player
                Vector3 followOffset = -PlayerDriver.Instance.transform.forward * 5f + Vector3.up * 2f;
                Vector3 desiredPosition = PlayerDriver.Instance.transform.position + followOffset + SkywardAscentEndOffset;

                // Smoothly follow the player
                transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 5);
            }

        }

        transform.LookAt(PlayerDriver.Instance.transform);
    }
}