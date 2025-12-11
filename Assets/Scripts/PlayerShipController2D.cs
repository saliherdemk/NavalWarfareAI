using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ShipMovement))]
public class PlayerShipController2D : MonoBehaviour
{
    public bool hitByMine = false;

    private ShipMovement _shipMovement;

    private ShipRaycast _shipRaycast;

    private RadarDetector _radarDetector;

    void Awake()
    {
        _shipMovement = GetComponent<ShipMovement>();
        _radarDetector = GetComponent<RadarDetector>();
        _shipRaycast = GetComponent<ShipRaycast>();
    }

    public void Reset(Vector2 spawnCoorinates)
    {
        _shipMovement.ResetMovement();
        transform.eulerAngles = Vector3.zero;
        hitByMine = false;
        transform.localPosition = new Vector3(spawnCoorinates.x, spawnCoorinates.y, 0f);
    }

    void ProcessMovementInput(float targetThrottle, float targetRudder)
    {
        _shipMovement.SetInput(targetThrottle, targetRudder);
    }

    public float[] GetSensors()
    {
        float[] shipInputs = _shipMovement.GetSensors();
        float[] raycastInputs = _shipRaycast.GetSensors();
        float[] radarInputs = _radarDetector.GetSensors(2);

        float[] globalFeatures = new float[]
        {
            _shipMovement.maxSpeed,
            _shipMovement.acceleration,
            _shipMovement.turnSpeed,
        };

        float[] inputVector = new float[
            shipInputs.Length + raycastInputs.Length + radarInputs.Length + globalFeatures.Length
        ];

        int offset = 0;
        shipInputs.CopyTo(inputVector, offset);
        offset += shipInputs.Length;

        raycastInputs.CopyTo(inputVector, offset);
        offset += raycastInputs.Length;

        radarInputs.CopyTo(inputVector, offset);
        offset += radarInputs.Length;

        globalFeatures.CopyTo(inputVector, offset);

        return inputVector;
    }
}
