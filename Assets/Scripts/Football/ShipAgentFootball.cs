﻿using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.MLAgents.Sensors;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class ShipAgentFootball : Agent
{
    private Rigidbody2D rBody;

    public enum Team
    {
        Left = 0,
        Right = 1
    }

    public enum Position
    {
        Striker,
        Generic,
        Goalie
    }

    [HideInInspector] 
    public Team team;
    private int m_PlayerIndex;
    private float _mBallTouch = 0.2f; // coefficient for the reward for colliding with a ball.
    const float kPower = 25;
    private float m_kickPower = 1f;
    public GameObject[] opponents;
    public GameObject[] teammates;
    public Arena area;
    public Position position;

    [HideInInspector] public float timePenalty;
    private BehaviorParameters m_BehaviorParameters;
    private Vector3 m_Transform;
    private float m_Rotation;
    private EnvironmentParameters m_ResetParams;

    private float m_Existential;
    private float _timePenaltyMultiplier;

    private List<Rigidbody2D> _opponentRbs = new List<Rigidbody2D>();
    private List<Rigidbody2D> _teamRbs = new List<Rigidbody2D>();
    private Rigidbody2D _ballRb;
    
    public float forwardSpeed = 2;
    public float rotationVelocity = 250;

    public GameObject leftGoal;
    public GameObject rightGoal;

    //for normalization
    private float _arenaXMax = 32.5f;
    private float _arenaYMax = 13.1f;
    //Estimates
    private float _maxXVelocity = 20f;
    private float _maxYVelocity = 20f;
    private float _maxAngularVelocity = 250f;
    

    public override void Initialize()
    {
        Time.timeScale = 3f;
        
        rBody = GetComponent<Rigidbody2D>();
        _ballRb = Target.GetComponent<Rigidbody2D>();
        
        
        m_Existential = 1f / MaxStep;
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();

        if (m_BehaviorParameters.TeamId == (int) Team.Left)
        {
            team = Team.Left;
            m_Transform = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
            m_Rotation = -90;
        }
        else
        {
            team = Team.Right;
            m_Transform = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
            m_Rotation = 90;
        }

        //opponents = GameObject.FindGameObjectsWithTag(team == Team.Left ? "rightAgent" : "leftAgent");

        foreach (var op in opponents)
        {
            _opponentRbs.Add(op.GetComponent<Rigidbody2D>());
        }
        
        foreach (var tm in teammates)
        {
            _teamRbs.Add(tm.GetComponent<Rigidbody2D>());
        }

        var playerState = new PlayerState
        {
            agentRb = rBody,
            startingPos = transform.localPosition,
            agentScript = this,
        };
        area.PlayerStates.Add(playerState);
        m_PlayerIndex = area.PlayerStates.IndexOf(playerState);
        playerState.playerIndex = m_PlayerIndex;

        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        var force = m_kickPower * kPower;
        if (c.gameObject.CompareTag("ball"))
        {
            // PADARYT CONTINUOUS REWARD UZ ATSTUMA TARP VARTU IR KAMUOLIO
            // REWARD UZ KAMUOLIO TOUCH
            // 
            if (Mathf.Abs(Target.localPosition.y) <= 6.5f)
            {
                AddReward(0.15f * _timePenaltyMultiplier);
            }
                

            /*Vector2 trans2 = transform.localPosition;
            var dir = c.contacts[0].point - trans2;
            dir = dir.normalized;
            c.gameObject.GetComponent<Rigidbody2D>().AddForce(dir * force);*/
        }
    }
    

    public Transform Target;
    //public Transform Opponent;

    public override void OnEpisodeBegin()
    {
        timePenalty = 0;
        _timePenaltyMultiplier = 1;
        _mBallTouch = m_ResetParams.GetWithDefault("ball_touch", _mBallTouch);
        transform.localRotation = Quaternion.Euler(0,0, m_Rotation);
        transform.localPosition = m_Transform;
        rBody.velocity = Vector3.zero;
        rBody.angularVelocity = 0;
        area.ResetBall();
    }

 
    
    public override void CollectObservations(VectorSensor sensor)
    {
        //Agent information
        sensor.AddObservation(NormalizePosition(transform.localPosition));
        sensor.AddObservation(NormalizeRotation(transform.localRotation));
        sensor.AddObservation(NormalizeAngularVelocity(rBody.angularVelocity));
        sensor.AddObservation(NormalizeVelocity(rBody.velocity));

        //Ball information
        sensor.AddObservation(NormalizePosition(Target.localPosition));
        sensor.AddObservation(NormalizeVelocity(_ballRb.velocity));
        
        
        //Opponents observations
        for (var i = 0; i < opponents.Length; i++)
        {
            sensor.AddObservation(NormalizePosition(opponents[i].transform.localPosition));
            sensor.AddObservation(NormalizeRotation(opponents[i].transform.localRotation));
            sensor.AddObservation(NormalizeVelocity(_opponentRbs[i].velocity));
        }
        
        //Teammates observations
        for (var i = 0; i < teammates.Length; i++)
        {
            sensor.AddObservation(NormalizePosition(teammates[i].transform.localPosition));
            sensor.AddObservation(NormalizeRotation(teammates[i].transform.localRotation));
            sensor.AddObservation(NormalizeVelocity(_teamRbs[i].velocity));
        }
    }
    

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        
        
        //Controls
        MoveAgent(actionBuffers.DiscreteActions);
        
        // Existential penalty UNUSED
        if (position == Position.Goalie)
            AddReward(m_Existential);
        else if(position == Position.Striker)
            AddReward(-m_Existential);
        else
        {
            timePenalty += m_Existential;
        }

        _timePenaltyMultiplier = 1-timePenalty;
        area.CheckForGoal();
        AddRewardForBallClosenessToGoal();
        
    }
    
    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discreteActionsOut = actionsOut.DiscreteActions;
        discreteActionsOut.Clear();
        if (Input.GetKey(KeyCode.UpArrow))
        {
            discreteActionsOut[0] = 1;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            discreteActionsOut[0] = 2;
        }
        
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            discreteActionsOut[1] = 1;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            discreteActionsOut[1] = 2;
        }
    }

    private void MoveAgent(ActionSegment<int> act)
    {
        var dirToGo = Vector2.zero;
        var rotateDir = 0f;

        var forwardAxis = act[0];
        var rotateAxis = act[1];

        switch (forwardAxis)
        {
            case 0:
                dirToGo = Vector2.zero;
                break;
            case 1:
                dirToGo = Vector2.up * forwardSpeed;
                
                break;
            case 2:
                dirToGo = Vector2.up * -forwardSpeed;
                break;
        }


        switch (rotateAxis)
        {
            case 0:
                rotateDir = 0;
                break;
            case 1:
                rotateDir = rotationVelocity;
                break;
            case 2:
                rotateDir = -rotationVelocity;
                break;
        }
        rBody.angularVelocity = rotateDir;
        rBody.AddRelativeForce(dirToGo);

    }
    
    //------------------------------------------normalization
    private Vector2 NormalizePosition(Vector3 pos)
    {
        /*Debug.Log(new Vector3((pos.x - (-_arenaXMax))/(_arenaXMax - (-_arenaXMax)), 
            (pos.y - (-_arenaYMax))/(_arenaYMax - (-_arenaYMax))));*/
        return new Vector2((pos.x - (-_arenaXMax))/(_arenaXMax - (-_arenaXMax)), 
            (pos.y - (-_arenaYMax))/(_arenaYMax - (-_arenaYMax)));
    }

    private float NormalizeRotation(Quaternion rot)
    {
        Quaternion rotation = rot;
        //Debug.Log(rotation.eulerAngles.z / 360.0f);
        return rotation.eulerAngles.z / 360.0f;
    }

    private Vector2 NormalizeVelocity(Vector2 vel)
    {
        /*Debug.Log(new Vector2((vel.x - (-_maxXVelocity))/(_maxXVelocity - (-_maxXVelocity)), 
            (vel.y - (-_maxYVelocity))/(_maxYVelocity - (-_maxYVelocity))));*/
        return new Vector2((vel.x - (-_maxXVelocity))/(_maxXVelocity - (-_maxXVelocity)), 
            (vel.y - (-_maxYVelocity))/(_maxYVelocity - (-_maxYVelocity)));
    }

    private float NormalizeAngularVelocity(float no)
    {
        //Debug.Log((no - (-_maxAngularVelocity)) / (_maxAngularVelocity - (-_maxAngularVelocity)));
        return (no - (-_maxAngularVelocity)) / (_maxAngularVelocity - (-_maxAngularVelocity));
    }

    private void AddRewardForBallClosenessToGoal()
    {
        float addedReward = 0;
        var ballLocalPos = Target.transform.localPosition;
        switch (team)
        {
            // if ball on the wrong side of the pitch add penalty
            case Team.Left when ballLocalPos.x >= 0:
                addedReward = 3 / Vector3.Distance(ballLocalPos, rightGoal.transform.localPosition);
                break;
            case Team.Left:
                addedReward = -(1 / Vector3.Distance(ballLocalPos, leftGoal.transform.localPosition));
                break;
            case Team.Right when ballLocalPos.x <= 0:
                addedReward = 3 / Vector3.Distance(ballLocalPos, leftGoal.transform.localPosition);
                break;
            case Team.Right:
                addedReward = -(1 / Vector3.Distance(ballLocalPos, rightGoal.transform.localPosition));
                break;
        }
        AddReward(addedReward*0.0025f);
        //Debug.Log(addedReward*0.005f);
    }

    private void AddRewardForAgentClosenessToGoal()
    {
        float addedReward = 0;
        switch (team)
        {
            case Team.Left:
                addedReward = 1 / Vector3.Distance(transform.localPosition, rightGoal.transform.localPosition);
                break;
            case Team.Right:
                addedReward = 1 / Vector3.Distance(transform.localPosition, leftGoal.transform.localPosition);
                break;
        }

        AddReward(addedReward * 0.002f);
    }
}
