using UnityEngine;
using System.Collections.Generic;
using StageNine;
using UnityEngine.UI;
using UnityEngine.Events;
using System;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour, IModalFocusHolder {

    public enum MenuWindows
    {
        NONE,
        MAIN,
        MOBILE_PLAY,
        MOBILE_CREATE,
        HELP,
        OPTIONS,
        MAP_EDITOR,
        ARMY_BUILDER,
        SINGLE_PLAYER,
        LOCAL_MULTIPLAYER,
        ONLINE_MULTIPLAYER,
        TUTORIAL,
        CAMPAIGN,
        VS_AI,
        FIND_A_GAME,
        QUICKPLAY,
        ARMY_SETUP,
        ARMY_SETUP_LOCAL
    }

    private enum ErrorCode
    {
        NONE,
        ARMY_TOO_EXPENSIVE,
        ARMY_NOT_LOADED
    }

    public const int DEFAULT_ARMY_POINT_MAX = 200;
    public const string MAIN_MENU_SCENE =
#if UNITY_IOS || UNITY_ANDROID
    "MainMenu Mobile";
#else
    "MainMenu";
#endif

    public static MainMenu singleton;

    public Stack<MenuWindows> menuHistory;

    public GameObject titleText;
    public GameObject mainMenuPanel;
    public GameObject optionsPanel;
    public GameObject gameSetupPanel;
    public GameObject armyBuilderPanel;
    public GameObject mobilePlayPanel;
    public GameObject mobileCreatePanel;

    public int numPlayers = 2;

    private bool gameSetupMapReady;
    private bool gameSetupArmiesReady;

    [SerializeField] private GameObject openFileDialog;
    [SerializeField] private GameObject saveAsFileDialog;
    [SerializeField] private OptionsMenuPanel optionsMenuPanel;

    [SerializeField] private Button startButton;
    [SerializeField] private Text startButtonLabel;

    [SerializeField] private Text redArmyName;
    [SerializeField] private Text blueArmyName;
    [SerializeField] private Text mapTitle;
    [SerializeField] private Image mapPreview;

    [SerializeField] private Sprite unknownTexture;

    //[SerializeField] private Button pawnMinusButton;
    [SerializeField] private Text pawnMinusButtonLabel;
    //[SerializeField] private Button archerMinusButton;
    [SerializeField] private Text archerMinusButtonLabel;
    //[SerializeField] private Button artilleryMinusButton;
    [SerializeField] private Text artilleryMinusButtonLabel;
    //[SerializeField] private Button knightMinusButton;
    [SerializeField] private Text knightMinusButtonLabel;
    //[SerializeField] private Button bikerMinusButton;
    [SerializeField] private Text bikerMinusButtonLabel;

    [SerializeField] private Button pawnPlusButton;
    [SerializeField] private Text pawnPlusButtonLabel;
    [SerializeField] private Button archerPlusButton;
    [SerializeField] private Text archerPlusButtonLabel;
    [SerializeField] private Button artilleryPlusButton;
    [SerializeField] private Text artilleryPlusButtonLabel;
    [SerializeField] private Button knightPlusButton;
    [SerializeField] private Text knightPlusButtonLabel;
    [SerializeField] private Button bikerPlusButton;
    [SerializeField] private Text bikerPlusButtonLabel;

    [SerializeField] private Button armyBuilderSaveButton;
    [SerializeField] private Text armyBuilderSaveButtonLabel;
    [SerializeField] private Button armyBuilderSaveAsButton;
    [SerializeField] private Text armyBuilderSaveAsButtonLabel;

    [SerializeField] private InputField _armyNameField;
    [SerializeField] private ArmyListHolder _armyGridPanel;

    [SerializeField] private InputField armyBuilderPointMaxField;
    [SerializeField] private InputField gameSetupPointMaxField;
    [SerializeField] private Text armyBuilderPointTotalText;

#region public methods

    public void BackButtonPressed()
    {
        var lastMenu = menuHistory.Pop();
        if(lastMenu == MenuWindows.MAIN)
        {
            //Application.Quit();
            menuHistory.Push(MenuWindows.MAIN);
            ClickedQuitButton();
        }
        else
        {
            HideWindow(lastMenu);
            ShowWindow(menuHistory.Peek());
        }
    }

    public void ClickedUnimplementedButton(string buttonName)
    {
        Debug.Log(buttonName + " not yet implemented.");
    }

    public void ClickedLocalMultiplayerButton()
    {
        HideWindow(menuHistory.Peek());
        menuHistory.Push(MenuWindows.LOCAL_MULTIPLAYER);
        ShowWindow(MenuWindows.LOCAL_MULTIPLAYER);
        ResetGameSetup();
    }

    public void ClickedMobilePlayButton()
    {
        HideWindow(menuHistory.Peek());
        menuHistory.Push(MenuWindows.MOBILE_PLAY);
        ShowWindow(MenuWindows.MOBILE_PLAY);
    }

    public void ClickedMobileCreateButton()
    {
        HideWindow(menuHistory.Peek());
        menuHistory.Push(MenuWindows.MOBILE_CREATE);
        ShowWindow(MenuWindows.MOBILE_CREATE);
    }

    public void ClickedLoadGameButton()
    {
        //openFileDialog.GetComponent<UIOpenFileDialog>().PopulateTextField("");
        openFileDialog.GetComponent<UIOpenFileDialog>().loadingCallback = (string fileName) =>
        {
            //verify file
            if (!FileManager.FileExtensionIs(fileName, CombatManager.SAVE_FILE_EXTENSION))
            {
                fileName += CombatManager.SAVE_FILE_EXTENSION;
            }
            if (FileManager.singleton.FileExists(CombatManager.SAVE_DIRECTORY + fileName))
            {
                var saveGame = FileManager.singleton.Load<SavedGame>(CombatManager.SAVE_DIRECTORY + fileName);
                if (saveGame == null)
                {
                    EventManager.singleton.ShowErrorPopup("File read error", "OK", new UnityAction(() =>
                    {
                        EventManager.singleton.ReturnFocus();
                    }
                    ), KeyCode.O);
                    //Debug.LogError("[MainMenu:ClickedLoadGameButton] Bad game file format!");
                }
                else
                {
                    GameLoader.singleton.gameSave = saveGame;
                    GameLoader.singleton.startUpMode = GameLoader.StartUpMode.LOAD_GAME;
                    SceneManager.LoadScene("Combat");
                }
            }
            else
            {
                EventManager.singleton.ShowErrorPopup("File not found", "OK", new UnityAction(() =>
                {
                    EventManager.singleton.ReturnFocus();
                }
                ), KeyCode.O);
                //Debug.LogError("[MainMenu:ClickedLoadGameButton] Couldn't find game file!");
            }
            //if file verifies, pass the game loader the file name
            //then:
            //else, throw errors
        };
        
        EventManager.singleton.GrantFocus(openFileDialog.GetComponent<ModalPopup>());
        openFileDialog.GetComponent<UIOpenFileDialog>().PopulateFileInfo(CombatManager.SAVE_DIRECTORY, CombatManager.SAVE_FILE_EXTENSION);
    }

    public void ClickedStartGameButton()
    {
        //TODO: this will need to know what game mode context it is starting.
        if (MapManager.singleton.currentFileName == "")
        {
            Debug.LogError("[MainMenu:ClickedStartGameButton] Can't start a game without a map!");
        }
        else
        {
            GameLoader.singleton.startUpMode = GameLoader.StartUpMode.NEW_GAME;
            SceneManager.LoadScene("Combat");
        }
    }

    public void ClickedOptionsButton()
    {
        HideWindow(menuHistory.Peek());
        menuHistory.Push(MenuWindows.OPTIONS);
        ShowWindow(MenuWindows.OPTIONS);
        PopulateOptionsMenu();
    }

    public void ClickedQuitButton()
    {
        EventManager.singleton.ShowDynamicPopup("Are you sure you want to quit?", "Yes", new UnityAction(() =>
        {
            Debug.Log("Quitting game! [Application.Quit() ignored in the editor]");
            Application.Quit();
            EventManager.singleton.ReturnFocus();
        }), KeyCode.Y, "No", new UnityAction(() =>
        {
            EventManager.singleton.ReturnFocus();
        }), KeyCode.N, 1, 1);
    }

    public void ClickedArmyBuilderButton()
    {
        HideWindow(menuHistory.Peek());
        menuHistory.Push(MenuWindows.ARMY_BUILDER);
        ShowWindow(MenuWindows.ARMY_BUILDER);
        ResetArmyBuilder();
        ResetArmyPointMaxInBuilder();
    }

    public void ClickedMapEditorButton()
    {
        SceneManager.LoadScene("MapEditor");
    }

    public void ShowOpenMapDialog()
    {
        openFileDialog.GetComponent<UIOpenFileDialog>().loadingCallback = (string fileName) =>
        {
            if (MapManager.singleton.LoadMap(fileName))
            {
                MapManager.singleton.HideTiles();
                var previewTexture = MapManager.singleton.MapPreview();
                mapPreview.sprite = Sprite.Create(previewTexture, new Rect(0, 0, previewTexture.width, previewTexture.height), Vector2.one * 0.5f);
                gameSetupMapReady = true;
                mapTitle.text = MapManager.singleton.mapName;
                gameSetupPointMaxField.text = MapManager.singleton.armyPointLimit.ToString();
                SetStartGameButtonEnabled();
            }
            else
            {
                Debug.LogError("[MainMenu:ShowOpenMapDialog] LoadMap returned false");
                //TODO: Give the user some sort of error!?
            }
        };
        
        EventManager.singleton.GrantFocus(openFileDialog.GetComponent<ModalPopup>());
        openFileDialog.GetComponent<UIOpenFileDialog>().PopulateFileInfo(MapManager.PICTURE_DIRECTORY, MapManager.PICTURE_FILE_EXTENSION);
    }

    public void ShowOpenRedArmyDialog()
    {
        ShowOpenArmyDialog(CombatManager.Faction.RED);
    }

    public void ShowOpenBlueArmyDialog()
    {
        ShowOpenArmyDialog(CombatManager.Faction.BLUE);
    }

    public void ShowOpenArmyDialog(CombatManager.Faction faction)
    {
        openFileDialog.GetComponent<UIOpenFileDialog>().loadingCallback = (string fileName) =>
        {
            //load army into red army slot
            if (!ArmyManager.singleton.LoadArmy(fileName, faction))
            {
                // the army didn't load right. blow up!
            }
            else
            {
                //populate army builder UI
                switch (menuHistory.Peek())
                {
                    case MenuWindows.LOCAL_MULTIPLAYER:
                        CheckArmyErrorPopup(faction);
                        break;
                    case MenuWindows.ARMY_BUILDER:
                        ResetBuilderToCurArmy();
                        break;
                    default:
                        break;
                }
            }
        };

        EventManager.singleton.GrantFocus(openFileDialog.GetComponent<ModalPopup>());
        openFileDialog.GetComponent<UIOpenFileDialog>().PopulateFileInfo(ArmyManager.ARMY_DIRECTORY, ArmyManager.ARMY_FILE_EXTENSION);
    }

    public void RemoveUnitButton(ArmyData.UnitType unit)
    {
        SetUnitCountInBuilder(unit, RemoveUnit(unit));
    }

    public void AddPawnButton() 
    {
        SetUnitCountInBuilder(ArmyData.UnitType.PAWN, AddUnit(ArmyData.UnitType.PAWN));
    }

    public void RemovePawnButton()
    {
        SetUnitCountInBuilder(ArmyData.UnitType.PAWN, RemoveUnit(ArmyData.UnitType.PAWN));
    }

    public void AddKnightButton()
    {
        SetUnitCountInBuilder(ArmyData.UnitType.KNIGHT, AddUnit(ArmyData.UnitType.KNIGHT));
    }

    public void RemoveKnightButton()
    {
        SetUnitCountInBuilder(ArmyData.UnitType.KNIGHT, RemoveUnit(ArmyData.UnitType.KNIGHT));
    }

    public void AddArcherButton()
    {
        SetUnitCountInBuilder(ArmyData.UnitType.ARCHER, AddUnit(ArmyData.UnitType.ARCHER));
    }

    public void RemoveArcherButton()
    {
        SetUnitCountInBuilder(ArmyData.UnitType.ARCHER, RemoveUnit(ArmyData.UnitType.ARCHER));
    }

    public void AddArtilleryButton()
    {
        SetUnitCountInBuilder(ArmyData.UnitType.ARTILLERY, AddUnit(ArmyData.UnitType.ARTILLERY));
    }

    public void RemoveArtilleryButton()
    {
        SetUnitCountInBuilder(ArmyData.UnitType.ARTILLERY, RemoveUnit(ArmyData.UnitType.ARTILLERY));
    }

    public void AddBikerButton()
    {
        SetUnitCountInBuilder(ArmyData.UnitType.BIKER, AddUnit(ArmyData.UnitType.BIKER));
    }

    public void RemoveBikerButton()
    {
        SetUnitCountInBuilder(ArmyData.UnitType.BIKER, RemoveUnit(ArmyData.UnitType.BIKER));
    }

    public void ArmyNameFieldChanged(string value)
    {
        Debug.Log("[MainMenu:ArmyNameFieldChanged] value is " + _armyNameField.text);
        ArmyManager.singleton.SetNameInBuilder(_armyNameField.text);
        CheckForLegalArmy();
    }

    public void ArmyNameInputCompleted()
    {
        Debug.Log("[MainMenu:ArmyNameInputCompleted] Input Ended!");
    }

    public void PressedArmySaveButton()
    {
        if (ArmyManager.singleton.currentFileName != "")
        {
            ArmyManager.singleton.SaveArmy(ArmyManager.singleton.currentFileName);
        }
        else
        {
            //launch the save as dialog
            ShowSaveAsDialog();
        }
    }

    public void PressedArmySaveAsButton()
    {
        ShowSaveAsDialog();
    }

    public void PressedArmyBuilderOpenButton()
    {
        UnityAction action = new UnityAction(() => 
        {
            ShowOpenArmyDialog(CombatManager.Faction.RED);
        });
        AskForSaveBefore(action);
    }

    public void PressedArmyNewButton()
    {
        AskForSaveBefore(new UnityAction(() =>
        {
            ResetArmyBuilder();
        }));
    }

    public void ShowSaveAsDialog() 
    {
        saveAsFileDialog.GetComponent<UISaveAsDialog>().saveAction = (string filename) =>
            {
                ArmyManager.singleton.SaveArmy(filename);
            };
        EventManager.singleton.GrantFocus(saveAsFileDialog.GetComponent<ModalPopup>());
        if (ArmyManager.singleton.currentFileName == "")
        {
            saveAsFileDialog.GetComponent<UISaveAsDialog>().PopulateFileInfo(ArmyManager.ARMY_DIRECTORY, ArmyManager.ARMY_FILE_EXTENSION, ArmyManager.singleton.GetArmyName(CombatManager.Faction.RED));
        }
        else
        {
            saveAsFileDialog.GetComponent<UISaveAsDialog>().PopulateFileInfo(ArmyManager.ARMY_DIRECTORY, ArmyManager.ARMY_FILE_EXTENSION, ArmyManager.singleton.currentFileName);
        }
    }

    public void ShowSaveAsDialog(UnityAction callback)
    {
        ShowSaveAsDialog();

        saveAsFileDialog.GetComponent<UISaveAsDialog>().confirmedSaveCallback = callback;

        //saveAsSaveButton.onClick.AddListener(action);
    }

    public void CloseArmyBuilder()
    {
        UnityAction action = new UnityAction(() =>
        {
            BackButtonPressed();
        });
        AskForSaveBefore(action);
    }

    public void OnSetupMaxPointInputFieldChanged(string value)
    {
        UpdateArmyPointMaxInSetup();
    }

    public void OnSetupMaxPointInputCompleted()
    {
        Debug.Log("[MainMenu:OnMaxPointInputCompleted] Input Ended!");
        //#if UNITY_STANDALONE
        //        if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
        //        {
        //            ConfirmOpen();
        //        }
        //#endif
    }

    public void OnBuilderMaxPointInputFieldChanged(string value)
    {
        UpdateArmyPointMaxInBuilder();
    }

    public void OnBuilderMaxPointInputCompleted()
    {
        Debug.Log("[MainMenu:OnMaxPointInputCompleted] Input Ended!");
//#if UNITY_STANDALONE
//        if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
//        {
//            ConfirmOpen();
//        }
//#endif
    }

#if UNITY_STANDALONE

    public void KeyboardUpdate()
    {
        if (Input.GetKeyDown(EventManager.singleton.shortcutMenuCancel))
        {
            BackButtonPressed();
        }
    }

#endif
#if UNITY_IOS || UNITY_ANDROID
    public void TouchScreenUpdate()
    {
        var touches = Input.touches;
        if (touches.Length == 1)
        {
            //panning or clicking on tiles
        }
        else if (touches.Length == 2)
        {
            //zooming or something fancy
        }
    }
#endif
#endregion

#region private methods

    private void AskForSaveBefore(UnityAction action)
    {
        // Pop up a confirmation dialog to ask the user if they want
        // to save their changes.  They have 3 options, "Yes", "No" and "Cancel".
        if (ArmyManager.singleton.hasChanged)
        {
            EventManager.singleton.ShowDynamicPopup("You have unsaved changes. Save?",
            "Yes", new UnityAction(() =>
            {
                // Save changes
                EventManager.singleton.ReturnFocus();
                SaveAction(action);
                // Do new action
            }), KeyCode.Y,
            "No", new UnityAction(() =>
            {
                // Do new action
                EventManager.singleton.ReturnFocus();
                action();
            }), KeyCode.N,
            "Cancel", new UnityAction(() =>
            {
                // Abort new action
                EventManager.singleton.ReturnFocus();
            }), KeyCode.C, 3, 3);
        }
        else
        {
            action();
        }
    }

    private void SaveAction(UnityAction action)
    {
        if (ArmyManager.singleton.currentFileName != "")
        {
            ArmyManager.singleton.SaveArmy(ArmyManager.singleton.currentFileName);
            action();
        }
        else
        {
            //launch the save as dialog
            ShowSaveAsDialog(action);
        }
    }

    private void HideWindow(MenuWindows eWindow)
    {
        SetWindowActive(eWindow, false);
    }

    private void ShowWindow(MenuWindows eWindow)
    {
        SetWindowActive(eWindow, true);
    }

    private void SetWindowActive(MenuWindows eWindow, bool active)
    {
        switch (eWindow)
        {
            case MenuWindows.OPTIONS:
                optionsPanel.SetActive(active);
                break;
            case MenuWindows.MAIN:
                mainMenuPanel.SetActive(active);
                titleText.SetActive(active);
                break;
            case MenuWindows.MOBILE_PLAY:
                mobilePlayPanel.SetActive(active);
                titleText.SetActive(active);
                break;
            case MenuWindows.MOBILE_CREATE:
                mobileCreatePanel.SetActive(active);
                titleText.SetActive(active);
                break;
            case MenuWindows.LOCAL_MULTIPLAYER:
                gameSetupPanel.SetActive(active);
                break;
            case MenuWindows.ARMY_BUILDER:
                armyBuilderPanel.SetActive(active);
                break;
            case MenuWindows.NONE:
            default:
                //throw new System.Exception();
                break;
        }
    }

    private void InitializeMenu()
    {
        menuHistory = new Stack<MenuWindows>();
        menuHistory.Push(MenuWindows.MAIN);
    }

    //private Button GetMinusButtonForType(ArmyData.UnitType type)
    //{
    //    switch (type)
    //    {
    //        case ArmyData.UnitType.ARCHER:
    //            return archerMinusButton;
    //        case ArmyData.UnitType.ARTILLERY:
    //            return artilleryMinusButton;
    //        case ArmyData.UnitType.BIKER:
    //            return bikerMinusButton;
    //        case ArmyData.UnitType.KNIGHT:
    //            return knightMinusButton;
    //        case ArmyData.UnitType.PAWN:
    //            return pawnMinusButton;
    //    }
    //    Debug.LogError("[MainMenu:GetMinusButtonForType] Invalid type!");
    //    return null;
    //}

    //private Text GetMinusButtonLabelForType(ArmyData.UnitType type)
    //{
    //    switch (type)
    //    {
    //        case ArmyData.UnitType.ARCHER:
    //            return archerMinusButtonLabel;
    //        case ArmyData.UnitType.ARTILLERY:
    //            return artilleryMinusButtonLabel;
    //        case ArmyData.UnitType.BIKER:
    //            return bikerMinusButtonLabel;
    //        case ArmyData.UnitType.KNIGHT:
    //            return knightMinusButtonLabel;
    //        case ArmyData.UnitType.PAWN:
    //            return pawnMinusButtonLabel;
    //    }
    //    Debug.LogError("[MainMenu:GetMinusButtonLabelForType] Invalid type!");
    //    return null;
    //}

    //private void DisableMinusButton(ArmyData.UnitType type)
    //{
    //    DisableButton(GetMinusButtonForType(type), GetMinusButtonLabelForType(type));
    //}

    //private void EnableMinusButton(ArmyData.UnitType type)
    //{
    //    EnableButton(GetMinusButtonForType(type), GetMinusButtonLabelForType(type));
    //}

    private Button GetPlusButtonForType(ArmyData.UnitType type)
    {
        switch (type)
        {
            case ArmyData.UnitType.ARCHER:
                return archerPlusButton;
            case ArmyData.UnitType.ARTILLERY:
                return artilleryPlusButton;
            case ArmyData.UnitType.BIKER:
                return bikerPlusButton;
            case ArmyData.UnitType.KNIGHT:
                return knightPlusButton;
            case ArmyData.UnitType.PAWN:
                return pawnPlusButton;
        }
        Debug.LogError("[MainMenu:GetPlusButtonForType] Invalid type!");
        return null;
    }

    private Text GetPlusButtonLabelForType(ArmyData.UnitType type)
    {
        switch (type)
        {
            case ArmyData.UnitType.ARCHER:
                return archerPlusButtonLabel;
            case ArmyData.UnitType.ARTILLERY:
                return artilleryPlusButtonLabel;
            case ArmyData.UnitType.BIKER:
                return bikerPlusButtonLabel;
            case ArmyData.UnitType.KNIGHT:
                return knightPlusButtonLabel;
            case ArmyData.UnitType.PAWN:
                return pawnPlusButtonLabel;
        }
        Debug.LogError("[MainMenu:GetPlusButtonLabelForType] Invalid type!");
        return null;
    }

    private void DisablePlusButton(ArmyData.UnitType type)
    {
        DisableButton(GetPlusButtonForType(type), GetPlusButtonLabelForType(type));
    }

    private void EnablePlusButton(ArmyData.UnitType type)
    {
        EnableButton(GetPlusButtonForType(type), GetPlusButtonLabelForType(type));
    }

    private void DisableButton(Button button, Text text)
    {
        button.interactable = false;
        if(text != null)
        {
            text.color = Color.grey;
        }
    }

    

    private void EnableButton(Button button, Text text)
    {
        button.interactable = true;
        if (text != null)
        {
            text.color = Color.black;
        }
    }

    private void ResetArmyBuilder()
    {
        ArmyManager.singleton.NewArmy();
        ResetBuilderToCurArmy();
    }

    private void ResetBuilderToCurArmy()
    {
        ArmyManager.singleton.SetBattlemageInBuilder(ArmyData.UnitType.BATTLEMAGE_LUMP);
        foreach (ArmyData.UnitType type in Enum.GetValues(typeof(ArmyData.UnitType)))
        {
            if(type != ArmyData.UnitType.NONE && type < ArmyData.FIRST_BATTLEMAGE)
            {
                SetUnitCountInBuilder(type, ArmyManager.singleton.GetUnitCount(type, CombatManager.Faction.RED));
            }
        }
        _armyNameField.text = ArmyManager.singleton.GetArmyName(CombatManager.Faction.RED);
    }

    private void CheckArmiesReady()
    {
        gameSetupArmiesReady = GameLoader.singleton.CheckArmiesReady();
    }

    private ErrorCode UpdateArmySetupUI(CombatManager.Faction faction, Text text)
    {
        string name = ArmyManager.singleton.GetArmyName(faction);
        text.color = Color.black;

        if (name != null)   
        {
            int cost = ArmyManager.singleton.GetTotalPointCost(faction);

            text.text = name + " (" + cost + ")";
            if (cost > GameLoader.singleton.currentArmyPointMax)
            {
                text.color = Color.red;
                return ErrorCode.ARMY_TOO_EXPENSIVE;
            }
        }
        else
        {
            text.text = "Not Ready";
            return ErrorCode.ARMY_NOT_LOADED;
        }
        return ErrorCode.NONE;
    }

    private void UpdateArmySetup()
    {
        CheckArmiesReady();
        UpdateArmySetupUI(CombatManager.Faction.RED, redArmyName);
        UpdateArmySetupUI(CombatManager.Faction.BLUE, blueArmyName);
        
        SetStartGameButtonEnabled();
    }

    private void CheckArmyErrorPopup(CombatManager.Faction faction)
    {
        Text nameText;
        switch(faction)
        {
            case CombatManager.Faction.RED:
                nameText = redArmyName;
                break;
            case CombatManager.Faction.BLUE:
                nameText = blueArmyName;
                break;
            default:
                Debug.LogError("[MainMenu:CheckArmyErrorPopup] Invalid Faction");
                return;
        }

        CheckArmiesReady();
        if(UpdateArmySetupUI(faction, nameText) == ErrorCode.ARMY_TOO_EXPENSIVE)
        {
            EventManager.singleton.ShowErrorPopup("The selected army's cost is too high", "I'm Sorry", () =>
            {
                EventManager.singleton.ReturnFocus();
            },
        KeyCode.O);
        }
        SetStartGameButtonEnabled();
    }

    private void SetStartGameButtonEnabled()
    {
        if(gameSetupMapReady && gameSetupArmiesReady)
            EnableButton(startButton, startButtonLabel);
        else
            DisableButton(startButton, startButtonLabel);
    }

    private int AddUnit(ArmyData.UnitType unitType)
    {
        var count = ArmyManager.singleton.AddUnitInBuilder(unitType);
        return count;
    }

    private int RemoveUnit(ArmyData.UnitType unitType)
    {
        var count = ArmyManager.singleton.RemoveUnitInBuilder(unitType);
        return count;
    }

    private void SetUnitCountInBuilder(ArmyData.UnitType unitType, int newCount)
    {
        Debug.Log("[MainMenu:SetUnitCountInBuilder] Setting " + unitType + " count to " + newCount);

        //if (_armyGridPanel.ContainsUnit(unitType))
        //{
        //    if (newCount == 0)
        //    {
        //        DisableMinusButton(unitType);
        //    }
        //}
        //else
        //{
        //    if (newCount != 0)
        //    {
        //        EnableMinusButton(unitType);
        //    }
        //    else
        //    {
        //        DisableMinusButton(unitType);
        //    }
        //}
        _armyGridPanel.SetUnitCount(unitType, newCount, true);
        armyBuilderPointTotalText.text = "Point Total: " + ArmyManager.singleton.GetTotalPointCost(CombatManager.Faction.RED) +"/";
        UpdateArmyBuilderUnitAddButtons();
        CheckForLegalArmy();
    }

    private void ResetArmyPointMaxInBuilder()
    {
        armyBuilderPointMaxField.text = DEFAULT_ARMY_POINT_MAX.ToString();
        //UpdateArmyPointMaxInBuilder();
    }

    private void ResetArmyPointMaxInSetup()
    {
        gameSetupPointMaxField.text = DEFAULT_ARMY_POINT_MAX.ToString();
        //UpdateArmyPointMaxInSetup();
    }

    private void UpdateArmyPointMaxValue(InputField field) 
    {
        if (field.text.StartsWith("-"))
        {
            field.text = field.text.Substring(1);
            field.caretPosition--;
        }
        if (field.text == "")
        {
            GameLoader.singleton.currentArmyPointMax = 0;
        }
        else
        {
            GameLoader.singleton.currentArmyPointMax = uint.Parse(field.text);
        }
        Debug.Log("[MainMenu:UpdateArmyPointMax] New point max: " + GameLoader.singleton.currentArmyPointMax);
    }

    private void UpdateArmyPointMaxInBuilder()
    {
        UpdateArmyPointMaxValue(armyBuilderPointMaxField);
        UpdateArmyBuilderUnitAddButtons();
    }

    private void UpdateArmyPointMaxInSetup()
    {
        UpdateArmyPointMaxValue(gameSetupPointMaxField);
        UpdateArmySetup();
    }

    private void UpdateArmyBuilderUnitAddButtons() 
    {
        long pointBalance = GameLoader.singleton.currentArmyPointMax - ArmyManager.singleton.GetTotalPointCost(CombatManager.Faction.RED);

        foreach (ArmyData.UnitType type in Enum.GetValues(typeof(ArmyData.UnitType)))
        {
            if (type != ArmyData.UnitType.NONE && type < ArmyData.FIRST_BATTLEMAGE)
            {
                if (pointBalance >= ArmyData.PointCost(type))
                {
                    EnablePlusButton(type);
                }
                else
                {
                    DisablePlusButton(type);
                }
            }
        }
    }

    private void CheckForLegalArmy()
    {

        string nameCheck = ArmyManager.singleton.GetArmyName(CombatManager.Faction.RED);
        int armyCount = ArmyManager.singleton.GetTotalUnitCount(CombatManager.Faction.RED);
        if (nameCheck != null && nameCheck != "" && armyCount > 0)
        {
            EnableButton(armyBuilderSaveButton, armyBuilderSaveButtonLabel);
            EnableButton(armyBuilderSaveAsButton, armyBuilderSaveAsButtonLabel);
        }

        else
        {
            DisableButton(armyBuilderSaveButton, armyBuilderSaveButtonLabel);
            DisableButton(armyBuilderSaveAsButton, armyBuilderSaveAsButtonLabel);
        }

    }

    //private Sprite GetUnitSprite(ArmyData.UnitType unitType)
    //{
    //    switch(unitType)
    //    {
    //        case ArmyData.UnitType.PAWN:
    //            return pawnSprite;
    //        case ArmyData.UnitType.KNIGHT:
    //            return knightSprite;
    //        case ArmyData.UnitType.ARTILLERY:
    //            return archerSprite;
    //        case ArmyData.UnitType.BIKER:
    //            return bikerSprite;
    //        default:
    //            return null;
    //    }
    //}

    private void ResetGameSetup()
    {
        gameSetupMapReady = false;
        mapTitle.text = "Not Ready";
        mapTitle.color = Color.black;
        mapPreview.sprite = unknownTexture;

        ArmyManager.singleton.ClearArmies();
        ResetArmyPointMaxInSetup();
        UpdateArmySetup();
    }

    private void PopulateOptionsMenu()
    {
        optionsMenuPanel.PopulateFields();
    }



#endregion

#region monobehaviors

    // Use this for initialization
    void Awake()
    {
        Debug.Log("[MainMenu:Awake]");
        if (singleton == null)
        {
            Debug.Log("Main Menu checking in!");
            singleton = this;
            InitializeMenu();
        }
        else
        {
            Debug.Log("Main Menu checking out!");
            GameObject.Destroy(gameObject);
        }
    }

    void Start()
    {
        EventManager.singleton.ResetControlFocus(this);
        Debug.Log("[MainMenu:Start]");
        MapManager.singleton.mapContext = MapManager.MapContext.MAIN_MENU;
        FileManager.singleton.CreateLogFile();//This might belong in awake
    }

    // Update is called once per frame
    void Update ()
    {

    }

#endregion
}
