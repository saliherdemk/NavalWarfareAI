using UnityEngine;

public abstract class Phase : MonoBehaviour
{
    public RoadGraphGenerator2D MapGenerator;
    public ShipController playerPrefab;
    public ShipController enemyPrefab;
    public GameObject targetMarkerPrefab;
    public Transform mapEnvironment;

    protected GameObject targetMarkerInstance;
    protected Leaf spawnLake;
    public Leaf targetLake;
    public Leaf enemyLake;

    protected int maxSteps = 5000;

    public abstract void InitPhase();
    public abstract void UpdatePhase();

    public virtual void RestartGame() { }

    protected void PlaceTargetMarker()
    {
        if (targetLake == null || mapEnvironment == null)
            return;

        Vector3 worldPos = mapEnvironment.TransformPoint(
            new Vector3(targetLake.lakeCenter.x, targetLake.lakeCenter.y, 0f)
        );

        if (targetMarkerInstance == null)
            targetMarkerInstance = Instantiate(targetMarkerPrefab, worldPos, Quaternion.identity);
        else
            targetMarkerInstance.transform.position = worldPos;
    }

    public int GetMaxStep()
    {
        return maxSteps;
    }
}
