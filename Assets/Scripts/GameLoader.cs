using UnityEngine;
using System.Collections;
using StageNine;
using System;

public class GameLoader : MonoBehaviour {
    //enums
    public enum StartUpMode
    {
        NONE,
        NEW_GAME,
        LOAD_GAME,
    }

    //subclasses

    //consts and static data
    public static GameLoader singleton;

    const int NUM_PLAYERS = 2;

    //public data
    public StartUpMode startUpMode;
    public uint currentArmyPointMax = 0;
    public SavedGame gameSave;

    //private data
    private SerializableMap newGameMap;
    private SerializableArmy newGameRedArmy;
    private SerializableArmy newGameBlueArmy;

    private int armiesReady;
    private bool mapReady;

    //public properties

    //methods
    #region public methods

    public bool CheckArmiesReady()
    {
        int armiesReady = 0;

        foreach (CombatManager.Faction faction in Enum.GetValues(typeof(CombatManager.Faction)))
        {
            if (faction != CombatManager.Faction.NONE)
            {
                if (CheckArmyReady(faction))
                {
                    armiesReady++;
                }
            }
        }

        return (armiesReady == NUM_PLAYERS);
    }

    private bool CheckArmyReady(CombatManager.Faction faction)
    {
        if (ArmyManager.singleton.GetArmyName(faction) != null)
        {
            if (ArmyManager.singleton.GetTotalPointCost(faction) <= currentArmyPointMax)
            {
                return true;
            }
        }
        return false;
    }
    #endregion

    #region private methods

    #endregion

    #region monobehaviors

    #endregion

    // Use this for initialization
    void Awake()
    {
        Debug.Log("[GameLoader:Awake]");
        if (singleton == null)
        {
            Debug.Log("GameLoader checking in.");
            DontDestroyOnLoad(gameObject);
            singleton = this;
            //InitializeFields();
        }
        else
        {
            //if anything needs to be re:init, have this call the singleton.
            Debug.Log("GameLoader checking out.");
            GameObject.Destroy(gameObject);
        }
        //_currentMapFileName = defaultMapFileName;
    }

    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
