using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct TrainAdvancedAbility : ActiveAbility
{
    // Variables
    public PlayerPossessedObject ppo { get; set; }
    public MapInfo map { get; set; }
    public bool isReady { get; set; }
    public bool isUnit { get; set; }
    public bool isStructure { get; set; }
    public bool subMenuActive { get; set; }
    private int selection;  // This is used to hold the selected unit from the submenu
    private int resourceCost;
    private bool tierReached;

    // Functions
    public string GetName() { return "Train Advanced"; }
    public string GetDescription() { return "Spend resources to spawn an advanced military unit"; }
    public bool IsValid()
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

        // This is what will contain valid locations to deploy a unit
        List<TileInfo> highlights = new List<TileInfo>();

        // Simplified variables for adjacent tiles
        int x = ppo.GetTile().GetPosX();
        int y = ppo.GetTile().GetPosY();
        int up = y - 1;
        int left = x - 1;
        int right = x + 1;
        int down = y + 1;

        // Include the space the production building is on
        if (ppo.GetTile().GetUnit() == null)
        {
            highlights.Add(ppo.GetTile());
        }
        // Fill valid adjacent spaces
        if (up >= 0)
        {
            if (ppo.CheckPathable(ppo.GetTile(), map.GetTile(x, up)) && map.GetTile(x, up).GetUnit() == null)
            {
                highlights.Add(map.GetTile(x, up));
            }
        }
        if (left >= 0)
        {
            if (ppo.CheckPathable(ppo.GetTile(), map.GetTile(left, y)) && map.GetTile(left, y).GetUnit() == null)
            {
                highlights.Add(map.GetTile(left, y));
            }
        }
        if (right <= map.GetMaxX())
        {
            if (ppo.CheckPathable(ppo.GetTile(), map.GetTile(right, y)) && map.GetTile(right, y).GetUnit() == null)
            {
                highlights.Add(map.GetTile(right, y));
            }
        }
        if (down <= map.GetMaxY())
        {
            if (ppo.CheckPathable(ppo.GetTile(), map.GetTile(x, down)) && map.GetTile(x, down).GetUnit() == null)
            {
                highlights.Add(map.GetTile(x, down));
            }
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

            // Then create a menu of units
            List<string> menuList = new List<string>();
            // Harasser
            if (ppo.GetPlayerController().GetNumBases() < 3)
            {
                menuList.Add("(40 Harasser)");
            }
            else if (ppo.GetPlayerController().GetResources() < 40)
            {
                menuList.Add("(40) Harasser");
            }
            else
            {
                menuList.Add("40 Harasser");
            }
            // Destroyer
            if (ppo.GetPlayerController().GetNumBases() < 3)
            {
                menuList.Add("(50 Destroyer)");
            }
            else if (ppo.GetPlayerController().GetResources() < 50)
            {
                menuList.Add("(50) Destroyer");
            }
            else
            {
                menuList.Add("50 Destroyer");
            }
            // Powerhouse
            if (ppo.GetPlayerController().GetNumBases() < 4)
            {
                menuList.Add("(75 Powerhouse)");
            }
            else if (ppo.GetPlayerController().GetResources() < 75)
            {
                menuList.Add("(75) Powerhouse");
            }
            else
            {
                menuList.Add("75 Powerhouse");
            }

            // Create the submenu
            ppo.GetUI().CreateCustomMenu(menuList, ppo.GetTile());
            subMenuActive = true;
        }
        else
        {
            // Selecting a unit to spawn
            selection = ppo.GetUI().GetSelection();

            // Remove the menu
            ppo.GetUI().RemoveActionMenu();
            subMenuActive = false;

            // Since the tile the building is on is sometimes a valid space, check if it should become a thick highlight
            ChangePos(ppo.GetTile());
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
        string uName = "";
        switch (selection)
        {
            case 0:  // Harasser
                resourceCost = 40;
                if (ppo.GetPlayerController().GetNumBases() >= 3) { tierReached = true; }
                tierReached = true;
                uName = "Harasser";
                break;
            case 1:  // Destroyer
                resourceCost = 50;
                if (ppo.GetPlayerController().GetNumBases() >= 3) { tierReached = true; }
                tierReached = true;
                uName = "Destroyer";
                break;
            case 2:  // Powerhouse
                resourceCost = 75;
                if (ppo.GetPlayerController().GetNumBases() >= 4) { tierReached = true; }
                uName = "Powerhouse";
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
        thickHighlights[0].Rpc_SetUpSpawnUnit(uName, ppo.GetPlayerNum());  // This ability can only be used by units

        return true;
    }
}
