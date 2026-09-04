using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class WorldGeneration : MonoBehaviour
{
    [Header("World Settings")]
    public int WorldSize = 32;
    public int ChunkSize = 100;
    
    [Header("Seed Settings")]
    public bool UseRandomSeed = true;
    public int Seed;

    [Header("Visualization")]
    public float StepDelay = 0.05f; // How fast the coroutine runs

    [Header("Modules")]
    [SerializeField] List<Chunk> ChunkModuleList;
    [SerializeField] List<Structure> StructureModuleList;

    private Cell[,] WorldGrid;
    private GameObject[,] InstantiatedObjects;
    private System.Random prng;
    private Queue<Vector2Int> propagationQueue;
    
    // BACKTRACKING: Stack to keep our previous safe states
    private Stack<GridSnapshot> historyStack = new Stack<GridSnapshot>();

    void Start()
    {
        StartCoroutine(GenerateWorldRoutine());
    }

    IEnumerator GenerateWorldRoutine()
    {
        if (UseRandomSeed) Seed = Random.Range(0, 100000);
        prng = new System.Random(Seed);
        Debug.Log($"Generating World with Seed: {Seed}");

        propagationQueue = new Queue<Vector2Int>();
        InstantiatedObjects = new GameObject[WorldSize, WorldSize];
        historyStack.Clear();

        InitializeWorld();
        
        // Yield a bit to ensure initialization is done
        yield return null; 
        
        StructurePrepass();

        // Run the main WFC loop with Coroutine yields
        yield return StartCoroutine(RunWFC());
        
        Debug.Log("World Generation Complete!");
    }

    #region CORE WFC & BACKTRACKING

    IEnumerator RunWFC()
    {
        while (true)
        {
            Vector2Int? lowestPos = GetLowestEntropyCellPosition();
            
            // If null, the world is fully collapsed! We are done.
            if (lowestPos == null)
                break; 

            Vector2Int currentPos = lowestPos.Value;

            // 1. SAVE STATE BEFORE WE GUESS
            SaveState(currentPos);

            // 2. COLLAPSE THE CELL
            Cell cell = WorldGrid[currentPos.x, currentPos.y];
            cell.Collapse(prng);
            
            // Instantiate visually immediately
            SpawnVisualChunk(currentPos.x, currentPos.y, cell.FinalModule);

            // 3. PROPAGATE CONSTRAINTS
            propagationQueue.Clear();
            propagationQueue.Enqueue(currentPos);
            bool success = Propagate();

            // 4. CHECK FOR CONTRADICTIONS
            if (!success)
            {
                Debug.LogWarning($"Contradiction at {currentPos}. Backtracking...");
                DoBacktrack();
            }

            // Wait for the specified delay so we can watch it happen
            if (StepDelay > 0)
                yield return new WaitForSeconds(StepDelay);
            else
                yield return null; 
        }
    }

    bool Propagate()
    {
        while (propagationQueue.Count > 0)
        {
            Vector2Int currentPos = propagationQueue.Dequeue();
            Cell currentCell = WorldGrid[currentPos.x, currentPos.y];

            foreach (var dir in Directions())
            {
                int nx = currentPos.x + dir.dx;
                int ny = currentPos.y + dir.dy;

                if (!InBounds(nx, ny)) continue;

                Cell neighbor = WorldGrid[nx, ny];
                if (neighbor.IsCollapsed) continue;

                HashSet<Chunk> allowedInNeighbor = new HashSet<Chunk>();
                foreach (Chunk possibleCurrent in currentCell.PossibleModules)
                {
                    List<Chunk> validForThisDir = GetValidNeighbors(possibleCurrent, dir.dir);
                    foreach (Chunk v in validForThisDir)
                    {
                        allowedInNeighbor.Add(v);
                    }
                }

                int originalCount = neighbor.PossibleModules.Count;
                neighbor.PossibleModules.RemoveAll(m => !allowedInNeighbor.Contains(m));

                if (neighbor.PossibleModules.Count < originalCount)
                {
                    // CONTRADICTION DETECTED!
                    if (neighbor.PossibleModules.Count == 0)
                        return false; 

                    if (!propagationQueue.Contains(new Vector2Int(nx, ny)))
                        propagationQueue.Enqueue(new Vector2Int(nx, ny));
                }
            }
        }
        return true; // Propagation successful
    }

    #endregion

    #region BACKTRACKING LOGIC

    void SaveState(Vector2Int targetPos)
    {
        GridSnapshot snapshot = new GridSnapshot
        {
            AttemptedPos = targetPos,
            GridCopy = new Cell[WorldSize, WorldSize]
        };

        // Deep copy the grid
        for (int x = 0; x < WorldSize; x++)
        {
            for (int y = 0; y < WorldSize; y++)
            {
                snapshot.GridCopy[x, y] = WorldGrid[x, y].Clone();
            }
        }

        historyStack.Push(snapshot);
    }

    void DoBacktrack()
    {
        if (historyStack.Count == 0)
        {
            Debug.LogError("Critical Error: Ran out of history to backtrack. World generation failed.");
            StopAllCoroutines();
            return;
        }

        GridSnapshot lastSafeState = historyStack.Pop();
        Chunk failedChoice = WorldGrid[lastSafeState.AttemptedPos.x, lastSafeState.AttemptedPos.y].FinalModule;

        // Restore the grid
        for (int x = 0; x < WorldSize; x++)
        {
            for (int y = 0; y < WorldSize; y++)
            {
                WorldGrid[x, y] = lastSafeState.GridCopy[x, y];

                // If a cell was collapsed but is now un-collapsed due to the rollback, destroy its visual object
                if (!WorldGrid[x, y].IsCollapsed && InstantiatedObjects[x, y] != null)
                {
                    Destroy(InstantiatedObjects[x, y]);
                    InstantiatedObjects[x, y] = null;
                }
            }
        }

        // Ban the choice that caused the failure from the restored cell
        Cell targetCell = WorldGrid[lastSafeState.AttemptedPos.x, lastSafeState.AttemptedPos.y];
        targetCell.PossibleModules.Remove(failedChoice);

        // If banning this module leaves the cell with 0 options, we need to backtrack AGAIN immediately
        if (targetCell.PossibleModules.Count == 0)
        {
            DoBacktrack();
        }
    }

    #endregion

    #region INITIALIZATION & PREPASS

    void InitializeWorld()
    {
        WorldGrid = new Cell[WorldSize, WorldSize];
        for (int x = 0; x < WorldSize; x++)
        {
            for (int y = 0; y < WorldSize; y++)
            {
                float noise = (float)prng.NextDouble() * 0.01f;
                WorldGrid[x, y] = new Cell(ChunkModuleList, noise);
            }
        }
    }

    void StructurePrepass()
    {
        int attempts = WorldSize / 2;
        for (int i = 0; i < attempts; i++)
        {
            Structure chosen = GetWeightedStructure();
            TryPlaceStructure(chosen);
        }
    }

    Structure GetWeightedStructure()
    {
        float totalWeight = StructureModuleList.Sum(s => s.SpawnWeight);
        float randomPoint = (float)prng.NextDouble() * totalWeight;

        float cumulative = 0f;
        foreach (var s in StructureModuleList)
        {
            cumulative += s.SpawnWeight;
            if (randomPoint <= cumulative) return s;
        }
        return StructureModuleList[0];
    }

    void TryPlaceStructure(Structure structure)
    {
        Vector2Int randomPos = new Vector2Int(prng.Next(0, WorldSize), prng.Next(0, WorldSize));
        List<(Vector2Int pos, Chunk chunk)> structureChunks = new List<(Vector2Int pos, Chunk chunk)>();

        foreach (Chunk chunk in structure.GetComponentsInChildren<Chunk>())
        {
            Vector3 localPos = chunk.transform.localPosition;
            int gridX = randomPos.x + Mathf.RoundToInt(localPos.x / ChunkSize);
            int gridY = randomPos.y + Mathf.RoundToInt(localPos.z / ChunkSize);

            if (!InBounds(gridX, gridY)) return;
            structureChunks.Add((new Vector2Int(gridX, gridY), chunk));
        }

        foreach (var item in structureChunks)
        {
            if (WorldGrid[item.pos.x, item.pos.y].IsCollapsed) return;
        }

        foreach (var item in structureChunks)
        {
            WorldGrid[item.pos.x, item.pos.y].ForceCollapse(item.chunk);
            SpawnVisualChunk(item.pos.x, item.pos.y, item.chunk); // Spawn visually during prepass too
            propagationQueue.Enqueue(item.pos);
        }

        Propagate(); // Propagate prepass constraints immediately
    }

    #endregion

    #region UTILITIES

    Vector2Int? GetLowestEntropyCellPosition()
    {
        float lowest = float.MaxValue;
        Vector2Int? selected = null;

        for (int x = 0; x < WorldSize; x++)
        {
            for (int y = 0; y < WorldSize; y++)
            {
                Cell c = WorldGrid[x, y];
                if (!c.IsCollapsed && c.Entropy < lowest)
                {
                    lowest = c.Entropy;
                    selected = new Vector2Int(x, y);
                }
            }
        }
        return selected;
    }

    void SpawnVisualChunk(int x, int y, Chunk module)
    {
        if (module == null || InstantiatedObjects[x,y] != null) return;

        Vector3 pos = new Vector3(x * ChunkSize, 0, y * ChunkSize);
        GameObject newChunk = Instantiate(module.gameObject, pos, Quaternion.identity, transform);
        InstantiatedObjects[x, y] = newChunk;
    }

    List<Chunk> GetValidNeighbors(Chunk candidate, string direction)
    {
        switch (direction)
        {
            case "North": return candidate.NorthValid;
            case "South": return candidate.SouthValid;
            case "East": return candidate.EastValid;
            case "West": return candidate.WestValid;
            default: return new List<Chunk>();
        }
    }

    IEnumerable<(int dx, int dy, string dir)> Directions()
    {
        yield return (0, 1, "North");
        yield return (1, 0, "East");
        yield return (0, -1, "South");
        yield return (-1, 0, "West");
    }

    bool InBounds(int x, int y)
    {
        return x >= 0 && y >= 0 && x < WorldSize && y < WorldSize;
    }

    #endregion
}

// Struct to hold our state snapshots for backtracking
public struct GridSnapshot
{
    public Vector2Int AttemptedPos;
    public Cell[,] GridCopy;
}