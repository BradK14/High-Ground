using Fusion;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    // Variables
    // Initialized in the inspector
    // Initialized at spawn
    GameManager gameManager;
    private MapInfo map;
    private List<Unit> units;
    private List<Structure> structures;
    private int resources;
    private BaseUI ui;
    private bool setupComplete;
    private int posX;
    private int posY;
    private bool isMyTurn;
    private int numBases;
    private TickTimer shiftMoveTimer;
    private TickTimer heldMoveTimer;
    private TickTimer heldMovingTimer;
    private bool shiftMove;
    private bool heldMoving;
    private bool heldMove;
    private bool heldMoveRepeat;
    private bool upRepeat;
    private bool leftRepeat;
    private bool rightRepeat;
    private bool downRepeat;
    private bool enterRepeat;
    private bool backRepeat;
    private bool rightMouseRepeat;
    private bool mouseDragHeld;
    private Vector2 mousePrevPos;
    // Initialized at run time
    [Networked] private PlayerRef playerRef { get; set; }
    [Networked] private int playerNum { get; set; }
    private Unit unit;
    private Structure structure;
    private TileInfo curTile;
    private TileInfo mouseTile;

    // Functions
    // Start Function
    public override void Spawned()
    {
        setupComplete = false;
        units = new List<Unit>();
        structures = new List<Structure>();
        resources = 0;
        posX = -1;
        posY = -1;
        isMyTurn = false;
        numBases = 0;
        shiftMove = false;
        heldMoving = false;
        heldMove = false;
        heldMoveRepeat = false;
        upRepeat = false;
        leftRepeat = false;
        rightRepeat = false;
        downRepeat = false;
        enterRepeat = false;
        backRepeat = false;
        rightMouseRepeat = false;
        mouseDragHeld = false;
        StartCoroutine(Setup());
    }

    // Update Function
    private void Update()
    {
        if (setupComplete)  
        {
            // Some mouse inputs can be done outside of the player's turn (Camera controls)
            UpdateMousePos(Input.mousePosition);
            RightMouse(Input.GetMouseButton(1));

            if (Runner.LocalPlayer == playerRef && gameManager.GetPlayerTurn() == playerNum)  // Only detect the correct player's input
            {
                Shift(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
                AnyDirection(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
                    Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow),
                    Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow),
                    Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow));
                Up(Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow));
                Left(Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow));
                Right(Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow));
                Down(Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow));
                Enter(Input.GetKey(KeyCode.Return) || Input.GetMouseButton(0));
                Back(Input.GetKey(KeyCode.Backspace));
            }
        }
    }

    // Coroutines
    private IEnumerator Setup()
    {
        // Wait for player to be set before setting other parts
        while (playerRef == null || playerRef == PlayerRef.None) { yield return null; }

        // Try to get a reference to the ui
        while (ui == null)
        {
            ui = FindObjectOfType<BaseUI>();
            yield return null;
        }

        // Try to get a reference to the game manager
        while (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();

            yield return null;
        }

        // Try to get a reference to the map and set the location of the cursor to 0, 0
        while (map == null)
        {
            MapInfo map_ = FindObjectOfType<MapInfo>();
            if (map_ != null)
            {
                if (map_.SetupIsComplete())  // Wait until the map is set up
                {
                    map = map_;
                    ChangePos(0, 0);
                    ChangeCameraPosWithCursor();
                }
            }
            
            yield return null;
        }

        setupComplete = true;
    }

    // Public Functions
    // Setters
    public void SetPlayerNum(int playerNum_) { playerNum = playerNum_; }
    public void SetPlayerRef(PlayerRef playerRef_) { playerRef = playerRef_; }
    // Getters
    public bool SetupIsComplete() { return setupComplete; }
    public PlayerRef GetPlayerRef() { return playerRef; }
    public int GetPlayerNum() { return playerNum; }
    public bool GetIsMyTurn() { return isMyTurn; }
    public TileInfo GetCurTile() { return curTile; }
    public int GetUnitInd(Unit unit_) { return units.IndexOf(unit_); }
    public int GetStructureInd(Structure structure_) { return structures.IndexOf(structure_); }
    public int GetResources() { return resources; }
    public int GetNumBases() { return numBases; }

    // Function for increasing resources
    public void AddResources(int res)
    {
        resources += res;

        Rpc_SetResources(resources);
    }

    public void SpendResources(int res)
    {
        resources -= res;

        Rpc_SetResources(resources);
    }

    // Functions to change the player's turn
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_StartTurn()
    {
        // Reset all unit actions
        foreach (Unit u in units)
        {
            u.StartTurn();
        }

        // Reset all structure actions
        int numSaturated = 0;
        foreach (Structure s in structures)
        {
            s.StartTurn();

            if (s.IsSaturated())  // Should only be true for bases that have max gatherers (false for all non-base structures)
            {
                numSaturated++;
            }
        }

        if (Runner.LocalPlayer == playerRef)
        {
            if (numSaturated == 5)
            {
                gameManager.PlayerWon(playerNum);
            }

            SetSprite(true);
        }

        isMyTurn = true;
    }

    public void EndTurn()
    {
        Rpc_SetSprite(false);
        Rpc_SetIsMyTurn(false);

        // Back out of any chain of selections
        DeselectAll();

        // Reset all unit's actions
        foreach (Unit u in units)
        {
            u.EndTurn();
        }

        // Reset all structure actions
        foreach (Structure s in structures)
        {
            s.EndTurn();
        }

        Rpc_ResetVision();
    }

    public void EndOfGame()
    {
        Rpc_SetSprite(false);
        Rpc_SetIsMyTurn(false);

        // Back out of any chain of selections
        DeselectAll();
    }

    // Tells the game manager to end this players turn
    public void EndTurnSignal()
    {
        if (gameManager.GetPlayerTurn() == playerNum)
        {
            gameManager.NextPlayerTurn();
        }
    }

    // Number of base modifiers (for detecting a loss by no bases)
    public void AddBase()
    {
        numBases++;
    }

    public void RemoveBase()
    {
        Rpc_SetNumBases(--numBases);

        if (numBases <= 0)
        {
            gameManager.PlayerLost(playerNum);
        }
    }

    // Unit List Helpers
    public void AddUnit(Unit unit_)
    {
        Rpc_AddUnit(unit_);
    }

    public void AddStructure(Structure structure_)
    {
        Rpc_AddStructure(structure_);
    }

    // Private Functions
    // Inputs
    // Shift
    private void Shift(bool pressed)
    {
        if (pressed)
        {
            if (shiftMoveTimer.ExpiredOrNotRunning(Runner))
            {
                shiftMove = true;
                shiftMoveTimer = TickTimer.CreateFromSeconds(Runner, 0.05f);
            }
            else
            {
                shiftMove = false;
            }
        }
        else
        {
            shiftMoveTimer = TickTimer.None;
            shiftMove = false;
        }
    }

    // W, A, D, S, up, left, right, or down arrow
    private void AnyDirection(bool upPressed, bool leftPressed, bool rightPressed, bool downPressed)
    {
        if (upPressed || leftPressed || rightPressed || downPressed)
        {
            if (!heldMoveRepeat && heldMoveTimer.ExpiredOrNotRunning(Runner))
            {
                heldMoveRepeat = true;
                heldMoveTimer = TickTimer.CreateFromSeconds(Runner, 0.35f);
            }
            else if (heldMoveTimer.ExpiredOrNotRunning(Runner))
            {
                heldMoving = true;
            }

            if (heldMoving && heldMovingTimer.ExpiredOrNotRunning(Runner) && !shiftMoveTimer.IsRunning)
            {
                heldMove = true;
                heldMovingTimer = TickTimer.CreateFromSeconds(Runner, 0.1f);
            }
            else
            {
                heldMove = false;
            }
        }
        else
        {
            heldMoveRepeat = false;
            heldMoving = false;
            heldMove = false;
            heldMoveTimer = TickTimer.None;
            heldMovingTimer = TickTimer.None;
        }
    }

    // W or Up arrow
    private void Up(bool pressed)
    {
        if (pressed)
        {
            if (!upRepeat || shiftMove || heldMove)
            {
                upRepeat = true;
                // Navigate action menu
                if (ui.isActionMenuActive())
                {
                    int selection = ui.ChangeSelection(false);
                    if (unit != null) { unit.SelectAbilityPreview(selection); }
                    else if (structure != null) { structure.SelectAbilityPreview(selection); }
                }
                else  // Change cursor position
                {
                    TileInfo tile = map.GetTile(posX, posY - 1);
                    if (tile != null)
                    {
                        ChangePos(posX, posY - 1);
                        ChangeCameraPosWithCursor();
                    }
                }
                
            }
        }
        else
        {
            upRepeat = false;
        }
    }

    // A or Left arrow
    private void Left(bool pressed)
    {
        if (pressed)
        {
            if (!leftRepeat || shiftMove || heldMove)
            {
                leftRepeat = true;
                // Navigate action menu
                if (ui.isActionMenuActive())
                {
                    // Do nothing (Choices are vertically arranged)
                }
                else  // Change cursor position
                {
                    TileInfo tile = map.GetTile(posX - 1, posY);
                    if (tile != null)
                    {
                        ChangePos(posX - 1, posY);
                        ChangeCameraPosWithCursor();
                    }
                }
            }
        }
        else
        {
            leftRepeat = false;
        }
    }

    // D or Right arrow
    private void Right(bool pressed)
    {
        if (pressed)
        {
            if (!rightRepeat || shiftMove || heldMove)
            {
                rightRepeat = true;
                // Navigate action menu
                if (ui.isActionMenuActive())
                {
                    // Do nothing (Choices are vertically arranged)
                }
                else  // Change cursor position
                {
                    TileInfo tile = map.GetTile(posX + 1, posY);
                    if (tile != null)
                    {
                        ChangePos(posX + 1, posY);
                        ChangeCameraPosWithCursor();
                    }
                }
            }
        }
        else
        {
            rightRepeat = false;
        }
    }

    // S or Down arrow
    private void Down(bool pressed)
    {
        if (pressed)
        {
            if (!downRepeat || shiftMove || heldMove)
            {
                downRepeat = true;
                // Navigate action menu
                if (ui.isActionMenuActive())
                {
                    int selection = ui.ChangeSelection(true);
                    if (unit != null) { unit.SelectAbilityPreview(selection); }
                    else if (structure != null) { structure.SelectAbilityPreview(selection); }
                }
                else  // Change cursor position
                {
                    TileInfo tile = map.GetTile(posX, posY + 1);
                    if (tile != null)
                    {
                        ChangePos(posX, posY + 1);
                        ChangeCameraPosWithCursor();
                    }
                }
            }
        }
        else
        {
            downRepeat = false;
        }
    }

    // Enter
    private void Enter(bool pressed)
    {
        if (pressed)
        {
            if (!enterRepeat)
            {
                enterRepeat = true;
                if (posX != -1)  // Cannot do anything while off the map
                {
                    if (map.GetTile(posX, posY) != null)
                    {
                        if (ui.isActionMenuActive())  // Make a selection from the action menu
                        {
                            if (unit == null && structure == null)  // If we are in the Unit/Structure choice menu
                            {
                                if (ui.GetSelection() == 0)
                                {
                                    ui.RemoveActionMenu();
                                    unit = map.GetTile(posX, posY).GetUnit();
                                    Rpc_SetUnit(unit);
                                    unit.SelectAbilityPreview(ui.CreateActionMenuForPPO(unit));
                                }
                                else
                                {
                                    ui.RemoveActionMenu();
                                    structure = map.GetTile(posX, posY).GetStructure();
                                    Rpc_SetStructure(structure);
                                    structure.SelectAbilityPreview(ui.CreateActionMenuForPPO(structure));
                                }
                            }
                            else if (unit != null)  // If a Unit's abilities are displayed
                            {
                                if (unit.SelectAbility(ui.GetSelection()))  // returns true if ability is done
                                {
                                    unit = null;
                                    Rpc_SetUnit(unit);
                                }
                            }
                            else if (structure != null)  // If a Structure's abilities are displayed
                            {
                                if (structure.SelectAbility(ui.GetSelection()))
                                {
                                    structure = null;
                                    Rpc_SetStructure(structure);
                                }
                            }
                        }
                        else if (unit == null && structure == null)  // When in a default state open a list of actions
                        {
                            SelectTile();
                        }  // If a unit or structure is selected and no menu is active, we are preparing to use an ability
                        else if (unit != null)  // When we have a unit selected
                        {
                            if (unit.UseAbility(curTile))  // Attempt to use the units ability
                            {
                                // If the ability is completely performed, release the unit
                                unit = null;
                                Rpc_SetUnit(unit);
                            }
                        }
                        else if (structure != null)
                        {
                            if (structure.UseAbility(curTile))
                            {
                                structure = null;
                                Rpc_SetStructure(structure);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            enterRepeat = false;
        }
    }

    // Backspace
    private void Back(bool pressed)
    {
        // Back out of using abilities or menus
        if (pressed)
        {
            if (!backRepeat)
            {
                backRepeat = true;
                DeselectAll();
            }
        }
        else
        {
            backRepeat = false;
        }
    }

    // Mouse Input
    // Right Mouse
    private void RightMouse(bool pressed)
    {
        // Back out of using abilities or menus
        if (pressed)
        {
            if (Runner.LocalPlayer == playerRef && gameManager.GetPlayerTurn() == playerNum &&  // Only detect the correct player's input
                (ui.isActionMenuActive() || unit != null || structure != null))
            {
                if (!rightMouseRepeat)
                {
                    rightMouseRepeat = true;
                    DeselectAll();
                }
            }
            else if (!rightMouseRepeat)
            {
                mouseDragHeld = true;
            }
        }
        else
        {
            rightMouseRepeat = false;
            mouseDragHeld = false;
        }
    }

    private void ResetMouseTile()
    {
        Vector2 mp = Input.mousePosition;
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mp);
        Vector3Int gridPos = map.GetCell(worldPos);
        Vector3Int gamePos = map.ConvertGridPosToGamePos(gridPos);

        mouseTile = map.GetTile(gamePos.x, gamePos.y);
    }

    private void UpdateMousePos(Vector2 mousePos)
    {
        if (!ui.isActionMenuActive())
        {
            if (mouseDragHeld && unit == null && structure == null)  // Move camera when holding right click
            {
                Vector3 worldPrevPos = Camera.main.ScreenToWorldPoint(mousePrevPos);
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                float top = map.GetTile(0, 0).GetWorldPos().y;
                float left = map.GetTile(0, 0).GetWorldPos().x;
                float right = map.GetTile(map.GetMaxX(), 0).GetWorldPos().x;
                float bottom = map.GetTile(0, map.GetMaxY()).GetWorldPos().y;

                Vector3 newPosition = new Vector3(Camera.main.transform.position.x + worldPrevPos.x - worldPos.x, Camera.main.transform.position.y + worldPrevPos.y - worldPos.y, -10f);
                bool xValidPosition = true;
                bool yValidPosition = true;
                // Too far up
                if (newPosition.y > top - 3.5f) { yValidPosition = false; }
                if (newPosition.x < left + 7.4f) { xValidPosition = false; }
                if (newPosition.x > right - 4.45f) { xValidPosition = false; }
                if (newPosition.y < bottom + 3.5) { yValidPosition = false; }

                if (xValidPosition && yValidPosition)
                {
                    Camera.main.transform.position = newPosition;
                }
                else if (xValidPosition)
                {
                    Camera.main.transform.position = new Vector3(newPosition.x, Camera.main.transform.position.y, -10f);
                }
                else if (yValidPosition)
                {
                    Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, newPosition.y, -10f);
                }
            }
            else if (Runner.LocalPlayer == playerRef && gameManager.GetPlayerTurn() == playerNum &&  // Only detect the correct player's input
                    mousePos.x < 1600 && mousePos.y < 1080 && mousePos.x >= 0 && mousePos.y >= 0)  // Change position of game cursor while pointing into the game
            {
                Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
                Vector3Int gridPos = map.GetCell(worldPos);
                Vector3Int gamePos = map.ConvertGridPosToGamePos(gridPos);

                if (map.GetTile(gamePos.x, gamePos.y) != mouseTile)
                {
                    ChangePos(gamePos.x, gamePos.y);
                    mouseTile = curTile;
                }
            }
        }
        else  // Change selection in the action menu
        {
            int selection = ui.ChangeSelectionMouse(mousePos);
            if (unit != null) { unit.SelectAbilityPreview(selection); }
            else if (structure != null) { structure.SelectAbilityPreview(selection); }
        }

        mousePrevPos = mousePos;
    }

    // Other Private Functions
    private void SelectTile()
    {
        TileInfo tile = map.GetTile(posX, posY);
        bool validUnit = false;
        bool validStructure = false;
        if (tile.GetUnit() != null)
        {
            if (tile.GetUnit().GetPlayerNum() == playerNum && !tile.GetUnit().HasUsedAllActions())
            {
                validUnit = true;
            }
        }
        if (tile.GetStructure() != null)
        {
            if (tile.GetStructure().GetPlayerNum() == playerNum && !tile.GetStructure().HasUsedAllActions())
            {
                validStructure = true;
            }
        }

        if (validUnit && validStructure)
        {
            // Create a menu for selecting either the unit or the structure
            ui.CreateUnitStructureMenu(tile);
        }
        else if (validUnit)
        {
            unit = tile.GetUnit();
            Rpc_SetUnit(unit);

            // Open the action menu
            unit.SelectAbilityPreview(ui.CreateActionMenuForPPO(tile.GetUnit()));
        }
        else if (validStructure)
        {
            structure = tile.GetStructure();
            Rpc_SetStructure(structure);

            // Open the action menu
            structure.SelectAbilityPreview(ui.CreateActionMenuForPPO(tile.GetStructure()));
        }
    }

    // Change position of cursor on the map
    private void ChangePos(int x, int y)
    {
        // If position did not actually change, then return early
        if (x == posX && y == posY)
        {
            return;
        }

        // If trying to move off the map, do not change the position just return
        if (map.GetTile(x, y) == null)
        {
            return;
        }
        else
        {
            // Change position of the cursor
            posX = x;
            posY = y;
            curTile = map.GetTile(posX, posY);  // Set a reference to the current tile at the cursor's position

            transform.position = map.GetTile(x, y).GetWorldPos();

            // When we have a unit selected, display ability information appropriately
            if (unit != null)
            {
                unit.ChangePosAbility(curTile);
            }
            else if (structure != null)
            {
                structure.ChangePosAbility(curTile);
            }
        }
    }

    // Deselection
    private void DeselectAll()
    {
        if (ui.isActionMenuActive())  // When looking at a menu
        {
            ui.RemoveActionMenu();
        }

        if (unit != null)  // When a unit is selected
        {
            unit.Deselect();
            unit = null;
            Rpc_SetUnit(unit);
        }
        
        if (structure != null)
        {
            structure.Deselect();
            structure = null;
            Rpc_SetStructure(structure);
        }
    }
    
    // Camera functions
    private void ChangeCameraPosWithCursor()
    {
        // If there is no cursor, don't do anything
        if (posX == -1)
        {
            return;
        }
        // Cursor too far up
        if (Camera.main.transform.position.y - 3.5f > map.GetTile(posX, posY).GetWorldPos().y)
        {
            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, map.GetTile(posX, posY).GetWorldPos().y + 3.5f, -10f);
        }  // Cursor too far left
        if (Camera.main.transform.position.x - 7.4f > map.GetTile(posX, posY).GetWorldPos().x)
        {
            Camera.main.transform.position = new Vector3(map.GetTile(posX, posY).GetWorldPos().x + 7.4f, Camera.main.transform.position.y, -10f);
        }  // Cursor too far right
        if (Camera.main.transform.position.x + 4.45f < map.GetTile(posX, posY).GetWorldPos().x)
        {
            Camera.main.transform.position = new Vector3(map.GetTile(posX, posY).GetWorldPos().x - 4.45f, Camera.main.transform.position.y, -10f);
        }  // Cursor too far down
        if (Camera.main.transform.position.y + 3.5f < map.GetTile(posX, posY).GetWorldPos().y)
        {
            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, map.GetTile(posX, posY).GetWorldPos().y - 3.5f, -10f);
        }
        // If the camera moved, the mouse's tile may have changed as well
        ResetMouseTile();
    }

    // RPC Functions
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_SetResources(int res)
    {
        resources = res;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_SetIsMyTurn(bool isMyTurn_)
    {
        isMyTurn = isMyTurn_;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_SetSprite(bool isOn)
    {
        // Only change sprite for the player locally
        if (Runner.LocalPlayer == playerRef)
        {
            GetComponent<SpriteRenderer>().enabled = isOn;
        }
    }

    private void SetSprite(bool isOn)
    {
        GetComponent<SpriteRenderer>().enabled = isOn;
    }

    // Reset the vision
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_ResetVision()
    {
        if (Runner.LocalPlayer == playerRef)
        {
            map.CoverMapInFog();

            foreach (Unit u in units)
            {
                u.DisableFogByVision();
            }
            foreach (Structure s in structures)
            {
                s.DisableFogByVision();
            }
        }
    }

    // Unit setters
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_ResetUnitList()
    {
        units.Clear();
        Unit[] units_ = FindObjectsOfType<Unit>();

        foreach (Unit u in units_)
        {
            if (!u.IsDestroyed())
            {
                if (playerNum == u.GetPlayerNum())
                {
                    units.Add(u);
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_SetUnit(Unit unit_)
    {
        unit = unit_;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_AddUnit(Unit unit_)
    {
        units.Add(unit_);

        if (Runner.LocalPlayer == playerRef)
        {
            unit_.EnableSprite(true);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_RemoveUnit(Unit unit_)
    {
        units.Remove(unit_);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_RemoveUnitAt(int index)
    {
        units.RemoveAt(index);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_RemoveNullUnits()
    {
        units.RemoveAll(null);
    }

    // Structure setters
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_ResetStructureList()
    {
        structures.Clear();
        Structure[] structures_ = FindObjectsOfType<Structure>();

        foreach (Structure s in structures_)
        {
            if (!s.IsDestroyed())
            {
                if (playerNum == s.GetPlayerNum())
                {
                    structures.Add(s);
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_SetStructure(Structure structure_)
    {
        structure = structure_;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_AddStructure(Structure structure_)
    {
        structures.Add(structure_);

        if (Runner.LocalPlayer == playerRef)
        {
            structure_.EnableSprite(true);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_RemoveStructure(Structure structure_)
    {
        structures.Remove(structure_);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_RemoveStructureAt(int index)
    {
        structures.RemoveAt(index);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetNumBases(int num)
    {
        numBases = num;
    }
}
