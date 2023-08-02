using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ActiveAbility
{
    // Variables
    public PlayerPossessedObject ppo { get; set; }
    public MapInfo map { get; set; }
    public bool isReady { get; set; }
    public bool isUnit { get; set; }
    public bool isStructure { get; set; }
    public bool subMenuActive { get; set; }

    // Functions
    // Functions shared by all structs
    public void SetObject(PlayerPossessedObject o)
    {
        ppo = o;

        if (ppo is Unit)
        {
            isUnit = true;
            isStructure = false;
        }
        else
        {
            isUnit = false;
            isStructure = true;
        }
    }
    public void SetMap(MapInfo m) { map = m; }
    public bool IsReady() { return isReady; }
    public void SetReady(bool isReady_) { isReady = isReady_; }
    // Functions to be defined by structs
    public string GetName();
    public string GetDescription();
    public bool IsValid();  // If a cost is associated with this ability, tell whether or not the player has enough resources
    public void SelectPreview();
    public bool Select();
    public void ChangePos(TileInfo tile);
    public bool Use(TileInfo tile);
}
