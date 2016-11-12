using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using StageNine;
using System;
public class ArmyDeploymentPanel : UIEconomyTracker
{

    public static ArmyDeploymentPanel singleton;

    public CombatManager.Faction curFaction = CombatManager.Faction.NONE;

    //private Dictionary<ArmyData.UnitType, DraggableUnit>

    //[SerializeField]
    //private GameObject factionChangeButton;
    [SerializeField] private GameObject[] unitIcons;
    [SerializeField] private DraggableUnit draggablePawn;
    [SerializeField] private DraggableUnit draggableKnight;
    [SerializeField] private DraggableUnit draggableArcher;
    [SerializeField] private DraggableUnit draggableArtillery;
    [SerializeField] private DraggableUnit draggableBiker;
    [SerializeField] private DraggableUnit draggableBattlemageLump;

    [SerializeField] private Button doneButton;
    [SerializeField] private Text doneButtonLabel;
    [SerializeField] private GameObject parentPanel;

    #region public methods

    public void Hide()
    {
        parentPanel.SetActive(false);
    }

    public void Show()
    {
        parentPanel.SetActive(true);
    }

    //public void ClickedFactionButton()
    //{
    //    switch (curFaction)
    //    {
    //        case CombatManager.Faction.RED:
    //            curFaction = CombatManager.Faction.BLUE;
    //            ChangeAllColors(Color.blue);
    //            break;
    //        case CombatManager.Faction.BLUE:
    //        default:
    //            curFaction = CombatManager.Faction.RED;
    //            ChangeAllColors(Color.red);
    //            break;

    //    }
    //}

    public void ToggleVisibility()
    {
        parentPanel.SetActive(!parentPanel.activeSelf);
    }

    public void BeginDeployment(CombatManager.Faction faction)
    {
        var unitCount = 0;
        curFaction = faction;
        SetFactionToDeploy(faction);
        var holder = GetComponent<ArmyListHolder>();
        foreach (ArmyData.UnitType type in Enum.GetValues(typeof(ArmyData.UnitType)))
        {
            if(type != ArmyData.UnitType.NONE)
            {

                unitCount = ArmyManager.singleton.GetUnitCount(type, faction);
                holder.SetUnitCount(type, unitCount, true);

                if (holder.ContainsUnit(type))
                {
                    GetDraggableForType(type).SetPosition(holder.GetImageLocation(type));
                    GetDraggableForType(type).gameObject.SetActive(true);
                }
                else
                {
                    GetDraggableForType(type).gameObject.SetActive(false);
                }
            }
        }
        Show();
        DisableDoneButton();
    }

    public void DecrementUnitSlot(ArmyData.UnitType type)
    {
        int newCount = ArmyManager.singleton.RemoveUnitInDeployment(type, curFaction);
        GetComponent<ArmyListHolder>().SetUnitCount(type, newCount, false);
        if(newCount == 0)
        {
            GetDraggableForType(type).gameObject.SetActive(false);
            CheckForDeploymentEnd();
        }
    }

    public void IncrementUnitSlot(ArmyData.UnitType type)
    {
        int newCount = ArmyManager.singleton.AddUnitInDeployment(type, curFaction);
        GetComponent<ArmyListHolder>().SetUnitCount(type, newCount, false);
        if (newCount == 1)
        {
            GetDraggableForType(type).gameObject.SetActive(true);
            DisableDoneButton();
        }
    }


    public void PressedDoneButton()
    {
        CombatManager.singleton.ConfirmDeployment();
    }

    public void PressedRandomButton()
    {
        CombatManager.singleton.DeployPatternRandom(curFaction);
        doneButton.Select();
    }

    public void PressedAutoButton()
    {
        CombatManager.singleton.DeployPatternFEBA(curFaction);
        doneButton.Select();
    }

    #endregion

    #region private methods

    private void CheckForDeploymentEnd()
    {
        Debug.Log("[ArmyDeploymentPanel:CheckForDeploymentEnd] Total count in " + curFaction + " army: " + ArmyManager.singleton.GetTotalUnitCount(curFaction));
        if(ArmyManager.singleton.GetTotalUnitCount(curFaction) == 0)
        {
            doneButton.interactable = true;
            doneButtonLabel.color = Color.black;
        }
        
    }

    private void DisableDoneButton()
    {
        doneButton.interactable = false;
        doneButtonLabel.color = Color.grey;
    }

    private void SetFactionToDeploy(CombatManager.Faction newFaction)
    {
        foreach (ArmyData.UnitType type in Enum.GetValues(typeof(ArmyData.UnitType)))
        {
            if(type != ArmyData.UnitType.NONE)
            {
                GetDraggableForType(type).SetFaction(newFaction);
            }
        }
    }

private void Initialize()
{
    //gameObject.GetComponent<ArmyListHolder>().SetUnitCount(ArmyData.UnitType.PAWN, 256);
    //gameObject.GetComponent<ArmyListHolder>().SetUnitCount(ArmyData.UnitType.KNIGHT, 14);
    //gameObject.GetComponent<ArmyListHolder>().SetUnitCount(ArmyData.UnitType.PAWN, 32);
    //gameObject.GetComponent<ArmyListHolder>().SetUnitCount(ArmyData.UnitType.ARCHER, 168);
    //gameObject.SetActive(false);
}

private DraggableUnit GetDraggableForType(ArmyData.UnitType type)
    {
        switch (type)
        {
            case ArmyData.UnitType.ARCHER:
                return draggableArcher;
            case ArmyData.UnitType.ARTILLERY:
                return draggableArtillery;
            case ArmyData.UnitType.BIKER:
                return draggableBiker;
            case ArmyData.UnitType.KNIGHT:
                return draggableKnight;
            case ArmyData.UnitType.PAWN:
                return draggablePawn;
            case ArmyData.UnitType.BATTLEMAGE_LUMP:
                return draggableBattlemageLump;
        }
        Debug.LogError("[ArmyDeploymentPanel:GetDraggableForType] Invalid type!");
        return null;
    }

    #endregion

    #region MonoBehaviours
    // Use this for initialization
    void Awake()
    {
        if (singleton == null)
        {
            Debug.Log("Hello, world!");
            singleton = this;
            
            Initialize();
        }
        else
        {
            Debug.Log("Goodbye, cruel world!");
            GameObject.Destroy(gameObject);
        }
    }

    #endregion

}
