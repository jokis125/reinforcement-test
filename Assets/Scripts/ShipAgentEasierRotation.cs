using Unity.Mathematics;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Random = UnityEngine.Random;

public class ShipAgentEasierRotation : Agent
{
    private Rigidbody2D rBody;
    private float distancePositive = 100f;
    private float distanceNegative = 0f;
    private float distanceTolerance = 0.4f;

    void Start()
    {
        rBody = GetComponent<Rigidbody2D>();
        //Time.timeScale = 2f;
    }

    public Transform Target;

    public override void OnEpisodeBegin()
    {
        
        if (Mathf.Abs(transform.localPosition.x) > 13 || Mathf.Abs(transform.localPosition.y) > 6)
        {
            //START WITH RANDOM STARTING POSITION
            //rBody.angularVelocity = Random.Range(-500,500);
            //rBody.velocity = new Vector2(Random.Range(-2,2), Random.Range(-2,2));
            transform.localPosition = new Vector3(Random.Range(-10, 10),Random.Range(-5,5), 0);
            var euler = transform.eulerAngles;
            euler.z = Random.Range(-360.0f, 360.0f);
            transform.eulerAngles = euler;
            
            //START FROM ZERO
            rBody.velocity = Vector2.zero;
            //rBody.angularVelocity = 0;
            //transform.localPosition = Vector3.zero;
            //transform.rotation = Quaternion.identity;
        }
        
        Target.localPosition = new Vector3(Random.Range(-9, 9) , Random.Range(-4,4), 0);
        distancePositive = Vector3.Distance(this.transform.localPosition, Target.localPosition);
        distanceNegative = Vector3.Distance(this.transform.localPosition, Target.localPosition);
        
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
    public float rotationVelocity = 100;
    
    public override void OnActionReceived(float[] vectorAction)
    {
        // Actions, size = 2
        var controlSignal = Vector3.zero;
        //Debug.Log(rBody.velocity);

        //Controls
        controlSignal.x = -vectorAction[0];
        rBody.angularVelocity = rotationVelocity * controlSignal.x;
        controlSignal.y = vectorAction[1];
        //rBody.AddRelativeForce(controlSignal.y * forceMultiplier);
        rBody.AddRelativeForce(new Vector2(0, controlSignal.y * forceMultiplier));
        


        // Rewards
        var distanceToTarget = Vector3.Distance(transform.localPosition, Target.localPosition);
        if (distanceToTarget + distanceTolerance  < distancePositive && distanceToTarget != 0)
        {
            var addedReward = (1 / distanceToTarget * 10 - Mathf.Abs(rBody.angularVelocity)*0.001f) * 0.01f;
            SetReward(addedReward);
            //Debug.Log($"positive {addedReward}");
            distancePositive = distanceToTarget;
        }
        /*
        else if (distanceToTarget - distanceTolerance > distanceNegative && distanceToTarget != 0)
        {
            var addedReward = -(distanceToTarget / 5 + Mathf.Abs(rBody.angularVelocity)*0.01f) * 0.01f;
            SetReward(addedReward);
            //Debug.Log($"negative{addedReward}");
            distanceNegative = distanceToTarget;
        }*/

        // Reached target
        if (distanceToTarget < 1.3f)
        {
            var reward = 1f;
            SetReward(reward);
            EndEpisode();
        }

        // Got out of camera view
        else if (Mathf.Abs(transform.localPosition.x) > 13 || Mathf.Abs(transform.localPosition.y) > 6)
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
