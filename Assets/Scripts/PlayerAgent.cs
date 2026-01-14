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
    private RadarDetector _radar;

    float _prevTargetDist;

    public GameManager gm;

    private float[] _nControllerSensors;
    private float[] _nTargetSensors;

    float _progressAccum;

    public override void Initialize()
    {
        _controller = GetComponent<PlayerShipController>();
        _movement = GetComponent<ShipMovement>();
        _raycast = GetComponent<ShipRaycast>();
        _radar = GetComponentInChildren<RadarDetector>();

        _nControllerSensors = new float[Normalizer.PLAYER_CONTROLLER_OBS_SIZE];
        _nTargetSensors = new float[Normalizer.PLAYER_TARGET_OBS_SIZE];
    }

    public override void OnEpisodeBegin()
    {
        gm.RestartGame();

        float[] speeds = { 0f, 0.5f, 1.0f };

        int d = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("enemy_difficulty", 8);
        _movement.speedMult = speeds[d / 3];

        _prevTargetDist = Vector2.Distance(transform.localPosition, gm.targetLake.lakeCenter);
        _progressAccum = 0f;
    }

    public float[] GetRelativePositionData(Vector2 targetWorldPosition)
    {
        Vector2 selfPos = transform.localPosition;
        Vector2 toTargetWorld = targetWorldPosition - selfPos;
        float distance = toTargetWorld.magnitude;
        Vector2 localDir = transform.InverseTransformDirection(toTargetWorld.normalized);
        return new float[] { localDir.x, localDir.y, distance };
    }

    private void AddArray(VectorSensor sensor, float[] values)
    {
        for (int i = 0; i < values.Length; i++)
            sensor.AddObservation(values[i]);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Normalizer.NormalizePlayerController(_controller.GetSensors(), _nControllerSensors);
        Normalizer.NormalizeRelativePositionData(
            GetRelativePositionData(gm.targetLake.lakeCenter),
            _nTargetSensors
        );

        AddArray(sensor, _nControllerSensors);
        AddArray(sensor, _nTargetSensors);
        // Debug.Log($"controller sensors[{string.Join(", ", _nControllerSensors)}]");
        // Debug.Log($"target sensors[{string.Join(", ", _nTargetSensors)}]");
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float throttle = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float rudder = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        _movement.SetInput(throttle, rudder);

        TargetProgressReward();
        MineDangerReward();
        EnemyDangerReward();

        AddReward(-0.001f);
    }

    void TargetProgressReward()
    {
        float curr = Vector2.Distance(transform.localPosition, gm.targetLake.lakeCenter);
        float delta = _prevTargetDist - curr;

        float r = delta * 0.01f;

        if (_progressAccum + r > 0.5f)
            r = Mathf.Max(0f, 0.5f - _progressAccum);

        _progressAccum += r;
        AddReward(r);

        _prevTargetDist = curr;
    }

    void MineDangerReward()
    {
        if (!_radar.TryGetClosestMine(out var mine))
            return;

        if (mine.Distance > 10f)
            return;

        float distDanger = 1f - Mathf.Clamp01(mine.Distance / 50f);
        float timeDanger = 1f - mine.TimeToExplosion;

        float danger = distDanger * timeDanger;
        AddReward(-danger * 0.01f);
    }

    void EnemyDangerReward()
    {
        Vector2 p = transform.localPosition;
        Vector2 t = gm.targetLake.lakeCenter;

        Vector2 ptWorld = (t - p).normalized;
        Vector2 ptLocal = transform.InverseTransformDirection(ptWorld);

        float worstThreat = 0f;

        foreach (var e in _radar.DetectedTargets)
        {
            if (e.Distance > 20f)
                continue;

            Vector2 peLocal = e.LocalDir;

            float alignment = Vector2.Dot(ptLocal, peLocal);
            if (alignment <= 0f)
                continue;

            float distWeight = 1f - Mathf.Clamp01(e.Distance / 20f);
            float threat = alignment * distWeight;

            worstThreat = Mathf.Max(worstThreat, threat);
        }

        AddReward(-worstThreat * 0.003f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;

        cont[0] = Keyboard.current.upArrowKey.isPressed ? 1 : 0;
        cont[0] = Keyboard.current.downArrowKey.isPressed ? -1 : cont[0];

        cont[1] = Keyboard.current.leftArrowKey.isPressed ? -1 : 0;
        cont[1] = Keyboard.current.rightArrowKey.isPressed ? 1 : cont[1];
    }
}
