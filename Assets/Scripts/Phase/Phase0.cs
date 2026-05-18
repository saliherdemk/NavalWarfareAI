using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class Phase0 : Phase
{
    public ShipController player;
    private PlayerAgent playerAgent;

    public override void InitPhase()
    {
        InstantiatePlayer();
        RestartGame();
    }

    public override void UpdatePhase()
    {
        CheckDeathsPhase();
    }

    public override void RestartGame()
    {
        if (MapGenerator == null)
            return;

        MapGenerator.GenerateMap();

        if (!ChooseSpawnTargetLakes())
        {
            RestartGame();
            return;
        }

        player.Reset(spawnLake.lakeCenter);

        targetMarkerInstance = Helper.PlaceOrMoveMarker(
            targetMarkerInstance,
            targetMarkerPrefab,
            mapEnvironment,
            targetLake.lakeCenter
        );
    }

    private void InstantiatePlayer()
    {
        if (player == null)
        {
            player = Instantiate(playerPrefab, mapEnvironment);
            playerAgent = player.GetComponent<PlayerAgent>();
            playerAgent.gm = this;
        }
    }

    private void CheckDeathsPhase()
    {
        if (player == null || playerAgent == null)
            return;

        if (playerAgent.IsDead)
        {
            playerAgent.AddReward(-1.0f);
            EndEpisode();
            return;
        }

        if (Helper.CommitedSuicide(player.transform, MapGenerator))
        {
            playerAgent.AddReward(-1.0f);
            EndEpisode();
            return;
        }

        if (Helper.PlayerReachedTarget(player.transform, targetLake.lakeCenter))
        {
            float timeBonus = 1f - (float)playerAgent.StepCount / maxSteps;
            playerAgent.AddReward(5.0f + timeBonus);
            EndEpisode();
            return;
        }

        if (playerAgent.StepCount >= maxSteps)
        {
            EndEpisode();
        }
    }

    private void EndEpisode()
    {
        foreach (Bullet b in FindObjectsByType<Bullet>(FindObjectsSortMode.None))
            Destroy(b.gameObject);

        playerAgent.EndEpisode();
    }

    private bool ChooseSpawnTargetLakes()
    {
        List<Leaf> allLeafs = MapGenerator.allLeafs;

        if (allLeafs.Count < 4)
            return false;

        spawnLake = allLeafs[Random.Range(0, allLeafs.Count)];

        float diff = Academy.Instance.EnvironmentParameters
            .GetWithDefault("difficulty", 2.0f);

        int[] values = new int[] { 50, 100, -1 };
        int trainingRadius = values[(int)diff];

        List<Leaf> validTargets = new List<Leaf>();

        if (trainingRadius > 0)
        {
            foreach (Leaf leaf in allLeafs)
            {
                if (leaf == spawnLake)
                    continue;

                float dist = Vector2Int.Distance(
                    spawnLake.lakeCenter,
                    leaf.lakeCenter
                );

                if (dist <= trainingRadius && dist > 5f)
                    validTargets.Add(leaf);
            }
        }
        else
        {
            float minDistance =
                Mathf.Min(MapGenerator.mapWidth, MapGenerator.mapHeight) * 0.5f;

            foreach (Leaf leaf in allLeafs)
            {
                if (leaf == spawnLake)
                    continue;

                float dist = Vector2Int.Distance(
                    spawnLake.lakeCenter,
                    leaf.lakeCenter
                );

                if (dist >= minDistance)
                    validTargets.Add(leaf);
            }
        }

        if (validTargets.Count == 0)
        {
            do
            {
                targetLake = allLeafs[Random.Range(0, allLeafs.Count)];
            }
            while (targetLake == spawnLake);
        }
        else
        {
            targetLake = validTargets[
                Random.Range(0, validTargets.Count)
            ];
        }

        return true;
    }
}
