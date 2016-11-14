using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Globalization;

public class FileManager : MonoBehaviour {
    public static FileManager singleton;

    private BinaryFormatter binaryFormatter;
    private static readonly string logDirectory = "/logs/";
    private static string _logFileName;

    public static string logFileName
    {
        get
        {
#if LOGGING   
            return _logFileName;
#else
            return null;
#endif
        }
    }

#region public methods
    public void Save<T>(T data, string filePath)
    {
        FileStream file = File.Open(Application.persistentDataPath + filePath, FileMode.OpenOrCreate);
        binaryFormatter.Serialize(file, data);
        file.Close();
    }

    public T Load<T>(string filePath)
    {
        FileStream file = null;
        try
        {
            if (File.Exists(Application.persistentDataPath + filePath))
            {
                file = File.Open(Application.persistentDataPath + filePath, FileMode.Open);
                T data = (T)binaryFormatter.Deserialize(file);
                file.Close();
                return data;
            }
            EventManager.singleton.ShowErrorPopup("File not found", "OK", new UnityAction(() =>
            {
                EventManager.singleton.ReturnFocus();
            }
            ), KeyCode.O);
            //Debug.LogError("[FileManager:Load] File " + filePath + " not found.");
            return default(T);
        }
        catch (Exception a)
        {
            if (file != null)
            {
                file.Close();
            }
            Debug.LogError("[FileManager:Load] File load exception: " + a.Message);
            return default(T);
        }
    }

    /// <summary>
    /// Not particularly cautious file management. <_<;
    /// </summary>
    /// <param name="source"></param>
    /// <param name="target"></param>
    public void CautiousReplace(string source, string target)
    {
        File.Copy(source, target, true);
    }

    public static bool FileExtensionIs(string fileName, string ext)
    {
        return fileName.EndsWith(ext);
    }

    public void EnsureDirectoryExists(string path)
    {
        //TODO: try catch?
        if (!Directory.Exists(Application.persistentDataPath + path))
        {
            CreateDirectory(Application.persistentDataPath + path);
        }
    }

    public List<string> FilenamesInDirectory(string path)
    {
        string fullPath = Application.persistentDataPath + path;
        List<string> output = new List<string>();
        if (Directory.Exists(fullPath))
        {
            DirectoryInfo di = new DirectoryInfo(fullPath);
            foreach (FileInfo fi in di.GetFiles())
            {
                output.Add(fi.Name);
            }
        }
        return output;
    }

    public bool FileExists(string filePath)
    {
        return File.Exists(Application.persistentDataPath + filePath);
    }

    public void CreateDirectory(string path)
    {
        //try catch?
        Directory.CreateDirectory(path);
    }

    public DateTime GetLastWriteTime(string filePath)
    {
        return File.GetLastWriteTime(Application.persistentDataPath + filePath);
    }

    public void CreateLogFile()
    {
#if LOGGING
        DateTime localDate = DateTime.Now;
        EnsureDirectoryExists(logDirectory);

        String sDate = localDate.Year.ToString() + "_" + localDate.Month.ToString() + "_" + localDate.Day.ToString() + "_" + localDate.Hour.ToString() + "_" + localDate.Minute.ToString() + "_" + localDate.Second.ToString();

        _logFileName = Application.persistentDataPath + logDirectory + sDate + "_test.log";
        AppendToLog("Test Starting");
#endif
    }

    public void AppendToLog(string logData, bool timeStamp = false)
    {
#if LOGGING
        //        Debug.Log("[FileManager:AppendToLog] path = " + logFileName);
        string logStamp = "";

        if (timeStamp)
        {
            logStamp = DateTime.Now.ToString() + ": ";
        }

        System.IO.File.AppendAllText(_logFileName, logStamp + logData + Environment.NewLine);
#endif
    }


#endregion

#region monobehaviors
    void Awake()
    {
        Debug.Log("[FileManager:Awake]");
        if (singleton == null)
        {
            Debug.Log("FileManager checking in.");
            print(Application.persistentDataPath);
            singleton = this;
            binaryFormatter = new BinaryFormatter();
        }
        else
        {
            Debug.Log("FileManager checking out.");
            GameObject.Destroy(gameObject);
        }
    }

    // Use this for initialization
    void Start ()
    {
        

        //SerializableMap test = new SerializableMap();
        //test.mapName = "testMap";
        //test.tiles = new SerializableTile[3];
        //for(int ii = 0; ii < test.tiles.Length; ++ii)
        //{
        //    test.tiles[ii] = new SerializableTile();
        //    test.tiles[ii].x = ii + 1;
        //    test.tiles[ii].y = test.tiles.Length - ii;
        //    test.tiles[ii].type = TileListener.TerrainType.GRASS;
        //}

        //string path = "/test.bit";
        ////Save<SerializableTile>(tl, path);
        //Save<SerializableMap>(test, path);

        ////SerializableTile load = Load<SerializableTile>(path);
        //SerializableMap load = Load<SerializableMap>(path);
        
        ////Debug.Log("[FileManager:Start] " + Application.persistentDataPath);
        //Debug.Log("[FileManager:Start] " + load.ToString());
    }

    // Update is called once per frame
    void Update () {
	
	}
#endregion
}
