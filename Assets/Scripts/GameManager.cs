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

    public PlayerShipController player;
    private PlayerAgent playerAgent;

    private GameObject targetMarkerInstance;

    private Leaf spawnLake;
    public Leaf targetLake;

    private Leaf enemy1Lake;
    private Leaf enemy2Lake;

    private int MaxStep = 10000;

    void Awake() { }

    void Start()
    {
        InstantiatePlayer();
        InstantiateEnemies();
        RestartGame();
    }

    void FixedUpdate()
    {
        if (!Academy.Instance.IsCommunicatorOn)
            return;

        CheckDeaths();
    }

    public void CheckDeaths()
    {
        if (enemy1.gameObject.activeSelf && CommitedSuicide(enemy1.transform))
        {
            enemy1Agent.AddReward(-1.0f);
            playerAgent.AddReward(+0.3f);
            enemy1Agent.EndEpisode();
            enemy1.gameObject.SetActive(false);
        }

        if (enemy2.gameObject.activeSelf && CommitedSuicide(enemy2.transform))
        {
            enemy2Agent.AddReward(-1.0f);
            playerAgent.AddReward(+0.3f);
            enemy2Agent.EndEpisode();
            enemy2.gameObject.SetActive(false);
        }

        if (CommitedSuicide(player.transform))
        {
            playerAgent.AddReward(-1.0f);

            if (enemy1.gameObject.activeSelf)
                enemy1Agent.AddReward(+1.0f);
            if (enemy2.gameObject.activeSelf)
                enemy2Agent.AddReward(+1.0f);

            EndEpisode();
            return;
        }

        if (player.hitByMine)
        {
            playerAgent.AddReward(-1.0f);

            if (enemy1.gameObject.activeSelf)
                enemy1Agent.AddReward(+1.0f);
            if (enemy2.gameObject.activeSelf)
                enemy2Agent.AddReward(+1.0f);

            EndEpisode();
            return;
        }

        Vector2 pos = player.transform.localPosition;

        if (
            enemy1.gameObject.activeSelf
            && Vector2.Distance(pos, enemy1.transform.localPosition) < 3f
        )
        {
            playerAgent.AddReward(-1.0f);
            enemy1Agent.AddReward(+1.0f);
            EndEpisode();
            return;
        }

        if (
            enemy2.gameObject.activeSelf
            && Vector2.Distance(pos, enemy2.transform.localPosition) < 3f
        )
        {
            playerAgent.AddReward(-1.0f);
            enemy2Agent.AddReward(+1.0f);
            EndEpisode();
            return;
        }

        if (PlayerReachedTarget())
        {
            playerAgent.AddReward(+1.0f);

            if (enemy1.gameObject.activeSelf)
                enemy1Agent.AddReward(-1.0f);
            if (enemy2.gameObject.activeSelf)
                enemy2Agent.AddReward(-1.0f);

            EndEpisode();
            return;
        }

        if (playerAgent.StepCount >= MaxStep)
        {
            playerAgent.AddReward(-0.5f);

            if (enemy1.gameObject.activeSelf)
                enemy1Agent.AddReward(+0.5f);
            if (enemy2.gameObject.activeSelf)
                enemy2Agent.AddReward(+0.5f);

            EndEpisode();
            return;
        }
    }

    private void EndEpisode()
    {
        // Debug.Log($"Player Reward: {playerAgent.GetCumulativeReward():F2}");
        Debug.Log($"Enemy1 Reward: {enemy1Agent.GetCumulativeReward():F2}");
        Debug.Log($"Enemy2 Reward: {enemy2Agent.GetCumulativeReward():F2}");
        // Debug.Log(playerAgent.StepCount);

        if (player.gameObject.activeSelf)
            playerAgent.EndEpisode();
        if (enemy1.gameObject.activeSelf)
            enemy1Agent.EndEpisode();
        if (enemy2.gameObject.activeSelf)
            enemy2Agent.EndEpisode();
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
        bool succeed = ChooseSpawnTargetLakes();
        if (!succeed)
        {
            RestartGame();
            return;
        }

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
        enemy1Agent.gm = this;

        enemy2 = Instantiate(enemyPrefab, mapEnvironment);
        enemy2Agent = enemy2.GetComponent<EnemyAgent>();
        enemy2Agent.gm = this;
    }

    bool ChooseSpawnTargetLakes()
    {
        List<Leaf> allLeafs = MapGenerator.allLeafs;
        if (allLeafs.Count < 4)
            return false;

        spawnLake = allLeafs[Random.Range(0, allLeafs.Count)];

        float diff = Academy.Instance.EnvironmentParameters.GetWithDefault("difficulty", 3.0f);

        int[] values = new int[] { 50, 100, 100, -1 };
        int trainingRadius = values[(int)diff];

        List<Leaf> validTargets = new List<Leaf>();

        if (trainingRadius > 0)
        {
            foreach (Leaf leaf in allLeafs)
            {
                if (leaf == spawnLake)
                    continue;
                float dist = Vector2Int.Distance(spawnLake.lakeCenter, leaf.lakeCenter);
                if (dist <= trainingRadius && dist > 5f)
                    validTargets.Add(leaf);
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
                    validTargets.Add(leaf);
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
        foreach (var lake in allLeafs)
        {
            if (lake != spawnLake && lake != targetLake)
                availableLakes.Add(lake);
        }

        float enemySpawnRadius = Academy.Instance.EnvironmentParameters.GetWithDefault(
            "enemy_spawn_radius",
            -1.0f
        );

        if (enemySpawnRadius > 0)
        {
            List<Leaf> closeLakes = new List<Leaf>();

            foreach (var lake in availableLakes)
            {
                float dist = Vector2Int.Distance(spawnLake.lakeCenter, lake.lakeCenter);
                if (dist <= enemySpawnRadius)
                {
                    closeLakes.Add(lake);
                }
            }

            if (closeLakes.Count > 0)
            {
                enemy1Lake = closeLakes[Random.Range(0, closeLakes.Count)];

                availableLakes.Remove(enemy1Lake);
            }
            else
            {
                Leaf closest = null;
                float minDist = float.MaxValue;

                foreach (var lake in availableLakes)
                {
                    float d = Vector2Int.Distance(spawnLake.lakeCenter, lake.lakeCenter);
                    if (d < minDist)
                    {
                        minDist = d;
                        closest = lake;
                    }
                }
                enemy1Lake = closest;
                availableLakes.Remove(closest);
            }
        }
        else
        {
            int r = Random.Range(0, availableLakes.Count);
            enemy1Lake = availableLakes[r];
            availableLakes.RemoveAt(r);
        }

        if (availableLakes.Count > 0)
        {
            enemy2Lake = availableLakes[Random.Range(0, availableLakes.Count)];
        }
        else
        {
            enemy2Lake = enemy1Lake;
        }

        return true;
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

    public int GetMaxStep()
    {
        return MaxStep;
    }
}
