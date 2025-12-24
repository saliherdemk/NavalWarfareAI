using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public RoadGraphGenerator2D MapGenerator;

    public PlayerShipController playerPrefab;
    public EnemyShipController enemyPrefab;

    public GameObject targetMarkerPrefab;

    public Transform mapEnvironment;

    private EnemyShipController enemy1;
    private EnemyAgent enemy1Agent;

    private EnemyShipController enemy2;
    private EnemyAgent enemy2Agent;

    private PlayerShipController player;
    private PlayerAgent playerAgent;

    private GameObject targetMarkerInstance;

    private Leaf spawnLake;
    public Leaf targetLake;

    private Leaf enemy1Lake;
    private Leaf enemy2Lake;

    void Awake() { }

    void Start()
    {
        InstantiatePlayer();
        InstantiateEnemies();
        RestartGame();
    }

    void FixedUpdate()
    {
        CheckDeaths();
    }

    public void CheckDeaths()
    {
        if (enemy1.gameObject.activeSelf && CheckEnemyDeath(enemy1))
        {
            enemy1Agent.SetReward(-1.0f);
            enemy1Agent.EndEpisode();
            enemy1.gameObject.SetActive(false);
        }

        if (enemy2.gameObject.activeSelf && CheckEnemyDeath(enemy2))
        {
            enemy2Agent.SetReward(-1.0f);
            enemy2Agent.EndEpisode();
            enemy2.gameObject.SetActive(false);
        }

        if (CommitedSuicide(player.transform))
        {
            playerAgent.SetReward(-1.0f);
            EndEpisode();
            return;
        }

        if (player.hitByMine)
        {
            playerAgent.SetReward(-1.0f);
            enemy1Agent.SetReward(1.0f);
            enemy2Agent.SetReward(1.0f);
            EndEpisode();
            return;
        }

        Vector2 pos = player.transform.localPosition;

        float dist1 = Vector2.Distance(pos, enemy1.transform.localPosition);
        float dist2 = Vector2.Distance(pos, enemy2.transform.localPosition);

        if (dist1 < 3f)
        {
            playerAgent.SetReward(-1.0f);
            enemy1Agent.SetReward(1.0f);
            EndEpisode();
            return;
        }

        if (dist2 < 3f)
        {
            playerAgent.SetReward(-1.0f);
            enemy2Agent.SetReward(1.0f);
            EndEpisode();
            return;
        }

        if (PlayerReachedTarget())
        {
            playerAgent.SetReward(1.0f);
            enemy1Agent.SetReward(-1.0f);
            enemy2Agent.SetReward(-1.0f);
            EndEpisode();
            return;
        }
    }

    private void EndEpisode()
    {
        // Debug.Log($"Player Reward: {playerAgent.GetCumulativeReward():F2}");
        // Debug.Log($"Enemy1 Reward: {enemy1Agent.GetCumulativeReward():F2}");
        // Debug.Log($"Enemy2 Reward: {enemy2Agent.GetCumulativeReward():F2}");

        if (player.gameObject.activeSelf)
            playerAgent.EndEpisode();
        if (enemy1.gameObject.activeSelf)
            enemy1Agent.EndEpisode();
        if (enemy2.gameObject.activeSelf)
            enemy2Agent.EndEpisode();

        RestartGame();
    }

    public bool CheckEnemyDeath(EnemyShipController enemy)
    {
        return CommitedSuicide(enemy.transform) || enemy.hitByMine;
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
        enemy1.Reset(enemy1Lake.lakeCenter);
        enemy2.Reset(enemy2Lake.lakeCenter);

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
        playerAgent = player.GetComponent<PlayerAgent>();
        playerAgent.gm = this;
    }

    void InstantiateEnemies()
    {
        enemy1 = Instantiate(enemyPrefab, mapEnvironment);
        enemy1Agent = enemy1.GetComponent<EnemyAgent>();

        enemy2 = Instantiate(enemyPrefab, mapEnvironment);
        enemy2Agent = enemy2.GetComponent<EnemyAgent>();
    }

    void ChooseSpawnTargetLakes()
    {
        List<Leaf> allLeafs = MapGenerator.allLeafs;
        if (allLeafs.Count < 2)
            return;

        spawnLake = allLeafs[Random.Range(0, allLeafs.Count)];

        float trainingRadius = Academy.Instance.EnvironmentParameters.GetWithDefault(
            "spawn_radius",
            -1f
        );

        List<Leaf> validTargets = new List<Leaf>();

        if (trainingRadius > 0f)
        {
            foreach (Leaf leaf in allLeafs)
            {
                if (leaf == spawnLake)
                    continue;

                float dist = Vector2Int.Distance(spawnLake.lakeCenter, leaf.lakeCenter);

                if (dist <= trainingRadius && dist > 5f)
                {
                    validTargets.Add(leaf);
                }
            }
        }
        else
        {
            float minDistance = Mathf.Min(MapGenerator.mapWidth, MapGenerator.mapHeight) * 0.5f;

            foreach (Leaf leaf in allLeafs)
            {
                if (leaf == spawnLake)
                    continue;

                float dist = Vector2Int.Distance(spawnLake.lakeCenter, leaf.lakeCenter);

                if (dist >= minDistance)
                {
                    validTargets.Add(leaf);
                }
            }
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

        List<Leaf> availableLakes = new List<Leaf>();

        foreach (var lake in MapGenerator.allLeafs)
        {
            if (lake != spawnLake && lake != targetLake)
                availableLakes.Add(lake);
        }

        for (int i = availableLakes.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (availableLakes[i], availableLakes[j]) = (availableLakes[j], availableLakes[i]);
        }

        enemy1Lake = availableLakes[0];
        enemy2Lake = availableLakes[1];
    }

    private bool CommitedSuicide(Transform shipTransform)
    {
        int mW = MapGenerator.mapWidth;
        int mH = MapGenerator.mapHeight;

        float xExt = 1.0f;
        float yExt = 2.0f;

        Vector2[] localPoints = new Vector2[]
        {
            new Vector2(0, yExt), // Top Center
            new Vector2(xExt, yExt), // Top Right
            new Vector2(-xExt, yExt), // Top Left
            new Vector2(xExt, -yExt), // Bottom Right
            new Vector2(-xExt, -yExt), // Bottom Left
            new Vector2(0, -yExt), // Bottom Center
        };

        Vector2 shipPos = shipTransform.transform.localPosition;
        Quaternion shipRot = shipTransform.transform.localRotation;

        foreach (Vector2 p in localPoints)
        {
            Vector3 rotatedPoint = shipRot * p;

            Vector2 worldPos = shipPos + (Vector2)rotatedPoint;

            int x = Mathf.RoundToInt(worldPos.x);
            int y = Mathf.RoundToInt(worldPos.y);

            if (x <= 0 || x >= mW || y <= 0 || y >= mH)
            {
                return true;
            }

            if (MapGenerator.map[x, y] == TileType.Wall)
            {
                return true;
            }
        }

        return false;
    }
}
