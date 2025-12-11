using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct DetectedTarget
{
    public Transform TargetTransform;
    public float Distance;
    public float RelativeAngle;
    public bool HasLOS;
    public Vector2 Velocity;
}

public struct DetectedMine
{
    public Transform MineTransform;
    public float Distance;
    public float RelativeAngle;
    public Vector2 Velocity;
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

    private void Update()
    {
        DetectTargets();
        DetectMines();
        DrawDebugRadar();
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
            Vector2 vel = sm.Velocity;

            VisibleTargets.Add(
                new DetectedTarget
                {
                    TargetTransform = target,
                    Distance = distance,
                    RelativeAngle = angleToTarget,
                    HasLOS = losClear,
                    Velocity = vel,
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
            Vector2 vel = mc.Velocity;

            DetectedMines.Add(
                new DetectedMine
                {
                    MineTransform = target,
                    Distance = distance,
                    RelativeAngle = angleToTarget,
                    Velocity = vel,
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

        foreach (var targetData in VisibleTargets)
        {
            Color debugColor = targetData.HasLOS ? Color.red : Color.blue;

            Debug.DrawLine(transform.position, targetData.TargetTransform.position, debugColor);
        }

        foreach (var mineData in DetectedMines)
        {
            Color mineColor = Color.green;

            Debug.DrawLine(transform.position, mineData.MineTransform.position, mineColor);
        }
    }

    public float[] GetSensors(int enemyCount)
    {
        List<float> inputs = new List<float>();
        Transform self = transform;

        var visibleEnemies = VisibleTargets
            .Where(t => t.HasLOS)
            .OrderBy(t => t.Distance)
            .Take(3)
            .ToList();

        for (int i = 0; i < enemyCount; i++)
        {
            if (i < visibleEnemies.Count)
            {
                var e = visibleEnemies[i];

                float forwardSpeed = Vector2.Dot(e.Velocity, transform.up);
                float sidewaysSpeed = Vector2.Dot(e.Velocity, transform.right);

                inputs.Add(e.Distance);
                inputs.Add(e.RelativeAngle);
                inputs.Add(forwardSpeed);
                inputs.Add(sidewaysSpeed);
            }
            else
            {
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

                float forwardSpeed = Vector2.Dot(m.Velocity, transform.up);
                float sidewaysSpeed = Vector2.Dot(m.Velocity, transform.right);

                inputs.Add(m.Distance);
                inputs.Add(m.RelativeAngle);
                inputs.Add(forwardSpeed);
                inputs.Add(sidewaysSpeed);
            }
            else
            {
                inputs.Add(0f);
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
