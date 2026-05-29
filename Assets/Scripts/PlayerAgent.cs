using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ShipController))]
[RequireComponent(typeof(ShipMovement))]
[RequireComponent(typeof(ShipRaycast))]
public class PlayerAgent : Agent
{
    private ShipController _controller;
    private ShipMovement _movement;

    float _prevTargetDist;

    public Phase gm;

    float _progressAccum;
    public bool IsDead { get; private set; }
    private float[] _cachedSensors;

    public void Kill()
    {
        IsDead = true;
    }

    public override void Initialize()
    {
        _controller = GetComponent<ShipController>();
        _movement = GetComponent<ShipMovement>();
    }

    public override void OnEpisodeBegin()
    {
        _prevTargetDist = Vector2.Distance(transform.localPosition, gm.targetLake.lakeCenter);
        _progressAccum = 0f;
        IsDead = false;
    }

    public float[] GetRelativePositionData(Vector2 targetWorldPosition)
    {
        Vector2 selfPos = transform.localPosition;
        Vector2 toTargetWorld = targetWorldPosition - selfPos;
        float distance = toTargetWorld.magnitude;
        Vector2 localDir = transform.InverseTransformDirection(toTargetWorld.normalized);
        return new float[] { localDir.x, localDir.y, distance / 283f };
    }

    private void AddArray(VectorSensor sensor, float[] values)
    {
        for (int i = 0; i < values.Length; i++)
            sensor.AddObservation(values[i]);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        _cachedSensors = _controller.GetSensors();
        AddArray(sensor, _cachedSensors);
        AddArray(sensor, GetRelativePositionData(gm.targetLake.lakeCenter));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float throttle = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float rudder = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        _movement.SetInput(throttle, rudder);

        float aimX = Mathf.Clamp(actions.ContinuousActions[2], -1f, 1f);
        float aimY = Mathf.Clamp(actions.ContinuousActions[3], -1f, 1f);
        bool fire = actions.DiscreteActions[0] == 1;

        Vector2 aimDir = new Vector2(aimX, aimY);
        if (fire && aimDir.sqrMagnitude > 0.01f)
        {
            _controller.TryFire(aimDir.normalized, gm.MapGenerator, gm.mapEnvironment);
            AddReward(-0.01f);
        }

        TargetProgressReward();
        WallProximityPenalty();

        AddReward(-0.001f);
    }

    void TargetProgressReward()
    {
        float curr = Vector2.Distance(transform.localPosition, gm.targetLake.lakeCenter);
        float delta = _prevTargetDist - curr;
        float r = delta * 0.005f;
        if (_progressAccum + r > 0.3f)
            r = Mathf.Max(0f, 0.3f - _progressAccum);
        _progressAccum += r;
        AddReward(r);
        _prevTargetDist = curr;
    }

    void WallProximityPenalty()
    {
        if (_cachedSensors == null)
            return;
        float minDist = float.MaxValue;
        foreach (float s in _cachedSensors)
            minDist = Mathf.Min(minDist, s);

        if (minDist < 0.2f)
            AddReward(-0.002f * (0.2f - minDist) / 0.2f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;
        var disc = actionsOut.DiscreteActions;

        cont[0] = Keyboard.current.upArrowKey.isPressed ? 1 : 0;
        cont[0] = Keyboard.current.downArrowKey.isPressed ? -1 : cont[0];

        cont[1] = Keyboard.current.leftArrowKey.isPressed ? -1 : 0;
        cont[1] = Keyboard.current.rightArrowKey.isPressed ? 1 : cont[1];

        Vector2 mouseScreen = Mouse.current.position.ReadValue();
        float vx = mouseScreen.x / Screen.width;
        float vy = mouseScreen.y / Screen.height;
        cont[2] = vx * 2f - 1f;
        cont[3] = vy * 2f - 1f;

        disc[0] = Keyboard.current.spaceKey.isPressed ? 1 : 0;
    }
}
