using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BaseUI : MonoBehaviour
{
    // Variables
    [SerializeField] private NetworkRunnerSpawner netRunner;
    [SerializeField] private GameObject inGameUI;
    [SerializeField] private Button endTurn;
    [SerializeField] private TextMeshProUGUI endTurnText;
    [SerializeField] private TextMeshProUGUI unitName;
    [SerializeField] private TextMeshProUGUI unitType;
    [SerializeField] private TextMeshProUGUI unitHP;
    [SerializeField] private TextMeshProUGUI unitVision;
    [SerializeField] private TextMeshProUGUI unitMovement;
    [SerializeField] private TextMeshProUGUI unitAttack;
    [SerializeField] private TextMeshProUGUI unitDescription;
    [SerializeField] private TextMeshProUGUI structureName;
    [SerializeField] private TextMeshProUGUI structureType;
    [SerializeField] private TextMeshProUGUI structureHP;
    [SerializeField] private TextMeshProUGUI structureVision;
    [SerializeField] private TextMeshProUGUI structureMovement;
    [SerializeField] private TextMeshProUGUI structureDescription;
    [SerializeField] private TextMeshProUGUI terrainResources;
    [SerializeField] private TextMeshProUGUI terrainElevation;
    [SerializeField] private TextMeshProUGUI terrainVisible;
    [SerializeField] private TextMeshProUGUI terrainCliffUp;
    [SerializeField] private TextMeshProUGUI terrainCliffLeft;
    [SerializeField] private TextMeshProUGUI terrainCliffRight;
    [SerializeField] private TextMeshProUGUI terrainCliffDown;
    private PlayerController player;
    private TileInfo curTileInfo;
    private bool endTurnActive;

    // Functions
    // Start
    private void Start()
    {
        player = null;
        curTileInfo = null;
        endTurn.onClick.AddListener(() => EndTurn());
        endTurnActive = false;
    }

    // Update
    private void Update()
    {
        // First wait till a player controller has been created
        if (player == null)
        {
            if (netRunner.GetRunner() != null)
            {
                PlayerController[] players = FindObjectsOfType<PlayerController>();
                foreach (PlayerController p in players)
                {
                    // Stupid Visual Studios or Unity or whatever thinks this is something different than what it is
#pragma warning disable CS0618 // Type or member is obsolete
                    if (p.GetPlayerRef() == netRunner.GetRunner())
#pragma warning restore CS0618 // Type or member is obsolete
                    {
                        player = p;
                        inGameUI.SetActive(true);
                        break;
                    }
                }
            }
        }  // Check if it is the player's turn
        else if (player.GetIsMyTurn())
        {
            // If it is the start of the turn, reactivate the end turn button and reset side panel info
            if (!endTurnActive)
            {
                endTurnActive = true;

                EnableEndTurn(true);

                if (curTileInfo != null)
                {
                    SetSidePanelInfo();
                }
            }

            // Wait until the player's cursor changes tiles, and reset UI info each time
            if (curTileInfo != player.GetCurTile())
            {
                curTileInfo = player.GetCurTile();
                if (curTileInfo != null)
                {
                    SetSidePanelInfo();
                }
            }
        }
        else if (endTurnActive)
        {
            endTurnActive = false;

            // Disable end turn button
            EnableEndTurn(false);

            // Remove side panel info
            SetSidePanelInfo(false);
            
        }
    }

    // Private Functions
    // Reset Side Panel info
    private void SetSidePanelInfo(bool setInfo = true)
    {
        // Unit Info
        if (curTileInfo.GetUnit() != null && setInfo)
        {
            Unit unit = curTileInfo.GetUnit();
            unitName.text = unit.GetUnitName();
            unitType.text = unit.GetUnitType();
            //unitImage = unit.GetSprite();
            unitHP.text = "Health: " + unit.GetHP().ToString() + '/' + unit.GetMaxHP().ToString();
            unitVision.text = "Vision: " + unit.GetVisionRange().ToString();
            unitMovement.text = "Move: " + unit.GetMoveRange().ToString();
            // Attack text varies based on attack type
            unitAttack.text = unit.GetAttackType() + ": " + unit.GetAttack().ToString();
            if (unit.GetAttackType() == "Ranged")
            {
                unitAttack.text += '\n' + "Range: (" + unit.GetAttackMinRange().ToString() + '-' + unit.GetAttackMaxRange().ToString() + ')';
            }
            unitDescription.text = unit.GetDescription();
        }
        else
        {
            unitName.text = "";
            unitType.text = "";
            //unitImage = NO
            unitHP.text = "";
            unitVision.text = "";
            unitMovement.text = "";
            unitAttack.text = "";
            unitDescription.text = "";
        }

        // Structure Info
        /*
        if (curTileInfo.GetStructure() != null && setInfo)
        {
            Structure structure = curTileInfo.GetStructure();
            structureName.text = structure.GetStructureName();
            structureType.text = structure.GetStructureType();
            structureImage = structure.GetSprite();
            structureHP.text = "Health: " + structure.GetHP().ToString() + '/' + structure.GetMaxHP().ToString();
            structureVision.text = "Vision: " + structure.GetVisionRange().ToString();
            //structureMovement.text = "";
            structureDescription.text = structure.GetDescription();
        }
        else
        {
            structureName.text = "";
            structureType.text = "";
            structureImage = NO
            structureHP.text = "";
            structureVision.text = "";
            structureMovement.text = "";
            structureDescription.text = "";
        }
        */

        // Terrain Info
        if (!setInfo)
        {
            //terrainResources.text = ;
            terrainElevation.text = "";
            //terrainVisible.text = ;
            terrainCliffUp.text = "";
            terrainCliffLeft.text = "";
            terrainCliffRight.text = "";
            terrainCliffDown.text = "";
        }
        else
        {
            terrainResources.text = "";
            terrainElevation.text = "Elevation: " + curTileInfo.GetElevation().ToString();
            terrainVisible.text = "";
            terrainCliffUp.text = "Cliff Up: ";
            if (curTileInfo.GetCliffUp())
            {
                terrainCliffUp.text += "Yes";
            }
            else
            {
                terrainCliffUp.text += "No";
            }
            terrainCliffLeft.text = "Cliff Left: ";
            if (curTileInfo.GetCliffLeft())
            {
                terrainCliffLeft.text += "Yes";
            }
            else
            {
                terrainCliffLeft.text += "No";
            }
            terrainCliffRight.text = "Cliff Right: ";
            if (curTileInfo.GetCliffRight())
            {
                terrainCliffRight.text += "Yes";
            }
            else
            {
                terrainCliffRight.text += "No";
            }
            terrainCliffDown.text = "Cliff Down: ";
            if (curTileInfo.GetCliffDown())
            {
                terrainCliffDown.text += "Yes";
            }
            else
            {
                terrainCliffDown.text += "No";
            }
        }
    }

    // Enable/Disable End Turn Button
    private void EnableEndTurn(bool enable)
    {
        if (enable)
        {
            endTurn.interactable = true;
            endTurnText.color = new Vector4(0f, 0.66f, 0f, 1f);
        }
        else
        {
            endTurn.interactable = false;
            endTurnText.color = new Vector4(0f, 0f, 0f, 1f);
        }
    }

    // End Turn Button Action
    private void EndTurn()
    {
        player.EndTurnSignal();
    }
}
