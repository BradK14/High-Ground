using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class TileInfo : NetworkBehaviour
{
    // Variables
    // Initialized in the inspector
    [SerializeField] private Unit testUnitPrefab;
    [SerializeField] private Unit builderPrefab;
    [SerializeField] private Unit scoutPrefab;
    [SerializeField] private Unit fighterAPrefab;
    [SerializeField] private Unit fighterBPrefab;
    [SerializeField] private Unit supportPrefab;
    [SerializeField] private Unit harasserPrefab;
    [SerializeField] private Unit destroyerPrefab;
    [SerializeField] private Unit powerhousePrefab;
    [SerializeField] private Structure testStructurePrefab;
    [SerializeField] private Structure basePrefab;
    [SerializeField] private Structure productionAPrefab;
    [SerializeField] private Structure productionBPrefab;
    [SerializeField] private Structure wallPrefab;
    [SerializeField] private Structure staticDefensePrefab;
    [SerializeField] private Structure constructionSitePrefab;
    [SerializeField] private NetworkObject indicatorPrefab;
    [SerializeField] private SpriteRenderer highlightRenderer;
    [SerializeField] private SpriteRenderer fogRenderer;
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
    [Networked] private NetworkObject indicator { get; set; }
    [Networked] private int resources { get; set; }
    private bool settingUpUnit;
    private bool settingUpStructure;
    private bool settingUpConstructionSite;
    // Initialized at run time
    [Networked] private bool hasResources { get; set; }
    [Networked] private Structure structure { get; set; }
    [Networked] private Unit unit { get; set; }
    [Networked] private int posX { get; set; }
    [Networked] private int posY { get; set; }
    [Networked] private Vector3 worldPos { get; set; }
    [Networked] private int elevation { get; set; }
    [Networked] private bool cliffUp { get; set; }
    [Networked] private bool cliffLeft { get; set; }
    [Networked] private bool cliffRight { get; set; }
    [Networked] private bool cliffDown { get; set; }
    private string uNameToSpawn;
    private string sNameToSpawn;
    private string cSNameToSpawn;
    private int uPNumToSpawn;
    private int sPNumToSpawn;
    private int cSPNumToSpawn;
    private Unit cSBuilder;

    // Functions
    // Start Function
    public override void Spawned()
    {
        indicator = Runner.Spawn(indicatorPrefab);
        hasResources = false;
        resources = 0;
        settingUpUnit = false;
        settingUpStructure = false;
        settingUpConstructionSite = false;
}

    // Update Function (for spawning units or structures)
    public override void FixedUpdateNetwork()
    {
        if (Runner.IsServer)
        {
            if (settingUpUnit)
            {
                SetUnitByName(uNameToSpawn, uPNumToSpawn);
                settingUpUnit = false;
            }

            if (settingUpStructure)
            {
                SetStructureByName(sNameToSpawn, sPNumToSpawn);
                settingUpStructure = false;
            }

            if (settingUpConstructionSite)
            {
                SetConstructionSiteByName(cSNameToSpawn, cSPNumToSpawn, cSBuilder);
                settingUpConstructionSite = false;
            }
        }
    }

    // Public Functions
    // Getters
    public bool HasResources() { return hasResources; }
    public int GetResources() { return resources; }
    public int GetPosX() { return posX; }
    public int GetPosY() { return posY; }
    public Vector3 GetWorldPos() { return worldPos; }
    public int GetElevation() { return elevation; }
    public bool GetCliffUp() { return cliffUp; }
    public bool GetCliffLeft() { return cliffLeft; }
    public bool GetCliffRight() { return cliffRight; }
    public bool GetCliffDown() { return cliffDown; }
    public Unit GetUnit() { return unit; }
    public Structure GetStructure() { return structure; }
    // Setters
    public void SetUnit(Unit unit_) { unit = unit_; }
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetUnit(Unit unit_) { unit = unit_; }
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetStructure(Structure structure_) { structure = structure_; }
    public bool GetFog() { return fogRenderer.enabled; }

    // Initializors
    // Position information
    public void SetPos(int posX_, int posY_, Vector3 worldPos_)
    {
        posX = posX_;
        posY = posY_;
        worldPos = worldPos_;
        transform.position = worldPos;
        indicator.transform.position = worldPos;
    }

    // Information dependent on the sprites drawn on the map is gathered by the name of the sprite
    // Resources
    public void SetResources(bool hasResources_)
    {
        hasResources = hasResources_;

        if (hasResources_)
        {
            resources = 100;
        }
        else
        {
            resources = 0;
        }
    }
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
    public void SetUnitByName(string unitName, int unitPNum)
    {
        unit = null;

        switch (unitName)
        {
            case "TestUnit":
                unit = Runner.Spawn(testUnitPrefab, GetWorldPos());
                break;
            case "Builder":
                unit = Runner.Spawn(builderPrefab, GetWorldPos());
                break;
            case "Scout":
                unit = Runner.Spawn(scoutPrefab, GetWorldPos());
                break;
            case "FighterA":
                unit = Runner.Spawn(fighterAPrefab, GetWorldPos());
                break;
            case "FighterB":
                unit = Runner.Spawn(fighterBPrefab, GetWorldPos());
                break;
            case "Support":
                unit = Runner.Spawn(supportPrefab, GetWorldPos());
                break;
            case "Harasser":
                unit = Runner.Spawn(harasserPrefab, GetWorldPos());
                break;
            case "Destroyer":
                unit = Runner.Spawn(destroyerPrefab, GetWorldPos());
                break;
            case "Powerhouse":
                unit = Runner.Spawn(powerhousePrefab, GetWorldPos());
                break;
        }

        // Do all the remaining set up for the unit and this tile
        if (unit != null)
        {
            unit.SetPlayerNum(unitPNum);  // Set it to it's appropriate player
            unit.SetCurTile(this);
            unit.Rpc_RevealIfSeen();
        }
    }

    // Structure information
    public void SetStructureByName(string structureName, int structurePNum)
    {
        structure = null;
        switch (structureName)
        {
            case "TestStructure":
                structure = Runner.Spawn(testStructurePrefab, GetWorldPos());
                break;
            case "Base":
                structure = Runner.Spawn(basePrefab, GetWorldPos());
                break;
            case "ProductionA":
                structure = Runner.Spawn(productionAPrefab, GetWorldPos());
                break;
            case "ProductionB":
                structure = Runner.Spawn(productionBPrefab, GetWorldPos());
                break;
            case "Wall":
                structure = Runner.Spawn(wallPrefab, GetWorldPos());
                break;
            case "StaticDefense":
                structure = Runner.Spawn(staticDefensePrefab, GetWorldPos());
                break;
        }

        if (structure != null)
        {
            structure.SetPlayerNum(structurePNum);
            structure.SetCurTile(this);
            structure.Rpc_RevealIfSeen();
        }
    }

    public void SetConstructionSiteByName(string cSName, int cSPNum, Unit builder)
    {
        structure = Runner.Spawn(constructionSitePrefab, GetWorldPos());

        structure.SetPlayerNum(cSPNum);
        structure.SetCurTile(this);

        structure.Rpc_SetCSName(cSName);
        structure.Rpc_SetCSBuilder(builder);

        builder.Rpc_SetCS(structure);
        
        structure.Rpc_RevealIfSeen();
    }

    // Highlight Handlers
    public void DisableHighlight()
    {
        highlightRenderer.enabled = false;
    }

    public void EnableHighlightMoveable(PlayerRef player)
    {
        highlightRenderer.color = new Vector4(0.5f, 0.5f, 1f, 0.25f);
        if (Runner.LocalPlayer == player)
        {
            highlightRenderer.enabled = true;
        }
    }

    public void EnableThickHighlightMoveable(PlayerRef player)
    {
        highlightRenderer.color = new Vector4(0.5f, 0.5f, 1f, 0.5f);
        if (Runner.LocalPlayer == player)
        {
            highlightRenderer.enabled = true;
        }
    }

    public void EnableHighlightAttackable(PlayerRef player)
    {
        highlightRenderer.color = new Vector4(1f, 0.5f, 0.5f, 0.5f);
        if (Runner.LocalPlayer == player)
        {
            highlightRenderer.enabled = true;
        }
    }

    public void EnableThickHighlightAttackable(PlayerRef player)
    {
        highlightRenderer.color = new Vector4(1f, 0.5f, 0.5f, 0.75f);
        if (Runner.LocalPlayer == player)
        {
            highlightRenderer.enabled = true;
        }
    }
    public void EnableHighlightSupportable(PlayerRef player)
    {
        highlightRenderer.color = new Vector4(0.5f, 1f, 0.5f, 0.5f);
        if (Runner.LocalPlayer == player)
        {
            highlightRenderer.enabled = true;
        }
    }

    public void EnableThickHighlightSupportable(PlayerRef player)
    {
        highlightRenderer.color = new Vector4(0.5f, 1f, 0.5f, 0.75f);
        if (Runner.LocalPlayer == player)
        {
            highlightRenderer.enabled = true;
        }
    }

    // Fog Handlers
    public void DisableFog()
    {
        fogRenderer.enabled = false;
        if (unit != null)
        {
            unit.EnableSprite(true);
        }
        if (structure != null)
        {
            structure.EnableSprite(true);
        }
    }

    public void EnableFog()
    {
        fogRenderer.enabled = true;
        if (unit != null)
        {
            unit.EnableSprite(false);
        }
    }

    // Indicator Handlers
    public void RemoveIndicator()
    {
        indicator.GetComponent<SpriteRenderer>().enabled = false;
    }

    public void DisplayIndicator(PlayerRef player, string indicatorType)
    {
        switch (indicatorType)
        {
            case "ConnectUp":
                indicator.GetComponent<SpriteRenderer>().sprite = pathConnectUp;
                break;
            case "ConnectLeft":
                indicator.GetComponent<SpriteRenderer>().sprite = pathConnectLeft;
                break;
            case "ConnectRight":
                indicator.GetComponent<SpriteRenderer>().sprite = pathConnectRight;
                break;
            case "ConnectDown":
                indicator.GetComponent<SpriteRenderer>().sprite = pathConnectDown;
                break;
            case "ConnectUpLeft":
                indicator.GetComponent<SpriteRenderer>().sprite = pathConnectUpLeft;
                break;
            case "ConnectUpRight":
                indicator.GetComponent<SpriteRenderer>().sprite = pathConnectUpRight;
                break;
            case "ConnectUpDown":
                indicator.GetComponent<SpriteRenderer>().sprite = pathConnectUpDown;
                break;
            case "ConnectLeftRight":
                indicator.GetComponent<SpriteRenderer>().sprite = pathConnectLeftRight;
                break;
            case "ConnectLeftDown":
                indicator.GetComponent<SpriteRenderer>().sprite = pathConnectLeftDown;
                break;
            case "ConnectRightDown":
                indicator.GetComponent<SpriteRenderer>().sprite = pathConnectRightDown;
                break;
            case "ArriveFromBelow":
                indicator.GetComponent<SpriteRenderer>().sprite = pathArriveFromBelow;
                break;
            case "ArriveFromRight":
                indicator.GetComponent<SpriteRenderer>().sprite = pathArriveFromRight;
                break;
            case "ArriveFromLeft":
                indicator.GetComponent<SpriteRenderer>().sprite = pathArriveFromLeft;
                break;
            case "ArriveFromAbove":
                indicator.GetComponent<SpriteRenderer>().sprite = pathArriveFromAbove;
                break;
        }

        if (player == Runner.LocalPlayer)
        {
            indicator.GetComponent<SpriteRenderer>().enabled = true;
        }
    }

    /* Rpcs
     * Rpc_SetResources(int resourcesRemaining)
     * public void Rpc_SetUpSpawnUnit(string uName, int uNum)
     * public void Rpc_SetUpSpawnStructure(string sName, int sNum)
     * public void Rpc_SetUpSpawnConstructionSite(string cSName, int cSPNum, Unit builder)
     */
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetResources(int resourcesRemaining)
    {
        resources = resourcesRemaining;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetUpSpawnUnit(string uName, int uNum)
    {
        if (Runner.IsServer)
        {
            if (unit == null)
            {
                uNameToSpawn = uName;
                uPNumToSpawn = uNum;
                settingUpUnit = true;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetUpSpawnStructure(string sName, int sNum)
    {
        if (Runner.IsServer)
        {
            if (structure == null)
            {
                sNameToSpawn = sName;
                sPNumToSpawn = sNum;
                settingUpStructure = true;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetUpSpawnConstructionSite(string cSName, int cSPNum, Unit builder)
    {
        if (Runner.IsServer)
        {
            if (structure == null)
            {
                cSNameToSpawn = cSName;
                cSPNumToSpawn = cSPNum;
                cSBuilder = builder;
                settingUpConstructionSite = true;
            }
        }
    }
}
