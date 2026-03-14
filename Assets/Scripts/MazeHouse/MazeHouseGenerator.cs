using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class MazeHouseGenerator : MonoBehaviour
{
    public enum CellType
    {
        Empty,
        Room,
        Corridor
    }

    [Header("Scene References")]
    [Tooltip("Outer boundary object (BoxCollider or Renderer) that defines house extents.")]
    public Transform houseBoundary;

    [Tooltip("Floor object for alignment (optional, used for Y position).")]
    public Transform floor;

    [Header("Layout Parameters")]
    public int numberOfRooms = 10;

    [Tooltip("Minimum room size in grid cells (X,Z).")]
    public Vector2Int minimumRoomSize = new Vector2Int(3, 3);

    [Tooltip("Maximum room size in grid cells (X,Z).")]
    public Vector2Int maximumRoomSize = new Vector2Int(8, 8);

    [Tooltip("Width of corridors and cell size in world units.")]
    public float corridorWidth = 2f;

    [Range(0f, 1f)]
    [Tooltip("0 = almost no side branches, 1 = very loopy maze.")]
    public float mazeComplexity = 0.3f;

    [Header("Walls")]
    [Tooltip("Prefab for wall segments. If null, a cube will be used.")]
    public GameObject wallPrefab;

    [Tooltip("Height of walls.")]
    public float wallHeight = 3f;

    [Tooltip("Thickness of walls in world units.")]
    public float wallThickness = 0.2f;

    [Header("Furniture")]
    [Tooltip("Root transform containing furniture prefabs as children (optional).")]
    public Transform furniturePrefabsRoot;

    [Tooltip("Tag used to find furniture source objects in the scene (optional).")]
    public string furnitureTag = "Furniture";

    [Range(0f, 1f)]
    [Tooltip("Chance of placing furniture in a suitable room cell.")]
    public float furnitureDensity = 0.2f;

    private CellType[,] grid;
    private int gridSizeX;
    private int gridSizeZ;
    private Bounds houseBounds;
    private Transform mazeRoot;
    private Transform roomsRoot;
    private Transform corridorsRoot;
    private Transform wallsRoot;
    private Transform furnitureRoot;
    private readonly System.Random rng = new System.Random();

    [ContextMenu("Generate Maze House")]
    public void GenerateMazeHouse()
    {
        if (houseBoundary == null)
        {
            Debug.LogError("MazeHouseGenerator: houseBoundary is not assigned.");
            return;
        }

        SetupBoundsAndGrid();
        if (gridSizeX <= 2 || gridSizeZ <= 2)
        {
            Debug.LogError("MazeHouseGenerator: Computed grid is too small. Adjust corridorWidth or boundary size.");
            return;
        }

        ClearOldGeneratedObjects();
        CreateHierarchy();

        GenerateRooms();
        GenerateMazeCorridors();
        BuildWalls();
        PlaceFurniture();

        Debug.Log("MazeHouseGenerator: Maze house generated.");
    }

    private void SetupBoundsAndGrid()
    {
        if (!TryGetBounds(houseBoundary, out houseBounds))
        {
            Debug.LogError("MazeHouseGenerator: Could not get bounds from houseBoundary. Add BoxCollider or Renderer.");
            return;
        }

        float shrink = corridorWidth * 0.5f;
        houseBounds.Expand(new Vector3(-shrink * 2f, 0f, -shrink * 2f));

        gridSizeX = Mathf.Max(3, Mathf.FloorToInt(houseBounds.size.x / corridorWidth));
        gridSizeZ = Mathf.Max(3, Mathf.FloorToInt(houseBounds.size.z / corridorWidth));
        grid = new CellType[gridSizeX, gridSizeZ];
    }

    private bool TryGetBounds(Transform t, out Bounds b)
    {
        b = new Bounds();

        var col = t.GetComponent<Collider>();
        if (col != null)
        {
            b = col.bounds;
            return true;
        }

        var rend = t.GetComponent<Renderer>();
        if (rend != null)
        {
            b = rend.bounds;
            return true;
        }

        return false;
    }

    private void CreateHierarchy()
    {
        var rootName = "MazeHouse";
        var existingRoot = transform.Find(rootName);
        if (existingRoot != null)
        {
            mazeRoot = existingRoot;
        }
        else
        {
            mazeRoot = new GameObject(rootName).transform;
            mazeRoot.SetParent(transform, false);
        }

        roomsRoot = CreateOrFindChild(mazeRoot, "Rooms");
        corridorsRoot = CreateOrFindChild(mazeRoot, "Corridors");
        wallsRoot = CreateOrFindChild(mazeRoot, "Walls");
        furnitureRoot = CreateOrFindChild(mazeRoot, "Furniture");
    }

    private Transform CreateOrFindChild(Transform parent, string name)
    {
        var child = parent.Find(name);
        if (child != null) return child;

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        return go.transform;
    }

    private void ClearOldGeneratedObjects()
    {
        var existing = transform.Find("MazeHouse");
        if (existing != null)
        {
            if (Application.isEditor)
            {
#if UNITY_EDITOR
                UnityEditor.Undo.RegisterFullObjectHierarchyUndo(existing.gameObject, "Clear MazeHouse");
                UnityEditor.Undo.DestroyObjectImmediate(existing.gameObject);
#else
                DestroyImmediate(existing.gameObject);
#endif
            }
            else
            {
                Destroy(existing.gameObject);
            }
        }
    }

    private Vector3 CellToWorld(int x, int z)
    {
        var min = houseBounds.min;
        float y = floor != null ? floor.position.y : houseBounds.min.y;
        return new Vector3(
            min.x + (x + 0.5f) * corridorWidth,
            y,
            min.z + (z + 0.5f) * corridorWidth
        );
    }

    private bool InBounds(int x, int z)
    {
        return x >= 0 && x < gridSizeX && z >= 0 && z < gridSizeZ;
    }

    private bool IsWalkable(int x, int z)
    {
        if (!InBounds(x, z)) return false;
        return grid[x, z] == CellType.Room || grid[x, z] == CellType.Corridor;
    }

    private void GenerateRooms()
    {
        int attempts = numberOfRooms * 10;
        int roomsPlaced = 0;

        for (int i = 0; i < attempts && roomsPlaced < numberOfRooms; ++i)
        {
            int w = rng.Next(minimumRoomSize.x, maximumRoomSize.x + 1);
            int h = rng.Next(minimumRoomSize.y, maximumRoomSize.y + 1);

            if (w >= gridSizeX - 2 || h >= gridSizeZ - 2)
                continue;

            int x = rng.Next(1, gridSizeX - w - 1);
            int z = rng.Next(1, gridSizeZ - h - 1);

            if (!CanPlaceRoom(x, z, w, h))
                continue;

            PlaceRoom(x, z, w, h);
            roomsPlaced++;
        }
    }

    private bool CanPlaceRoom(int startX, int startZ, int width, int height)
    {
        for (int x = startX - 1; x < startX + width + 1; x++)
        {
            for (int z = startZ - 1; z < startZ + height + 1; z++)
            {
                if (!InBounds(x, z)) return false;
                if (grid[x, z] != CellType.Empty) return false;
            }
        }
        return true;
    }

    private void PlaceRoom(int startX, int startZ, int width, int height)
    {
        for (int x = startX; x < startX + width; x++)
        {
            for (int z = startZ; z < startZ + height; z++)
            {
                grid[x, z] = CellType.Room;
            }
        }
    }

    private void GenerateMazeCorridors()
    {
        bool[,] visited = new bool[gridSizeX, gridSizeZ];

        int startX = rng.Next(0, gridSizeX);
        int startZ = rng.Next(0, gridSizeZ);

        DepthFirstCarve(startX, startZ, visited);

        int extraLinks = Mathf.RoundToInt(mazeComplexity * gridSizeX * gridSizeZ * 0.1f);
        for (int i = 0; i < extraLinks; i++)
        {
            int x = rng.Next(1, gridSizeX - 1);
            int z = rng.Next(1, gridSizeZ - 1);
            if (grid[x, z] == CellType.Empty)
            {
                grid[x, z] = CellType.Corridor;
            }
        }

        int forcedRow = gridSizeZ / 2;
        for (int x = 0; x < gridSizeX; x++)
        {
            if (grid[x, forcedRow] == CellType.Empty)
                grid[x, forcedRow] = CellType.Corridor;
        }

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                if (grid[x, z] == CellType.Corridor)
                {
                    var pos = CellToWorld(x, z);
                    var go = new GameObject($"Corridor_{x}_{z}");
                    go.transform.position = pos;
                    go.transform.SetParent(corridorsRoot, true);
                }
                else if (grid[x, z] == CellType.Room)
                {
                    var pos = CellToWorld(x, z);
                    var go = new GameObject($"RoomCell_{x}_{z}");
                    go.transform.position = pos;
                    go.transform.SetParent(roomsRoot, true);
                }
            }
        }
    }

    private void DepthFirstCarve(int sx, int sz, bool[,] visited)
    {
        if (!InBounds(sx, sz)) return;
        if (visited[sx, sz]) return;
        visited[sx, sz] = true;

        if (grid[sx, sz] == CellType.Empty)
        {
            grid[sx, sz] = CellType.Corridor;
        }

        var dirs = new List<Vector2Int>
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };
        Shuffle(dirs);

        foreach (var d in dirs)
        {
            int nx = sx + d.x;
            int nz = sz + d.y;
            if (!InBounds(nx, nz)) continue;

            if (!visited[nx, nz])
            {
                DepthFirstCarve(nx, nz, visited);
            }
        }
    }

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int k = rng.Next(i + 1);
            var tmp = list[i];
            list[i] = list[k];
            list[k] = tmp;
        }
    }

    private void BuildWalls()
    {
        GameObject prefab = wallPrefab;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                bool hereWalkable = IsWalkable(x, z);

                if (hereWalkable)
                {
                    if (!IsWalkable(x + 1, z))
                    {
                        Vector3 pos = (CellToWorld(x, z) + CellToWorld(x + 1, z)) * 0.5f;
                        CreateWallSegment(prefab, pos, Quaternion.Euler(0, 0, 0));
                    }
                    if (!IsWalkable(x - 1, z))
                    {
                        Vector3 pos = (CellToWorld(x, z) + CellToWorld(x - 1, z)) * 0.5f;
                        CreateWallSegment(prefab, pos, Quaternion.Euler(0, 0, 0));
                    }
                    if (!IsWalkable(x, z + 1))
                    {
                        Vector3 pos = (CellToWorld(x, z) + CellToWorld(x, z + 1)) * 0.5f;
                        CreateWallSegment(prefab, pos, Quaternion.Euler(0, 90, 0));
                    }
                    if (!IsWalkable(x, z - 1))
                    {
                        Vector3 pos = (CellToWorld(x, z) + CellToWorld(x, z - 1)) * 0.5f;
                        CreateWallSegment(prefab, pos, Quaternion.Euler(0, 90, 0));
                    }
                }
            }
        }
    }

    private void CreateWallSegment(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject wallGO;

        if (prefab != null)
        {
            wallGO = (GameObject)Instantiate(prefab, position, rotation, wallsRoot);
        }
        else
        {
            wallGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wallGO.transform.SetParent(wallsRoot, true);
            wallGO.transform.position = position;
            wallGO.transform.rotation = rotation;
        }

        var scale = wallGO.transform.localScale;
        if (Mathf.Abs(rotation.eulerAngles.y) < 1f || Mathf.Abs(rotation.eulerAngles.y - 180f) < 1f)
        {
            scale.x = wallThickness;
            scale.y = wallHeight;
            scale.z = corridorWidth;
        }
        else
        {
            scale.x = corridorWidth;
            scale.y = wallHeight;
            scale.z = wallThickness;
        }

        wallGO.transform.localScale = scale;
    }

    private void PlaceFurniture()
    {
        var furnitureSources = new List<GameObject>();

        if (furniturePrefabsRoot != null)
        {
            foreach (Transform child in furniturePrefabsRoot)
            {
                if (child.gameObject.activeSelf)
                    furnitureSources.Add(child.gameObject);
            }
        }

        if (!string.IsNullOrEmpty(furnitureTag))
        {
            try
            {
                var tagged = GameObject.FindGameObjectsWithTag(furnitureTag);
                foreach (var go in tagged)
                {
                    if (!furnitureSources.Contains(go))
                        furnitureSources.Add(go);
                }
            }
            catch (UnityException) { }
        }

        if (furnitureSources.Count == 0 || furnitureDensity <= 0f)
            return;

        var candidateCells = new List<Vector2Int>();
        for (int x = 0; x < gridSizeX; x++)
        {
            for (int z = 0; z < gridSizeZ; z++)
            {
                if (grid[x, z] != CellType.Room) continue;

                bool nearCorridor = false;
                for (int dx = -1; dx <= 1 && !nearCorridor; dx++)
                {
                    for (int dz = -1; dz <= 1 && !nearCorridor; dz++)
                    {
                        if (dx == 0 && dz == 0) continue;
                        int nx = x + dx;
                        int nz = z + dz;
                        if (InBounds(nx, nz) && grid[nx, nz] == CellType.Corridor)
                        {
                            nearCorridor = true;
                        }
                    }
                }

                if (!nearCorridor)
                    candidateCells.Add(new Vector2Int(x, z));
            }
        }

        foreach (var cell in candidateCells)
        {
            if ((float)rng.NextDouble() > furnitureDensity)
                continue;

            var prefab = furnitureSources[rng.Next(furnitureSources.Count)];
            Vector3 pos = CellToWorld(cell.x, cell.y);

            float offset = corridorWidth * 0.3f;
            pos += new Vector3(
                (float)(rng.NextDouble() * 2 - 1) * offset,
                0f,
                (float)(rng.NextDouble() * 2 - 1) * offset
            );

            if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out var hit, 20f))
            {
                pos.y = hit.point.y;
            }

            var instance = (GameObject)Instantiate(prefab, pos, Quaternion.identity, furnitureRoot);
            instance.name = prefab.name;
        }
    }
}
