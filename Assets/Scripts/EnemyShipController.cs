using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(ShipMovement))]
[RequireComponent(typeof(RadarDetector))]
public class EnemyShipController : MonoBehaviour
{
    public GameObject minePrefab;

    public int initialMineCount = 10;
    public float mineCooldownTime = 5f;

    public float mineLaunchOffset = 5.0f;
    private float mineLaunchSpeed = 30f;

    private int _currentMineCount;
    private float _mineCooldownTimer;

    private ShipMovement _shipMovement;
    private ShipRaycast _shipRaycast;

    void Awake()
    {
        _shipMovement = GetComponent<ShipMovement>();
        _shipRaycast = GetComponent<ShipRaycast>();

        _currentMineCount = initialMineCount;
        _mineCooldownTimer = 0f;
    }

    public void Reset(Vector2 spawnCoorinates)
    {
        gameObject.SetActive(true);
        _shipMovement.ResetMovement();
        transform.eulerAngles = Vector3.zero;
        _currentMineCount = initialMineCount;
        _mineCooldownTimer = 0f;
        transform.localPosition = new Vector3(spawnCoorinates.x, spawnCoorinates.y, 0f);
    }

    void Update()
    {
        _mineCooldownTimer -= Time.deltaTime;
    }

    public void LaunchMine(Vector2 direction, EnemyAgent owner)
    {
        if (_currentMineCount == 0 || _mineCooldownTimer > 0f)
            return;

        if (direction.sqrMagnitude < 0.01f)
            direction = transform.up;
        direction.Normalize();
        Vector3 launchPosition = transform.position + (Vector3)direction * mineLaunchOffset;
        GameObject mineInstance = Instantiate(minePrefab, launchPosition, Quaternion.identity);
        MineController mineController = mineInstance.GetComponent<MineController>();

        if (mineController != null)
        {
            Vector2 initialVelocity = direction * mineLaunchSpeed;
            mineController.SetInitialVelocity(initialVelocity);
            mineController.SetOwner(owner);
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

        float[] mineInputs = new float[] { _currentMineCount, _mineCooldownTimer };

        float[] inputVector = new float[
            shipInputs.Length + raycastInputs.Length + mineInputs.Length
        ];

        int offset = 0;
        shipInputs.CopyTo(inputVector, offset);
        offset += shipInputs.Length;

        raycastInputs.CopyTo(inputVector, offset);
        offset += raycastInputs.Length;

        mineInputs.CopyTo(inputVector, offset);
        offset += mineInputs.Length;

        return inputVector;
    }
}
