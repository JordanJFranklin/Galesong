using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractor : MonoBehaviour
{
    public float collectionDistance;
    public float interactRange = 3f;
    public Transform holdPoint;
    public float throwForce = 10f;

    public bool holdingObject;
    private GameObject currentHeldObject;
    private Rigidbody heldRb;
    private ThrowableObject heldInteractable;

    public GameObject currentNPC;
    public LayerMask NPCMask;
    public LayerMask ItemMask;
    RaycastHit hitNPC;

    private Rigidbody rb;
    private CharacterStats stats;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        stats = GetComponent<CharacterStats>();
    }

    private void FixedUpdate()
    {
        Interact();
        CollectionOnTouch();
        CollectNearbyDrops();
        HandleHeldObject();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out HaliDrop hali) &&
            !hali.dropType.Equals(DropType.HaliCeleste) &&
            !hali.dropType.Equals(DropType.HaliMoon))
        {
            if (!hali.isSeekingPlayer)
            {
                hali.stats = stats;
                hali.StartSeeking();
            }
        }

        if (other.TryGetComponent(out PrismaDrop prisma))
        {
            if (!prisma.isSeekingPlayer)
            {
                prisma.stats = stats;
                prisma.StartSeeking();
            }
        }

        if (other.TryGetComponent(out CardDropBundle cardDrop))
        {
            foreach (var card in cardDrop.containedCards)
            {
                GetComponent<CardManager>().GainDropCard(card);
            }

            Destroy(other.gameObject);
        }
    }

    private void CollectNearbyDrops()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, collectionDistance, ItemMask, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out HaliDrop hali) &&
                !hali.dropType.Equals(DropType.HaliCeleste) &&
                !hali.dropType.Equals(DropType.HaliMoon) && 
                !hali.dropType.Equals(DropType.HaliSoul))
            {
                if (!hali.isSeekingPlayer)
                {
                    hali.stats = stats;
                    hali.StartSeeking();
                }
            }
            else if (hit.TryGetComponent(out PrismaDrop prisma))
            {
                if (!prisma.isSeekingPlayer)
                {
                    prisma.stats = stats;
                    prisma.StartSeeking();
                }
            }

            if (hit.TryGetComponent(out CardDropBundle cardDrop))
            {
                foreach (var card in cardDrop.containedCards)
                {
                    GetComponent<CardManager>().GainDropCard(card);
                }

                Destroy(cardDrop.gameObject);
            }
        }
    }

    private void CollectionOnTouch()
    {
        PlayerDriver player = PlayerDriver.Instance;
        Collider[] overlaps = new Collider[10];

        int num = Physics.OverlapCapsuleNonAlloc(
            transform.TransformPoint(player.physicsProperties.collisionOriginStart),
            transform.TransformPoint(player.physicsProperties.collisionOriginEnd),
            player.physicsProperties.collisionRadius,
            overlaps,
            player.physicsProperties.excludePlayer,
            QueryTriggerInteraction.UseGlobal);

        Collider myCollider = player.physicsProperties.CapsuleCol;

        for (int i = 0; i < num; i++)
        {
            if (overlaps[i].TryGetComponent(out PrismaDrop prisma))
            {
                prisma.stats = stats;
                stats.GainScarlet(prisma.dropType == DropType.Prisma
                    ? prisma.restoreAmount
                    : stats.GetStatValue(StatType.Scarlet));
                Destroy(prisma.gameObject);
            }

            if (overlaps[i].TryGetComponent(out HaliDrop hali) &&
                !hali.dropType.Equals(DropType.HaliCeleste) &&
                !hali.dropType.Equals(DropType.HaliMoon) && 
                !hali.dropType.Equals(DropType.HaliSoul))
            {
                hali.stats = stats;
                ProgressionInv.Instance.GainCurrency(hali.value);
                Destroy(hali.gameObject);
            }
        }
    }

    private void HandleHeldObject()
    {
        if (currentHeldObject)
        {
            heldRb.MovePosition(holdPoint.position);

            var scheme = InputManager.Instance.PlayerInputScheme;

            if (scheme.WasPressedThisFrame(KeyActions.HeavyAttack))
            {
                DropHeldObject();
                holdingObject = false;
            }

            if (scheme.WasPressedThisFrame(KeyActions.LightAttack))
            {
                ThrowHeldObject();
                holdingObject = false;
            }
        }
    }

    private void Interact()
    {
        var scheme = InputManager.Instance?.PlayerInputScheme;
        PlayerDriver player = PlayerDriver.Instance;

        if (scheme == null)
        {
            Debug.LogError("[Interact] InputManager.Instance or PlayerInputScheme is NULL!");
            return;
        }

        if (scheme.WasPressedThisFrame(KeyActions.Interact))
        {
            // --- Safeguards ---
            if (player == null)
            {
                Debug.LogError("[Interact] PlayerDriver.Instance is NULL!");
                return;
            }

            if (player.physicsProperties == null)
            {
                Debug.LogError("[Interact] player.physicsProperties is NULL!");
                return;
            }

            if (PlayerSettings.Instance == null)
            {
                Debug.LogError("[Interact] PlayerSettings.Instance is NULL!");
                return;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[Interact] DialogueManager.Instance is NULL!");
                return;
            }

            if (Camera.main == null)
            {
                Debug.LogError("[Interact] Camera.main is NULL!");
                return;
            }

            // --- Dialogue interaction ---
            if (!player.physicsProperties.dashing &&
                !PlayerSettings.Instance.gameplaySettings.Mode.Equals(CameraMode.DialogueMode) &&
                !PlayerSettings.Instance.gameplaySettings.Mode.Equals(CameraMode.TargetMode) &&
                !DialogueManager.Instance.isDialoguePlaying)
            {
                if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out hitNPC, 5f, NPCMask))
                {
                    if (hitNPC.transform == null)
                    {
                        Debug.LogError("[Interact] Raycast hit something with no transform!");
                    }
                    else if (hitNPC.transform.TryGetComponent(out NPC npc))
                    {
                        if (npc == null)
                        {
                            Debug.LogError("[Interact] NPC component is NULL after raycast hit!");
                        }
                        else if (npc.DialogueFile == null)
                        {
                            Debug.LogWarning("[Interact] NPC found but DialogueFile is NULL: " + npc.name);
                        }
                        else if (!npc.isCommunicating)
                        {
                            npc._player = gameObject;
                            npc.StartConversation(gameObject);
                            player.ResetGroundDetection();
                            return; // we already interacted, don't try pickup
                        }
                    }
                }
            }

            // --- Object pickup ---
            if (currentHeldObject == null)
            {
                if (Physics.Raycast(Camera.main.transform.position, Camera.main.transform.forward, out RaycastHit hit, interactRange))
                {
                    if (hit.collider == null)
                    {
                        Debug.LogError("[Interact] Raycast hit but collider is NULL!");
                    }
                    else
                    {
                        if (!hit.collider.TryGetComponent(out Rigidbody targetRb))
                        {
                            Debug.LogWarning("[Interact] Object hit does not have a Rigidbody: " + hit.collider.name);
                        }

                        if (!hit.collider.TryGetComponent(out ThrowableObject obj))
                        {
                            Debug.LogWarning("[Interact] Object hit does not have a ThrowableObject: " + hit.collider.name);
                        }

                        if (targetRb != null && obj != null)
                        {
                            PickUpObject(hit.collider.gameObject);
                            holdingObject = true;
                            return;
                        }
                    }
                }
                else
                {
                    Collider[] hits = Physics.OverlapSphere(transform.position, interactRange);
                    float closestDist = float.MaxValue;
                    Collider closest = null;

                    foreach (var h in hits)
                    {
                        if (h == null)
                        {
                            Debug.LogError("[Interact] OverlapSphere returned a NULL collider!");
                            continue;
                        }

                        float dist = Vector3.Distance(transform.position, h.transform.position);
                        if (dist < closestDist &&
                            h.attachedRigidbody != null &&
                            h.transform.TryGetComponent(out ThrowableObject obj))
                        {
                            closest = h;
                            closestDist = dist;
                        }
                    }

                    if (closest != null)
                    {
                        PickUpObject(closest.gameObject);
                        return;
                    }
                    else
                    {
                        Debug.LogWarning("[Interact] No valid ThrowableObject found in overlap sphere.");
                    }
                }
            }
        }
    }



    private void PickUpObject(GameObject obj)
    {
        currentHeldObject = obj;
        heldRb = obj.GetComponent<Rigidbody>();
        heldInteractable = obj.GetComponent<ThrowableObject>();

        // Parent to hold point (or wherever you're holding it)
        obj.transform.SetParent(holdPoint);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;

        // Disable physics to prevent jitter
        if (heldRb != null)
        {
            heldRb.linearVelocity = Vector3.zero;
            heldRb.angularVelocity = Vector3.zero;
            heldRb.isKinematic = true;
            heldRb.useGravity = false;
            heldRb.interpolation = RigidbodyInterpolation.None; // Optional
            heldRb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            heldRb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (heldInteractable != null)
        {
            heldInteractable.SetHeldState(true);
        }
    }


    private void DropHeldObject()
    {
        if (currentHeldObject == null) return;

        currentHeldObject.transform.parent = null;

        heldRb.useGravity = true;
        heldRb.isKinematic = false;
        heldRb.constraints = RigidbodyConstraints.None;

        if (heldInteractable != null)
        {
            heldInteractable.SetHeldState(false);
            heldInteractable = null;
        }

        currentHeldObject = null;
        heldRb = null;
    }

    private void ThrowHeldObject()
    {
        if (currentHeldObject == null) return;

        // Detach from hold point or player
        currentHeldObject.transform.parent = null;

        if (heldRb != null)
        {
            // Set Rigidbody settings for better collision detection
            heldRb.isKinematic = false;
            heldRb.useGravity = true;
            heldRb.interpolation = RigidbodyInterpolation.Interpolate;
            heldRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            heldRb.constraints = RigidbodyConstraints.None;


            // Apply throw force in world forward direction
            Vector3 throwDirection = Camera.main.transform.forward;

            Vector3 playerVelocity = PlayerDriver.Instance.physicsProperties.vel;
            Vector3 totalThrowVelocity = throwDirection * throwForce + playerVelocity;

            heldRb.linearVelocity = totalThrowVelocity;

        }

        // Inform the interactable it's no longer held
        if (heldInteractable != null)
        {
            heldInteractable.SetHeldState(false);
            heldInteractable = null;
        }

        // Clear references
        currentHeldObject = null;
        heldRb = null;
    }



    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, collectionDistance);
    }
}
