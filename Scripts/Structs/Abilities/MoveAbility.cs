using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct MoveAbility : ActiveAbility
{
    // Variables
    public PlayerPossessedObject ppo { get; set; }
    public MapInfo map { get; set; }
    public bool isReady { get; set; }
    public bool isUnit { get; set; }
    public bool isStructure { get; set; }
    public bool subMenuActive { get; set; }

    /* Overridden inherited functions
     * GetName()
     * GetDescription()
     * SelectPreview()
     * Select()
     * ChangePos(TileInfo tile)
     * Use()
     */

    public string GetName() { return "Move"; }

    public string GetDescription() { return "Moves the unit or structure along a path"; }

    public bool IsValid() { return true; }

    // Create preview of moveable area
    public void SelectPreview()
    {
        ppo.SetHighlights(GetTilesInMoveRange(ppo.GetTile()));
        foreach (TileInfo t in ppo.GetHighlights())
        {
            t.EnableHighlightMoveable(ppo.GetPlayer());
        }
    }

    // Mark moveable locations as thick moveable highlights
    public bool Select()
    {
        ppo.GetUI().RemoveActionMenu();

        List<TileInfo> thickHighlights = new List<TileInfo>();
        foreach (TileInfo t in ppo.GetHighlights())
        {
            thickHighlights.Add(t);
            t.EnableThickHighlightMoveable(ppo.GetPlayer());
        }

        List<TileInfo> curTile = new List<TileInfo>();
        curTile.Add(ppo.GetTile());
        ppo.SetAbilityIndicators(curTile);
        MoveIndicatorDisplay();

        return false;
    }

    // Alter the move path of the object
    public void ChangePos(TileInfo tile)
    {
        List<TileInfo> highlights = ppo.GetHighlights();
        List<TileInfo> thickHighlights = ppo.GetThickHighlights();
        List<TileInfo> abilityIndicators = ppo.GetAbilityIndicators();

        if (!highlights.Contains(tile))  // If the new space is not valid, do nothing
        {
            return;
        }

        bool directlyPathable = false;
        // First checks if the new position is adjacent and isn't already in the path
        if (((tile.GetPosX() == abilityIndicators[abilityIndicators.Count - 1].GetPosX() && (tile.GetPosY() == abilityIndicators[abilityIndicators.Count - 1].GetPosY() - 1 || tile.GetPosY() == abilityIndicators[abilityIndicators.Count - 1].GetPosY() + 1))
            || (tile.GetPosY() == abilityIndicators[abilityIndicators.Count - 1].GetPosY() && (tile.GetPosX() == abilityIndicators[abilityIndicators.Count - 1].GetPosX() - 1 || tile.GetPosX() == abilityIndicators[abilityIndicators.Count - 1].GetPosX() + 1)))
            && !abilityIndicators.Contains(tile)
            && thickHighlights.Contains(tile))
        {
            directlyPathable = ppo.CheckPathable(abilityIndicators[abilityIndicators.Count - 1], tile);
        }

        // Then either retrace 1 step in the path, continue the path, or rebuild the path
        bool pathAltered = false;
        if (abilityIndicators.Count > 1)
        {
            if (abilityIndicators[abilityIndicators.Count - 2] == map.GetTile(tile.GetPosX(), tile.GetPosY()))
            {
                abilityIndicators[abilityIndicators.Count - 1].RemoveIndicator();
                abilityIndicators.Remove(abilityIndicators[abilityIndicators.Count - 1]);
                pathAltered = true;
            }
        }

        bool somethingInTheWay = false;
        if (!tile.GetFog())  // If an enemy unit is in the way
        {
            if (tile.GetUnit() != null)
            {
                if (tile.GetUnit().GetPlayerNum() != ppo.GetPlayerNum())
                {
                    somethingInTheWay = true;
                }
            }
        }
        if (tile.HasResources()) { somethingInTheWay = true; }  // Resources block pathing

        if (directlyPathable && !pathAltered)
        {
            abilityIndicators.Add(tile);
        }
        else if (!pathAltered && highlights.Contains(tile))
        {
            if (!somethingInTheWay)
            {
                TileInfo startTile = abilityIndicators[0];
                foreach (TileInfo t in abilityIndicators)  // Clear out and remove the ability indicators
                {
                    t.RemoveIndicator();
                }

                abilityIndicators.Clear();

                if (startTile != tile)
                {
                    List<TileInfo> path = BuildPath(startTile, tile);
                    foreach (TileInfo t in path)
                    {
                        abilityIndicators.Add(t);
                    }
                }
                else
                {
                    abilityIndicators.Add(startTile);
                }
            }
        }

        MoveIndicatorDisplay();

        // Refresh the highlights
        foreach (TileInfo t in thickHighlights)  // Clear and remove thick highlights
        {
            t.DisableHighlight();
        }

        thickHighlights.Clear();
        foreach (TileInfo t in highlights)
        {
            t.EnableHighlightMoveable(ppo.GetPlayer());
        }
        // With the path created, find the remaining pathable zone you can reach if there is any
        List<TileInfo> p = new List<TileInfo>();
        foreach (TileInfo t in abilityIndicators)
        {
            p.Add(t);
        }

        if (!somethingInTheWay)
        {
            thickHighlights = GetTilesInMoveRange(tile, p);
            for (int i = 0; i < thickHighlights.Count; i++)
            {
                if (i != 0)
                {
                    thickHighlights[i].EnableThickHighlightMoveable(ppo.GetPlayer());
                }
            }
        }

        ppo.SetHighlights(highlights);
        ppo.SetThickHighlights(thickHighlights);
        ppo.SetAbilityIndicators(abilityIndicators);
    }

    public bool Use(TileInfo tile)
    {
        List<TileInfo> abilityIndicators = ppo.GetAbilityIndicators();

        // Determine the new location of this unit
        int destinationIndex = 0;
        for (int i = 0; i < abilityIndicators.Count; i++)
        {
            if (abilityIndicators[i].GetUnit() != null)
            {
                if (abilityIndicators[i].GetUnit().GetPlayerNum() != ppo.GetPlayerNum())
                {
                    break;
                }
            }
            else
            {
                destinationIndex = i;
            }
        }
        // Remove fog for the path leading up to the destination
        for (int i = 0; i < destinationIndex; i++)
        {
            abilityIndicators[i].DisableFog();
        }

        TileInfo oldTile = ppo.GetTile();
        ppo.SetCurTile(abilityIndicators[destinationIndex]);  // First locally set the new tile for vision functions
        if (isUnit)
        {
            oldTile.Rpc_SetUnit(null);  // Remove this unit from the previous tile
            abilityIndicators[destinationIndex].Rpc_SetUnit((Unit)ppo);  // Set the new tile's unit to this unit
        }
        else
        {
            oldTile.Rpc_SetStructure(null);  // Remove this structure from the previous tile
            abilityIndicators[destinationIndex].Rpc_SetStructure((Structure)ppo);  // Set the new tile's structure to this unit
        }
        ppo.Rpc_SetTransform(abilityIndicators[destinationIndex].GetWorldPos());  // Physically move this unit to the new tile
        ppo.Rpc_RevealIfSeen();

        // Set new vision gained from moving the unit
        ppo.DisableFogByVision();

        // Set this ability as used
        isReady = false;

        return true;
    }

    /* Helper Functions
     * GetTilesInMoveRange();
     * MoveIndicatorDisplay();
     * BuildPath();
     */
    // Creates a preview of how far this unit can move
    public List<TileInfo> GetTilesInMoveRange(TileInfo start, List<TileInfo> curPath = null)
    {
        List<TileInfo> seenTiles = new List<TileInfo>();
        seenTiles.Add(start);
        int moveRemaining;
        if (curPath == null)
        {
            moveRemaining = ppo.GetMoveRange();
        }
        else
        {
            moveRemaining = ppo.GetMoveRange() - curPath.Count + 1;
        }

        int jPrev = 0;
        for (int i = moveRemaining; i > 0; i--)
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
                        if (curPath.Contains(map.GetTile(x, upY)))
                        {
                            isAlreadyInPath = true;
                        }
                    }
                    if (!seenTiles.Contains(map.GetTile(x, upY)) && !isAlreadyInPath)
                    {
                        if (ppo.CheckPathable(map.GetTile(x, y), map.GetTile(x, upY)))
                        {
                            seenTiles.Add(map.GetTile(x, upY));
                        }
                    }
                }
                // Check left
                if (leftX >= 0)
                {
                    bool isAlreadyInPath = false;
                    if (curPath != null)
                    {
                        if (curPath.Contains(map.GetTile(leftX, y)))
                        {
                            isAlreadyInPath = true;
                        }
                    }
                    if (!seenTiles.Contains(map.GetTile(leftX, y)) && !isAlreadyInPath)
                    {
                        if (ppo.CheckPathable(map.GetTile(x, y), map.GetTile(leftX, y)))
                        {
                            seenTiles.Add(map.GetTile(leftX, y));
                        }
                    }
                }
                // Check right
                if (rightX <= map.GetMaxX())
                {
                    bool isAlreadyInPath = false;
                    if (curPath != null)
                    {
                        if (curPath.Contains(map.GetTile(rightX, y)))
                        {
                            isAlreadyInPath = true;
                        }
                    }
                    if (!seenTiles.Contains(map.GetTile(rightX, y)) && !isAlreadyInPath)
                    {
                        if (ppo.CheckPathable(map.GetTile(x, y), map.GetTile(rightX, y)))
                        {
                            seenTiles.Add(map.GetTile(rightX, y));
                        }
                    }
                }
                // Check down
                if (downY <= map.GetMaxY())
                {
                    bool isAlreadyInPath = false;
                    if (curPath != null)
                    {
                        if (curPath.Contains(map.GetTile(x, downY)))
                        {
                            isAlreadyInPath = true;
                        }
                    }
                    if (!seenTiles.Contains(map.GetTile(x, downY)) && !isAlreadyInPath)
                    {
                        if (ppo.CheckPathable(map.GetTile(x, y), map.GetTile(x, downY)))
                        {
                            seenTiles.Add(map.GetTile(x, downY));
                        }
                    }
                }
            }
            jPrev = jMax;
        }

        return seenTiles;
    }

    // Display a move path
    private void MoveIndicatorDisplay()
    {
        List<TileInfo> abilityIndicators = ppo.GetAbilityIndicators();
        // First clear the previous path's images
        foreach (TileInfo p in abilityIndicators)
        {
            p.RemoveIndicator();
        }

        // Special case for a path of only size 1
        if (abilityIndicators.Count == 1)
        {
            abilityIndicators[0].RemoveIndicator();
        }
        else
        {
            // Display the entire path
            for (int i = 0; i < abilityIndicators.Count; i++)
            {
                int presX = abilityIndicators[i].GetPosX();
                int presY = abilityIndicators[i].GetPosY();
                if (i == 0)  // The first tile in the path
                {
                    int postX = abilityIndicators[i + 1].GetPosX();
                    int postY = abilityIndicators[i + 1].GetPosY();
                    if (postY < presY)
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "ConnectUp");
                    }
                    else if (postX < presX)
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "ConnectLeft");
                    }
                    else if (postX > presX)
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "ConnectRight");
                    }
                    else if (postY > presY)
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "ConnectDown");
                    }
                    else
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "Start");
                    }
                }
                else if (i == abilityIndicators.Count - 1)  // The last tile in the path
                {
                    int prevX = abilityIndicators[i - 1].GetPosX();
                    int prevY = abilityIndicators[i - 1].GetPosY();
                    if (prevY > presY)
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "ArriveFromBelow");
                    }
                    else if (prevX > presX)
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "ArriveFromRight");
                    }
                    else if (prevX < presX)
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "ArriveFromLeft");
                    }
                    else if (prevY < presY)
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "ArriveFromAbove");
                    }
                    else
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "Start");
                    }
                }
                else  // Tiles in the middle of the path
                {
                    int prevX = abilityIndicators[i - 1].GetPosX();
                    int prevY = abilityIndicators[i - 1].GetPosY();
                    int postX = abilityIndicators[i + 1].GetPosX();
                    int postY = abilityIndicators[i + 1].GetPosY();
                    if ((prevY < presY && postX < presX) || (prevX < presX && postY < presY))
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "ConnectUpLeft");
                    }
                    else if ((prevY < presY && postX > presX) || (prevX > presX && postY < presY))
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "ConnectUpRight");
                    }
                    else if ((prevY < presY && postY > presY) || (prevY > presY && postY < presY))
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "ConnectUpDown");
                    }
                    else if ((prevX < presX && postX > presX) || (prevX > presX && postX < presX))
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "ConnectLeftRight");
                    }
                    else if ((prevX < presX && postY > presY) || (prevY > presY && postX < presX))
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "ConnectLeftDown");
                    }
                    else if ((prevX > presX && postY > presY) || (prevY > presY && postX > presX))
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "ConnectRightDown");
                    }
                    else
                    {
                        abilityIndicators[i].DisplayIndicator(ppo.GetPlayer(), "Start");
                    }
                }
            }
        }
    }

    // Build shortest path from one point to another
    public List<TileInfo> BuildPath(TileInfo start, TileInfo end)
    {
        // Build a path choosing any shortest path from the start point to the end point
        // First find the shortest movement required
        List<TileInfo> seenTiles = new List<TileInfo>();
        Dictionary<TileInfo, int> allTiles = new Dictionary<TileInfo, int>();
        seenTiles.Add(start);
        allTiles[start] = 0;

        int jPrev = 0;
        int moves = 0;
        bool matchFound = false;
        while (!matchFound)
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
                    if (!seenTiles.Contains(map.GetTile(x, upY)))
                    {
                        if (ppo.CheckPathable(map.GetTile(x, y), map.GetTile(x, upY)))
                        {
                            allTiles[map.GetTile(x, upY)] = moves;
                            if (map.GetTile(x, upY) == end)
                            {
                                matchFound = true;
                                break;
                            }
                            else
                            {
                                seenTiles.Add(map.GetTile(x, upY));
                            }
                        }
                    }
                }
                // Check left
                if (leftX >= 0)
                {
                    if (!seenTiles.Contains(map.GetTile(leftX, y)))
                    {
                        if (ppo.CheckPathable(map.GetTile(x, y), map.GetTile(leftX, y)))
                        {
                            allTiles[map.GetTile(leftX, y)] = moves;
                            if (map.GetTile(leftX, y) == end)
                            {
                                matchFound = true;
                                break;
                            }
                            else
                            {
                                seenTiles.Add(map.GetTile(leftX, y));
                            }
                        }
                    }
                }
                // Check right
                if (rightX <= map.GetMaxX())
                {
                    if (!seenTiles.Contains(map.GetTile(rightX, y)))
                    {
                        if (ppo.CheckPathable(map.GetTile(x, y), map.GetTile(rightX, y)))
                        {
                            allTiles[map.GetTile(rightX, y)] = moves;
                            if (map.GetTile(rightX, y) == end)
                            {
                                matchFound = true;
                                break;
                            }
                            else
                            {
                                seenTiles.Add(map.GetTile(rightX, y));
                            }
                        }
                    }
                }
                // Check down
                if (downY <= map.GetMaxY())
                {
                    if (!seenTiles.Contains(map.GetTile(x, downY)))
                    {
                        if (ppo.CheckPathable(map.GetTile(x, y), map.GetTile(x, downY)))
                        {
                            allTiles[map.GetTile(x, downY)] = moves;
                            if (map.GetTile(x, downY) == end)
                            {
                                matchFound = true;
                                break;
                            }
                            else
                            {
                                seenTiles.Add(map.GetTile(x, downY));
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
                        if (map.GetTile(x, upY) == prevTile)
                        {
                            if (ppo.CheckPathable(seenTiles[i], prevTile))
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
                        if (map.GetTile(leftX, y) == prevTile)
                        {
                            if (ppo.CheckPathable(seenTiles[i], prevTile))
                            {
                                pathStack.Push(seenTiles[i]);
                                prevTile = seenTiles[i];
                                break;
                            }
                        }
                    }
                    // Check right
                    if (rightX <= map.GetMaxX())
                    {
                        if (map.GetTile(rightX, y) == prevTile)
                        {
                            if (ppo.CheckPathable(seenTiles[i], prevTile))
                            {
                                pathStack.Push(seenTiles[i]);
                                prevTile = seenTiles[i];
                                break;
                            }
                        }
                    }
                    // Check down
                    if (downY <= map.GetMaxY())
                    {
                        if (map.GetTile(x, downY) == prevTile)
                        {
                            if (ppo.CheckPathable(seenTiles[i], prevTile))
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
}
