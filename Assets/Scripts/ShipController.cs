using UnityEngine;

[RequireComponent(typeof(ShipMovement))]
public class ShipController : MonoBehaviour
{
    private ShipMovement _shipMovement;

    private ShipRaycast _shipRaycast;

    public RadarDetector _radarDetector;

    public GameObject bulletPrefab;

    private float _lastFireTime = -3f;
    private const float FIRE_COOLDOWN = 3f;

    void Awake()
    {
        _shipMovement = GetComponent<ShipMovement>();
        _radarDetector = GetComponent<RadarDetector>();
        _shipRaycast = GetComponent<ShipRaycast>();
    }

    public void Reset(Vector2 spawnCoorinates)
    {
        _shipMovement.ResetMovement();
        _lastFireTime = -3f;
        transform.eulerAngles = Vector3.zero;
        transform.localPosition = new Vector3(spawnCoorinates.x, spawnCoorinates.y, 0f);
    }

    void ProcessMovementInput(float targetThrottle, float targetRudder)
    {
        _shipMovement.SetInput(targetThrottle, targetRudder);
    }

    public bool TryFire(Vector2 aimDir, RoadGraphGenerator2D mapGen, Transform parent)
    {
        if (Time.time - _lastFireTime < FIRE_COOLDOWN)
            return false;

        _lastFireTime = Time.time;

        GameObject bulletGO = Instantiate(bulletPrefab, parent);
        bulletGO.transform.localPosition = transform.localPosition + (Vector3)aimDir * 3f;
        Bullet bullet = bulletGO.AddComponent<Bullet>();
        bullet.isPlayerBullet = true;
        bullet.Initialize(aimDir, mapGen);

        return true;
    }

    public float GetCooldownStatus()
    {
        float elapsed = Time.time - _lastFireTime;
        return Mathf.Clamp01(1f - elapsed / FIRE_COOLDOWN);
    }

    public float[] GetSensors()
    {
        float[] shipInputs = _shipMovement.GetSensors();
        float[] raycastInputs = _shipRaycast.GetSensors();

        float[] inputVector = new float[
            shipInputs.Length + raycastInputs.Length + RadarDetector.RADAR_OBS_SIZE + 1
        ];

        int offset = 0;
        shipInputs.CopyTo(inputVector, offset);
        offset += shipInputs.Length;

        raycastInputs.CopyTo(inputVector, offset);
        offset += raycastInputs.Length;

        _radarDetector.WriteSensors(inputVector, ref offset);

        inputVector[offset] = GetCooldownStatus();

        return inputVector;
    }
}
