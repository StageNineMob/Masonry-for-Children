using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using UnityEngine.Events;

public class FileViewer : MonoBehaviour
{
    //enums

    //subclasses

    //consts and static data
    const float SCROLLBAR_TOP = 1f;
    const int NO_FILE = -1;
    const float SCROLL_FACTOR = 1000f;

    //public data

    //private data
    private List<FileViewListElement> elements;
    private string _directory, _extension;
    [SerializeField]
    private GameObject listElementPrefab;
    [SerializeField] private Scrollbar scrollbar;
    [SerializeField] private InputField _fileNameField;
    private float _scrollAreaMinHeight;
    private float _listElementHeight;
    private int _currentlySelectedFile = NO_FILE;
    [SerializeField] private Image metadataThumbnail;
    [SerializeField] private Text metadataText;

    private UnityAction _doubleClickCallback;

    //public properties
    public UnityAction doubleClickCallback
    {
        set
        {
            _doubleClickCallback = value;
        }
    }

    //methods
    #region public methods

    /// <summary>
    /// 
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="extension"></param>
    public void PopulateList(string directory, string extension)
    {
        _directory = directory;
        _extension = extension;

        FileManager.singleton.EnsureDirectoryExists(directory);
        List<string> fileNames = FileManager.singleton.FilenamesInDirectory(directory);
        foreach (string name in fileNames)
        {
            if (FileManager.FileExtensionIs(name, extension))
            {
                FileViewListElement element = Instantiate(listElementPrefab).GetComponent<FileViewListElement>();
                if(element == null)
                {
                    Debug.LogError("wut");
                }
                element.SetFileName(name);
                element.transform.SetParent(transform);
                element.transform.localScale = Vector3.one;

                elements.Add(element);
            }
        }
        // Resize scroll area to fit text and move to top of list
        if(elements.Count * _listElementHeight > _scrollAreaMinHeight)
        {
            GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().rect.width, elements.Count * _listElementHeight);
            scrollbar.value = SCROLLBAR_TOP;
            transform.localPosition = new Vector3(0,(GetComponent<RectTransform>().rect.height - _scrollAreaMinHeight) * -0.5f);
        }
    }

    public void NextInList()
    {
        if (_currentlySelectedFile != NO_FILE)
        {
            elements[(_currentlySelectedFile + 1) % elements.Count].ActivateSelf();
        }
    }

    public void PreviousInList()
    {
        if (_currentlySelectedFile != NO_FILE)
        {
            elements[(_currentlySelectedFile + elements.Count - 1) % elements.Count].ActivateSelf();
        }
    }


    public void Scroll(float axisValue)
    {
        if(GetComponent<RectTransform>().rect.height > _scrollAreaMinHeight)
        {
            scrollbar.value += axisValue * SCROLL_FACTOR / (GetComponent<RectTransform>().rect.height - _scrollAreaMinHeight);
            if(scrollbar.value > 1f)
            {
                scrollbar.value = 1f;
            }
            if (scrollbar.value < 0f)
            {
                scrollbar.value = 0f;
            }
        }
    }

    public void ClearList()
    {
        foreach(var element in elements)
        {
            Destroy(element.gameObject);
        }
        elements.Clear();
        GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().rect.width, _scrollAreaMinHeight);
        transform.localPosition = Vector3.zero;
    }

    public void SetFileName(string fileName)
    {
        _fileNameField.text = fileName;
        // TODO: 
        //      :| Figure out a way to highlight the name in the text field without...
        //      B| ...brakinstuff
        _fileNameField.MoveTextStart(false);
        _fileNameField.MoveTextEnd(true);
    }

    public void RespondToDoubleClick(string fileName)
    {
        if (_fileNameField.text == fileName)
        {
            _doubleClickCallback();
        }
        else
        {
            SetFileName(fileName);
        }
    }

    public void HighlightFileName(string fileName)
    {
        ClearMetadata();
#if  FILESYSTEM_CASE_INSENSITIVE
        fileName = fileName.ToLower();
#endif
        if (_currentlySelectedFile != NO_FILE)
            elements[_currentlySelectedFile].highlight = false;
        _currentlySelectedFile = NO_FILE;

        if (!FileManager.FileExtensionIs(fileName, _extension))
            fileName += _extension;
        for (int ii = 0; ii < elements.Count; ii++)
        {
            if (elements[ii].filename == fileName)
            {
                _currentlySelectedFile = ii;
                elements[ii].highlight = true;
                JumpToFileIndex(ii);
                DecodeMetadata(fileName);
                return;
            }
        }
    }

    public void JumpToFileIndex(int index)
    {
        var elementYPos = transform.localPosition.y + elements[index].transform.localPosition.y;
        var limit = (_scrollAreaMinHeight - _listElementHeight) * .5f;
        if (elementYPos > limit)
        {
            transform.localPosition += new Vector3(0, limit - elementYPos);
        }
        else if (elementYPos < -limit)
        {
            transform.localPosition += new Vector3(0, -limit - elementYPos);
        }
    }

    public void ClearMetadata()
    {
        metadataText.text = "";
        metadataThumbnail.color = Color.clear;
        //TODO: replace the sprite with our "IMAGE NOT FOUND" texture;
        metadataThumbnail.sprite = null;
    }

    #endregion

    #region private methods

    private void DecodeMetadata(string filename)
    {
        string textToPrint = "";
        switch(_extension)
        {
            case ".bit":
                {
                    metadataThumbnail.color = Color.white;

                    if (MapManager.singleton.LoadMap(filename, MapManager.PREVIEW_MODE))
                    {
                        textToPrint += MapManager.singleton.previewMapName + Environment.NewLine;
                        textToPrint += Environment.NewLine + "Point Limit: " + MapManager.singleton.previewArmyPointLimit;
                        metadataText.text = textToPrint;

                        var previewTexture = MapManager.singleton.MapPreview(MapManager.PREVIEW_MODE);
                        metadataThumbnail.sprite = Sprite.Create(previewTexture, new Rect(0, 0, previewTexture.width, previewTexture.height), Vector2.one * 0.5f);
                    }
                    else
                    {
                        // MAYBEDO: blow up?
                        Debug.LogError("[FileViewer:DecodeMetadata] Failed to load map");
                    }
                    break;
                }
            case ".jrk":
                {
                    metadataThumbnail.color = Color.white;

                    var previewArmy = ArmyManager.singleton.LoadArmy(filename);
                    textToPrint += previewArmy.armyName + Environment.NewLine;
                    textToPrint += Environment.NewLine + "Total Point Value: " + previewArmy.pointTotal;
                    metadataText.text = textToPrint;

                    var battleMageType = previewArmy.GetBattleMage();
                    //TODO: Army Manager should retrieve the correct texture.
                    break;
                }
            case ".fyt":
                {
                    metadataThumbnail.color = Color.white;

                    var fileLastWriteTime = FileManager.singleton.GetLastWriteTime(_directory + filename);
                    var previewGame = FileManager.singleton.Load<SavedGame>(_directory + filename);

                    textToPrint += fileLastWriteTime.ToString() + Environment.NewLine;
                    textToPrint += Environment.NewLine + "Play Time: " + FormatFloatTime(previewGame.gameTimer);
                    metadataText.text = textToPrint;

                    MapManager.singleton.DeserializeMap(previewGame.map, MapManager.PREVIEW_MODE);
                    if(MapManager.singleton.mapContext == MapManager.MapContext.COMBAT)
                    {
                        CombatManager.singleton.SetAllUnitsVisibility(false);
                    }
                    var previewTexture = MapManager.singleton.MapPreview(MapManager.PREVIEW_MODE);
                    if (MapManager.singleton.mapContext == MapManager.MapContext.COMBAT)
                    {
                        CombatManager.singleton.SetAllUnitsVisibility(true);
                    }
                    metadataThumbnail.sprite = Sprite.Create(previewTexture, new Rect(0, 0, previewTexture.width, previewTexture.height), Vector2.one * 0.5f);
                    break;
                }
            default:
                break;
        }
    }

    private static string FormatFloatTime(float floatTime)
    {
        int seconds = Mathf.FloorToInt(floatTime % 60);
        int minutes = Mathf.FloorToInt(floatTime / 60);
        string minutesSeconds = minutes.ToString() + ":" + seconds.ToString("00");
        return minutesSeconds;
    }

    #endregion

    #region monobehaviors

    // Use this for initialization
    void Awake()
    {
        elements = new List<FileViewListElement>();
        _scrollAreaMinHeight = GetComponent<RectTransform>().rect.height;
        _listElementHeight = listElementPrefab.GetComponent<LayoutElement>().minHeight;
    }
    #endregion
}
