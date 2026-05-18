using UnityEngine;

public struct DetectedShip
{
    public float Distance;
    public Vector2 LocalDir;
    public float ClosingSpeed;
}

public class RadarDetector : MonoBehaviour
{
    public const int TARGET_SLOTS = 2;
    public const int FRIEND_SLOTS = 1;
    public const int RADAR_OBS_SIZE = (TARGET_SLOTS + FRIEND_SLOTS) * 5;

    [Header("Radar Settings")]
    public float Range = 50f;

    public LayerMask TargetMask;
    public LayerMask FriendMask;

    private LayerMask _combinedMask;
    private static readonly Collider2D[] _hits = new Collider2D[8];

    public DetectedShip?[] Targets { get; } = new DetectedShip?[TARGET_SLOTS];
    public DetectedShip? Friend { get; private set; }

    private ShipMovement _shipMovement;

    void Awake()
    {
        _shipMovement = GetComponent<ShipMovement>();
        _combinedMask = TargetMask | FriendMask;
    }

    public void TickRadar()
    {
        for (int i = 0; i < Targets.Length; i++)
            Targets[i] = null;
        Friend = null;

        Vector2 selfPos = transform.position;
        Vector2 up = transform.up;
        Vector2 right = transform.right;
        Vector2 selfVel = _shipMovement.Velocity;

        int count = Physics2D.OverlapCircleNonAlloc(selfPos, Range, _hits, _combinedMask);

        int targetWritten = 0;
        bool friendFound = false;

        for (int i = 0; i < count; i++)
        {
            GameObject go = _hits[i].gameObject;
            if (go == gameObject)
                continue;

            ShipMovement sm = go.GetComponent<ShipMovement>();
            if (sm == null)
                continue;

            Vector2 toShip = (Vector2)go.transform.position - selfPos;
            float shipDist = toShip.magnitude;
            if (shipDist < 0.001f)
                continue;

            Vector2 dirWorld = toShip / shipDist;
            float closingSpeed = Vector2.Dot(sm.Velocity - selfVel, dirWorld);

            DetectedShip ship = new DetectedShip
            {
                Distance = shipDist,
                LocalDir = new Vector2(
                    dirWorld.x * right.x + dirWorld.y * right.y,
                    dirWorld.x * up.x + dirWorld.y * up.y
                ),
                ClosingSpeed = closingSpeed,
            };

            if (((1 << go.layer) & TargetMask.value) != 0 && targetWritten < Targets.Length)
                Targets[targetWritten++] = ship;
            else if (!friendFound && ((1 << go.layer) & FriendMask.value) != 0)
            {
                Friend = ship;
                friendFound = true;
            }
        }
    }

    public bool TryGetClosestTarget(out DetectedShip target)
    {
        return TryGetClosest(Targets, out target);
    }

    public bool TryGetClosestFriend(out DetectedShip friend)
    {
        if (Friend.HasValue)
        {
            friend = Friend.Value;
            return true;
        }
        friend = default;
        return false;
    }

    private static bool TryGetClosest(DetectedShip?[] slots, out DetectedShip result)
    {
        result = default;
        bool found = false;
        float best = float.MaxValue;

        for (int i = 0; i < slots.Length; i++)
        {
            if (!slots[i].HasValue)
                continue;
            float d = slots[i].Value.Distance;
            if (d < best)
            {
                best = d;
                result = slots[i].Value;
                found = true;
            }
        }
        return found;
    }

    public void WriteSensors(float[] buffer, ref int offset)
    {
        TickRadar();

        WriteShipSlots(buffer, ref offset, Targets);
        WriteShipSlot(buffer, ref offset, Friend);
    }

    private void WriteShipSlots(float[] buffer, ref int offset, DetectedShip?[] slots)
    {
        foreach (var slot in slots)
            WriteShipSlot(buffer, ref offset, slot);
    }

    private void WriteShipSlot(float[] buffer, ref int offset, DetectedShip? slot)
    {
        if (slot.HasValue)
        {
            var s = slot.Value;
            buffer[offset++] = 1f;
            buffer[offset++] = s.Distance / Range;
            buffer[offset++] = s.LocalDir.x;
            buffer[offset++] = s.LocalDir.y;
            buffer[offset++] = s.ClosingSpeed / _shipMovement.maxSpeed;
        }
        else
        {
            offset += 5;
        }
    }
}
