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
    private ShipRaycast _raycast;

    private Vector2 _targetPos;
    private float _lastDist;
    private float _startDist;

    public GameManager gm;

    private float[] _normalizedSensors;
    private float[] _normalizedAngle;

    public override void Initialize()
    {
        MaxStep = 30000;

        _controller = GetComponent<PlayerShipController2D>();
        _movement = GetComponent<ShipMovement>();
        _raycast = GetComponent<ShipRaycast>();

        _normalizedSensors = new float[Normalizer.GetPlayerSensorCount()];
        _normalizedAngle = new float[Normalizer.GetTargetAngleCount()];
    }

    public override void OnEpisodeBegin()
    {
        gm.RestartGame();
        _targetPos = gm.targetLake.lakeCenter;
        _startDist = Vector2.Distance(transform.localPosition, _targetPos);
        _lastDist = _startDist;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Normalizer.NormalizePlayerController(_controller.GetSensors(), _normalizedSensors);
        foreach (var s in _normalizedSensors)
        {
            sensor.AddObservation(s);
        }

        float currentDist = Vector2.Distance(transform.localPosition, _targetPos);
        float normalizedDist = Normalizer.NormalizeTargetDistance(currentDist);
        sensor.AddObservation(normalizedDist);

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

        if (gm.PlayerReachedTarget())
        {
            SetReward(1.0f);
            // Debug.Log($"<color=green>WIN! Total Reward: {GetCumulativeReward():F2}</color>");
            EndEpisode();
            return;
        }

        if (gm.PlayerDie())
        {
            SetReward(-1.0f);
            // Debug.Log($"<color=red>DIED. Total Reward: {GetCumulativeReward():F2}</color>");
            EndEpisode();
            return;
        }

        float currentDist = Vector2.Distance(transform.localPosition, _targetPos);
        float diff = _lastDist - currentDist;

        if (_startDist > 0)
        {
            AddReward(diff / _startDist);
        }

        AddReward(-1f / MaxStep);

        // if (StepCount >= MaxStep - 1 && MaxStep > 0)
        // {
        //     Debug.Log($"<color=yellow>TIMEOUT. Total Reward: {GetCumulativeReward():F2}</color>");
        // }

        _lastDist = currentDist;
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
