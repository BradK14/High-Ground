using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct RepairAbility : ActiveAbility
{
    // Variables
    public PlayerPossessedObject ppo { get; set; }
    public MapInfo map { get; set; }
    public bool isReady { get; set; }
    public bool isUnit { get; set; }
    public bool isStructure { get; set; }
    public bool subMenuActive { get; set; }

    // Functions
    public string GetName() { return "Repair"; }
    public string GetDescription() { return "Repair or continue the construction of a structure."; }
    public bool IsValid() { return true; }  // If a cost is associated with this ability, tell whether or not the player has enough resources
    
    public void SelectPreview()
    {
        // This is what will contain valid locations
        List<TileInfo> highlights = new List<TileInfo>();

        // Simplified variables for adjacent tiles
        int x = ppo.GetTile().GetPosX();
        int y = ppo.GetTile().GetPosY();
        int up = y - 1;
        int left = x - 1;
        int right = x + 1;
        int down = y + 1;

        // Check for valid repairable spaces
        if (up >= 0)
        {
            if (ppo.CheckPathable(ppo.GetTile(), map.GetTile(x, up)) || map.GetTile(x, up).HasResources())
            {
                highlights.Add(map.GetTile(x, up));
            }
        }
        if (left >= 0)
        {
            if (ppo.CheckPathable(ppo.GetTile(), map.GetTile(left, y)) || map.GetTile(left, y).HasResources())
            {
                highlights.Add(map.GetTile(left, y));
            }
        }
        if (right <= map.GetMaxX())
        {
            if (ppo.CheckPathable(ppo.GetTile(), map.GetTile(right, y)) || map.GetTile(right, y).HasResources())
            {
                highlights.Add(map.GetTile(right, y));
            }
        }
        if (down <= map.GetMaxY())
        {
            if (ppo.CheckPathable(ppo.GetTile(), map.GetTile(x, down)) || map.GetTile(x, down).HasResources())
            {
                highlights.Add(map.GetTile(x, down));
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
            t.EnableHighlightSupportable(ppo.GetPlayer());
        }

        // Save highlight info in ppo
        ppo.SetHighlights(highlights);
    }

    public bool Select()
    {
        ppo.GetUI().RemoveActionMenu();

        return false;
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

        // Change target deployment tile if new position has an allied structure
        if (tile.GetStructure() != null)
        {
            if (tile.GetStructure().GetPlayerNum() == ppo.GetPlayerNum())
            {
                thickHighlights.Clear();
                thickHighlights.Add(tile);
            }
        }

        // Reset highlights
        foreach (TileInfo t in highlights)
        {
            t.EnableHighlightSupportable(ppo.GetPlayer());
        }

        if (thickHighlights.Count > 0)
        {
            thickHighlights[0].EnableThickHighlightSupportable(ppo.GetPlayer());
        }
    }

    public bool Use(TileInfo tile)
    {
        List<TileInfo> thickHighlights = ppo.GetThickHighlights();

        // Check if a valid space is readied
        if (thickHighlights.Count == 0)
        {
            return false;
        }

        // If we are continuing the construction of a construction site or repairing a structure
        // First stop building or repairing the structure by its previous builder
        thickHighlights[0].GetStructure().StopBuildingCS();

        // Set the new builder to the one casting this ability
        Unit u = (Unit)ppo;
        thickHighlights[0].GetStructure().Rpc_SetCSBuilder(u);
        u.Rpc_SetCS(thickHighlights[0].GetStructure());

        return true;
    }
}
