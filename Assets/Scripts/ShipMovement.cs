using UnityEngine;

public class ShipMovement : MonoBehaviour
{
    public float maxSpeed = 6f;
    public float acceleration = 3f;
    public float deceleration = 1.5f;
    public float turnSpeed = 60f;
    public float rudderResponsiveness = 2f;
    public float forwardDrag = 0.2f;
    public float sidewaysDrag = 3.0f;

    private float _targetThrottle = 0f;
    private float _targetRudder = 0f;

    private float _throttle = 0f;
    private float _rudder = 0f;
    private Vector2 _velocity;

    public Vector2 Velocity => _velocity;

    public void SetInput(float targetThrottle, float targetRudder)
    {
        _targetThrottle = targetThrottle;
        _targetRudder = targetRudder;
    }

    public void ResetMovement()
    {
        _velocity = Vector2.zero;
        _throttle = 0f;
        _rudder = 0f;
        _targetThrottle = 0f;
        _targetRudder = 0f;

        transform.rotation = Quaternion.identity;
    }

    void FixedUpdate()
    {
        float deltaTime = Time.deltaTime;

        _throttle = Mathf.Lerp(_throttle, _targetThrottle, deltaTime * 1.5f);
        _rudder = Mathf.Lerp(_rudder, _targetRudder, deltaTime * rudderResponsiveness);

        Vector2 forward = transform.up;
        Vector2 right = transform.right;

        float forwardVel = Vector2.Dot(_velocity, forward);
        float sidewaysVel = Vector2.Dot(_velocity, right);

        if (_throttle != 0)
        {
            forwardVel += _throttle * acceleration * deltaTime;
        }
        else
        {
            forwardVel = Mathf.Lerp(forwardVel, 0f, deceleration * deltaTime);
        }

        forwardVel = Mathf.Lerp(forwardVel, 0f, forwardDrag * deltaTime);
        sidewaysVel = Mathf.Lerp(sidewaysVel, 0f, sidewaysDrag * deltaTime);

        _velocity = forwardVel * forward + sidewaysVel * right;

        if (_velocity.magnitude > maxSpeed)
        {
            _velocity = _velocity.normalized * maxSpeed;
        }

        transform.position += (Vector3)_velocity * deltaTime;

        float rotationDirection = Mathf.Sign(forwardVel);

        if (Mathf.Abs(forwardVel) < 0.1f)
            rotationDirection = 0f;

        float speedFactor = Mathf.Clamp01(_velocity.magnitude / maxSpeed);

        float rotationAmount = -_rudder * turnSpeed * speedFactor * rotationDirection * deltaTime;

        transform.Rotate(0f, 0f, rotationAmount);
    }

    public float[] GetSensors()
    {
        float shipX = transform.position.x;
        float shipY = transform.position.y;

        float shipRotation = transform.eulerAngles.z;
        float shipSpeed = _velocity.magnitude;

        return new float[] { shipX, shipY, shipRotation, shipSpeed };
    }
}
