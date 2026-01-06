using NormalizerClass;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerShipController))]
[RequireComponent(typeof(ShipMovement))]
[RequireComponent(typeof(ShipRaycast))]
public class PlayerAgent : Agent
{
    private PlayerShipController _controller;
    private ShipMovement _movement;
    private ShipRaycast _raycast;

    private Vector2 _targetPos;

    public GameManager gm;

    private float[] _normalizedSensors;
    private float[] _normalizedAngle;

    public override void Initialize()
    {
        _controller = GetComponent<PlayerShipController>();
        _movement = GetComponent<ShipMovement>();
        _raycast = GetComponent<ShipRaycast>();

        _normalizedSensors = new float[Normalizer.GetPlayerSensorCount()];
        _normalizedAngle = new float[Normalizer.GetTargetAngleCount()];
    }

    public override void OnEpisodeBegin()
    {
        gm.RestartGame();

        _movement.speedMult = Academy.Instance.EnvironmentParameters.GetWithDefault(
            "player_speed_multiplier",
            1.0f
        );
        _targetPos = gm.targetLake.lakeCenter;
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

        CalculateMovementRewards();
        CalculateRadarRewards();

        AddReward(-0.0002f);
    }

    private void CalculateMovementRewards()
    {
        float[] surroundings = _raycast.GetSensors();
        float rayDist = _raycast.GetRayDistance();
        Vector2 shipVelocity = _movement.Velocity;

        float totalDanger = 0f;

        for (int i = 0; i < surroundings.Length; i++)
        {
            float dist = surroundings[i];

            float angleStep = 360f / surroundings.Length;
            float angle = i * angleStep;
            Vector2 rayDir = Quaternion.Euler(0f, 0f, angle) * transform.up;

            float closingSpeed = Vector2.Dot(shipVelocity, rayDir);

            if (closingSpeed > 0.1f)
            {
                float risk = closingSpeed / (dist + 0.1f);
                if (risk > 0.5f)
                {
                    totalDanger += risk;
                }
            }
        }

        float ttcPenalty = Mathf.Clamp(totalDanger * 0.05f, 0f, 1.0f);
        AddReward(-ttcPenalty);

        float speed = _movement.Velocity.magnitude;

        if (speed > 0.1f)
        {
            Vector2 dirToTarget = (_targetPos - (Vector2)transform.localPosition).normalized;
            Vector2 moveDir = _movement.Velocity.normalized;

            float alignment = Vector2.Dot(moveDir, dirToTarget);

            if (alignment > 0)
            {
                int lastIndex = surroundings.Length - 1;
                float frontDist = surroundings[0] / rayDist;
                float leftDist = surroundings[1] / rayDist;
                float rightDist = surroundings[lastIndex] / rayDist;

                float forwardClearance = (frontDist + leftDist + rightDist) / 3.0f;
                float speedFactor = Mathf.Clamp01(speed / _movement.maxSpeed);
                AddReward(alignment * speedFactor * 0.002f * forwardClearance);
            }
        }
    }

    private void CalculateRadarRewards()
    {
        var mines = _controller._radarDetector.DetectedMines;
        float mineDanger = 0f;

        foreach (var mine in mines)
        {
            if (mine.ClosingSpeed > 0.5f && mine.Distance < 20f)
            {
                float tti = mine.Distance / mine.ClosingSpeed;

                if (tti < 3.0f)
                {
                    float risk = 0.01f / tti;
                    mineDanger += risk;
                }
            }
        }
        AddReward(-Mathf.Clamp(mineDanger, 0f, 0.1f));

        var enemies = _controller._radarDetector.VisibleTargets;
        float enemyPressure = 0f;

        foreach (var enemy in enemies)
        {
            if (enemy.Distance < 25f && enemy.ClosingSpeed > 1.0f)
            {
                float pressure = enemy.ClosingSpeed / enemy.Distance;

                if (pressure > 0.2f)
                {
                    enemyPressure += pressure;
                }
            }
        }

        AddReward(-Mathf.Clamp(enemyPressure * 0.05f, 0f, 0.05f));
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
