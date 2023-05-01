using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : NetworkBehaviour
{
    // Variables
    // Initialized in the inspector
    [SerializeField] protected Sprite color0;
    [SerializeField] protected Sprite color1;
    [SerializeField] protected Sprite color2;
    // Initialized at spawn
    [Networked] protected string unitName { get; set; }
    [Networked] protected string unitType { get; set; }
    [Networked] protected int maxHP { get; set; }
    [Networked] protected int HP { get; set; }
    [Networked] protected int visionRange { get; set; }
    [Networked] protected int moveRange { get; set; }
    [Networked] protected int attack { get; set; }
    [Networked] protected int attackMinRange { get; set; }
    [Networked] protected int attackMaxRange { get; set; }
    [Networked] protected string attackType { get; set; }
    [Networked] protected string description { get; set; }
    [Networked] protected bool moved { get; set; }
    [Networked] protected bool allActionsUsed { get; set; }
    // Set at run time
    [Networked] protected int playerNum { get; set; }

    // Functions
    // Public Functions
    // Setters
    public void SetPlayerNum(int playerNum_)
    {
        playerNum = playerNum_;
        playerNumChanged = !playerNumChanged;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetTransform(Vector3 pos) { transform.position = pos; }

    // Getters
    public int GetPlayerNum() { return playerNum; }
    public string GetUnitName() { return unitName; }
    public string GetUnitType() { return unitType; }
    public int GetMaxHP() { return maxHP; }
    public int GetHP() { return HP; }
    public int GetVisionRange() { return visionRange; }
    public int GetMoveRange() { return moveRange; }
    public int GetAttack() { return attack; }
    public int GetAttackMinRange() { return attackMinRange; }
    public int GetAttackMaxRange() { return attackMaxRange; }
    public string GetAttackType() { return attackType; }
    public string GetDescription() { return description; }
    public bool HasMoved() { return moved; }

    // Reset actions
    public void ResetActions()
    {
        Rpc_SetAllActionsUsed(false);
    }

    // Move Function (called by the playercontroller)
    public void Move(List<TileInfo> path)
    {
        moved = true;
        Rpc_SetMoved(moved);
    }

    // On Changed Functions
    [Networked(OnChanged = nameof(OnPlayerNumChanged))]
    public NetworkBool playerNumChanged { get; set; }
    public static void OnPlayerNumChanged(Changed<Unit> changed)
    {
        Dictionary<int, PlayerController> players = FindObjectOfType<GameManager>().GetPlayers();
        if (changed.Behaviour.playerNum == 0)
        {
            changed.Behaviour.GetComponent<SpriteRenderer>().sprite = changed.Behaviour.color0;
        }
#pragma warning disable CS0618 // Type or member is obsolete
        else if (changed.Behaviour.Runner.LocalPlayer == players[changed.Behaviour.playerNum].GetPlayerRef())
#pragma warning restore CS0618 // Type or member is obsolete
        {
            changed.Behaviour.GetComponent<SpriteRenderer>().sprite = changed.Behaviour.color1;
            players[changed.Behaviour.playerNum].AddUnit(changed.Behaviour);
        }
        else
        {
            changed.Behaviour.GetComponent<SpriteRenderer>().sprite = changed.Behaviour.color2;
        }
    }

    // Private Functions
    // Rpc Functions
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_SetMoved(bool moved_)
    {
        moved = moved_;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_SetAllActionsUsed(bool allActionsUsed_)
    {
        if (allActionsUsed_)
        {
            allActionsUsed = true;
        }
        else  // All action related variables need to be set as unused
        {
            allActionsUsed = false;
            moved = false;
        }
    }
}
