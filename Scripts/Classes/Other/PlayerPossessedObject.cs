using Fusion;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PlayerPossessedObject : NetworkBehaviour
{
    // Variables
    // Initialized in the inspector
    [SerializeField] protected SpriteRenderer sprite;
    [SerializeField] protected Animator animator;
    [SerializeField] protected SpriteRenderer actionsLeftIndicator;
    [SerializeField] protected TextMeshPro HPDisplay;
    // Initialized at spawn
    protected bool setupComplete;
    protected bool ppoSetupComplete;
    protected List<ActiveAbility> activeAbilities;
    protected List<int> actionsLimited;
    protected int selectedAbility;
    protected List<TileInfo> highlights { get; set; }  // For highlighting general tiles that may relate to the unit's ability
    protected List<TileInfo> thickHighlights { get; set; }  // For highlighting areas related to where cursor is pointing
    protected List<TileInfo> abilityIndicators { get; set; }  // Used to display paths when moving, multiple targeted locations for abilities, etc
    protected bool isDestroyed;
    // Set at run time
    private BaseUI ui;
    protected MapInfo map;
    [Networked] protected TileInfo curTile { get; set; }
    [Networked] protected int playerNum { get; set; }
    protected PlayerRef player;
    protected PlayerController playerController;
    // Initialize these variables in child spawn function
    protected string objectName;
    protected string objectType;
    protected int maxHP;
    protected int HP;
    protected int attack;
    protected int attackMinRange;
    protected int attackMaxRange;
    protected string attackType;
    protected int moveRange;
    protected int visionRange;
    protected string description;
    protected bool setupAbilitiesReady;
    protected bool allActionsUsed;


    // Functions
    // Start
    public override void Spawned()
    {
        isDestroyed = false;
        setupComplete = false;
        setupAbilitiesReady = false;
        ppoSetupComplete = false;
        allActionsUsed = true;
        selectedAbility = -1;
        highlights = new List<TileInfo>();
        thickHighlights = new List<TileInfo>();
        abilityIndicators = new List<TileInfo>();
        activeAbilities = new List<ActiveAbility>();
        actionsLimited = new List<int>();
        StartCoroutine(Setup());
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        isDestroyed = true;
    }

    // Coroutines
    // Setup
    protected virtual IEnumerator Setup()
    {
        // Get a reference to the ui
        while (ui == null)
        {
            ui = FindObjectOfType<BaseUI>();

            yield return null;
        }

        // Get a reference to the map
        while (map == null)
        {
            map = FindObjectOfType<MapInfo>();

            yield return null;
        }

        // Wait for abilities to be prepared by child classes before setting up their references
        bool abilitiesPrepared = false;
        while (!abilitiesPrepared)
        {
            if (setupAbilitiesReady)
            {
                foreach (ActiveAbility aa in activeAbilities)
                {
                    aa.SetReady(true);
                    aa.SetObject(this);
                    aa.subMenuActive = false;
                    aa.SetMap(map);
                }

                abilitiesPrepared = true;
            }

            yield return null;
        }

        // Wait until a player has been assigned
        while (playerNum == 0) { yield return new WaitForSeconds(0.1f); }

        // After a number is given, get the player's reference
        while (player == null || player == PlayerRef.None)
        {
            Dictionary<int, PlayerController> players = FindObjectOfType<GameManager>().GetPlayers();
            if (players[playerNum].GetPlayerRef() != null || players[playerNum].GetPlayerRef() != PlayerRef.None)
            {
                player = players[playerNum].GetPlayerRef();
                playerController = players[playerNum];
            }

            yield return null;
        }

        // After a player has been assigned, give the player this unit's vision
        bool initialVisionSet = false;
        while (!initialVisionSet)
        {
            if (Runner.LocalPlayer == player)
            {
                if (curTile != null && map.SetupIsComplete())  // Wait for the tile's to finish getting setup
                {
                    DisableFogByVision();
                    initialVisionSet = true;
                }
            }
            else
            {
                initialVisionSet = true;
            }

            yield return null;
        }

        ppoSetupComplete = true;
    }

    // Public Functions
    // Setters
    public virtual void SetPlayerNum(int playerNum_)
    {
        playerNum = playerNum_;
        playerNumChanged = !playerNumChanged;
    }
    public void SetCurTile(TileInfo tile) { curTile = tile; Rpc_SetTile(tile); }
    public void SetAllActionsUsed(bool used) { allActionsUsed = used; }
    public void SetHighlights(List<TileInfo> tiles) { highlights = tiles; }
    public void SetThickHighlights(List<TileInfo> tiles) { thickHighlights = tiles; }
    public void SetAbilityIndicators(List<TileInfo> tiles) { abilityIndicators = tiles; }
    // Getters
    public bool IsDestroyed() { return isDestroyed; }
    public BaseUI GetUI() { return ui; }
    public PlayerRef GetPlayer() { return player; }
    public int GetPlayerNum() { return playerNum; }
    public PlayerController GetPlayerController() { return playerController; }
    public TileInfo GetTile() { return curTile; }
    public string GetObjectName() { return objectName; }
    public string GetObjectType() { return objectType; }
    public Sprite GetSprite() { return sprite.sprite; }
    public string GetDescription() { return description; }
    public int GetMaxHP() { return maxHP; }
    public int GetHP() { return HP; }
    public int GetAttack() { return attack; }
    public int GetAttackMinRange() { return attackMinRange; }
    public int GetAttackMaxRange() { return attackMaxRange; }
    public string GetAttackType() { return attackType; }
    public int GetMoveRange() { return moveRange; }
    public int GetVisionRange() { return visionRange; }
    public bool HasUsedAllActions() { return allActionsUsed; }
    public List<TileInfo> GetHighlights() { return highlights; }
    public List<TileInfo> GetThickHighlights() { return thickHighlights; }
    public List<TileInfo> GetAbilityIndicators() { return abilityIndicators; }
    public List<ActiveAbility> GetActiveAbilities() { return activeAbilities; }
    // Toggles
    public void EnableSprite(bool enable)
    {
        sprite.enabled = enable;

        ResetHPDisplayInfo();
        HPDisplay.enabled = enable;

        if (Runner.LocalPlayer == player)
        {
            actionsLeftIndicator.enabled = enable;
        }
    }

    /* Functions
     * SelectAbilityPreview(int selection);
     * SelectAbility(int selection);
     * ChangePosAbility(TileInfo tile);
     * UseAbility(TileInfo tile);
     * Deselect();
     * StartTurn();
     * EndTurn();
     * DisableFogByVision();
     * CheckIfVisible(TileInfo tile);
     * CheckAndSetIfAllActionsHaveBeenUsed();
     * ResetActions();
     * TakeDamage(int damage);
     * ClearHighlights();
     * ClearThickHighlights();
     * ClearAbilityIndicators();
     * ResetHPDisplayInfo();
     */

    // Ability handlers, usually called from player controller
    public void SelectAbilityPreview(int selection)
    {
        if (selectedAbility == -1)  // Only do something while deciding on an ability to use
        {
            ClearHighlights();
            activeAbilities[selection].SelectPreview();
        }
        else
        {
            activeAbilities[selectedAbility].SelectPreview();
        }
    }

    // Sets a specific ability to active
    public bool SelectAbility(int selection)
    {
        bool abilityUsed = false;

        if (selectedAbility == -1)
        {
            abilityUsed = activeAbilities[selection].Select();
            selectedAbility = selection;
        }
        else  // An ability is already selected and we have an ability's sub menu active
        {
            abilityUsed = activeAbilities[selectedAbility].Select();
        }

        if (abilityUsed)
        {
            activeAbilities[selectedAbility].SetReady(false);
            selectedAbility = -1;
            // Disable all highlights
            ClearHighlights();
            ClearThickHighlights();
            ClearAbilityIndicators();
            // Set if all actions have been used or not
            CheckAndSetIfAllActionsHaveBeenUsed();
        }

        return abilityUsed;
    }

    // Recieve new position from the cursor
    public void ChangePosAbility(TileInfo tile)
    {
        activeAbilities[selectedAbility].ChangePos(tile);
    }

    // Recieve a selection command
    public virtual bool UseAbility(TileInfo tile)
    {
        bool abilityUsed = activeAbilities[selectedAbility].Use(tile);

        if (abilityUsed)
        {
            activeAbilities[selectedAbility].SetReady(false);
            selectedAbility = -1;
            // Disable all highlights
            ClearHighlights();
            ClearThickHighlights();
            ClearAbilityIndicators();
            // Set if all actions have been used or not
            CheckAndSetIfAllActionsHaveBeenUsed();
        }

        return abilityUsed;
    }

    // Used by player controller to stop readying an ability and remove paths/highlights etc
    public void Deselect()
    {
        if (selectedAbility >= 0)
        {
            activeAbilities[selectedAbility].subMenuActive = false;
        }

        selectedAbility = -1;

        // Disable all highlights
        ClearHighlights();
        ClearThickHighlights();
        ClearAbilityIndicators();
    }

    // Turn start/end functions to be given definition if necessary by children
    public virtual void StartTurn() { ResetActions(); }
    public virtual void EndTurn() { ResetActions(); }

    public void DisableFogByVision()
    {
        // First get all tiles in unit's vision range
        List<TileInfo> seenTiles = new List<TileInfo>();
        seenTiles.Add(curTile);

        int jPrev = 0;
        for (int i = visionRange + 1; i > 0; i--)
        {
            int jMax = seenTiles.Count;  // Only continue using new additions to the list
            for (int j = jPrev; j < jMax; j++)
            {
                // Create positional variables for easier readability
                int x = seenTiles[j].GetPosX();
                int leftX = x - 1;
                int rightX = x + 1;
                int y = seenTiles[j].GetPosY();
                int upY = y - 1;
                int downY = y + 1;

                // Check up
                if (upY >= 0)
                {
                    if (!seenTiles.Contains(map.GetTile(x, upY)))
                    {
                        seenTiles.Add(map.GetTile(x, upY));
                    }
                }
                // Check left
                if (leftX >= 0)
                {
                    if (!seenTiles.Contains(map.GetTile(leftX, y)))
                    {
                        seenTiles.Add(map.GetTile(leftX, y));
                    }
                }
                // Check right
                if (rightX <= map.GetMaxX())
                {
                    if (!seenTiles.Contains(map.GetTile(rightX, y)))
                    {
                        seenTiles.Add(map.GetTile(rightX, y));
                    }
                }
                // Check down
                if (downY <= map.GetMaxY())
                {
                    if (!seenTiles.Contains(map.GetTile(x, downY)))
                    {
                        seenTiles.Add(map.GetTile(x, downY));
                    }
                }
            }
            jPrev = jMax;  // Next iteration continues without repeating tiles
        }

        // Reveal tiles seen by unit
        foreach (TileInfo t in seenTiles)
        {
            if (!t.GetFog())  // If the tile is already seen and doesn't need to be checked
            {
                continue;
            }
            else if (t == curTile)  // If the tile is the same one the unit is on
            {
                t.DisableFog();
            }
            else if (CheckIfVisible(t))  // Valid if there is nothing blocking this space
            {
                // Check if there is nothing blocking this tile
                // Variables for tracking info easier
                int startX = curTile.GetPosX();
                int startY = curTile.GetPosY();
                int targetX = t.GetPosX();
                int targetY = t.GetPosY();

                int xDir;  // Determines the direction the target is in
                int distFromXtoX; // Determines the distance to the target
                if (targetX > startX)
                {
                    xDir = 1;
                    distFromXtoX = targetX - startX;
                }
                else if (targetX < startX)
                {
                    xDir = -1;
                    distFromXtoX = startX - targetX;
                }
                else
                {
                    xDir = 0;
                    distFromXtoX = 0;
                }


                int yDir;
                int distFromYtoY;
                if (targetY > startY)
                {
                    yDir = 1;
                    distFromYtoY = targetY - startY;
                }
                else if (targetY < startY)
                {
                    yDir = -1;
                    distFromYtoY = startY - targetY;
                }
                else
                {
                    yDir = 0;
                    distFromYtoY = 0;
                }

                double xDirInc = distFromXtoX; // How much to move in the x direction
                double yDirInc = distFromYtoY;  // How much to move in the y direction

                bool visible = true;  // Assume visible until proven false

                double temp = xDirInc;
                xDirInc /= (xDirInc + yDirInc);
                yDirInc /= (temp + yDirInc);

                xDirInc *= xDir;
                yDirInc *= yDir;

                double x = startX + 0.5;
                double y = startY + 0.5;
                double distTraveled = 0;
                double distToTravel;
                bool travelingX;
                if (distFromXtoX > distFromYtoY)
                {
                    distToTravel = distFromXtoX;
                    travelingX = true;
                }
                else
                {
                    distToTravel = distFromYtoY;
                    travelingX = false;
                }

                while (distTraveled < distToTravel)
                {
                    // Increment
                    x += xDirInc;
                    y += yDirInc;

                    if (travelingX)
                    {
                        distTraveled += xDirInc * xDir;
                    }
                    else
                    {
                        distTraveled += yDirInc * yDir;
                    }

                    // Check tile
                    int xInt = (int)x;
                    int yInt = (int)y;

                    // When on an intersection between four tiles check for diagonally placed walls
                    if (x % 1 == 0 && y % 1 == 0)
                    {
                        // Moving Down Right
                        if (xDir == 1 && yDir == 1)
                        {
                            if (!CheckIfVisible(map.GetTile(xInt - xDir, yInt)) && !CheckIfVisible(map.GetTile(xInt, yInt - yDir)))
                            {
                                visible = false;
                                break;
                            }
                        }  // Moving Up Left
                        else if (xDir == -1 && yDir == -1)
                        {
                            if (!CheckIfVisible(map.GetTile(xInt + xDir, yInt)) && !CheckIfVisible(map.GetTile(xInt, yInt + yDir)))
                            {
                                visible = false;
                                break;
                            }
                        }  // Moving Up Right
                        else if (xDir == 1 && yDir == -1)
                        {
                            if (!CheckIfVisible(map.GetTile(xInt, yInt)) && !CheckIfVisible(map.GetTile(xInt - 1, yInt - 1)))
                            {
                                visible = false;
                                break;
                            }
                        }  // Moving Down Left
                        else if (xDir == -1 && yDir == 1)
                        {
                            if (!CheckIfVisible(map.GetTile(xInt, yInt)) && !CheckIfVisible(map.GetTile(xInt - 1, yInt - 1)))
                            {
                                visible = false;
                                break;
                            }
                        }
                    }  // If not on an intersection
                    else if (!CheckIfVisible(map.GetTile(xInt, yInt)))
                    {
                        visible = false;
                        break;
                    }
                }

                // Finally, set the tiles visibility to true if it is decided to be visible
                if (visible)
                {
                    t.DisableFog();
                }
            }
        }
    }

    // Checks both elevation and distance from unit
    private bool CheckIfVisible(TileInfo tile)
    {
        // Checking distance from current tile to the other tile
        int totalX = curTile.GetPosX() - tile.GetPosX();
        int totalY = curTile.GetPosY() - tile.GetPosY();
        if (totalX < 0) { totalX *= -1; }
        if (totalY < 0) { totalY *= -1; }
        int total = totalX + totalY;

        if (total == 1)  // Adjacent tiles are visible as long as there is no cliff between them
        {
            if (curTile.GetPosY() - 1 == tile.GetPosY() && !curTile.GetCliffUp() && !tile.GetCliffDown()) { return true; }  // If the tile is above
            if (curTile.GetPosX() - 1 == tile.GetPosX() && !curTile.GetCliffLeft() && !tile.GetCliffRight()) { return true; }  // Left
            if (curTile.GetPosX() + 1 == tile.GetPosX() && !curTile.GetCliffRight() && !tile.GetCliffLeft()) { return true; }  // Right
            if (curTile.GetPosY() + 1 == tile.GetPosY() && !curTile.GetCliffDown() && !tile.GetCliffUp()) { return true; }  // Down
        }
        if (total > visionRange)  // A tile farther than the range of the objects vision is only valid with a high ground advantage
        {
            if (curTile.GetElevation() > tile.GetElevation())
            {
                return true;
            }
        }
        else if (curTile.GetElevation() >= tile.GetElevation())
        {
            return true;
        }

        return false;
    }

    protected void CheckAndSetIfAllActionsHaveBeenUsed()
    {
        // 
        allActionsUsed = true;
        for (int i = 0; i < activeAbilities.Count; i++)
        {
            if (!activeAbilities[i].IsReady())
            {
                if (actionsLimited.Contains(i))
                {
                    allActionsUsed = true;
                    break;
                }
            }
            else
            {
                allActionsUsed = false;
            }
        }

        // Set an indicator on screen for the user to know if all actions have been used or not
        if (allActionsUsed)
        {
            actionsLeftIndicator.color = new Vector4(1.0f, 0f, 0f, 1.0f);
        }
        else
        {
            actionsLeftIndicator.color = new Vector4(1.0f, 1.0f, 0f, 1.0f);
        }
    }

    public void ResetActions()
    {
        if (activeAbilities.Count > 0)
        {
            allActionsUsed = false;
            foreach (ActiveAbility a in activeAbilities)
            {
                a.SetReady(true);
            }
        }

        if (!allActionsUsed)
        {
            actionsLeftIndicator.color = new Vector4(0f, 1.0f, 0f, 1.0f);
        }
        else
        {
            actionsLeftIndicator.color = new Vector4(1.0f, 0f, 0f, 1.0f);
        }
    }

    // Receive effects from other sources
    public virtual void TakeDamage(int damage)
    {
        HP -= damage;
        Rpc_SetHP(HP);
    }

    public void RecieveHP(int health)
    {
        HP += health;
        
        if (HP > maxHP)
        {
            HP = maxHP;
        }

        Rpc_SetHP(HP);
    }

    // Highlight and tile info lists
    private void ClearHighlights()
    {
        foreach (TileInfo t in highlights)
        {
            t.DisableHighlight();
        }

        highlights.Clear();
    }
    private void ClearThickHighlights()
    {
        foreach (TileInfo t in thickHighlights)
        {
            t.DisableHighlight();
        }

        thickHighlights.Clear();
    }
    private void ClearAbilityIndicators()
    {
        foreach (TileInfo t in abilityIndicators)
        {
            t.RemoveIndicator();
        }

        abilityIndicators.Clear();
    }

    protected void ResetHPDisplayInfo()
    {
        HPDisplay.text = HP.ToString();
        if (HP < maxHP)
        {
            HPDisplay.color = new Vector4(1.0f, 1.0f, 0f, 1.0f);
        }
        else
        {
            HPDisplay.color = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        }
    }

    /* Functions to define limitations to apply to common abilities used across many objects
     * IsPathable(TileInfo first, TileInfo second);
     * CheckPathableUp(TileInfo first, TileInfo second);
     * CheckPathableLeft(TileInfo first, TileInfo second);
     * CheckPathableRight(TileInfo first, TileInfo second);
     * CheckPathableDown(TileInfo first, TileInfo second);
     * CheckAttackableUp(TileInfo first, TileInfo second);
     * CheckAttackableLeft(TileInfo first, TileInfo second);
     * CheckAttackableRight(TileInfo first, TileInfo second);
     * CheckAttackableDown(TileInfo first, TileInfo second);
     */
    // Default move limitations
    // Checks if the adjacent space is pathable
    public bool CheckPathable(TileInfo first, TileInfo second)
    {
        // Don't path through resources
        if (second.HasResources())
        {
            return false;
        }

        // Don't path when there is a visible enemy in the way
        if (second.GetUnit() != null)
        {
            if (second.GetUnit().GetPlayerNum() != playerNum
                && !second.GetFog())
            {
                return false;
            }
        }

        // Don't path onto unpathable structures
        if (second.GetStructure() != null)
        {
            if (!second.GetStructure().IsPathable()
                && !second.GetFog())
            {
                return false;
            }
        }

        // Up
        if (first.GetPosY() > second.GetPosY())
        {
            return CheckPathableUp(first, second);
        }  // Left
        else if (first.GetPosX() > second.GetPosX())
        {
            return CheckPathableLeft(first, second);
        }  // Right
        else if (first.GetPosX() < second.GetPosX())
        {
            return CheckPathableRight(first, second);
        }  // Down
        else if (first.GetPosY() < second.GetPosY())
        {
            return CheckPathableDown(first, second);
        }

        // If the second tile is the same as the first
        return false;
    }

    public bool CheckPathableUp(TileInfo first, TileInfo second)
    {
        // Don't path when cliffs are blocking the way
        if (first.GetCliffUp()
            || second.GetCliffDown())
        {
            return false;
        }

        return true;
    }

    public bool CheckPathableLeft(TileInfo first, TileInfo second)
    {
        if (first.GetCliffLeft()
            || second.GetCliffRight())
        {
            return false;
        }

        return true;
    }

    public bool CheckPathableRight(TileInfo first, TileInfo second)
    {
        if (first.GetCliffRight()
            || second.GetCliffLeft())
        {
            return false;
        }

        return true;
    }

    public bool CheckPathableDown(TileInfo first, TileInfo second)
    {
        if (first.GetCliffDown()
            || second.GetCliffUp())
        {
            return false;
        }

        return true;
    }

    // Attack Limitations
    public bool CheckAttackableUp(TileInfo first, TileInfo second)
    {
        if (!second.GetFog())
        {
            if (attackType == "Melee")
            {
                if (!first.GetCliffUp() && !second.GetCliffDown())
                {
                    return true;
                }
            }
            else if (attackType == "Ranged")
            {
                return true;
            }
        }

        return false;
    }

    public bool CheckAttackableLeft(TileInfo first, TileInfo second)
    {
        if (!second.GetFog())
        {
            if (attackType == "Melee")
            {
                if (!first.GetCliffLeft() && !second.GetCliffRight())
                {
                    return true;
                }
            }
            else if (attackType == "Ranged")
            {
                return true;
            }
        }

        return false;
    }

    public bool CheckAttackableRight(TileInfo first, TileInfo second)
    {
        if (!second.GetFog())
        {
            if (attackType == "Melee")
            {
                if (!first.GetCliffRight() && !second.GetCliffLeft())
                {
                    return true;
                }
            }
            else if (attackType == "Ranged")
            {
                return true;
            }
        }

        return false;
    }

    public bool CheckAttackableDown(TileInfo first, TileInfo second)
    {
        if (!second.GetFog())
        {
            if (attackType == "Melee")
            {
                if (!first.GetCliffDown() && !second.GetCliffUp())
                {
                    return true;
                }
            }
            else if (attackType == "Ranged")
            {
                return true;
            }
        }

        return false;
    }

    /* RPCs
     * Rpc_DestroySelf()
     * SetHP(int HP_);
     * SetTransform(Vector3 pos);
     * RevealIfSeen();
     * SetTile(TileInfo tile);
     * SetAllActionsUsed(bool allActionsUsed_);
     */
    [Rpc(RpcSources.All, RpcTargets.All)]
    protected void Rpc_DestroySelf()
    {
        isDestroyed = true;

        if (Runner.IsServer)
        {
            Runner.Despawn(GetComponent<NetworkObject>());
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    protected void Rpc_SetHP(int HP_)
    {
        HP = HP_;

        ResetHPDisplayInfo();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SetTransform(Vector3 pos) { transform.position = pos; }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public virtual void Rpc_RevealIfSeen()
    {
        if (!curTile.GetFog())
        {
            sprite.enabled = true;
            HPDisplay.enabled = true;
        }
        else
        {
            sprite.enabled = false;
            HPDisplay.enabled = false;
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    protected void Rpc_SetTile(TileInfo tile)
    {
        curTile = tile;
    }

    // On Changed Functions
    // When changing the objects player number, change its color to either blue for allied, red for enemy, or white/black for neutral
    // When it is allied, also add this object to the player controller's list, and set a player ref on this object to its owners
    [Networked(OnChanged = nameof(OnPlayerNumChanged))]
    public NetworkBool playerNumChanged { get; set; }
    public static void OnPlayerNumChanged(Changed<PlayerPossessedObject> changed)
    {
        Dictionary<int, PlayerController> players = FindObjectOfType<GameManager>().GetPlayers();
        if (changed.Behaviour.playerNum == 0)
        {
            changed.Behaviour.animator.SetBool("Nobody", true);
            changed.Behaviour.animator.SetBool("Mine", false);
            changed.Behaviour.animator.SetBool("Enemy", false);
        }
#pragma warning disable CS0618 // Type or member is obsolete
        else if (changed.Behaviour.Runner.LocalPlayer == players[changed.Behaviour.playerNum].GetPlayerRef())
#pragma warning restore CS0618 // Type or member is obsolete
        {
            changed.Behaviour.animator.SetBool("Nobody", false);
            changed.Behaviour.animator.SetBool("Mine", true);
            changed.Behaviour.animator.SetBool("Enemy", false);
        }
        else
        {
            changed.Behaviour.animator.SetBool("Nobody", false);
            changed.Behaviour.animator.SetBool("Mine", false);
            changed.Behaviour.animator.SetBool("Enemy", true);
        }
    }
}
