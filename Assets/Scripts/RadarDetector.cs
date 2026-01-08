using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public struct DetectedTarget
{
    public float Distance;
    public Vector2 LocalDir;
    public float ClosingSpeed;
}

public struct DetectedMine
{
    public float Distance;
    public Vector2 LocalDir;
    public float TimeToExplosion;
}

public class RadarDetector : MonoBehaviour
{
    [Header("Radar Settings")]
    public float Range = 50f;

    public LayerMask TargetMask;
    public LayerMask ObstacleMask;

    [Header("Mine Detection")]
    public LayerMask MineMask;

    public List<DetectedTarget> DetectedTargets { get; private set; } = new();
    public List<DetectedMine> DetectedMines { get; private set; } = new();

    private ShipMovement _shipMovement;

    void Awake()
    {
        _shipMovement = GetComponent<ShipMovement>();
    }

    public void TickRadar()
    {
        DetectTargets();
        DetectMines();
    }

    public bool TryGetClosestTarget(out DetectedTarget target)
    {
        if (DetectedTargets.Count == 0)
        {
            target = default;
            return false;
        }

        target = DetectedTargets.OrderBy(t => t.Distance).First();
        return true;
    }

    public bool TryGetClosestMine(out DetectedMine mine)
    {
        if (DetectedMines.Count == 0)
        {
            mine = default;
            return false;
        }

        mine = DetectedMines.OrderBy(m => m.Distance).First();
        return true;
    }

    private void DetectTargets()
    {
        DetectedTargets.Clear();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Range, TargetMask);

        Vector2 selfPos = transform.position;

        foreach (Collider2D col in hits)
        {
            Transform t = col.transform;
            Vector2 toTarget = (Vector2)t.position - selfPos;
            float dist = toTarget.magnitude;
            if (dist < 0.001f)
                continue;

            Vector2 dirWorld = toTarget / dist;
            Vector2 dirLocal = transform.InverseTransformDirection(dirWorld);

            ShipMovement sm = t.GetComponent<ShipMovement>();
            if (sm == null)
                continue;

            Vector2 relVel = sm.Velocity - _shipMovement.Velocity;
            float closingSpeed = Vector2.Dot(relVel, dirWorld);

            DetectedTargets.Add(
                new DetectedTarget
                {
                    Distance = dist,
                    LocalDir = dirLocal,
                    ClosingSpeed = closingSpeed,
                }
            );
        }
    }

    private void DetectMines()
    {
        DetectedMines.Clear();

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, Range, MineMask);

        Vector2 selfPos = transform.position;

        foreach (Collider2D col in hits)
        {
            Transform m = col.transform;
            Vector2 toMine = (Vector2)m.position - selfPos;
            float dist = toMine.magnitude;
            if (dist < 0.001f)
                continue;

            Vector2 dirWorld = toMine / dist;
            Vector2 dirLocal = transform.InverseTransformDirection(dirWorld);

            MineController mc = col.GetComponent<MineController>();
            if (mc == null)
                continue;

            DetectedMines.Add(
                new DetectedMine
                {
                    Distance = dist,
                    LocalDir = dirLocal,
                    TimeToExplosion = mc.TimeToExplosion01,
                }
            );
        }
    }

    public float[] GetSensors(int maxTargets = 2, int maxMines = 2)
    {
        TickRadar();

        List<float> inputs = new();

        var targets = DetectedTargets.OrderBy(t => t.Distance).Take(maxTargets).ToList();

        for (int i = 0; i < maxTargets; i++)
        {
            if (i < targets.Count)
            {
                var t = targets[i];
                inputs.Add(1f);
                inputs.Add(t.Distance);
                inputs.Add(t.LocalDir.x);
                inputs.Add(t.LocalDir.y);
                inputs.Add(t.ClosingSpeed);
            }
            else
            {
                inputs.AddRange(new float[5]);
            }
        }

        var mines = DetectedMines.OrderBy(m => m.Distance).Take(maxMines).ToList();

        for (int i = 0; i < maxMines; i++)
        {
            if (i < mines.Count)
            {
                var m = mines[i];
                inputs.Add(1f);
                inputs.Add(m.Distance);
                inputs.Add(m.LocalDir.x);
                inputs.Add(m.LocalDir.y);
                inputs.Add(m.TimeToExplosion);
            }
            else
            {
                inputs.AddRange(new float[5]);
            }
        }

        return inputs.ToArray();
    }
}
