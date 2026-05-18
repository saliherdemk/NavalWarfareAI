using UnityEngine;

[RequireComponent(typeof(ShipMovement))]
public class ShipController : MonoBehaviour
{
    private ShipMovement _shipMovement;

    private ShipRaycast _shipRaycast;

    public RadarDetector _radarDetector;

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

        float[] inputVector = new float[
            shipInputs.Length + raycastInputs.Length + RadarDetector.RADAR_OBS_SIZE
        ];

        int offset = 0;
        shipInputs.CopyTo(inputVector, offset);
        offset += shipInputs.Length;

        raycastInputs.CopyTo(inputVector, offset);
        offset += raycastInputs.Length;

        _radarDetector.WriteSensors(inputVector, ref offset);

        return inputVector;
    }
}
