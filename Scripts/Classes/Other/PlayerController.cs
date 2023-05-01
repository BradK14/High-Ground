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
    [Networked] GameManager gameManager { get; set; }
    [Networked] private MapInfo map { get; set; }
    private List<Unit> units;
    private List<TileInfo> movePreview;
    private List<TileInfo> pathable;
    private List<TileInfo> path;
    [Networked] private bool setUpComplete { get; set; }
    private int posX;
    private int posY;
    private bool isMyTurn;
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
    // Initialized at run time
    [Networked] private PlayerRef playerRef { get; set; }
    [Networked] private int playerNum { get; set; }
    private Unit unit;
    private TileInfo unitPrevTile;
    private TileInfo curTile;

    // Functions
    // Start Function
    public override void Spawned()
    {
        setUpComplete = false;
        units = new List<Unit>();
        movePreview = new List<TileInfo>();
        pathable = new List<TileInfo>();
        path = new List<TileInfo>();
        posX = -1;
        posY = -1;
        isMyTurn = false;
        shiftMove = false;
        heldMoving = false;
        heldMove = false;
        heldMoveRepeat = false;
        upRepeat = false;
        leftRepeat = false;
        rightRepeat = false;
        downRepeat = false;
        enterRepeat = false;
    }

    // Update Function
    private void Update()
    {
        if (setUpComplete && Runner.LocalPlayer == playerRef && gameManager.GetPlayerTurn() == playerNum)  // Only detect the correct player's input
        {
            Shift(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));
            AnyDirection(Input.GetKey(KeyCode.W), Input.GetKey(KeyCode.A), Input.GetKey(KeyCode.D), Input.GetKey(KeyCode.S));
            Up(Input.GetKey(KeyCode.W));
            Left(Input.GetKey(KeyCode.A));
            Right(Input.GetKey(KeyCode.D));
            Down(Input.GetKey(KeyCode.S));
            NetInputEnter(Input.GetKey(KeyCode.Return));
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Wait for map to spawn before accepting input
        if (map == null)
        {
            map = FindObjectOfType<MapInfo>();
        }
        else if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }
        else if (map.GetIsTilesSet() && !setUpComplete)
        {
            ChangePos(0, 0);
            ChangeCameraPosWithCursor();
            setUpComplete = true;
        }
        /*  IF I WANT TO PUT NETWORK INPUTS IN THEY GO HERE
        else if (GetInput(out InputData input))
        {
            //Shift(input.shift);
            //AnyDirection(input.up, input.left, input.right, input.down);
            //Up(input.up);
            //Left(input.left);
            //Right(input.right);
            //Down(input.down);
            //MousePosInput(input.mousePos);
            //NetInputEnter(input.enter || input.leftMouse);
        }*/
    }

    // Public Functions
    // Setters/Getters
    public void SetPlayerNum(int playerNum_) { playerNum = playerNum_; }
    public void SetPlayerRef(PlayerRef playerRef_) { playerRef = playerRef_; }
    public PlayerRef GetPlayerRef() { return playerRef; }
    public int GetPlayerNum() { return playerNum; }
    public bool GetIsMyTurn() { return isMyTurn; }
    public TileInfo GetCurTile() { return curTile; }

    // Functions to change the player's turn
    public void StartTurn()
    {
        Rpc_SetSprite(true);
        Rpc_SetIsMyTurn(true);

        // TODO Create and display the move preview of a unit that the cursor is on

    }

    public void EndTurn()
    {
        Rpc_SetSprite(false);
        Rpc_SetIsMyTurn(false);

        // TODO Back out of any chain of selections

        // Remove the move preview
        MovePreviewDestroy();

        // Reset all unit's actions
        foreach (Unit u in units)
        {
            u.ResetActions();
        }
    }

    // Tells the game manager to end this players turn
    public void EndTurnSignal()
    {
        if (gameManager.GetPlayerTurn() == playerNum)
        {
            gameManager.NextPlayerTurn();
        }
    }

    // Unit List Helpers
    public void AddUnit(Unit unit_)
    {
        Rpc_AddUnit(unit_);
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
                TileInfo tile = map.GetTile(posX, posY - 1);
                if (tile != null)
                {
                    ChangePos(posX, posY - 1);
                    ChangeCameraPosWithCursor();
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
                TileInfo tile = map.GetTile(posX - 1, posY);
                if (tile != null)
                {
                    ChangePos(posX - 1, posY);
                    ChangeCameraPosWithCursor();
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
                TileInfo tile = map.GetTile(posX + 1, posY);
                if (tile != null)
                {
                    ChangePos(posX + 1, posY);
                    ChangeCameraPosWithCursor();
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
                TileInfo tile = map.GetTile(posX, posY + 1);
                if (tile != null)
                {
                    ChangePos(posX, posY + 1);
                    ChangeCameraPosWithCursor();
                }
            }
        }
        else
        {
            downRepeat = false;
        }
    }

    // Enter
    private void NetInputEnter(bool pressed)
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
                        if (unit == null)  // When in a default state open a list of actions
                        {
                            SelectUnit();
                        }
                        else  // When we have a unit selected
                        {
                            MoveUnit();
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

    private void SelectUnit()
    {
        TileInfo tile = map.GetTile(posX, posY);
        if (tile.GetUnit() != null)
        {
            if (tile.GetUnit().GetPlayerNum() == playerNum)
            {
                Rpc_SetUnitPrevTile(tile);
                Rpc_SetUnit(tile.GetUnit());
                foreach (TileInfo t in movePreview)
                {
                    pathable.Add(t);
                }
                PathableDisplay();
                Rpc_AddToPath(tile);
                PathDisplay();
            }
        }
    }

    private void MoveUnit()
    {
        unit.Move(path);
        TileInfo tile = path[path.Count - 1];
        unitPrevTile.Rpc_SetUnit(null);
        Rpc_SetUnitPrevTile(null);
        tile.Rpc_SetUnit(unit);
        unit.Rpc_SetTransform(tile.GetWorldPos());
        Rpc_SetUnit(null);
        MovePreviewDestroy();
        PathableDestroy();
        PathDestroy();
        
        if (posX != -1)
        {
            if (map.GetTile(posX, posY).GetUnit() != null)
            {
                if (!map.GetTile(posX, posY).GetUnit().HasMoved())
                {
                    movePreview = map.GetTilesInMoveRange(posX, posY, map.GetTile(posX, posY).GetUnit());
                    MovePreviewDisplay();
                }
            }
        }
    }

    // Mouse Movement
    private void MousePosInput(Vector2 mousePos)
    {
        Vector3Int gridPos = map.GetCell(mousePos);
        Vector3Int mapPos = map.ConvertGridPosToGamePos(gridPos);
        ChangePos(mapPos.x, mapPos.y);
    }

    // Other private functions
    // Change position of cursor on the map
    private void ChangePos(int x, int y)
    {
        // If position did not actually change, then return early
        if (x == posX && y == posY)
        {
            return;
        }

        // When changing positions, remove the previous move preview from the screen
        if (unit == null) { MovePreviewDestroy(); }

        // If trying to move off the map, do not change the position just return
        if (map.GetTile(x, y) == null)
        {
            return;
        }
        else
        {
            // When we are moving a unit, set pathing information
            if (unit != null && movePreview.Contains(map.GetTile(x, y)))
            {
                bool directlyPathable = false;
                // First checks if the new position is adjacent and isn't already in the path
                if (((x == path[path.Count - 1].GetPosX() && (y == path[path.Count - 1].GetPosY() - 1 || y == path[path.Count - 1].GetPosY() + 1))
                    || (y == path[path.Count - 1].GetPosY() && (x == path[path.Count - 1].GetPosX() - 1 || x == path[path.Count - 1].GetPosX() + 1)))
                    && !path.Contains(map.GetTile(x, y))
                    && pathable.Contains(map.GetTile(x, y)))
                {
                    directlyPathable = map.IsPathable(map.GetTile(posX, posY), map.GetTile(x, y), unit);
                }

                // Change position of the cursor early for pathing functions
                posX = x;
                posY = y;
                // Then either retrace 1 step in the path, continue the path, or rebuild the path
                bool pathAltered = false;
                if (path.Count > 1)
                {
                    if (path[path.Count - 2] == map.GetTile(x, y))
                    {
                        path[path.Count - 1].RemovePath();
                        Rpc_RemoveFromPath(path[path.Count - 1]);
                        pathAltered = true;
                    }
                }
                if (directlyPathable && !pathAltered)
                {
                    Rpc_AddToPath(map.GetTile(x, y));
                }
                else if (!pathAltered)
                {
                    AutoBuildPath();
                }

                PathDisplay();

                // With the path created, find the remaining pathable zone you can reach if there is any
                PathableDestroy();
                List<TileInfo> p = new List<TileInfo>();
                for (int i = 0; i < path.Count; i++)
                {
                    p.Add(path[i]);
                }
                pathable = map.GetTilesInMoveRange(x, y, unit, p);
                MovePreviewDisplay();
                PathableDisplay();
            }

            // Change position of the cursor
            posX = x;
            posY = y;
            transform.position = map.GetTile(x, y).GetWorldPos();

            // If there is a friendly unit here, display a preview of it's move range
            if (map.GetTile(x, y).GetUnit() != null && unit == null)
            {
                if (map.GetTile(x, y).GetUnit().GetPlayerNum() == playerNum && !map.GetTile(x, y).GetUnit().HasMoved())
                {
                    movePreview = map.GetTilesInMoveRange(x, y, map.GetTile(x, y).GetUnit());
                    MovePreviewDisplay();
                }
            }
        }

        // Set a reference to the current tile at the cursor's position
        if (posX != -1)
        {
            curTile = map.GetTile(posX, posY);
        }
    }

    // Function made for building a path automatically based on the position of the cursor
    private void AutoBuildPath()
    {
        TileInfo startTile = path[0];
        PathDestroy();

        if (startTile != map.GetTile(posX, posY))
        {
            List<TileInfo> p = map.BuildPath(startTile, map.GetTile(posX, posY), unit);
            foreach (TileInfo t in p)
            {
                Rpc_AddToPath(t);
            }
        }
        else
        {
            Rpc_AddToPath(startTile);
        }
    }

    // Highlight Handlers
    private void MovePreviewDestroy()
    {
        for (int i = 0; i < movePreview.Count; i++)
        {
            movePreview[i].DisableHighlight();
        }

        movePreview.Clear();
    }

    private void PathableDestroy()
    {
        for (int i = 0; i < pathable.Count; i++)
        {
            pathable[i].DisableHighlight();
        }

        pathable.Clear();
    }

    private void PathDestroy()
    {
        for (int i = 0; i < path.Count; i++)
        {
            path[i].RemovePath();
        }

        Rpc_ClearPath();
    }

    private void MovePreviewDisplay()
    {
        for (int i = 0; i < movePreview.Count; i++)
        {
            movePreview[i].EnableHighlightPathable(playerRef);
        }
    }

    private void PathableDisplay()
    {
        for (int i = 0; i < pathable.Count; i++)
        {
            pathable[i].EnableHighlightImmediatelyPathable(playerRef);
        }
    }

    private void PathDisplay()
    {
        // First clear the previous path's images
        foreach (TileInfo p in path)
        {
            p.RemovePath();
        }

        // Special case for a path of only size 1
        if (path.Count == 1)
        {
            path[0].DisplayPath(playerRef, "Start");
        }
        else
        {
            // Display the entire path
            for (int i = 0; i < path.Count; i++)
            {
                int presX = path[i].GetPosX();
                int presY = path[i].GetPosY();
                if (i == 0)  // The first tile in the path
                {
                    int postX = path[i + 1].GetPosX();
                    int postY = path[i + 1].GetPosY();
                    if (postY < presY)
                    {
                        path[i].DisplayPath(playerRef, "ConnectUp");
                    }
                    else if (postX < presX)
                    {
                        path[i].DisplayPath(playerRef, "ConnectLeft");
                    }
                    else if (postX > presX)
                    {
                        path[i].DisplayPath(playerRef, "ConnectRight");
                    }
                    else if (postY > presY)
                    {
                        path[i].DisplayPath(playerRef, "ConnectDown");
                    }
                    else
                    {
                        path[i].DisplayPath(playerRef, "Start");
                    }
                }
                else if (i == path.Count - 1)  // The last tile in the path
                {
                    int prevX = path[i - 1].GetPosX();
                    int prevY = path[i - 1].GetPosY();
                    if (prevY > presY)
                    {
                        path[i].DisplayPath(playerRef, "ArriveFromBelow");
                    }
                    else if (prevX > presX)
                    {
                        path[i].DisplayPath(playerRef, "ArriveFromRight");
                    }
                    else if (prevX < presX)
                    {
                        path[i].DisplayPath(playerRef, "ArriveFromLeft");
                    }
                    else if (prevY < presY)
                    {
                        path[i].DisplayPath(playerRef, "ArriveFromAbove");
                    }
                    else
                    {
                        path[i].DisplayPath(playerRef, "Start");
                    }
                }
                else  // Tiles in the middle of the path
                {
                    int prevX = path[i - 1].GetPosX();
                    int prevY = path[i - 1].GetPosY();
                    int postX = path[i + 1].GetPosX();
                    int postY = path[i + 1].GetPosY();
                    if ((prevY < presY && postX < presX) || (prevX < presX && postY < presY))
                    {
                        path[i].DisplayPath(playerRef, "ConnectUpLeft");
                    }
                    else if ((prevY < presY && postX > presX) || (prevX > presX && postY < presY))
                    {
                        path[i].DisplayPath(playerRef, "ConnectUpRight");
                    }
                    else if ((prevY < presY && postY > presY) || (prevY > presY && postY < presY))
                    {
                        path[i].DisplayPath(playerRef, "ConnectUpDown");
                    }
                    else if ((prevX < presX && postX > presX) || (prevX > presX && postX < presX))
                    {
                        path[i].DisplayPath(playerRef, "ConnectLeftRight");
                    }
                    else if ((prevX < presX && postY > presY) || (prevY > presY && postX < presX))
                    {
                        path[i].DisplayPath(playerRef, "ConnectLeftDown");
                    }
                    else if ((prevX > presX && postY > presY) || (prevY > presY && postX > presX))
                    {
                        path[i].DisplayPath(playerRef, "ConnectRightDown");
                    }
                    else
                    {
                        path[i].DisplayPath(playerRef, "Start");
                    }
                }
            }
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
            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, map.GetTile(posX, posY).GetWorldPos().y + 3.5f, -10);
        }  // Cursor too far left
        if (Camera.main.transform.position.x - 7.4f > map.GetTile(posX, posY).GetWorldPos().x)
        {
            Camera.main.transform.position = new Vector3(map.GetTile(posX, posY).GetWorldPos().x + 7.4f, Camera.main.transform.position.y, -10);
        }  // Cursor too far right
        if (Camera.main.transform.position.x + 4.4f < map.GetTile(posX, posY).GetWorldPos().x)
        {
            Camera.main.transform.position = new Vector3(map.GetTile(posX, posY).GetWorldPos().x - 4.4f, Camera.main.transform.position.y, -10);
        }  // Cursor too far down
        if (Camera.main.transform.position.y + 3.5f < map.GetTile(posX, posY).GetWorldPos().y)
        {
            Camera.main.transform.position = new Vector3(Camera.main.transform.position.x, map.GetTile(posX, posY).GetWorldPos().y - 3.5f, -10);
        }
    }

    // RPC Functions
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

    // Unit setters
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_SetUnit(Unit unit_)
    {
        unit = unit_;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_SetUnitPrevTile(TileInfo tile)
    {
        unitPrevTile = tile;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_AddUnit(Unit unit_)
    {
        units.Add(unit_);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_RemoveUnit(Unit unit_)
    {
        units.Remove(unit_);
    }

    // Path manipulation
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_AddToPath(TileInfo tile)
    {
        path.Add(tile);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_RemoveFromPath(TileInfo tile)
    {
        path.Remove(tile);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_ClearPath()
    {
        path.Clear();
    }
}
