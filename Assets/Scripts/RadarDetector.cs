using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct DetectedTarget
{
    public float Distance;
    public float RelativeAngle;
    public float ClosingSpeed;
    public bool HasLOS;
    public Vector2 WorldVelocity;
}

public struct DetectedMine
{
    public float Distance;
    public float RelativeAngle;
    public float ClosingSpeed;
}

public class RadarDetector : MonoBehaviour
{
    [Header("Radar Settings")]
    public float Range = 50f;

    public LayerMask TargetMask;

    public LayerMask ObstacleMask;

    [Header("Mine Detection")]
    public LayerMask MineMask;

    public List<DetectedTarget> VisibleTargets { get; private set; } = new List<DetectedTarget>();
    public List<DetectedMine> DetectedMines { get; private set; } = new List<DetectedMine>();

    private ShipMovement _shipMovement;

    void Awake()
    {
        _shipMovement = GetComponent<ShipMovement>();
    }

    private void DetectTargets()
    {
        VisibleTargets.Clear();

        Collider2D[] potentialTargets = Physics2D.OverlapCircleAll(
            transform.position,
            Range,
            TargetMask
        );

        foreach (Collider2D targetCollider in potentialTargets)
        {
            if (targetCollider.transform == transform)
            {
                continue;
            }

            Transform target = targetCollider.transform;
            Vector2 targetPosition = target.position;
            Vector2 selfPosition = transform.position;

            Vector2 directionToTarget = targetPosition - selfPosition;
            float distance = directionToTarget.magnitude;

            bool losClear = CheckLineOfSight(selfPosition, targetPosition, distance);

            float angleToTarget = Vector2.SignedAngle(transform.up, directionToTarget);

            ShipMovement sm = target.GetComponent<ShipMovement>();
            Vector2 relVel = sm.Velocity - _shipMovement.Velocity;

            float closingSpeed = 0f;
            if (distance > 0.001f)
            {
                closingSpeed = Vector2.Dot(relVel, directionToTarget / distance);
            }

            VisibleTargets.Add(
                new DetectedTarget
                {
                    Distance = distance,
                    RelativeAngle = angleToTarget,
                    ClosingSpeed = closingSpeed,
                    HasLOS = losClear,
                    WorldVelocity = sm.Velocity,
                }
            );
        }
    }

    private void DetectMines()
    {
        DetectedMines.Clear();

        Collider2D[] nearbyMines = Physics2D.OverlapCircleAll(transform.position, Range, MineMask);

        foreach (Collider2D mineCollider in nearbyMines)
        {
            Transform target = mineCollider.transform;
            Vector2 targetPosition = target.position;
            Vector2 selfPosition = transform.position;

            Vector2 directionToTarget = targetPosition - selfPosition;
            float distance = directionToTarget.magnitude;

            float angleToTarget = Vector2.SignedAngle(transform.up, directionToTarget);

            MineController mc = mineCollider.GetComponent<MineController>();
            Vector2 relVel = mc.Velocity - _shipMovement.Velocity;

            float closingSpeed = 0f;
            if (distance > 0.001f)
            {
                closingSpeed = Vector2.Dot(relVel, directionToTarget / distance);
            }

            DetectedMines.Add(
                new DetectedMine
                {
                    Distance = distance,
                    RelativeAngle = angleToTarget,
                    ClosingSpeed = closingSpeed,
                }
            );
        }
    }

    private bool CheckLineOfSight(Vector2 origin, Vector2 target, float distance)
    {
        Vector2 direction = (target - origin).normalized;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, distance, ObstacleMask);

        if (hit.collider == null)
        {
            return true;
        }

        return false;
    }

    private void DrawDebugRadar()
    {
        DebugExtension.DrawCircle(transform.position, Range, Color.yellow);

        foreach (var t in VisibleTargets)
        {
            Vector2 dir = Quaternion.Euler(0f, 0f, t.RelativeAngle) * transform.up;
            Vector2 end = (Vector2)transform.position + dir * t.Distance;

            Color color = t.HasLOS ? Color.red : Color.blue;
            Debug.DrawLine(transform.position, end, color);
        }

        foreach (var m in DetectedMines)
        {
            Vector2 dir = Quaternion.Euler(0f, 0f, m.RelativeAngle) * transform.up;
            Vector2 end = (Vector2)transform.position + dir * m.Distance;

            Debug.DrawLine(transform.position, end, Color.green);
        }
    }

    public float[] GetSensors(int enemyCount)
    {
        DetectTargets();
        DetectMines();
        // DrawDebugRadar();
        List<float> inputs = new List<float>();
        Transform self = transform;

        var visibleEnemies = VisibleTargets
            .Where(t => t.HasLOS)
            .OrderBy(t => t.Distance)
            .Take(2)
            .ToList();

        for (int i = 0; i < enemyCount; i++)
        {
            if (i < visibleEnemies.Count)
            {
                var e = visibleEnemies[i];

                Vector2 localVel = transform.InverseTransformVector(e.WorldVelocity);

                inputs.Add(e.Distance);
                inputs.Add(e.RelativeAngle);
                inputs.Add(e.ClosingSpeed);
                inputs.Add(localVel.x);
                inputs.Add(localVel.y);
            }
            else
            {
                inputs.Add(0f);
                inputs.Add(0f);
                inputs.Add(0f);
                inputs.Add(0f);
                inputs.Add(0f);
            }
        }

        var closestMines = DetectedMines.OrderBy(m => m.Distance).Take(2).ToList();

        for (int i = 0; i < 2; i++)
        {
            if (i < closestMines.Count)
            {
                var m = closestMines[i];

                inputs.Add(m.Distance);
                inputs.Add(m.RelativeAngle);
                inputs.Add(m.ClosingSpeed);
            }
            else
            {
                inputs.Add(0f);
                inputs.Add(0f);
                inputs.Add(0f);
            }
        }

        return inputs.ToArray();
    }
}

public static class DebugExtension
{
    public static void DrawCircle(Vector3 position, float radius, Color color, float duration = 0f)
    {
        int segments = 32;
        Vector3 previousPoint = position + new Vector3(radius, 0, 0);
        for (int i = 1; i <= segments; i++)
        {
            float angle = i * 360f / segments;
            Vector3 newPoint =
                position
                + new Vector3(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                    0
                );
            Debug.DrawLine(previousPoint, newPoint, color, duration);
            previousPoint = newPoint;
        }
    }
}
