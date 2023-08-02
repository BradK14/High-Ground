using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct AttackAbility : ActiveAbility
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
     * ChangePos()
     * Use()
     */
    public string GetName() { return "Attack"; }
    public string GetDescription() { return "Attack an enemy"; }
    public bool IsValid() { return true; }
    public void SelectPreview()
    {
        // Don't do anything when submenu is open
        if (subMenuActive)
        {
            return;
        }

        List<TileInfo> highlights = ppo.GetHighlights();
        List<TileInfo> seenTiles = new List<TileInfo>();
        seenTiles.Add(ppo.GetTile());
        if (ppo.GetAttackMinRange() == 0) { highlights.Add(ppo.GetTile()); }  // Add the objects current tile separate from the loop

        int range = ppo.GetAttackMaxRange();
        if (ppo.GetAttackType() == "Ranged") { range++; }  // Possible 1 extra range with high ground for ranged units

        int jPrev = 0;
        for (int i = 1; i <= range; i++)
        {
            int jMax = seenTiles.Count;
            for (int j = jPrev; j < jMax; j++)
            {
                // Create positional variables for easier readability
                int neutX = seenTiles[j].GetPosX();
                int leftX = neutX - 1;
                int rightX = neutX + 1;
                int neutY = seenTiles[j].GetPosY();
                int upY = neutY - 1;
                int downY = neutY + 1;

                // Check up
                if (upY >= 0)
                {
                    if (!seenTiles.Contains(map.GetTile(neutX, upY)))
                    {
                        seenTiles.Add(map.GetTile(neutX, upY));
                        if (i >= ppo.GetAttackMinRange() && ppo.CheckAttackableUp(map.GetTile(neutX, neutY), map.GetTile(neutX, upY)))
                        {
                            if (i > ppo.GetAttackMaxRange())
                            {
                                if (map.GetTile(neutX, upY).GetElevation() < ppo.GetTile().GetElevation())
                                {
                                    highlights.Add(map.GetTile(neutX, upY));
                                }
                            }
                            else
                            {
                                highlights.Add(map.GetTile(neutX, upY));
                            }
                        }
                    }
                }
                // Check left
                if (leftX >= 0)
                {
                    if (!seenTiles.Contains(map.GetTile(leftX, neutY)))
                    {
                        seenTiles.Add(map.GetTile(leftX, neutY));
                        if (i >= ppo.GetAttackMinRange() && ppo.CheckAttackableLeft(map.GetTile(neutX, neutY), map.GetTile(leftX, neutY)))
                        {
                            if (i > ppo.GetAttackMaxRange())
                            {
                                if (map.GetTile(leftX, neutY).GetElevation() < ppo.GetTile().GetElevation())
                                {
                                    highlights.Add(map.GetTile(leftX, neutY));
                                }
                            }
                            else
                            {
                                highlights.Add(map.GetTile(leftX, neutY));
                            }
                        }
                    }
                }
                // Check right
                if (rightX <= map.GetMaxX())
                {
                    if (!seenTiles.Contains(map.GetTile(rightX, neutY)))
                    {
                        seenTiles.Add(map.GetTile(rightX, neutY));
                        if (i >= ppo.GetAttackMinRange() && ppo.CheckAttackableRight(map.GetTile(neutX, neutY), map.GetTile(rightX, neutY)))
                        {
                            if (i > ppo.GetAttackMaxRange())
                            {
                                if (map.GetTile(rightX, neutY).GetElevation() < ppo.GetTile().GetElevation())
                                {
                                    highlights.Add(map.GetTile(rightX, neutY));
                                }
                            }
                            else
                            {
                                highlights.Add(map.GetTile(rightX, neutY));
                            }
                        }
                    }
                }
                // Check down
                if (downY <= map.GetMaxY())
                {
                    if (!seenTiles.Contains(map.GetTile(neutX, downY)))
                    {
                        seenTiles.Add(map.GetTile(neutX, downY));
                        if (i >= ppo.GetAttackMinRange() && ppo.CheckAttackableDown(map.GetTile(neutX, neutY), map.GetTile(neutX, downY)))
                        {
                            if (i > ppo.GetAttackMaxRange())
                            {
                                if (map.GetTile(neutX, downY).GetElevation() < ppo.GetTile().GetElevation())
                                {
                                    highlights.Add(map.GetTile(neutX, downY));
                                }
                            }
                            else
                            {
                                highlights.Add(map.GetTile(neutX, downY));
                            }
                        }
                    }
                }
            }
            jPrev = jMax;
        }

        // Attack range set, enable highlights
        foreach (TileInfo t in highlights)
        {
            t.EnableHighlightAttackable(ppo.GetPlayer());
        }
        ppo.SetHighlights(highlights);
    }

    public bool Select()
    {
        ppo.GetUI().RemoveActionMenu();

        if (subMenuActive)
        {
            if (ppo.GetUI().GetSelection() == 0)
            {
                ppo.GetThickHighlights()[0].GetUnit().TakeDamage(ppo.GetAttack());
            }
            else
            {
                ppo.GetThickHighlights()[0].GetStructure().TakeDamage(ppo.GetAttack());
            }

            subMenuActive = false;

            return true;
        }

        // If there is no menu, then check if there is an attackable object on the same space as the object if the object has a 0 min range
        ChangePos(ppo.GetTile());  // ChangePos function handles this nicely

        return false;
    }  

    public void ChangePos(TileInfo tile)
    {
        List<TileInfo> highlights = ppo.GetHighlights();
        List<TileInfo> thickHighlights = ppo.GetThickHighlights();

        // No position change
        if (thickHighlights.Contains(tile))
        {
            return;
        }

        // If it is inside the attack range and an enemy is present, highlight it thicker
        if (highlights.Contains(tile) && (tile.GetUnit() != null || tile.GetStructure() != null))
        {
            // First determine if the tile is a valid new attack location
            bool tileAttackable = false;
            if (tile.GetUnit() != null)
            {
                if (tile.GetUnit().GetPlayerNum() != ppo.GetPlayerNum())
                {
                    tileAttackable = true;
                }
            }
            if (tile.GetStructure() != null)
            {
                if (tile.GetStructure().GetPlayerNum() != ppo.GetPlayerNum())
                {
                    tileAttackable = true;
                }
            }

            // Then change the attack location to the new location
            if (tileAttackable)
            {
                // Replace current thick highlights with normal ones
                List<TileInfo> tiles = new List<TileInfo>();
                foreach (TileInfo t in thickHighlights)
                {
                    tiles.Add(t);
                }

                foreach (TileInfo t in thickHighlights)
                {
                    t.DisableHighlight();
                }

                thickHighlights.Clear();
                foreach (TileInfo t in tiles)
                {
                    t.EnableHighlightAttackable(ppo.GetPlayer());
                }

                // Then highlight the new selection
                thickHighlights.Add(tile);
                tile.EnableThickHighlightAttackable(ppo.GetPlayer());
            }
        }

        ppo.SetHighlights(highlights);
        ppo.SetThickHighlights(thickHighlights);
    }

    public bool Use(TileInfo tile)
    {
        List<TileInfo> thickHighlights = ppo.GetThickHighlights();

        // If there is no valid target, do nothing
        if (thickHighlights.Count == 0)
        {
            return false;
        }

        bool enemyUnit = false;
        bool enemyStructure = false;
        if (thickHighlights[0].GetUnit() != null)
        {
            if (thickHighlights[0].GetUnit().GetPlayerNum() != ppo.GetPlayerNum())
            {
                enemyUnit = true;
            }
        }
        if (thickHighlights[0].GetStructure() != null)
        {
            if (thickHighlights[0].GetStructure().GetPlayerNum() != ppo.GetPlayerNum())
            {
                enemyStructure = true;
            }
        }

        // Deal damage to the selected unit or structure
        if (enemyUnit && enemyStructure)
        {
            // Create a selection menu to decide between attacking the unit or the structure
            ppo.GetUI().CreateUnitStructureMenu(ppo.GetTile());
            subMenuActive = true;
            return false;
        }
        else if (enemyUnit)
        {
            thickHighlights[0].GetUnit().TakeDamage(ppo.GetAttack());
            isReady = false;
        }
        else if (enemyStructure)
        {
            thickHighlights[0].GetStructure().TakeDamage(ppo.GetAttack());
            isReady = false;
        }

        return true;
    }
}
