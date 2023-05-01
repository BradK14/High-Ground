using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class TileInfo : NetworkBehaviour
{
    // Variables
    // Initialized in the inspector
    [SerializeField] private Unit testUnitPrefab;  // TODO: Move this to mapInfo, and pass a spawned unit to this instead of its name.
    [SerializeField] private NetworkObject pathPrefab;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite pathStart;
    [SerializeField] private Sprite pathConnectUp;
    [SerializeField] private Sprite pathConnectLeft;
    [SerializeField] private Sprite pathConnectRight;
    [SerializeField] private Sprite pathConnectDown;
    [SerializeField] private Sprite pathConnectUpLeft;
    [SerializeField] private Sprite pathConnectUpRight;
    [SerializeField] private Sprite pathConnectUpDown;
    [SerializeField] private Sprite pathConnectLeftRight;
    [SerializeField] private Sprite pathConnectLeftDown;
    [SerializeField] private Sprite pathConnectRightDown;
    [SerializeField] private Sprite pathArriveFromBelow;
    [SerializeField] private Sprite pathArriveFromRight;
    [SerializeField] private Sprite pathArriveFromLeft;
    [SerializeField] private Sprite pathArriveFromAbove;
    // Initialized on spawn
    [Networked] private NetworkObject path { get; set; }
    // Initialized at run time
    //[Networked] private Resources resources { get; set; }
    //[Networked] private Structure structure { get; set; }
    [Networked] private Unit unit { get; set; }
    [Networked] private int posX { get; set; }
    [Networked] private int posY { get; set; }
    [Networked] private Vector3 worldPos { get; set; }
    [Networked] private int elevation { get; set; }
    [Networked] private bool cliffUp { get; set; }
    [Networked] private bool cliffLeft { get; set; }
    [Networked] private bool cliffRight { get; set; }
    [Networked] private bool cliffDown { get; set; }

    // Functions
    // Start Function
    public override void Spawned()
    {
        path = Runner.Spawn(pathPrefab);
    }

    // Public Functions
    // Setters/Getters
    public int GetPosX() { return posX; }
    public int GetPosY() { return posY; }
    public Vector3 GetWorldPos() { return worldPos; }
    public int GetElevation() { return elevation; }
    public bool GetCliffUp() { return cliffUp; }
    public bool GetCliffLeft() { return cliffLeft; }
    public bool GetCliffRight() { return cliffRight; }
    public bool GetCliffDown() { return cliffDown; }
    public Unit GetUnit() { return unit; }
    public void SetUnit(Unit unit_) { unit = unit_; }
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetUnit(Unit unit_) { unit = unit_; }

    // Initializors
    // Position information
    public void SetPos(int posX_, int posY_, Vector3 worldPos_)
    {
        posX = posX_;
        posY = posY_;
        worldPos = worldPos_;
        transform.position = worldPos;
        path.transform.position = worldPos;
    }

    // Information dependent on the sprites drawn on the map is gathered by the name of the sprite
    // Elevation information
    public void SetElevationByName(string groundName)
    {
        switch (groundName)
        {
            case "Ground5":
                elevation = 5;
                break;
            case "Ground4":
                elevation = 4;
                break;
            case "Ground3":
                elevation = 3;
                break;
            case "Ground2":
                elevation = 2;
                break;
            case "Ground1":
                elevation = 1;
                break;
            default:
                elevation = 0;
                break;
        }
    }

    // Cliff information
    public void SetCliffsByName(string cliffName)
    {
        // For cliff names, stick to the naming convention of favoring the directions in this order: 1st up, 2nd left, 3rd right, 4th down
        cliffUp = false;
        cliffLeft = false;
        cliffRight = false;
        cliffDown = false;
        switch (cliffName)
        {
            case "CliffUp":
                cliffUp = true;
                break;
            case "CliffLeft":
                cliffLeft = true;
                break;
            case "CliffRight":
                cliffRight = true;
                break;
            case "CliffDown":
                cliffDown = true;
                break;
            case "CliffUpLeft":
                cliffUp = true;
                cliffLeft = true;
                break;
            case "CliffLeftDown":
                cliffLeft = true;
                cliffDown = true;
                break;
            case "CliffRightDown":
                cliffRight = true;
                cliffDown = true;
                break;
            case "CliffUpRight":
                cliffUp = true;
                cliffRight = true;
                break;
            case "CliffLeftRight":
                cliffLeft = true;
                cliffRight = true;
                break;
            case "CliffUpDown":
                cliffUp = true;
                cliffDown = true;
                break;
            case "CliffUpLeftDown":
                cliffUp = true;
                cliffLeft = true;
                cliffDown = true;
                break;
            case "CliffLeftRightDown":
                cliffLeft = true;
                cliffRight = true;
                cliffDown = true;
                break;
            case "CliffUpRightDown":
                cliffUp = true;
                cliffRight = true;
                cliffDown = true;
                break;
            case "CliffUpLeftRight":
                cliffUp = true;
                cliffLeft = true;
                cliffRight = true;
                break;
            case "CliffUpLeftRightDown":
                cliffUp = true;
                cliffLeft = true;
                cliffRight = true;
                cliffDown = true;
                break;
        }
    }

    // Unit information
    public void SetUnitByName(string unitName)
    {
        unit = null;
        switch (unitName)
        {
            case "TestUnit1":
                unit = Runner.Spawn(testUnitPrefab, GetWorldPos());
                unit.SetPlayerNum(1);
                break;
            case "TestUnit2":
                unit = Runner.Spawn(testUnitPrefab, GetWorldPos());
                unit.SetPlayerNum(2);
                break;
        }
    }

    // Highlight Handlers
    public void DisableHighlight()
    {
        spriteRenderer.enabled = false;
    }

    public void EnableHighlightPathable(PlayerRef player)
    {
        spriteRenderer.color = new Vector4(0.5f, 0.5f, 1f, 0.25f);
        if (Runner.LocalPlayer == player)
        {
            spriteRenderer.enabled = true;
        }
    }

    public void EnableHighlightImmediatelyPathable(PlayerRef player)
    {
        spriteRenderer.color = new Vector4(0.5f, 0.5f, 1f, 0.5f);
        if (Runner.LocalPlayer == player)
        {
            spriteRenderer.enabled = true;
        }
    }

    // Path Handlers
    public void RemovePath()
    {
        path.GetComponent<SpriteRenderer>().enabled = false;
    }

    public void DisplayPath(PlayerRef player, string pathType)
    {
        switch (pathType)
        {
            case "Start":
                path.GetComponent<SpriteRenderer>().sprite = pathStart;
                break;
            case "ConnectUp":
                path.GetComponent<SpriteRenderer>().sprite = pathConnectUp;
                break;
            case "ConnectLeft":
                path.GetComponent<SpriteRenderer>().sprite = pathConnectLeft;
                break;
            case "ConnectRight":
                path.GetComponent<SpriteRenderer>().sprite = pathConnectRight;
                break;
            case "ConnectDown":
                path.GetComponent<SpriteRenderer>().sprite = pathConnectDown;
                break;
            case "ConnectUpLeft":
                path.GetComponent<SpriteRenderer>().sprite = pathConnectUpLeft;
                break;
            case "ConnectUpRight":
                path.GetComponent<SpriteRenderer>().sprite = pathConnectUpRight;
                break;
            case "ConnectUpDown":
                path.GetComponent<SpriteRenderer>().sprite = pathConnectUpDown;
                break;
            case "ConnectLeftRight":
                path.GetComponent<SpriteRenderer>().sprite = pathConnectLeftRight;
                break;
            case "ConnectLeftDown":
                path.GetComponent<SpriteRenderer>().sprite = pathConnectLeftDown;
                break;
            case "ConnectRightDown":
                path.GetComponent<SpriteRenderer>().sprite = pathConnectRightDown;
                break;
            case "ArriveFromBelow":
                path.GetComponent<SpriteRenderer>().sprite = pathArriveFromBelow;
                break;
            case "ArriveFromRight":
                path.GetComponent<SpriteRenderer>().sprite = pathArriveFromRight;
                break;
            case "ArriveFromLeft":
                path.GetComponent<SpriteRenderer>().sprite = pathArriveFromLeft;
                break;
            case "ArriveFromAbove":
                path.GetComponent<SpriteRenderer>().sprite = pathArriveFromAbove;
                break;
        }

        if (player == Runner.LocalPlayer)
        {
            path.GetComponent<SpriteRenderer>().enabled = true;
        }
    }
}
