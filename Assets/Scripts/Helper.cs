using UnityEngine;

public static class Helper
{
    public static bool CommitedSuicide(Transform shipTransform, RoadGraphGenerator2D mapGenerator)
    {
        int mW = mapGenerator.mapWidth;
        int mH = mapGenerator.mapHeight;

        float xExt = 1.0f;
        float yExt = 2.0f;

        Vector2[] localPoints = new Vector2[]
        {
            new Vector2(0, yExt),
            new Vector2(xExt, yExt),
            new Vector2(-xExt, yExt),
            new Vector2(xExt, -yExt),
            new Vector2(-xExt, -yExt),
            new Vector2(0, -yExt),
        };

        Vector2 shipPos = shipTransform.localPosition;
        Quaternion shipRot = shipTransform.localRotation;

        foreach (Vector2 p in localPoints)
        {
            Vector3 rotatedPoint = shipRot * p;
            Vector2 worldPos = shipPos + (Vector2)rotatedPoint;

            int x = Mathf.RoundToInt(worldPos.x);
            int y = Mathf.RoundToInt(worldPos.y);

            if (x <= 0 || x >= mW || y <= 0 || y >= mH)
                return true;

            if (mapGenerator.map[x, y] == TileType.Wall)
                return true;
        }

        return false;
    }

    public static bool PlayerReachedTarget(Transform shipTransform, Vector2 targetPosition)
    {
        return Vector2.Distance(shipTransform.localPosition, targetPosition) < 5.0f;
    }

    public static GameObject PlaceOrMoveMarker(
        GameObject markerInstance,
        GameObject markerPrefab,
        Transform parent,
        Vector2 localPosition
    )
    {
        Vector3 worldPos = parent.TransformPoint(new Vector3(localPosition.x, localPosition.y, 0f));

        if (markerInstance == null)
        {
            markerInstance = Object.Instantiate(markerPrefab, worldPos, Quaternion.identity);
        }
        else
        {
            markerInstance.transform.position = worldPos;
        }

        return markerInstance;
    }
}
