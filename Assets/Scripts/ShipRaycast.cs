using UnityEngine;

public class ShipRaycast : MonoBehaviour
{
    private int rayCount = 16;
    private float rayDistance = 16f;
    public LayerMask detectMask;

    private float[] rays;

    void Awake()
    {
        rays = new float[rayCount];
    }

    void CastCircleRays()
    {
        float angleStep = 360f / rayCount;

        for (int i = 0; i < rayCount; i++)
        {
            Vector2 direction = Quaternion.Euler(0f, 0f, angleStep * i) * transform.up;

            RaycastHit2D hit = Physics2D.Raycast(
                transform.position,
                direction,
                rayDistance,
                detectMask
            );

            Debug.DrawLine(
                transform.position,
                (Vector2)transform.position + direction * rayDistance,
                hit.collider ? Color.red : Color.green
            );

            float dist = hit.collider ? hit.distance : rayDistance;
            rays[i] = dist;
        }
    }

    Vector2 AngleToDir(float angle)
    {
        float rad = angle * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }

    public float[] GetSensors()
    {
        CastCircleRays();
        return rays;
    }

    public float GetRayDistance()
    {
        return rayDistance;
    }
}
