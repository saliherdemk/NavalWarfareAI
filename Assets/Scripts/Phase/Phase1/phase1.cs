using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;

public class Phase1 : Phase
{
    public ShipController player;
    private PlayerAgent playerAgent;

    public ShipController enemy;
    private EnemyAgent enemyAgent;

    public TextAsset[] presetMaps;
    public int presetMapIndex;

    public override void InitPhase()
    {
        InstantiateEntities();
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

        TextAsset randomMap = presetMaps[Random.Range(0, presetMaps.Length)];
        var (sp, tg, en) = MapGenerator.LoadMapFromCSV(randomMap);

        spawnLake = sp;
        targetLake = tg;
        enemyLake = en;

        player.Reset(spawnLake.lakeCenter);
        enemy.Reset(enemyLake.lakeCenter);

        targetMarkerInstance = Helper.PlaceOrMoveMarker(
            targetMarkerInstance,
            targetMarkerPrefab,
            mapEnvironment,
            targetLake.lakeCenter
        );
    }

    private void InstantiateEntities()
    {
        if (player == null)
        {
            player = Instantiate(playerPrefab, mapEnvironment);
            playerAgent = player.GetComponent<PlayerAgent>();
            playerAgent.gm = this;
        }

        if (enemy == null)
        {
            enemy = Instantiate(enemyPrefab, mapEnvironment);
            enemy.GetComponent<ShipMovement>().speedMult = 0f;
            enemyAgent = enemy.GetComponent<EnemyAgent>();
            enemyAgent.gm = this;
        }
    }

    private void CheckDeathsPhase()
    {
        if (player == null || playerAgent == null || enemy == null || enemyAgent == null)
            return;

        if (enemyAgent.IsDead && enemy.gameObject.activeSelf)
        {
            playerAgent.AddReward(1.0f);
            enemy.gameObject.SetActive(false);
            return;
        }

        if (!enemyAgent.IsDead && Helper.PlayerCollidedWithEnemy(player.transform, enemy.transform))
        {
            playerAgent.Kill();
            enemyAgent.AddReward(1.0f);
            playerAgent.AddReward(-1.0f);
            EndEpisode();
            return;
        }

        if (playerAgent.IsDead)
        {
            enemyAgent.AddReward(1.0f);
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

        RestartGame();

        playerAgent.EndEpisode();
        enemyAgent.EndEpisode();
    }
}
