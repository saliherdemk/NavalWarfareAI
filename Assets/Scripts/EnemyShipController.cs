using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ShipMovement))]
[RequireComponent(typeof(RadarDetector))]
public class EnemyShipController : MonoBehaviour
{
    public bool hitByMine = false;

    public GameObject minePrefab;

    public int initialMineCount = 10;

    public float mineCooldownTime = 5f;

    public float mineLaunchOffset = 5.0f;
    public float mineLaunchSpeed = 10f;

    private int _currentMineCount;
    private float _mineCooldownTimer;

    private ShipMovement _shipMovement;
    private ShipRaycast _shipRaycast;
    private RadarDetector _radarDetector;

    private List<MineController> spawnedMines = new List<MineController>();

    void Awake()
    {
        _shipMovement = GetComponent<ShipMovement>();
        _radarDetector = GetComponent<RadarDetector>();
        _shipRaycast = GetComponent<ShipRaycast>();

        _currentMineCount = initialMineCount;
        _mineCooldownTimer = 0f;
    }

    public void Reset(Vector2 spawnCoorinates)
    {
        gameObject.SetActive(true);
        _shipMovement.ResetMovement();
        transform.eulerAngles = Vector3.zero;
        hitByMine = false;
        transform.localPosition = new Vector3(spawnCoorinates.x, spawnCoorinates.y, 0f);
    }

    void Update()
    {
        _mineCooldownTimer -= Time.deltaTime;
    }

    private void ThrowMine()
    {
        if (_mineCooldownTimer <= 0f)
        {
            Vector3 mouseScreenPos = Mouse.current.position.ReadValue();

            Vector3 targetWorldPosition = Camera.main.ScreenToWorldPoint(mouseScreenPos);
            targetWorldPosition.z = 0f;

            LaunchMineAtWorldPoint(targetWorldPosition);
        }
    }

    public void LaunchMineAtWorldPoint(Vector3 targetWorldPoint)
    {
        Vector3 launchDirection = (targetWorldPoint - transform.position).normalized;

        Vector3 launchPosition = transform.position + launchDirection * mineLaunchOffset;

        GameObject mineInstance = Instantiate(minePrefab, launchPosition, Quaternion.identity);

        MineController mineController = mineInstance.GetComponent<MineController>();

        if (mineController != null)
        {
            Vector2 initialVelocity = launchDirection * mineLaunchSpeed;
            mineController.SetOwner(transform);
            mineController.SetInitialVelocity(initialVelocity);
            spawnedMines.Add(mineController);
        }
        else
        {
            Destroy(mineInstance);
            return;
        }

        _currentMineCount--;
        _mineCooldownTimer = mineCooldownTime;
    }

    public float[] GetSensors()
    {
        float[] shipInputs = _shipMovement.GetSensors();
        float[] raycastInputs = _shipRaycast.GetSensors();
        float[] radarInputs = _radarDetector.GetSensors(1);

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
