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
    [SerializeField] private GameObject actionMenu;
    [SerializeField] private GameObject actionMenuSelector;
    [SerializeField] private GameObject victoryDefeat;
    [SerializeField] private Image victoryDefeatImage;
    [SerializeField] private Sprite victoryImage;
    [SerializeField] private Sprite defeatImage;
    [SerializeField] private TextMeshProUGUI actionMenuText;
    [SerializeField] private Button endTurn;
    [SerializeField] private Button surrenderButton;
    [SerializeField] private Button exitGame;
    [SerializeField] private Button endOfGameButton;
    [SerializeField] private TextMeshProUGUI endTurnText;
    [SerializeField] private TextMeshProUGUI unitName;
    [SerializeField] private TextMeshProUGUI unitType;
    [SerializeField] private Image unitImage;
    [SerializeField] private TextMeshProUGUI unitHP;
    [SerializeField] private TextMeshProUGUI unitVision;
    [SerializeField] private TextMeshProUGUI unitMovement;
    [SerializeField] private TextMeshProUGUI unitAttack;
    [SerializeField] private TextMeshProUGUI unitDescription;
    [SerializeField] private TextMeshProUGUI structureName;
    [SerializeField] private TextMeshProUGUI structureType;
    [SerializeField] private Image structureImage;
    [SerializeField] private TextMeshProUGUI structureHP;
    [SerializeField] private TextMeshProUGUI structureVision;
    [SerializeField] private TextMeshProUGUI structureMovement;
    [SerializeField] private TextMeshProUGUI structureDescription;
    [SerializeField] private Image resourceImage;
    [SerializeField] private TextMeshProUGUI terrainResources;
    [SerializeField] private TextMeshProUGUI terrainElevation;
    [SerializeField] private TextMeshProUGUI totalResources;
    private PlayerController player;
    private TileInfo curTileInfo;
    private bool endTurnActive;
    private int actionMenuSelection;
    private List<int> actionMenuSelections;

    // Functions
    // Start
    private void Start()
    {
        player = null;
        curTileInfo = null;
        endTurn.onClick.AddListener(() => EndTurn());
        surrenderButton.onClick.AddListener(() => Surrender());
        exitGame.onClick.AddListener(() => ExitGame());
        endOfGameButton.onClick.AddListener(() => ExitGame());
        endTurnActive = false;
        actionMenuSelection = 0;
        actionMenuSelections = new List<int>();
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
                    if (curTileInfo.GetFog())
                    {
                        SetSidePanelInfo(false);
                    }
                    else
                    {
                        SetSidePanelInfo();
                    }
                }
            }

            // Wait until the player's cursor changes tiles, and reset UI info each time
            if (curTileInfo != player.GetCurTile())
            {
                curTileInfo = player.GetCurTile();
                if (curTileInfo != null)
                {
                    if (curTileInfo.GetFog())
                    {
                        SetSidePanelInfo(false);
                    }
                    else
                    {
                        SetSidePanelInfo();
                    }
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

    // Public Functions
    // Victory / Defeat display
    public void DisplayVictoryDefeat(bool victory)
    {
        if (!victoryDefeat.activeInHierarchy)
        {
            surrenderButton.interactable = false;

            if (victory)
            {
                victoryDefeatImage.sprite = victoryImage;
            }
            else
            {
                victoryDefeatImage.sprite = defeatImage;
            }

            victoryDefeat.SetActive(true);
        }
    }

    // Action Menu
    public bool isActionMenuActive() { return actionMenu.activeSelf; }
    public int GetSelection() { return actionMenuSelections[actionMenuSelection]; }
    public int CreateUnitStructureMenu(TileInfo tile)  // Tile passed in for menu placement
    {
        // Fill action menu
        actionMenuText.text = "Unit\nStructure";
        actionMenuSelections.Add(0);  // 0 for Unit
        actionMenuSelections.Add(1);  // 1 for Structure

        // Relocate action menu and resize
        actionMenu.transform.position = Camera.main.WorldToScreenPoint(tile.GetWorldPos());
        actionMenu.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30 * actionMenuSelections.Count);

        // Set selector to first element
        actionMenuSelection = 0;
        int height = 30 * actionMenuSelections.Count;
        actionMenuSelector.transform.localPosition = new Vector3(actionMenuSelector.transform.localPosition.x, (height / 2) - 15);
        // Show action menu
        actionMenu.SetActive(true);

        return actionMenuSelections[actionMenuSelection];
    }

    public int CreateActionMenuForPPO(PlayerPossessedObject ppo)
    {
        List<ActiveAbility> abilities = ppo.GetActiveAbilities();

        for (int i = 0; i < abilities.Count; i++)
        {
            if (abilities[i].IsReady())
            {
                actionMenuText.text += abilities[i].GetName() + '\n';  // Fill action menu

                actionMenuSelections.Add(i);
            }
        }

        // Relocate action menu and resize
        actionMenu.transform.position = Camera.main.WorldToScreenPoint(ppo.GetTile().GetWorldPos());
        actionMenu.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30 * actionMenuSelections.Count);

        // Set selector to first element
        actionMenuSelection = 0;
        int height = 30 * actionMenuSelections.Count;
        actionMenuSelector.transform.localPosition = new Vector3(actionMenuSelector.transform.localPosition.x, (height / 2) - 15);

        // Show action menu
        actionMenu.SetActive(true);

        return actionMenuSelections[actionMenuSelection];
    }

    // Action menu created by a simple list of strings, for various uses
    public int CreateCustomMenu(List<string> menuList, TileInfo tile)
    {
        for (int i = 0; i < menuList.Count; i++)
        {
            actionMenuText.text += menuList[i] + '\n';  // Fill action menu

            actionMenuSelections.Add(i);
        }

        // Relocate action menu and resize
        actionMenu.transform.position = Camera.main.WorldToScreenPoint(tile.GetWorldPos());
        actionMenu.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 30 * actionMenuSelections.Count);

        // Set selector to first element
        actionMenuSelection = 0;
        int height = 30 * actionMenuSelections.Count;
        actionMenuSelector.transform.localPosition = new Vector3(actionMenuSelector.transform.localPosition.x, (height / 2) - 15);

        // Show action menu
        actionMenu.SetActive(true);

        return actionMenuSelections[actionMenuSelection];
    }

    // Following input, change the selection of which choice in the action menu we want
    public int ChangeSelection(bool next)
    {
        if (next)
        {
            if(++actionMenuSelection >= actionMenuSelections.Count)
            {
                actionMenuSelection = 0;
            }
        }
        else
        {
            if(--actionMenuSelection < 0)
            {
                actionMenuSelection = actionMenuSelections.Count - 1;
            }
        }

        // Move the selector to show what is being selected
        int height = 30 * actionMenuSelections.Count;
        actionMenuSelector.transform.localPosition = new Vector3(actionMenuSelector.transform.localPosition.x, (height / 2) - 15 - (30 * actionMenuSelection));
        return actionMenuSelections[actionMenuSelection];
    }

    public int ChangeSelectionMouse(Vector2 mousePos)
    {
        float top = actionMenu.transform.position.y + (actionMenuSelections.Count * 30 / 2);  // 60 is the height of the action menu
        float left = actionMenu.transform.position.x - 100;  // 80 is a reference to half the width of the action menu
        float right = actionMenu.transform.position.x + 100;
        float bottom = actionMenu.transform.position.y - (actionMenuSelections.Count * 30 / 2);

        if (mousePos.y < top && mousePos.x > left && mousePos.x < right && mousePos.y > bottom)
        {
            float amHeight = actionMenuSelections.Count * 60;
            for (int i = 1; i <= actionMenuSelections.Count; i++)
            {
                if (mousePos.y > top - i * 30)
                {
                    actionMenuSelection = i - 1;
                    break;
                }
            }
        }
        else
        {
            return actionMenuSelections[actionMenuSelection];
        }

        // Move the selector to show what is being selected
        int height = 30 * actionMenuSelections.Count;
        actionMenuSelector.transform.localPosition = new Vector3(actionMenuSelector.transform.localPosition.x, (height / 2) - 15 - (30 * actionMenuSelection));

        return actionMenuSelections[actionMenuSelection];
    }

    public void RemoveActionMenu()
    {
        actionMenuText.text = "";
        actionMenuSelections.Clear();
        actionMenu.SetActive(false);
    }

    // Private Functions
    // Reset Side Panel info
    private void SetSidePanelInfo(bool setInfo = true)
    {
        // Unit Info
        if (curTileInfo.GetUnit() != null && setInfo)
        {
            Unit unit = curTileInfo.GetUnit();
            unitName.text = unit.GetObjectName();
            unitType.text = unit.GetObjectType();
            unitImage.sprite = unit.GetSprite();
            unitImage.enabled = true;
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
            unitImage.enabled = false;
            unitHP.text = "";
            unitVision.text = "";
            unitMovement.text = "";
            unitAttack.text = "";
            unitDescription.text = "";
        }

        // Structure Info
        
        if (curTileInfo.GetStructure() != null && setInfo)
        {
            Structure structure = curTileInfo.GetStructure();
            structureName.text = structure.GetObjectName();
            structureType.text = structure.GetObjectType();
            structureImage.sprite = structure.GetSprite();
            structureImage.enabled = true;
            structureHP.text = "Health: " + structure.GetHP().ToString() + '/' + structure.GetMaxHP().ToString();
            structureVision.text = "Vision: " + structure.GetVisionRange().ToString();
            structureDescription.text = structure.GetDescription();
        }
        else
        {
            structureName.text = "";
            structureType.text = "";
            structureImage.enabled = false;
            structureHP.text = "";
            structureVision.text = "";
            structureMovement.text = "";
            structureDescription.text = "";
        }
        

        // Terrain Info
        if (curTileInfo.HasResources() && setInfo)
        {
            resourceImage.enabled = true;
            terrainResources.text = "Resources left: " + curTileInfo.GetResources().ToString();
            terrainElevation.text = "Elevation: " + curTileInfo.GetElevation().ToString();
            totalResources.text = "Total Resources: " + player.GetResources();
        }
        else
        {
            resourceImage.enabled = false;
            terrainResources.text = "";
            terrainElevation.text = "";
            totalResources.text = "Total Resources: " + player.GetResources();
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

    private void Surrender()
    {
        GameManager gm = FindObjectOfType<GameManager>();

        gm.PlayerLost(player.GetPlayerNum());
    }

    private void ExitGame()
    {
        Application.Quit();
    }
}
