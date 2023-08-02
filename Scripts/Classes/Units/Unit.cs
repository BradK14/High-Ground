using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Unit : PlayerPossessedObject
{
    // Variables
    [SerializeField] private GameObject workIndicator;
    protected Structure constructionSite;

    // Functions
    // Start
    public override void Spawned()
    {
        base.Spawned();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        isDestroyed = true;

        if (hasState)
        {
            if (Runner.IsServer)
            {
                curTile.Rpc_SetUnit(null);
                PlayerController pc = FindObjectOfType<GameManager>().GetPlayers()[playerNum];
                pc.Rpc_ResetUnitList();
                pc.Rpc_ResetVision();
            }
        }
    }

    // Coroutines
    protected override IEnumerator Setup()
    {
        StartCoroutine(base.Setup());

        while (!ppoSetupComplete) { yield return null; }  // Wait for parent routine to finish

        bool unitAssigned = false;
        while (!unitAssigned)
        {
            if (Runner.LocalPlayer == player)
            {
                if (FindObjectOfType<GameManager>().GetPlayers()[playerNum].SetupIsComplete())
                {
                    FindObjectOfType<GameManager>().GetPlayers()[playerNum].AddUnit(this);
                    unitAssigned = true;
                }
            }
            else
            {
                unitAssigned = true;
            }

            yield return null;
        }
    }

    /* Various Functions
     * TakeDamage(int damage)
     * Rpc_SetCS(Structure cs)
     * StopBuildingCS()
     */
    // Receive effects from other sources
    public override void TakeDamage(int damage)
    {
        base.TakeDamage(damage);

        if (HP <= 0)
        {
            Rpc_DestroySelf();
        }
    }

    // For builders only
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetCS(Structure cs)
    {
        constructionSite = cs;

        if (cs != null)
        {
            if (cs.GetTile().GetPosY() < curTile.GetPosY())
            {
                workIndicator.transform.localPosition = new Vector3(0f, 0.5f, 1f);
            }
            else if (cs.GetTile().GetPosX() < curTile.GetPosX())
            {
                workIndicator.transform.localPosition = new Vector3(-0.5f, 0f, 1f);
            }
            else if (cs.GetTile().GetPosX() > curTile.GetPosX())
            {
                workIndicator.transform.localPosition = new Vector3(0.5f, 0f, 1f);
            }
            else if (cs.GetTile().GetPosY() > curTile.GetPosY())
            {
                workIndicator.transform.localPosition = new Vector3(0f, -0.5f, 1f);
            }

            workIndicator.SetActive(true);
        }
        else
        {
            workIndicator.SetActive(false);
        }
    }

    // For builders only
    // Removes references between a construction site and its builder
    public void StopBuildingCS()
    {
        if (constructionSite != null)
        {
            constructionSite.Rpc_SetCSBuilder(null);
        }
        
        Rpc_SetCS(null);
    }

    // Includes an indicator for working on structures
    [Rpc(RpcSources.All, RpcTargets.All)]
    public override void Rpc_RevealIfSeen()
    {
        if (!curTile.GetFog())
        {
            sprite.enabled = true;
            HPDisplay.enabled = true;
            if (workIndicator != null) { workIndicator.GetComponent<SpriteRenderer>().enabled = true; }
        }
        else
        {
            sprite.enabled = false;
            HPDisplay.enabled = false;
            if (workIndicator != null) { workIndicator.GetComponent<SpriteRenderer>().enabled = false; }
        }
    }
}
