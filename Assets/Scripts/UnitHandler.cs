using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
using System.Collections;
using System.Collections.Generic;
using StageNine;
using UnityEngine.Events;
using System;

public abstract class UnitHandler : MonoBehaviour {

    private const float FULL_HEALTH_SCALE = 0.96f;
    private const float HEALTH_BAR_ANIMATION_TIME = 0.45f;
    private Queue<LinkedList<GameObject>> moveQueue;

    public enum PathingType
    {
        FOOT = 0,
        WHEEL = 1,
        FLY = 2,
        RANGED_DIRECT = 3,
        RANGED_INDIRECT = 4,
        MELEE = 5,
    }

    public enum MovementType
    {
        FOOT = PathingType.FOOT,
        WHEEL = PathingType.WHEEL,
        FLY = PathingType.FLY,
    }

    public enum AttackType
    {
        RANGED_DIRECT = PathingType.RANGED_DIRECT,
        RANGED_INDIRECT = PathingType.RANGED_INDIRECT,
        MELEE = PathingType.MELEE,
    }

    [HideInInspector] public CombatManager.Faction faction = CombatManager.Faction.NONE;

    public int movementRange;
    public MovementType movementType;
    public AttackType attackType;
    [HideInInspector] public int movementCur;
    public int attackRangeMin;
    public int attackRangeMax;
    public int attacksMax = 1;
    [HideInInspector] public int attacksCur;

    public float attackVal;
    public static GameObject HealthBarPrefab;
    public static GameObject StatusDisplayPrefab;
    public UnitSpecialAbility specialAbility = null;
    public AnimaticPopup.PlacementType placementType;

    protected IntVector2 _gridPos;
    protected GameObject _location;
    protected GameObject target; // used for attack/movement confirm step.
    [SerializeField]protected ParticleSystem explosionParticle;

    [SerializeField] protected int _maxHP, _curHP;

    protected float healthXRatio;
    protected float healthColorRatio; // 1.0f / _maxHP
    protected GameObject HealthBar;
    protected GameObject HealthBarBorder;
    protected GameObject StatusDisplay;
    protected IntVector2 undoPos = null;
    protected GameObject undoLocation = null;
	protected bool specialCannotUndo = false;
    protected List<StatusIconData> statusIcons;
    protected int _queuedDamage = 0;
    protected bool animatingMovement = false;
    private bool forceActed = false;
    [SerializeField] private float highlightBlendAmount = 0.5f, usedBlendAmount = 0.75f;
    [SerializeField] private Color highlightBaseColor = Color.red, usedBaseColor = Color.grey;
    private Color defaultColor, attackHighlightColor, usedColor;

    [SerializeField] private Sprite _animaticSprite;
    [SerializeField] private Sprite _weaponSprite;

    [SerializeField] protected SpriteRenderer overlaySprite;

    // public properties
    public virtual ArmyData.UnitType unitType
    {
        get { return ArmyData.UnitType.NONE; }
    }

    public virtual bool isDraggable
    {
        get 
        {
            if (CombatManager.singleton.GetGameState() == CombatManager.GameState.ARMY_PLACEMENT)
            {
                return CombatManager.singleton.currentPlayer == faction;
            }
            return false;
        }
    }

    public virtual bool disableAttackButton
    {
        get
        {
            return false;
        }
    }

    public bool hasQueuedDamage
    {
        get { return _queuedDamage > 0; }
    }

    public bool hasTarget
    {
        get { return target != null; }
    }

    public IntVector2 gridPos
    {
        get { return _gridPos; }
    }

    public virtual float defenseModifier // a multiplier that is applied to the attacker's attack value, lower is better
    {
        get
        {
            return ComputeTerrainDefensiveModifier();
        }
    }

    public virtual float attackModifier // a multiplier that is applied to our attack value, lower is worse
    {
        get
        {
            return ComputeTerrainAttackModifier();
        }
    }

    public int maxHP
    {
        get { return _maxHP; }
        set
        {
            _maxHP = value;
            healthXRatio = HealthBar.transform.localScale.x / _maxHP;
            healthColorRatio = 1.0f / _maxHP;
            SetHealthBar(_curHP);
        }
    }

    public int curHP
    {
        get { return _curHP; }
        set
        {
            int tempHP = _curHP;
            _curHP = value;

            if (tempHP > 0)
            {
                //TODO: if currently dead, reactivate
                if(_curHP <= 0 && CombatManager.singleton.UnitIsAlive(gameObject))
                {
                    DoDeath();
                }
                UpdateHealthBar(tempHP);
            }
        }
    }

    public GameObject getTarget()
    {
        return target;
    }

    public bool isAlive
    {
        get { return _curHP > 0; }
    }

    public bool willBeAlive
    {
        get { return _curHP > _queuedDamage; }
    }

    public bool hasActed
    {
        get
        {
            return HasMoved() || HasAttacked() || forceActed;
        }
        set
        {
            if (value)
            {
                forceActed = true;
            }
            else
            {
                forceActed = false;
                //TODO: make a method that restores move and attack to the unit.
            }
        }
    }

    public Sprite animaticSprite
    {
        get { return _animaticSprite; }
    }

    public Sprite weaponSprite
    {
        get { return _weaponSprite; }
    }

    public int queuedHP
    {
        get
        {
            return _curHP - _queuedDamage;
        }
    }

    public int queuedDamage
    {
        get
        {
            return _queuedDamage;
        }
    }

    public abstract ActorAnimation attackAnimation
    {
        get;
    }

    public virtual RandomSoundPackage animaticAttackStartSound
    {
        get
        {
            return null;
        }
    }
    public virtual RandomSoundPackage animaticAttackHitSound
    {
        get
        {
            return null;
        }
    }

    public virtual RandomSoundPackage animaticDieSound
    {
        get
        {
            return null;
        }
    }

    protected virtual float animaticSpritesPerUnit
    {
        get
        {
            return 5f;
        }
    }

    public GameObject location
    {
        get
        {
            return _location;
        }
    }

    #region public methods

    public static Color HealthBarColor(float portion)
    {
        return new Color(2.0f - portion * 2.0f, portion * 2.0f, 0f);
    }

    public virtual void OurInit()
    {
        _gridPos = null;
        moveQueue = new Queue<LinkedList<GameObject>>(); 
        HealthBarBorder = Instantiate(HealthBarPrefab) as GameObject;
        HealthBar = HealthBarBorder.transform.GetChild(0).gameObject;
        StatusDisplay = Instantiate(StatusDisplayPrefab) as GameObject;
        statusIcons = new List<StatusIconData>();
        Vector3 temp = HealthBarBorder.transform.position;
        HealthBarBorder.transform.SetParent(transform);
        HealthBarBorder.transform.localPosition = temp;
        temp = StatusDisplay.transform.position;
        StatusDisplay.transform.SetParent(transform);
        StatusDisplay.transform.localPosition = temp;
        healthXRatio = HealthBar.transform.localScale.x / _maxHP;
        healthColorRatio = 1.0f / _maxHP;
        NewRound();
    }

    public void CalculateHighlightColors()
    {
        defaultColor = GetComponent<SpriteRenderer>().color;
        attackHighlightColor = new Color(highlightBaseColor.r * highlightBlendAmount + defaultColor.r * (1 - highlightBlendAmount),
                                         highlightBaseColor.g * highlightBlendAmount + defaultColor.g * (1 - highlightBlendAmount),
                                         highlightBaseColor.b * highlightBlendAmount + defaultColor.b * (1 - highlightBlendAmount));
        usedColor = new Color(usedBaseColor.r * usedBlendAmount + defaultColor.r * (1 - usedBlendAmount),
                              usedBaseColor.g * usedBlendAmount + defaultColor.g * (1 - usedBlendAmount),
                              usedBaseColor.b * usedBlendAmount + defaultColor.b * (1 - usedBlendAmount));
    }

    public void RestoreDefaultColor()
    {
        SetCurrentColor(defaultColor);
    }
    public void SetUsedColor()
    {
        SetCurrentColor(usedColor);
    }
    public void HighlightAttack()
    {
        SetCurrentColor(attackHighlightColor);
    }

    public void SetGridPosition(IntVector2 newPos, bool animate)
    {
        _gridPos = new IntVector2(newPos);

        _location = MapManager.singleton.GetTileAt(newPos);
        //animatingMovement = false;
        if (!animate)
        {
            moveQueue.Clear();
            animatingMovement = false;
            transform.position = MapManager.singleton.GetPositionAt(newPos);
            Debug.Log("[UnitHandler:SetGridPosition] Not Animating");
        }
        else
        {
            //TODO: Movement animation coroutine 
            //Get path from combat manager
            LinkedList<GameObject> path = CombatManager.singleton.GetPathTo(newPos);
            //Start animation coroutine
            if(path == null)
            {
                Debug.LogError("[UnitHandler:SetGridPosition] This path is null");
            }
            else
            {
                moveQueue.Enqueue(path);
                if (!animatingMovement)
                {
                    StartNextMoveAnimation();
                }
                //StartCoroutine(AnimateMovement(path));
            }
        }
    }

    public void RespondToClick()
    {
        print("Unit at " + gridPos + " clicked");
        switch (CombatManager.singleton.GetGameState())
        {
            case CombatManager.GameState.ARMY_PLACEMENT:
                CombatManager.singleton.ReturnToBank(this);
                break;
            case CombatManager.GameState.ORDERING:
                break;
            case CombatManager.GameState.INSPECTING:
            case CombatManager.GameState.IDLE:
                //if (CombatManager.singleton.currentPlayer == faction && !hasActed)
                //    CombatManager.singleton.SelectGameState(CombatManager.GameState.ORDERING, gameObject);
                //else
                CombatManager.singleton.SelectGameState(CombatManager.GameState.INSPECTING, gameObject);
                break;
            case CombatManager.GameState.ATTACKING:
                CombatManager.singleton.selectedUnit.GetComponent<UnitHandler>().PrepAttack(gameObject);
                break;
            case CombatManager.GameState.SPECIAL:
            case CombatManager.GameState.CONFIRMING:
                CombatManager.singleton.curAbility.UnitClick(gameObject, CombatManager.singleton.GetGameState());
                break;
            case CombatManager.GameState.MOVING:
                //CombatManager.singleton.SelectGameState(CombatManager.GameState.ORDERING, CombatManager.singleton.selectedUnit);
                break;
        }
    }

    public void GetPathableTiles(out Dictionary<GameObject, MapManager.PathfindingNode> reachableTiles)
    {
        // Two lists of pairs of distances and tiles
        //  unsearchedTiles:    Tiles discovered by the algorithm, but not checked for neighbors
        LinkedList<MapManager.PathfindingNode> unsearchedTiles = new LinkedList<MapManager.PathfindingNode>();

        // Start by adding our current location to unsearchedTiles
        unsearchedTiles.AddFirst(new MapManager.PathfindingNode(0,_location));

        LinkedList<MapManager.PathfindingNode> doneTiles;
        MapManager.PathingInternal(out doneTiles, unsearchedTiles, movementCur, (PathingType)movementType, faction, gameObject);

        reachableTiles = new Dictionary<GameObject, MapManager.PathfindingNode>();
        foreach (var pathNode in doneTiles)
        {
            if (!pathNode.destinationTile.GetComponent<TileListener>().occupied)
                reachableTiles.Add(pathNode.destinationTile, pathNode);
        }
    }

    public void GetAttackableTilesAndUnits(out List<GameObject> tiles, out List<GameObject> units, bool friendlyFire = false)
    {
        GetAttackableTilesAndUnits(out tiles, out units, attackRangeMax, attackRangeMin, attackType, friendlyFire);
    }

    public void GetAttackableTilesAndUnits(out List<GameObject> tiles, out List<GameObject> units, int maxRange, int minRange, AttackType curAttackType, bool friendlyFire = false)
    {
        tiles = GetAttackableTiles(maxRange, minRange, curAttackType);
        units = new List<GameObject>();

        List<GameObject> enemyUnits = CombatManager.singleton.GetPlayerUnits(CombatManager.GetOpposingFaction(faction));

        foreach (GameObject enemy in enemyUnits)
        {
            var uH = enemy.GetComponent<UnitHandler>();
            if (tiles.Contains(uH._location))
            {
                units.Add(enemy);
            }
        }
        if(friendlyFire)
        {
            List<GameObject> allyUnits = CombatManager.singleton.GetPlayerUnits(faction);
            foreach (GameObject ally in allyUnits)
            {
                var uH = ally.GetComponent<UnitHandler>();
                if (tiles.Contains(uH._location))
                {
                    units.Add(ally);
                }
            }
        }
    }

    public List<GameObject> GetAttackableTiles(int maxRange, int minRange, AttackType curAttackType)
    {
        return GetAttackableTiles(maxRange, minRange, curAttackType, _location);
    }

    public List<GameObject> GetAttackableTiles(int maxRange, int minRange, AttackType curAttackType, GameObject startLocation)
    {
        // Two lists of pairs of distances and tiles
        //  unsearchedTiles:    Tiles discovered by the algorithm, but not checked for neighbors
        LinkedList<MapManager.PathfindingNode> unsearchedTiles = new LinkedList<MapManager.PathfindingNode>();

        // Start by adding our current location to unsearchedTiles
        unsearchedTiles.AddFirst(new MapManager.PathfindingNode(0, startLocation));

        LinkedList<MapManager.PathfindingNode> doneTiles;
        MapManager.PathingInternal(out doneTiles, unsearchedTiles, maxRange, (PathingType)curAttackType, CombatManager.Faction.NONE, gameObject);

        var attackableTiles = new List<GameObject>();
        foreach (var pathNode in doneTiles)
        {
            var tL = pathNode.destinationTile.GetComponent<TileListener>();
            var range = (tL.gridPos - gridPos).magnitude;
            if (range >= minRange)
            {
                attackableTiles.Add(pathNode.destinationTile);
            }
        }

        return attackableTiles;
    }

    public virtual List<GameObject> GetThreatTiles()
    {
        Dictionary<GameObject, MapManager.PathfindingNode> paths;
        GetPathableTiles(out paths);
        List<GameObject> threatTiles = GetAttackableTiles(attackRangeMax, attackRangeMin, attackType);

        foreach(var pair in paths)
        {
            if(pair.Key.GetComponent<TileListener>().GetPathingType(gameObject, false) == StageNine.TileModifier.PathingType.CLEAR)
            {
                List<GameObject> newThreatTiles = GetAttackableTiles(attackRangeMax, attackRangeMin, attackType, pair.Key);

                foreach(var tile in newThreatTiles)
                {
                    if(!threatTiles.Contains(tile))
                    {
                        threatTiles.Add(tile);
                    }
                }
            }
        }
        return threatTiles;
    }

    //@deprecated
    //public List<GameObject> GetAttackableUnits()
    //{
    //    List<GameObject> tiles, units;
    //    GetAttackableTilesAndUnits(out tiles, out units);
    //    return units;
    //}

    public virtual bool TryMoveTo(IntVector2 pos)
    {
        bool success = false;
        GameObject targetTile = MapManager.singleton.GetTileAt(pos);

        if (targetTile != null)
        {
            if (!targetTile.GetComponent<TileListener>().occupied)
            {
                //assert tile is open
                if (CombatManager.singleton.IsReachable(targetTile))
                {
                    //assert tile is within move radius
                    if(target != targetTile && targetTile.GetComponent<TileListener>().CheckForMods(gameObject, false))
                    {
                        //assert tile is "risky" and not already targeted
                        target = targetTile;
                        CombatManager.singleton.TargetReticleOn(target);
                        CombatManager.singleton.ShowInformationPopup("This move cannot be undone.\nConfirm?", new UnityAction(() =>
                        {
                            MoveTo(pos);
                        }));
                        return false;
                    }
                    //undoLocation only updates if it is empty
                    if (undoLocation == null)
                    {
                        undoPos = new IntVector2(gridPos);
                        undoLocation = _location;
                    }
                    _location.GetComponent<TileListener>().occupant = null;
                    SetGridPosition(pos, OptionsManager.singleton.movementAnimationsEnabled);
                    targetTile.GetComponent<TileListener>().occupant = gameObject;
                    movementCur = movementCur - CombatManager.singleton.GetPathLength(targetTile); // Bikers can move multiple times.
                    if (MapManager.singleton.GetTileAt(pos).GetComponent<TileListener>().TileEnter(gameObject))
                    {
                        specialCannotUndo = true;
                    }
                    success = true;
                }
                else
                {
                    //assert tile is outside of move radius
                    Debug.Log("Outside Movement Range");
                    //CombatManager.singleton.SelectGameState(CombatManager.GameState.IDLE, null);
                }
            }
            else
            {
                //assert tile is occupied; note
                Debug.LogError("target tile is occupied. NOTE: THIS SHOULD NEVER HAPPEN");
            }
        }
        else
        {
            //assert invalid tile coordinates
            Debug.LogError("no tile found");
        }
        return success;
    }

    public virtual void MoveTo(IntVector2 pos)
    {
        if(TryMoveTo(pos))
        {
            movementCur = 0;
            CombatManager.singleton.UpdateUI();
            CombatManager.singleton.SelectGameState(CombatManager.GameState.ORDERING, gameObject);
        }
    }

    public virtual void UndoMovement()
    {
        if(undoLocation == null)
        {
            // wig out?
            Debug.LogError("no undo location");
        }
        else
        {
            _location.GetComponent<TileListener>().occupant = null;
            SetGridPosition(undoPos, false);
            movementCur = movementRange; // If you undo movement, you should be back at max movement
            CombatManager.singleton.UpdateUI();
            undoLocation.GetComponent<TileListener>().occupant = gameObject;
            undoLocation = null;
            CombatManager.singleton.SelectGameState(CombatManager.GameState.INSPECTING, gameObject);
        }
    }

    public bool HasMoved()
    {
        return movementCur < movementRange;
    }

    public bool HasAttacked()
    {
        return attacksCur < attacksMax;
    }

    // STAT SETTERGETTERS
    public int GetMovementRange()
    {
        return movementRange;
    }
    public int GetMovementCur()
    {
        return movementCur;
    }
    public void SetMovement(int newMovement) 
    {
        movementCur = newMovement;
    }
    public int GetAttacksMax()
    {
        return attacksMax;
    }
    public int GetAttacksCur()
    {
        return attacksCur;
    }
    public void SetAttacks(int newAttacks)
    {
        attacksCur = newAttacks;
    }

    public void ShowHealthBar()
    {
        HealthBarBorder.SetActive(true);
    }

    public void HideHealthBar()
    {
        HealthBarBorder.SetActive(false);
    }

    public void UpdateHealthBar(int oldValue)
    {
        StartCoroutine(HealthBarAnimationCoroutine(HealthBar, healthColorRatio * oldValue, healthColorRatio * _curHP, HEALTH_BAR_ANIMATION_TIME));
    }

    public void UpdateHealthBarInstantly()
    {
        SetHealthBar(HealthBar, healthColorRatio * _curHP);
    }

    /// <summary>
    /// SetHealthBar appropriately scales and colors the health bar.
    /// </summary>
    /// <param name="bar">bar is a reference to the game object that needs to be scaled and colored</param>
    /// <param name="portion">portion needs to be a float between 0 and 1 inclusive, with 0 being dead and 1 being full health</param>
    public static void SetHealthBar(GameObject bar, float portion)
    {
        if (portion < 0.0f)
            portion = 0.0f;
        if (portion > 1.0f)
            portion = 1.0f;
        bar.transform.localScale = new Vector3(portion * FULL_HEALTH_SCALE, bar.transform.localScale.y, bar.transform.localScale.z);
        var image = bar.GetComponent<Image>();
        if (image != null)
        {
            image.color = UnitHandler.HealthBarColor(portion);
        }
        else
        {
            bar.GetComponent<SpriteRenderer>().color = UnitHandler.HealthBarColor(portion);
        }
    }

    public static IEnumerator HealthBarAnimationCoroutine(GameObject bar, float start, float end, float time, Text numericDisplay = null, int maxHP = 1)
    {
        float current = start;
        float curTime = 0f;
        while (curTime < time)
        {
            UnitHandler.SetHealthBar(bar, current);
            yield return null;
            curTime += Time.deltaTime;
            var amount = 1 - (curTime / time);
            amount = amount * amount;
            current = start * amount + end * (1 - amount);
            if(numericDisplay != null)
            {
                int curNumber = Mathf.CeilToInt(current * maxHP);
                if(curNumber < 0)
                {
                    curNumber = 0;
                }
                numericDisplay.text = curNumber.ToString();
            }
        }
        UnitHandler.SetHealthBar(bar, end);
        if (numericDisplay != null)
        {
            int curNumber = Mathf.FloorToInt(end * maxHP);
            if (curNumber < 0)
            {
                curNumber = 0;
            }
            numericDisplay.text = curNumber.ToString();
        }
    }

    public void ClearTarget()
    {
        target = null;
    }

    public void ConfirmAttack()
    {
        PrepAttack(target);
    }

    public void ConfirmMove()
    {
        MoveTo(target.GetComponent<TileListener>().gridPos);
    }

    public void PrepAttack(GameObject newTarget)
    {
        VoiceController.singleton.FireAllVoices();
        var targetUnit = newTarget.GetComponent<UnitHandler>();
        if (newTarget == target)
        {
            StartAttack(targetUnit);
            CombatManager.singleton.SelectGameState(CombatManager.GameState.ORDERING, CombatManager.singleton.selectedUnit);
            return;
        }

        int range = (gridPos - targetUnit.gridPos).magnitude;
        if (CombatManager.singleton.IsAttackable(newTarget))
        {
            //populate and display combat predictions and confirmation prompt.
            target = newTarget;
            var attackPrediction = PredictAttackerDamageOutput(target.GetComponent<UnitHandler>());
            var defendPrediction = PredictDefenderDamageOutput(target.GetComponent<UnitHandler>());
            VoiceController.singleton.RequestVoice(Camera.main.WorldToScreenPoint(target.transform.position), attackPrediction.ToString("0.00"));
            if(defendPrediction > float.Epsilon)
            {
                VoiceController.singleton.RequestVoice(Camera.main.WorldToScreenPoint(_location.transform.position), defendPrediction.ToString("0.00"));
            }
            CombatManager.singleton.TargetReticleOn(target);
            CombatManager.singleton.ShowInformationPopup("Deal " + attackPrediction + " of their " + target.GetComponent<UnitHandler>().curHP + " HP \n Take " + defendPrediction + " of our " + curHP + " HP", new UnityAction(() =>
            {
                StartAttack(targetUnit);
                CombatManager.singleton.SelectGameState(CombatManager.GameState.ORDERING, CombatManager.singleton.selectedUnit);
            }));

        }
        else
        {
            ClearTarget();
        }
    }

    public void AddStatus(StatusIconData newStatus)
    {
        statusIcons.Add(newStatus);
        if(statusIcons.Count == 1)
        {
            // we've just added our first status icon. show it
            StatusDisplay.GetComponent<SpriteRenderer>().sprite = statusIcons[0].sprite;
            StatusDisplay.GetComponent<SpriteRenderer>().color = statusIcons[0].color;
        }
        // what do we need to do otherwise?
    }

    public void RemoveStatus(StatusIconData oldStatus)
    {
        if(statusIcons.Contains(oldStatus))
        {
            statusIcons.Remove(oldStatus);
            if (statusIcons.Count == 0)
            {
                // we've just removed our last status icon. don't show anything
                StatusDisplay.GetComponent<SpriteRenderer>().sprite = null;
            }
            // do what we need to do for display?
        }
        else
        {
            // do we need an error here?
        }
    }

    public abstract void StartAttack(UnitHandler defender);
    public abstract float PredictAttackerDamageOutput(UnitHandler defender);
    public abstract float PredictDefenderDamageOutput(UnitHandler defender);
    public abstract float PredictDamageAfterInjury(UnitHandler defender, int damageTaken);
    public abstract void Retaliate(UnitHandler attacker);
    public abstract float GetAttackPower();

    //pre: curHP <= 0;
    public virtual void DoDeath()
    {
        //print("Oh, I am slain");
        CombatManager.singleton.KillUnit(gameObject);
        FileManager.singleton.AppendToLog("Unit killed: " + CombatManager.singleton.GetNameOfFaction(faction) + " " + ArmyData.GetNameOfType(unitType));
        CombatManager.singleton.LogArmyCost(faction);
        _location.GetComponent<TileListener>().occupant = null;
        // MAYBEDO: Maybe make this less unreasonable? For now, 0 these out so they can't act
        attacksCur = 0;
        movementCur = 0;

        CleanUpModifiers();

        StartCoroutine(DelayVisibleDeath());
    }

    public virtual void DoDeathQuietly()
    {
        CombatManager.singleton.KillUnit(gameObject);
        _location.GetComponent<TileListener>().occupant = null;

        CleanUpModifiers();
        gameObject.SetActive(false);
    }

    public void AnimateDeath()
    {
        Instantiate(explosionParticle, transform.position, Quaternion.identity);
        gameObject.SetActive(false);
    }

    public virtual void OnDelete()
    {
        CleanUpModifiers();
    }

    public virtual void CleanUpModifiers(){}

    public virtual void NewRound() 
    {
        movementCur = movementRange;
        attacksCur = attacksMax;
        forceActed = false;
        undoLocation = null;
		specialCannotUndo = false;
    }

    public virtual bool CanUndoMovement()
    {
        if(undoLocation != null &&
           attacksCur == attacksMax &&
		   !specialCannotUndo)
        {
            return true;
        }
        return false;
    }

    public static int RandomDamage(float tempAttack)
    {
        print("Temporary attack value: " + tempAttack);
        int curAttack;
        if (tempAttack < 1.0f)
        {
            curAttack = 1;
            print("Attack value < 1, doing 1 damage");
        }
        else
        {
            curAttack = (int)tempAttack;
            print("Integer attack value: " + curAttack);
            if (Random.value < tempAttack - curAttack)
            {
                curAttack++;
                print("Attack damage increased to " + curAttack);
            }
        }
        return curAttack;
    }

    /// <summary>
    /// returns the scalar value of all terrain modifiers affecting this unit's defense.
    /// </summary>
    /// <returns></returns>
    public float ComputeTerrainDefensiveModifier()
    {
        if(movementType == MovementType.FLY)
            return 1f;
        if(_location != null)
        {
            var tile = _location.GetComponent<TileListener>();
            if(tile != null)
            {
                if(tile.HasTerrainProperty(TerrainDefinition.TerrainProperty.DEFENSE_BONUS))
                    return 0.9f; // TODO: Replace magic numbers
                if (tile.HasTerrainProperty(TerrainDefinition.TerrainProperty.DEFENSE_PENALTY))
                    return 1.1f; // TODO: Replace magic numbers
                return 1f;
            }
            Debug.LogError("[UnitHandler:ComputeTerrainDefensiveModifier] Unit's current location is missing a TileListener!?");
            return 1f;
        }
        Debug.LogError("[UnitHandler:ComputeTerrainDefensiveModifier] Unit is missing a current location reference!");
        return 1f;
    }

    public float ComputeTerrainAttackModifier()
    {
        if (movementType == MovementType.FLY)
            return 1f;
        if (_location != null)
        {
            var tile = _location.GetComponent<TileListener>();
            if (tile != null)
            {
                if (tile.HasTerrainProperty(TerrainDefinition.TerrainProperty.ATTACK_PENALTY))
                    return 0.9f; // TODO: Replace magic numbers
                return 1f;
            }
            Debug.LogError("[UnitHandler:ComputeTerrainAttackModifier] Unit's current location is missing a TileListener!?");
            return 1f;
        }
        Debug.LogError("[UnitHandler:ComputeTerrainAttackModifier] Unit is missing a current location reference!");
        return 1f;
    }
    public void QueueDamage(int damage)
    {
        _queuedDamage += damage;  
    }
    public void ResolveDamage()
    {
        curHP -= _queuedDamage;
        _queuedDamage = 0;
    }
    public virtual int DetermineActorCount(float unitHpPortion)
    {
        return Mathf.Max(Mathf.CeilToInt(unitHpPortion * animaticSpritesPerUnit), 0);
    }

    public void DisplayText(string textToDisplay)
    {

    }

    public virtual SerializableUnit Serialize()
    {
        SerializableUnit output = new SerializableUnit();

        output.unitType = unitType;
        output.curHp = queuedHP;
        output.movementCur = movementCur;
        output.attacksCur = attacksCur;
        output.gridPos = gridPos;
        output.properties = null;

        return output;
    }

    public void SetHpOnLoad(int newHP)
    {
        int tempHP = _curHP;
        _curHP = newHP;

        if (tempHP > 0)
        {
            //TODO: if currently dead, reactivate
            if (_curHP <= 0 && CombatManager.singleton.UnitIsAlive(gameObject))
            {
                DoDeathQuietly();
            }
            UpdateHealthBarInstantly();
        }
    }

    public virtual void LoadDeserializedData(SerializableUnit data)
    {
        SetHpOnLoad(data.curHp);
        attacksCur = data.attacksCur;
        movementCur = data.movementCur;
    }
    #endregion

    #region private methods
    private void SetCurrentColor(Color newColor)
    {
        GetComponent<SpriteRenderer>().color = newColor;
    }

    private void StartNextMoveAnimation()
    {
        if(moveQueue.Count > 0)
        {
            StartCoroutine(AnimateMovement(moveQueue.Dequeue()));
        }
        else
        {
            animatingMovement = false;
        }
    }

    protected void SetHealthBar(int newHP)
    {
        SetHealthBar(HealthBar, newHP * healthColorRatio);
    }

    protected IEnumerator DelayVisibleDeath()
    {
        yield return new WaitForSeconds(HEALTH_BAR_ANIMATION_TIME);
        AnimateDeath();
    }

    //protected virtual IEnumerator AnimateMovement(LinkedList<GameObject> path)
    //{
    //    animatingMovement = true;
    //    float elapsedTime = 0f;
    //    for (LinkedListNode<GameObject> node = path.First; node.Next != null && animatingMovement; node = node.Next)
    //    {
    //        Vector3 startPos = node.Value.transform.position;
    //        Vector3 moveVector = node.Next.Value.transform.position - startPos;
    //        while (elapsedTime < 1f && animatingMovement)
    //        {
    //            transform.position = startPos + moveVector * elapsedTime;
    //            elapsedTime += Time.deltaTime * movementRange;
    //            yield return null;
    //        }
    //        elapsedTime -= 1f;
    //    }
    //    transform.position = location.transform.position;
    //}

    protected virtual IEnumerator AnimateMovement(LinkedList<GameObject> path)
    {
        animatingMovement = true;
        float elapsedTime = .5f;
        Vector3 ghostPosition = Vector3.zero;
        Vector3 moveVector; 
        for (LinkedListNode<GameObject> node = path.First; node.Next != null && animatingMovement; node = node.Next)
        {
            Vector3 ghostStartPos = node.Value.transform.position;
            Vector3 ghostMoveVector = node.Next.Value.transform.position - ghostStartPos;
            while (elapsedTime < 1f && animatingMovement)
            {
                ghostPosition = ghostStartPos + ghostMoveVector * elapsedTime;
                moveVector = ghostPosition - transform.position;
                transform.position += moveVector * Time.deltaTime * movementRange * 2f;
                elapsedTime += Time.deltaTime * movementRange;
                yield return null;
            }
            elapsedTime -= 1f;
        }
        ghostPosition = path.Last.Value.transform.position;

        moveVector = ghostPosition - transform.position;
        Vector3 startPosition = transform.position;
        while (animatingMovement && elapsedTime < 0.5f)
        {
            transform.position = startPosition + moveVector * elapsedTime * 2f;
            elapsedTime += Time.deltaTime * movementRange;
            yield return null;
        }
        
        if (moveQueue.Count == 0)
        {
            animatingMovement = false;
            transform.position = _location.transform.position;
        }
        else
        {
            transform.position = ghostPosition;
            StartNextMoveAnimation();
        }
    }

    protected virtual float TargetDefense(UnitHandler target)
    {
        return target.defenseModifier;
    }

    #endregion

    #region monobehaviors
    #endregion


}
[Serializable]
public class SerializableUnit
{
    [Serializable]
    public class AdditionalUnitProperties{}

    public ArmyData.UnitType unitType;
    public int curHp;
    public int movementCur;
    public int attacksCur;
    public IntVector2 gridPos;
    public AdditionalUnitProperties properties;
}