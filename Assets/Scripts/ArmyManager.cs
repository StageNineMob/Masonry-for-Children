using UnityEngine;
using System.Collections.Generic;
using StageNine;
using System;

public class ArmyManager : MonoBehaviour
{
    //enums

    //subclasses

    //consts and static data
    public static ArmyManager singleton;

    public const string ARMY_FILE_EXTENSION = ".jrk";
    public const string ARMY_DIRECTORY = "/doods/";

    //public data
    [HideInInspector] public bool hasChanged = false;

    //private data
    private ArmyData redArmy, redArmyBackup; // used by army builder for file I/O
    private ArmyData blueArmy, blueArmyBackup;
    private string _currentFileName = "";

    //[SerializeField] private GameObject pawnPrefab;
    //[SerializeField] private GameObject bikerPrefab;
    //[SerializeField] private GameObject archerPrefab;
    //[SerializeField] private GameObject knightPrefab;
    //[SerializeField] private GameObject battlemageLumpPrefab;

    //public properties
    public string currentFileName 
    { 
        get 
        {
            return _currentFileName; 
        } 
    }
    //methods
    #region public methods

    public ArmyData LoadArmy(string fileName)
    {
        string correctedFileName = fileName;
        if (!FileManager.FileExtensionIs(fileName, ARMY_FILE_EXTENSION))
        {
            correctedFileName += ARMY_FILE_EXTENSION;
        }
        SerializableArmy load = FileManager.singleton.Load<SerializableArmy>(ARMY_DIRECTORY + correctedFileName);
        if (load == null)
            return null;

        ArmyData output = new ArmyData();
        //if file was opened successfully, change the army name;
        _currentFileName = correctedFileName;
        Debug.Log("[ArmyManager:LoadArmy] Loaded Army: " + load.armyName + " to target");
        output.armyName = load.armyName;

        for(int ii = 0; ii < load.counts.Length; ++ii)
        {
            output.AddUnit(load.types[ii], load.counts[ii]);
        }
        hasChanged = false;
        return output;
    }

    public bool LoadArmy(string fileName, CombatManager.Faction faction)
    {
        var loadedArmy = LoadArmy(fileName);
        if(loadedArmy == null)
            return false;
        switch (faction)  // note: this must remain a switch so that we are changing the pointer itself.
        {
            case CombatManager.Faction.RED:
                redArmy = loadedArmy;
                break;
            case CombatManager.Faction.BLUE:
                blueArmy = loadedArmy;
                break;
            default:
                Debug.LogError("Unrecognized faction.");
                return false;
        }
        return true;
    }

    public void SaveArmy(string fileName)
    {
        SaveArmy(redArmy, fileName);
    }

    public void SaveArmy(ArmyData army, string fileName)
    {
        if (FileManager.FileExtensionIs(fileName, ARMY_FILE_EXTENSION))
        {
            _currentFileName = fileName;
        }
        else
        {
            _currentFileName = fileName + ARMY_FILE_EXTENSION;
        }
        SaveArmy(army);
    }

    public void SetNameInBuilder(string name) 
    {
        redArmy.armyName = name;
    }

    public void SaveArmy(ArmyData army)
    {
        Debug.Log("[ArmyManager:SaveArmy] Saving Army " + _currentFileName);
        FileManager.singleton.EnsureDirectoryExists(ARMY_DIRECTORY);

        SerializableArmy save = new SerializableArmy();
        save.armyName = army.armyName;
        var dict = army.units;
        save.types = new ArmyData.UnitType[dict.Count];
        save.counts = new int[dict.Count];
        int ii = 0;
        foreach (var pair in dict)
        {
            save.types[ii] = pair.Key;
            save.counts[ii] = pair.Value;
            ii++;
        }
        FileManager.singleton.Save<SerializableArmy>(save, ARMY_DIRECTORY + _currentFileName);
        hasChanged = false;
    }

    public void NewArmy()
    {
        if (redArmy == null)
        {
            redArmy = new ArmyData();
        }
        else
        {
            redArmy.Clear();
        }
        _currentFileName = "";
        hasChanged = false;
    }

    public void ClearArmies()
    {
        redArmy = null;
        blueArmy = null;
    }

    public int AddUnitInDeployment(ArmyData.UnitType unit, CombatManager.Faction faction)
    {
        int count = -1;

        ArmyData army = GetArmyByFaction(faction);
        army.AddUnit(unit);
        count = GetUnitCountIn(unit, army);

        return count;
    }

    public int RemoveUnitInDeployment(ArmyData.UnitType unit, CombatManager.Faction faction)
    {
        int count = -1;

        ArmyData army = GetArmyByFaction(faction);
        army.RemoveUnit(unit);
        count = GetUnitCountIn(unit, army);
        
        return count;
    }

    public void SetBattlemageInBuilder(ArmyData.UnitType type)
    {
        if(type >= ArmyData.FIRST_BATTLEMAGE)
        {
            redArmy.SetBattlemage(type);
        }
        else
        {
            Debug.LogError("[ArmyManager:SetBattlemageInBuilder] Invalid Battlemage");
        }
    }

    public int AddUnitInBuilder(ArmyData.UnitType type)
    {
        redArmy.AddUnit(type);
        hasChanged = true;
        return redArmy.GetCountOfType(type);
    }

    public int RemoveUnitInBuilder(ArmyData.UnitType type)
    {
        redArmy.RemoveUnit(type);
        hasChanged = true;
        return redArmy.GetCountOfType(type);
    }

    public int GetUnitCount(ArmyData.UnitType unit, CombatManager.Faction faction)
    {
        return GetUnitCountIn(unit, GetArmyByFaction(faction));
    }

    public int GetTotalUnitCount(CombatManager.Faction faction)
    {
        return GetTotalUnitCount(GetArmyByFaction(faction));
    }

    public int GetTotalPointCost(CombatManager.Faction faction)
    {
        return GetArmyByFaction(faction).pointTotal;
    }

    public string GetArmyName(CombatManager.Faction faction)
    {
        return GetArmyName(GetArmyByFaction(faction));
    }

    public string GetArmyName(ArmyData army)
    {
        if (army != null)
        {
            return army.armyName;
        }
        Debug.Log("[ArmyManager:GetArmyName] Army uninitialized.");
        return null;
    }

    public void LogCurrentArmyContents(List<GameObject> unitList, CombatManager.Faction faction)
    {
        ArmyData army = new ArmyData(unitList);
        LogArmyContents(army, faction);
    }

    private static void LogArmyContents(ArmyData army, CombatManager.Faction faction)
    {
        string output = "The " + CombatManager.singleton.GetNameOfFaction(faction).ToLower() + " army contains: ";
        bool first = true;
        foreach (var pair in army.units)
        {
            if (first)
                first = false;
            else
                output += ", ";
            output += pair.Value + " " + ArmyData.GetNameOfType(pair.Key);
        }

        FileManager.singleton.AppendToLog(output);
    }

    public void LogInitialArmyContents(CombatManager.Faction faction)
    {
        ArmyData army = GetArmyByFaction(faction);
        LogArmyContents(army, faction);
    }

    public void BackupArmyData()
    {
        redArmyBackup = new ArmyData(redArmy);
        blueArmyBackup = new ArmyData(blueArmy);
    }

    public void RevertToBackup()
    {
        redArmy = redArmyBackup;
        blueArmy = blueArmyBackup;
    }
    #endregion

    #region private methods

    private ArmyData GetArmyByFaction(CombatManager.Faction faction)
    {
        switch (faction)
        {
            case CombatManager.Faction.RED:
                return redArmy;
            case CombatManager.Faction.BLUE:
                return blueArmy;
            default:
                Debug.LogError("[ArmyManager:GetArmyByFaction] Unrecognized faction.");
                return null;
        }
    }

    private int GetUnitCountIn(ArmyData.UnitType unit, ArmyData army)
    {
        if (army.units.ContainsKey(unit))
        {
            return army.units[unit];
        }
        else
        {
            return 0;
        }
    }

    private int GetTotalUnitCount(ArmyData army)
    {
        int total = 0;
        foreach (ArmyData.UnitType type in Enum.GetValues(typeof(ArmyData.UnitType)))
        {
            total += GetUnitCountIn(type, army);
        }
        return total;
    }

    #endregion

    #region monobehaviors

    void Start()
    {
        
    }

    void Awake()
    {
        Debug.Log("[ArmyManager:Awake]");
        if (singleton == null)
        {
            Debug.Log("ArmyManager checking in.");
            DontDestroyOnLoad(gameObject);
            singleton = this;
            // InitializeFields();
        }
        else
        {
            //if anything needs to be re:init, have this call the singleton.
            Debug.Log("ArmyManager checking out.");
            GameObject.Destroy(gameObject);
        }
        // _currentMapFileName = defaultMapFileName;
    }

    #endregion
}
