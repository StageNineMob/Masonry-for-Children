using UnityEngine;
using System.Collections;
using StageNine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MapEditorManager : MonoBehaviour, IModalFocusHolder
{
    //public data
    public static MapEditorManager singleton;
    public enum Swatch { button0, button1, button2, button3, button4, button5 }
    //private data
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject saveAsDialog;
    [SerializeField] private GameObject optionsDialog;
    //[SerializeField] private Button saveAsSaveButton;
    [SerializeField] private GameObject openFileDialog;
    [SerializeField] private GameObject terrainBrushesPanel;
    [SerializeField] private GameObject symmetryModePanel;
    [SerializeField] private Text symmetryModeButtonText;
    [SerializeField] private InputField mapNameInputField;
    [SerializeField] private InputField armyPointLimitInputField;
    [SerializeField] private ModalPopup mapPropertiesPopup;
    [SerializeField] private Button mapPropertiesConfirmButton;
    private uint tempPointLimit;
    [SerializeField] private GameObject checkMark;
    [SerializeField] private GameObject panToolButton;
    [SerializeField] private GameObject UIPanel;

    private UnityAction confirmPropertiesCallback;

    [SerializeField] private GameObject terrainBrushesButton;
    [SerializeField] private GameObject swatchButton0;
    [SerializeField] private GameObject swatchButton1;
    [SerializeField] private GameObject swatchButton2;
    [SerializeField] private GameObject swatchButton3;
    [SerializeField] private GameObject swatchButton4;
    [SerializeField] private GameObject swatchButton5;
    private GameObject[] swatchButtons;

    private Color[] swatchColors;

    public Color currentBrushColor;

    #region public methods
#if UNITY_STANDALONE

    public void KeyboardUpdate()
    {
        var mouseWheelAxis = Input.GetAxis("Mouse ScrollWheel");
        if (mouseWheelAxis != 0)
        {
            MapManager.singleton.CameraZoom(mouseWheelAxis, Input.mousePosition);
        }

        //Camera Zooming Segment
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
            Debug.Log("[MapEditorManager:KeyboardUpdate] Keyboard Panning");

            MapManager.singleton.CameraPan(panSum * EventManager.singleton.pixelPanSpeed);
        }

        if(Input.GetKeyDown(EventManager.singleton.shortcutOpenMenu))
        {
            ShowPauseMenu();
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
        CloseSubPanels();
        SetBrushPanelVisibility(false);
        EventManager.singleton.GrantFocus(pauseMenu.GetComponent<ModalPopup>());
    }

    public void InitSwatchColors()
    {
        if(swatchColors.Length != swatchButtons.Length)
        {
            Debug.LogError("[MapEditorManager:InitSwatchColors] swatchColors.Length != swatchButtons.Length");
        }
        swatchColors[0] = Color.red;
        swatchColors[1] = Color.magenta;
        swatchColors[2] = Color.blue;
        swatchColors[3] = Color.cyan;
        swatchColors[4] = Color.green;
        swatchColors[5] = Color.yellow;

        swatchButtons[0] = swatchButton0;
        swatchButtons[1] = swatchButton1;
        swatchButtons[2] = swatchButton2;
        swatchButtons[3] = swatchButton3;
        swatchButtons[4] = swatchButton4;
        swatchButtons[5] = swatchButton5;

        int buttonCount = swatchButtons.Length;
        for (int ii = 0; ii < buttonCount; ++ii)
        {
            swatchButtons[ii].GetComponent<Image>().color = swatchColors[ii];
        }
    }

    public void SetBrushPanelVisibility(bool visible)
    {
        UIPanel.SetActive(visible);
    }
    public void ShowOptionsDialog()
    {
        //TODO: implement it;
        Debug.Log("[MapEditorManager:ShowOptionsDialog]");
    }

    public void ShowSaveAsDialog()
    {
        saveAsDialog.GetComponent<UISaveAsDialog>().saveAction = (string fileName) =>
        {
            SaveMapAs(fileName);
        };
        EventManager.singleton.GrantFocus(saveAsDialog.GetComponent<ModalPopup>());
        if (MapManager.singleton.currentFileName == "")
        {
            saveAsDialog.GetComponent<UISaveAsDialog>().PopulateFileInfo(MapManager.PICTURE_DIRECTORY, MapManager.PICTURE_FILE_EXTENSION, MapManager.singleton.mapName);
        }
        else
        {
            saveAsDialog.GetComponent<UISaveAsDialog>().PopulateFileInfo(MapManager.PICTURE_DIRECTORY, MapManager.PICTURE_FILE_EXTENSION, MapManager.singleton.currentFileName);
        }
    }

    public void ShowSaveAsDialog(UnityAction callback)
    {
        ShowSaveAsDialog();

        saveAsDialog.GetComponent<UISaveAsDialog>().confirmedSaveCallback = callback;

        //saveAsSaveButton.onClick.AddListener(action);
    }

    public void ShowOpenFileDialog()
    {
        openFileDialog.GetComponent<UIOpenFileDialog>().loadingCallback = (string fileName) =>
        {
            LoadMap(fileName);
        };

        EventManager.singleton.GrantFocus(openFileDialog.GetComponent<ModalPopup>());
        openFileDialog.GetComponent<UIOpenFileDialog>().PopulateFileInfo(MapManager.PICTURE_DIRECTORY, MapManager.PICTURE_FILE_EXTENSION);
    }

    //public void CheckForForceMapProperties(UnityAction callback)
    //{
    //    if (MapManager.singleton.mapName == "" || MapManager.singleton.armyPointLimit == 0)
    //    {
    //        ShowMapPropertiesPopup(callback);
    //    }
    //    else
    //        callback();
    //}

    public void SaveMapAs(string fileName)
    {
        Debug.Log("[MapEditorManager:SaveMapAs]");
        MapManager.singleton.SaveCurrentMapAs(fileName);
    }

    public void LoadMap(string fileName)
    {
        Debug.Log("[MapEditorManager:LoadMap]");
        if (MapManager.singleton.LoadMap(fileName)) 
        {
            // Things that need to happen only when a map is successfully loaded go here
            SetBrushPanelVisibility(true);
            EventManager.singleton.ReturnFocus();
            ResetToolSelection();
        }
    }

    public void RequestNewMap()
    {
        MapManager.singleton.CreateNewMap();
        ResetToolSelection();
    }

    public void ResetToolSelection()
    {
        WorldInterfaceLayer.singleton.SetCameraMode();
        MoveCheckMark(panToolButton);
        SetSymmetryMode(MapManager.SymmetrySetting.NONE);
    }

    public void SetTerrainBrushesPanelActive(bool visibility)
    {
        terrainBrushesPanel.SetActive(visibility);
    }

    public void PressedTerrainBrushesButton()
    {
        SetTerrainBrushesPanelActive(true);
        SetSymmetryModePanelActive(false);
    }

    public void SetSymmetryModePanelActive(bool visiblity)
    {
        symmetryModePanel.SetActive(visiblity);
    }

    public void PressedSymmetryModeButton()
    {
        SetSymmetryModePanelActive(!symmetryModePanel.activeSelf);
        SetTerrainBrushesPanelActive(false);
    }

    public void PressedSwatchButton(int button )
    {
        currentBrushColor = swatchColors[(int)button];
        terrainBrushesButton.GetComponent<Image>().color = currentBrushColor;
        MapManager.singleton.SelectSwatch1Brush();
        SetTerrainBrushesPanelActive(false);
    }

    public void PressedSwatch1Button()
    {
        MapManager.singleton.SelectSwatch1Brush();
        SetTerrainBrushesPanelActive(false);
    }

    public void PressedEraserBrushButton()
    {
        MapManager.singleton.SelectEraserBrush();
        CloseSubPanels();
    }

    //public void MapNameFieldChanged(string blank)
    //{
    //    ValidateMapProperties();
    //}

    public void MapNameFieldInputCompleted()
    {
        Debug.Log("[MapEditorManager:MapNameFieldInputCompleted] Input Ended!");
#if UNITY_STANDALONE
        if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
        {
            MapPropertiesPressedEnterKey();
        }
#endif
    }

    public void ArmyLimitFieldChanged(string blank)
    {
        if (armyPointLimitInputField.text.StartsWith("-"))
        {
            armyPointLimitInputField.text = armyPointLimitInputField.text.Substring(1);
            armyPointLimitInputField.caretPosition--;
        }
        if (armyPointLimitInputField.text == "")
        {
            tempPointLimit = 0;
        }
        else
        {
            tempPointLimit = uint.Parse(armyPointLimitInputField.text);
        }
        Debug.Log("[MapEditorManager:ArmyLimitFieldChanged] New point max: " + tempPointLimit);

        //ValidateMapProperties();
    }

    public void ArmyLimitFieldInputCompleted()
    {
        Debug.Log("[MapEditorManager:MapNameFieldInputCompleted] Input Ended!");
#if UNITY_STANDALONE
        if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter))
        {
            MapPropertiesPressedEnterKey();
        }
#endif
    }

    //public void PressedMapPropertiesButton()
    //{
    //    ShowMapPropertiesPopup(null);
    //}

    public void PressedConfirmMapPropertiesButton()
    {
        MapManager.singleton.mapName = mapNameInputField.text;
        MapManager.singleton.armyPointLimit = tempPointLimit;
        // MAYBEDO: Only set hasChanged if the mapName or armyPointLimit actually changed
        MapManager.singleton.hasChanged = true;
        EventManager.singleton.ReturnFocus();
        if (confirmPropertiesCallback != null)
        {
            confirmPropertiesCallback();
            confirmPropertiesCallback = null;
        }
    }

    public void PressedCancelMapPropertiesButton()
    {
        EventManager.singleton.ReturnFocus();
    }

    public void MoveCheckMark(GameObject hoverOver)
    {
        checkMark.transform.position = hoverOver.transform.position;
    }

    public void SetSymmetryMode(MapManager.SymmetrySetting setting)
    {
        MapManager.singleton.symmetrySetting = setting;
        switch(setting)
        {
            case MapManager.SymmetrySetting.NONE:
                symmetryModeButtonText.text = "No Symmetry";
                break;
            case MapManager.SymmetrySetting.HORIZONTAL:
                symmetryModeButtonText.text = "Horizontal Symmetry";
                break;
            case MapManager.SymmetrySetting.ROTATIONAL:
                symmetryModeButtonText.text = "Rotational Symmetry";
                break;
            default:
                symmetryModeButtonText.text = "4 Dimentional Symmetry";
                Debug.Log("[MapEditorManager:ChangeSymmetryType] typeIndex " + setting);
                break;
        }
        //        Debug.Log("[MapEditorManager:ChangeSymmetryType] typeIndex " + dropdown.value);
    }

    public void PressedNoSymmetryButton()
    {
        SetSymmetryMode(MapManager.SymmetrySetting.NONE);
        SetSymmetryModePanelActive(false);
    }

    public void PressedHorizontalSymmetryButton()
    {
        SetSymmetryMode(MapManager.SymmetrySetting.HORIZONTAL);
        SetSymmetryModePanelActive(false);
    }

    public void PressedRotationalSymmetryButton()
    {
        SetSymmetryMode(MapManager.SymmetrySetting.ROTATIONAL);
        SetSymmetryModePanelActive(false);
    }

    public void CloseSubPanels()
    {
        SetTerrainBrushesPanelActive(false);
        SetSymmetryModePanelActive(false);
    }

    #endregion

    #region private methods

    //private void ValidateMapProperties()
    //{
    //    if (mapNameInputField.text == "" || tempPointLimit == 0)
    //    {
    //        // MAYBEDO: Make sure that army point limit allows for at least one unit?
    //        mapPropertiesConfirmButton.interactable = false;
    //    }
    //    else
    //    {
    //        mapPropertiesConfirmButton.interactable = true;
    //    }
    //}


    //private void ShowMapPropertiesPopup(UnityAction callback)
    //{
    //    //ResetInputFields();
    //    ValidateMapProperties();
    //    confirmPropertiesCallback = callback;
    //    EventManager.singleton.GrantFocus(mapPropertiesPopup);
    //}

    //private void ResetInputFields()
    //{
    //    //mapNameInputField.text = MapManager.singleton.mapName;
    //    //armyPointLimitInputField.text = MapManager.singleton.armyPointLimit.ToString();
    //    //Debug.Log("[MapEditorManager:ResetInputFields] Set input field text to " + mapNameInputField.text);
    //    //MapManager.singleton.hasChanged = false;
    //}

    #if UNITY_STANDALONE

    private void MapPropertiesPressedEnterKey()
    {
        if (mapNameInputField.text == "")
        {
            mapNameInputField.Select();
        }

        else if (MapManager.singleton.armyPointLimit == 0)
        {
            armyPointLimitInputField.Select();
        }

        else 
        {
            EventManager.singleton.ReturnFocus();
        }

    }
    #endif

    #endregion

    #region monobehaviors
    // Use this for initialization
    void Awake()
    {
        Debug.Log("[MapEditorManager:Awake]");
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
    void Start () {
        swatchColors = new Color[6];
        swatchButtons = new GameObject[6];
        InitSwatchColors();
        Debug.Log("[MapEditorManager:Start]");
        MapManager.singleton.mapContext = MapManager.MapContext.MAP_EDITOR;
        //MapManager.singleton.LoadMap("test.bit");
        EventManager.singleton.ResetControlFocus(this);
	}
	
	// Update is called once per frame
	void Update () {

    }
    #endregion
}
