using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using PathCreation;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.HighDefinition;
using Unity.VisualScripting;
using static UnityEngine.UI.Image;
using UnityEngine.TextCore.Text;
using GameSettings;
using static UnityEngine.GraphicsBuffer;
using UnityEngine.Rendering;
using InputKeys;
using System;
using UnityEditor;

public enum MovementType { Strafe, FreeMove, FPSMove }
public enum RunSpeedLevel { Run_Level_1_Jog, Run_Level_2_Run, Run_Level_3_TopSpeed, Run_Level_4_Boost }

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(GroundChecker))]
[RequireComponent(typeof(PlayerInteractor))]
[RequireComponent(typeof(PlayerSettings))]
[RequireComponent(typeof(InputManager))]
[RequireComponent(typeof(DialogueManager))]

public class PlayerDriver : MonoBehaviour
{
    [System.Serializable]
    public class PlayerPhysics
    {
        [Header("Debugging")]
        public bool collisionDebug = false;

        [Header("Respawning")]
        public Shrine lastCheckpoint;

        #region Player Locks
        [Header("Player Locks")]

        [Tooltip("Locks all player movement.")]
        public bool movementLock;

        [Tooltip("Locks all air movement.")]
        public bool airMovementLock;

        [Tooltip("Locks player turning.")]
        public bool turnLock;

        [Tooltip("Overrides all movement controls.")]
        public bool OverrideMovement = false;

        [Tooltip("Overrides directional rotation controls.")]
        public bool OverrideDirectionRotations = false;

        [Tooltip("Overrides ground detection state.")]
        public bool OverrideGround = false;

        [Tooltip("Overrides gravity force application.")]
        public bool OverrideForceGravity = false;

        [Tooltip("Overrides prevention of dodging.")]
        public bool OverridePreventDodging = false;
        #endregion

        #region Vectors
        [Header("Vectors")]

        [Tooltip("Physics velocity vector.")]
        public Vector3 velocity;

        [Tooltip("Movement input vector (WASD).")]
        public Vector3 move;

        [Tooltip("Final computed velocity vector.")]
        public Vector3 vel;

        [Tooltip("Direction vector based on WASD inputs.")]
        public Vector3 dir;
        #endregion

        #region Player Scaling
        [Header("Player Scaling")]

        [Tooltip("Maximum slope angle the player can walk up.")]
        public float slopeLimit = 50;

        [Tooltip("Height of the player model.")]
        public float modelHeight = 1f;

        [Tooltip("Height at which raycasts are cast.")]
        public float rayHeight = 1f;

        [Tooltip("Y-axis origin point of the capsule collider.")]
        public float capsuleOriginPoint = -0.12f;

        [Tooltip("Height of the capsule collider.")]
        public float capsuleOriginHeight = 2f;

        [Tooltip("Start point of the capsule collider.")]
        public Vector3 collisionOriginStart = new Vector3(0, 0.91f, 0);

        [Tooltip("End point of the capsule collider.")]
        public Vector3 collisionOriginEnd = new Vector3(0, -1.14f, 0);

        [Tooltip("Radius of the capsule collider.")]
        public float collisionRadius = 0.5f;

        [Tooltip("Range of the raycast point.")]
        public float rayPointDistance = 1.0f;

        [Tooltip("Range of the floor raycast.")]
        public float floorRayDistance = 2f;
        #endregion

        #region Gravity Options
        [Header("Gravity Options")]

        [Tooltip("Gravity multiplier over time.")]
        public float defaultGravityScale = 1f;

        [Tooltip("Initial gravity force affecting all movement.")]
        public float intialGravity = 80f;

        [Tooltip("Gravity multiplier over time.")]
        public float gravityMultiplier;

        [Tooltip("Maximum gravity multiplier over time.")]
        public float gravityMultiplierMax;

        [Tooltip("Incremental gravity force increase.")]
        public float gravityIncrease;

        [Tooltip("Toggle for ground detection on/off.")]
        public bool groundDetectionToggle = true;

        [Tooltip("Touched Objects")]
        public List<GameObject> physicallyTouchedObjects = new List<GameObject>();
        #endregion

        [Header("Detailed Ground Info")]
        public bool isStandingInWater;

        #region Simple Movement Motor Functions
        [Header("Simple Movement Motor Functions")]

        [Tooltip("Turn smoothing speed.")]
        public float turnSpeed = 15f;

        [Tooltip("Speed at which the player model turns to face key directions.")]
        public float directionLookSpeed = 5f;

        [Tooltip("Type of player movement.")]
        public MovementType MoveType;

        [Tooltip("Base walking speed.")]
        public float baseMovementSpeed = 10f;

        [Tooltip("Base air movement speed.")]
        public float baseAirSpeed = 5f;

        [Tooltip("Current movement speed.")]
        public float currentMovementSpeed;

        [Tooltip("Current rigidbody physics speed.")]
        public Vector3 physicalSpeed;

        [Tooltip("Current direction of movement.")]
        public Vector3 physicalDirection;

        [Tooltip("Maximum allowed rigidbody physics speed.")]
        public float maxRigidBodySpeed = 100;

        [Tooltip("Air acceleration while moving over time.")]
        public float airforce = 0.5f;

        [Tooltip("Angle threshold for momentum forces.")]
        public float forceMomentumAngle = 10;

        [Tooltip("Natural physics force carrying player along slopes.")]
        public float momentumforce = 70f;

        [Tooltip("Force built when running in any direction.")]
        public float runforce = 35f;
        #endregion

        #region Physics Drivers
        [Header("Physics Drivers")]

        [Tooltip("Reference to the player's Rigidbody component.")]
        public Rigidbody rb;

        [Tooltip("Layer mask for objects that should not be collided with or detected as ground.")]
        public LayerMask excludePlayer;

        [Tooltip("Reference to the player's Capsule Collider.")]
        public Collider CapsuleCol;
        #endregion

        #region Grinding
        [Header("Grinding")]

        [Tooltip("Apply forward leap force when grinding.")]
        public bool applyGrindLeapForwardForce;

        [Tooltip("Apply backward leap force when grinding.")]
        public bool applyGrindBackwardsLeapForce;

        [Tooltip("Allow leaping off the grind path.")]
        public bool leapOffGrindPath;

        [Tooltip("Path object representing the grind rail.")]
        public PathCreator GrindPath;

        [Tooltip("Direction along the grind rail. Negative = backwards, positive = forwards.")]
        public int GrindDirection = 1;

        [Tooltip("Offset from the grind rail.")]
        public Vector3 GrindRailOffset;

        [Tooltip("Maximum allowable grind angle.")]
        public float GrindAngleLimit = 50f;

        [Tooltip("Maximum acceleration while grinding.")]
        public float maximumGrindAcceleration;

        [Tooltip("Physical force applied to accelerate grinding.")]
        public float physicalGrindForceAcceleration;

        [Tooltip("Rate of grind force acceleration.")]
        public float grindforceAccelRate = 10;

        [Tooltip("Rate of grind force deceleration.")]
        public float grindforceDeaccelRate = 5;

        [Tooltip("Force applied when passing over grind surfaces.")]
        public float physicalPassOverGrindForce;

        [Tooltip("Current speed while grinding.")]
        public float grindSpeed = 2f;

        [Tooltip("Total distance traveled while grinding.")]
        public float grindDistanceTraveled;
        #endregion

        #region Run
        [Header("Run")]

        [Tooltip("Current speed level in running states.")]
        public RunSpeedLevel LevelOfSpeed;

        [Tooltip("Duration the player has been moving.")]
        public float movementDuration = 0;

        [Tooltip("Jogging speed value.")]
        public float jogSpeed = 0;

        [Tooltip("Speed threshold to start running.")]
        public float runThreshold = 0;

        [Tooltip("Current running speed.")]
        public float runSpeed = 0;

        [Tooltip("Upper speed threshold for the top speed.")]
        public float topThreshold = 0;

        [Tooltip("Maximum top speed achievable.")]
        public float topSpeed = 0;
        #endregion

        #region Boost
        [Header("Boost")]

        [Tooltip("Speed when grinding with boost applied.")]
        public float boostgrindSpeed = 10f;

        [Tooltip("Current boost speed value.")]
        public float boostSpeed = 0;
        #endregion

        #region Sliding/Crouching
        [Header("Sliding/Crouching")]

        [Tooltip("Layer mask detecting objects considered ceilings for crouching.")]
        public LayerMask crouchLayers;

        [Tooltip("Distance checked above the player’s head to detect ceilings for crouching.")]
        public float crouchDistCheck = 3f;

        [Tooltip("Base movement speed while crouching.")]
        public float crouchMovementSpeed = 4f;
        #endregion

        #region Dodging
        [Header("Dodging")]

        [Tooltip("Maximum duration allowed for a dodge.")]
        public float MaxDodgeDuration = 0.5f;

        [Tooltip("Speed applied while dodging.")]
        public float DodgeSpeed = 20f;
        #endregion

        #region Jump
        [Header("Jump")]

        [Tooltip("Number of total jumps the player can perform at the currently (resets on landing)")]
        public int numberOfTotalJumpsAvaliable;

        [Tooltip("Maximum additional jumps allowed (e.g., double jumps).")]
        public int numberOfTotalJumpsMax = 2;

        [Tooltip("Current duration of the jump in seconds.")]
        public float currJumpTime = 0;

        [Tooltip("Initial jump duration time.")]
        public float setJumpTime = 1;

        [Tooltip("Height applied to the jump vector.")]
        public float jumpHeight;

        [Tooltip("Time window after jumping during which another jump input is ignored.")]
        public float jumpDeadZoneTime = 0.3f;

        [Tooltip("Strength applied to the initial jump force.")]
        public float jumpforce = 6f;

        [Tooltip("Strength applied to the double jump force.")]
        public float jumpdoubleforce = 3f;

        [Tooltip("Incremental increase in fall speed after jump, reducing the jump height additive force over time.")]
        public float incrementJumpFallSpeed = 0.1f;
        #endregion

        #region Tempest Kick
        [Header("Tempest Kick")]

        [Tooltip("Maximum distance the Tempest Kick covers.")]
        public float TempestDistance;

        [Tooltip("Speed at which the Tempest Kick moves.")]
        public float TempestSpeed;

        [Tooltip("Progress of the Tempest Kick animation or movement (0 to 1).")]
        public float TempestKickProgress;

        [Tooltip("Starting position of the Tempest Kick.")]
        public Vector3 TempestKickStartPosition;

        [Tooltip("Ending position of the Tempest Kick.")]
        public Vector3 TempestKickEndPosition;
        #endregion

        #region Skyward Ascent
        [Header("Skyward Ascent")]

        [Tooltip("Timer tracking duration of the Skyward Ascent.")]
        public float skywardAscentTimer = 0f;

        [Tooltip("Progress value (0-1) of the Skyward Ascent action.")]
        public float SkywardAscentProgress = 0f;

        [Tooltip("Start position of the Skyward Ascent movement.")]
        public Vector3 SkywardAscentStartPosition;

        [Tooltip("End position of the Skyward Ascent movement.")]
        public Vector3 SkywardAscentEndPosition;

        [Tooltip("Total distance covered during Skyward Ascent.")]
        public float SkywardAscentDistance;

        [Tooltip("Speed at which the Skyward Ascent progresses.")]
        public float SkywardAscentSpeed = 10f;
        #endregion

        #region Nimbus
        [Header("Nimbus")]

        [Tooltip("Upwards force when jumping off a wall.")]
        public float nimbuspeed = 5f;

        [Tooltip("Upwards speed while targeting during Nimbus movement.")]
        public float nimbusspeedTargeting = 5f;

        [Tooltip("Current Nimbus movement speed.")]
        public float currNimbusSpeed = 0;

        [Tooltip("Maximum flight speed allowed during Nimbus movement.")]
        public float maximumNimbusFlightSpeed = 20f;

        [Tooltip("Maximum flight speed allowed while targeting during Nimbus.")]
        public float maximumNimbusFlightSpeedTargeting = 20f;

        [Tooltip("Layer mask to determine which collisions cancel Nimbus movement.")]
        public LayerMask NimbusCollisionCancelMask;

        [Tooltip("Flag indicating if the player is currently zipping to a target.")]
        public bool isZippingToTarget = false;

        [Tooltip("Starting position of the zip.")]
        public Vector3 zipStart;

        [Tooltip("Ending position of the zip.")]
        public Vector3 zipEnd;

        [Tooltip("Elapsed time since the start of the zip.")]
        public float zipTime = 0f;

        [Tooltip("Y coordinate locked during Nimbus vertical movement (NaN if unlocked).")]
        public float nimbusLockY = float.NaN;

        [Tooltip("Whether vertical lerp for Nimbus is active.")]
        public bool isVerticalLerpActive = false;

        [Tooltip("Starting Y position for vertical lerp during Nimbus.")]
        public float verticalLerpStartY = 0f;

        [Tooltip("Ending Y position for vertical lerp during Nimbus.")]
        public float verticalLerpEndY = 0f;

        [Tooltip("Timer tracking vertical lerp progress.")]
        public float verticalLerpTimer = 0f;

        [Tooltip("Duration of vertical lerp during Nimbus movement.")]
        public float verticalLerpDuration = 0.75f; // tweak duration to your liking

        [Tooltip("Desired Y offset applied during Nimbus movement.")]
        public float desiredYOffset;

        [Tooltip("Speed at which Nimbus vertical lerp occurs.")]
        public float nimbusLerpSpeed = 6f;
        #endregion

        #region Freefalling
        [Header("Freefalling")]

        [Tooltip("Current speed while freefalling.")]
        public float freefallspeed;

        [Tooltip("Gravity force applied during freefall.")]
        public float freefallgravityfallforce;

        [Tooltip("Maximum gravity force that can be applied during freefall.")]
        public float maximumfreefallgravityfallforce;

        [Tooltip("Angle adjustment for freefalling movement control.")]
        public float freefallangleAdjust = 10f;
        #endregion

        #region Wall Kick
        [Header("Wall Kick")]

        [Tooltip("Upward force applied when the player jumps off a wall.")]
        public float WallKickHeight = 5f;

        [Tooltip("Forward force applied when the player jumps off a wall.")]
        public float WallKickDistance = 8f;

        [Tooltip("Upwards force applied when the player jumps off a wall.")]
        public float wallKickUpForce = 10f;

        [Tooltip("Speed at which the player slides down the wall.")]
        public float WallSlideSpeed = 2f;

        [Tooltip("Acceleration applied to increase wall slide speed when the player wants to fall faster.")]
        public float WallSlideSpeedAcceleration = 2f;

        [Tooltip("Offset applied to the player model when positioning near the wall.")]
        public Vector3 Offset = new Vector3(0, 0, -0.5f);

        [Tooltip("Layer mask specifying which object layers the player can wall jump off.")]
        public LayerMask wallKickLayer;
        #endregion

        #region Grappling
        [Header("Grappling")]

        [Tooltip("True when the player is about to initiate a grapple (preparation state).")]
        public bool PreGrappling = false;

        [Tooltip("Apply an additional jump force after a successful grapple.")]
        public bool applyGrappleJumpForce = false;

        [Tooltip("Blocks grappling if an object is hit during the linecast check.")]
        public bool linecastGrappleBlock = false;

        [Tooltip("Indicates whether the grapple UI/screen is currently active.")]
        public bool onGrappleScreen = false;

        [Tooltip("The vector direction used for grappling.")]
        public Vector3 GrappleVector;

        [Tooltip("The anchor point the player grapples from.")]
        public Transform GrappleAnchor;

        [Tooltip("The object being grappled to.")]
        public Transform GrappleObject;

        [Tooltip("The origin point where the grapple line starts.")]
        public Transform lineOrigin;

        [Tooltip("The line renderer used to visually display the grapple line.")]
        public LineRenderer grappleLine;

        [Tooltip("Maximum angle (in degrees) allowed for a grapple swing.")]
        public float maxGrappleSwingAngle = 75f;

        [Tooltip("Maximum distance allowed to grapple a point.")]
        public float grapplePointDist = 50f;

        [Tooltip("Maximum distance allowed to grapple swing.")]
        public float grappleSwingDist = 50f;

        [Tooltip("Layer mask to block grapple connections (e.g., walls or objects that interrupt line of sight).")]
        public LayerMask grappleBlockMask;

        [Tooltip("Maximum velocity during a grapple swing.")]
        public float maxSwingSpeed = 100f;

        [Tooltip("Speed at which the grapple line reels in when holding input.")]
        public float grappleReelSpeed = 10f;

        [Tooltip("Duration the player remains in leap mode after initiating a grapple leap.")]
        public float grappleLeapTime = 5f;

        [Tooltip("Vertical leap height added when using a grapple leap.")]
        public float grappleLeapHeight = 10f;

        [Tooltip("The force applied when leaping from a grapple.")]
        public float grappleLeapForce = 20f;

        [Tooltip("The force applied during grapple swinging.")]
        public float swingForce = 150f;

        [Tooltip("The movement speed while grappling (non-swing mode).")]
        public float grappleSpeed = 2f;

        [Tooltip("The speed at which the player is pulled toward the grapple point.")]
        public float pullSpeed = 2f;

        [Tooltip("Offset applied to the grapple point for targeting correction or aiming adjustments.")]
        public float grapplePointOffset = 2f;

        [Tooltip("Distance at which the grapple is automatically broken.")]
        public float grapplebreakDistance = 3f;

        [Tooltip("Radius around the center of screen to detect valid grapple targets.")]
        public float grappleScreenRadius = 0.2f;
        #endregion

        #region Player States
        [Header("Player States")] // These flags are used for animations, gameplay transitions, or input logic

        [Tooltip("Enables or disables gravity's effect on the player.")]
        public bool ApplyGravity = true;

        public bool temporarilyDisableGravity = false;

        [Tooltip("True when the player is running.")]
        public bool isRunning;

        [Tooltip("True when the player is performing a boost action.")]
        public bool isBoosting;

        [Tooltip("True while the player is rolling.")]
        public bool isRolling = false;

        [Tooltip("True while the player is crouching.")]
        public bool isCrouching = false;

        [Tooltip("True if the player has executed a double jump.")]
        public bool DoubleJump = false;

        [Tooltip("True if the player is considered on the ground.")]
        public bool Grounded = false;

        [Tooltip("True if the player is physically touching the ground via collision checks.")]
        public bool PhysicallyGrounded = false;

        [Tooltip("True when the player is hovering mid-air.")]
        public bool hovering = false;

        [Tooltip("True when the player is currently dashing.")]
        public bool dashing = false;

        [Tooltip("If true, the player is allowed to dash.")]
        public bool canDash = true;

        [Tooltip("If true, the player is allowed to jump.")]
        public bool canJump = true;

        [Tooltip("True when the player is falling downwards.")]
        public bool descending = false;

        [Tooltip("True when the player is in position and state to perform a wall kick.")]
        public bool readyToWallKick = false;

        [Tooltip("True if the player is currently performing a wall kick.")]
        public bool isWallKicking = false;

        [Tooltip("True if the player is hanging on a wall.")]
        public bool isWallHanging = false;

        [Tooltip("True when the player is in the middle of performing a Tempest Kick.")]
        public bool isTempestKicking = false;

        [Tooltip("True when the player is preparing to execute a Tempest Kick.")]
        public bool preparingTempestKick = false;

        [Tooltip("True when the player is charging Skyward Ascent.")]
        public bool isChargingSkywardAscent = false;

        [Tooltip("True when the player is preparing the Skyward Ascent ability.")]
        public bool preparingSkywardAscent = false;

        [Tooltip("True once the Skyward Ascent is fully charged.")]
        public bool isSkywardAscentCharged = false;

        [Tooltip("True while the player is ascending with Skyward Ascent.")]
        public bool isSkywardAscending = false;

        [Tooltip("True when the Nimbus ability is in use.")]
        public bool isNimbusInUse = false;

        [Tooltip("True when the player is freefalling.")]
        public bool isFreefalling = false;

        [Tooltip("True while the player is being pulled toward a grapple point.")]
        public bool pullingToGrapplePoint;

        [Tooltip("True while the player is being pulled toward a grapple swing point.")]
        public bool pullingToSwingPoint;

        [Tooltip("True if the player is currently in swing mode (grappling).")]
        public bool swingMode = false;

        [Tooltip("True if the player is currently in pull mode (grappling).")]
        public bool pullMode = false;

        [Tooltip("True while the player is being pulled by a grapple.")]
        public bool isPulling = false;

        [Tooltip("True when the player is within range of a swingable grapple target.")]
        public bool inSwingRange = false;

        [Tooltip("True when the player is within range of a standard grapple point.")]
        public bool inPointRange = false;

        [Tooltip("True if the player is currently grinding on a rail.")]
        public bool isGrinding = false;
        #endregion

    }

    // Reference to all physics-related properties for the player (movement, gravity, state flags, etc.)
    public PlayerPhysics physicsProperties;

    // Reference to the player camera controller (used for cinematic cameras, FOV, targeting, etc.)
    public PlayerCamera MyCamera;

    // Reference to the global or local event system managing input-based or scripted events
    public EntityEventManager PlayerEvents;

    // Internal reference to the ground detection system (e.g., slope detection, surface tagging, etc.)
    private GroundChecker gChecker;

    // Reference to gameplay and movement configuration values (e.g., keybindings, sensitivity, etc.)
    private PlayerSettings p_Settings;

    //Character Stats
    private CharacterStats Stats;

    //Combat Manager
    private ComboManager combatSystem;

    //Cooldown manager
    private CooldownManager cooldownManager;

    // Used to store the player’s default upright rotation
    private Quaternion baseRotation;

    // Target rotation the player should rotate toward (e.g., based on movement direction or camera)
    private Quaternion targetRotation;

    //Moving Platform Detecton
    private Transform _ridingPlatform;          // what platform we’re currently on
    private Vector3 _lastPlatformWorldPos;    // its world position last frame
    private bool _justLandedOnPlatform;    // to ignore the first‐frame delta

    // Timer tracking how long a dodge has been active; used for timing the dodge phase
    private float currDodgeDuration;

    // Acceleration applied during waking or descending transitions (e.g., slope descent)
    private float wakingDescentAcceleration;

    // Singleton instance for the PlayerDriver — allows global static access to this specific instance
    private static PlayerDriver _instance;

    // Flag indicating whether the singleton was destroyed (prevents duplicate instantiation)
    static bool _destroyed;

    public static PlayerDriver Instance
    {
        get
        {
            // Prevent re-creation of the singleton during play mode exit.
            if (_destroyed) return null;

            // If the instance is already valid, return it. Needed if called from a
            // derived class that wishes to ensure the instance is initialized.
            if (_instance != null) return _instance;

            // Find the existing instance (across domain reloads).
            if ((_instance = FindAnyObjectByType<PlayerDriver>()) != null) return _instance;

            // Create a new GameObject instance to hold the singleton component.
            var gameObject = new GameObject(typeof(PlayerDriver).Name);

            // Move the instance to the DontDestroyOnLoad scene to prevent it from
            // being destroyed when the current scene is unloaded.
            DontDestroyOnLoad(gameObject);

            // Create the MonoBehavior component. Awake() will assign _instance.
            return gameObject.AddComponent<PlayerDriver>();
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

        // Perform any initial setup logic specific to the player
        InitializePlayer();
    }

    // Start is called before the first frame update
    void Start()
    {
        //Get player event manager
        PlayerEvents = GetComponent<EntityEventManager>();

        // Get the stats container (e.g., health, scarlet, attack power, etc.)
        Stats = GetComponent<CharacterStats>();

        // Get the settings/config component (keybinds, sensitivity, etc.)
        p_Settings = GetComponent<PlayerSettings>();

        // Get the ground checker component (detecting slopes, grounded status, etc.)
        gChecker = GetComponent<GroundChecker>();

        // Store the player's initial upright rotation for reference
        baseRotation = transform.rotation;

        // Set the active jump deadzone cooldown to its initial value
        activeDeadZoneTimer = physicsProperties.jumpDeadZoneTime;

        combatSystem = GetComponent<ComboManager>();

        cooldownManager = GetComponent<CooldownManager>();
    }


    void Update()
    {
        DirectionalTurning();     // Handles player turning based on input direction (e.g., WASD or analog stick)
        SlopeMomentum();          // Applies slope-based momentum adjustments while walking/running

        // Prevent movement if an attack step is playing and uses attack motion
        if (combatSystem.IsCurrentComboStepPlaying(out var playingStep))
        {
            if (playingStep.useAttackMotion)
            {
                physicsProperties.movementDuration = 0;
            }
        }
    }

    private void FixedUpdate()
    {
        StartCoroutine(GetPhysicalSpeed());   // Calculates Rigidbody velocity after physics step (useful for dynamic state feedback)
        CompleteMovement();                   // Core movement logic including movement direction, velocity, acceleration
        DodgeLogic();                         // Evaluates whether dodge conditions are met (e.g., cooldown, direction, stamina)
        Dodge();                              // Executes dodge movement if allowed
        ViperessGrappleHook();                // Handles grapple input detection and activation logic
        ViperessGrappleDisengage();          // Handles disengaging from a grapple state
        Grinding();                           // Handles grinding logic if player is on a grindable path (e.g., rails)
        TempestKick();                        // Controls behavior during or initiating Tempest Kick attack
        SkywardAscent();                      // Controls behavior during or initiating Skyward Ascent movement
    }

    private void LateUpdate()
    {
        GrappleLineRender(); // Renders the visual line (rope/cable) between the player and grapple point
    }

    #region Gravity & Grounded
    /// <summary>
    /// If the player is on a moving platform, applies the platform's delta position to the player.
    /// Prevents jitter on the first frame of landing by skipping delta once.
    /// </summary>

    public void PlatformFollowing()
    {
        // 1) Apply platform motion if we’re riding
        if (_ridingPlatform != null)
        {
            Vector3 currentPlatPos = _ridingPlatform.position;
            if (!_justLandedOnPlatform)
            {
                // move the player by exactly how far the platform moved
                Vector3 delta = currentPlatPos - _lastPlatformWorldPos;
                transform.position += delta;
                if (physicsProperties.collisionDebug)
                    Debug.Log($"[FixedUpdate] Riding platform, applied delta {delta}");
            }
            else
            {
                // skip delta on the very first FixedUpdate after landing
                _justLandedOnPlatform = false;
            }
            _lastPlatformWorldPos = currentPlatPos;
        }
    }
    /// <summary>
    /// Returns the normal of the ground directly beneath the player using a raycast.
    /// Defaults to Vector3.up if no hit is detected.
    /// </summary>

    Vector3 GetGroundNormal()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 3f))
        {
            return hit.normal;
        }

        return Vector3.up; // Default to flat ground if no hit
    }
    /// <summary>
    /// Resets the ground detection toggle and relevant jump/attack-related states (e.g., disables wall kick block and tempest kick).
    /// </summary>

    public void ResetGroundDetection()
    {
        physicsProperties.groundDetectionToggle = true;
        activeDeadZoneTimer = physicsProperties.jumpDeadZoneTime;
        disableWallKick = false;
        physicsProperties.isTempestKicking = false;
    }
    /// <summary>
    /// Calculates and returns the angle between the ground normal and Vector3.up (slope steepness).
    /// </summary>

    float GetGroundSlopeAngle()
    {
        Vector3 groundNormal = GetGroundNormal();
        return Vector3.Angle(groundNormal, Vector3.up);
    }
    /// <summary>
    /// Applies extra momentum force when running downhill beyond a threshold slope angle.
    /// Includes downhill speed boost and gravity binding for grounded movement on slopes.
    /// </summary>

    void SlopeMomentum()
    {
        Vector3 groundNormal = GetGroundNormal();
        Vector3 horizontalForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        // Get slope direction (the downhill direction)
        Vector3 slopeDirection = Vector3.ProjectOnPlane(-groundNormal, Vector3.up).normalized;

        // Dot product tells us how aligned our movement is with the slope
        float alignment = Vector3.Dot(horizontalForward, slopeDirection);

        // Combine with slope steepness
        float slopeSteepness = Mathf.Sin(GetGroundSlopeAngle() * Mathf.Deg2Rad);

        if (gChecker.groundSlopeAngle > physicsProperties.forceMomentumAngle && physicsProperties.Grounded && InputManager.Instance.PlayerInputScheme.MovementVector != Vector3.zero && !physicsProperties.swingMode)
        {
            //Downward force to bind you to the floor
            physicsProperties.rb.AddForce(Vector3.down * 25f, ForceMode.Acceleration);

            //Add Slope Momentum
            physicsProperties.rb.AddForce(gChecker.groundSlopeDir * physicsProperties.momentumforce);
            physicsProperties.rb.AddForce(transform.forward * physicsProperties.runforce);
        }

        if (alignment * slopeSteepness < 0 && InputManager.Instance.PlayerInputScheme.MovementVector != Vector3.zero && physicsProperties.Grounded && !physicsProperties.swingMode )
        {
            //physicsProperties.rb.AddForce(transform.forward * physicsProperties.runforce);

            physicsProperties.movementDuration += Time.deltaTime * 2f;

            if (physicsProperties.collisionDebug)
            {
                print(alignment * slopeSteepness);
            }
        }
    }
    /// <summary>
    /// Applies manual gravity when not grounded, swinging, pulling, or using certain abilities.
    /// Increases gravity multiplier over time and applies it to the velocity vector.
    /// </summary>
    private void Gravity()
    {
        if (physicsProperties.temporarilyDisableGravity)
        {
            physicsProperties.gravityMultiplier = 0f;
            physicsProperties.velocity.y = 0f;
            physicsProperties.rb.linearVelocity = new Vector3(
                physicsProperties.rb.linearVelocity.x,
                0f,
                physicsProperties.rb.linearVelocity.z
            );
            return;
        }

        bool shouldApplyGravity =
            physicsProperties.ApplyGravity &&
            !physicsProperties.Grounded &&
            !physicsProperties.swingMode &&
            !physicsProperties.pullingToSwingPoint &&
            !physicsProperties.pullingToGrapplePoint &&
            !physicsProperties.isTempestKicking &&
            !physicsProperties.hovering;

        if (shouldApplyGravity)
        {
            // Increase gravity multiplier over time
            physicsProperties.gravityMultiplier += (physicsProperties.gravityIncrease * Time.deltaTime);
            physicsProperties.gravityMultiplier = Mathf.Clamp(
                physicsProperties.gravityMultiplier,
                0f,
                physicsProperties.gravityMultiplierMax
            );

            // Get gravity scale from stats (e.g., 1 = normal, 0.5 = half gravity, 0 = no gravity)
            float gravityScale = Stats.GetStatValue(StatType.GravityScale);

            // Calculate total gravity with scale applied
            float baseGravity = physicsProperties.intialGravity + physicsProperties.gravityMultiplier;
            float totalGravity = baseGravity * gravityScale;

            // Apply gravity to vertical velocity
            physicsProperties.velocity.y -= totalGravity * Time.deltaTime;

            // Clamp downward velocity
            float maxFallSpeed = -physicsProperties.gravityMultiplierMax;
            physicsProperties.velocity.y = Mathf.Max(physicsProperties.velocity.y, maxFallSpeed);

            // Update velocity vector
            physicsProperties.vel = new Vector3(
                physicsProperties.velocity.x,
                physicsProperties.velocity.y,
                physicsProperties.velocity.z
            );

            // Apply movement
            transform.position += physicsProperties.vel * Time.deltaTime;
        }

        if (physicsProperties.Grounded ||
            physicsProperties.isTempestKicking ||
            physicsProperties.swingMode ||
            physicsProperties.pullingToSwingPoint ||
            physicsProperties.pullingToGrapplePoint ||
            physicsProperties.hovering)
        {
            physicsProperties.gravityMultiplier = 0f;
        }
    }




    private Vector3 rayPoint = Vector3.zero;
    RaycastHit tempHit;
    /// <summary>
    /// Enables or disables gravity for the player’s rigidbody and internal gravity logic.
    /// </summary>
    /// <param name="useGravity">True to apply gravity, false to disable it.</param>

    public void SetGravity(bool useGravity)
    {
        physicsProperties.rb.useGravity = useGravity;
        physicsProperties.ApplyGravity = useGravity;
    }
    /// <summary>
    /// Performs a downward raycast to determine if the player is grounded.
    /// Handles detection of moving platforms and steep slopes. Adjusts snapping to terrain height.
    /// </summary>

    private void GroundChecking()
    {
        // Ray origin above the player's base
        Vector3 rayPoint = transform.position + Vector3.up * physicsProperties.rayHeight;

        if (physicsProperties.collisionDebug)
            Debug.Log($"[GroundChecking] Ray from {rayPoint}");

        // Fire ray downward
        bool hit = Physics.Raycast(
            rayPoint,
            Vector3.down,
            out tempHit,
            physicsProperties.floorRayDistance + physicsProperties.modelHeight,
            physicsProperties.excludePlayer
        );

        if (hit && !physicsProperties.isGrinding && physicsProperties.groundDetectionToggle)
        {
            physicsProperties.Grounded = true;
            physicsProperties.numberOfTotalJumpsAvaliable = physicsProperties.numberOfTotalJumpsMax;
            physicsProperties.currJumpTime = 0f;

            if (physicsProperties.collisionDebug)
                Debug.Log($"[GroundChecking] Ground hit: {tempHit.collider.name}");

            // Detect if we're on a moving platform
            var mp = tempHit.transform.GetComponentInParent<MovingPlatform>();
            if (mp != null && mp.Platform != null)
            {
                // Just landed on new platform
                if (_ridingPlatform != mp.Platform)
                {
                    _ridingPlatform = mp.Platform;
                    _lastPlatformWorldPos = _ridingPlatform.position;
                    _justLandedOnPlatform = true;

                    if (physicsProperties.collisionDebug)
                        Debug.Log($"[GroundChecking] Landed on moving platform '{mp.name}'");
                }
            }
            else
            {
                // No platform underfoot
                if (_ridingPlatform != null && physicsProperties.collisionDebug)
                    Debug.Log("[GroundChecking] Left platform — clearing reference.");

                _ridingPlatform = null;
            }
        }
        else
        {
            if (physicsProperties.Grounded && physicsProperties.collisionDebug)
                Debug.Log("[GroundChecking] Not grounded anymore. Clearing platform and jump state.");

            physicsProperties.Grounded = false;
            _ridingPlatform = null;
        }

        // Apply slope correction (no changes here)
        if (physicsProperties.Grounded
            && Vector3.Distance(transform.position, tempHit.point) <= 1.1f)
        {
            Vector3 goal = tempHit.point + Vector3.up * physicsProperties.modelHeight;
            if (gChecker.groundSlopeAngle > physicsProperties.slopeLimit)
            {
                transform.position = new Vector3(transform.position.x, goal.y, transform.position.z);

                if (physicsProperties.collisionDebug)
                    Debug.Log($"[GroundChecking] Slope too steep ({gChecker.groundSlopeAngle:F1}°), snapping to {goal.y:F2}");
            }
            else if (physicsProperties.collisionDebug)
            {
                Debug.Log($"[GroundChecking] Slope angle {gChecker.groundSlopeAngle:F1}° OK. No snap needed.");
            }
        }
        else if (physicsProperties.collisionDebug)
        {
            Debug.Log("[GroundChecking] Too far from hit point or not grounded — skipping slope snap.");
        }
    }

    private void OnCollisionStay(Collision Col)
    {
        if (physicsProperties.collisionDebug)
        {
            Debug.Log($"OnCollisionStay with {Col.collider.transform.tag}");
        }

        if (Col.collider.transform.tag != "Rail" && Col.collider.transform.tag != "Rope")
        {
            physicsProperties.PhysicallyGrounded = true;
            if (physicsProperties.collisionDebug)
            {
                Debug.Log("PhysicallyGrounded set to true (non-Rail/Rope collision)");
            }
        }

        if (Col.collider.transform.tag == "Fracture")
        {
            Col.collider.GetComponent<Rigidbody>().AddForce(Vector3.up * 25f);
            if (physicsProperties.collisionDebug)
            {
                Debug.Log("Applied upward force to Fracture object");
            }
        }
    }
    private void OnCollisionExit(Collision Col)
    {
        //Remove All Objects Touched To Object List
        physicsProperties.physicallyTouchedObjects.Remove(Col.transform.gameObject);

        if (physicsProperties.collisionDebug)
        {
            Debug.Log($"OnCollisionExit with {Col.transform.tag}");
        }

        if (Col.transform.tag == "Water")
        {
            physicsProperties.isStandingInWater = false;
            if (physicsProperties.collisionDebug)
            {
                Debug.Log("Left Water, isStandingInWater set to false");
            }
        }

        switch (Col.transform.tag)
        {
            case "Rail":
            case "Rope":
                if (!physicsProperties.isRolling)
                {
                    physicsProperties.PhysicallyGrounded = false;
                    if (physicsProperties.collisionDebug)
                    {
                        Debug.Log($"PhysicallyGrounded set to false on exit from {Col.transform.tag}");
                    }
                }
                break;

            default:
                physicsProperties.PhysicallyGrounded = false;
                if (physicsProperties.collisionDebug)
                {
                    Debug.Log($"PhysicallyGrounded set to false on exit from {Col.transform.tag}");
                }
                break;
        }
    }
    private PathCreator PreviousPath;
    private void OnCollisionEnter(Collision Col)
    {
        //Add All Objects Touched To Object List
        physicsProperties.physicallyTouchedObjects.Add(Col.transform.gameObject);
        
        if (physicsProperties.collisionDebug)
        {
            Debug.Log($"OnCollisionEnter with {Col.transform.tag}");
        }

        physicsProperties.PhysicallyGrounded = true;
        if (physicsProperties.collisionDebug)
        {
            Debug.Log("PhysicallyGrounded set to true on collision enter");
        }

        if (Col.transform.tag == "Water")
        {
            physicsProperties.isStandingInWater = true;
            if (physicsProperties.collisionDebug)
            {
                Debug.Log("Entered Water, isStandingInWater set to true");
            }
        }

        if (physicsProperties.isTempestKicking)
        {
            if (physicsProperties.collisionDebug)
            {
                Debug.Log("Tempest Kick END");
            }
            Camera.main.fieldOfView = 60;
            physicsProperties.ApplyGravity = true;
            physicsProperties.rb.useGravity = true;
            physicsProperties.isTempestKicking = false;
        }

        // Grinding logic with debug info
        if (ProgressionInv.Instance.hasJadeSerpentBangle &&
            !physicsProperties.isNimbusInUse &&
            !physicsProperties.isGrinding &&
            Col.collider.transform.tag == "Rail" && CooldownManager.Instance.CooldownState("Rail Grind") && !physicsProperties.OverrideMovement)
        {
            if (physicsProperties.GrindPath != null)
            {
                PreviousPath = physicsProperties.GrindPath;
            }

            physicsProperties.GrindPath = Col.collider.transform.GetComponent<PathParent>().ParentPath;
            physicsProperties.grindDistanceTraveled = physicsProperties.GrindPath.path.GetClosestDistanceAlongPath(transform.position);

            Vector3 Point = physicsProperties.GrindPath.path.GetClosestPointOnPath(transform.position);

            if (Vector3.Distance(transform.position, physicsProperties.GrindPath.path.GetPoint(physicsProperties.GrindPath.path.NumPoints - 1)) <= 2 ||
                Vector3.Distance(transform.position, physicsProperties.GrindPath.path.GetPoint(0)) <= 2)
            {
                 CooldownManager.Instance.IntiateCooldown("Grind Release");
            }

            physicsProperties.physicalPassOverGrindForce += physicsProperties.physicalSpeed.magnitude;

            if (PreviousPath != null && physicsProperties.GrindPath.transform.name != PreviousPath.transform.name)
            {
                physicsProperties.physicalPassOverGrindForce += 10; // Bonus force on new rail

                if (physicsProperties.collisionDebug)
                {
                    Debug.Log("Entered new Grind Path, bonus force applied.");
                }
            }

            physicsProperties.applyGrindLeapForwardForce = false;
            physicsProperties.applyGrindBackwardsLeapForce = false;
            physicsProperties.leapOffGrindPath = false;

            Vector3 startPoint = physicsProperties.GrindPath.path.GetPoint(0);
            Vector3 endPoint = physicsProperties.GrindPath.path.GetPoint(physicsProperties.GrindPath.path.NumPoints - 1);

            if (physicsProperties.collisionDebug)
            {
                Debug.Log($"Start Point Angle: {Vector3.Angle(transform.position - startPoint, transform.forward)}");
                Debug.Log($"End Point Angle: {Vector3.Angle(transform.position - endPoint, transform.forward)}");
            }

            if (Vector3.Angle(transform.position - startPoint, transform.forward) <= Vector3.Angle(transform.position - endPoint, transform.forward))
            {
                physicsProperties.GrindDirection = 1;
            }
            else
            {
                physicsProperties.GrindDirection = -1;
            }

            EnterGrind();
        }
    }
    /// <summary>
    /// Performs a capsule overlap check to detect and resolve penetrations (e.g., through walls or objects).
    /// Corrects player position and velocity based on penetration vector.
    /// </summary>

    private void CollisionCheck()
    {
        if (physicsProperties.CapsuleCol == null)
        {
            if (physicsProperties.collisionDebug)
            {
                Debug.LogWarning("CollisionCheck aborted: CapsuleCol is null.");
            }
            return;
        }

        Collider[] overlaps = new Collider[10];

        int num = Physics.OverlapCapsuleNonAlloc(
            transform.TransformPoint(physicsProperties.collisionOriginStart),
            transform.TransformPoint(physicsProperties.collisionOriginEnd),
            physicsProperties.collisionRadius,
            overlaps,
            physicsProperties.excludePlayer,
            QueryTriggerInteraction.Ignore);


        if (physicsProperties.collisionDebug)
        {
            Debug.Log($"CollisionCheck found {num} overlapping colliders.");
        }

        Collider myCollider = physicsProperties.CapsuleCol;

        for (int i = 0; i < num; i++)
        {
            Transform t = overlaps[i].transform;

            if (Physics.ComputePenetration(myCollider, transform.position, transform.rotation, overlaps[i], t.position, t.rotation, out Vector3 dir, out float dist))
            {
                Vector3 penetrationVector = dir * dist;

                Vector3 compositeVelocity = physicsProperties.velocity + physicsProperties.vel + physicsProperties.move;
                Vector3 velocityProjected = Vector3.Project(compositeVelocity, -dir);

                transform.position += penetrationVector;

                Vector3 horizontalVelLoss = velocityProjected;
                horizontalVelLoss.y = 0; // preserve vertical velocity

                physicsProperties.vel -= horizontalVelLoss;

                if (physicsProperties.collisionDebug)
                {
                    Debug.Log($"Penetration detected with {t.name}. Penetration vector: {penetrationVector}, Velocity penalty applied: {horizontalVelLoss}");
                }
            }
        }

    }
    #endregion

    #region Grappling
    RaycastHit swingHookHit;
    SpringJoint springJoint;
    bool createSwingPhysics = false;
    public Transform currentZTarget;
    private Transform currentGrappleTarget;
    private bool GrappleObjKinematicState;
    private bool GrappleObjGravityState;

    /// <summary>
    /// Handles the selection and execution of grappling behavior.
    /// Evaluates targets in the vicinity, checks for visibility and priority, 
    /// and initiates pull or swing physics depending on the grapple type.
    /// </summary>
    void ViperessGrappleHook()
    {
        var scheme = InputManager.Instance.PlayerInputScheme;
        float grappleRange = physicsProperties.grapplePointDist;
        Vector3 origin = Camera.main.transform.position;
        Vector3 forward = Camera.main.transform.forward;

        Collider[] hits = Physics.OverlapSphere(transform.position, grappleRange);

        Transform bestSwingableTarget = null;
        float bestSwingableScreenDist = float.MaxValue;

        Transform bestPullableTarget = null;
        float bestPullableScreenDist = float.MaxValue;
        float bestPullableWorldDist = float.MaxValue;

        int pullableVisibleCount = 0;
        int swingableVisibleCount = 0;

        Transform overrideTarget = null;
        bool hasLineOfSightToPlayer = !Physics.Linecast(origin, transform.position, physicsProperties.grappleBlockMask);

        //Smart Target Searching Around The Player
        foreach (Collider col in hits)
        {
            GrappleProperites props = col.GetComponent<GrappleProperites>();
            if (props == null || props.properties == GrappleProps.UnInteractable) continue;

            Vector3 worldPos = col.transform.position;
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(worldPos);
            if (screenPoint.z < 0 || screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1) continue;

            if (Physics.Linecast(origin, worldPos, out RaycastHit obstructHit, physicsProperties.grappleBlockMask))
                if (obstructHit.transform != col.transform) continue;

            if (props.properties == GrappleProps.Swingable) swingableVisibleCount++;
            else if (props.properties == GrappleProps.Pullable) pullableVisibleCount++;

            if (!physicsProperties.pullMode && hasLineOfSightToPlayer)
            {
                Vector3 dirTo = (worldPos - origin).normalized;
                if (Vector3.Dot(forward, dirTo) > 0.98f)
                    overrideTarget = col.transform;
            }
        }

        foreach (Collider col in hits)
        {
            GrappleProperites props = col.GetComponent<GrappleProperites>();
            if (props == null || props.properties == GrappleProps.UnInteractable) continue;

            Vector3 worldPos = col.transform.position;
            Vector3 screenPoint = Camera.main.WorldToViewportPoint(worldPos);
            if (screenPoint.z < 0 || screenPoint.x < 0 || screenPoint.x > 1 || screenPoint.y < 0 || screenPoint.y > 1) continue;

            if (Physics.Linecast(origin, worldPos, out RaycastHit obstructHit, physicsProperties.grappleBlockMask))
                if (obstructHit.transform != col.transform) continue;

            float screenDist = Vector2.Distance(new Vector2(screenPoint.x, screenPoint.y), new Vector2(0.5f, 0.5f));
            float worldDist = Vector3.Distance(transform.position, worldPos);
            bool isTooClose = worldDist < 1.5f;

            switch (props.properties)
            {
                case GrappleProps.Swingable:
                    if (screenDist < bestSwingableScreenDist)
                    {
                        bestSwingableScreenDist = screenDist;
                        bestSwingableTarget = col.transform;
                    }
                    break;

                case GrappleProps.Pullable:
                    if (isTooClose && (pullableVisibleCount > 1 || swingableVisibleCount > 0)) continue;
                    if (worldDist < bestPullableWorldDist && screenDist < bestPullableScreenDist)
                    {
                        bestPullableWorldDist = worldDist;
                        bestPullableScreenDist = screenDist;
                        bestPullableTarget = col.transform;
                    }
                    break;
            }
        }
        

        Transform bestTarget = null;

        if (PlayerSettings.Instance.gameplaySettings.Mode == CameraMode.TargetMode && currentZTarget != null)
        {
            currentGrappleTarget = currentZTarget;
        }

        //Z Targeting Focus 
        if (currentZTarget != null)
        {
            GrappleProperites zProps = currentZTarget.GetComponent<GrappleProperites>();
            if (zProps != null && zProps.properties != GrappleProps.UnInteractable)
            {
                float zDist = Vector3.Distance(transform.position, currentZTarget.position);
                if (zDist <= grappleRange && !Physics.Linecast(origin, currentZTarget.position, physicsProperties.grappleBlockMask))
                {
                    bestTarget = currentZTarget;
                    physicsProperties.GrappleVector = currentZTarget.position;

                    if (physicsProperties.GrappleAnchor == null)
                    {
                        GameObject hookAnchor = new GameObject("ZTargetGrappleAnchor");
                        hookAnchor.transform.position = currentZTarget.position;
                        hookAnchor.transform.parent = currentZTarget;
                        physicsProperties.GrappleAnchor = hookAnchor.transform;
                    }
                }
            }
        }

        if (bestTarget == null)
        {
            bestTarget = overrideTarget ?? (bestPullableTarget != null && bestSwingableTarget != null
                ? (bestPullableWorldDist < Vector3.Distance(transform.position, bestSwingableTarget.position)
                    ? bestPullableTarget : bestSwingableTarget)
                : bestPullableTarget ?? bestSwingableTarget);
        }

        currentGrappleTarget = PlayerSettings.Instance.gameplaySettings.Mode.Equals(CameraMode.TargetMode) && currentZTarget != null
            ? currentZTarget : bestTarget;

        float distance = 0;

        if (bestTarget != null)
        {
            distance = Vector3.Distance(transform.position, bestTarget.transform.position);
        }

        if (scheme.WasPressedThisFrame(KeyActions.Grapple) && bestTarget != null && !Stats.IsStatusActive(StatusEffectType.Stun) &&
            !physicsProperties.swingMode && !physicsProperties.pullingToSwingPoint && !physicsProperties.pullingToGrapplePoint && 
            !physicsProperties.pullMode && cooldownManager.CooldownState("Grapple Hook") && distance >= 10f && !physicsProperties.physicallyTouchedObjects.Contains(bestTarget.gameObject))
        {
            physicsProperties.GrappleObject = bestTarget;

            Vector3 directionToTarget = (bestTarget.position - transform.position).normalized;

            if (Physics.Raycast(transform.position, directionToTarget, out RaycastHit grappleHit, grappleRange))
            {
                GameObject hookAnchor = new GameObject("GrappleAnchor");
                hookAnchor.transform.position = grappleHit.point;
                hookAnchor.transform.parent = bestTarget;
                physicsProperties.GrappleAnchor = hookAnchor.transform;
                physicsProperties.GrappleVector = grappleHit.point;
            }
            else if (physicsProperties.GrappleAnchor == null)
            {
                GameObject hookAnchor = new GameObject("FallbackGrappleAnchor");
                hookAnchor.transform.position = bestTarget.position;
                hookAnchor.transform.parent = bestTarget;
                physicsProperties.GrappleAnchor = hookAnchor.transform;
                physicsProperties.GrappleVector = bestTarget.position;
            }

            GrappleProps propType = bestTarget.GetComponent<GrappleProperites>().properties;
            switch (propType)
            {
                case GrappleProps.Swingable:
                    if ((physicsProperties.GrappleVector.y - transform.position.y) >= 1f)
                        physicsProperties.swingMode = true;
                    PlayerEvents.OnGrappleTrigger();
                    break;

                case GrappleProps.Pullable:
                    physicsProperties.pullMode = true;
                    physicsProperties.isPulling = true;

                    if (bestTarget.TryGetComponent(out Rigidbody rb))
                    {
                        GrappleObjGravityState = rb.useGravity;
                        GrappleObjKinematicState = rb.isKinematic;
                    }
                    PlayerEvents.OnGrappleTrigger();
                    break;
            }

            cooldownManager.IntiateCooldown("Grapple Hook");
        }

        if (physicsProperties.swingMode && scheme.IsHeld(KeyActions.LightAttack))
        {
            transform.position = Vector3.LerpUnclamped(transform.position, physicsProperties.GrappleVector, physicsProperties.grappleSpeed * Time.deltaTime);
            physicsProperties.ApplyGravity = false;
            physicsProperties.rb.useGravity = false;
            physicsProperties.rb.linearVelocity = Vector3.zero;

            Quaternion q = Quaternion.FromToRotation(transform.forward, transform.up) * Quaternion.LookRotation(physicsProperties.GrappleVector - transform.position, transform.up);
            Vector3 angles = new Vector3(q.eulerAngles.x, transform.eulerAngles.y, 0);
            transform.rotation = Quaternion.Euler(angles);

            springJoint.minDistance -= distance;
            springJoint.maxDistance -= distance;
        }

        if (physicsProperties.swingMode)
        {
            physicsProperties.movementLock = true;
            physicsProperties.turnLock = true;

            if (!createSwingPhysics)
            {
                springJoint = gameObject.AddComponent<SpringJoint>();
                springJoint.autoConfigureConnectedAnchor = false;
                springJoint.connectedAnchor = physicsProperties.GrappleAnchor.position;
                springJoint.maxDistance = 15f;
                springJoint.minDistance = physicsProperties.grapplebreakDistance;
                springJoint.spring = 25f;
                springJoint.damper = 3f;
                springJoint.massScale = 30f;
                createSwingPhysics = true;
            }
            else
            {
                springJoint.connectedAnchor = physicsProperties.GrappleAnchor.position;
            }

            float input = InputManager.Instance.PlayerInputScheme.Vertical;
            Vector3 toGrapple = physicsProperties.GrappleVector - transform.position;
            Vector3 swingDir = Vector3.ProjectOnPlane(transform.forward, toGrapple.normalized).normalized;

            if (Mathf.Abs(input) > 0.1f)
            {
                physicsProperties.rb.AddForce(swingDir * input * physicsProperties.swingForce, ForceMode.Acceleration);
            }

            physicsProperties.rb.AddForce(Vector3.down * 5f, ForceMode.Acceleration);

            if (physicsProperties.rb.linearVelocity.magnitude > physicsProperties.maxSwingSpeed)
                physicsProperties.rb.linearVelocity = Vector3.ClampMagnitude(physicsProperties.rb.linearVelocity, physicsProperties.maxSwingSpeed);

            if (physicsProperties.GrappleAnchor != null)
                physicsProperties.GrappleVector = physicsProperties.GrappleAnchor.position;
        }

        //Grapple Object Towards The Player
        if (physicsProperties.pullMode && physicsProperties.isPulling && physicsProperties.GrappleObject != null)
        {
            physicsProperties.ApplyGravity = false;
            physicsProperties.rb.useGravity = false;
            physicsProperties.rb.linearVelocity = Vector3.zero;
            physicsProperties.velocity = Vector3.zero;
            physicsProperties.vel = Vector3.zero;

            physicsProperties.movementLock = true;
            physicsProperties.turnLock = true;

            Transform target = physicsProperties.GrappleObject;
            float step = physicsProperties.pullSpeed * Time.deltaTime;
            target.position = Vector3.MoveTowards(target.position, transform.position, step);

            float breakdistance = Vector3.Distance(target.position, transform.position);
            if (breakdistance <= physicsProperties.grapplebreakDistance)
            {
                if (target.TryGetComponent(out Rigidbody targetRb))
                {
                    targetRb.linearVelocity = Vector3.zero;
                    targetRb.useGravity = GrappleObjGravityState;
                    targetRb.isKinematic = GrappleObjKinematicState;
                }

                physicsProperties.movementLock = false;
                physicsProperties.turnLock = false;
                ViperessGrappleStopPulling();
            }
        }
    }
    /// <summary>
    /// Safely disengages the current grapple, resetting physics, releasing target objects,
    /// and optionally applying a launch force based on current swing momentum.
    /// </summary>
    public void ViperessGrappleStopPulling()
    {
        physicsProperties.numberOfTotalJumpsAvaliable = 2;
        physicsProperties.movementLock = false;
        physicsProperties.turnLock = false;

        print("DISENGAGED");

        //Releasing all constraints
        transform.SetParent(null);

        physicsProperties.GrappleObject.GetComponent<Rigidbody>().isKinematic = GrappleObjKinematicState;
        physicsProperties.GrappleObject.GetComponent<Rigidbody>().useGravity = GrappleObjGravityState;

        physicsProperties.GrappleObject = null;

        physicsProperties.ApplyGravity = true;
        physicsProperties.rb.useGravity = true;
        physicsProperties.rb.isKinematic = false;
        
        physicsProperties.rb.linearVelocity = Vector3.zero;
        physicsProperties.velocity = Vector3.zero;
        physicsProperties.vel = Vector3.zero;
        physicsProperties.gravityMultiplier = 0;

        if (physicsProperties.GrappleObject != null)
        {
            switch (physicsProperties.GrappleObject.GetComponent<GrappleProperites>().properties)
            {
                case GrappleProps.Swingable:
                    physicsProperties.swingMode = false;
                    break;

                case GrappleProps.Pullable:
                    physicsProperties.pullMode = false;
                    break;
            }
        }

        if (physicsProperties.pullMode)
        {
            physicsProperties.pullMode = false;
        }

        if (physicsProperties.swingMode)
        {
            physicsProperties.swingMode = false;
        }

        Destroy(springJoint);

        if (springJoint != null)
        {
            Destroy(physicsProperties.GrappleAnchor.gameObject);
            physicsProperties.GrappleAnchor = null;
        }

        if (physicsProperties.GrappleAnchor != null)
        {
            Destroy(physicsProperties.GrappleAnchor.gameObject);
            physicsProperties.GrappleAnchor = null;
        }

        createSwingPhysics = false;

        transform.rotation = baseRotation;

        Vector3 currentVelocity = physicsProperties.rb.linearVelocity;

        // Use horizontal movement direction only (to prevent launching straight up/down unless intended)
        Vector3 launchDirection = new Vector3(currentVelocity.x, 0f, currentVelocity.z).normalized;

        if (launchDirection.magnitude > 0.1f)
        {
            float launchForce = currentVelocity.magnitude * physicsProperties.grappleLeapForce; // You can define this in your physicsProperties
            physicsProperties.rb.AddForce(launchDirection * launchForce, ForceMode.VelocityChange);
        }

        if(physicsProperties.MoveType.Equals(MovementType.FPSMove))
        {
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0);
        }

        physicsProperties.GrappleObject = null;
    }
    float currGrappleLeapTime = 0;
    /// <summary>
    /// Handles mid-grapple disengagements, including jump interrupts or forced breaks from stun or invalid targets.
    /// Applies jump force when swinging, and safely resets states.
    /// </summary>

    void ViperessGrappleDisengage()
    {
        var scheme = InputManager.Instance.PlayerInputScheme;

        // Disengage from Swing (with Jump Impulse)
        if (physicsProperties.swingMode && scheme.IsHeld(KeyActions.Jump))
        {
            if (!Stats.IsStatusActive(StatusEffectType.Stun))
            {
                Rigidbody rb = physicsProperties.rb;

                float swingSpeed = rb.linearVelocity.magnitude;

                // Scale multipliers — tweak for feel
                float velocityHeightBoost = swingSpeed * 0.2f;
                float velocityForwardBoost = swingSpeed * 0.15f;

                // Base jump boost + velocity-influenced force
                Vector3 jumpBoost =
                    Vector3.up * (physicsProperties.grappleLeapHeight + velocityHeightBoost) +
                    transform.forward * (physicsProperties.grappleLeapForce + velocityForwardBoost);

                // Apply impulse force
                rb.AddForce(jumpBoost, ForceMode.VelocityChange);
            }

            ViperessGrappleStopPulling();
        }


        // Disengage from Pull (no impulse)
        if (physicsProperties.pullMode && scheme.WasPressedThisFrame(KeyActions.Jump))
        {
            ViperessGrappleStopPulling();
        }

        // Forced Disengage Conditions
        if ((physicsProperties.swingMode || physicsProperties.pullMode) && Stats.IsStatusActive(StatusEffectType.Stun))
        {
            ViperessGrappleStopPulling();
        }

        // Disengage if GrappleObject is null
        if ((physicsProperties.pullMode || physicsProperties.pullingToGrapplePoint) && physicsProperties.GrappleObject == null)
        {
            ViperessGrappleStopPulling();
        }
    }
    /// <summary>
    /// Manages the visual rendering of the grapple line.
    /// Draws a line from the player's origin to the current grapple point if a grapple is active.
    /// </summary>
    void GrappleLineRender()
    {
        if (physicsProperties.grappleLine == null) return;

        bool isGrappling = physicsProperties.swingMode || physicsProperties.pullMode;

        physicsProperties.grappleLine.enabled = isGrappling;

        if (!isGrappling || physicsProperties.GrappleObject == null) return;

        // Update the endpoint position to track the live position of the grapple anchor or object
        Vector3 targetPosition = physicsProperties.GrappleAnchor != null
            ? physicsProperties.GrappleAnchor.position
            : physicsProperties.GrappleObject.position;

        physicsProperties.grappleLine.SetPosition(0, physicsProperties.lineOrigin.position);
        physicsProperties.grappleLine.SetPosition(1, targetPosition);
    }


    #endregion

    #region Grinding 
    /// <summary>
    /// Triggers when the player enters a grind rail.
    /// Disables gravity and marks the player as grinding.
    /// </summary>

    void EnterGrind()
    {
        physicsProperties.isGrinding = true;
        physicsProperties.ApplyGravity = false;
        physicsProperties.rb.useGravity = false;
        PlayerEvents.OnGrindEnterTrigger();
    }
    private Vector3 ClosestGrindPoint;
    /// <summary>
    /// Handles grinding movement along a rail path.
    /// Adjusts acceleration based on rail slope and player input, calculates distance traveled,
    /// manages rotation, and triggers leaping off under specific conditions like end-of-rail or manual jump input.
    /// </summary>

    void Grinding()
    {
        //Fix The Angle So You Don't accelerate at middle angles between the extremes
        //In The Future Taking Direct Damage Should Knock You Off The Grind Rail

        var scheme = InputManager.Instance.PlayerInputScheme;

        if (physicsProperties.isGrinding && ProgressionInv.Instance.hasJadeSerpentBangle)
        {
            physicsProperties.numberOfTotalJumpsAvaliable = physicsProperties.numberOfTotalJumpsMax;

            //Get Direction Of Player Relative To Rail
            Vector3 Direction = transform.position - physicsProperties.GrindPath.path.GetClosestPointOnPath(transform.position);
            //Get Angle Of Rail To Player
            float CurrentAngle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg;

            //Grind Physics Acceleration
            physicsProperties.physicalGrindForceAcceleration = Mathf.Clamp(physicsProperties.physicalGrindForceAcceleration, -physicsProperties.maximumGrindAcceleration, physicsProperties.maximumGrindAcceleration);

            if (physicsProperties.GrindDirection == 1)
            {
                //Determine If The Rail is Flat
                if (CurrentAngle > 85 && CurrentAngle <= 90)
                {
                    //Do Not Apply Forces
                    print("Direction: " + physicsProperties.GrindDirection + " Rot Angle: " + CurrentAngle);
                }
                //This Means We are On A Slanted Rail And Should Apply Forces
                else
                {
                    //Speed Up
                    if (CurrentAngle < physicsProperties.GrindAngleLimit)
                    {
                        physicsProperties.physicalGrindForceAcceleration += physicsProperties.grindforceAccelRate * Time.deltaTime;
                        print("Direction: " + physicsProperties.GrindDirection + "  Speeding" + " Rot Angle: " + CurrentAngle);
                    }

                    //Slow Down
                    if (CurrentAngle > physicsProperties.GrindAngleLimit)
                    {
                        physicsProperties.physicalGrindForceAcceleration -= physicsProperties.grindforceDeaccelRate * Time.deltaTime;
                        print("Direction: " + physicsProperties.GrindDirection + "  Slowing" + " Rot Angle: " + CurrentAngle);
                    }
                }
            }
            //Reverse The Additive Calculation When Going Backwards On The Path CurrentAngle < 80
            else
            {
                //Determine If The Rail is Flat
                if (CurrentAngle > 85 && CurrentAngle <= 90)
                {
                    //Do Not Apply Forces
                    print("Direction: " + physicsProperties.GrindDirection + " Rot Angle: " + CurrentAngle);
                }
                //This Means We are On A Slanted Rail And Should Apply Forces
                else
                {
                    //Speed Up
                    if (CurrentAngle > physicsProperties.GrindAngleLimit)
                    {
                        physicsProperties.physicalGrindForceAcceleration += physicsProperties.grindforceAccelRate * Time.deltaTime;
                        print("Direction: " + physicsProperties.GrindDirection + "  Speeding" + " Rot Angle: " + CurrentAngle);
                    }

                    //Slow Down
                    if (CurrentAngle < physicsProperties.GrindAngleLimit)
                    {
                        physicsProperties.physicalGrindForceAcceleration -= physicsProperties.grindforceDeaccelRate * Time.deltaTime;
                        print("Direction: " + physicsProperties.GrindDirection + "  Slowing" + " Rot Angle: " + CurrentAngle);
                    }
                }
            }

            //Grind Speed And Movement Is Determined Here
            //This can allow to pass a endofpathinstruction to stop movement or loop the movement upon reaching the end of a path as well
            if (!scheme.IsHeld(KeyActions.Dodge))
            {
                physicsProperties.grindDistanceTraveled += physicsProperties.GrindDirection * ((physicsProperties.grindSpeed * Stats.GetStatValue(StatType.MovementSpeed)) + (physicsProperties.grindSpeed + physicsProperties.physicalGrindForceAcceleration)) * Time.deltaTime;
            }
            else
            {
                physicsProperties.grindDistanceTraveled += physicsProperties.GrindDirection * ((physicsProperties.grindSpeed * Stats.GetStatValue(StatType.MovementSpeed)) + (physicsProperties.boostgrindSpeed + physicsProperties.physicalGrindForceAcceleration)) * Time.deltaTime;
            }
            //Move Object To Grind On The Path
            if (!physicsProperties.GrindPath.bezierPath.isClosed)
            {
                transform.position = physicsProperties.GrindPath.path.GetPointAtDistance(physicsProperties.grindDistanceTraveled, EndOfPathInstruction.Stop) + physicsProperties.GrindRailOffset;
            }
            else
            {
                transform.position = physicsProperties.GrindPath.path.GetPointAtDistance(physicsProperties.grindDistanceTraveled, EndOfPathInstruction.Loop) + physicsProperties.GrindRailOffset;
            }

            //Determine Look Direction
            if (physicsProperties.GrindDirection == 1)
            {
                //Affix Rotation to match the forward path
                transform.rotation = physicsProperties.GrindPath.path.GetRotationAtDistance(physicsProperties.grindDistanceTraveled);
                //print("End Point Dist: " + Vector3.Distance(transform.position + physicsProperties.GrindRailOffset, physicsProperties.GrindPath.path.GetPoint(physicsProperties.GrindPath.path.NumPoints - 1)));
            }
            else
            {
                //Affix Rotation to match the backwards path
                //Get Negative Rotation Is My Own Custom Method
                transform.rotation = physicsProperties.GrindPath.path.GetNegativeRotationAtDistance(physicsProperties.grindDistanceTraveled);
                //print("Start Point Dist: " + Vector3.Distance(transform.position + physicsProperties.GrindRailOffset, physicsProperties.GrindPath.path.GetPoint(0)));
            }

            if (physicsProperties.GrindDirection == 1 && !physicsProperties.GrindPath.bezierPath.isClosed)
            {
                //When close enough to an end point jump off the path when facing the end point
                if (Vector3.Distance(transform.position + physicsProperties.GrindRailOffset, physicsProperties.GrindPath.path.GetPoint(physicsProperties.GrindPath.path.NumPoints - 1)) <= 2)
                {
                    physicsProperties.leapOffGrindPath = true;
                    physicsProperties.applyGrindLeapForwardForce = true;
                    scheme.reverseInput = true;
                }

                print("Directional Leap: " + physicsProperties.physicalDirection.z);

                //Jump Off Because The Force Caused You To Slip To The Starting Point With Negative Acceleration
                if (Vector3.Distance(transform.position + physicsProperties.GrindRailOffset, physicsProperties.GrindPath.path.GetPoint(0)) <= 2 && !physicsProperties.leapOffGrindPath &&  CooldownManager.Instance.CooldownState("Grind Release"))
                {
                    physicsProperties.leapOffGrindPath = true;

                    //Causes The Jump To Be Backwards
                    physicsProperties.applyGrindBackwardsLeapForce = true;
                    print("Ending Point End");
                }

                //Jump Off Because The Force Caused You To Slip To The Starting Point With Negative Acceleration
                if (Vector3.Distance(transform.position + physicsProperties.GrindRailOffset, physicsProperties.GrindPath.path.GetPoint(physicsProperties.GrindPath.path.NumPoints - 1)) <= 2 && physicsProperties.physicalDirection.z < 0)
                {
                    physicsProperties.leapOffGrindPath = true;

                    //Causes The Jump To Be Backwards
                    physicsProperties.applyGrindBackwardsLeapForce = true;
                    scheme.reverseInput = true;
                    print("Starting Point End");
                }
            }

            if (physicsProperties.GrindDirection == -1 && !physicsProperties.GrindPath.bezierPath.isClosed)
            {
                //When close enough to an end point jump off the path when facing the start point
                if (Vector3.Distance(transform.position + physicsProperties.GrindRailOffset, physicsProperties.GrindPath.path.GetPoint(0)) <= 2 && !physicsProperties.leapOffGrindPath &&  CooldownManager.Instance.CooldownState("Grind Release"))
                {
                    physicsProperties.leapOffGrindPath = true;
                    physicsProperties.applyGrindLeapForwardForce = true;
                    scheme.reverseInput = true;
                }

                print("Directional Leap: " + physicsProperties.physicalDirection.z);

                //Jump Off Because The Force Caused You To Slip To The Starting Point With Negative Acceleration
                if (Vector3.Distance(transform.position + physicsProperties.GrindRailOffset, physicsProperties.GrindPath.path.GetPoint(0)) <= 2 && physicsProperties.physicalDirection.z < 0)
                {
                    physicsProperties.leapOffGrindPath = true;

                    //Causes The Jump To Be Backwards
                    physicsProperties.applyGrindBackwardsLeapForce = true;
                    scheme.reverseInput = true;
                    print("Ending Point End");
                }

                //Jump Off Because The Force Caused You To Slip To The Starting Point With Negative Acceleration
                if (Vector3.Distance(transform.position + physicsProperties.GrindRailOffset, physicsProperties.GrindPath.path.GetPoint(physicsProperties.GrindPath.path.NumPoints - 1)) <= 2 && physicsProperties.physicalDirection.z < 0)
                {
                    physicsProperties.leapOffGrindPath = true;

                    //Causes The Jump To Be Backwards
                    physicsProperties.applyGrindBackwardsLeapForce = true;
                    scheme.reverseInput = true;
                    print("Starting Point End");
                }
            }

            //Leap Off Grind Path
            if (scheme.IsHeld(KeyActions.Jump))
            {
                physicsProperties.leapOffGrindPath = true;
                physicsProperties.applyGrindLeapForwardForce = true;
            }
        }

        //When Grounded Set Grind Force To 0
        if (physicsProperties.Grounded)
        {
            physicsProperties.physicalGrindForceAcceleration = 0;
            physicsProperties.physicalPassOverGrindForce = 0;
            scheme.reverseInput = false;
        }
    }
    /// <summary>
    /// Ends the grind sequence and restores player physics such as gravity and movement speed.
    /// Also initiates grind cooldown and resets grind-specific force values.
    /// </summary>

    public void ExitGrind()
    {
        Vector3 lookrotforward = new Vector3(transform.forward.x, transform.forward.y, transform.forward.z);

        //Reset Rotation
        lookrotforward.x = 0;
        lookrotforward.z = 0;
        //transform.rotation = Quaternion.LookRotation(lookrotforward);


        physicsProperties.isGrinding = false;
        physicsProperties.ApplyGravity = true;
        physicsProperties.rb.useGravity = true;
        physicsProperties.grindDistanceTraveled = 0;
        physicsProperties.physicalPassOverGrindForce = physicsProperties.physicalGrindForceAcceleration;
        physicsProperties.movementDuration = physicsProperties.runThreshold;
        physicsProperties.LevelOfSpeed = RunSpeedLevel.Run_Level_2_Run;

        PlayerEvents.OnGrindExitTrigger();

        CooldownManager.Instance.IntiateCooldown("Rail Grind");
    }
    #endregion

    #region Dodging
    Vector3 DashDir = Vector3.zero;
    bool DodgeReleased = true;
    /// <summary>
    /// Initiates a dodge/dash action when the correct input conditions are met.
    /// Handles both grounded and aerial dodge activation logic.
    /// Prevents activation when climbing, stunned, grappling, or in air-restricted states.
    /// </summary>
    void Dodge()
    {
        var scheme = InputManager.Instance.PlayerInputScheme;

        //Grounded Dash
        if (scheme.IsHeld(KeyActions.Dodge))
        {
            if (!Physics.Raycast(transform.position, transform.forward, 3f) && physicsProperties.Grounded && !physicsProperties.isNimbusInUse && !physicsProperties.isBoosting && !physicsProperties.OverridePreventDodging && !physicsProperties.isGrinding && !physicsProperties.descending &&  !physicsProperties.movementLock &&  CooldownManager.Instance.CooldownState("Dodge") && !physicsProperties.dashing && !physicsProperties.swingMode && !physicsProperties.pullMode && !physicsProperties.pullingToGrapplePoint && !physicsProperties.pullingToSwingPoint && !Stats.IsStatusActive(StatusEffectType.Stun))
            {
                PlayerEvents.OnDodgeTrigger();
                DashDir = new Vector3(physicsProperties.move.x, 0, physicsProperties.move.z);
                currDodgeDuration = (physicsProperties.MaxDodgeDuration + (physicsProperties.MaxDodgeDuration * Stats.GetStatValue(StatType.DashDuration)));
                physicsProperties.dashing = true;
                DodgeReleased = false;
                disableWallKick = false;
                print("Ground Dodge Action");
            }
        }

        //Air Dash
        if (scheme.IsHeld(KeyActions.Dodge))
        {
            if (!Physics.Raycast(transform.position, transform.forward, 3f) && !physicsProperties.Grounded && !physicsProperties.isNimbusInUse && !physicsProperties.isBoosting && !physicsProperties.OverridePreventDodging && !physicsProperties.isGrinding  && !physicsProperties.descending &&  !physicsProperties.movementLock &&  CooldownManager.Instance.CooldownState("Dodge") && !physicsProperties.dashing && !physicsProperties.swingMode && !physicsProperties.pullMode && !physicsProperties.pullingToGrapplePoint && !physicsProperties.pullingToSwingPoint && !Stats.IsStatusActive(StatusEffectType.Stun))
            {
                PlayerEvents.OnDodgeTrigger();
                DashDir = new Vector3(physicsProperties.move.x, 0, physicsProperties.move.z);
                currDodgeDuration = (physicsProperties.MaxDodgeDuration + (physicsProperties.MaxDodgeDuration * Stats.GetStatValue(StatType.DashDuration)));
                physicsProperties.dashing = true;
                DodgeReleased = false;
                disableWallKick = false;
                print("Air Dodge Action");
            }
        }

        if (scheme.IsHeld(KeyActions.Dodge) && !physicsProperties.dashing)
        {
            DodgeReleased = true;
        }
    }
    RaycastHit Dodgehit;
    /// <summary>
    /// Controls the actual movement and physics during an active dodge/dash.
    /// Includes slope detection, cooldown enforcement, directional control, and position translation.
    /// Adjusts dash cancellation if slope is too steep or blocked by a wall.
    /// </summary>
    void DodgeLogic()
    {
        //Side Step
        if (physicsProperties.dashing && !physicsProperties.swingMode && !physicsProperties.pullMode && !Stats.IsStatusActive(StatusEffectType.Stun))
        {
            if (!physicsProperties.isTempestKicking)
            {
                //physicsProperties.CapsuleCol.isTrigger = true;
            }

            physicsProperties.groundDetectionToggle = false;

            //Disable Gravity
            physicsProperties.ApplyGravity = false;
            physicsProperties.rb.linearVelocity = Vector3.zero;
            physicsProperties.gravityMultiplier = 0;
            physicsProperties.rb.useGravity = false;

            currDodgeDuration -= Time.deltaTime * 1.5f;

            physicsProperties.currentMovementSpeed = (physicsProperties.DodgeSpeed);

            //Move Transform
            if (physicsProperties.MoveType.Equals(MovementType.FreeMove))
            {
                // 1. Perform predictive spherecast in dash direction
                if (Physics.SphereCast(transform.position + Vector3.up * 0.15f, 1f, transform.forward, out RaycastHit slopeHit, 1.5f))
                {
                    float slopeAngle = Vector3.Angle(slopeHit.normal, Vector3.up);

                    // 2. Cancel dash if slope is too steep
                    if (slopeAngle >= 25)
                    {
                         CooldownManager.Instance.IntiateCooldown("Dodge");

                        //Set Run State To True
                        physicsProperties.CapsuleCol.isTrigger = false;
                        physicsProperties.ApplyGravity = true;
                        physicsProperties.rb.useGravity = true;

                        DashDir = Vector3.zero;
                        currDodgeDuration = physicsProperties.MaxDodgeDuration;

                        Debug.Log($"Dash stopped - Hit {slopeAngle}° slope");
                        physicsProperties.dashing = false;
                    }
                }

                if (Physics.Raycast(transform.position, transform.forward, 3f))
                {
                     CooldownManager.Instance.IntiateCooldown("Dodge");

                    //Set Run State To True
                    physicsProperties.CapsuleCol.isTrigger = false;
                    physicsProperties.ApplyGravity = true;
                    physicsProperties.rb.useGravity = true;

                    DashDir = Vector3.zero;
                    currDodgeDuration = physicsProperties.MaxDodgeDuration;
                    physicsProperties.dashing = false;
                }

                // 1. Get the intended dash direction (character's forward)
                Vector3 intendedDirection = transform.forward;

                // 2. Detect ground slope
                Vector3 groundNormal = Dodgehit.normal;
                Vector3 slopeAdjustedDirection = Vector3.ProjectOnPlane(intendedDirection, groundNormal).normalized;

                // 3. Apply slight upward boost when dashing uphill
                float upwardBoost = Mathf.Clamp01(Vector3.Dot(slopeAdjustedDirection, Vector3.up)) * 0.5f;
                slopeAdjustedDirection += Vector3.up * upwardBoost;

                physicsProperties.vel = new Vector3(physicsProperties.velocity.x, physicsProperties.velocity.y, slopeAdjustedDirection.z + physicsProperties.currentMovementSpeed * 1.2f);

                //Set Direction
                physicsProperties.vel = transform.TransformDirection(physicsProperties.vel);
                transform.TransformDirection(transform.forward);

                //Actively Moving
                //Stops Directional Upward Movement
                physicsProperties.vel.y = 0;
                transform.position += physicsProperties.vel * Time.deltaTime;
                physicsProperties.velocity = Vector3.zero;
            }

            if (physicsProperties.MoveType.Equals(MovementType.Strafe) || physicsProperties.MoveType.Equals(MovementType.FPSMove))
            {
                //Dash Forward If No Keys Are Hit
                if (DashDir == Vector3.zero)
                {
                    physicsProperties.vel = new Vector3(physicsProperties.velocity.x, physicsProperties.velocity.y, physicsProperties.velocity.z + physicsProperties.currentMovementSpeed * 1.2f);

                    //Set Direction
                    physicsProperties.vel = transform.TransformDirection(physicsProperties.vel);
                    transform.TransformDirection(transform.forward);

                    //Actively Moving
                    transform.position += physicsProperties.vel * Time.deltaTime;
                    physicsProperties.velocity = Vector3.zero;
                }
                //Dash In Our Direction Our Keys Are Being Pressed
                else
                {
                    physicsProperties.vel = new Vector3(DashDir.x * physicsProperties.currentMovementSpeed, physicsProperties.velocity.y, DashDir.z * physicsProperties.currentMovementSpeed * 1.2f);

                    //Set Direction
                    physicsProperties.vel = transform.TransformDirection(physicsProperties.vel);
                    transform.TransformDirection(physicsProperties.move);

                    //Actively Moving
                    transform.position += physicsProperties.vel * Time.deltaTime;
                    physicsProperties.velocity = Vector3.zero;
                }
            }
        }

        //Cancel Mid Dash
        if (Stats.IsStatusActive(StatusEffectType.Stun))
        {
            physicsProperties.CapsuleCol.isTrigger = false;

            physicsProperties.dashing = false;
            physicsProperties.ApplyGravity = true;
            physicsProperties.rb.useGravity = true;

            DashDir = Vector3.zero;
        }

        if (currDodgeDuration <= 0 && physicsProperties.dashing)
        {
             CooldownManager.Instance.IntiateCooldown("Dodge");

            //Set Run State To True
            physicsProperties.CapsuleCol.isTrigger = false;
            physicsProperties.isRunning = true;
            physicsProperties.LevelOfSpeed = RunSpeedLevel.Run_Level_2_Run;
            physicsProperties.ApplyGravity = true;
            physicsProperties.rb.useGravity = true;

            DashDir = Vector3.zero;
            currDodgeDuration = physicsProperties.MaxDodgeDuration;
            physicsProperties.dashing = false;
        }
    }
    #endregion

    #region General Movement
    /// <summary>
    /// Executes the full movement stack in a strict order to ensure stable player motion.
    /// Includes gravity, movement, jumping, collision, and state transitions.
    /// </summary>
    void CompleteMovement()
    {
        //This Order of Gravity (); Move (); Jump (); FinalMove (); GroundChecking (); CollisionCheck (); cannot be changed or this will not work at all.
        PlatformFollowing();
        Gravity();
        Nimbus();
        Freefall();
        Move();
        Running();
        Boost();
        Jump();
        WallKick();
        GroundChecking();
        CollisionCheck();
        MoveCorrection();
    }
    /// <summary>
    /// Calculates the player's effective movement speed based on base speed and stat bonuses,
    /// with a minimum speed clamp.
    /// </summary>
    /// <param name="baseSpeed">The starting speed value before modifiers.</param>
    /// <returns>Final calculated movement speed.</returns>
    public float CalculatePlayerMovement(float baseSpeed)
    {
        float speedCalc = baseSpeed + Stats.GetStatValue(StatType.MovementSpeed);

        //Minimum Speed
        if (speedCalc < 1)
        {
            speedCalc = 1;
        }

        return (speedCalc);
    }
    /// <summary>
    /// Sets the player's current movement type (FreeMove, Strafe, FPSMove, etc.).
    /// </summary>
    /// <param name="Type">The movement type to assign.</param>

    public void SetMoveType(MovementType Type)
    {
        physicsProperties.MoveType = Type;
    }
    /// <summary>
    /// Coroutine that calculates the player's physical velocity and direction by comparing positions across frames.
    /// Used for reactive physics systems like grappling or grinding.
    /// </summary>

    IEnumerator GetPhysicalSpeed()
    {
        Vector3 lastPosOfPhysicalSpeed = transform.position;

        //Get Direction Of Player Relative To Rail
        if (physicsProperties.GrindPath != null)
        {
            Vector3 Direction = transform.position - physicsProperties.GrindPath.transform.position;
        }

        Vector3 lastDirOfMovement = transform.InverseTransformDirection(transform.forward);

        yield return new WaitForEndOfFrame();

        physicsProperties.physicalSpeed = ((lastPosOfPhysicalSpeed - transform.position) / Time.deltaTime);
        physicsProperties.physicalDirection = lastDirOfMovement;
    }
    /// <summary>
    /// Resets all movement-related vectors if the player is stunned to prevent unwanted motion.
    /// </summary>

    void MoveCorrection()
    {
        if (Stats.IsStatusActive(StatusEffectType.Stun))
        {
            physicsProperties.velocity = new Vector3(0, 0, 0);
            physicsProperties.move = Vector3.zero;
            physicsProperties.vel = Vector3.zero;
            physicsProperties.dir = Vector3.zero;
            physicsProperties.jumpHeight = 0;
        }
    }
    private float flightAcceleration = 0;
    /// <summary>
    /// Determines the player's movement direction based on input and camera orientation,
    /// applying adjustments for different movement types like FreeMove or Strafe.
    /// </summary>
    private bool wasLockedOnLastFrame = false;
    private Quaternion stableStrafeRotation;

    public void MoveDirection()
    {
        Vector3 camForward = Vector3.zero;
        Vector3 camRight = Vector3.zero;

        var input = InputManager.Instance.PlayerInputScheme.MovementVector;

        if (physicsProperties.MoveType.Equals(MovementType.FreeMove))
        {
            camForward = MyCamera.Rotater.forward;
            camForward.y = 0;
            camForward.Normalize();

            camRight = MyCamera.Rotater.right;
            camRight.y = 0;
            camRight.Normalize();
        }
        else if (physicsProperties.MoveType.Equals(MovementType.FPSMove))
        {
            // FPS uses camera forward directly (no flattening to world plane)
            camForward = transform.forward;
            camRight = transform.right;

            // Optional: zero Y if you want ground-only FPS movement
            camForward.y = 0;
            camRight.y = 0;

            camForward.Normalize();
            camRight.Normalize();
        }
        else if (physicsProperties.MoveType.Equals(MovementType.Strafe) && MyCamera.LockOnTarget != null)
        {
            Vector3 toTarget = MyCamera.LockOnTarget.transform.position - transform.position;
            toTarget.y = 0;

            if (!wasLockedOnLastFrame)
            {
                stableStrafeRotation = Quaternion.LookRotation(toTarget.normalized);
                wasLockedOnLastFrame = true;
            }

            camForward = stableStrafeRotation * Vector3.forward;
            camRight = stableStrafeRotation * Vector3.right;

            camForward.y = 0;
            camRight.y = 0;

            camForward.Normalize();
            camRight.Normalize();
        }
        else
        {
            wasLockedOnLastFrame = false;
        }

        // Create Movement Vector
        Vector3 moveDir = camForward * input.z + camRight * input.x;

        physicsProperties.velocity += moveDir;

        if (physicsProperties.Grounded)
        {
            flightAcceleration = Mathf.LerpUnclamped(flightAcceleration, 0, Time.deltaTime);
        }

        if (physicsProperties.MoveType.Equals(MovementType.Strafe))
        {
            moveDir = camForward * input.x + camRight * input.z;
        }
    }


    /// <summary>
    /// Handles player rotation based on input and camera direction.
    /// Adjusts turning for various states including grappling, pulling, strafing, and grounded movement.
    /// </summary>
    public void FaceEnemy()
    {
        if (PlayerCamera.Instance.isLockedOn && TryGetComponent<ComboManager>(out ComboManager comboMgr))
        {
            if (comboMgr.IsCurrentComboStepPlaying(out ComboStep currentStep))
            {
                var target = PlayerCamera.Instance.LockOnTarget;

                if (target != null)
                {
                    Vector3 directionToTarget = target.transform.position - transform.position;
                    directionToTarget.y = 0f; // Zero out vertical difference to avoid X tilt

                    if (directionToTarget.sqrMagnitude > 0.001f)
                    {
                        Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
                        Vector3 euler = targetRotation.eulerAngles;

                        // Keep only Y rotation, zero X and Z
                        Quaternion yOnlyRotation = Quaternion.Euler(0f, euler.y, 0f);

                        // Smooth rotate character towards the target Y rotation
                        transform.rotation = yOnlyRotation;
                    }
                }
            }
        }
    }
    void DirectionalTurning()
    {
        var scheme = InputManager.Instance.PlayerInputScheme;

        //Align Player to floor
        //transform.rotation = Quaternion.AngleAxis(gChecker.groundSlopeAngle, transform.right);

        //FPS Turn Directions
        if (physicsProperties.MoveType.Equals(MovementType.FPSMove) && !physicsProperties.turnLock && !physicsProperties.swingMode && !physicsProperties.pullingToGrapplePoint && !physicsProperties.pullingToSwingPoint && !physicsProperties.pullMode && !physicsProperties.swingMode && !Stats.IsStatusActive(StatusEffectType.Stun) && !physicsProperties.isGrinding && !physicsProperties.OverrideDirectionRotations)
        {
            if (!InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.enabled)
            {
                //Rotate Player
                transform.Rotate(Vector3.up, Input.GetAxis("Mouse X") * PlayerSettings.Instance.gameplaySettings.sensitivity * Time.deltaTime);
            }
            else
            {
                float angleBlendX = -scheme.GetInputMovementDirection().x * PlayerSettings.Instance.gameplaySettings.controllerSensitivity * Time.deltaTime;

                //Rotate Player
                transform.Rotate(Vector3.up, angleBlendX);
            }
        }

        //Free Move Directions (Normal Camera Driven Directional)
        if (physicsProperties.MoveType.Equals(MovementType.FreeMove) && !physicsProperties.turnLock && !Stats.IsStatusActive(StatusEffectType.Stun) && !physicsProperties.isGrinding && !physicsProperties.OverrideDirectionRotations)
        {
            //Input Vector
            Vector3 InputDirection = new Vector3(InputManager.Instance.PlayerInputScheme.Horizontal, InputManager.Instance.PlayerInputScheme.Vertical, 0);

            //Directional Input [Straight]
            KeyCode key0 = InputManager.Instance.PlayerInputScheme.Inputs[InputManager.Instance.PlayerInputScheme.InputKeyIndexLib[KeyActions.WalkForward]].key;
            KeyCode key1 = InputManager.Instance.PlayerInputScheme.Inputs[InputManager.Instance.PlayerInputScheme.InputKeyIndexLib[KeyActions.WalkBackwards]].key;
            KeyCode key2 = InputManager.Instance.PlayerInputScheme.Inputs[InputManager.Instance.PlayerInputScheme.InputKeyIndexLib[KeyActions.WalkLeft]].key;
            KeyCode key3 = InputManager.Instance.PlayerInputScheme.Inputs[InputManager.Instance.PlayerInputScheme.InputKeyIndexLib[KeyActions.WalkRight]].key;

            if (MyCamera != null)
            {
                // Not grappling or in special states
                if (!physicsProperties.swingMode &&
                    !physicsProperties.isWallKicking &&
                    !physicsProperties.isTempestKicking &&
                    !physicsProperties.isNimbusInUse &&
                    !physicsProperties.isFreefalling)
                {
                    // Get input direction (already includes keyboard/controller logic, reversed if needed)
                    Vector3 inputDir = scheme.GetInputMovementDirection(false);
                    inputDir.y = 0f;

                    if (inputDir.sqrMagnitude > 0.01f)
                    {
                        // Project onto camera space
                        Vector3 camForward = MyCamera.transform.forward;
                        Vector3 camRight = MyCamera.transform.right;

                        // Flatten the directions
                        camForward.y = 0f;
                        camRight.y = 0f;
                        camForward.Normalize();
                        camRight.Normalize();


                        // Build camera-relative world direction
                        Vector3 worldDir = camForward * inputDir.z + camRight * inputDir.x;

                        if (worldDir != Vector3.zero)
                        {
                            Quaternion targetRotation = Quaternion.LookRotation(worldDir);
                            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, physicsProperties.directionLookSpeed * Time.deltaTime);
                        }
                    }
                }
            }
            else
            {
                if (!physicsProperties.isNimbusInUse && !physicsProperties.isFreefalling)
                {
                    Vector3 inputDirection = new Vector3(scheme.GetInputMovementDirection().x, 0, scheme.GetInputMovementDirection().y).normalized;

                    if (inputDirection.magnitude >= 0.1f)
                    {
                        Quaternion targetRotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0) * Quaternion.LookRotation(inputDirection);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, physicsProperties.directionLookSpeed * Time.deltaTime);
                    }
                }
            }
        }

        //Grapple Mode Rotation For Player Body
        if (physicsProperties.swingMode && physicsProperties.GrappleObject.GetComponent<GrappleProperites>().properties.Equals(GrappleProps.Swingable))
        {
            currGrappleLeapTime = physicsProperties.grappleLeapTime;

            Quaternion q = Quaternion.FromToRotation(transform.up, transform.forward) * Quaternion.LookRotation(physicsProperties.GrappleVector - transform.position, transform.up);
            Vector3 angles = new Vector3(q.eulerAngles.x, transform.eulerAngles.y, 0);
            transform.rotation = Quaternion.Euler(angles);

            if (physicsProperties.physicalSpeed.magnitude < 2)
            {
                Vector3 Goal = new Vector3(transform.eulerAngles.x, MyCamera.Rotater.eulerAngles.y, 0);
                Quaternion Rot = Quaternion.Euler(Goal);
                transform.rotation = Rot;
            }
        }

        //Pulling Mode Rotation
        if (physicsProperties.pullMode && physicsProperties.GrappleObject.GetComponent<GrappleProperites>().properties.Equals(GrappleProps.Pullable))
        {
            // Forward direction based on camera or aiming object
            Vector3 forward = PlayerCamera.Instance.Rotater.forward;
            forward.y = 0;
            forward.Normalize();

            // Up direction toward grapple object (adds tilt effect)
            Vector3 toGrapple = (physicsProperties.GrappleObject.position - transform.position).normalized;

            // Reconstruct a correct orientation with constrained forward and dynamic up
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 up = Vector3.Cross(forward, right).normalized;

            // Apply upward tilt toward grapple (blending the real up with toGrapple for visual hint)
            up = Vector3.Lerp(up, toGrapple, 0.5f).normalized;

            Quaternion q = Quaternion.LookRotation(forward, up);

            if (Quaternion.Angle(q, baseRotation) <= 180)
            {
                targetRotation = q;
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, physicsProperties.directionLookSpeed * (Time.deltaTime / Time.timeScale));
        }

        //Orients Player To Strafe Around Target
        if (physicsProperties.MoveType.Equals(MovementType.Strafe) && !physicsProperties.isGrinding && !physicsProperties.OverrideDirectionRotations)
        {
            if (MyCamera.LockOnTarget != null && !physicsProperties.isNimbusInUse)
            {
                Vector3 lookDir = -transform.position - -MyCamera.LockOnTarget.transform.position;
                lookDir.y = 0;

                Quaternion q = Quaternion.LookRotation(lookDir);

                if (Quaternion.Angle(q, baseRotation) <= 180)
                {
                    targetRotation = q;
                }

                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, physicsProperties.directionLookSpeed * (Time.deltaTime / Time.timeScale));
            }
        }
    }
    /// <summary>
    /// Core movement function that manages input vector processing, grounded/air/wall/grind movement behaviors,
    /// applies forces or velocity, and triggers related movement events.
    /// </summary>

    void Move()
    {
        if (!physicsProperties.movementLock && !physicsProperties.OverrideMovement)
        {
            // Reset vectors as in original code
            physicsProperties.velocity = Vector3.zero;
            physicsProperties.vel = Vector3.zero;

            // Get input vector, but force Y = 0 to prevent flying
            Vector3 inputVec = InputManager.Instance.PlayerInputScheme.MovementVector;

            // Prevent movement if an attack step is playing and uses attack motion
            if (combatSystem.IsCurrentComboStepPlaying(out var playingStep))
            {
                if (playingStep.useAttackMotion)
                {
                    physicsProperties.movementDuration = 0;
                    return;
                }
            }

            //This disable and enables hovering when holding space
            if (!Stats.CanHover())
            {
                inputVec.y = 0f;
            }
            else
            {
                PlayerEvents.OnHoverTrigger();
            }
            
            if(inputVec == Vector3.zero)
            {
                PlayerEvents.OnStandStillTrigger();
            }
            
            physicsProperties.move = Vector3.ClampMagnitude(inputVec, 1f);

            // Calculate movement direction
            MoveDirection();

            // Handle stunned state separately
            if (Stats.GetFlag(StatusEffectType.Stun))
            {
                // Simplified movement during stun
                if (physicsProperties.MoveType == MovementType.FPSMove)
                {
                    physicsProperties.velocity = physicsProperties.move;

                    // Apply movement instantly
                    physicsProperties.rb.linearVelocity = new Vector3(
                        physicsProperties.velocity.x,
                        physicsProperties.rb.linearVelocity.y,
                        physicsProperties.velocity.z
                    );
                }
                return; // Exit early for stun state
            }

            // AIR CONTROL ==============================================
            if (!physicsProperties.isFreefalling && !physicsProperties.isNimbusInUse &&
                !physicsProperties.isTempestKicking && !physicsProperties.Grounded &&
                !physicsProperties.airMovementLock && !physicsProperties.pullingToGrapplePoint &&
                !physicsProperties.pullingToSwingPoint && !physicsProperties.hovering &&
                !physicsProperties.dashing && !physicsProperties.swingMode &&
                !physicsProperties.isGrinding)
            {
                physicsProperties.currentMovementSpeed = physicsProperties.baseAirSpeed;

                // Grapple Jump Force
                if (physicsProperties.applyGrappleJumpForce)
                {
                    Vector3 dir = transform.TransformDirection(physicsProperties.rb.linearVelocity);
                    physicsProperties.vel = new Vector3(
                        physicsProperties.velocity.x + dir.x * physicsProperties.grappleLeapForce,
                        physicsProperties.velocity.y * physicsProperties.grappleLeapForce,
                        physicsProperties.velocity.z + dir.z * physicsProperties.grappleLeapForce
                    );
                    physicsProperties.rb.AddForce(physicsProperties.vel, ForceMode.Impulse);
                }
                else
                {
                    physicsProperties.vel = new Vector3(
                        physicsProperties.velocity.x * physicsProperties.currentMovementSpeed,
                        0,
                        physicsProperties.velocity.z * physicsProperties.currentMovementSpeed + physicsProperties.physicalPassOverGrindForce
                    );
                }

                // Apply movement instantly based on type
                switch (physicsProperties.MoveType)
                {
                    case MovementType.Strafe:
                        physicsProperties.rb.AddRelativeForce(
                            physicsProperties.vel * CalculatePlayerMovement(physicsProperties.airforce),
                            ForceMode.VelocityChange
                        );
                        break;

                    case MovementType.FPSMove:
                        if (!physicsProperties.isWallKicking)
                        {
                            physicsProperties.rb.AddRelativeForce(
                                physicsProperties.move * CalculatePlayerMovement(physicsProperties.airforce),
                                ForceMode.VelocityChange
                            );
                        }
                        break;

                    case MovementType.FreeMove:
                        if (!physicsProperties.leapOffGrindPath && !physicsProperties.isWallKicking)
                        {
                            physicsProperties.rb.AddRelativeForce(
                                physicsProperties.move * CalculatePlayerMovement(physicsProperties.airforce),
                                ForceMode.VelocityChange
                            );
                        }
                        break;
                }
            }

            // WALL KICK MOVEMENT =======================================
            if (!physicsProperties.isNimbusInUse && physicsProperties.isWallKicking && !physicsProperties.isTempestKicking)
            {
                physicsProperties.currentMovementSpeed = CalculatePlayerMovement(physicsProperties.baseMovementSpeed);

                physicsProperties.vel = new Vector3(0, physicsProperties.velocity.y, 1 * physicsProperties.WallKickDistance);
            }


            // GROUNDED MOVEMENT ========================================
            if (!physicsProperties.isNimbusInUse && !physicsProperties.isTempestKicking &&
                    !physicsProperties.isWallKicking && !physicsProperties.OverrideMovement &&
                    gChecker.groundSlopeAngle < physicsProperties.slopeLimit &&
                    physicsProperties.Grounded && !physicsProperties.dashing &&
                    !physicsProperties.isRunning && !physicsProperties.isGrinding)
            {
                physicsProperties.currentMovementSpeed = physicsProperties.baseMovementSpeed;
                physicsProperties.vel = new Vector3(
                    physicsProperties.velocity.x * physicsProperties.currentMovementSpeed,
                    0,
                    physicsProperties.velocity.z * physicsProperties.currentMovementSpeed
                );

                // Apply instantly
                Vector3 currentY = new Vector3(0, physicsProperties.rb.linearVelocity.y, 0);
                physicsProperties.rb.linearVelocity = currentY + new Vector3(physicsProperties.vel.x, physicsProperties.velocity.y, physicsProperties.vel.z);
            }

            // RUNNING MOVEMENT =========================================
            if (!physicsProperties.isNimbusInUse && !physicsProperties.isTempestKicking &&
                    !physicsProperties.isWallKicking && !physicsProperties.OverrideMovement &&
                    !physicsProperties.dashing && !physicsProperties.isGrinding)
            {

                if (physicsProperties.LevelOfSpeed.Equals(RunSpeedLevel.Run_Level_1_Jog))
                {
                    physicsProperties.currentMovementSpeed = CalculatePlayerMovement(physicsProperties.jogSpeed);

                    if (InputManager.Instance.PlayerInputScheme.MovementVector != Vector3.zero || physicsProperties.velocity != Vector3.zero)
                    {
                        PlayerEvents.OnWalkTrigger();
                    }
                }

                if (physicsProperties.LevelOfSpeed.Equals(RunSpeedLevel.Run_Level_2_Run))
                {
                    physicsProperties.currentMovementSpeed = CalculatePlayerMovement(physicsProperties.runSpeed);

                    if (InputManager.Instance.PlayerInputScheme.MovementVector != Vector3.zero || physicsProperties.velocity != Vector3.zero)
                    {
                        PlayerEvents.OnRunTrigger();
                    }
                }

                if (physicsProperties.LevelOfSpeed.Equals(RunSpeedLevel.Run_Level_3_TopSpeed))
                {
                    physicsProperties.currentMovementSpeed = CalculatePlayerMovement(physicsProperties.topSpeed);

                    if (InputManager.Instance.PlayerInputScheme.MovementVector != Vector3.zero || physicsProperties.velocity != Vector3.zero)
                    {
                        PlayerEvents.OnRunTrigger();
                    }
                }

                if (physicsProperties.LevelOfSpeed.Equals(RunSpeedLevel.Run_Level_4_Boost))
                {
                    physicsProperties.currentMovementSpeed = CalculatePlayerMovement(physicsProperties.boostSpeed);

                    if (InputManager.Instance.PlayerInputScheme.MovementVector != Vector3.zero || physicsProperties.velocity != Vector3.zero)
                    {
                        PlayerEvents.OnBoostTrigger();
                    }
                }

                if (physicsProperties.MoveType.Equals(MovementType.FreeMove) || physicsProperties.MoveType.Equals(MovementType.FPSMove))
                {
                    physicsProperties.vel = new Vector3(physicsProperties.velocity.x * physicsProperties.currentMovementSpeed, physicsProperties.velocity.y, physicsProperties.velocity.z * physicsProperties.currentMovementSpeed);
                }

                if (physicsProperties.MoveType.Equals(MovementType.Strafe))
                {
                    physicsProperties.vel = new Vector3(physicsProperties.velocity.x * physicsProperties.currentMovementSpeed, physicsProperties.velocity.y, physicsProperties.velocity.z * physicsProperties.currentMovementSpeed);
                }
            }

            // FINAL MOVEMENT APPLICATION ===============================
            if (!physicsProperties.dashing)
            {
                // Handle grind leap forces
                if (physicsProperties.leapOffGrindPath)
                {
                    physicsProperties.currentMovementSpeed = physicsProperties.baseMovementSpeed +
                                                          physicsProperties.baseAirSpeed +
                                                          physicsProperties.physicalPassOverGrindForce;

                    physicsProperties.groundDetectionToggle = false;

                    if (physicsProperties.applyGrindBackwardsLeapForce)
                    {
                        physicsProperties.vel = new Vector3(
                            physicsProperties.velocity.x * physicsProperties.baseAirSpeed * physicsProperties.move.x,
                            0,
                            -physicsProperties.currentMovementSpeed
                        );
                    }
                    else
                    {
                        physicsProperties.vel = new Vector3(
                            physicsProperties.velocity.x * physicsProperties.baseAirSpeed * physicsProperties.move.x,
                            0,
                            physicsProperties.currentMovementSpeed
                        );
                    }

                    physicsProperties.physicalPassOverGrindForce = Mathf.Lerp(
                        physicsProperties.physicalPassOverGrindForce,
                        0,
                        0.1f * Time.deltaTime
                    );
                }

                // Transform direction if needed
                if (physicsProperties.applyGrappleJumpForce ||
                    physicsProperties.leapOffGrindPath ||
                    physicsProperties.isWallKicking)
                {
                    physicsProperties.vel = transform.TransformDirection(physicsProperties.vel);
                }

                // Apply movement instantly based on type
                switch (physicsProperties.MoveType)
                {
                    case MovementType.FPSMove:
                    case MovementType.FreeMove:
                        // Wall collision check (your original logic)
                        if (Physics.Raycast(transform.position, transform.forward, out var wallcheck, 1.5f))
                        {
                            float surfaceAngle = Vector3.Angle(wallcheck.normal, Vector3.up);
                            if (surfaceAngle > 35)
                            {
                                float approachAngle = Vector3.Angle(transform.forward, -wallcheck.normal);
                                if (approachAngle > 30f)
                                {
                                    Vector3 blockedDirection = Vector3.Project(transform.forward, wallcheck.normal);
                                    physicsProperties.vel -= blockedDirection;

                                    if (approachAngle < 45f)
                                    {
                                        physicsProperties.vel = Vector3.zero;
                                        physicsProperties.LevelOfSpeed = RunSpeedLevel.Run_Level_1_Jog;
                                        physicsProperties.movementDuration = 0;
                                    }
                                }
                            }
                        }

                        // Apply movement instantly
                        if (!physicsProperties.isNimbusInUse)
                        {
                            physicsProperties.rb.linearVelocity = new Vector3(
                                physicsProperties.vel.x,
                                physicsProperties.rb.linearVelocity.y,
                                physicsProperties.vel.z
                            );
                        }
                        else
                        {
                            physicsProperties.rb.linearVelocity = transform.forward * physicsProperties.currNimbusSpeed;
                        }
                        break;

                    case MovementType.Strafe:
                        physicsProperties.vel = transform.TransformDirection(physicsProperties.vel);

                        if (!physicsProperties.isNimbusInUse)
                        {
                            physicsProperties.rb.linearVelocity = new Vector3(
                                physicsProperties.vel.x,
                                physicsProperties.rb.linearVelocity.y,
                                physicsProperties.vel.z
                            );
                        }
                        else
                        {
                            physicsProperties.rb.linearVelocity = transform.forward * physicsProperties.currNimbusSpeed;
                        }
                        break;
                }

                // Clean up grind states when landing
                if (physicsProperties.Grounded &&
                   (physicsProperties.applyGrindLeapForwardForce ||
                    physicsProperties.applyGrindBackwardsLeapForce))
                {
                    physicsProperties.applyGrindLeapForwardForce = false;
                    physicsProperties.applyGrindBackwardsLeapForce = false;
                    physicsProperties.leapOffGrindPath = false;
                    ExitGrind();
                }
            }
        }
    }
    #endregion

    #region Running/Boosting
    /// <summary>
    /// Triggers a temporary speed boost if the player has the Dragon Orb Sash and double-taps the dodge input.
    /// Resets the boost if directional input is released.
    /// </summary>
    void Boost()
    {
        var scheme = InputManager.Instance.PlayerInputScheme;

        bool hasSash = ProgressionInv.Instance.hasDragonOrbSash;
        if (!hasSash)
            return;

        // Double-tap Dodge to boost
        if (!physicsProperties.isNimbusInUse && scheme.WasDoubleTapped(KeyActions.Dodge))
        {
            physicsProperties.isBoosting = true;
            physicsProperties.LevelOfSpeed = RunSpeedLevel.Run_Level_4_Boost;
            physicsProperties.movementDuration = physicsProperties.topThreshold;
            PlayerEvents.OnBoostTrigger();
        }

        if (!scheme.IsHoldingDirection() && physicsProperties.isBoosting)
        {
            physicsProperties.isBoosting = false;
            physicsProperties.LevelOfSpeed = RunSpeedLevel.Run_Level_3_TopSpeed;
            physicsProperties.movementDuration = physicsProperties.topThreshold;
        }
    }
    /// <summary>
    /// Manages the player's running state and speed level progression based on movement duration, 
    /// gear effects (e.g., Jetstream Heels), and camera or status conditions.
    /// </summary>
    void Running()
    {
        if (ProgressionInv.Instance.hasJetstreamHeels)
        {
            physicsProperties.movementDuration = Mathf.Clamp(physicsProperties.movementDuration, 0f, physicsProperties.topThreshold + 0.5f);
        }
        else
        {
            physicsProperties.movementDuration = Mathf.Clamp(physicsProperties.movementDuration, 0f, physicsProperties.runThreshold + 0.5f);
        }

        if (physicsProperties.isRunning && !p_Settings.gameplaySettings.Mode.Equals(CameraMode.TargetMode) && !Stats.IsStatusActive(StatusEffectType.Stun))
        {
            //If no direction is being input then stop running
            if (InputManager.Instance.PlayerInputScheme.MovementVector == Vector3.zero)
            {
                physicsProperties.isRunning = false;
            }
        }

        if (p_Settings.gameplaySettings.Mode.Equals(CameraMode.TargetMode) || Stats.IsStatusActive(StatusEffectType.Stun))
        {
            physicsProperties.isRunning = false;
        }

        if (!physicsProperties.isBoosting && !physicsProperties.isNimbusInUse && !physicsProperties.pullMode && !physicsProperties.swingMode)
        {
            if (InputManager.Instance.PlayerInputScheme.MovementVector != Vector3.zero)
            {
                physicsProperties.movementDuration += Time.deltaTime;

                if (physicsProperties.LevelOfSpeed.Equals(RunSpeedLevel.Run_Level_1_Jog) && physicsProperties.movementDuration >= physicsProperties.runThreshold)
                {
                    physicsProperties.LevelOfSpeed = RunSpeedLevel.Run_Level_2_Run;

                }

                if (physicsProperties.LevelOfSpeed.Equals(RunSpeedLevel.Run_Level_2_Run) && physicsProperties.movementDuration >= physicsProperties.topThreshold && ProgressionInv.Instance.hasJetstreamHeels)
                {
                    physicsProperties.LevelOfSpeed = RunSpeedLevel.Run_Level_3_TopSpeed;
                }

                if (physicsProperties.LevelOfSpeed.Equals(RunSpeedLevel.Run_Level_3_TopSpeed) && physicsProperties.MoveType.Equals(MovementType.Strafe))
                {
                    physicsProperties.movementDuration = physicsProperties.runThreshold;
                    physicsProperties.LevelOfSpeed = RunSpeedLevel.Run_Level_2_Run;
                }
            }
            else
            {
                physicsProperties.movementDuration -= Time.deltaTime * 10;

                if (physicsProperties.LevelOfSpeed.Equals(RunSpeedLevel.Run_Level_2_Run) && physicsProperties.movementDuration <= physicsProperties.runThreshold)
                {
                    physicsProperties.LevelOfSpeed = RunSpeedLevel.Run_Level_1_Jog;
                }

                if (physicsProperties.LevelOfSpeed.Equals(RunSpeedLevel.Run_Level_3_TopSpeed) && physicsProperties.movementDuration <= physicsProperties.topThreshold && ProgressionInv.Instance.hasJetstreamHeels)
                {
                    physicsProperties.LevelOfSpeed = RunSpeedLevel.Run_Level_2_Run;
                }

                if (physicsProperties.LevelOfSpeed.Equals(RunSpeedLevel.Run_Level_3_TopSpeed) && physicsProperties.MoveType.Equals(MovementType.Strafe))
                {
                    physicsProperties.movementDuration = physicsProperties.runThreshold;
                    physicsProperties.LevelOfSpeed = RunSpeedLevel.Run_Level_2_Run;
                }
            }
        }
    }
    #endregion

    #region Air/Flight/Jumping Movement

    private float activeDeadZoneTimer;

    /// <summary>
    /// Calculates the player's jump height by adding base height and any jump stat bonuses.
    /// Ensures a minimum jump height of 1 unit.
    /// </summary>
    /// <param name="baseHeight">The base height used for the jump calculation.</param>
    /// <returns>The total jump height including bonuses.</returns>
    public float CalculatePlayerJump(float baseHeight)
    {
        float JumpCalc = baseHeight + Stats.GetStatValue(StatType.JumpHeight);

        //Minimum Jump
        if (JumpCalc < 1)
        {
            JumpCalc = 1;
        }

        return (JumpCalc);
    }
    /// <summary>
    /// Handles all jump behavior, including grounded jumps, double jumps, grind jumps, and wall kick logic.
    /// Adjusts jump height, updates jump counters, and fires related jump events.
    /// Prevents jumping under certain conditions like stun, rope climbing, or grapple states.
    /// </summary>
    public void Jump()
    {
        //Jump Key Input Variable
        var scheme = InputManager.Instance.PlayerInputScheme;

        //if movement is allowed from input
        if (!physicsProperties.movementLock)
        {
            if (!physicsProperties.OverrideMovement && !physicsProperties.isNimbusInUse)
            {
                //reset jump
                if (physicsProperties.Grounded && physicsProperties.PhysicallyGrounded && !physicsProperties.pullMode && !physicsProperties.pullingToGrapplePoint)
                {
                    disableWallKick = false;
                    physicsProperties.jumpHeight = 0;
                    physicsProperties.canJump = true;
                    physicsProperties.numberOfTotalJumpsAvaliable = physicsProperties.numberOfTotalJumpsMax;
                }

                //cancel jumping if no jumps left
                if (physicsProperties.numberOfTotalJumpsAvaliable <= 0)
                {
                    physicsProperties.canJump = false;
                }

                //Grounded Jump
                if (scheme.IsHeld(KeyActions.Jump))
                {
                    if (!physicsProperties.preparingSkywardAscent && !physicsProperties.isNimbusInUse && !physicsProperties.isSkywardAscentCharged && !physicsProperties.leapOffGrindPath && !physicsProperties.applyGrindBackwardsLeapForce && !physicsProperties.applyGrindLeapForwardForce && physicsProperties.numberOfTotalJumpsAvaliable == physicsProperties.numberOfTotalJumpsMax && activeDeadZoneTimer == physicsProperties.jumpDeadZoneTime && physicsProperties.PhysicallyGrounded && !physicsProperties.dashing && !Stats.IsStatusActive(StatusEffectType.Stun))
                    {
                        PlayerEvents.OnJumpTrigger();

                        //cancel ground detection
                        physicsProperties.groundDetectionToggle = false;
                        //cancel grapple leap force
                        physicsProperties.applyGrappleJumpForce = false;
                        //start dead zone timer so that you cannot input another jump during this duration
                        activeDeadZoneTimer = physicsProperties.jumpDeadZoneTime;
                        //grant jump height force
                        physicsProperties.jumpHeight += ((10 * physicsProperties.movementDuration / physicsProperties.topSpeed) + CalculatePlayerJump(physicsProperties.jumpforce) + 1f);
                        //subtract the jump
                        physicsProperties.numberOfTotalJumpsAvaliable -= 1;
                        disableWallKick = true;
                        print("Grounded Jump");
                    }
                }

                //if either kind of grounded is false then fall
                if (!physicsProperties.PhysicallyGrounded || !physicsProperties.OverrideMovement && !physicsProperties.Grounded)
                {
                    physicsProperties.jumpHeight -= (physicsProperties.jumpHeight * Time.deltaTime) - physicsProperties.incrementJumpFallSpeed * Time.deltaTime;
                }

                //Note: PhysicallyGrounded Fixed Frames Causes Irregularities In Jump Triggering At Times
                if (!physicsProperties.leapOffGrindPath && !physicsProperties.applyGrindBackwardsLeapForce && !physicsProperties.applyGrindLeapForwardForce && !physicsProperties.PhysicallyGrounded &&  !physicsProperties.pullMode && !physicsProperties.swingMode)
                {
                    //Mid air Double Jump
                    if (scheme.IsHeld(KeyActions.Jump))
                    {
                        if (physicsProperties.numberOfTotalJumpsAvaliable > 0 && !physicsProperties.isNimbusInUse && !physicsProperties.isTempestKicking && ProgressionInv.Instance.hasCloudBurstCowl && !physicsProperties.Grounded && activeDeadZoneTimer == physicsProperties.jumpDeadZoneTime && !physicsProperties.applyGrappleJumpForce && !physicsProperties.swingMode && !physicsProperties.pullingToSwingPoint && physicsProperties.canJump && !Stats.IsStatusActive(StatusEffectType.Stun))
                        {
                            PlayerEvents.OnDoubleJumpTrigger();

                            activeDeadZoneTimer = physicsProperties.jumpDeadZoneTime;
                            physicsProperties.groundDetectionToggle = false;

                            physicsProperties.applyGrappleJumpForce = false;
                            disableWallKick = true;

                            physicsProperties.gravityMultiplier = 0;
                            print("Double Jump");

                            physicsProperties.groundDetectionToggle = false;


                            physicsProperties.rb.linearVelocity = new Vector3(physicsProperties.rb.linearVelocity.x, 0, physicsProperties.rb.linearVelocity.z);

                            float jumpForce = (10 * physicsProperties.movementDuration / physicsProperties.topSpeed) + CalculatePlayerJump(physicsProperties.jumpdoubleforce); 
                            physicsProperties.rb.linearVelocity = new Vector3(physicsProperties.rb.linearVelocity.x,0,  physicsProperties.rb.linearVelocity.z);
                            physicsProperties.velocity.y += jumpForce;
                            physicsProperties.jumpHeight += jumpForce;

                            physicsProperties.DoubleJump = true;

                            physicsProperties.numberOfTotalJumpsAvaliable -= 1;
                            print(physicsProperties.numberOfTotalJumpsAvaliable);
                        }
                    }

                    //cancel wall kicking
                    if (scheme.IsHeld(KeyActions.Jump))
                    {
                        if (physicsProperties.isWallKicking && ProgressionInv.Instance.hasBeastClaws && !physicsProperties.readyToWallKick && !physicsProperties.swingMode && !physicsProperties.pullingToSwingPoint && activeDeadZoneTimer == physicsProperties.jumpDeadZoneTime || Stats.IsStatusActive(StatusEffectType.Stun))
                        {
                            PlayerEvents.OnDoubleJumpTrigger();

                            disableWallKick = true;
                            physicsProperties.applyGrappleJumpForce = false;

                            physicsProperties.gravityMultiplier = 0;

                            physicsProperties.numberOfTotalJumpsAvaliable -= 1;
                            print("Reduced Jump Double");

                            physicsProperties.groundDetectionToggle = false;
                            physicsProperties.rb.linearVelocity = Vector3.zero;
                            physicsProperties.jumpHeight += physicsProperties.jumpdoubleforce;
                            physicsProperties.vel.y = physicsProperties.jumpdoubleforce;
                            physicsProperties.velocity.y = physicsProperties.jumpdoubleforce;

                            physicsProperties.isWallKicking = false;
                            physicsProperties.DoubleJump = true;
                        }
                    }
                }

                //Jump For Grinding
                if (physicsProperties.isGrinding)
                {
                    //Grind Jump Exit
                    if (physicsProperties.leapOffGrindPath)
                    {
                        PlayerEvents.OnJumpTrigger();

                        physicsProperties.applyGrappleJumpForce = false;

                        physicsProperties.gravityMultiplier = 0;

                        print("Grind Jump");

                        physicsProperties.groundDetectionToggle = false;

                        physicsProperties.rb.linearVelocity = new Vector3(physicsProperties.rb.linearVelocity.x, 0, physicsProperties.rb.linearVelocity.z);
                        physicsProperties.jumpHeight += CalculatePlayerJump(physicsProperties.jumpforce) + 1f;
                        physicsProperties.vel.z = CalculatePlayerJump(physicsProperties.jumpforce) + 1f;
                        physicsProperties.velocity.z = CalculatePlayerJump(physicsProperties.jumpforce) + 1f;

                        ExitGrind();
                    }
                }

                //Reset Ground Detection
                if (!physicsProperties.groundDetectionToggle)
                {
                    activeDeadZoneTimer -= Time.deltaTime;

                    if (activeDeadZoneTimer <= 0)
                    {
                        physicsProperties.groundDetectionToggle = true;
                        activeDeadZoneTimer = physicsProperties.jumpDeadZoneTime;
                        disableWallKick = false;

                        if (physicsProperties.DoubleJump)
                        {
                            physicsProperties.DoubleJump = false;
                        }
                    }
                }

                physicsProperties.velocity.y += CalculatePlayerJump(physicsProperties.jumpHeight);
            }
        }
    }
    /// <summary>
    /// Resets the active jump dead zone timer to its default value, 
    /// allowing the player to jump again after a delay.
    /// </summary>
    public void ResetJumpOverride()
    {
        activeDeadZoneTimer = physicsProperties.jumpDeadZoneTime;
    }

    private bool zipJustFinished = false;
    float keyanglebarrel = 0;
    /// <summary>
    /// Manages Nimbus flight ability activation, movement, rotation, and vertical lerping.
    /// Handles both controller and keyboard/mouse inputs for free flight and target-lock movement.
    /// Applies gravity suspension and detects collisions to cancel Nimbus mode if needed.
    /// </summary>
    void Nimbus()
    {
        //Get Crouch Key Input Manager First Null Reference Check
        if (InputManager.Instance != null)
        {
            float angleBlendY = InputManager.Instance.PlayerInputScheme.centralinputs.RightStickNintendoSwitch.y;
            float angleBlendX = InputManager.Instance.PlayerInputScheme.centralinputs.RightStickNintendoSwitch.x;
            float anglebarrelroll = InputManager.Instance.PlayerInputScheme.centralinputs.NimbusBarrelNintendoSwitch.x;

            var scheme = InputManager.Instance.PlayerInputScheme;

            //Enable Nimbus Cloud
            if (scheme.WasPressedThisFrame(KeyActions.Nimbus) && 
                CooldownManager.Instance.CooldownState("Nimbus") && 
                !physicsProperties.swingMode && !physicsProperties.isPulling && 
                !physicsProperties.isWallHanging && !physicsProperties.pullMode)
            {
                physicsProperties.isWallKicking = false;
                PlayerEvents.OnNimbusFlightTrigger();

                // Toggle Nimbus usage
                physicsProperties.isNimbusInUse = !physicsProperties.isNimbusInUse;
                physicsProperties.isFreefalling = false;

                if (!physicsProperties.isNimbusInUse) // Turning OFF Nimbus
                {
                    PlayerCamera.Instance.GameCamera.transform.eulerAngles = PlayerCamera.Instance.Rotater.eulerAngles;

                    physicsProperties.currNimbusSpeed = 0;

                    physicsProperties.ApplyGravity = true;
                    physicsProperties.rb.useGravity = true;
                    physicsProperties.gravityMultiplier = 0;
                    physicsProperties.rb.linearVelocity = Vector3.zero;

                    // Reset vertical lerp state on Nimbus OFF
                    physicsProperties.nimbusLockY = float.NaN;
                    physicsProperties.isVerticalLerpActive = false;
                    physicsProperties.verticalLerpTimer = 0f;
                }
                else // Turning ON Nimbus
                {
                    if (angleBlendY != 0 || angleBlendX != 0)
                    {
                        physicsProperties.currNimbusSpeed = physicsProperties.maximumNimbusFlightSpeed / 2f;
                    }

                    // Reset vertical lerp state on Nimbus ON so lerp runs fresh
                    physicsProperties.nimbusLockY = float.NaN;
                    physicsProperties.isVerticalLerpActive = false;
                    physicsProperties.verticalLerpTimer = 0f;
                }

                physicsProperties.currNimbusSpeed = 0;
                 CooldownManager.Instance.IntiateCooldown("Nimbus");
            }

            //Nimbus Movement
            if (physicsProperties.isNimbusInUse && !physicsProperties.MoveType.Equals(MovementType.Strafe))
            {
                physicsProperties.ApplyGravity = false;
                physicsProperties.rb.useGravity = false;
                physicsProperties.gravityMultiplier = 0;

                physicsProperties.currentMovementSpeed = CalculatePlayerMovement(physicsProperties.nimbuspeed);

                float verticalInput = InputManager.Instance.PlayerInputScheme.MovementVector.y; // Space = +1, Ctrl = -1

                Vector3 verticalMove = Vector3.zero;

                if (Mathf.Abs(verticalInput) > 0.1f)
                {
                    verticalMove = Vector3.up * verticalInput * physicsProperties.currNimbusSpeed;
                }

                if (InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.enabled)
                {
                    Vector3 inputDirectionNorm = new Vector3(angleBlendX, 0, angleBlendY).normalized;

                    if (inputDirectionNorm.magnitude >= 0.1f)
                    {
                        Quaternion targetRotation = Quaternion.Euler(Camera.main.transform.eulerAngles) * Quaternion.LookRotation(inputDirectionNorm);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, physicsProperties.directionLookSpeed / 2 * Time.deltaTime);

                        transform.Rotate(Vector3.up, InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.RightStick.ReadValue<Vector2>().x * PlayerSettings.Instance.gameplaySettings.controllerSensitivity * Time.deltaTime);
                    }

                    if (anglebarrelroll != 0)
                    {
                        transform.Rotate(Vector3.forward, anglebarrelroll * PlayerSettings.Instance.gameplaySettings.controllerSensitivity * Time.deltaTime);
                    }

                    if (!scheme.IsHeld(KeyActions.Dodge))
                    {
                        physicsProperties.currNimbusSpeed = Mathf.Clamp(physicsProperties.currNimbusSpeed, -physicsProperties.maximumNimbusFlightSpeed, physicsProperties.maximumNimbusFlightSpeed);
                    }
                    else
                    {
                        physicsProperties.currNimbusSpeed = Mathf.Clamp(physicsProperties.currNimbusSpeed, -physicsProperties.maximumNimbusFlightSpeed * 1.5f, physicsProperties.maximumNimbusFlightSpeed * 1.5f);
                    }

                    if (inputDirectionNorm.magnitude <= 0.1f)
                    {
                        physicsProperties.currNimbusSpeed = Mathf.Lerp(physicsProperties.currNimbusSpeed, 0, Time.deltaTime * 2f);

                        if (physicsProperties.currNimbusSpeed <= 0)
                        {
                            physicsProperties.currNimbusSpeed = 0;
                        }

                        if (InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.RightStick.ReadValue<Vector2>().x != 0)
                        {
                            physicsProperties.currNimbusSpeed -= physicsProperties.nimbuspeed * (3 * Time.deltaTime);
                        }
                    }
                    else
                    {
                        if (InputManager.Instance.PlayerInputScheme.centralinputs.RunOrDodgeNintendoSwitch.IsPressed())
                        {
                            physicsProperties.currNimbusSpeed += physicsProperties.nimbuspeed * (2 * Time.deltaTime);
                        }
                        else
                        {
                            physicsProperties.currNimbusSpeed += physicsProperties.nimbuspeed * (10 * Time.deltaTime);
                        }
                    }

                    // Apply vertical movement
                    transform.position += verticalMove * Time.deltaTime;
                }
                else // Keyboard/Mouse
                {
                    Vector3 inputDirectionNorm = InputManager.Instance.PlayerInputScheme.MovementVector;
                    inputDirectionNorm.y = 0;
                    inputDirectionNorm = inputDirectionNorm.normalized;

                    if (inputDirectionNorm.magnitude >= 0.1f)
                    {
                        Quaternion targetRotation = Quaternion.Euler(Camera.main.transform.eulerAngles) * Quaternion.LookRotation(inputDirectionNorm);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, physicsProperties.directionLookSpeed / 2 * Time.deltaTime);

                        transform.Rotate(Vector3.up, keyanglebarrel * PlayerSettings.Instance.gameplaySettings.sensitivity * Time.deltaTime);
                    }

                    keyanglebarrel = Mathf.Clamp(keyanglebarrel, -1.5f, 1.5f);

                    if (scheme.IsHeld(KeyActions.WalkRight))
                        keyanglebarrel += 2;

                    if (scheme.IsHeld(KeyActions.WalkLeft))
                        keyanglebarrel -= 2;

                    if (!scheme.IsHeld(KeyActions.WalkLeft) && !scheme.IsHeld(KeyActions.WalkRight))
                        keyanglebarrel = 0;

                    transform.Rotate(Vector3.forward, keyanglebarrel * PlayerSettings.Instance.gameplaySettings.sensitivity * Time.deltaTime);

                    if (!scheme.IsHeld(KeyActions.Dodge))
                    {
                        physicsProperties.currNimbusSpeed = Mathf.Clamp(physicsProperties.currNimbusSpeed, -physicsProperties.maximumNimbusFlightSpeed, physicsProperties.maximumNimbusFlightSpeed);
                    }
                    else
                    {
                        physicsProperties.currNimbusSpeed = Mathf.Clamp(physicsProperties.currNimbusSpeed, -physicsProperties.maximumNimbusFlightSpeed * 1.5f, physicsProperties.maximumNimbusFlightSpeed * 1.5f);
                    }

                    if (inputDirectionNorm.magnitude <= 0.1f)
                    {
                        physicsProperties.currNimbusSpeed = Mathf.Lerp(physicsProperties.currNimbusSpeed, 0, Time.deltaTime * 2f);

                        if (physicsProperties.currNimbusSpeed <= 0)
                        {
                            physicsProperties.currNimbusSpeed = 0;
                        }

                        if (keyanglebarrel != 0)
                        {
                            physicsProperties.currNimbusSpeed -= physicsProperties.nimbuspeed * (3 * Time.deltaTime);
                        }
                    }
                    else
                    {
                        if (InputManager.Instance.PlayerInputScheme.centralinputs.RunOrDodgeNintendoSwitch.IsPressed())
                        {
                            physicsProperties.currNimbusSpeed += physicsProperties.nimbuspeed * (2 * Time.deltaTime);
                        }
                        else
                        {
                            physicsProperties.currNimbusSpeed += physicsProperties.nimbuspeed * (10 * Time.deltaTime);
                        }
                    }

                    // Apply vertical movement
                    transform.position += verticalMove * Time.deltaTime;
                }

                // Reset velocity
                physicsProperties.velocity = Vector3.zero;
                physicsProperties.vel = Vector3.zero;

                // Obstacle cancel
                RaycastHit hit;
                if (Physics.SphereCast(transform.position, 2f, Vector3.down, out hit, 3f, physicsProperties.NimbusCollisionCancelMask)
                 || Physics.Raycast(transform.position, transform.forward, 3f, physicsProperties.excludePlayer)
                 || Physics.Raycast(transform.position, transform.right, 3f, physicsProperties.excludePlayer)
                 || Physics.Raycast(transform.position, -transform.right, 3f, physicsProperties.excludePlayer))
                {
                    PlayerCamera.Instance.GameCamera.transform.eulerAngles = PlayerCamera.Instance.Rotater.eulerAngles;

                    physicsProperties.currNimbusSpeed = 0;
                    physicsProperties.ApplyGravity = true;
                    physicsProperties.rb.useGravity = true;
                    physicsProperties.gravityMultiplier = 0;
                    physicsProperties.isNimbusInUse = false;
                }
            }

            // Nimbus Target Movement
            if (physicsProperties.isNimbusInUse && physicsProperties.MoveType.Equals(MovementType.Strafe) && MyCamera.LockOnTarget != null)
            {
                physicsProperties.currNimbusSpeed = 0;
                physicsProperties.rb.linearVelocity = Vector3.zero;

                physicsProperties.ApplyGravity = false;
                physicsProperties.rb.useGravity = false;
                physicsProperties.gravityMultiplier = 0;

                Transform target = MyCamera.LockOnTarget.transform;

                // --- START vertical lerp on Nimbus start ---
                if (!physicsProperties.isVerticalLerpActive && float.IsNaN(physicsProperties.nimbusLockY))
                {
                    physicsProperties.isVerticalLerpActive = true;
                    physicsProperties.verticalLerpTimer = 0f;
                    physicsProperties.verticalLerpStartY = transform.position.y;
                    physicsProperties.verticalLerpEndY = target.position.y + physicsProperties.desiredYOffset;
                }

                if (physicsProperties.isVerticalLerpActive)
                {
                    physicsProperties.verticalLerpTimer += Time.deltaTime;
                    float t = Mathf.Clamp01(physicsProperties.verticalLerpTimer / physicsProperties.verticalLerpDuration);

                    float lerpedY = Mathf.Lerp(physicsProperties.verticalLerpStartY, physicsProperties.verticalLerpEndY, t);
                    Vector3 pos = transform.position;
                    pos.y = lerpedY;
                    transform.position = pos;

                    if (t >= 1f)
                    {
                        physicsProperties.isVerticalLerpActive = false;
                        physicsProperties.nimbusLockY = lerpedY;
                    }

                    return;
                }
                // --- END vertical lerp ---

                if (float.IsNaN(physicsProperties.nimbusLockY))
                {
                    physicsProperties.nimbusLockY = target.position.y;
                }

                Vector3 toTarget = target.position - transform.position;
                toTarget.y = 0;

                if (toTarget.sqrMagnitude < 0.01f)
                {
                    Debug.LogWarning("Zip aborted — toTarget vector too small.");
                    return;
                }

                toTarget.Normalize();

                Vector3 rightOfTarget = Vector3.Cross(Vector3.up, toTarget);
                Vector3 desiredMove = Vector3.zero;

                Vector3 desiredFacing = target.position - transform.position;
                desiredFacing.y = 0;
                desiredFacing.Normalize();

                float moveSpeed = physicsProperties.nimbusspeedTargeting;

                //Boosting
                if (scheme.IsHeld(KeyActions.Dodge)) moveSpeed *= 2f;

                Vector3 moveInput = scheme.MovementVector;
                float inputThreshold = 0.05f;
                float forwardInput = Mathf.Abs(moveInput.z) > inputThreshold ? moveInput.z : 0f;
                float strafeInput = Mathf.Abs(moveInput.x) > inputThreshold ? moveInput.x : 0f;
                float verticalInput = InputManager.Instance.PlayerInputScheme.MovementVector.y; // Space = +1, Ctrl = -1

                desiredMove += toTarget * forwardInput * moveSpeed;
                desiredMove += rightOfTarget * strafeInput * moveSpeed;
                desiredMove += Vector3.up * verticalInput * moveSpeed / 3f;

                // Trigger Zip
                if (scheme.WasDoubleTapped(KeyActions.Dodge))
                {
                    Vector3 zipOffset = -toTarget * 3.5f + Vector3.up * 1.5f;
                    Vector3 zipTarget = target.position + zipOffset;

                    physicsProperties.zipStart = transform.position;
                    physicsProperties.zipEnd = zipTarget;
                    physicsProperties.zipTime = 0f;
                    physicsProperties.isZippingToTarget = true;
                    zipJustFinished = false;
                }

                // Handle zipping
                if (physicsProperties.isZippingToTarget)
                {
                    physicsProperties.zipTime += Time.deltaTime * 2f;
                    transform.position = Vector3.Lerp(physicsProperties.zipStart, physicsProperties.zipEnd, physicsProperties.zipTime);

                    if (physicsProperties.zipTime >= 1f || Vector3.Distance(transform.position, physicsProperties.zipEnd) < 0.5f)
                    {
                        physicsProperties.isZippingToTarget = false;
                        zipJustFinished = true;
                        physicsProperties.nimbusLockY = transform.position.y;
                    }

                    desiredFacing = target.position - transform.position;
                    desiredFacing.y = 0;
                    desiredFacing.Normalize();
                }
                else
                {
                    if (zipJustFinished)
                    {
                        zipJustFinished = false;
                        verticalInput = 0;
                    }

                    Vector3 floatTarget = transform.position + desiredMove * Time.deltaTime;

                    if (!zipJustFinished)
                        physicsProperties.nimbusLockY += verticalInput * moveSpeed * Time.deltaTime;

                    floatTarget.y = physicsProperties.nimbusLockY;

                    transform.position = floatTarget;
                }

                // Face the target
                if (desiredFacing != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(desiredFacing, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, physicsProperties.directionLookSpeed * Time.deltaTime);
                }

                // Collision cancel
                RaycastHit hit;
                bool hitDetected = Physics.SphereCast(transform.position, 2f, Vector3.down, out hit, 3f, physicsProperties.NimbusCollisionCancelMask)
                    || Physics.Raycast(transform.position, transform.forward, 3f, physicsProperties.NimbusCollisionCancelMask)
                    || Physics.Raycast(transform.position, transform.right, 3f, physicsProperties.NimbusCollisionCancelMask)
                    || Physics.Raycast(transform.position, -transform.right, 3f, physicsProperties.NimbusCollisionCancelMask);

                if (hitDetected)
                {
                    PlayerCamera.Instance.GameCamera.transform.eulerAngles = PlayerCamera.Instance.Rotater.eulerAngles;

                    physicsProperties.currNimbusSpeed = 0;
                    physicsProperties.ApplyGravity = true;
                    physicsProperties.rb.useGravity = true;
                    physicsProperties.gravityMultiplier = 0;
                    physicsProperties.isNimbusInUse = false;

                    if (physicsProperties.isZippingToTarget)
                    {
                        physicsProperties.zipEnd = transform.position;
                        physicsProperties.zipTime = 1f;
                        physicsProperties.isZippingToTarget = false;
                        zipJustFinished = true;
                        physicsProperties.nimbusLockY = transform.position.y;
                    }

                    physicsProperties.nimbusLockY = float.NaN;
                }
            }
        }
    }
    /// <summary>
    /// Toggles and controls the player's freefall state.
    /// Rotates the character downward, simulates a controlled descent with directional input,
    /// applies downward force, and disables freefall upon grounding or collision.
    /// </summary>
    void Freefall()
    {
        if (InputManager.Instance != null)
        {
            float angleBlendY = InputManager.Instance.PlayerInputScheme.centralinputs.RightStickNintendoSwitch.y;
            float angleBlendX = InputManager.Instance.PlayerInputScheme.centralinputs.RightStickNintendoSwitch.x;
            float anglebarrelroll = InputManager.Instance.PlayerInputScheme.centralinputs.NimbusBarrelNintendoSwitch.x;

            var scheme = InputManager.Instance.PlayerInputScheme;

            //Enable Freefall
            if (scheme.WasPressedThisFrame(KeyActions.Crouch) && CooldownManager.Instance.CooldownState("Freefall") && !physicsProperties.isNimbusInUse && !physicsProperties.readyToWallKick && !physicsProperties.swingMode && !physicsProperties.pullingToGrapplePoint)
            {
                PlayerEvents.OnFreefallTrigger();
                
                //Ground cannot be within the range of 25 to start the free fall
                if (!Physics.Raycast(transform.position, Vector3.down, 15f))
                {
                    physicsProperties.isFreefalling = !physicsProperties.isFreefalling;
                    CooldownManager.Instance.IntiateCooldown("Freefall");
                }
            }

            if (physicsProperties.isFreefalling)
            {
                physicsProperties.currentMovementSpeed = CalculatePlayerMovement(physicsProperties.freefallspeed);

                // Get camera's forward vector (in world space)
                Vector3 camForward = Camera.main.transform.forward;

                // Create a rotation that would align the character's forward with camera's forward
                Quaternion camRotation = Quaternion.LookRotation(camForward);

                // Extract just the X and Y tilt components (remove yaw)
                Vector3 tiltEuler = camRotation.eulerAngles;
                Quaternion tiltOnly = Quaternion.Euler(physicsProperties.freefallangleAdjust, tiltEuler.y, tiltEuler.z);

                // Create downward-facing rotation
                Quaternion lookDown = Quaternion.LookRotation(Vector3.down);

                // Combine them - tilt first, then force downward orientation
                transform.rotation = tiltOnly * lookDown;

                physicsProperties.move = Vector3.ClampMagnitude(InputManager.Instance.PlayerInputScheme.MovementVector, 1f);
                physicsProperties.velocity += physicsProperties.move;

                if (scheme.IsHeld(KeyActions.Dodge))
                {
                    physicsProperties.vel = new Vector3(physicsProperties.velocity.x * physicsProperties.currentMovementSpeed, physicsProperties.velocity.z * physicsProperties.currentMovementSpeed, physicsProperties.freefallgravityfallforce);

                    physicsProperties.rb.AddRelativeForce(physicsProperties.move * physicsProperties.currentMovementSpeed, ForceMode.Acceleration);

                    physicsProperties.rb.AddForce(Vector3.down * physicsProperties.freefallgravityfallforce);
                }
                if (!scheme.IsHeld(KeyActions.Dodge))
                {
                    physicsProperties.vel = new Vector3(physicsProperties.velocity.x * physicsProperties.currentMovementSpeed, physicsProperties.velocity.z * physicsProperties.currentMovementSpeed, 0);

                    physicsProperties.rb.AddRelativeForce(physicsProperties.move * physicsProperties.currentMovementSpeed, ForceMode.Acceleration);
                }

                physicsProperties.vel = transform.TransformDirection(physicsProperties.vel);
                transform.TransformDirection(transform.forward);

                //Actively Moving
                transform.position += physicsProperties.vel * Time.deltaTime;
                physicsProperties.velocity = Vector3.zero;

                if (physicsProperties.Grounded)
                {
                    Quaternion targetRotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
                    transform.rotation = targetRotation;

                    physicsProperties.ApplyGravity = true;
                    physicsProperties.rb.useGravity = true;
                    physicsProperties.gravityMultiplier = 0;

                    physicsProperties.isFreefalling = false;
                }

                // Out hit point from our cast(s)
                RaycastHit hit;

                physicsProperties.velocity = Vector3.zero;
                physicsProperties.vel = Vector3.zero;

                // SPHERECAST
                // "Casts a sphere along a ray and returns detailed information on what was hit."
                if (Physics.SphereCast(transform.position, 2f, Vector3.down, out hit, 3f) || Physics.Raycast(transform.position, transform.forward, 3f, physicsProperties.excludePlayer) || Physics.Raycast(transform.position, transform.right, 3f, physicsProperties.excludePlayer) || Physics.Raycast(transform.position, -transform.right, 3f, physicsProperties.excludePlayer))
                {
                    Quaternion targetRotation = Quaternion.Euler(0, Camera.main.transform.eulerAngles.y, 0);
                    transform.rotation = targetRotation;

                    physicsProperties.ApplyGravity = true;
                    physicsProperties.rb.useGravity = true;
                    physicsProperties.gravityMultiplier = 0;

                    physicsProperties.isFreefalling = false;
                }
            }
        }
    }

    //Will Add Enemy Detection Reduction Soon
    //Wall Jump
    RaycastHit kickRay;
    RaycastHit tempestRay;
    public Vector3 tempestdir;
    private Vector3 pos;
    public bool enableWallSlide;
    private bool disableWallKick = false;
    /// <summary>
    /// Handles wall interaction mechanics including wall sliding, wall hanging, and wall kicks.
    /// Adjusts gravity and movement based on player proximity and input,
    /// and supports transitioning into Tempest Kick if conditions are met.
    /// </summary>
    void WallKick()
    {
        //Actively Hang 
        if (ProgressionInv.Instance.hasBeastClaws)
        {
            var scheme = InputManager.Instance.PlayerInputScheme;

            float angleBlendY = InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.LeftStick.ReadValue<Vector2>().y;
            float angleBlendX = InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.LeftStick.ReadValue<Vector2>().x;

            Vector3 inputDirection = new Vector3(angleBlendX, 0, angleBlendY).normalized;

            if (Physics.Raycast(transform.position, transform.forward, out kickRay, 1f, physicsProperties.wallKickLayer))
            {
                tempestdir = kickRay.normal;
            }

            //enable wall sliding when on the wall
            if (physicsProperties.isTempestKicking || !physicsProperties.Grounded && Physics.Raycast(transform.position, transform.forward, out kickRay, 3f, physicsProperties.wallKickLayer) || scheme.IsHoldingDirection() && !physicsProperties.Grounded && Physics.Raycast(transform.position, transform.forward, out kickRay, 1f, physicsProperties.wallKickLayer))
            {
                enableWallSlide = true;
            }

            if (physicsProperties.isNimbusInUse || physicsProperties.Grounded || physicsProperties.Grounded && Physics.Raycast(transform.position, transform.forward, out kickRay, 1f, physicsProperties.wallKickLayer))
            {
                enableWallSlide = false;

                if (physicsProperties.readyToWallKick)
                {
                    physicsProperties.rb.useGravity = true;
                    physicsProperties.ApplyGravity = true;
                    physicsProperties.isWallKicking = false;
                    physicsProperties.isWallHanging = false;
                }
            }

            //Check For Wall And Attach
            if (!physicsProperties.isTempestKicking && !Stats.IsStatusActive(StatusEffectType.Stun) && enableWallSlide && Vector3.Angle(kickRay.normal, transform.forward) >= 150 &&  !physicsProperties.Grounded && Physics.Raycast(transform.position, transform.forward, out kickRay, 1f, physicsProperties.wallKickLayer) && !physicsProperties.hovering && !physicsProperties.pullMode && !physicsProperties.swingMode && !physicsProperties.descending || Physics.Raycast(transform.position, transform.forward, out kickRay, 1f, physicsProperties.wallKickLayer) && physicsProperties.isTempestKicking)
            {
                if(PlayerSettings.Instance.gameplaySettings.Mode.Equals(CameraMode.FPSMode))
                {
                    PlayerCamera.Instance.CurrZoom = 0;
                    PlayerSettings.Instance.gameplaySettings.Mode = CameraMode.UnTargetedMode;
                }

                //Reset Jumps
                physicsProperties.numberOfTotalJumpsAvaliable = 1;

                //Face Wall
                transform.rotation = Quaternion.LookRotation(-kickRay.normal);

                //Set Wall Slide Boolean
                physicsProperties.readyToWallKick = true;

                //Remove Speed On Wall
                physicsProperties.rb.linearVelocity = Vector3.zero;
                physicsProperties.vel = new Vector3(0, physicsProperties.vel.y, 0);

                //Cancel Running
                physicsProperties.isRunning = false;

                //Turn Off All Gravity
                physicsProperties.vel.y = 0;
                physicsProperties.velocity.y = 0;
                physicsProperties.rb.useGravity = false;
                physicsProperties.gravityMultiplier = 0;
                physicsProperties.ApplyGravity = false;
                physicsProperties.dashing = false;
                physicsProperties.isTempestKicking = false;

                //Hasten Wall Slide
                if (!physicsProperties.Grounded && scheme.IsHeld(KeyActions.Dodge))
                {
                    physicsProperties.WallSlideSpeedAcceleration = Mathf.Clamp(physicsProperties.WallSlideSpeedAcceleration, 0, 50f);
                    physicsProperties.currentMovementSpeed = CalculatePlayerMovement(physicsProperties.WallSlideSpeedAcceleration += Time.deltaTime + (physicsProperties.WallSlideSpeed));
                }
                else
                {
                    physicsProperties.currentMovementSpeed = CalculatePlayerMovement(physicsProperties.WallSlideSpeed);
                    physicsProperties.WallSlideSpeedAcceleration = 0;
                }

                if (scheme.IsHeld(KeyActions.Dodge))
                {
                    if (physicsProperties.Grounded)
                    {
                        physicsProperties.currentMovementSpeed = CalculatePlayerMovement(physicsProperties.WallSlideSpeed);
                        physicsProperties.WallSlideSpeedAcceleration = 0;
                    }
                }
            }
            else
            {
                if ( physicsProperties.readyToWallKick && !scheme.IsHeld(KeyActions.Guard))
                {
                    physicsProperties.WallSlideSpeedAcceleration = physicsProperties.WallSlideSpeed;
                    physicsProperties.readyToWallKick = false;
                    physicsProperties.rb.useGravity = true;
                    physicsProperties.ApplyGravity = true;
                    physicsProperties.turnLock = false;
                }
            }

            //Wall Hanging
            if (physicsProperties.readyToWallKick && !physicsProperties.Grounded)
            {
                if (!scheme.IsHeld(KeyActions.Guard))
                {
                    physicsProperties.isWallHanging = false;
                    physicsProperties.turnLock = false;

                    //Wall Offset Slide
                    pos = new Vector3(transform.position.x, kickRay.point.y + (-physicsProperties.currentMovementSpeed * Time.deltaTime), transform.position.z);
                    transform.position = pos;

                    if (ProgressionInv.Instance.hasGrace_TempestKick)
                    {
                        physicsProperties.preparingTempestKick = false;
                    }
                }
                else
                {
                    physicsProperties.turnLock = true;

                    //Must set player to be parented to a uniform object that is (1,1,1) in order to prevent skew scaling
                    if (Physics.Raycast(transform.position, transform.forward, out kickRay, 1f, physicsProperties.wallKickLayer))
                    {
                        if (kickRay.transform.GetComponentInParent<MovingPlatform>() != null && kickRay.transform.GetComponentInParent<MovingPlatform>().Platform != null)
                        {
                            transform.SetParent(kickRay.transform.GetComponentInParent<MovingPlatform>().Platform);
                        }
                    }

                    physicsProperties.isWallHanging = true;


                    if (ProgressionInv.Instance.hasGrace_TempestKick && !physicsProperties.isTempestKicking)
                    {
                        physicsProperties.preparingTempestKick = true;
                    }
                    else
                    {
                        physicsProperties.preparingTempestKick = false;
                    }
                }
            }

            //Wait For Input For Wall Jump
            if (physicsProperties.readyToWallKick && scheme.IsHeld(KeyActions.Jump) && !scheme.IsHeld(KeyActions.Guard))
            {
                PlayerEvents.OnWallJumpTrigger();
                print("Wall Jumped");
                //Set Wall Kicking True
                physicsProperties.turnLock = false;
                physicsProperties.ApplyGravity = true;
                physicsProperties.isWallKicking = true;
                physicsProperties.rb.useGravity = true;
                enableWallSlide = false;

                //Face Opposite Of Wall
                transform.rotation = Quaternion.LookRotation(kickRay.normal);

                //Set Gravity To 0
                physicsProperties.gravityMultiplier = 0;
                physicsProperties.groundDetectionToggle = false;

                //Set Jump Height Off Of Wall
                physicsProperties.jumpHeight += physicsProperties.WallKickHeight;
                physicsProperties.vel.y = physicsProperties.WallKickHeight;
                physicsProperties.velocity.y = physicsProperties.WallKickHeight;
                physicsProperties.numberOfTotalJumpsAvaliable = 1;

                //This Requires Key Movement To Move Forward!
            }
        }
        else
        {
            physicsProperties.preparingTempestKick = false;
        }

        //Detects Ceiling
        if (Physics.Raycast(transform.position, transform.up, 1f, physicsProperties.crouchLayers) && physicsProperties.isWallKicking)
        {
            physicsProperties.isWallKicking = false;
            physicsProperties.isWallHanging = false;
        }
        //Cancel Method #1
        if (physicsProperties.readyToWallKick && physicsProperties.Grounded)
        {
            physicsProperties.readyToWallKick = false;
            physicsProperties.rb.useGravity = true;
            physicsProperties.ApplyGravity = true;
            physicsProperties.isWallKicking = false;
            physicsProperties.isWallHanging = false;
        }
        //Cancel Method #2
        if (physicsProperties.Grounded)
        {
            physicsProperties.isWallKicking = false;
            physicsProperties.isWallHanging = false;
        }
    }
    private Vector3 TempestKickDirection;
    /// <summary>
    /// Executes the Tempest Kick ability, a cinematic dash-kick off a wall.
    /// Prepares start and end positions, aligns direction, and moves the character in a controlled arc.
    /// Triggers effects like camera shake and ends with a timed transition.
    /// </summary>
    void TempestKick()
    {
        // Draw path line in Scene view
        if (physicsProperties.isTempestKicking || physicsProperties.preparingTempestKick)
        {
            Debug.DrawLine(physicsProperties.TempestKickStartPosition, physicsProperties.TempestKickEndPosition, Color.red, 1f);
        }

        // Preparation phase: Set start and calculate end position (with wall detection)
        if (physicsProperties.preparingTempestKick)
        {
            PlayerEvents.OnWallJumpTrigger();

            physicsProperties.TempestKickStartPosition = transform.position;
            Vector3 intendedEnd = transform.position + -transform.forward * physicsProperties.TempestDistance;

            Debug.Log($"Tempest Kick Start Position: {physicsProperties.TempestKickStartPosition}");
            Debug.Log($"Intended Tempest Kick End Position: {intendedEnd}");
            Debug.DrawLine(physicsProperties.TempestKickStartPosition, intendedEnd, Color.blue, 1f);

            if (Physics.Linecast(transform.position, intendedEnd, out RaycastHit hit, LayerMask.GetMask("Wall")))
            {
                physicsProperties.TempestKickEndPosition = hit.point;
                Debug.Log($"Collision Detected! Adjusted End Position: {hit.point}");
                Debug.DrawLine(physicsProperties.TempestKickStartPosition, hit.point, Color.green, 1f);
            }
            else
            {
                physicsProperties.TempestKickEndPosition = intendedEnd;
                Debug.Log("No collision detected, using intended end position.");
            }
        }

        // Kick Initiation
        if (!physicsProperties.isTempestKicking && physicsProperties.readyToWallKick && !physicsProperties.Grounded && ((Input.GetKey(InputManager.Instance.PlayerInputScheme.centralinputs.Guard) && Input.GetKey(InputManager.Instance.PlayerInputScheme.centralinputs.Jump)) || (InputManager.Instance.PlayerInputScheme.centralinputs.GuardNintendoSwitch.IsPressed() && InputManager.Instance.PlayerInputScheme.centralinputs.JumpNintendoSwitch.IsPressed())) && ProgressionInv.Instance.hasGrace_TempestKick)
        {
            //CameraShake.Instance.Shake(1f, 1f);
            Debug.Log("Tempest Kick START");

            Vector3 direction = physicsProperties.TempestKickEndPosition - physicsProperties.TempestKickStartPosition;
            TempestKickDirection = direction.normalized;
            transform.rotation = Quaternion.LookRotation(TempestKickDirection);

            physicsProperties.turnLock = false;
            physicsProperties.ApplyGravity = false;
            physicsProperties.rb.useGravity = false;
            enableWallSlide = false;
            physicsProperties.TempestKickProgress = 0f;
            physicsProperties.isTempestKicking = true;

            Debug.Log($"Kick Direction: {TempestKickDirection}");
        }

        // Kick Movement Logic
        if (physicsProperties.isTempestKicking)
        {
            Vector3 start = physicsProperties.TempestKickStartPosition;
            Vector3 end = physicsProperties.TempestKickEndPosition;
            float distance = Vector3.Distance(start, end);
            float speed = physicsProperties.TempestSpeed;

            // Progress based on speed and distance
            physicsProperties.TempestKickProgress += (speed / distance) * Time.deltaTime;
            float t = Mathf.Clamp01(physicsProperties.TempestKickProgress);
            transform.position = Vector3.Lerp(start, end, t);

            Debug.Log($"TempestKick Progress: {t * 100}%");
            Debug.Log($"Current Position: {transform.position}");
            Debug.DrawLine(start, transform.position, Color.yellow, 0.1f);

            if (t >= 1f)
            {
                StartCoroutine(EndTempestKickWithDelay());
            }
        }
    }
    /// <summary>
    /// Coroutine that finalizes the Tempest Kick sequence with a short delay,
    /// camera shake, FOV reset, gravity reactivation, and reorientation of the character.
    /// </summary>
    public IEnumerator EndTempestKickWithDelay()
    {
        //CameraShake.Instance.Shake(0.05f, 1f); // Strong shake at end
        yield return new WaitForSeconds(0.1f);

        Debug.Log("Tempest Kick END");
        Camera.main.fieldOfView = 60;
        physicsProperties.ApplyGravity = true;
        physicsProperties.rb.useGravity = true;
        physicsProperties.isTempestKicking = false;

        // Maintain rotation based on stored direction
        transform.rotation = Quaternion.LookRotation(TempestKickDirection);
    }
    /// <summary>
    /// Manages the Skyward Ascent ability, allowing the player to launch vertically into the air.
    /// Handles both input detection and interpolation of the ascent motion over time.
    /// Also sets up the cinematic preparation state.
    /// </summary>
    void SkywardAscent()
    {
        // Remove charging, it's now instant
        if (!InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player.enabled)
        {
            if (!physicsProperties.isNimbusInUse && !physicsProperties.isSkywardAscending && physicsProperties.Grounded && Input.GetKey(InputManager.Instance.PlayerInputScheme.centralinputs.Guard) && Input.GetKey(InputManager.Instance.PlayerInputScheme.centralinputs.Jump))
            {
                StartSkywardAscent();
                PlayerEvents.OnJumpTrigger();
                PlayerEvents.OnDoubleJumpTrigger();
            }

            if (!physicsProperties.isNimbusInUse && !physicsProperties.isSkywardAscending && Input.GetKey(InputManager.Instance.PlayerInputScheme.centralinputs.Guard))
            {
                physicsProperties.preparingSkywardAscent = true;

                physicsProperties.SkywardAscentStartPosition = transform.position;
                physicsProperties.SkywardAscentEndPosition = transform.position + Vector3.up * physicsProperties.SkywardAscentDistance;
            }
            else
            {
                physicsProperties.preparingSkywardAscent = false;
            }
        }
        else
        {
            if (!physicsProperties.isNimbusInUse && !physicsProperties.isSkywardAscending && physicsProperties.Grounded && InputManager.Instance.PlayerInputScheme.centralinputs.GuardNintendoSwitch.IsPressed() && InputManager.Instance.PlayerInputScheme.centralinputs.JumpNintendoSwitch.IsPressed())
            {
                StartSkywardAscent();
            }

            if (!physicsProperties.isNimbusInUse && !physicsProperties.isSkywardAscending && InputManager.Instance.PlayerInputScheme.centralinputs.GuardNintendoSwitch.IsPressed())
            {
                physicsProperties.preparingSkywardAscent = true;

                physicsProperties.SkywardAscentStartPosition = transform.position;
                physicsProperties.SkywardAscentEndPosition = transform.position + Vector3.up * physicsProperties.SkywardAscentDistance;
            }
            else
            {
                physicsProperties.preparingSkywardAscent = false;
            }
        }

        // Movement During Skyward Ascent
        if (physicsProperties.isSkywardAscending)
        {
            // Update timer
            physicsProperties.skywardAscentTimer += Time.deltaTime;

            float totalDuration = physicsProperties.SkywardAscentDistance / physicsProperties.SkywardAscentSpeed;
            float t = Mathf.Clamp01(physicsProperties.skywardAscentTimer / totalDuration);
            physicsProperties.SkywardAscentProgress = t;

            Vector3 start = physicsProperties.SkywardAscentStartPosition;
            Vector3 end = physicsProperties.SkywardAscentEndPosition;

            transform.position = Vector3.Lerp(start, end, t);

            Debug.Log($"SkywardAscent Progress: {t * 100}%");
            Debug.DrawLine(start, transform.position, Color.yellow, 0.1f);

            if (t >= 1f)
            {
                EndSkywardAscent();
            }
        }

    }
    /// <summary>
    /// Initializes the Skyward Ascent sequence by setting timing, disabling gravity,
    /// rotating the player to face upward, and establishing start/end positions.
    /// </summary>
    void StartSkywardAscent()
    {
        physicsProperties.skywardAscentTimer = 0;
        print("Skyward Ascent Start");

        // Set Flight
        physicsProperties.gravityMultiplier = 0;
        physicsProperties.SkywardAscentStartPosition = transform.position;
        physicsProperties.SkywardAscentEndPosition = transform.position + Vector3.up * physicsProperties.SkywardAscentDistance;
        physicsProperties.preparingSkywardAscent = false;
        enableWallSlide = false;

        physicsProperties.groundDetectionToggle = false;

        // Face the Sky
        transform.rotation = Quaternion.LookRotation(Vector3.up);

        physicsProperties.isSkywardAscending = true;
    }
    /// <summary>
    /// Finalizes the Skyward Ascent ability by resetting gravity and flags,
    /// allowing the player to resume normal grounded movement and detection.
    /// </summary>
    void EndSkywardAscent()
    {
        print("Skyward Ascent End");
        physicsProperties.gravityMultiplier = 0;
        physicsProperties.isSkywardAscending = false;
        physicsProperties.groundDetectionToggle = true;
    }
    #endregion

    #region Debugging

    void OnDrawGizmos()
    {
        // Gizmos to visualize the start and end positions of Tempest Kick
        if (physicsProperties.isTempestKicking || physicsProperties.preparingTempestKick)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(physicsProperties.TempestKickStartPosition, physicsProperties.TempestKickEndPosition);

            // If preparing, draw the intended end position
            if (physicsProperties.preparingTempestKick)
            {
                Gizmos.color = Color.blue;
                Vector3 intendedEnd = physicsProperties.TempestKickStartPosition + -transform.forward * physicsProperties.TempestDistance;
                Gizmos.DrawLine(physicsProperties.TempestKickStartPosition, intendedEnd);

                // If collision detected, draw the end position from the hit point
                if (Physics.Linecast(physicsProperties.TempestKickStartPosition, intendedEnd, out RaycastHit hit, LayerMask.GetMask("Wall")))
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(physicsProperties.TempestKickStartPosition, hit.point);
                }
            }
        }

        if (currentGrappleTarget != null)
        {
            Gizmos.color = (currentGrappleTarget == currentZTarget) ? Color.magenta : Color.cyan;
            Gizmos.DrawSphere(currentGrappleTarget.position, 0.3f);

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;

            float dist = Vector3.Distance(transform.position, currentGrappleTarget.position);

            if (dist >= 10)
            {
                UnityEditor.Handles.Label(currentGrappleTarget.position + Vector3.up * 1.0f,
                $"Grapple Target: [{currentGrappleTarget.name}] Dist: {dist}");
            }
            else
            {
                UnityEditor.Handles.Label(currentGrappleTarget.position + Vector3.up * 1.0f,
                $"Grapple Target: [{currentGrappleTarget.name}] Dist: {dist} You are TOO close to the target!");
            }

#endif
        }



    }

    #endregion

    /// <summary>
    /// Respawns the player at their last activated checkpoint.
    /// Resets all motion vectors and restores health, status, and usable items via the shrine effect.
    /// </summary>
    public void Respawn()
    {
        //Set Position
        transform.position = physicsProperties.lastCheckpoint.Checkpoint.position;

        //Reset Vectors
        Vector3 velocity = Vector3.zero;
        Vector3 move = Vector3.zero;
        Vector3 vel = Vector3.zero;
        Vector3 dir = Vector3.zero;

        //Heal And Cleanse And Restore Gourds
        physicsProperties.lastCheckpoint.ShrineEffect(Stats, GetComponent<Gourds>());
    }

    //will be changed in the future
    void InitializePlayer()
    {
        //Don't Destory These On Load Of Other Scenes
        //DontDestroyOnLoad(gameObject);
        //DontDestroyOnLoad(MyCamera.Rotater);
        //DontDestroyOnLoad(MyCamera.GeneralLook);
        //DontDestroyOnLoad(MyCamera.GameCamera);

        combatSystem = GetComponent<ComboManager>();

        //Grab Spawners
        foreach (PlayerSpawner spawner in FindObjectsOfType<PlayerSpawner>())
        {
            spawner.currentPlayer = gameObject;
        }
    }
}