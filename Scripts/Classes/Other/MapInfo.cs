using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapInfo : NetworkBehaviour
{
    // Variables
    // Initialized in inspector
    [SerializeField] private Tilemap groundMap;
    [SerializeField] private Tilemap cliffMap;
    [SerializeField] private Tilemap resourceMap;
    [SerializeField] private Tilemap structureMap;
    [SerializeField] private Tilemap unitMap;
    [SerializeField] private TileInfo tileInfoPrefab;
    // Initialized at start
    private bool setupComplete;
    private int minLocalX;
    private int minLocalY;
    private int maxLocalX;
    private int maxLocalY;
    private int maxX;
    private int maxY;
    private int totalTiles;
    private List<List<TileInfo>> tiles;

    // Functions
    // Start Function
    public override void Spawned()
    {
        setupComplete = false;
        InitializeLocalVariables();
        CreateTilesHost();
        StartCoroutine(Setup());
    }

    // Coroutines
    private IEnumerator Setup()
    {
        bool isTilesSet = false;
        while (!isTilesSet)
        {
            TileInfo[] tileCopies = FindObjectsOfType<TileInfo>();

            if (tileCopies.Length == totalTiles)  // Wait for tiles to finish spawning
            {
                for (int i = 0; i < tileCopies.Length; i++)
                {
                    int x = tileCopies[i].GetPosX();
                    int y = tileCopies[i].GetPosY();
                    tiles[x][y] = tileCopies[i];  // First attach the tile to the array
                }
                isTilesSet = true;
            }

            yield return null;
        }

        setupComplete = true;
    }

    // Public Functions
    // Getters
    public bool SetupIsComplete() { return setupComplete; }
    public int GetTotalTiles() { return totalTiles; }
    public int GetMaxX() { return maxX; }
    public int GetMaxY() { return maxY; }


    // Special Accessor to the 2D array of tiles
    public TileInfo GetTile(int x, int y)
    {
        if (x < 0 || x > maxX || y < 0 || y > maxY)
        {
            return null;
        }

        return tiles[x][y];
    }

    // Exchanges a world position for this map's cell's position
    public Vector3Int GetCell(Vector2 pos)
    {
        return groundMap.WorldToCell(pos);
    }

    // Converts a cell's position on the map to game related positional information
    public Vector3Int ConvertGridPosToGamePos(Vector3Int pos)
    {
        pos.x -= minLocalX;
        pos.y -= maxLocalY;
        pos.y *= -1;
        return pos;
    }

    // Fog of war functions
    // Enables fog for the entire map
    public void CoverMapInFog()
    {
        for (int i = 0; i < tiles.Count; i++)
        {
            for (int j = 0; j < tiles[i].Count; j++)
            {
                tiles[i][j].EnableFog();
            }
        }
    }

    // Private Functions
    // Creates and initializes all the tiles, as well as finding the map's size
    private void InitializeLocalVariables()
    {
        // First get the local bounds of the grid
        // Starting with the minimum local x value
        minLocalX = 0;
        TileBase tile = groundMap.GetTile(new Vector3Int(minLocalX, 0));
        while (tile != null)
        {
            tile = groundMap.GetTile(new Vector3Int(--minLocalX, 0));
        }
        minLocalX++;

        // Maximum local x value
        maxLocalX = 0;
        tile = groundMap.GetTile(new Vector3Int(minLocalX, 0));
        while (tile != null)
        {
            tile = groundMap.GetTile(new Vector3Int(++maxLocalX, 0));
        }
        maxLocalX--;

        // Minimum local y value
        minLocalY = 0;
        tile = groundMap.GetTile(new Vector3Int(minLocalX, 0));
        while (tile != null)
        {
            tile = groundMap.GetTile(new Vector3Int(0, --minLocalY));
        }
        minLocalY++;

        // Maximum local y value
        maxLocalY = 0;
        tile = groundMap.GetTile(new Vector3Int(minLocalX, 0));
        while (tile != null)
        {
            tile = groundMap.GetTile(new Vector3Int(0, ++maxLocalY));
        }
        maxLocalY--;

        // Set user friendly maximum xy values (minimums are always 0), and the total tiles
        maxX = -1 * minLocalX + maxLocalX;
        maxY = -1 * minLocalY + maxLocalY;
        totalTiles = (maxX + 1) * (maxY + 1);

        // Set up an empty 2D list for tiles
        tiles = new List<List<TileInfo>>(maxX + 1);
        for (int i = 0; i <= maxX; i++)
        {
            tiles.Add(new List<TileInfo>(maxY + 1));
            for (int j = 0; j <= maxY; j++)
            {
                tiles[i].Add(null);
            }
        }
    }

    // Spawns all the tiles for both players to reference
    private void CreateTilesHost()
    {
        if (Runner.IsServer)
        {
            for (int i = 0; i <= maxX; i++)
            {
                for (int j = 0; j <= maxY; j++)
                {
                    // Set up needed variables
                    int localX = minLocalX + i;
                    int localY = maxLocalY - j;
                    Vector3Int worldXY = new Vector3Int(localX, localY);
                    Vector3 worldPos = groundMap.GetCellCenterWorld(worldXY);

                    // Create and initialize the tile
                    tiles[i][j] = Runner.Spawn(tileInfoPrefab);
                    tiles[i][j].SetPos(i, j, worldPos);
                    tiles[i][j].SetElevationByName(groundMap.GetTile(worldXY).name);  // Set elevation
                    if (resourceMap.GetTile(worldXY) != null) { tiles[i][j].SetResources(true); }  // Set resources
                    if (cliffMap.GetTile(worldXY) != null)  // Set cliffs
                    {
                        tiles[i][j].SetCliffsByName(cliffMap.GetTile(worldXY).name);
                    }
                    else
                    {
                        tiles[i][j].SetCliffsByName("");  // Empty string used to show that the cell in this location is empty
                    }
                    if (structureMap.GetTile(worldXY) != null)  // Set structures
                    {
                        string sName = structureMap.GetTile(worldXY).name;
                        int len = sName.Length;
                        int pNum = sName[len - 1] - '0';
                        sName = sName.Substring(0, len - 1);
                        tiles[i][j].SetStructureByName(sName, pNum);
                    }
                    if (unitMap.GetTile(worldXY) != null)  // Set units
                    {
                        
                        string uName = unitMap.GetTile(worldXY).name;
                        int len = uName.Length;
                        int pNum = uName[len - 1] - '0';  // Convert the last number of the string to an integer to be used as the player number
                        uName = uName.Substring(0, len - 1);
                        tiles[i][j].SetUnitByName(uName, pNum);
                    }
                }
            }
        }

        // Clear the tiles that were replaced with prefabs
        structureMap.ClearAllTiles();
        unitMap.ClearAllTiles();
    }

    // RPCs
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetupSetTiles(TileInfo[] tileCopies)
    {
        for (int i = 0; i < tileCopies.Length; i++)
        {
            int x = tileCopies[i].GetPosX();
            int y = tileCopies[i].GetPosY();
            tiles[x][y] = tileCopies[i];  // First attach the tile to the array
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetupComplete()
    {
        setupComplete = true;
    }
}
