using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public RoadGraphGenerator2D MapGenerator;
    public PlayerShipController2D playerPrefab;

    public EnemyShip enemyPrefab;

    public GameObject targetMarkerPrefab;

    public int enemyCount = 2;

    public Transform mapEnvironment;

    private List<EnemyShip> spawnedEnemies = new List<EnemyShip>();

    private PlayerShipController2D player;

    private GameObject targetMarkerInstance;

    private Leaf spawnLake;
    public Leaf targetLake;

    void Awake() { }

    void Start()
    {
        InstantiatePlayer();
        RestartGame();
    }

    public bool PlayerDie()
    {
        int mW = MapGenerator.mapWidth;
        int mH = MapGenerator.mapHeight;

        Vector2 pos = player.transform.localPosition;
        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);

        bool outOfMap = x <= 0 || x >= mW || y <= 0 || y >= mH;
        bool hitWall = false;
        if (!outOfMap)
        {
            hitWall = MapGenerator.map[x, y] == TileType.Wall;
        }
        bool hitMine = player.hitByMine;
        bool hitEnemy = false;

        foreach (EnemyShip enemy in spawnedEnemies)
        {
            float dist = Vector2.Distance(pos, enemy.transform.localPosition);

            if (dist < 3f)
            {
                hitEnemy = true;
                break;
            }
        }

        return outOfMap || hitWall || hitMine || hitEnemy;
    }

    public bool PlayerReachedTarget()
    {
        return Vector2.Distance(player.transform.localPosition, targetLake.lakeCenter) < 5.0f;
    }

    public void RestartGame()
    {
        if (MapGenerator == null)
            return;

        MapGenerator.GenerateMap();
        ChooseSpawnTargetLakes();

        player.Reset(spawnLake.lakeCenter);
        SpawnEnemies();
        PlaceTargetMarker();
    }

    void PlaceTargetMarker()
    {
        if (targetLake == null)
            return;

        Vector3 worldPos = mapEnvironment.TransformPoint(
            new Vector3(targetLake.lakeCenter.x, targetLake.lakeCenter.y, 0f)
        );

        if (targetMarkerInstance == null)
        {
            targetMarkerInstance = Instantiate(targetMarkerPrefab, worldPos, Quaternion.identity);
        }
        else
        {
            targetMarkerInstance.transform.position = worldPos;
        }
    }

    void InstantiatePlayer()
    {
        player = Instantiate(playerPrefab, mapEnvironment);
        var agent = player.GetComponent<PlayerAgent>();
        agent.gm = this;
    }

    void ChooseSpawnTargetLakes()
    {
        List<Leaf> allLeafs = MapGenerator.allLeafs;
        if (allLeafs.Count < 2)
            return;

        spawnLake = allLeafs[Random.Range(0, allLeafs.Count)];

        float minDistance = Mathf.Min(MapGenerator.mapWidth, MapGenerator.mapHeight) * 0.5f;

        List<Leaf> validTargets = new List<Leaf>();

        foreach (Leaf leaf in allLeafs)
        {
            if (leaf == spawnLake)
                continue;

            float dist = Vector2Int.Distance(spawnLake.lakeCenter, leaf.lakeCenter);

            if (dist >= minDistance)
                validTargets.Add(leaf);
        }

        if (validTargets.Count == 0)
        {
            do
            {
                targetLake = allLeafs[Random.Range(0, allLeafs.Count)];
            } while (targetLake == spawnLake);
        }
        else
        {
            targetLake = validTargets[Random.Range(0, validTargets.Count)];
        }
    }

    void SpawnEnemies()
    {
        foreach (var e in spawnedEnemies)
        {
            if (e)
            {
                e.Remove();
            }
        }
        spawnedEnemies.Clear();

        List<Leaf> availableLakes = new List<Leaf>();

        foreach (var lake in MapGenerator.allLeafs)
        {
            if (lake != spawnLake && lake != targetLake)
                availableLakes.Add(lake);
        }

        if (availableLakes.Count < enemyCount)
        {
            Debug.LogWarning("Not enough lakes to spawn enemies!");
            return;
        }

        for (int i = availableLakes.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (availableLakes[i], availableLakes[j]) = (availableLakes[j], availableLakes[i]);
        }

        for (int i = 0; i < enemyCount; i++)
        {
            Leaf lake = availableLakes[i];
            Vector2 pos = lake.lakeCenter;

            EnemyShip e = Instantiate(enemyPrefab, mapEnvironment);
            e.transform.localPosition = new Vector3(pos.x, pos.y, 0f);

            spawnedEnemies.Add(e);
        }
    }

    bool CheckGameOver()
    {
        if (player.hitByMine)
        {
            return true;
        }
        Vector2 pos = player.transform.position;

        int x = Mathf.RoundToInt(pos.x);
        int y = Mathf.RoundToInt(pos.y);

        if (x >= 0 && x < MapGenerator.mapWidth && y >= 0 && y < MapGenerator.mapHeight)
        {
            if (MapGenerator.map[x, y] == TileType.Wall)
            {
                return true;
            }
        }

        foreach (EnemyShip enemy in spawnedEnemies)
        {
            if (enemy == null)
                continue;

            Vector2 enemyPos = enemy.transform.position;

            int enemyX = Mathf.RoundToInt(enemyPos.x);
            int enemyY = Mathf.RoundToInt(enemyPos.y);

            if (
                enemyX >= 0
                && enemyX < MapGenerator.mapWidth
                && enemyY >= 0
                && enemyY < MapGenerator.mapHeight
            )
            {
                if (MapGenerator.map[enemyX, enemyY] == TileType.Wall)
                {
                    Destroy(enemy.gameObject);
                    return false;
                }
            }

            float dist = Vector2.Distance(pos, enemy.transform.position);

            if (dist < 1.5f)
            {
                return true;
            }
        }

        return false;
    }
}
