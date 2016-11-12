using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using StageNine;
using StageNine.Events;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class CombatManager : MonoBehaviour, IEventListener, IModalFocusHolder
{
    public enum GameState
    {
		NONE,
        ARMY_PLACEMENT,
        IDLE,
        ORDERING,
        MOVING,
        ATTACKING,
        CONFIRMING,
		SPECIAL,
        INSPECTING
    }

    public enum Faction
    {
        NONE,
        RED,
        BLUE
    }

    public enum WinCondition
    {
        NONE,
        KILL_BATTLEMAGE,
        KILL_ALL
    }

    public const string SAVE_FILE_EXTENSION = ".fyt";
    public const string SAVE_DIRECTORY = "/saves/";
    const string DEFAULT_SAVE_FILENAME = "test.fyt";
    private const string AUTOSAVE_FILENAME = "autosave.fyt";
    private const string AUTOSAVE_TEMP_FILENAME = "autosave.tmp";

    const string UNAVAILABLE_ACTION_LABEL = "- - - - -";
    const int NUM_MAGIC_BUTTONS = 5;
    const Faction DEFAULT_FIRST_PLAYER_FACTION = Faction.RED;

    public static CombatManager singleton;


    public GameObject unitPawnPrefab;
    public GameObject unitArcherPrefab;
    public GameObject unitArtilleryPrefab;
    public GameObject unitBikerPrefab;
	public GameObject unitKnightPrefab;
    public GameObject unitBattlemageLumpPrefab;
    public GameObject HealthBarPrefab;
    public GameObject StatusDisplayPrefab;

    public Faction currentPlayer = Faction.RED;
    public Faction lastActivePlayer = Faction.RED;
    public int numPlayers = 2;
    public int roundCount = 1;
    public int blueTurnCounter = 0;
    public int redTurnCounter = 0;
    public int blueSkippedTurnsCounter = 0;
    public int redSkippedTurnsCounter = 0;

    public bool cycleThroughEnemies = true;
    public bool cycleThroughBothArmies = false;
    public bool displayRoundAfterAnimation = false;

    public string saveFileName = null;
    
    [HideInInspector] public GameObject selectedUnit;

    private WinCondition winConditionRed = WinCondition.KILL_BATTLEMAGE;
    private WinCondition winConditionBlue = WinCondition.KILL_BATTLEMAGE;
    private int numPlayersAlive = 2;
    private int numTurnSkips = 0;
    private GameState gameState = GameState.IDLE;
    private UnitSpecialAbility _curAbility;

	private Text hpLabel, dmgLabel, moveLabel, unitLabel, turnLabel, moveButtonLabel, attackButtonLabel, specialButtonLabel, undoButtonLabel, cancelButtonLabel, endTurnButtonLabel, battleMagicMenuButtonLabel;
    private Button moveButton, attackButton, specialButton, undoButton, cancelButton, endTurnButton, battleMagicMenuButton;
    private Text[] battleMagicButtonLabels;
    private Button[] battleMagicButtons;

    private GameObject endTurnPanel;
    private GameObject unitInfoPanel;
    private GameObject selectionPanel;
    private GameObject battleMagicPanel;
    private float unitInfoPanelFullHeight, unitInfoPanelReducedHeight;
    private bool turnSkipConfirmation = false;

    private float gameStartTimer;
    private float redElapsedTime;
    private float blueElapsedTime;
    private float turnElapsedTime;
    private float otherElapsedTime;

    [SerializeField] private GameObject selectReticle;
    [SerializeField] private GameObject targetReticle;
    [SerializeField] private InformationPopup _informationPopup;

    [SerializeField] private GameObject pauseMenu;

    [SerializeField] private Text victoryText;
    [SerializeField] private AnimaticPopup animaticPopup;
    [SerializeField] private AnimatedBannerPanel animatedBanner;
    

    //prefabs
    private List<GameObject> blueUnits = new List<GameObject>();
    private List<GameObject> blueDeadUnits = new List<GameObject>();
    private List<GameObject> redUnits = new List<GameObject>();
    private List<GameObject> redDeadUnits = new List<GameObject>();
    private Dictionary<GameObject, MapManager.PathfindingNode> reachableTiles = new Dictionary<GameObject, MapManager.PathfindingNode>();
    private List<GameObject> attackableTiles = new List<GameObject>();
    private List<GameObject> attackableUnits = new List<GameObject>();

    private Transform unitHolder;

    [SerializeField] private GameObject arrowPrefab;
    private Stack<GameObject> unemployedArrows;
    private Stack<GameObject> pathArrows;

    //@HAX move attack button when special button replaces it
    float attackButtonY, specialButtonY;

    #region public properties

    public UnitSpecialAbility curAbility
    {
        get { return _curAbility; }
    }

    #endregion

    #region public methods

    public Color GetColorOfFaction(Faction faction)
    {
        switch (faction)
        {
            case Faction.BLUE:
                return Color.blue;
            case Faction.RED:
                return Color.red;
            default:
                Debug.LogError("[CombatManager:GetColorOfFaction] army faction not recognized");
                return Color.cyan;
        }
    }

    public string GetNameOfFaction(Faction faction)
    {
        switch (faction)
        {
            case CombatManager.Faction.RED:
                return "Red";
            case CombatManager.Faction.BLUE:
                return "Blue";
            default:
                Debug.LogError("[CombatManager:GetNameOfFaction] Invalid faction");
                return "INVALID FACTION NAME";
        }
    }

    public static Faction GetOpposingFaction(Faction faction)
    {
        switch (faction)
        {
            case Faction.BLUE:
                return Faction.RED;
            case Faction.RED:
                return Faction.BLUE;
            default:
                Debug.LogError("[CombatManager:GetOpposingFaction] army faction not recognized");
                return Faction.NONE;
        }
    }

    public bool Connect()
    {
        if (EventManager.singleton != null)
        {
            EventManager.singleton.ConnectListener(this);
            return true;
        }
        return false;
    }

    public bool Disconnect()
    {
        if (EventManager.singleton != null)
        {
            EventManager.singleton.DisconnectListener(this);
            return true;
        }
        return false;
    }

    public void HandleEvent(StageNine.Events.EventType eType)
    {
        if(eType == StageNine.Events.EventType.RESIZE_SCREEN)
        {
            Debug.Log("[CombatManager:HndleEvent] Resize event recieved.");
            //note: if the screen resize step is significant, due to maximize or restore, then the camera can end up far out of bounds. 
            MapManager.singleton.LimitCameraScrolling();
        }
    }

    public GameState GetGameState()
    {
        return gameState;
    }

    public void SelectGameState(GameState newState, GameObject newUnit)
    {
        SelectGameState(newState, newUnit, null);
    }

    public void SelectGameState(GameState newState, GameObject newUnit, UnitSpecialAbility ability)
    {
        selectedUnit = newUnit;
        ChangeGameState(newState, ability);
    }

    public void HighlightAttackableTilesAndUnits()
    {
        foreach (var tile in attackableTiles)
        {
            tile.GetComponent<TileListener>().HighlightAttack();
        }
        foreach (var unit in attackableUnits)
        {
            unit.GetComponent<UnitHandler>().HighlightAttack();
        }
    }

    public void HighlightThreatTiles()
    {
        if(selectedUnit == null)
        {
            Debug.LogError("[CombatManager:HighlightThreatTiles] No Selected Unit to get threat");
        }
        else
        {
            var threat = selectedUnit.GetComponent<UnitHandler>().GetThreatTiles();
            foreach(var tile in threat)
            {
                tile.GetComponent<TileListener>().HighlightAttack();
            }
        }
    }

    public void HighlightSplash(GameObject centerTile,int radius)
    {
        //Currently using attackType MELEE because melee doesn't care about terrain
        var splashTiles = centerTile.GetComponent<TileListener>().GetTargetableTilesFromTile(radius, UnitHandler.AttackType.MELEE);
        foreach (var tile in splashTiles)
        {
            tile.GetComponent<TileListener>().HighlightAttack();
            var unit = tile.GetComponent<TileListener>().occupant;
            if(unit != null)
            {
                unit.GetComponent<UnitHandler>().HighlightAttack();
            }
        }
    }

    public void EnterSpecialAttackState(List<GameObject> inAttackableTiles, List<GameObject> inAttackableUnits)
    {
        attackableTiles = inAttackableTiles;
        attackableUnits = inAttackableUnits;

        HighlightAttackableTilesAndUnits();

        MapManager.singleton.MoveCameraFocus(attackableTiles);

        SetUpActionButtons();
        SetUpCancelButton();
    }

    public void PartialHighlightAttackableTiles(float highlightAmount)
    {
        foreach (var tile in attackableTiles)
        {
            tile.GetComponent<TileListener>().HighlightPartialAttack(highlightAmount);
        }
    }

    // Is one of these going to turn into the undo button?
    public void ClickedEndTurnButton()
    {
        HideBattleMagicPanel();
        if (gameState == GameState.IDLE || gameState == GameState.INSPECTING)
        {
            if (turnSkipConfirmation)
            {
                ChangeTurn(true);
            }
            else
            {
                ShowInformationPopup("Really skip turn?", new UnityAction(() =>
                {
                    ChangeTurn(true);
                }));
                turnSkipConfirmation = true;
            }
        }
        else if (gameState == GameState.ORDERING)
        {
            ChangeTurn(false);
        }
        else
        {
            if (selectedUnit.GetComponent<UnitHandler>().hasActed)
            {
                ChangeGameState(GameState.ORDERING);
                ShowInformationPopup("Cancel current action and end turn?", new UnityAction(() =>
                {
                    ChangeTurn(false);
                }));
            }
            else
            {
                if(cancelButton.interactable)
                {
                    ClickedCancelButton();
                }
                ShowInformationPopup("Really skip turn?", new UnityAction(() =>
                {
                    ChangeTurn(true);
                }));
                turnSkipConfirmation = true;
            }
        }
    }

    private void HideBattleMagicPanel()
    {
        if(battleMagicPanel.activeSelf)
        {
            ClickedBattleMagicMenuCloseButton();
        }
    }

    public void ClickedMoveButton()
    {
        if (gameState == GameState.MOVING && selectedUnit.GetComponent<UnitHandler>().hasTarget)
            selectedUnit.GetComponent<UnitHandler>().ConfirmMove();
        else if(gameState == GameState.IDLE)
            Debug.Log("How did you even click this?");
        else if (selectedUnit.GetComponent<UnitHandler>().GetMovementCur() > 0)
            ChangeGameState(GameState.MOVING);
    }

    public void ClickedAttackButton()
    {
        if (gameState == GameState.ATTACKING && selectedUnit.GetComponent<UnitHandler>().hasTarget)
            selectedUnit.GetComponent<UnitHandler>().ConfirmAttack();
        else if (gameState == GameState.IDLE)
            Debug.Log("How did you even click this?");
        else
            ChangeGameState(GameState.ATTACKING);
    }

	public void ClickedSpecialButton()
	{
        if (gameState == GameState.IDLE)
            Debug.LogError("How did you even click this?");
        else
        {
            if(gameState == GameState.CONFIRMING && _curAbility == selectedUnit.GetComponent<UnitHandler>().specialAbility)
            {
                _curAbility.ExecuteAbility();
            }
            else
            {
                // TODO: This all will get messy when things have multiple special abilities
                ChangeGameState(GameState.SPECIAL, selectedUnit.GetComponent<UnitHandler>().specialAbility);
            }
        }
	}

    public void ClickedBattleMagicMenuOpenButton()
    {
        if(cancelButton.interactable)
        {
            ClickedCancelButton();
        }
        HideInformationPopup();
        //show the BM menu
        battleMagicPanel.SetActive(true);
        //set the buttons labels
        //change hotkey context
        //check and set interactability of battle magic buttons
        SetUpBattleMagicButtons();
            //make covered up menu buttons non-interactable
        GreyOutActionButtons();

        //TODO: juice! animations~! wow!
    }

    public void ClickedBattleMagicMenuCloseButton()
    {
        //hide the BM menu
        battleMagicPanel.SetActive(false);
        //change hotkey
            //check and set interactability of unit menu buttons
        SetUpActionButtons();
            //make battle magic buttons non-interactable
        GreyOutBattleMagicButtons();

        //TODO: juice! animations~! wow!
    }

    public void ClickedBattleMagicAbilityButton(int spellID)
    {
        SelectGameState(GameState.SPECIAL, selectedUnit, selectedUnit.GetComponent<UHBattlemage>().battleMagic[spellID]);
    }

    public void ClickedUndoButton()
    {
        // suuh = SelectedUnitUnitHandler
        UnitHandler suuh = selectedUnit.GetComponent<UnitHandler>();
        if(suuh.CanUndoMovement())
        {
            suuh.UndoMovement();
        }
    }

    public void ClickedCancelButton()
    {
        if (gameState == GameState.MOVING || gameState == GameState.ATTACKING ||
            gameState == GameState.CONFIRMING || gameState == GameState.SPECIAL)
        {
            if(selectedUnit.GetComponent<UnitHandler>().hasActed)
            {
                ChangeGameState(GameState.ORDERING);
            } else
            {
                ChangeGameState(GameState.INSPECTING);
            }
        } else
        {
            Debug.Log("How did you even click this?");
        }
    }

    public void UpdateUI()
    {
        if (selectedUnit != null && gameState != GameState.IDLE)
        {
            hpLabel.text = "HP: " + selectedUnit.GetComponent<UnitHandler>().curHP + "/" + selectedUnit.GetComponent<UnitHandler>().maxHP;
            dmgLabel.text = "Dmg: " + selectedUnit.GetComponent<UnitHandler>().GetAttackPower();
            moveLabel.text = "Move: " + selectedUnit.GetComponent<UnitHandler>().movementCur + "/" + selectedUnit.GetComponent<UnitHandler>().movementRange;
            unitLabel.text = "Unit: " + selectedUnit.GetComponent<UnitHandler>().name;
        }
        else
        {
            hpLabel.text = "HP:";
            dmgLabel.text = "Dmg:";
            moveLabel.text = "Move:";
            unitLabel.text = "Unit:";
        }
    }

    public void GreyOutUnavailableButtons()
    {
        if (selectedUnit != null && gameState != GameState.IDLE) 
        {
            if (selectedUnit.GetComponent<UnitHandler>().GetMovementCur() <= 0)
            {
                moveButtonLabel.color = Color.grey;
                moveButton.interactable = false;
            }

            if (selectedUnit.GetComponent<UnitHandler>().GetAttacksCur() <= 0) 
            {
                attackButtonLabel.color = Color.grey;
                attackButton.interactable = false;
            }

			if(selectedUnit != null && selectedUnit.GetComponent<UnitHandler>().specialAbility != null && !selectedUnit.GetComponent<UnitHandler>().specialAbility.buttonInteractable)
			{
				specialButtonLabel.color = Color.grey;
				specialButton.interactable = false;
			}
            
			if (!selectedUnit.GetComponent<UnitHandler>().CanUndoMovement()) 
            {
                undoButtonLabel.color = Color.grey;
                undoButton.interactable = false;
            }

            if (selectedUnit != null && selectedUnit.GetComponent<UnitHandler>().disableAttackButton)
            {
                //@HAX move attack button when special button replaces it
                SwapAttackButtons(true);
            }
            else
            {
                SwapAttackButtons(false);
            }
        }
    }

    //@HAX move attack button when special button replaces it
    public void SwapAttackButtons(bool attackDisabled)
    {
        if(attackDisabled)
        {
            Vector3 pos = attackButton.transform.localPosition;
            attackButton.transform.localPosition = new Vector3(pos.x, specialButtonY, pos.z);
            pos = specialButton.transform.localPosition;
            specialButton.transform.localPosition = new Vector3(pos.x, attackButtonY, pos.z);
            attackButtonLabel.color = Color.grey;
            attackButtonLabel.text = UNAVAILABLE_ACTION_LABEL;
            attackButton.interactable = false;
        }
        else
        {
            Vector3 pos = attackButton.transform.localPosition;
            attackButton.transform.localPosition = new Vector3(pos.x, attackButtonY, pos.z);
            pos = specialButton.transform.localPosition;
            specialButton.transform.localPosition = new Vector3(pos.x, specialButtonY, pos.z);
            attackButtonLabel.text = "[A]TTACK";
        }
    }

    public void AssignFaction(GameObject unit, Faction newFaction)
    {
        Faction currentFaction = unit.GetComponent<UnitHandler>().faction;
        if (currentFaction != newFaction)
        {
            if (currentFaction != Faction.NONE)
            {
                RemoveUnitFromFaction(unit, currentFaction);
            }
            unit.GetComponent<UnitHandler>().faction = newFaction;
            switch (newFaction)
            {
                case Faction.RED:
                    unit.GetComponent<SpriteRenderer>().color = Color.red;
                    if (unit.GetComponent<UnitHandler>().isAlive)
                    {
                        redUnits.Add(unit);
                    }
                    else
                    {
                        redDeadUnits.Add(unit);
                    }
                    break;
                case Faction.BLUE:
                    unit.GetComponent<SpriteRenderer>().color = Color.blue;
                    if (unit.GetComponent<UnitHandler>().isAlive)
                    {
                        blueUnits.Add(unit);
                    }
                    else
                    {
                        blueDeadUnits.Add(unit);
                    }
                    break;
                default:
                    Debug.LogError("[CombatManager:AssignFaction] Attempting to assign unit to bad faction");
                    break;
            }
            unit.GetComponent<UnitHandler>().CalculateHighlightColors();
        }
    }

    public List<GameObject> GetPlayerUnits(Faction faction)
    {
        switch (faction)
        {
            case Faction.BLUE:
                return blueUnits;
            case Faction.RED:
                return redUnits;
            default:
                Debug.Log("assert(true);");
                return null;
        }
    }

    public void RemoveUnitFromFaction(GameObject unit, Faction currentFaction)
    {
        if (currentFaction == Faction.RED)
        {
            redUnits.Remove(unit);
            redDeadUnits.Remove(unit);
        }
        else
        {
            blueUnits.Remove(unit);
            blueDeadUnits.Remove(unit);
        }
    }

    public void ChangeTurn(bool skipTurn)
    {
        Faction originalPlayer = currentPlayer;
        if (skipTurn)
        {
            FileManager.singleton.AppendToLog(GetNameOfFaction(currentPlayer) + " is skipping their turn.");
            numTurnSkips++;
            IncrementTurnSkips(currentPlayer);
        }
        else
            numTurnSkips = 0;

        if (currentPlayer == Faction.NONE)
        {
            currentPlayer = Faction.BLUE;
        }

        CheckNewRound();
        SelectGameState(GameState.IDLE, null);
        RestoreUnitHighlights();

        switch (currentPlayer)
        {
            case Faction.RED:
                currentPlayer = Faction.BLUE;
                UpdateTurnLabel(currentPlayer);

                if (HaveAllUnitsActed(blueUnits))
                {
                    //If all blue units have acted, they automatically pass their turn for round control purposes
                    ChangeTurn(true);
                }
                else
                {
                    lastActivePlayer = Faction.BLUE;
                }
                break;
            case Faction.BLUE:
                currentPlayer = Faction.RED;
                UpdateTurnLabel(currentPlayer);

                if (HaveAllUnitsActed(redUnits))
                {
                    //If all red units have acted, they automatically pass their turn for round control purposes
                    ChangeTurn(true);
                }
                else
                {
                    lastActivePlayer = Faction.RED;
                }
                break;
            case Faction.NONE:
            default:
                Debug.LogError("[CombatManager:ChangeTurn] Current player faction invalid.");
                break;
        }
        LogChangeTurn(originalPlayer);
        AutoSave();
    }

    private void UpdateTurnLabel(Faction faction)
    {
        turnLabel.text = GetNameOfFaction(faction).ToUpper() + " TURN";
        turnLabel.color = GetColorOfFaction(faction);
    }

    private void IncrementTurnSkips(Faction currentPlayer)
    {
        switch (currentPlayer)
        {
            case Faction.RED:
                redSkippedTurnsCounter++;
                break;
            case Faction.BLUE:
                blueSkippedTurnsCounter++;
                break;
        }
    }

    private void LogChangeTurn(Faction originalPlayer)
    {
        if (originalPlayer == currentPlayer)
        {
            FileManager.singleton.AppendToLog("Player did not change");
        }

        bool firstTime = (int)(Time.time - gameStartTimer) == 0;

        //        int redArmyCost = 0;


        if (!firstTime)
        {
            FileManager.singleton.AppendToLog("Turn took this many seconds: " + turnElapsedTime);
            FileManager.singleton.AppendToLog("Game has lasted this many seconds: " + ( Time.time - gameStartTimer));
        }

        switch (originalPlayer)
        {
            case Faction.RED:
                redElapsedTime += turnElapsedTime;
                if (!firstTime)
                    FileManager.singleton.AppendToLog("Red player elapsed time: " + redElapsedTime);
                break;
            case Faction.BLUE:
                blueElapsedTime += turnElapsedTime;
                if (!firstTime)
                    FileManager.singleton.AppendToLog("Blue player elapsed time: " + blueElapsedTime);
                break;
        }

        turnElapsedTime = 0f;

        FileManager.singleton.AppendToLog("");
        switch (currentPlayer)
        {
            case Faction.RED:
                {
                    redTurnCounter++;
                    string turnDisplay = redTurnCounter.ToString();
                    if (redSkippedTurnsCounter != 0)
                        turnDisplay += " (" + redSkippedTurnsCounter + " skipped)";
                    FileManager.singleton.AppendToLog("Red turn number: " + turnDisplay, true);
                    break;
                }
            case Faction.BLUE:
                {
                    blueTurnCounter++;
                    string turnDisplay = blueTurnCounter.ToString();
                    if (blueSkippedTurnsCounter != 0)
                        turnDisplay += " (" + blueSkippedTurnsCounter + " skipped)";
                    FileManager.singleton.AppendToLog("Blue turn number: " + turnDisplay, true);
                    break;
                }
        }
    }

    public void LogArmyDetails(Faction faction)
    {
        string colorName = GetNameOfFaction(faction);
        FileManager.singleton.AppendToLog(colorName + " army name: " + ArmyManager.singleton.GetArmyName(faction));
        LogArmyCost(faction);
        ArmyManager.singleton.LogCurrentArmyContents(GetUnitsOfFaction(faction), faction);
    }

    public void LogArmyCost(Faction faction)
    {
        List<GameObject> army = GetUnitsOfFaction(faction);
        string colorName = GetNameOfFaction(faction).ToLower();

        int armyValue = 0;
        foreach (var unit in army)
        {
            armyValue += ArmyData.PointCost(unit.GetComponent<UnitHandler>().unitType);
        }
        FileManager.singleton.AppendToLog("Number of " + colorName + " units: " + army.Count);
        FileManager.singleton.AppendToLog("The " + colorName + " army is worth " + armyValue + " points.");
    }

    public void LogEndOfPause(string pauseTypeName)
    {
        FileManager.singleton.AppendToLog(pauseTypeName + " for " + otherElapsedTime + " seconds");
        otherElapsedTime = 0f;
    }

    private bool HaveAllUnitsActed(List<GameObject> units)
    {
        foreach (var unit in units)
        {
            if (!unit.GetComponent<UnitHandler>().hasActed)
            {
                return false;
            }
        }
        return true;
    }

    public void CheckVictory()
    {
        /*
        bool blueDefeat = true;
        foreach(var unit in blueUnits)
        {
            if (unit.GetComponent<UnitHandler>().isAlive)
            {
                blueDefeat = false;
                break; //assert this team has not lost;
            }
        }
        bool redDefeat = true;
        foreach(var unit in redUnits)
        {
            if (unit.GetComponent<UnitHandler>().isAlive)
            {
                redDefeat = false;
                break; //assert this team has not lost;
            }
        }*/
        bool redWins = CheckForWinCondition(winConditionRed, blueUnits);
        bool blueWins = CheckForWinCondition(winConditionBlue, redUnits);
        string victoryLog = Environment.NewLine;

        if (redWins && blueWins)
        {
            //blue and red lose;
            victoryLog += "DRAW";
            Debug.Log("You Both Lose");
            victoryText.text = "You Both Lose";
            victoryText.color = Color.black;
        }
        else if (redWins)
        {
            //red wins
            victoryLog += "RED WINS";
            Debug.Log("Red Wins");
            victoryText.text = "Red Wins";
            victoryText.color = Color.red;
        }
        else if (blueWins)
        {
            //blue wins
            victoryLog += "BLUE WINS";
            Debug.Log("Blue Wins");
            victoryText.text = "Blue Wins";
            victoryText.color = Color.blue;
        }

        if (redWins || blueWins)
        {
            FileManager.singleton.AppendToLog(victoryLog);
            LogGameSummary();

            victoryText.gameObject.SetActive(true);
            EventManager.singleton.ShowDynamicPopup("Play again?",
                "No", new UnityAction(() =>
                {
                    SceneManager.LoadScene(MainMenu.MAIN_MENU_SCENE);
                }), KeyCode.N,
                "Hell No!", new UnityAction(() =>
                {
                    SceneManager.LoadScene(MainMenu.MAIN_MENU_SCENE);
                }), KeyCode.H, 2, 2);
        }
    }

    public void CheckNewRound()
    {
        if (numTurnSkips != numPlayersAlive)
        {
            if (!HaveAllUnitsActed(redUnits) || !HaveAllUnitsActed(blueUnits))
                return;
        }
        foreach (var unit in blueUnits)
        {
            unit.GetComponent<UnitHandler>().NewRound();
        }
        foreach (var unit in redUnits)
        {
            unit.GetComponent<UnitHandler>().NewRound();
        }
        roundCount++;
        numTurnSkips = 0;
        DisplayRoundNumber();
        currentPlayer = lastActivePlayer;
    }

    public void DisplayRoundNumber()
    {
        if (!AnimaticPopup.animationRunning)
        {
            string sRound = ("ROUND " + roundCount);
            animatedBanner.StartAnimation(2f, sRound);

            FileManager.singleton.AppendToLog(Environment.NewLine + sRound);
        }
        else
        {
            displayRoundAfterAnimation = true;
        }
        
    }

    public void KillUnit(GameObject unit)
    {
        if(unit.GetComponent<UnitHandler>().faction == Faction.BLUE)
        {
            if(blueUnits.Contains(unit))
            {
                blueDeadUnits.Add(unit);
                blueUnits.Remove(unit);
            }
        }
        else
        {
            if (redUnits.Contains(unit))
            {
                redDeadUnits.Add(unit);
                redUnits.Remove(unit);
            }
        }
    }

    public bool IsReachable(GameObject target) 
    {
        if (target != null)
            return reachableTiles.ContainsKey(target);
        else
            return false;
    }

    public bool IsAttackable(GameObject target)
    {
        if(target.GetComponent<UnitHandler>() != null)
        {
            return attackableUnits.Contains(target);
        }
        return attackableTiles.Contains(target);
    }

    public int GetPathLength(GameObject target)
    {
        if (target != null && reachableTiles.ContainsKey(target))
            return reachableTiles[target].distance;
        else
            return -1;
    }

    public void HighlightPath(GameObject tile)
    {
        if(reachableTiles.ContainsKey(tile))
        {


        }

    }

	public void DeleteUnit(GameObject unit)
	{
        if (unit != selectedUnit)
        {
            UnitHandler uH = unit.GetComponent<UnitHandler>();
            if (uH.isAlive)
            {
                MapManager.singleton.GetTileAt(uH.gridPos).GetComponent<TileListener>().occupant = null;
            }
            RemoveUnitFromFaction(unit, uH.faction);
            unit.GetComponent<UnitHandler>().OnDelete();
			GameObject.Destroy(unit);
		}
		else
		{
			Debug.Log("[CombatManager:DeleteUnit] Unit to delete is selectedUnit");
		}
	}

    public GameObject InstantiateUnit(GameObject prefab, Faction newFaction, IntVector2 newPos)
    {
        // MAYBEDO: Should these errors be just log statements instead? We expect this behavior to come up based on user input
        GameObject tile = MapManager.singleton.GetTileAt(newPos);
        if (tile == null)
        {
            Debug.LogError("[CombatManager:InstantiateUnit] Can't instantiate a unit at invalid position " + newPos);
        }
        else
        {
            if (tile.GetComponent<TileListener>().occupant != null)
            {
                Debug.LogError("[CombatManager:InstantiateUnit] Can't instantiate a unit in occupied tile" + newPos);
            }
            else
            {
                if (gameState == GameState.ARMY_PLACEMENT && tile.GetComponent<TileListener>().deployable != currentPlayer)
                {
                    Debug.LogError("[CombatManager:InstantiateUnit] Can't deploy outside of deployable zone");
                    return null;
                }
                GameObject instance = Instantiate(prefab, MapManager.singleton.GetPositionAt(newPos), Quaternion.identity) as GameObject;
                //Setting Faction and Color
                AssignFaction(instance, newFaction);
                instance.transform.SetParent(unitHolder);
                instance.GetComponent<UnitHandler>().OurInit();
                instance.GetComponent<UnitHandler>().SetGridPosition(newPos, false);
                MapManager.singleton.GetTileAt(newPos).GetComponent<TileListener>().occupant = instance;
                if (gameState == GameState.ARMY_PLACEMENT)
                {
                    instance.GetComponent<UnitHandler>().HideHealthBar();
                    var type = instance.GetComponent<UnitHandler>().unitType;
                    ArmyDeploymentPanel.singleton.DecrementUnitSlot(type);
                }
                return instance;
            }
        }
        return null;
    }

    public void TargetReticleOn(GameObject target)
    {
        targetReticle.transform.position = target.transform.position;
        targetReticle.SetActive(true);
    }

    public void TargetReticleOff()
    {
        targetReticle.SetActive(false);
    }

#if UNITY_STANDALONE
    public void KeyboardUpdate()
    {
        //Camera Zooming Segment
        var mouseWheelAxis = Input.GetAxis("Mouse ScrollWheel");
        if (mouseWheelAxis != 0)
        {
            MapManager.singleton.CameraZoom(mouseWheelAxis, Input.mousePosition);
        }
        float zoomSum = 0;
        if (Input.GetKey(EventManager.singleton.shortcutCameraZoomIn))
        {
            zoomSum += 1f;
        }

        if (Input.GetKey(EventManager.singleton.shortcutCameraZoomOut))
        {
            zoomSum -= 1f;
        }
        if (zoomSum != 0f)
        {
            MapManager.singleton.CameraZoom(zoomSum * EventManager.singleton.keyboardZoomSpeed);
        }

        //Camera Panning Segment
        Vector3 panSum = new Vector3();
        if (Input.GetKey(EventManager.singleton.shortcutCameraPanRight))
        {
            panSum += Vector3.right;
        }
        if (Input.GetKey(EventManager.singleton.shortcutCameraPanLeft))
        {
            panSum += Vector3.left;
        }
        if (Input.GetKey(EventManager.singleton.shortcutCameraPanUp))
        {
            panSum += Vector3.up;
        }
        if (Input.GetKey(EventManager.singleton.shortcutCameraPanDown))
        {
            panSum += Vector3.down;
        }

        if (panSum != Vector3.zero)
        {
            Debug.Log("Keyboard Panning");
            MapManager.singleton.CameraPan(panSum * EventManager.singleton.pixelPanSpeed);
        }

        // Keyboard Shortcuts
        if (gameState == GameState.ARMY_PLACEMENT)
        {
            if (Input.GetKeyDown(EventManager.singleton.shortcutDeploymentAuto))
            {
                ArmyDeploymentPanel.singleton.PressedAutoButton();
                return;
            }
            if (Input.GetKeyDown(EventManager.singleton.shortcutDeploymentRandom))
            {
                ArmyDeploymentPanel.singleton.PressedRandomButton();
                return;
            }
            if (Input.GetKeyDown(EventManager.singleton.shortcutOpenMenu))
            {
                ShowPauseMenu();
                return;
            }
        }
        else
        {
            if (Input.GetKeyDown(EventManager.singleton.shortcutEndTurn))
            {
                if (endTurnButton.IsInteractable())
                {
                    ClickedEndTurnButton();
                    return;
                }
            }

            if (Input.GetKeyDown(EventManager.singleton.shortcutMove))
            {
                if (moveButton.IsInteractable())
                {
                    ClickedMoveButton();
                    return;
                }
            }

            if (Input.GetKeyDown(EventManager.singleton.shortcutAttack))
            {
                if (attackButton.IsInteractable())
                {
                    ClickedAttackButton();
                    return;
                }
            }

            if (Input.GetKeyDown(EventManager.singleton.shortcutBattleMagic))
            {
                if (battleMagicMenuButton.IsInteractable())
                {
                    ClickedBattleMagicMenuOpenButton();
                    return;
                }
            }

            if (selectedUnit != null)
            {
                var ability = selectedUnit.GetComponent<UnitHandler>().specialAbility;
                if (ability != null)
                {
                    if (Input.GetKeyDown(ability.hotKey))
                    {
                        if (specialButton.IsInteractable())
                        {
                            ClickedSpecialButton();
                            return;
                        }
                    }
                }
            }

            if (battleMagicPanel.activeSelf)
            {
                foreach(var spell in selectedUnit.GetComponent<UHBattlemage>().battleMagic)
                {
                    if (Input.GetKeyDown(spell.hotKey))
                    {
                        SelectGameState(GameState.SPECIAL, selectedUnit, spell);
                        return;
                    }
                }
            }

            if (Input.GetKeyDown(EventManager.singleton.shortcutUndo))
            {
                if (undoButton.IsInteractable())
                {
                    ClickedUndoButton();
                    return;
                }
            }

            if (Input.GetKeyDown(EventManager.singleton.shortcutGameplayCancel))
            {
                if (cancelButton.IsInteractable())
                {
                    ClickedCancelButton();
                    return;
                }
            }

            // Deselects the unit using the Esc key
            if (Input.GetKeyDown(EventManager.singleton.shortcutDeselect))
            {
                HideInformationPopup();
                if (battleMagicPanel.activeSelf)
                {
                    ClickedBattleMagicMenuCloseButton();
                    return;
                }
                else if (gameState == GameState.INSPECTING)
                {
                    SelectGameState(GameState.IDLE, null);
                    return;
                }
                else if (cancelButton.IsInteractable())
                {
                    ClickedCancelButton();
                    return;
                }
                else if (EventManager.singleton.shortcutDeselect == EventManager.singleton.shortcutOpenMenu)
                {
                    ShowPauseMenu();
                    return;
                }
            }

            if (Input.GetKeyDown(EventManager.singleton.shortcutCycle))
            {
                if (gameState == GameState.IDLE || gameState == GameState.INSPECTING)
                {
                    if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                        SelectPreviousUnitButton();
                    else
                        SelectNextUnitButton();
                    return;
                }
            }

            if (EventManager.singleton.shortcutOpenMenu != EventManager.singleton.shortcutDeselect && Input.GetKeyDown(EventManager.singleton.shortcutOpenMenu))
            {
                ShowPauseMenu();
                return;
            }
        }
        //Debug
        if(Input.GetKeyDown(EventManager.singleton.shortcutToggleDebugPanel))
        {
            BoardEditor.singleton.ToggleVisibility();
            return;
        }
    }
#endif
#if UNITY_IOS || UNITY_ANDROID
    public void TouchScreenUpdate()
    {
        MapManager.singleton.HandleTouchPanAndZoom();
    }
#endif

    public void ShowPauseMenu()
    {
        EventManager.singleton.GrantFocus(pauseMenu.GetComponent<ModalPopup>());
    }

    public void ShowInformationPopup(string infoText, UnityAction confirmButtonAction)
    {
        _informationPopup.ShowCustom(infoText, confirmButtonAction);
    }

    public void HideInformationPopup()
    {
        _informationPopup.Hide();
        turnSkipConfirmation = false;
    }

    public void ReturnToBank(UnitHandler unit)
    {
        if(unit.faction == currentPlayer)
        {
            ArmyDeploymentPanel.singleton.IncrementUnitSlot(unit.unitType);
            DeleteUnit(unit.gameObject);
        }
    }

    public void ConfirmDeployment()
    {
        //check if all deployment phases are completed
        if (ArmyManager.singleton.GetTotalUnitCount(Faction.RED) == 0 &&
            ArmyManager.singleton.GetTotalUnitCount(Faction.BLUE) == 0)
        {
            //moves to idle
            MapManager.singleton.RestoreAllTileColors();
            ChangeTurn(false);
        }
        else
        {
            //if not, begin deployment for other player.
            currentPlayer = Faction.BLUE;
            ArmyDeploymentPanel.singleton.BeginDeployment(currentPlayer);
        }
    }

    /// <summary>
    /// place all units of given army randomly
    /// 
    /// </summary>
    /// <param name="army"></param>
    public void DeployPatternRandom(Faction army)
    {
        //build a list of valid placement tiles
        var deployableArea = GetRemainingDeployableAreaOf(army);

        //place each unit in a deployable tile and remove that tile from the list.
        foreach (ArmyData.UnitType type in Enum.GetValues(typeof(ArmyData.UnitType)))
        {
            if (type != ArmyData.UnitType.NONE)
            {
                var prefab = GetPrefabForUnitType(type);
                while (ArmyManager.singleton.GetUnitCount(type, army) > 0)
                {
                    var location = deployableArea[Random.Range(0, deployableArea.Count)];
                    InstantiateUnit(prefab, army, location);
                    deployableArea.Remove(location);
                }
            }
        }
    }

    public void DeployPatternFEBA(Faction army){
        var febaProxi = GetBattleLineDistances(army);
        int ii = 0;
        foreach (var type in ArmyData.febaPriority)
        {
            //Assert UnitType.NONE is not present in the list
            var prefab = GetPrefabForUnitType(type);
            while (ArmyManager.singleton.GetUnitCount(type, army) > 0)
            {
                InstantiateUnit( prefab, army, febaProxi[ii]);
                ii++;
            }
        }
    }

    public List<IntVector2> GetBattleLineDistances(Faction army)
    {
        var mapTiles = MapManager.singleton.GetMapTiles();
        LinkedList<MapManager.PathfindingNode> doneTiles;
        Faction enemy;
        switch(army)
        {
            case Faction.RED:
                enemy = Faction.BLUE;
                break;
            case Faction.BLUE:
                enemy = Faction.RED;
                break;
            default:
                Debug.LogError("[CombatManager:GetBattleLineDistances] Invalid Faction.");
                return null;
        }

        // Two lists of pairs of distances and tiles
        // unsearchedTiles:    Tiles discovered by the algorithm, but not checked for neighbors
        LinkedList<MapManager.PathfindingNode> unsearchedTiles = new LinkedList<MapManager.PathfindingNode>();

        // Start by adding our current location to unsearchedTiles
        foreach (var tile in mapTiles)
        {
            if (tile.GetComponent<TileListener>().deployable == enemy)
            {
                unsearchedTiles.AddFirst(new MapManager.PathfindingNode(0, tile));
            }
        }
        MapManager.PathingInternal(out doneTiles, unsearchedTiles, 998, UnitHandler.PathingType.FOOT, Faction.NONE, unitPawnPrefab);

        List<IntVector2> outputLocations = new List<IntVector2>();
        foreach(var pathNode in doneTiles)
        {
            var tL = pathNode.destinationTile.GetComponent<TileListener>();
            if (tL.deployable == army)
            {
                outputLocations.Insert(0, tL.gridPos);
            }
        }
        string debug = "";
        foreach (var location in outputLocations)
        {
            debug += location.ToString() + ", ";
        }
        Debug.Log("[CombatManager:GetBattleLineDistances] Locations from closest to furthest: " + debug);
        return outputLocations;
    }

    public List<IntVector2> GetRemainingDeployableAreaOf(Faction army)
    {
        List<IntVector2> area = new List<IntVector2>();

        foreach (var tile in MapManager.singleton.GetMapTiles())
        {
            var tL = tile.GetComponent<TileListener>();
            if (tL.deployable == army && !tL.occupied)
            {
                area.Add(tL.gridPos);
            }
        }
        return area;
    }

    public List<IntVector2> GetDeployableAreaOf(Faction army)
    {
        List<IntVector2> area = new List<IntVector2>();

        foreach (var tile in MapManager.singleton.GetMapTiles())
        {
            var tL = tile.GetComponent<TileListener>();
            if (tL.deployable == army)
            {
                area.Add(tL.gridPos);
            }
        }
        return area;
    }

    public GameObject GetPrefabForUnitType(ArmyData.UnitType unitType)
    {
        switch (unitType)
        {
            case ArmyData.UnitType.PAWN:
                return unitPawnPrefab;
            case ArmyData.UnitType.KNIGHT:
                return unitKnightPrefab;
            case ArmyData.UnitType.BIKER:
                return unitBikerPrefab;
            case ArmyData.UnitType.ARCHER:
                return unitArcherPrefab;
            case ArmyData.UnitType.ARTILLERY:
                return unitArtilleryPrefab;
            case ArmyData.UnitType.BATTLEMAGE_LUMP:
                return unitBattlemageLumpPrefab;
            default:
                Debug.LogError("[CombatManager:GetPrefabForUnitType] Invalid unit type");
                return null;
        }
    }

    public void ExchangeTileOccupants(IntVector2 locationFrom, IntVector2 locationTo)
    {
        var tileFrom = MapManager.singleton.GetTileAt(locationFrom).GetComponent<TileListener>();
        var tileTo = MapManager.singleton.GetTileAt(locationTo).GetComponent<TileListener>();

        if (tileFrom != tileTo)
        {
            var occupantFrom = tileFrom.occupant;
            var occupantTo = tileTo.occupant;
            tileTo.occupant = occupantFrom;
            tileFrom.occupant = occupantTo;
            if (occupantFrom != null)
            {
                occupantFrom.GetComponent<UnitHandler>().SetGridPosition(locationTo, false);
            }
            if (occupantTo != null)
            {
                occupantTo.GetComponent<UnitHandler>().SetGridPosition(locationFrom, false);
            }
        }
    }

    public void SelectNextUnitButton()
    {
        // If no unit is selected...
        if (selectedUnit == null)
        {
            SelectFirstUnit(currentPlayer, true);
        }
        // Otherwise, if an enemy is selected...
        else if (selectedUnit.GetComponent<UnitHandler>().faction != currentPlayer)
        {
            if (cycleThroughEnemies)
            {
                SelectNextUnit(false);
            }
            else
            {
                SelectFirstUnit(currentPlayer, true);
            }
        }
        // Otherwise, a friendly unit is selected...
        else
        {
            SelectNextUnit(true);
        }
        var focusTiles = new List<GameObject>();
        focusTiles.Add(selectedUnit);
        MapManager.singleton.MoveCameraFocus(focusTiles);
    }

    public void SelectPreviousUnitButton()
    {
        // If no unit is selected...
        if (selectedUnit == null)
        {
            SelectLastUnit(currentPlayer, true);
        }
        // Otherwise, if an enemy is selected...
        else if (selectedUnit.GetComponent<UnitHandler>().faction != currentPlayer)
        {
            if (cycleThroughEnemies)
            {
                SelectPreviousUnit(false);
            }
            else
            {
                SelectLastUnit(currentPlayer, true);
            }
        }
        // Otherwise, a friendly unit is selected...
        else
        {
            SelectPreviousUnit(true);
        }
        var focusTiles = new List<GameObject>();
        focusTiles.Add(selectedUnit);
        MapManager.singleton.MoveCameraFocus(focusTiles);
    }

    public void RestoreUnitHighlights()
    {
        foreach (var unit in redUnits)
        {
            if (!unit.GetComponent<UnitHandler>().hasActed || unit == selectedUnit)
                unit.GetComponent<UnitHandler>().RestoreDefaultColor();
            else
                unit.GetComponent<UnitHandler>().SetUsedColor();
        }
        foreach (var unit in blueUnits)
        {
            if (!unit.GetComponent<UnitHandler>().hasActed || unit == selectedUnit)
                unit.GetComponent<UnitHandler>().RestoreDefaultColor();
            else
                unit.GetComponent<UnitHandler>().SetUsedColor();
        }
    }

    public void ShowAnimatic(GameObject attacker, GameObject defender, float duration)
    {
        if (OptionsManager.singleton.battleAnimationsEnabled)
        {
            animaticPopup.StartAnimation(duration, attacker, defender);
        }
        else
        {
            ResolveDamage();
        }
    }

    public void ResolveDamage()
    {
        List<UnitHandler> resolvingUnits = new List<UnitHandler>();
        // MAYBEDO: Does this need to happen in a specific order?
        foreach(var unit in redUnits)
        {
            if(unit.GetComponent<UnitHandler>().hasQueuedDamage)
            {
                resolvingUnits.Add(unit.GetComponent<UnitHandler>());
            }
        }
        foreach (var unit in blueUnits)
        {
            if (unit.GetComponent<UnitHandler>().hasQueuedDamage)
            {
                resolvingUnits.Add(unit.GetComponent<UnitHandler>());
            }
        }
        foreach (var unit in resolvingUnits)
        {
            unit.ResolveDamage();
        }
        UpdateUI();
        CheckVictory();
    }

    public bool UnitIsAlive(GameObject unit)
    {
        if (redUnits.Contains(unit) || blueUnits.Contains(unit))
        {
            return true;
        }
        return false;
    }

    public void MouseOverTile(GameObject tile)
    {
        if (gameState == GameState.MOVING)
        {
            if (reachableTiles.ContainsKey(tile))
            {
                TracePath(reachableTiles[tile].path);
            }
        }
    }

    public void MouseOff()
    {
        if (gameState == GameState.MOVING)
        {
            EmptyQuiver();
        }
    }

    public LinkedList<GameObject> GetPathTo(IntVector2 coords)
    {
        GameObject tile = MapManager.singleton.GetTileAt(coords);
        if (reachableTiles.ContainsKey(tile))
        {
            return reachableTiles[tile].path;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Used for prompted game save action.
    /// </summary>
    /// <param name="fileName"></param>
    public void SaveGame(string fileName = null)
    {
        FileManager.singleton.EnsureDirectoryExists(SAVE_DIRECTORY);
        if (fileName == null)
        {
            fileName = DEFAULT_SAVE_FILENAME;
        }
        if (FileManager.FileExtensionIs(fileName, SAVE_FILE_EXTENSION))
        {
            saveFileName = fileName;
        }
        else
        {
            saveFileName = fileName + SAVE_FILE_EXTENSION;
        }

        SaveGameInternal(SAVE_DIRECTORY + saveFileName);
    }

    public void LoadGame()
    {
        //file
        SavedGame game = GameLoader.singleton.gameSave;
        if (game == null)
        {
            Debug.LogError("[CombatManager:LoadGame] cannot load game");
            return;
        }

        //Map
        MapManager.singleton.DeserializeMap(game.map);

        // logging metrics
        redElapsedTime = game.redElapsedTime;
        blueElapsedTime = game.blueElapsedTime;
        gameStartTimer = Time.time - game.gameTimer;
        roundCount = game.roundCount;
        redTurnCounter = game.redTurnCounter;
        redSkippedTurnsCounter = game.redSkippedTurnsCounter;
        blueTurnCounter = game.blueTurnCounter;
        blueSkippedTurnsCounter = game.blueSkippedTurnsCounter;
        numTurnSkips = game.numTurnSkips;

        if (game.previousLogFileName != null)
        {
            FileManager.singleton.AppendToLog("Resuming from Saved Game, previous Log File: " + game.previousLogFileName);
        }
        else
        {
            FileManager.singleton.AppendToLog("Resuming from Saved Game, previous Log File not saved.");
        }

        //current player
        currentPlayer = game.currentPlayer;
        UpdateTurnLabel(currentPlayer);

        //units
        DeserializeUnits(game.blueDeadUnits, Faction.BLUE, true);
        DeserializeUnits(game.redDeadUnits, Faction.RED, true);
        DeserializeUnits(game.blueUnits, Faction.BLUE, false);
        DeserializeUnits(game.redUnits, Faction.RED, false);

        RestoreUnitHighlights();
    }

    public void DeserializeUnits(SerializableUnit[] units, Faction faction, bool dead)
    {
        GameObject toInstantiate;
        UnitHandler uH;
        foreach(var unit in units)
        {
            toInstantiate = InstantiateUnit(GetPrefabForUnitType(unit.unitType), faction, unit.gridPos);
            uH = toInstantiate.GetComponent<UnitHandler>();
            uH.LoadDeserializedData(unit);
        }
    }

    private void SaveUnits(List<GameObject> liveUnits, List<GameObject> deadUnits, List<SerializableUnit> savedUnits, List<SerializableUnit> savedDeadUnits)
    {
        // potential asynchronicity problem... units could move from liveUnits to deadUnits
        // while we're accessing them
        foreach (var unit in liveUnits)
        {
            var uH = unit.GetComponent<UnitHandler>();
            if (uH.willBeAlive)
            {
                savedUnits.Add(uH.Serialize());
            }
            else
            {
                savedDeadUnits.Add(uH.Serialize());
            }
        }
        foreach (var unit in deadUnits)
        {
            var uH = unit.GetComponent<UnitHandler>();
            savedDeadUnits.Add(uH.Serialize());
        }
    }

    public void SetAllUnitsVisibility(bool visible)
    {
        unitHolder.gameObject.SetActive(visible);
    }

    #endregion

    #region private methods

    private void BoardSetup()
    {
        gameState = GameState.NONE;
        _curAbility = null;
        switch (GameLoader.singleton.startUpMode)
        {
            case GameLoader.StartUpMode.NEW_GAME:
                currentPlayer = Faction.RED;
                ChangeGameState(GameState.ARMY_PLACEMENT);
                unitHolder = new GameObject("Units").transform;
                break;
            case GameLoader.StartUpMode.LOAD_GAME:
                unitHolder = new GameObject("Units").transform;
                LoadGame();
                ArmyDeploymentPanel.singleton.Hide();
                MapManager.singleton.RestoreAllTileColors();
                ChangeGameState(GameState.IDLE);
                break;
        }
    }

    private void SetupScene()
    {
        Debug.Log("Setting up scene");
        UnitHandler.HealthBarPrefab = HealthBarPrefab;
        UnitHandler.StatusDisplayPrefab = StatusDisplayPrefab;
        endTurnPanel = GameObject.Find("Turn Panel");
        selectionPanel = GameObject.Find("Selection Panel");
        HideSelectionPanel();
        battleMagicPanel = GameObject.Find("BattleMagicPanel");
        battleMagicMenuButton = GameObject.Find("BattleMagicMenuButton").GetComponent<Button>();
        battleMagicMenuButtonLabel = GameObject.Find("BattleMagicMenuButtonLabel").GetComponent<Text>();
        battleMagicButtonLabels = new Text[NUM_MAGIC_BUTTONS];
        battleMagicButtons = new Button[NUM_MAGIC_BUTTONS];
        unemployedArrows = new Stack<GameObject>();
        pathArrows = new Stack<GameObject>();
        for(int ii = 0; ii < NUM_MAGIC_BUTTONS; ++ii)
        {
            battleMagicButtons[ii] = GameObject.Find("BattleMagicButton0" + ii).GetComponent<Button>();
            battleMagicButtonLabels[ii] = GameObject.Find("BattleMagicButton0" + ii + "Label").GetComponent<Text>();
        }
        battleMagicPanel.SetActive(false);
        unitInfoPanel = GameObject.Find("UnitPanel");
        unitInfoPanelFullHeight = unitInfoPanel.GetComponent<RectTransform>().rect.height;
        hpLabel = GameObject.Find("HPLabel").GetComponent<Text>();
        dmgLabel = GameObject.Find("DmgLabel").GetComponent<Text>();
        moveLabel = GameObject.Find("MoveLabel").GetComponent<Text>();
        unitLabel = GameObject.Find("UnitLabel").GetComponent<Text>();
        turnLabel = GameObject.Find("TurnLabel").GetComponent<Text>();
        moveButtonLabel = GameObject.Find("MoveButtonLabel").GetComponent<Text>();
        attackButtonLabel = GameObject.Find("AttackButtonLabel").GetComponent<Text>();
        specialButtonLabel = GameObject.Find("SpecialButtonLabel").GetComponent<Text>();
        undoButtonLabel = GameObject.Find("UndoButtonLabel").GetComponent<Text>();
        cancelButtonLabel = GameObject.Find("CancelButtonLabel").GetComponent<Text>();
        endTurnButtonLabel = GameObject.Find("EndTurnButtonLabel").GetComponent<Text>();
        moveButton = GameObject.Find("MoveButton").GetComponent<Button>();
        attackButton = GameObject.Find("AttackButton").GetComponent<Button>();
        specialButton = GameObject.Find("SpecialButton").GetComponent<Button>();
        undoButton = GameObject.Find("UndoButton").GetComponent<Button>();
        cancelButton = GameObject.Find("CancelButton").GetComponent<Button>();
        endTurnButton = GameObject.Find("EndTurnButton").GetComponent<Button>();
        unitInfoPanelReducedHeight = (moveLabel.transform.localPosition.y + moveButton.transform.localPosition.y -unitInfoPanelFullHeight) * -.5f;

        //@HAX move attack button when special button replaces it
        attackButtonY = attackButton.transform.localPosition.y;
        specialButtonY = specialButton.transform.localPosition.y;

        selectedUnit = null;

        BoardSetup();
    }

    private List<GameObject> GetUnitsOfFaction(Faction faction)
    {
        switch (faction)
        {
            case CombatManager.Faction.RED:
                return redUnits;
            case CombatManager.Faction.BLUE:
                return blueUnits;
            default:
                Debug.LogError("[CombatManager:GetUnitsOfFaction] Invalid faction");
                return redUnits;
        }
    }

    private void ChangeGameState(GameState newState)
    {
        ChangeGameState(newState, null);
    }

    private void ChangeGameState(GameState newState, UnitSpecialAbility newAbility)
    {
		Debug.Log("Changing game state to " + newState);
		if(currentPlayer == Faction.NONE)
		{
			Debug.LogError("[CombatManager:ChangeGameState] Current player faction invalid.");
		}

        HideInformationPopup();

        // STATE.EXIT()
        switch (gameState)
        {
            case GameState.NONE:
                //TODO: anything that need to happen before initial army placement
                
                break;
            case GameState.ARMY_PLACEMENT:
                ArmyDeploymentPanel.singleton.Hide();
                SetGameplayPanelVisibility(true);
                WorldInterfaceLayer.singleton.SetCameraMode();
                ShowAllHealthBars();
                roundCount = 1;
                gameStartTimer = Time.time;
                turnElapsedTime = 0;
                ArmyManager.singleton.RevertToBackup();
                FileManager.singleton.AppendToLog("Map Name: " + MapManager.singleton.mapName);
                FileManager.singleton.AppendToLog("Number of Map Tiles: " + MapManager.singleton.GetMapTiles().Count + Environment.NewLine);
                LogArmyDetails(Faction.RED);
                FileManager.singleton.AppendToLog("");
                LogArmyDetails(Faction.BLUE);
                DisplayRoundNumber();
                break;
            case GameState.IDLE:
                ShowUnitPanel();
                HideSelectionPanel();
                break;
            case GameState.ORDERING:
                break;
            case GameState.MOVING:
                MapManager.singleton.RestoreAllTileColors();
                TargetReticleOff();
                EmptyQuiver();
                //reachableTiles.Clear();
                break;
            case GameState.ATTACKING:
                MapManager.singleton.RestoreAllTileColors();
                RestoreUnitHighlights();
                TargetReticleOff();
                VoiceController.singleton.FireAllVoices();
                break;
            case GameState.CONFIRMING:
                if (_curAbility == null)
                {
                    Debug.LogError("[CombatManager:ChangeGameState] exiting confirm state with no current ability");
                }
                else
                {
                    _curAbility.ConfirmStateExit();
                }
                break;
            case GameState.SPECIAL:
                if (_curAbility == null)
                {
                    Debug.LogError("[CombatManager:ChangeGameState] exiting special state with no current ability");
                }
                else
                {
                    _curAbility.SpecialStateExit();
                }
                break;
            case GameState.INSPECTING:
                HideSelectionPanel();
                MapManager.singleton.RestoreAllTileColors();
                break;
            default:
                Debug.LogError("assert(true);");
                break;
        }
        //TODO: is there a more better way to do this?
        HideBattleMagicPanel();

        gameState = newState;
        _curAbility = newAbility;

        // STATE.ENTER()
        switch (newState)
        {
            case GameState.ARMY_PLACEMENT:
                //tell the deployment panel to init and show.
                ArmyManager.singleton.BackupArmyData();
                ArmyDeploymentPanel.singleton.BeginDeployment(currentPlayer);
                SetGameplayPanelVisibility(false);
                WorldInterfaceLayer.singleton.SetUnitMode();
                break;
            case GameState.IDLE:
                GreyOutActionButtons();
                GreyOutCancelButton();
                SelectReticleOff();
                HideUnitPanel();
                ShowSelectionPanel();
                break;
            case GameState.ORDERING:
                SetUpActionButtons();
                GreyOutCancelButton();
                if (selectedUnit.GetComponent<UnitHandler>().isAlive)
                {
                    SelectReticleToSelectedUnit();
                }
                else
                {
                    SelectReticleOff();
                }
                CheckForAutoEndTurn();
                break;
            case GameState.MOVING:
                {
                    selectedUnit.GetComponent<UnitHandler>().ClearTarget();
                    selectedUnit.GetComponent<UnitHandler>().GetPathableTiles(out reachableTiles);
                    foreach (var pair in reachableTiles)
                    {
                        if (pair.Key.GetComponent<TileListener>().GetPathingType(selectedUnit, false) == StageNine.TileModifier.PathingType.CLEAR)
                        {
                            pair.Key.GetComponent<TileListener>().HighlightMove();
                        }
                        else
                        {
                            pair.Key.GetComponent<TileListener>().HighlightAttack();
                        }
                    }

                    RefocusCameraOnReachableArea(reachableTiles);

                    SetUpActionButtons();
                    SetUpCancelButton();
                    SelectReticleToSelectedUnit();
                    break;
                }
            case GameState.ATTACKING:
                {
                    selectedUnit.GetComponent<UnitHandler>().ClearTarget();
                    selectedUnit.GetComponent<UnitHandler>().GetAttackableTilesAndUnits(out attackableTiles, out attackableUnits);
                    HighlightAttackableTilesAndUnits();
                    MapManager.singleton.MoveCameraFocus(attackableTiles);
                    SetUpActionButtons();
                    SetUpCancelButton();
                    break;
                }
            case GameState.CONFIRMING:
                if (_curAbility == null)
                {
                    Debug.LogError("[CombatManager:ChangeGameState] entering confirm state with no current ability");
                }
                else
                {
                    _curAbility.ConfirmStateEnter();
                    SetUpCancelButton();
                }
                break;
            case GameState.SPECIAL:
                if (_curAbility == null)
                {
                    Debug.LogError("[CombatManager:ChangeGameState] entering special state with no speical ability");
                }
                else
                {
                    SetUpCancelButton();
                    _curAbility.SpecialStateEnter();
                }
                break;
            case GameState.INSPECTING:

                if (selectedUnit.GetComponent<UnitHandler>().faction == currentPlayer &&
                    !selectedUnit.GetComponent<UnitHandler>().hasActed)
                {
                    Debug.Log("Inspecting unit, can act");
                    EnlargeUnitPanel();
                    SetUpActionButtons();
                }
                else
                {
                    if (selectedUnit.GetComponent<UnitHandler>().faction != currentPlayer)
                    {
                        Debug.Log("Inspecting unit, This is an enemy unit.");
                        ShrinkUnitPanel();
                        HighlightThreatTiles();
                    }
                    else
                    {
                        Debug.Log("[CombatManager:ChangeGameState] inspecting unit has already acted.");
                        EnlargeUnitPanel();
                    }
                    GreyOutActionButtons();
                }
                GreyOutCancelButton();
                SelectReticleToSelectedUnit();
                ShowSelectionPanel();
                break;
            default:
                Debug.LogError("assert(true);");
                break;
        }
        UpdateUI();
    }

    private void CheckForAutoEndTurn()
    {
        if(OptionsManager.singleton.autoEndTurnEnabled)
        {
            if(battleMagicMenuButton.interactable)
            {
                if (selectedUnit.GetComponent<UHBattlemage>().battleMagicReady)
                {
                    // the selected unit is a battlemage with at least one available battlemagic
                    return;
                }
            }
            if(!(moveButton.interactable ||
                 attackButton.interactable ||
                 specialButton.interactable ||
                 undoButton.interactable))
            {
                // the selected unit can't move, undo movement, attack, or use a special ability
                ChangeTurn(false);
            }
        }
    }

    private void RefocusCameraOnReachableArea(Dictionary<GameObject, MapManager.PathfindingNode> tiles)
    {
        List<GameObject> cameraFocus = new List<GameObject>();
        foreach (var pair in tiles)
        {
            cameraFocus.Add(pair.Key);
        }
        MapManager.singleton.MoveCameraFocus(cameraFocus);
    }

    private void ShowAllHealthBars()
    {
        foreach (var unit in redUnits)
        {
            unit.GetComponent<UnitHandler>().ShowHealthBar();
        }
        foreach (var unit in blueUnits)
        {
            unit.GetComponent<UnitHandler>().ShowHealthBar();
        }

    }

    private void SetGameplayPanelVisibility(bool visible)
    {
        endTurnPanel.SetActive(visible);
        unitInfoPanel.SetActive(visible);
    }

    private void GreyOutActionButtons()
    {
        moveButtonLabel.color = Color.grey;
        moveButton.interactable = false;
        attackButtonLabel.color = Color.grey;
        attackButton.interactable = false;
		specialButtonLabel.color = Color.grey;
		if(selectedUnit != null && selectedUnit.GetComponent<UnitHandler>().specialAbility != null)
		{
			specialButtonLabel.text = selectedUnit.GetComponent<UnitHandler>().specialAbility.buttonLabel;
		}
		else
		{
			specialButtonLabel.text = UNAVAILABLE_ACTION_LABEL;
		}
		specialButton.interactable = false;
        battleMagicMenuButtonLabel.color = Color.grey;
        battleMagicMenuButton.interactable = false;
        undoButtonLabel.color = Color.grey;
        undoButton.interactable = false;

        //@HAX move attack button when special button replaces it
        SwapAttackButtons(false);
    }

    private void SetUpActionButtons()
    {
        moveButtonLabel.color = Color.black;
        moveButton.interactable = true;
        attackButtonLabel.color = Color.black;
        attackButton.interactable = true;
		if(selectedUnit != null && selectedUnit.GetComponent<UnitHandler>().specialAbility != null)
		{
			specialButtonLabel.color = Color.black;
			specialButtonLabel.text = selectedUnit.GetComponent<UnitHandler>().specialAbility.buttonLabel;
			specialButton.interactable = true;
		}
		else
		{
			specialButtonLabel.color = Color.grey;
			specialButtonLabel.text = UNAVAILABLE_ACTION_LABEL;
			specialButton.interactable = false;
		}
        if (selectedUnit != null && selectedUnit.GetComponent<UHBattlemage>() != null)
        {
            battleMagicMenuButtonLabel.color = Color.black;
            battleMagicMenuButton.interactable = true;
        }
        else
        {
            battleMagicMenuButtonLabel.color = Color.grey;
            battleMagicMenuButton.interactable = false;
        }
        undoButtonLabel.color = Color.black;
        undoButton.interactable = true;
        GreyOutUnavailableButtons();
    }

    private void GreyOutBattleMagicButtons()
    {
        for(int ii = 0; ii < NUM_MAGIC_BUTTONS; ++ii)
        {
            battleMagicButtons[ii].interactable = false;
        }
    }

    private void SetUpBattleMagicButtons()
    {
        var UHBM = selectedUnit.GetComponent<UHBattlemage>();
        for (int ii = 0; ii < NUM_MAGIC_BUTTONS; ++ii)
        {
            if (UHBM.battleMagic.Count > ii)
            {
                battleMagicButtonLabels[ii].text = UHBM.battleMagic[ii].buttonLabel;
                if (UHBM.battleMagic[ii].buttonInteractable)
                {
                    battleMagicButtons[ii].interactable = true;
                    battleMagicButtonLabels[ii].color = Color.black;
                }
                else
                {
                    battleMagicButtons[ii].interactable = false;
                    battleMagicButtonLabels[ii].color = Color.grey;
                }
            }
            else
            {
                battleMagicButtonLabels[ii].text = UNAVAILABLE_ACTION_LABEL;
                battleMagicButtons[ii].interactable = false;
                battleMagicButtonLabels[ii].color = Color.grey;
            }
        }
    }

    private void GreyOutCancelButton()
    {
        cancelButtonLabel.color = Color.grey;
        cancelButton.interactable = false;
    }

    private void SetUpCancelButton()
    {
        cancelButtonLabel.color = Color.black;
        cancelButton.interactable = true;
    }

    private void ShowUnitPanel()
    {
        unitInfoPanel.SetActive(true);
    }

    private void HideUnitPanel()
    {
        unitInfoPanel.SetActive(false);
    }

    private void ShowSelectionPanel()
    {
        selectionPanel.SetActive(true);
    }

    private void HideSelectionPanel()
    {
        selectionPanel.SetActive(false);
    }

    private void EnlargeUnitPanel()
    {
        if(unitInfoPanel.GetComponent<RectTransform>().rect.height != unitInfoPanelFullHeight)
        {
            unitInfoPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(unitInfoPanel.GetComponent<RectTransform>().rect.width, unitInfoPanelFullHeight);

            moveButton.gameObject.SetActive(true);
            attackButton.gameObject.SetActive(true);
            specialButton.gameObject.SetActive(true);
            cancelButton.gameObject.SetActive(true);
            undoButton.gameObject.SetActive(true);
            battleMagicMenuButton.gameObject.SetActive(true);

            unitInfoPanel.transform.localPosition -= new Vector3(0f, (unitInfoPanelFullHeight - unitInfoPanelReducedHeight) * 0.5f);
        }
    }

    private void ShrinkUnitPanel()
    {
        if (unitInfoPanel.GetComponent<RectTransform>().rect.height != unitInfoPanelReducedHeight)
        {
            unitInfoPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(unitInfoPanel.GetComponent<RectTransform>().rect.width, unitInfoPanelReducedHeight);

            moveButton.gameObject.SetActive(false);
            attackButton.gameObject.SetActive(false);
            specialButton.gameObject.SetActive(false);
            cancelButton.gameObject.SetActive(false);
            undoButton.gameObject.SetActive(false);
            battleMagicMenuButton.gameObject.SetActive(false);

            unitInfoPanel.transform.localPosition += new Vector3(0f, (unitInfoPanelFullHeight - unitInfoPanelReducedHeight) * 0.5f);
        }
    }

    private float GetCurrentCanvasScale()
    {
        return Screen.height / unitInfoPanel.GetComponentInParent<CanvasScaler>().referenceResolution.y;
    }

    private void SelectReticleToSelectedUnit()
    {
        selectReticle.transform.position = MapManager.singleton.GetTileAt(selectedUnit.GetComponent<UnitHandler>().gridPos).transform.position;
        selectReticle.SetActive(true);
    }
    private void SelectReticleOff()
    {
        selectReticle.SetActive(false);
    }

    private void SelectFirstUnit(Faction faction, bool activeOnly)
    {
        switch(faction)
        {
            case Faction.RED:
                foreach (var unit in redUnits)
                {
                    if (!unit.GetComponent<UnitHandler>().hasActed || !activeOnly)
                    {
                        SelectGameState(GameState.INSPECTING, unit);
                        return;
                    }
                }
                break;
            case Faction.BLUE:
                foreach (var unit in blueUnits)
                {
                    if (!unit.GetComponent<UnitHandler>().hasActed || !activeOnly)
                    {
                        SelectGameState(GameState.INSPECTING, unit);
                        return;
                    }
                }
                break;
        }
    }

    private void SelectLastUnit(Faction faction, bool activeOnly)
    {
        switch (faction)
        {
            case Faction.RED:
                for (int ii = redUnits.Count - 1; ii >= 0; ii--)
                {
                    if (!redUnits[ii].GetComponent<UnitHandler>().hasActed || !activeOnly)
                    {
                        SelectGameState(GameState.INSPECTING, redUnits[ii]);
                        return;
                    }
                }
                break;
            case Faction.BLUE:
                for (int ii = blueUnits.Count - 1; ii >= 0; ii--)
                {
                    if (!blueUnits[ii].GetComponent<UnitHandler>().hasActed || !activeOnly)
                    {
                        SelectGameState(GameState.INSPECTING, blueUnits[ii]);
                        return;
                    }
                }
                break;
        }
    }

    private void SelectNextUnit(bool activeOnly)
    {
        SelectNextOrPrevious(activeOnly, true);
    }

    private void SelectPreviousUnit(bool activeOnly)
    {
        SelectNextOrPrevious(activeOnly, false);
    }

    private void SelectNextOrPrevious(bool activeOnly, bool nextUnit)
    {
        int direction;
        if (nextUnit)
            direction = 1;
        else
            direction = -1;

        int selectedIndex;
        switch(selectedUnit.GetComponent<UnitHandler>().faction)
        {
            case Faction.RED:
                selectedIndex = redUnits.IndexOf(selectedUnit);
                for (int ii = selectedIndex + direction; ii < redUnits.Count && ii >= 0; ii += direction)
                {
                    if (!redUnits[ii].GetComponent<UnitHandler>().hasActed || !activeOnly)
                    {
                        SelectGameState(GameState.INSPECTING, redUnits[ii]);
                        return;
                    }
                }
                if(cycleThroughBothArmies)
                {
                    if (nextUnit)
                        SelectFirstUnit(Faction.BLUE, !activeOnly);
                    else
                        SelectLastUnit(Faction.BLUE, !activeOnly);
                }
                else
                {
                    if (nextUnit)
                        SelectFirstUnit(Faction.RED, activeOnly);
                    else
                        SelectLastUnit(Faction.RED, activeOnly);
                }
                break;
            case Faction.BLUE:
                selectedIndex = blueUnits.IndexOf(selectedUnit);
                for (int ii = selectedIndex + direction; ii < blueUnits.Count && ii >= 0; ii += direction)
                {
                    if (!blueUnits[ii].GetComponent<UnitHandler>().hasActed || !activeOnly)
                    {
                        SelectGameState(GameState.INSPECTING, blueUnits[ii]);
                        return;
                    }
                }
                if (cycleThroughBothArmies)
                {
                    if (nextUnit)
                        SelectFirstUnit(Faction.RED, !activeOnly);
                    else
                        SelectLastUnit(Faction.RED, !activeOnly);
                }
                else
                {
                    if (nextUnit)
                        SelectFirstUnit(Faction.BLUE, activeOnly);
                    else
                        SelectLastUnit(Faction.BLUE, activeOnly);
                }
                break;
        }
    }

    private bool CheckForWinCondition(WinCondition condition, List<GameObject> unitList)
    {
        switch (condition)
        {
            case WinCondition.KILL_ALL:
                return unitList.Count == 0;
            case WinCondition.KILL_BATTLEMAGE:
                foreach (var unit in unitList)
                {
                    if (unit.GetComponent<UnitHandler>().unitType >= ArmyData.FIRST_BATTLEMAGE)
                    {
                        return false;
                    }
                }
                return true;
            default:
                Debug.LogError("[CombatManager:CheckForWinCondition] Invalid Win Condition");
                return false;
        }
    }

    private GameObject HireArrow()
    {
        //check if there are any off-duty arrows in the pool.
        if (unemployedArrows.Count > 0)
        {
            //if there are, pop it off of the pool and return it.
            var newArrow = unemployedArrows.Pop();
            newArrow.SetActive(true);
            return newArrow;
        }
        else
        {
            var newArrow = Instantiate(arrowPrefab) as GameObject;
            newArrow.transform.SetParent(unitHolder);
            return newArrow;
        }
    }

    private void FireArrow(GameObject arrow)
    {
        arrow.SetActive(false);
        unemployedArrows.Push(arrow);
    }

    private void PlaceArrow(GameObject headTile, GameObject tailTile)
    {
        Vector3 centerPos = (headTile.transform.position + tailTile.transform.position) * 0.5f;
        Quaternion angle = Quaternion.FromToRotation(Vector3.up, headTile.transform.position - tailTile.transform.position);
        GameObject arrow = HireArrow();
        arrow.transform.position = centerPos;
        arrow.transform.rotation = angle;
        pathArrows.Push(arrow);
    }

    private void EmptyQuiver()
    {
        while(pathArrows.Count > 0)
        {
            FireArrow(pathArrows.Pop());
        }
    }

    private void TracePath(LinkedList<GameObject> path)
    {
        for(LinkedListNode<GameObject> node = path.First; node.Next != null; node = node.Next)
        {
            PlaceArrow(node.Next.Value, node.Value);
        }
    }

    //@deprecated
    //private void AdjustCameraLimits(int gridX, int gridY)
    //{
    //    var worldPosition = GetPositionAt(gridX, gridY);
    //    xCameraMin = Math.Min(xCameraMin, worldPosition.x - TILE_WIDTH * 0.5f);
    //    xCameraMax = Math.Max(xCameraMax, worldPosition.x + TILE_WIDTH * 0.5f);
    //    yCameraMin = Math.Min(yCameraMin, worldPosition.y - TILE_HEIGHT * 0.5f);
    //    yCameraMax = Math.Max(yCameraMax, worldPosition.y + TILE_HEIGHT * 0.5f);
    //}

    private void AutoSave()
    {
        FileManager.singleton.EnsureDirectoryExists(SAVE_DIRECTORY);
        SaveGameInternal(SAVE_DIRECTORY + AUTOSAVE_FILENAME);
        //TODO: figure out how to solve this synchonicity issue.
        //SaveGameInternal(SAVE_DIRECTORY + AUTOSAVE_TEMP_FILENAME);
        //FileManager.singleton.CautiousReplace(SAVE_DIRECTORY + AUTOSAVE_TEMP_FILENAME, SAVE_DIRECTORY + AUTOSAVE_FILENAME);
    }

    //internal
    private void SaveGameInternal(string filePath)
    {
        SavedGame game = new SavedGame();

        //units
        List<SerializableUnit> savedRedUnits = new List<SerializableUnit>();
        List<SerializableUnit> savedRedDeadUnits = new List<SerializableUnit>();
        List<SerializableUnit> savedBlueUnits = new List<SerializableUnit>();
        List<SerializableUnit> savedBlueDeadUnits = new List<SerializableUnit>();
        SaveUnits(redUnits, redDeadUnits, savedRedUnits, savedRedDeadUnits);
        SaveUnits(blueUnits, blueDeadUnits, savedBlueUnits, savedBlueDeadUnits);

        game.redUnits = savedRedUnits.ToArray();
        game.redDeadUnits = savedRedDeadUnits.ToArray();
        game.blueUnits = savedBlueUnits.ToArray();
        game.blueDeadUnits = savedBlueDeadUnits.ToArray();

        //current player
        if (gameState == GameState.ARMY_PLACEMENT)
        {
            game.currentPlayer = DEFAULT_FIRST_PLAYER_FACTION;
        }
        else
        {
            game.currentPlayer = currentPlayer;
        }

        // logging metrics
        game.redElapsedTime = redElapsedTime;
        game.blueElapsedTime = blueElapsedTime;
        game.gameTimer = Time.time - gameStartTimer;
        game.roundCount = roundCount;
        game.redTurnCounter = redTurnCounter;
        game.redSkippedTurnsCounter = redSkippedTurnsCounter;
        game.blueTurnCounter = blueTurnCounter;
        game.blueSkippedTurnsCounter = blueSkippedTurnsCounter;
        game.numTurnSkips = numTurnSkips;
        game.previousLogFileName = FileManager.logFileName;

        //Map
        game.map = MapManager.singleton.SerializeMap();


        FileManager.singleton.Save<SavedGame>(game, filePath);
    }

    private void LogGameSummary()
    {
        switch(currentPlayer)
        {
            case Faction.RED:
                redElapsedTime += turnElapsedTime;
                break;
            case Faction.BLUE:
                blueElapsedTime += turnElapsedTime;
                break;
        }

        turnElapsedTime = 0;

        LogArmyDetails(Faction.RED);
        FileManager.singleton.AppendToLog("Red player elapsed time: " + redElapsedTime);
        LogArmyDetails(Faction.BLUE);
        FileManager.singleton.AppendToLog("Blue player elapsed time: " + blueElapsedTime);
        FileManager.singleton.AppendToLog("Game lasted this many seconds: " + (Time.time - gameStartTimer));
        FileManager.singleton.AppendToLog("GAME OVER" + Environment.NewLine);
    }
    #endregion

    #region monobehaviors
    // Use this for initialization
    void Awake()
    {
        Debug.Log("[CombatManager:Awake]");
        if (singleton == null)
		{
			Debug.Log("Hello, world!");
			singleton = this;
		}
		else
		{
			Debug.Log("Goodbye, cruel world!");
			GameObject.Destroy(gameObject);
		}
	}

    void Start()
    {
        Debug.Log("[CombatManager:Start]");

        if (!Connect())
        {
            Debug.LogError("[CombatManager:Start] Could not connect listener to EventManager");
        }
        else
        {
            EventManager.singleton.ResetControlFocus(this);
        }
        MapManager.singleton.mapContext = MapManager.MapContext.COMBAT;
        SetupScene();
        animaticPopup.OurInit();
    }

    void Update()
    {
        if (displayRoundAfterAnimation)
        {
            displayRoundAfterAnimation = false;
            DisplayRoundNumber();
        }
        if(EventManager.singleton.HasControlFocus(this))
        {
            turnElapsedTime += Time.deltaTime;
        }
        else
        {
            otherElapsedTime += Time.deltaTime;
        }
    }

    void OnDestroy()
    {
        Debug.Log("[CombatManager:OnDestroy]");
        if (!Disconnect())
        {
            Debug.LogWarning("[CombatManager:OnDestroy] Couldn't disconnect from EventManager!");
        }
    }

#endregion


}
