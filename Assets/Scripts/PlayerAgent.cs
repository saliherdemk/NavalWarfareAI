using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerShipController2D))]
public class PlayerAgent : Agent
{
    private PlayerShipController2D _controller;
    private ShipMovement _movement;

    private Vector2 _targetPos;
    private float _lastDist;

    public GameManager gm;

    public override void Initialize()
    {
        MaxStep = 0;

        _controller = GetComponent<PlayerShipController2D>();
        _movement = GetComponent<ShipMovement>();
    }

    public override void OnEpisodeBegin()
    {
        gm.RestartGame();

        _targetPos = gm.targetLake.lakeCenter;
        _lastDist = Vector2.Distance(transform.position, _targetPos);

        _controller.hitByMine = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float[] sensors = _controller.GetSensors();

        for (int i = 0; i < sensors.Length; i++)
            sensor.AddObservation(sensors[i]);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float targetThrottle = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float targetRudder = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        _movement.SetInput(targetThrottle, targetRudder);

        float currentDist = Vector2.Distance(transform.position, _targetPos);
        float diff = _lastDist - currentDist;

        AddReward(diff * 0.01f);

        _lastDist = currentDist;

        if (gm.PlayerDie())
        {
            AddReward(-1f);
            EndEpisode();
            return;
        }

        Debug.Log(gm.PlayerReachedTarget());
        if (gm.PlayerReachedTarget())
        {
            AddReward(+3f);
            EndEpisode();
            return;
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;

        cont[0] = Keyboard.current.wKey.isPressed ? 1 : 0;
        cont[0] = Keyboard.current.sKey.isPressed ? -1 : cont[0];

        cont[1] = Keyboard.current.aKey.isPressed ? -1 : 0;
        cont[1] = Keyboard.current.dKey.isPressed ? 1 : cont[1];
    }
}
