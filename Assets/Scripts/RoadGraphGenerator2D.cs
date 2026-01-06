using System.Collections.Generic;
using Unity.MLAgents;
using UnityEngine;
#if UNITY_EDITOR
#endif

public enum TileType
{
    Water,
    Wall,
}

public class Leaf
{
    public RectInt rect;
    public List<Leaf> neighbors = new List<Leaf>();
    public Vector2Int lakeCenter;
}

public class RoadGraphGenerator2D : MonoBehaviour
{
    [Header("Map Settings")]
    public int mapWidth;
    public int mapHeight;

    [Header("Lake Settings")]
    public int lakeNoise;

    [Header("Canal Settings")]
    public int minCanalWidth;
    public int maxCanalWidth;
    public float canalRandomness;

    [Header("Prefabs")]
    public GameObject wallPrefab;
    public Transform wallContainer;

    public TileType[,] map;
    public List<Leaf> allLeafs = new List<Leaf>();

    public void GenerateMap()
    {
        ResetMap();
        SplitMap();
        AssignNeighbors();
        CreateLakes();
        CreateCanals();
        InstantiateWalls();
    }

    void ResetMap()
    {
        foreach (Transform child in wallContainer)
            Destroy(child.gameObject);

        map = new TileType[mapWidth, mapHeight];
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                map[x, y] = TileType.Wall;
            }
        }

        allLeafs.Clear();
    }

    void SplitMap()
    {
        int difficulty = Mathf.RoundToInt(
            Academy.Instance.EnvironmentParameters.GetWithDefault("difficulty", 3)
        );

        int[] values = new int[] {16, 8, 4, 4};
       
        int minLeafSize = Mathf.Max(mapWidth, mapHeight) / 16;
        int maxLeafSize = Mathf.Max(mapWidth, mapHeight) / values[difficulty];
        SplitBSP(new RectInt(0, 0, mapWidth, mapHeight), minLeafSize, maxLeafSize);
    }

    void SplitBSP(RectInt rect, int minLeafSize, int maxLeafSize)
    {
        Leaf leaf = new Leaf { rect = rect };
        bool splitHorizontally = rect.width < rect.height;

        if (
            (splitHorizontally && rect.height > maxLeafSize)
            || (!splitHorizontally && rect.width > maxLeafSize)
        )
        {
            if (splitHorizontally)
            {
                int split = Random.Range(minLeafSize, rect.height - minLeafSize);
                SplitBSP(new RectInt(rect.x, rect.y, rect.width, split), minLeafSize, maxLeafSize);
                SplitBSP(
                    new RectInt(rect.x, rect.y + split, rect.width, rect.height - split),
                    minLeafSize,
                    maxLeafSize
                );
            }
            else
            {
                int split = Random.Range(minLeafSize, rect.width - minLeafSize);
                SplitBSP(new RectInt(rect.x, rect.y, split, rect.height), minLeafSize, maxLeafSize);
                SplitBSP(
                    new RectInt(rect.x + split, rect.y, rect.width - split, rect.height),
                    minLeafSize,
                    maxLeafSize
                );
            }
            return;
        }
        allLeafs.Add(leaf);
    }

    void AssignNeighbors()
    {
        bool AreAdjacent(RectInt a, RectInt b) =>
            a.xMin <= b.xMax && a.xMax >= b.xMin && a.yMin <= b.yMax && a.yMax >= b.yMin;

        List<Leaf> leafs = allLeafs;
        for (int i = 0; i < leafs.Count; i++)
        {
            for (int j = i + 1; j < leafs.Count; j++)
            {
                if (AreAdjacent(leafs[i].rect, leafs[j].rect))
                {
                    leafs[i].neighbors.Add(leafs[j]);
                    leafs[j].neighbors.Add(leafs[i]);
                }
            }
        }
    }

    void CreateLakes()
    {
        foreach (var leaf in allLeafs)
        {
            int leafArea = leaf.rect.width * leaf.rect.height;
            float minFraction = 0.2f;
            float maxFraction = 0.4f;
            int lakeArea = Mathf.RoundToInt(leafArea * Random.Range(minFraction, maxFraction));
            float maxRatio = 1.5f;

            int lakeWidth = Mathf.Max(
                Mathf.Min(Mathf.RoundToInt(Mathf.Sqrt(lakeArea * maxRatio)), leaf.rect.width - 2),
                10
            );
            int lakeHeight = Mathf.Max(
                Mathf.Min(Mathf.CeilToInt((float)lakeArea / lakeWidth), leaf.rect.height - 2),
                10
            );

            leaf.lakeCenter = new Vector2Int(
                leaf.rect.xMin + leaf.rect.width / 2,
                leaf.rect.yMin + leaf.rect.height / 2
            );

            CreateCircularLake(leaf.lakeCenter, leaf.rect, lakeWidth / 2);
        }
    }

    void CreateCircularLake(Vector2Int center, RectInt leaf, int radius)
    {
        for (int x = leaf.xMin; x < leaf.xMax; x++)
        for (int y = leaf.yMin; y < leaf.yMax; y++)
        {
            float nx = (x - center.x) / (float)radius;
            float ny = (y - center.y) / (float)radius;
            float d = nx * nx + ny * ny;

            if (d <= 1f + Random.Range(-lakeNoise * 0.01f, lakeNoise * 0.01f))
                map[x, y] = TileType.Water;
        }
    }

    void CreateCanals()
    {
        HashSet<(Leaf, Leaf)> connected = new HashSet<(Leaf, Leaf)>();
        foreach (var leaf in allLeafs)
        {
            foreach (var neighbor in leaf.neighbors)
            {
                if (!connected.Contains((leaf, neighbor)) && !connected.Contains((neighbor, leaf)))
                {
                    CreateCanal(leaf.lakeCenter, neighbor.lakeCenter);
                    connected.Add((leaf, neighbor));
                }
            }
        }
    }

    void CreateCanal(Vector2Int start, Vector2Int end)
    {
        Vector2Int current = start;
        int width = Random.Range(minCanalWidth, maxCanalWidth + 1);

        while (current != end)
        {
            FillCanalTile(current, width);
            Vector2Int dir = new Vector2Int(
                Mathf.Clamp(end.x - current.x, -1, 1),
                Mathf.Clamp(end.y - current.y, -1, 1)
            );
            if (Random.value < canalRandomness)
            {
                if (Random.value < 0.5f)
                    dir.x = Random.value < 0.5f ? -1 : 1;
                else
                    dir.y = Random.value < 0.5f ? -1 : 1;
            }
            current += dir;
        }
    }

    void FillCanalTile(Vector2Int pos, int width)
    {
        int half = width / 2;
        for (int dx = -half; dx <= half; dx++)
        for (int dy = -half; dy <= half; dy++)
        {
            int x = pos.x + dx;
            int y = pos.y + dy;
            if (x >= 0 && x < mapWidth && y >= 0 && y < mapHeight)
                map[x, y] = TileType.Water;
        }
    }

    void InstantiateWalls()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                if (map[x, y] == TileType.Wall)
                {
                    GameObject go = Instantiate(wallPrefab, wallContainer);

                    go.transform.localPosition = new Vector3(x, y, 0);
                }
            }
        }
    }
    //
    // void OnDrawGizmos()
    // {
    //     if (allLeafs == null)
    //         return;
    //
    //     Gizmos.color = Color.green;
    //
    //     if (wallContainer != null)
    //     {
    //         Gizmos.matrix = wallContainer.localToWorldMatrix;
    //     }
    //
    //     foreach (var leaf in allLeafs)
    //     {
    //         Vector3 bl = new Vector3(leaf.rect.xMin, leaf.rect.yMin, 0);
    //         Vector3 tl = new Vector3(leaf.rect.xMin, leaf.rect.yMax, 0);
    //         Vector3 tr = new Vector3(leaf.rect.xMax, leaf.rect.yMax, 0);
    //         Vector3 br = new Vector3(leaf.rect.xMax, leaf.rect.yMin, 0);
    //
    //         Gizmos.DrawLine(bl, tl);
    //         Gizmos.DrawLine(tl, tr);
    //         Gizmos.DrawLine(tr, br);
    //         Gizmos.DrawLine(br, bl);
    //     }
    //
    //     Gizmos.matrix = Matrix4x4.identity;
    // }
}
