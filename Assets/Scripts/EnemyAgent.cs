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
    private int currentPhase;

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

        float phaseValue = Academy.Instance.EnvironmentParameters.GetWithDefault(
            "training_phase",
            0f
        );

        int d = (int)Academy.Instance.EnvironmentParameters.GetWithDefault("enemy_difficulty", 0);

        int currentPhase =
            d <= 3 ? 0
            : d <= 6 ? 1
            : 2;

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

        switch (currentPhase)
        {
            case 0:
                BasicPursuitRewards();
                if (fireSignal == 1)
                {
                    _controller.LaunchMine(new Vector2(mineAimX, mineAimY));
                    AddReward(-0.01f);
                }
                break;

            case 1:
                PathInterceptionRewards();
                if (fireSignal == 1)
                {
                    _controller.LaunchMine(new Vector2(mineAimX, mineAimY));
                }
                break;

            case 2:
                AdvancedTacticsRewards(fireSignal == 1);
                if (fireSignal == 1)
                {
                    _controller.LaunchMine(new Vector2(mineAimX, mineAimY));
                }
                break;
        }

        AddReward(-0.00005f);
    }

    private void BasicPursuitRewards()
    {
        Vector2 playerPos = gm.player.transform.localPosition;
        Vector2 enemyPos = transform.localPosition;
        float distToPlayer = Vector2.Distance(playerPos, enemyPos);

        if (distToPlayer < 50f)
        {
            float proximityReward = (50f - distToPlayer) / 50f;
            AddReward(proximityReward * 0.005f);
        }

        Vector2 toPlayer = (playerPos - enemyPos).normalized;
        float velocityAlignment = Vector2.Dot(_movement.Velocity.normalized, toPlayer);
        if (velocityAlignment > 0)
        {
            AddReward(velocityAlignment * 0.002f);
        }
    }

    private void PathInterceptionRewards()
    {
        Vector2 p = gm.player.transform.localPosition;
        Vector2 e = transform.localPosition;
        Vector2 t = gm.targetLake.lakeCenter;

        Vector2 playerToTarget = t - p;
        float distPlayerToTarget = playerToTarget.magnitude;

        if (distPlayerToTarget > 5f)
        {
            Vector2 playerToEnemy = e - p;
            float projection = Vector2.Dot(playerToEnemy, playerToTarget.normalized);

            if (projection > 0 && projection < distPlayerToTarget)
            {
                Vector2 perpendicular = playerToEnemy - (playerToTarget.normalized * projection);
                float lateralDist = perpendicular.magnitude;

                if (lateralDist < 25f)
                {
                    float pathAlignment = 1f - (lateralDist / 25f);
                    float progressAlongPath = projection / distPlayerToTarget;

                    AddReward(pathAlignment * progressAlongPath * 0.015f);
                }
            }
        }

        float distPE = Vector2.Distance(p, e);
        if (distPE < 30f)
        {
            AddReward((30f - distPE) / 30f * 0.002f);
        }

        Vector2 playerVel = gm.player.GetComponent<ShipMovement>().Velocity;
        if (playerVel.magnitude > 0.1f)
        {
            Vector2 playerToEnemy = e - p;
            float behindAlignment = Vector2.Dot(playerToEnemy.normalized, -playerVel.normalized);

            if (behindAlignment > 0.7f && distPE > 15f)
            {
                AddReward(-0.003f);
            }
        }
    }

    private void AdvancedTacticsRewards(bool justFiredMine)
    {
        Vector2 p = gm.player.transform.localPosition;
        Vector2 e = transform.localPosition;
        Vector2 t = gm.targetLake.lakeCenter;

        Vector2 playerVel = gm.player.GetComponent<ShipMovement>().Velocity;
        Vector2 enemyVel = _movement.Velocity;

        float distPE = Vector2.Distance(p, e);

        if (playerVel.magnitude > 0.5f)
        {
            Vector2 predictedPlayerPos = p + playerVel * 3f;
            Vector2 toInterceptPoint = predictedPlayerPos - e;

            float interceptAlignment = Vector2.Dot(
                enemyVel.normalized,
                toInterceptPoint.normalized
            );
            if (interceptAlignment > 0.5f && distPE < 50f && distPE > 10f)
            {
                AddReward(interceptAlignment * 0.008f);
            }
        }

        if (justFiredMine)
        {
            if (distPE < 25f && distPE > 8f)
            {
                Vector2 playerToEnemy = (e - p).normalized;
                float approachAlignment = Vector2.Dot(playerVel.normalized, playerToEnemy);

                if (approachAlignment > 0.3f)
                {
                    AddReward(0.05f);
                }
                else
                {
                    AddReward(-0.02f);
                }
            }
            else
            {
                AddReward(-0.01f);
            }
        }

        Vector2 playerToTarget = t - p;
        float distPlayerToTarget = playerToTarget.magnitude;

        if (distPlayerToTarget > 5f)
        {
            Vector2 playerToEnemy = e - p;
            float projection = Vector2.Dot(playerToEnemy, playerToTarget.normalized);

            if (projection > 0 && projection < distPlayerToTarget)
            {
                Vector2 perpendicular = playerToEnemy - (playerToTarget.normalized * projection);
                float lateralDist = perpendicular.magnitude;

                if (lateralDist < 20f)
                {
                    float pathAlignment = 1f - (lateralDist / 20f);
                    float progressAlongPath = projection / distPlayerToTarget;
                    AddReward(pathAlignment * progressAlongPath * 0.008f);
                }
            }
        }

        float enemyDistToTarget = Vector2.Distance(e, t);
        float playerDistToTarget = Vector2.Distance(p, t);

        if (enemyDistToTarget < playerDistToTarget && distPE < 40f)
        {
            float shortcutAdvantage = (playerDistToTarget - enemyDistToTarget) / playerDistToTarget;
            AddReward(shortcutAdvantage * 0.005f);
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
