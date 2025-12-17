using NormalizerClass;
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

    private float[] _normalizedSensors;
    private float[] _normalizedAngle;

    public override void Initialize()
    {
        MaxStep = 0;

        _controller = GetComponent<PlayerShipController2D>();
        _movement = GetComponent<ShipMovement>();

        _normalizedSensors = new float[Normalizer.GetPlayerSensorCount()];
        _normalizedAngle = new float[Normalizer.GetTargetAngleCount()];
    }

    public override void OnEpisodeBegin()
    {
        gm.RestartGame();
        _targetPos = gm.targetLake.lakeCenter;
        _lastDist = Vector2.Distance(transform.localPosition, _targetPos);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Normalizer.NormalizePlayerController(_controller.GetSensors(), _normalizedSensors);
        foreach (var s in _normalizedSensors)
        {
            sensor.AddObservation(s);
        }

        sensor.AddObservation(_lastDist);

        float angleToTarget = Vector2.SignedAngle(
            transform.up,
            _targetPos - (Vector2)transform.localPosition
        );

        Normalizer.NormalizeTargetAngle(angleToTarget, _normalizedAngle);
        foreach (float item in _normalizedAngle)
        {
            sensor.AddObservation(item);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float targetThrottle = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float targetRudder = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        _movement.SetInput(targetThrottle, targetRudder);

        float currentDist = Vector2.Distance(transform.localPosition, _targetPos);
        float diff = _lastDist - currentDist;

        AddReward(diff * 0.01f);

        _lastDist = currentDist;

        if (gm.PlayerDie())
        {
            AddReward(-1f);
            EndEpisode();
            return;
        }

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
