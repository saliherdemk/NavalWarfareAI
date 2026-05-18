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

    public override void Initialize()
    {
        _controller = GetComponent<ShipController>();
        _movement = GetComponent<ShipMovement>();
    }

    public override void OnEpisodeBegin()
    {
        gm.RestartGame();

        _prevTargetDist = Vector2.Distance(transform.localPosition, gm.targetLake.lakeCenter);
        _progressAccum = 0f;
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
        AddArray(sensor, _controller.GetSensors());
        AddArray(sensor, GetRelativePositionData(gm.targetLake.lakeCenter));
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float throttle = Mathf.Clamp(actions.ContinuousActions[0], -1f, 1f);
        float rudder = Mathf.Clamp(actions.ContinuousActions[1], -1f, 1f);

        _movement.SetInput(throttle, rudder);

        TargetProgressReward();

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

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var cont = actionsOut.ContinuousActions;

        cont[0] = Keyboard.current.upArrowKey.isPressed ? 1 : 0;
        cont[0] = Keyboard.current.downArrowKey.isPressed ? -1 : cont[0];

        cont[1] = Keyboard.current.leftArrowKey.isPressed ? -1 : 0;
        cont[1] = Keyboard.current.rightArrowKey.isPressed ? 1 : cont[1];
    }
}
