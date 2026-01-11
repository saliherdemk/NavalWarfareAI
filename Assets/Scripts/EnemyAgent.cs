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

    private float[] _nControllerSensors;
    private float[] _nPlayerSensors;
    private float[] _nTargetSensors;
    private float[] _nRelVelSensors;

    private float _prevPlayerTargetDist;

    public override void Initialize()
    {
        _controller = GetComponent<EnemyShipController>();
        _movement = GetComponent<ShipMovement>();
        _raycast = GetComponent<ShipRaycast>();

        _nControllerSensors = new float[Normalizer.ENEMY_CONTROLLER_OBS_SIZE];
        _nPlayerSensors = new float[Normalizer.ENEMY_PLAYER_OBS_SIZE];
        _nTargetSensors = new float[Normalizer.ENEMY_TARGET_OBS_SIZE];
        _nRelVelSensors = new float[Normalizer.ENEMY_REL_VELOCITY_OBS_SIZE];
    }

    public override void OnEpisodeBegin()
    {
        _movement.speedMult = Academy.Instance.EnvironmentParameters.GetWithDefault(
            "enemy_speed_multiplier",
            1.0f
        );

        _prevPlayerTargetDist = Vector2.Distance(
            gm.player.transform.localPosition,
            gm.targetLake.lakeCenter
        );
    }

    public float[] GetRelativePositionData(Vector2 targetWorldPosition)
    {
        Vector2 selfPos = transform.localPosition;
        Vector2 toTargetWorld = targetWorldPosition - selfPos;
        float distance = toTargetWorld.magnitude;
        Vector2 localDir = transform.InverseTransformDirection(toTargetWorld.normalized);
        return new float[] { localDir.x, localDir.y, distance };
    }

    public float[] GetRelativeVelocityData()
    {
        Vector2 playerVel = gm.player.GetComponent<ShipMovement>().Velocity;
        Vector2 worldRelativeVel = playerVel - _movement.Velocity;
        Vector3 localRelativeVel = transform.InverseTransformDirection(worldRelativeVel);
        return new float[] { localRelativeVel.x, localRelativeVel.y };
    }

    private void AddArray(VectorSensor sensor, float[] values)
    {
        for (int i = 0; i < values.Length; i++)
            sensor.AddObservation(values[i]);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Normalizer.NormalizeEnemyController(_controller.GetSensors(), _nControllerSensors);
        Normalizer.NormalizeRelativePositionData(
            GetRelativePositionData(gm.player.transform.localPosition),
            _nPlayerSensors
        );
        Normalizer.NormalizeRelativePositionData(
            GetRelativePositionData(gm.targetLake.lakeCenter),
            _nTargetSensors
        );
        Normalizer.NormalizeRelativeVelocity(GetRelativeVelocityData(), _nRelVelSensors);

        AddArray(sensor, _nControllerSensors);
        AddArray(sensor, _nPlayerSensors);
        AddArray(sensor, _nTargetSensors);
        AddArray(sensor, _nRelVelSensors);
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
            AddReward(-0.005f);
        }

        PlayerDistanceReward();
        TargetPlayerTriangleReward();

        AddReward(-0.0002f);
    }

    public void PlayerDistanceReward()
    {
        Vector3 playerLocalPos = gm.player.transform.localPosition;
        float currDist = Vector2.Distance(playerLocalPos, gm.targetLake.lakeCenter);

        float playerTargetDelta = _prevPlayerTargetDist - currDist;
        float distPE = Vector2.Distance(playerLocalPos, transform.localPosition);

        if (distPE < 30f)
        {
            AddReward(-playerTargetDelta * 0.003f);
        }

        _prevPlayerTargetDist = currDist;
    }

    public void TargetPlayerTriangleReward()
    {
        Vector2 p = gm.player.transform.localPosition;
        Vector2 e = transform.localPosition;
        Vector2 t = gm.targetLake.lakeCenter;

        Vector2 pt = (t - p).normalized;
        Vector2 pe = (e - p).normalized;

        float alignment = Vector2.Dot(pt, pe);
        float distPE = Vector2.Distance(p, e);

        if (distPE < 15f)
        {
            float distWeight = 1f - Mathf.Clamp01(distPE / 15f);
            alignment = Mathf.Clamp01(alignment);
            AddReward(alignment * distWeight * 0.003f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;

        cont[0] = Keyboard.current.wKey.isPressed ? 1 : 0;
        cont[0] = Keyboard.current.sKey.isPressed ? -1 : cont[0];

        cont[1] = Keyboard.current.aKey.isPressed ? -1 : 0;
        cont[1] = Keyboard.current.dKey.isPressed ? 1 : cont[1];

        if (Keyboard.current.spaceKey.isPressed)
        {
            float[] playerDirection = GetRelativePositionData(gm.player.transform.localPosition);
            float[] targetDirection = GetRelativePositionData(gm.targetLake.lakeCenter);
            float[] relVelocity = GetRelativeVelocityData();
            Debug.Log($"playerDirection: [{string.Join(", ", relVelocity)}]");
        }
    }
}
