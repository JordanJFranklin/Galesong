using UnityEngine;

[ExecuteAlways]
public class DropPhysicsController : MonoBehaviour
{
    [Header("Gravity Settings")]
    public bool applyGravity = true;
    public bool grounded = false;
    public float gravityStrength = 9.81f;
    public float gravityMultiplier = 0f;
    public float gravityIncreaseRate = 1f;
    public float gravityMax = 20f;

    [Header("Ground Check")]
    public float rayHeight = 0.5f;
    public float rayDistance = 0.6f;
    public LayerMask collisionMask;

    [Header("Collision Capsule")]
    public Vector3 capsuleOffsetStart = new Vector3(0, 0.91f, 0);
    public Vector3 capsuleOffsetEnd = new Vector3(0, -1.14f, 0);
    public float capsuleRadius = 0.5f;

    [Header("Velocity")]
    public Vector3 velocity;

    [Header("Gizmos")]
    public bool showGizmos = true;

    private RaycastHit tempHit;

    void FixedUpdate()
    {
        GroundCheck();
        Gravity();
        CollisionCorrection();
    }

    private void Gravity()
    {
        if (!applyGravity || grounded)
        {
            gravityMultiplier = 0f;
            return;
        }

        gravityMultiplier += gravityIncreaseRate * Time.deltaTime;
        gravityMultiplier = Mathf.Clamp(gravityMultiplier, 0, gravityMax);

        velocity.y -= (gravityStrength * Time.deltaTime + gravityMultiplier * Time.deltaTime);
        transform.position += velocity * Time.deltaTime;
    }

    private void GroundCheck()
    {
        Vector3 rayOrigin = transform.position + Vector3.up * rayHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out tempHit, rayDistance, collisionMask & ~LayerMask.GetMask("Player")))
        {
            grounded = true;

            // Snap to ground (optional)
            float yOffset = tempHit.point.y + capsuleOffsetStart.y;
            Vector3 newPos = transform.position;
            newPos.y = Mathf.Max(newPos.y, yOffset);
            transform.position = newPos;

            // Reset vertical velocity
            velocity.y = 0;
        }
        else
        {
            grounded = false;
        }
    }

    private void CollisionCorrection()
    {
        Collider[] overlaps = new Collider[10];
        int count = Physics.OverlapCapsuleNonAlloc(
        transform.TransformPoint(capsuleOffsetStart),
        transform.TransformPoint(capsuleOffsetEnd),
        capsuleRadius,
        overlaps,
        collisionMask & ~LayerMask.GetMask("Player"),
        QueryTriggerInteraction.Ignore);


        for (int i = 0; i < count; i++)
        {
            Collider col = overlaps[i];
            if (Physics.ComputePenetration(
                GetComponent<Collider>(), transform.position, transform.rotation,
                col, col.transform.position, col.transform.rotation,
                out Vector3 dir, out float dist))
            {
                Vector3 penetration = dir * dist;
                transform.position += penetration;

                Vector3 projectedVel = Vector3.Project(velocity, -dir);
                velocity -= projectedVel;
            }
        }
    }

    public Vector3 GetVelocity()
    {
        return velocity;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        Gizmos.color = Color.cyan;
        Vector3 a = transform.TransformPoint(capsuleOffsetStart);
        Vector3 b = transform.TransformPoint(capsuleOffsetEnd);

        Gizmos.DrawWireSphere(a, capsuleRadius);
        Gizmos.DrawWireSphere(b, capsuleRadius);
        Gizmos.DrawLine(a + Vector3.forward * capsuleRadius, b + Vector3.forward * capsuleRadius);
        Gizmos.DrawLine(a + Vector3.back * capsuleRadius, b + Vector3.back * capsuleRadius);
        Gizmos.DrawLine(a + Vector3.left * capsuleRadius, b + Vector3.left * capsuleRadius);
        Gizmos.DrawLine(a + Vector3.right * capsuleRadius, b + Vector3.right * capsuleRadius);

        Gizmos.color = Color.yellow;
        Vector3 rayStart = transform.position + Vector3.up * rayHeight;
        Gizmos.DrawLine(rayStart, rayStart + Vector3.down * rayDistance);
        Gizmos.DrawSphere(rayStart + Vector3.down * rayDistance, 0.05f);

        if (Application.isPlaying)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position, transform.position + velocity);
        }
    }
#endif
}
