using UnityEngine;

public class MineController : MonoBehaviour
{
    private float ExplosionDelay = 5.0f;
    private float ExplosionRadius = 5f;
    public float MineDrag = 0.5f;
    public LayerMask ObstacleMask;

    private float MaxTravelDistance = 20f;

    private float _explosionTimer;
    private Vector2 _velocity;
    private EnemyAgent owner;

    public Vector2 Velocity => _velocity;

    private Vector3 _spawnPosition;
    private bool _reachedMaxRange = false;

    private SpriteRenderer _rangeRenderer;

    void Start()
    {
        _explosionTimer = ExplosionDelay;
        _spawnPosition = transform.position;

        _rangeRenderer = GetComponent<SpriteRenderer>();
        if (_rangeRenderer != null)
        {
            float diameter = ExplosionRadius * 2f;
            transform.localScale = new Vector3(diameter, diameter, 1f);
            _rangeRenderer.color = new Color(1f, 0.2f, 0f, 0.5f);
        }
    }

    public void SetInitialVelocity(Vector2 initialVelocity)
    {
        _velocity = initialVelocity;
    }

    public void SetOwner(EnemyAgent _owner)
    {
        owner = _owner;
    }

    public float TimeToExplosion01 => Mathf.Clamp01(_explosionTimer / ExplosionDelay);

    void Update()
    {
        float dt = Time.deltaTime;

        if (CheckShipCollision())
        {
            Explode();
            return;
        }

        _explosionTimer -= dt;
        if (_explosionTimer <= 0f)
        {
            Explode();
            return;
        }

        if (CheckWallHit())
        {
            _velocity = Vector2.zero;
            _reachedMaxRange = true;
        }

        if (!_reachedMaxRange)
        {
            float traveled = Vector3.Distance(transform.position, _spawnPosition);
            if (traveled >= MaxTravelDistance)
            {
                _velocity = Vector2.zero;
                _reachedMaxRange = true;
            }
        }

        if (!_reachedMaxRange)
        {
            _velocity = Vector2.Lerp(_velocity, Vector2.zero, MineDrag * dt);
            // _velocity *= Mathf.Exp(-MineDrag * dt);

            transform.position += (Vector3)_velocity * dt;
        }
    }

    private bool CheckWallHit()
    {
        if (_velocity.sqrMagnitude < 0.001f)
            return false;

        float distance = _velocity.magnitude * Time.deltaTime + 0.1f;

        RaycastHit2D hit = Physics2D.Raycast(
            transform.position,
            _velocity.normalized,
            distance,
            ObstacleMask
        );

        return hit.collider != null;
    }

    private bool CheckShipCollision()
    {
        Collider2D[] objects = Physics2D.OverlapCircleAll(transform.position, ExplosionRadius);

        foreach (Collider2D col in objects)
        {
            if (col.CompareTag("PlayerShip"))
                return true;
        }

        return false;
    }

    private void Explode()
    {
        Collider2D[] objectsInRange = Physics2D.OverlapCircleAll(
            transform.position,
            ExplosionRadius
        );

        owner.EvaluateMine(this);

        foreach (Collider2D col in objectsInRange)
        {
            if (col.CompareTag("PlayerShip"))
            {
                var playerShip = col.GetComponent<PlayerShipController>();
                playerShip.hitByMine = true;
            }
        }

        Destroy(gameObject);
    }
}
