using System;
using UnityEngine;
using System.Collections;

public class OptionsManager : MonoBehaviour {

    //enums

    //subclasses

    //consts and static data
    const string optionsFilePath = "/prefs/";
    const string optionsFileName = "options.gum";
    public static OptionsManager singleton;

    //public data
    public float soundVolume = 1f;
    public float musicVolume = .5f;
    public bool battleAnimationsEnabled;
    public bool movementAnimationsEnabled = true;
    public bool autoEndTurnEnabled = true;

    //private data
    private SerializableOptions _cachedOptions;

    //public properties

    //methods
    #region public methods

    public void SetOptions(SerializableOptions options)
    {
        _cachedOptions = options;

        battleAnimationsEnabled = options.battleAnimationsEnabled;
        movementAnimationsEnabled = options.movementAnimationsEnabled;
        autoEndTurnEnabled = options.autoEndTurnEnabled;
        soundVolume = options.soundVolume;
        musicVolume = options.musicVolume;
        MusicPlayer.singleton.PlayMusicVolume(musicVolume);
    }

    public void SaveOptions()
    {
        FileManager.singleton.EnsureDirectoryExists(optionsFilePath);
        FileManager.singleton.Save<SerializableOptions>(_cachedOptions, optionsFilePath + optionsFileName);
    }

    #endregion

    #region private methods
    private void LoadOptions()
    {
        if (FileManager.singleton.FileExists(optionsFilePath + optionsFileName))
        {
            try
            {
                SetOptions(FileManager.singleton.Load<SerializableOptions>(optionsFilePath + optionsFileName));
            }
            catch (Exception)
            {
                Debug.LogError("[OptionsManager:LoadOptions] Error loading options, using defaults");
                battleAnimationsEnabled = true;
                movementAnimationsEnabled = true;
                autoEndTurnEnabled = false;
                soundVolume = 1f;
                musicVolume = 0.5f;
            }
        }
        else
        {
            Debug.LogWarning("[OptionsManager:LoadOptions] File not found.");
        }
    }
    #endregion

    #region monobehaviors
    void Awake()
    {
        Debug.Log("[OptionsManager:Awake]");
        if (singleton == null)
        {
            Debug.Log("OptionsManager checking in.");
            DontDestroyOnLoad(gameObject);
            singleton = this;
            // InitializeFields();
        }
        else
        {
            //if anything needs to be re:init, have this call the singleton.
            Debug.Log("OptionsManager checking out.");
            GameObject.Destroy(gameObject);
        }
        // _currentMapFileName = defaultMapFileName;
    }

    void Start()
    {
        if(singleton == this)
        {
            LoadOptions();
        }
    }

    #endregion

}



[Serializable]
public class SerializableOptions
{
    public bool battleAnimationsEnabled;
    public bool movementAnimationsEnabled;
    public bool autoEndTurnEnabled;
    public float soundVolume;
    public float musicVolume;

    #region public methods
    public override string ToString()
    {
        string output = "";
        return output;
    }
    #endregion
}