using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum MovePlatformState {MoveToPoint, WaitToMove}
public enum DrawDebugMode {Off,OnSelect,AlwaysOn }
public class MovingPlatform : MonoBehaviour
{
    [System.Serializable]
    public class MovementPoint
    {
        public Vector3 Point;
        public float WaitTime;
        public bool ShouldWaitForTime;
    }
    [Header("Debug")]
    public DrawDebugMode drawDebugMotionPath;

    [Header("Settings")]
    public bool isPlatformMoving = true;
    public bool isAdditive = true;
    private float currentTimer;
    public MovePlatformState moveState;
    public Transform Platform;
    public float speed;
    public int currentPoint;
    public List<MovementPoint> MovePoints;

    // Start is called before the first frame update
    void Start()
    {
        if (MovePoints.Count > 0)
        {
            Platform.localPosition = MovePoints[0].Point;
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        PlatformStateMachine();
    }

    void PlatformStateMachine()
    {
        if (Platform != null)
        {
            Platform.position = Platform.position;
            Platform.SetParent(null);
        }

        if (isPlatformMoving && MovePoints.Count > 0)
        {
            if (moveState.Equals(MovePlatformState.MoveToPoint))
            {
                //Goal Position Reference
                Vector3 GoalPosition = Vector3.zero;

                //if additive add the previous position with the next posiiton
                if (isAdditive)
                {
                    if ((currentPoint - 1) < 0)
                    {
                        GoalPosition = MovePoints[currentPoint].Point + Vector3.zero;
                    }
                    else
                    {
                        GoalPosition = MovePoints[currentPoint].Point;
                    }
                }
                //if subtractive add the previous position with the next posiiton
                else
                {
                    if ((currentPoint + 1) < MovePoints.Count)
                    {
                        GoalPosition = MovePoints[currentPoint].Point - Vector3.zero;
                    }
                    else
                    {
                        GoalPosition = MovePoints[currentPoint].Point - MovePoints[currentPoint - 1].Point;
                    }
                }

                Platform.localPosition = Vector3.LerpUnclamped(Platform.localPosition, GoalPosition, Time.deltaTime * speed);

                if (Vector3.Distance(Platform.localPosition, GoalPosition) < 1.5f)
                {
                    if (MovePoints[currentPoint].ShouldWaitForTime)
                    {
                        currentTimer = MovePoints[currentPoint].WaitTime;
                        moveState = MovePlatformState.WaitToMove;
                    }
                    else
                    {
                        IncrementIndex();
                    }
                }
            }

            if (moveState.Equals(MovePlatformState.WaitToMove))
            {
                currentTimer = Mathf.Clamp(currentTimer, 0, MovePoints[currentPoint].WaitTime);
                currentTimer -= Time.deltaTime;

                if (currentTimer <= 0)
                {
                    IncrementIndex();
                }
            }
        }
    }

    void IncrementIndex()
    {
        moveState = MovePlatformState.MoveToPoint;

        //Minimum Point
        if (currentPoint == 0)
        {
            isAdditive = true;
        }

        //Maximum Point
        if (currentPoint == MovePoints.Count)
        {
            isAdditive = false;
        }

        if (isAdditive)
        {
            if ((currentPoint + 1) < MovePoints.Count)
            {
                currentPoint += 1;
            }
            else
            {
                currentPoint -= 1;
                isAdditive = false;
                return;
            }
        }
        else
        {
            if ((currentPoint - 1) >= 0)
            {
                currentPoint -= 1;
            }
            else
            {
                currentPoint += 1;
                isAdditive = true;
                return;
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (drawDebugMotionPath == DrawDebugMode.AlwaysOn && MovePoints != null && MovePoints.Count > 0)
        {
            DrawPathGizmos();
        }
    }

    void OnDrawGizmosSelected()
    {
        if (drawDebugMotionPath == DrawDebugMode.OnSelect && MovePoints != null && MovePoints.Count > 0)
        {
            DrawPathGizmos();
        }
    }

    private void DrawPathGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(MovePoints[0].Point, 0.4f);

        for (int i = 0; i < MovePoints.Count - 1; i++)
        {
            Vector3 start = MovePoints[i].Point;
            Vector3 end = MovePoints[i + 1].Point;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(start, end);
            Gizmos.DrawWireSphere(end, 0.2f);
        }

        // Stronger indicators for first and last
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(MovePoints[0].Point, 0.4f);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(MovePoints[MovePoints.Count - 1].Point, 0.4f);
    }
#endif


}