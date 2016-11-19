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
    public GameObject mobilePlayPanel;
    public GameObject mobileCreatePanel;

    public int numPlayers = 2;


    [SerializeField] private GameObject openFileDialog;
    [SerializeField] private GameObject saveAsFileDialog;
    [SerializeField] private OptionsMenuPanel optionsMenuPanel;
    [SerializeField] private Button startButton;
    [SerializeField] private Text startButtonLabel;
    [SerializeField] private Sprite unknownTexture;

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


    public void ClickedMapEditorButton()
    {
        SceneManager.LoadScene("MapEditor");
    }

    //public void ShowOpenMapDialog()
    //{
    //    openFileDialog.GetComponent<UIOpenFileDialog>().loadingCallback = (string fileName) =>
    //    {
    //        if (MapManager.singleton.LoadMap(fileName))
    //        {
    //            MapManager.singleton.HideTiles();
    //            var previewTexture = MapManager.singleton.MapPreview();
    //            mapPreview.sprite = Sprite.Create(previewTexture, new Rect(0, 0, previewTexture.width, previewTexture.height), Vector2.one * 0.5f);
    //            gameSetupMapReady = true;
    //            mapTitle.text = MapManager.singleton.mapName;
    //            gameSetupPointMaxField.text = MapManager.singleton.armyPointLimit.ToString();
    //            SetStartGameButtonEnabled();
    //        }
    //        else
    //        {
    //            Debug.LogError("[MainMenu:ShowOpenMapDialog] LoadMap returned false");
    //            //TODO: Give the user some sort of error!?
    //        }
    //    };
        
    //    EventManager.singleton.GrantFocus(openFileDialog.GetComponent<ModalPopup>());
    //    openFileDialog.GetComponent<UIOpenFileDialog>().PopulateFileInfo(MapManager.PICTURE_DIRECTORY, MapManager.PICTURE_FILE_EXTENSION);
    //}








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
