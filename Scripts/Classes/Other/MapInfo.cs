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
    [SerializeField] private Tilemap unitMap;
    [SerializeField] private TileInfo tileInfoPrefab;
    // Initialized at start
    private bool isTilesSet;
    private List<Unit> units;
    private int minLocalX;
    private int minLocalY;
    private int maxLocalX;
    private int maxLocalY;
    private int maxX;
    private int maxY;
    private int totalTiles;
    // Initialized at run time
    private List<List<TileInfo>> tiles;

    // Functions
    // Start Function
    public override void Spawned()
    {
        isTilesSet = false;
        units = new List<Unit>();
        InitializeLocalVariables();
        CreateTilesHost();
    }

    // Update Function
    public override void FixedUpdateNetwork()
    {
        // Set the tiles that were built in the spawn function, so each player has a reference to them
        if (!isTilesSet)
        {
            isTilesSet = TrySetTiles();
        }
    }

    // Public Functions
    // Getters
    public int GetTotalTiles() { return totalTiles; }
    public bool GetIsTilesSet() { return isTilesSet; }

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

    // Checks if the adjacent space is pathable
    public bool IsPathable(TileInfo first, TileInfo second, Unit unit)
    {
        // Up
        if (first.GetPosY() > second.GetPosY())
        {
            return CheckPathableUp(first, second, unit);
        }  // Left
        else if (first.GetPosX() > second.GetPosX())
        {
            return CheckPathableLeft(first, second, unit);
        }  // Right
        else if (first.GetPosX() < second.GetPosX())
        {
            return CheckPathableRight(first, second, unit);
        }  // Down
        else if (first.GetPosY() < second.GetPosY())
        {
            return CheckPathableDown(first, second, unit);
        }

        return false;
    }

    // Creates a preview of how far this unit can move
    public List<TileInfo> GetTilesInMoveRange(int startX, int startY, Unit unit, List<TileInfo> curPath = null)
    {
        List<TileInfo> seenTiles = new List<TileInfo>();
        seenTiles.Add(tiles[startX][startY]);
        int moveRange;
        if (curPath == null)
        {
            moveRange = unit.GetMoveRange();
        }
        else
        {
            moveRange = unit.GetMoveRange() - curPath.Count + 1;
        }

        int jPrev = 0;
        for (int i = moveRange; i > 0; i--)
        {
            int jMax = seenTiles.Count;
            for (int j = jPrev; j < jMax; j++)
            {
                // Create positional variables for easier readability
                int x = seenTiles[j].GetPosX();
                int leftX = x - 1;
                int rightX = x + 1;
                int y = seenTiles[j].GetPosY();
                int upY = y - 1;
                int downY = y + 1;

                // Check up
                if (upY >= 0)
                {
                    bool isAlreadyInPath = false;
                    if (curPath != null)
                    {
                        if (curPath.Contains(tiles[x][upY]))
                        {
                            isAlreadyInPath = true;
                        }
                    }
                    if (!seenTiles.Contains(tiles[x][upY]) && !isAlreadyInPath)
                    {
                        if (CheckPathableUp(tiles[x][y], tiles[x][upY], unit))
                        {
                            seenTiles.Add(tiles[x][upY]);
                        }
                    }
                }
                // Check left
                if (leftX >= 0)
                {
                    bool isAlreadyInPath = false;
                    if (curPath != null)
                    {
                        if (curPath.Contains(tiles[leftX][y]))
                        {
                            isAlreadyInPath = true;
                        }
                    }
                    if (!seenTiles.Contains(tiles[leftX][y]) && !isAlreadyInPath)
                    {
                        if (CheckPathableLeft(tiles[x][y], tiles[leftX][y], unit))
                        {
                            seenTiles.Add(tiles[leftX][y]);
                        }
                    }
                }
                // Check right
                if (rightX <= maxX)
                {
                    bool isAlreadyInPath = false;
                    if (curPath != null)
                    {
                        if (curPath.Contains(tiles[rightX][y]))
                        {
                            isAlreadyInPath = true;
                        }
                    }
                    if (!seenTiles.Contains(tiles[rightX][y]) && !isAlreadyInPath)
                    {
                        if (CheckPathableRight(tiles[x][y], tiles[rightX][y], unit))
                        {
                            seenTiles.Add(tiles[rightX][y]);
                        }
                    }
                }
                // Check down
                if (downY <= maxY)
                {
                    bool isAlreadyInPath = false;
                    if (curPath != null)
                    {
                        if (curPath.Contains(tiles[x][downY]))
                        {
                            isAlreadyInPath = true;
                        }
                    }
                    if (!seenTiles.Contains(tiles[x][downY]) && !isAlreadyInPath)
                    {
                        if (CheckPathableDown(tiles[x][y], tiles[x][downY], unit))
                        {
                            seenTiles.Add(tiles[x][downY]);
                        }
                    }
                }
            }
            jPrev = jMax;
        }

        return seenTiles;
    }

    // Build shortest path from one point to another
    public List<TileInfo> BuildPath(TileInfo start, TileInfo end, Unit unit)
    {
        
        // Build a path choosing any shortest path from the start point to the end point
        // First find the shortest movement required
        List<TileInfo> seenTiles = new List<TileInfo>();
        Dictionary<TileInfo, int> allTiles = new Dictionary<TileInfo, int>();
        seenTiles.Add(tiles[start.GetPosX()][start.GetPosY()]);
        allTiles[tiles[start.GetPosX()][start.GetPosY()]] = 0;

        int jPrev = 0;
        int moves = 0;
        bool matchFound = false;
        while(!matchFound)
        {
            moves++;
            matchFound = true;  // Assume we are done if we can not enter the for loop
            int jMax = seenTiles.Count;
            for (int j = jPrev; j < jMax; j++)
            {
                matchFound = false;
                // Create positional variables for easier readability
                int x = seenTiles[j].GetPosX();
                int leftX = x - 1;
                int rightX = x + 1;
                int y = seenTiles[j].GetPosY();
                int upY = y - 1;
                int downY = y + 1;

                // Check up
                if (upY >= 0)
                {
                    if (!seenTiles.Contains(tiles[x][upY]))
                    {
                        if (CheckPathableUp(tiles[x][y], tiles[x][upY], unit))
                        {
                            allTiles[tiles[x][upY]] = moves;
                            if (tiles[x][upY] == end)
                            {
                                matchFound = true;
                                break;
                            }
                            else
                            {
                                seenTiles.Add(tiles[x][upY]);
                            }
                        }
                    }
                }
                // Check left
                if (leftX >= 0)
                {
                    if (!seenTiles.Contains(tiles[leftX][y]))
                    {
                        if (CheckPathableLeft(tiles[x][y], tiles[leftX][y], unit))
                        {
                            allTiles[tiles[leftX][y]] = moves;
                            if (tiles[leftX][y] == end)
                            {
                                matchFound = true;
                                break;
                            }
                            else
                            {
                                seenTiles.Add(tiles[leftX][y]);
                            }
                        }
                    }
                }
                // Check right
                if (rightX <= maxX)
                {
                    if (!seenTiles.Contains(tiles[rightX][y]))
                    {
                        if (CheckPathableRight(tiles[x][y], tiles[rightX][y], unit))
                        {
                            allTiles[tiles[rightX][y]] = moves;
                            if (tiles[rightX][y] == end)
                            {
                                matchFound = true;
                                break;
                            }
                            else
                            {
                                seenTiles.Add(tiles[rightX][y]);
                            }
                        }
                    }
                }
                // Check down
                if (downY <= maxY)
                {
                    if (!seenTiles.Contains(tiles[x][downY]))
                    {
                        if (CheckPathableDown(tiles[x][y], tiles[x][downY], unit))
                        {
                            allTiles[tiles[x][downY]] = moves;
                            if (tiles[x][downY] == end)
                            {
                                matchFound = true;
                                break;
                            }
                            else
                            {
                                seenTiles.Add(tiles[x][downY]);
                            }
                        }
                    }
                }
            }
            jPrev = jMax;
        }

        // Retrace steps from end to start
        Stack<TileInfo> pathStack = new Stack<TileInfo>();
        pathStack.Push(end);

        moves = allTiles[end] - 1;
        TileInfo prevTile = end;
        while (moves > 0)
        {
            for (int i = 0; i < seenTiles.Count; i++)
            {
                // Check if the tile is in the correct number of moves away
                if (allTiles[seenTiles[i]] == moves)
                {
                    // See if the previous tile is adjacent to this one
                    int x = seenTiles[i].GetPosX();
                    int leftX = x - 1;
                    int rightX = x + 1;
                    int y = seenTiles[i].GetPosY();
                    int upY = y - 1;
                    int downY = y + 1;

                    // Check up
                    if (upY >= 0)
                    {
                        if (tiles[x][upY] == prevTile)
                        {
                            if (CheckPathableUp(seenTiles[i], prevTile, unit))
                            {
                                pathStack.Push(seenTiles[i]);
                                prevTile = seenTiles[i];
                                break;
                            }
                        }
                    }
                    // Check left
                    if (leftX >= 0)
                    {
                        if (tiles[leftX][y] == prevTile)
                        {
                            if (CheckPathableLeft(seenTiles[i], prevTile, unit))
                            {
                                pathStack.Push(seenTiles[i]);
                                prevTile = seenTiles[i];
                                break;
                            }
                        }
                    }
                    // Check right
                    if (rightX <= maxX)
                    {
                        if (tiles[rightX][y] == prevTile)
                        {
                            if (CheckPathableRight(seenTiles[i], prevTile, unit))
                            {
                                pathStack.Push(seenTiles[i]);
                                prevTile = seenTiles[i];
                                break;
                            }
                        }
                    }
                    // Check down
                    if (downY <= maxY)
                    {
                        if (tiles[x][downY] == prevTile)
                        {
                            if (CheckPathableDown(seenTiles[i], prevTile, unit))
                            {
                                pathStack.Push(seenTiles[i]);
                                prevTile = seenTiles[i];
                                break;
                            }
                        }
                    }
                }
            }
            moves--;
        }
        pathStack.Push(start);

        List<TileInfo> path = new List<TileInfo>();

        foreach (TileInfo p in pathStack)
        {
            path.Add(p);
        }
        
        return path;
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
                    if (cliffMap.GetTile(worldXY) != null)  // Set cliffs
                    {
                        tiles[i][j].SetCliffsByName(cliffMap.GetTile(worldXY).name);
                    }
                    else
                    {
                        tiles[i][j].SetCliffsByName("");  // Empty string used to show that the cell in this location is empty
                    }
                    // RESOURCES HERE
                    // STRUCTURES HERE
                    if (unitMap.GetTile(worldXY) != null)  // Set units
                    {
                        tiles[i][j].SetUnitByName(unitMap.GetTile(worldXY).name);
                        units.Add(tiles[i][j].GetUnit());
                    }
                }
            }
        }

        // Clear the tiles that were replaced with prefabs
        unitMap.ClearAllTiles();
    }

    // Used by host and client to grab all spawned tiles and set their information based on the tile map's drawn in tiles
    private bool TrySetTiles()
    {
        TileInfo[] tileCopies = FindObjectsOfType<TileInfo>();

        if (tileCopies.Length == totalTiles)
        {
            for (int i = 0; i < tileCopies.Length; i++)
            {
                int x = tileCopies[i].GetPosX();
                int y = tileCopies[i].GetPosY();
                tiles[x][y] = tileCopies[i];  // First attach the tile to the array
            }

            return true;
        }

        return false;
    }

    // Unit pathing helpers
    // These take two valid adjacent tiles, and applies the world rules to find out if it is pathable for a given unit to make it there
    private bool CheckPathableUp(TileInfo first, TileInfo second, Unit unit)
    {
        if (!first.GetCliffUp()
            && !second.GetCliffDown())
        {
            return true;
        }

        return false;
    }

    private bool CheckPathableLeft(TileInfo first, TileInfo second, Unit unit)
    {
        if (!first.GetCliffLeft()
            && !second.GetCliffRight())
        {
            return true;
        }

        return false;
    }

    private bool CheckPathableRight(TileInfo first, TileInfo second, Unit unit)
    {
        if (!first.GetCliffRight()
            && !second.GetCliffLeft())
        {
            return true;
        }

        return false;
    }

    private bool CheckPathableDown(TileInfo first, TileInfo second, Unit unit)
    {
        if (!first.GetCliffDown()
            && !second.GetCliffUp())
        {
            return true;
        }

        return false;
    }
}
