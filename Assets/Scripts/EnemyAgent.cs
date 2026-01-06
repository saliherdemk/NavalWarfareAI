using NormalizerClass;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(EnemyShipController))]
public class EnemyAgent : Agent
{
    public GameManager gm;

    private EnemyShipController _controller;
    private ShipMovement _movement;
    private ShipRaycast _raycast;

    private float[] _normalizedSensors;

    public override void Initialize()
    {
        _controller = GetComponent<EnemyShipController>();
        _movement = GetComponent<ShipMovement>();
        _raycast = GetComponent<ShipRaycast>();

        _normalizedSensors = new float[Normalizer.GetEnemySensorCount()];
    }

    public override void OnEpisodeBegin()
    {
        _movement.speedMult = Academy.Instance.EnvironmentParameters.GetWithDefault(
            "enemy_speed_multiplier",
            1.0f
        );
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Normalizer.NormalizeEnemyController(_controller.GetSensors(), _normalizedSensors);
        foreach (var s in _normalizedSensors)
        {
            sensor.AddObservation(s);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float targetThrottle = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float targetRudder = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        float mineAimX = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);
        float mineAimY = Mathf.Clamp(actions.ContinuousActions[3], -1f, 1f);

        int fireSignal = actions.DiscreteActions[0];

        _movement.SetInput(targetThrottle, targetRudder);

        if (fireSignal == 1)
        {
            _controller.LaunchMine(new Vector2(mineAimX, mineAimY));

            AddReward(-0.002f);
        }

        CalculateMovementPenalty();
        CalculateMineAvoidancePenalty();

        AddReward(-0.0001f);
    }

    private void CalculateMovementPenalty()
    {
        float[] surroundings = _raycast.GetSensors();
        Vector2 shipVelocity = _movement.Velocity;

        float totalDanger = 0f;
        float angleStep = 360f / surroundings.Length;

        for (int i = 0; i < surroundings.Length; i++)
        {
            float dist = surroundings[i];
            float angle = i * angleStep;
            Vector2 rayDir = Quaternion.Euler(0f, 0f, angle) * transform.up;

            float closingSpeed = Vector2.Dot(shipVelocity, rayDir);

            if (closingSpeed > 0.1f)
            {
                float risk = closingSpeed / (dist + 0.1f);
                if (risk > 0.5f)
                    totalDanger += risk;
            }
        }

        float ttcPenalty = Mathf.Clamp(totalDanger * 0.01f, 0f, 0.2f);
        AddReward(-ttcPenalty);
    }

    private void CalculateMineAvoidancePenalty()
    {
        var mines = _controller._radarDetector.DetectedMines;

        foreach (var mine in mines)
        {
            if (mine.Distance < 10f && mine.ClosingSpeed > 0.3f)
            {
                float tti = mine.Distance / Mathf.Max(mine.ClosingSpeed, 0.1f);
                if (tti < 2.0f)
                {
                    AddReward(-0.01f / tti);
                }
            }
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
