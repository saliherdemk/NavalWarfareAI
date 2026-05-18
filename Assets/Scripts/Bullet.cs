using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float speed = 15f;
    public bool isPlayerBullet;
    private Vector2 direction;
    private RoadGraphGenerator2D mapGenerator;
    private bool destroyed;

    public Vector2 Velocity => direction * speed;

    public void Initialize(Vector2 dir, RoadGraphGenerator2D mapGen)
    {
        direction = dir.normalized;
        mapGenerator = mapGen;
    }

    void FixedUpdate()
    {
        if (destroyed) return;
        transform.localPosition += (Vector3)(direction * speed * Time.fixedDeltaTime);
        CheckWallCollision();
    }

    void CheckWallCollision()
    {
        Vector2 pos = transform.localPosition;
        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);

        if (x <= 0 || x >= mapGenerator.mapWidth || y <= 0 || y >= mapGenerator.mapHeight)
        {
            destroyed = true;
            Destroy(gameObject);
            return;
        }

        if (mapGenerator.map[x, y] == TileType.Wall)
        {
            destroyed = true;
            Destroy(gameObject);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (destroyed) return;
        if (collision.gameObject.CompareTag("PlayerShip"))
        {
            destroyed = true;
            PlayerAgent pa = collision.gameObject.GetComponent<PlayerAgent>();
            if (pa != null)
                pa.Kill();
            Destroy(gameObject);
            return;
        }
        if (collision.gameObject.CompareTag("EnemyShip"))
        {
            destroyed = true;
            Destroy(collision.gameObject);
            Destroy(gameObject);
            return;
        }
        destroyed = true;
        Destroy(gameObject);
    }
}
