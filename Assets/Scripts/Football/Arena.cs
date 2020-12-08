using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class PlayerState
{
    public int playerIndex;
    public Rigidbody2D agentRb;
    public Vector3 startingPos;
    public ShipAgentFootball agentScript;
    public float ballPosReward;
}
public class Arena : MonoBehaviour
{
    public GameObject ball;
    [HideInInspector] 
    public Rigidbody2D ballRb;
    public List<PlayerState> PlayerStates = new List<PlayerState>();
    [HideInInspector]
    public Vector3 ballStartingPos;
    //public GameObject goalTextUI;

    private EnvironmentParameters m_ResetParams;
    private float goalLine = 41; //-41 and 41
    
    void Awake()
    {
        ballRb = ball.GetComponent<Rigidbody2D>();
        ballStartingPos = ball.transform.localPosition;

        m_ResetParams = Academy.Instance.EnvironmentParameters;
    }

    public void GoalTouched(ShipAgentFootball.Team scoredTeam)
    {
        foreach (var playerState in PlayerStates)
        {
            if(playerState.agentScript.team == scoredTeam)
                playerState.agentScript.AddReward(1 + playerState.agentScript.timePenalty);
            else
                playerState.agentScript.AddReward(-1);
            playerState.agentScript.EndEpisode();
        }
    }

    public void CheckForGoal()
    {
        if (ball.transform.localPosition.x > goalLine)
        {
            GoalTouched(ShipAgentFootball.Team.Left);
        }
        else if (ball.transform.localPosition.x < -goalLine)
        {
            GoalTouched(ShipAgentFootball.Team.Right);
        }
    }

    public void ResetBall()
    {
        ball.transform.localPosition = ballStartingPos;
        ballRb.velocity = Vector2.zero;
        ballRb.angularVelocity = 0;
    }
}
