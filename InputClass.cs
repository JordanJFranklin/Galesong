using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public enum KeyActions{WalkForward,WalkBackwards,WalkRight,WalkLeft,Dodge,Jump,SkillA,SkillB,SkillC,SkillD,Crouch,TargetEnemy,Grapple, LightAttack, HeavyAttack, Guard, Interact, EscapeMenu, ResetCamera, Nimbus, Gourd}

namespace InputKeys
{
    /// <summary>
    /// Represents a single key binding for an action (keyboard-specific).
    /// </summary>
    [System.Serializable]
    public class InputKey
    {
        [Tooltip("Name of the action (for editor display purposes only).")]
        public string ActionName;

        [Tooltip("The type of action this key is bound to.")]
        public KeyActions BoundAction;

        [Tooltip("The actual keyboard key assigned to this action.")]
        public KeyCode key;
    }

    /// <summary>
    /// Stores all input configuration and runtime input logic for both keyboard and controller.
    /// </summary>
    [System.Serializable]
    public class InputScheme
    {
        [Tooltip("Maps KeyActions to indices in the Inputs list for quick lookup.")]
        public Dictionary<KeyActions, int> InputKeyIndexLib = new();

        [Tooltip("Default key bindings at start or reset.")]
        public List<InputKey> DefaultInputs = new();

        [Tooltip("Current active key bindings.")]
        public List<InputKey> Inputs = new();

        [Tooltip("True if controller input is enabled.")]
        public bool enableController;

        [Tooltip("Invert Mouse Looking on the X Axis.")]
        public bool invertX;

        [Tooltip("Invert Mouse Looking on the Y Axis.")]
        public bool invertY;

        [Tooltip("Unity Input System's main input map asset.")]
        public PlayerInput controls;

        [Tooltip("Nintendo Switch Pro controller profile.")]
        public NintendoSwitchPro NintendoSwitchProController;

        [Tooltip("Combined movement vector calculated from input.")]
        public Vector3 MovementVector;

        [Tooltip("Vertical elevation component of movement (e.g. jump/crouch).")]
        public float Elevation;

        [Tooltip("Left/right movement component.")]
        public float Horizontal;

        [Tooltip("Forward/backward movement component.")]
        public float Vertical;

        [Tooltip("Rate at which movement accelerates.")]
        public float acceleration = 10;

        [Tooltip("Rate at which movement decelerates.")]
        public float deacceleration = 5;

        [Tooltip("If true, movement input is inverted.")]
        public bool reverseInput = false;

        [Tooltip("Centralized controller input actions.")]
        public CentralizedInputs centralinputs;

        // --- Double Tap Detection Settings ---
        private readonly Dictionary<KeyActions, float> lastTapTimes = new();
        private readonly Dictionary<KeyActions, bool> awaitingSecondTap = new();

        [Tooltip("Time window (seconds) allowed between two taps to register as a double tap.")]
        public float doubleTapThreshold = 0.35f;

        public InputScheme()
        {
            InitializeDoubleTapTracking();
        }

        /// <summary>
        /// Gets the KeyCode (keyboard) or controller InputAction for the given KeyAction.
        /// </summary>
        public (KeyCode? keyCode, InputAction inputAction) GetKeyFromAction(KeyActions action)
        {
            // Keyboard fallback
            KeyCode? keyCode = null;
            InputAction controllerAction = null;

            if (InputKeyIndexLib.TryGetValue(action, out int index) && index >= 0 && index < Inputs.Count)
            {
                keyCode = Inputs[index].key;
            }

            if (NintendoSwitchProController.Player.enabled)
            {
                controllerAction = GetControllerInputAction(action);
            }

            return (keyCode, controllerAction);
        }


        /// <summary>Initializes the double-tap tracking for all input actions.</summary>
        public void InitializeDoubleTapTracking()
        {
            foreach (KeyActions action in Enum.GetValues(typeof(KeyActions)))
            {
                lastTapTimes[action] = -999f;
                awaitingSecondTap[action] = false;
            }
        }

        /// <summary>
        /// Returns true if this action was double tapped within the threshold window (on keyboard or controller).
        /// </summary>
        public bool WasDoubleTapped(KeyActions action)
        {
            bool pressed = false;
            bool isSwitch = NintendoSwitchProController.Player.enabled;

            if (isSwitch)
            {
                var input = GetControllerInputAction(action);
                if (input != null)
                    pressed = input.WasPressedThisFrame();
            }
            else
            {
                var key = Inputs[InputKeyIndexLib[action]].key;
                pressed = Input.GetKeyDown(key);
            }

            float now = Time.time;

            if (pressed)
            {
                float sinceLast = now - lastTapTimes[action];

                if (awaitingSecondTap[action] && sinceLast <= doubleTapThreshold)
                {
                    awaitingSecondTap[action] = false;
                    Debug.Log($"Double Tap Occurred: {action}");
                    return true;
                }

                lastTapTimes[action] = now;
                awaitingSecondTap[action] = true;
            }

            // Expire double-tap window
            if (awaitingSecondTap[action] && now - lastTapTimes[action] > doubleTapThreshold)
            {
                awaitingSecondTap[action] = false;
            }

            return false;
        }

        /// <summary>Returns true if the key or controller input is being held down.</summary>
        public bool IsHeld(KeyActions action)
        {
            if (NintendoSwitchProController.Player.enabled)
            {
                var input = GetControllerInputAction(action);
                return input != null && input.IsPressed();
            }
            else
            {
                var key = Inputs[InputKeyIndexLib[action]].key;
                return Input.GetKey(key);
            }
        }

        /// <summary>Returns true if the key or controller input was pressed this frame.</summary>
        public bool WasPressedThisFrame(KeyActions action)
        {
            if (NintendoSwitchProController.Player.enabled)
            {
                var input = GetControllerInputAction(action);
                return input != null && input.WasPressedThisFrame();
            }
            else
            {
                var key = Inputs[InputKeyIndexLib[action]].key;
                return Input.GetKeyDown(key);
            }
        }

        /// <summary>Returns true if the key or controller input was released this frame.</summary>
        public bool WasReleasedThisFrame(KeyActions action)
        {
            if (NintendoSwitchProController.Player.enabled)
            {
                var input = GetControllerInputAction(action);
                return input != null && input.WasReleasedThisFrame();
            }
            else
            {
                var key = Inputs[InputKeyIndexLib[action]].key;
                return Input.GetKeyUp(key);
            }
        }

        /// <summary>
        /// Returns true if any movement direction is currently being held (WASD or analog stick).
        /// </summary>
        public bool IsHoldingDirection()
        {
            if (NintendoSwitchProController.Player.enabled)
            {
                Vector2 stick = NintendoSwitchProController.Player.LeftStick.ReadValue<Vector2>();
                return stick.magnitude > 0.1f;
            }

            return
                Input.GetKey(Inputs[InputKeyIndexLib[KeyActions.WalkForward]].key) ||
                Input.GetKey(Inputs[InputKeyIndexLib[KeyActions.WalkBackwards]].key) ||
                Input.GetKey(Inputs[InputKeyIndexLib[KeyActions.WalkLeft]].key) ||
                Input.GetKey(Inputs[InputKeyIndexLib[KeyActions.WalkRight]].key);
        }

        /// <summary>
        /// Returns the movement direction from input as a Vector3.
        /// - X: Horizontal (left/right)
        /// - Y: Elevation (up/down)
        /// - Z: Vertical (forward/backward)
        /// </summary>
        public Vector3 GetInputMovementDirection(bool reverseInput = false)
        {
            Vector3 moveDir = Vector3.zero;

            if (NintendoSwitchProController.Player.enabled)
            {
                Vector2 stick = NintendoSwitchProController.Player.LeftStick.ReadValue<Vector2>();
                moveDir = new Vector3(stick.x, Elevation, stick.y);
            }
            else
            {
                KeyCode keyForward = Inputs[InputKeyIndexLib[KeyActions.WalkForward]].key;
                KeyCode keyBackward = Inputs[InputKeyIndexLib[KeyActions.WalkBackwards]].key;
                KeyCode keyLeft = Inputs[InputKeyIndexLib[KeyActions.WalkLeft]].key;
                KeyCode keyRight = Inputs[InputKeyIndexLib[KeyActions.WalkRight]].key;
                KeyCode keyUp = Inputs[InputKeyIndexLib[KeyActions.Jump]].key;
                KeyCode keyDown = Inputs[InputKeyIndexLib[KeyActions.Crouch]].key;

                float horizontal = 0f;
                float vertical = 0f;
                float elevation = 0f;

                if (reverseInput)
                {
                    if (Input.GetKey(keyForward)) vertical -= 1f;
                    if (Input.GetKey(keyBackward)) vertical += 1f;
                    if (Input.GetKey(keyLeft)) horizontal += 1f;
                    if (Input.GetKey(keyRight)) horizontal -= 1f;
                    if (Input.GetKey(keyUp)) elevation -= 1f;
                    if (Input.GetKey(keyDown)) elevation += 1f;
                }
                else
                {
                    if (Input.GetKey(keyForward)) vertical += 1f;
                    if (Input.GetKey(keyBackward)) vertical -= 1f;
                    if (Input.GetKey(keyRight)) horizontal += 1f;
                    if (Input.GetKey(keyLeft)) horizontal -= 1f;
                    if (Input.GetKey(keyUp)) elevation += 1f;
                    if (Input.GetKey(keyDown)) elevation -= 1f;
                }

                moveDir = new Vector3(horizontal, elevation, vertical);
            }

            return Vector3.ClampMagnitude(moveDir, 1f);
        }

        /// <summary>
        /// Returns the InputAction object for the given KeyAction on controller.
        /// </summary>
        private InputAction GetControllerInputAction(KeyActions action)
        {
            var c = centralinputs;
            return action switch
            {
                KeyActions.WalkForward => c.WalkForwardNintendoSwitch,
                KeyActions.WalkBackwards => c.WalkBackwardNintendoSwitch,
                KeyActions.WalkRight => c.WalkRightNintendoSwitch,
                KeyActions.WalkLeft => c.WalkLeftNintendoSwitch,
                KeyActions.Jump => c.JumpNintendoSwitch,
                KeyActions.Dodge => c.RunOrDodgeNintendoSwitch,
                KeyActions.TargetEnemy => c.ZTargetNintendoSwitch,
                KeyActions.Grapple => c.GrappleNintendoSwitch,
                KeyActions.LightAttack => c.LightAttackNintendoSwitch,
                KeyActions.HeavyAttack => c.HeavyAttackNintendoSwitch,
                KeyActions.Guard => c.GuardNintendoSwitch,
                KeyActions.Interact => c.InteractNintendoSwitch,
                KeyActions.EscapeMenu => c.PauseNintendoSwitch,
                KeyActions.Nimbus => c.NimbusNintendoSwitch,
                KeyActions.SkillA => c.SkillANintendoSwitch,
                KeyActions.SkillB => c.SkillBNintendoSwitch,
                KeyActions.SkillC => c.SkillCNintendoSwitch,
                KeyActions.SkillD => c.SkillDNintendoSwitch,
                KeyActions.Gourd => c.GourdNintendoSwitch,
                _ => null
            };
        }
    }



    [System.Serializable]
    public class CentralizedInputs
    {
        [Header("Keyboard/Mouse")]
        public KeyCode WalkForward;
        public KeyCode WalkBackward;
        public KeyCode WalkRight;
        public KeyCode WalkLeft;
        public KeyCode Jump;
        public KeyCode RunOrDodge;
        public KeyCode ZTarget;
        public KeyCode Crouch;
        public KeyCode Grapple;
        public KeyCode LightAttack;
        public KeyCode HeavyAttack;
        public KeyCode Guard;
        public KeyCode Interact;
        public KeyCode Menu;
        public KeyCode Nimbus;
        public KeyCode SkillA;
        public KeyCode SkillB;
        public KeyCode SkillC;
        public KeyCode SkillD;
        public KeyCode Gourd;

        [Header("Nintendo Switch Pro Controller Inputs")]
        public InputAction WalkForwardNintendoSwitch;
        public InputAction WalkBackwardNintendoSwitch;
        public InputAction WalkRightNintendoSwitch;
        public InputAction WalkLeftNintendoSwitch;
        public InputAction JumpNintendoSwitch;
        public InputAction RunOrDodgeNintendoSwitch;
        public InputAction ZTargetNintendoSwitch;
        public InputAction FreefallNintendoSwitch;
        public InputAction GrappleNintendoSwitch;
        public InputAction LightAttackNintendoSwitch;
        public InputAction HeavyAttackNintendoSwitch;
        public InputAction GuardNintendoSwitch;
        public InputAction InteractNintendoSwitch;
        public InputAction PauseNintendoSwitch;
        public InputAction NimbusNintendoSwitch;
        public InputAction SkillANintendoSwitch;
        public InputAction SkillBNintendoSwitch;
        public InputAction SkillCNintendoSwitch;
        public InputAction SkillDNintendoSwitch;
        public InputAction FPSCameraNintendoSwitch;
        public InputAction GourdNintendoSwitch;

        public Vector2 NimbusBarrelNintendoSwitch;
        public Vector2 RightStickNintendoSwitch;
        public Vector2 LeftStickNintendoSwitch;

        // ----------------------------
        // Initialization Methods
        // ----------------------------

        public void InitializeKeyboardInputs()
        {
            var scheme = InputManager.Instance.PlayerInputScheme;

            WalkForward = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.WalkForward]].key;
            WalkBackward = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.WalkBackwards]].key;
            WalkRight = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.WalkRight]].key;
            WalkLeft = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.WalkLeft]].key;
            Jump = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.Jump]].key;
            RunOrDodge = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.Dodge]].key;
            ZTarget = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.TargetEnemy]].key;
            Crouch = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.Crouch]].key;
            Grapple = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.Grapple]].key;
            LightAttack = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.LightAttack]].key;
            HeavyAttack = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.HeavyAttack]].key;
            Guard = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.Guard]].key;
            Interact = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.Interact]].key;
            Menu = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.EscapeMenu]].key;
            Nimbus = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.Nimbus]].key;
            SkillA = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.SkillA]].key;
            SkillB = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.SkillB]].key;
            SkillC = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.SkillC]].key;
            SkillD = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.SkillD]].key;
            Gourd = scheme.Inputs[scheme.InputKeyIndexLib[KeyActions.Gourd]].key;
        }

        public void InitializeNintendoSwitchControllerInputs()
        {
            var controller = InputManager.Instance.PlayerInputScheme.NintendoSwitchProController.Player;

            JumpNintendoSwitch = controller.Jump;
            RunOrDodgeNintendoSwitch = controller.Dodge;
            ZTargetNintendoSwitch = controller.ZTarget;
            FreefallNintendoSwitch = controller.Freefall;
            GrappleNintendoSwitch = controller.GrappleHook;
            LightAttackNintendoSwitch = controller.LightAttack;
            HeavyAttackNintendoSwitch = controller.HeavyAttack;
            GuardNintendoSwitch = controller.Guard;
            InteractNintendoSwitch = controller.Interact;
            PauseNintendoSwitch = controller.Pause;
            NimbusNintendoSwitch = controller.Nimbus;
            SkillANintendoSwitch = controller.SkillA;
            SkillBNintendoSwitch = controller.SkillB;
            SkillCNintendoSwitch = controller.SkillC;
            SkillDNintendoSwitch = controller.SkillD;
            FPSCameraNintendoSwitch = controller.FPSCamera;
            GourdNintendoSwitch = controller.Gourd;

            NimbusBarrelNintendoSwitch = controller.NimbusBarrel.ReadValue<Vector2>();
            RightStickNintendoSwitch = controller.RightStick.ReadValue<Vector2>();
            LeftStickNintendoSwitch = controller.LeftStick.ReadValue<Vector2>();
        }
    }

}
