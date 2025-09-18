using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[RequireComponent(typeof(Rigidbody))]
public class BuoyancyPhysics : MonoBehaviour
{
    public List<Transform> floaters = new List<Transform>();
    public bool selfIntialize;
    public bool waterSurfaced;
    public bool isSubmerged;
    public bool isSinking;
    public bool enablePhysics = true;
    public float underwaterBias = 0f;
    public float underwaterDrag = 3f;
    public float underWaterAngularDrag = 1f;
    public float airDrag = 0f;
    public float airAngularDrag = 0.05f;
    public float floatingPower = 15f;
    public float sinkThreshold = 10f;
    public float isSinkingForce = 5f;
    public float diveTime;
    public float currDiveTime;
    public float waterHeightOffset = 0f;
    public float currentWaterLevel;
    public Vector3 currentWaterHeight;
    Rigidbody rb;
    int floatersUnderwater;
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currDiveTime = diveTime;
        if(!floaters.Contains(transform) && selfIntialize)
        {
            floaters.Add(transform);
        }
    }

    private void Update()
    {
        //Set Dive Time for submerging
        currDiveTime = Mathf.Clamp(currDiveTime, 0, diveTime);

        //Countdown 
        if (currDiveTime > 0 && isSinking)
        {
            currDiveTime -= Time.deltaTime;
        }

        if (currDiveTime <= 0 && isSinking)
        {
            isSinking = false;
        }
    }

    void FixedUpdate()
    {
        floatersUnderwater = 0;

        if (floaters != null && enablePhysics)
        {
            foreach (Transform WaterFloat in floaters)
            {
                Vector3 FinalWaterHeight = new Vector3(currentWaterHeight.x, currentWaterHeight.y + waterHeightOffset, currentWaterHeight.z);
                float waterToFloatersDifferentiation = WaterFloat.position.y - waterHeightOffset - FinalWaterHeight.y;
                waterSurfaced = waterToFloatersDifferentiation > -1;
                currentWaterLevel = waterToFloatersDifferentiation;

                //print(transform.name + " - Water Forces: " + waterToFloatersDifferentiation + " Surface: " + (underwaterBias - waterHeightOffset));

                if (waterToFloatersDifferentiation < (underwaterBias - waterHeightOffset))
                {
                    //Float To Surface Force
                    if (!isSinking)
                    {
                        rb.AddForceAtPosition(Vector3.up * floatingPower * Mathf.Abs(waterToFloatersDifferentiation), WaterFloat.position, ForceMode.Impulse);
                    }

                    //Sink Force
                    if (isSinking)
                    {
                        rb.AddForceAtPosition(Vector3.down * isSinkingForce, transform.position * Mathf.Abs(waterToFloatersDifferentiation), ForceMode.Acceleration);
                    }

                    floatersUnderwater += 1;

                    if (!isSubmerged)
                    {
                        isSubmerged = true;
                        SwitchState(true);
                    }
                }

                if (isSubmerged && floatersUnderwater == 0)
                {
                    isSubmerged = false;
                    SwitchState(false);
                }
            }
        }
    }

    public void DetectDiveImpact(float speedAtImpact)
    {
        if(speedAtImpact > sinkThreshold)
        {
            currDiveTime = diveTime;
            //isSinking = true;
            print(gameObject.name + " Will Sink");
        }
    }

    public void EndDive()
    {
        isSinking = false;
        currDiveTime = 0;
    }

    public void SwitchState(bool underwater)
    {
        if(isSubmerged)
        {
            rb.linearDamping = underwaterDrag;
            rb.angularDamping = underWaterAngularDrag;
        }
        else
        {
            rb.linearDamping = airDrag;
            rb.angularDamping = airAngularDrag;
        }
    }
}
