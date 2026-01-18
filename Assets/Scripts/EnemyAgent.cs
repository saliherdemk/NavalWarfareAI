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

    private float[] _nControllerSensors;
    private float[] _nPlayerSensors;
    private float[] _nTargetSensors;
    private float[] _nRelVelSensors;

    private float _prevPlayerTargetDist;

    public override void Initialize()
    {
        _controller = GetComponent<EnemyShipController>();
        _movement = GetComponent<ShipMovement>();

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

    public void EvaluateMine(MineController mine)
    {
        float distance = Vector2.Distance(
            mine.transform.localPosition,
            gm.player.transform.localPosition
        );

        float normalizedDistance = Mathf.Clamp01(distance / 30f);
        float closeness = 1f - normalizedDistance;
        AddReward(closeness * 0.5f);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float targetThrottle = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float targetRudder = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);
        float mineAimX = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);
        float mineAimY = Mathf.Clamp(actions.ContinuousActions[3], -1f, 1f);
        int fireSignal = actions.DiscreteActions[0];

        _movement.SetInput(targetThrottle, targetRudder);

        float currDist = Vector2.Distance(
            gm.player.transform.localPosition,
            gm.targetLake.lakeCenter
        );

        float delta = currDist - _prevPlayerTargetDist;

        delta = Mathf.Clamp(delta, -0.01f, 0.01f);

        _prevPlayerTargetDist = currDist;

        if (fireSignal == 1)
        {
            _controller.LaunchMine(new Vector2(mineAimX, mineAimY), this);
        }
        else
        {
            AddReward(delta * 0.02f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        var disc = actionsOut.DiscreteActions;

        cont[0] = 0f;
        if (Keyboard.current.wKey.isPressed)
            cont[0] = 1f;
        if (Keyboard.current.sKey.isPressed)
            cont[0] = -1f;

        cont[1] = 0f;
        if (Keyboard.current.aKey.isPressed)
            cont[1] = -1f;
        if (Keyboard.current.dKey.isPressed)
            cont[1] = 1f;

        disc[0] = 0;
        cont[2] = 0f;
        cont[3] = 0f;

        if (Keyboard.current.spaceKey.isPressed)
        {
            Vector2 toPlayer = gm.player.transform.localPosition - transform.localPosition;

            toPlayer.Normalize();

            cont[2] = toPlayer.x;
            cont[3] = toPlayer.y;
            disc[0] = 1;
        }
    }
}
