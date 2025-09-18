using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ThrowableObject : MonoBehaviour
{
    private bool isBeingHeld = false;

    [Header("Target Hold Point")]
    public Transform holdPoint;

    [Header("Floaty Movement Settings")]
    public float followLerpSpeed = 10f;
    public float maxOffsetDistance = 2f;

    [Header("Floating Animation")]
    public float floatSpeed = 2f;
    public float floatHeight = 0.2f;

    [Header("Idle Rotation")]
    public float rotationSpeed = 30f;
    public Vector3 rotationAxis = Vector3.up;

    private Transform visualModel;
    private Vector3 visualStartPos;
    private Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        visualModel = transform.GetChild(0); // Assumes first child is the model
        visualStartPos = visualModel.localPosition;
    }

    private void FixedUpdate()
    {
        if (isBeingHeld && holdPoint != null)
        {
            Vector3 targetPos = holdPoint.position;
            float distance = Vector3.Distance(transform.position, targetPos);

            if (distance > maxOffsetDistance)
            {
                transform.position = targetPos;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, followLerpSpeed * Time.deltaTime);
            }
        }

        if (visualModel != null && isBeingHeld)
        {
            float offsetY = Mathf.Sin(Time.time * floatSpeed) * floatHeight;

            // World-space float position
            Vector3 floatOffset = Vector3.up * offsetY;
            visualModel.position = transform.position + floatOffset;

            // Rotate in world space if needed (can change to Space.World)
            visualModel.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.World);
        }
        else
        {
            visualModel.transform.localPosition = Vector3.zero;
        }
    }

    /// <summary>
    /// Sets this object's held state and assign the hold point transform.
    /// Also disables/enables physics as needed.
    /// </summary>
    public void SetHeldState(bool held, Transform newHoldPoint = null)
    {
        isBeingHeld = held;
        holdPoint = newHoldPoint;

        if (held)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
        else
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            holdPoint = null;
        }
    }
}
