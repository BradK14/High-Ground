using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainBuilderAbility : ActiveAbility
{
    public PlayerPossessedObject ppo { get; set; }
    public MapInfo map { get; set; }
    public bool isReady { get; set; }
    public bool isUnit { get; set; }
    public bool isStructure { get; set; }
    public bool subMenuActive { get; set; }
    private int resourceCost = 10;
    
    public string GetName()
    {
        if (IsValid())
        {
            return resourceCost.ToString() + " Builder";
        }

        return "(" + resourceCost.ToString() + ") Builder";
    }
    public string GetDescription() { return "Trains a Builder"; }
    public bool IsValid()
    {
        if (ppo.GetPlayerController().GetResources() >= resourceCost)
        {
            return true;
        }

        return false;
    }

    public void SelectPreview()
    {
        // This is what will contain valid locations to deploy a builder
        List<TileInfo> highlights = new List<TileInfo>();

        // Simplified variables for adjacent tiles
        int x = ppo.GetTile().GetPosX();
        int y = ppo.GetTile().GetPosY();
        int up = y - 1;
        int left = x - 1;
        int right = x + 1;
        int down = y + 1;

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
        if (!IsValid()) { return false; }  // Needs enough resources

        List<TileInfo> thickHighlights = ppo.GetThickHighlights();

        // Check if a valid space is readied
        if (thickHighlights.Count == 0)
        {
            return false;
        }

        // Spend resources and create a builder at the chosen location
        ppo.GetPlayerController().SpendResources(resourceCost);

        thickHighlights[0].Rpc_SetUpSpawnUnit("Builder", ppo.GetPlayerNum());

        return true;
    }
}
