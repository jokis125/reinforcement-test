using Unity.Mathematics;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class ShipAgent : Agent
{
    private Rigidbody2D rBody;
    private float lastUpdateDistance = 100f;
    private float distanceTolerance = 0.2f;

    void Start()
    {
        rBody = GetComponent<Rigidbody2D>();
        //Time.timeScale = 2f;
    }

    public Transform Target;

    public override void OnEpisodeBegin()
    {
        
        if (Mathf.Abs(transform.localPosition.x) > 13 || Mathf.Abs(transform.localPosition.y) > 6  || Mathf.Abs(rBody.angularVelocity) > 700)
        {
            //START WITH RANDOM STARTING POSITION
            rBody.angularVelocity = Random.Range(-500,500);
            rBody.velocity = new Vector2(Random.Range(-2,2), Random.Range(-2,2));
            transform.localPosition = new Vector3(Random.Range(-10, 10),Random.Range(-5,5), 0);
            var euler = transform.eulerAngles;
            euler.z = Random.Range(-360.0f, 360.0f);
            transform.eulerAngles = euler;
            
            //START FROM ZERO
            //rBody.velocity = Vector2.zero;
            //rBody.angularVelocity = 0;
            //transform.localPosition = Vector3.zero;
            //transform.rotation = Quaternion.identity;
        }
        
        Target.localPosition = new Vector3(Random.Range(-9, 9) , Random.Range(-4,4), 0);
        lastUpdateDistance = Vector3.Distance(this.transform.localPosition, Target.localPosition);
    }
    
    public override void CollectObservations(VectorSensor sensor)
    {
        // Target and Agent positions
        sensor.AddObservation(Target.localPosition);
        sensor.AddObservation(transform.localPosition);
        
        // Agent rotation
        sensor.AddObservation(transform.localRotation.z);
        sensor.AddObservation(rBody.angularVelocity);

        // Agent velocity
        sensor.AddObservation(rBody.velocity.x);
        sensor.AddObservation(rBody.velocity.y);
    }
    
    public float forceMultiplier = 10;
    public float rotationMultiplier = 10;
    public override void OnActionReceived(float[] vectorAction)
    {
        // Actions, size = 2
        var controlSignal = Vector3.zero;

        if(vectorAction[1] != 0)
        {
            controlSignal.y = vectorAction[1];
            rBody.AddRelativeForce(controlSignal * forceMultiplier);
        }

        if (vectorAction[0] != 0)
        {
            controlSignal.x = -vectorAction[0];
            rBody.AddTorque(controlSignal.x * rotationMultiplier, ForceMode2D.Force);
            //rBody.MoveRotation(transform.rotation);
        }

        // Rewards
        //Debug.Log(rBody.angularVelocity);
        var distanceToTarget = Vector3.Distance(this.transform.localPosition, Target.localPosition);
        if (distanceToTarget + distanceTolerance < lastUpdateDistance)
        {
            var addedReward = (1 / (distanceToTarget + 0.1f) * 10 - Mathf.Abs(rBody.angularVelocity)*0.01f)*0.01f;
            SetReward(addedReward);
            //Debug.Log($"current distance {distanceToTarget}\n old distance {lastUpdateDistance}");
            lastUpdateDistance = distanceToTarget;
        }

        // Reached target
        if (distanceToTarget < 1.3f)
        {
            SetReward(1);
            EndEpisode();
        }

        // Got out of camera view
        else if (Mathf.Abs(transform.localPosition.x) > 13 || Mathf.Abs(transform.localPosition.y) > 6 || Mathf.Abs(rBody.angularVelocity) > 700)
        {
            SetReward(-0.4f);
            EndEpisode();
        }
    }
    
    public override void Heuristic(float[] actionsOut)
    {
        actionsOut[0] = Input.GetAxis("Horizontal");
        actionsOut[1] = Input.GetAxis("Vertical");
    }
}
