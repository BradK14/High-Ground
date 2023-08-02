using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/*
 * This ability can only be used by units
 */
public struct BuildAbility : ActiveAbility
{
    // Variables
    public PlayerPossessedObject ppo { get; set; }
    public MapInfo map { get; set; }
    public bool isReady { get; set; }
    public bool isUnit { get; set; }
    public bool isStructure { get; set; }
    public bool subMenuActive { get; set; }
    private int selection;  // This is used to hold the selected structure from the submenu
    private int resourceCost;
    private bool tierReached;

    // Functions
    public string GetName() { return "Build"; }
    public string GetDescription() { return "Choose a structure to build"; }

    public bool IsValid()  // If a cost is associated with this ability, tell whether or not the player has enough resources
    {
        if (tierReached && ppo.GetPlayerController().GetResources() >= resourceCost)
        {
            return true;
        }

        return false;
    }

    public void SelectPreview()
    {
        // Only do something when the submenu is active
        if (!subMenuActive)
        {
            return;
        }

        // This is what will contain valid locations
        List<TileInfo> highlights = new List<TileInfo>();

        // Simplified variables for adjacent tiles
        int x = ppo.GetTile().GetPosX();
        int y = ppo.GetTile().GetPosY();
        int up = y - 1;
        int left = x - 1;
        int right = x + 1;
        int down = y + 1;

        // Fill valid adjacent spaces
        if (ppo.GetUI().GetSelection() == 0)  // When trying to build a base
        {
            if (up >= 0)
            {
                if (map.GetTile(x, up).HasResources() && map.GetTile(x, up).GetStructure() == null)
                {
                    highlights.Add(map.GetTile(x, up));
                }
            }
            if (left >= 0)
            {
                if (map.GetTile(left, y).HasResources() && map.GetTile(left, y).GetStructure() == null)
                {
                    highlights.Add(map.GetTile(left, y));
                }
            }
            if (right <= map.GetMaxX())
            {
                if (map.GetTile(right, y).HasResources() && map.GetTile(right, y).GetStructure() == null)
                {
                    highlights.Add(map.GetTile(right, y));
                }
            }
            if (down <= map.GetMaxY())
            {
                if (map.GetTile(x, down).HasResources() && map.GetTile(x, down).GetStructure() == null)
                {
                    highlights.Add(map.GetTile(x, down));
                }
            }
        }
        else  // All other structures
        {
            if (up >= 0)
            {
                if (ppo.CheckPathable(ppo.GetTile(), map.GetTile(x, up))
                    && map.GetTile(x, up).GetStructure() == null
                    && map.GetTile(x, up).GetUnit() == null)
                {
                    highlights.Add(map.GetTile(x, up));
                }
            }
            if (left >= 0)
            {
                if (ppo.CheckPathable(ppo.GetTile(), map.GetTile(left, y))
                    && map.GetTile(left, y).GetStructure() == null
                    && map.GetTile(left, y).GetUnit() == null)
                {
                    highlights.Add(map.GetTile(left, y));
                }
            }
            if (right <= map.GetMaxX())
            {
                if (ppo.CheckPathable(ppo.GetTile(), map.GetTile(right, y))
                    && map.GetTile(right, y).GetStructure() == null
                    && map.GetTile(right, y).GetUnit() == null)
                {
                    highlights.Add(map.GetTile(right, y));
                }
            }
            if (down <= map.GetMaxY())
            {
                if (ppo.CheckPathable(ppo.GetTile(), map.GetTile(x, down))
                    && map.GetTile(x, down).GetStructure() == null
                    && map.GetTile(x, down).GetUnit() == null)
                {
                    highlights.Add(map.GetTile(x, down));
                }
            }
        }

        // First remove previous highlights
        foreach (TileInfo t in ppo.GetHighlights())
        {
            t.DisableHighlight();
        }

        // Enable moveable highlights
        foreach (TileInfo t in highlights)
        {
            t.EnableHighlightMoveable(ppo.GetPlayer());
        }

        // Save highlight info in ppo
        ppo.SetHighlights(highlights);
    }

    public bool Select()
    {
        // When first selecting this ability
        if (!subMenuActive)
        {
            // First remove the old menu
            ppo.GetUI().RemoveActionMenu();

            // Then create a menu of structures
            List<string> menuList = new List<string>();
            // Base
            if (ppo.GetPlayerController().GetResources() < 100)
            {
                menuList.Add("(100) Base");
            }
            else
            {
                menuList.Add("100 Base");
            }
            // Production
            if (ppo.GetPlayerController().GetResources() < 30)
            {
                menuList.Add("(30)Production");
            }
            else
            {
                menuList.Add("30 Production");
            }
            // Wall
            if (ppo.GetPlayerController().GetNumBases() < 2)
            {
                menuList.Add("(40 Wall)");
            }
            else if (ppo.GetPlayerController().GetResources() < 40)
            {
                menuList.Add("(40) Wall");
            }
            else
            {
                menuList.Add("40 Wall");
            }
            // Advanced Production
            if (ppo.GetPlayerController().GetNumBases() < 3)
            {
                menuList.Add("(40Production+)");
            }
            else if (ppo.GetPlayerController().GetResources() < 40)
            {
                menuList.Add("(40)Production+");
            }
            else
            {
                menuList.Add("40Production+");
            }
            // Static Defense
            if (ppo.GetPlayerController().GetNumBases() < 3)
            {
                menuList.Add("(50S.Defense)");
            }
            else if (ppo.GetPlayerController().GetResources() < 50)
            {
                menuList.Add("(50)S.Defense");
            }
            else
            {
                menuList.Add("50 S.Defense");
            }

            ppo.GetUI().CreateCustomMenu(menuList, ppo.GetTile());
            subMenuActive = true;
        }
        else
        {
            // Selecting a building to construct
            selection = ppo.GetUI().GetSelection();

            // Remove the menu
            ppo.GetUI().RemoveActionMenu();
            subMenuActive = false;
        }

        return false;  // Either way, we haven't finished with this ability yet
    }

    public void ChangePos(TileInfo tile)
    {
        List<TileInfo> highlights = ppo.GetHighlights();
        List<TileInfo> thickHighlights = ppo.GetThickHighlights();

        // Do nothing if not within a valid space
        if (!highlights.Contains(tile))
        {
            return;
        }

        // Change target deployment tile
        thickHighlights.Clear();
        thickHighlights.Add(tile);

        // Reset highlights
        foreach (TileInfo t in highlights)
        {
            t.EnableHighlightMoveable(ppo.GetPlayer());
        }

        thickHighlights[0].EnableThickHighlightMoveable(ppo.GetPlayer());
    }

    public bool Use(TileInfo tile)
    {
        // First set the resource cost, check for tier requirement, and set the name before checking if this is valid
        tierReached = false;
        string sName = "";
        switch (selection)
        {
            case 0:  // Base
                resourceCost = 100;
                tierReached = true;
                sName = "Base";
                break;
            case 1:  // Production
                resourceCost = 30;
                tierReached = true;
                sName = "ProductionA";
                break;
            case 2:  // Wall
                resourceCost = 40;
                if (ppo.GetPlayerController().GetNumBases() >= 2) { tierReached = true; }
                sName = "Wall";
                break;
            case 3:  // Advanced Production
                resourceCost = 40;
                if (ppo.GetPlayerController().GetNumBases() >= 3) { tierReached = true; }
                sName = "ProductionB";
                break;
            case 4:  // Static Defense
                resourceCost = 50;
                if (ppo.GetPlayerController().GetNumBases() >= 3) { tierReached = true; }
                sName = "StaticDefense";
                break;
        }

        // Needs enough resources
        if (!IsValid()) { return false; }

        // Location for new structure is marked by a single thick highlight
        List<TileInfo> thickHighlights = ppo.GetThickHighlights();

        // Check if a valid space is readied
        if (thickHighlights.Count == 0)
        {
            return false;
        }

        // Spend resources
        ppo.GetPlayerController().SpendResources(resourceCost);

        // Spawn the structure
        thickHighlights[0].Rpc_SetUpSpawnConstructionSite(sName, ppo.GetPlayerNum(), (Unit)ppo);  // This ability can only be used by units

        return true;
    }
}
