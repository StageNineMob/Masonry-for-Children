using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using StageNine;

public class ArmyListHolder : MonoBehaviour
{
    //enums

    //subclasses

    //consts and static data

    //public data

    //private data
    [SerializeField] private Sprite pawnSprite;
    [SerializeField] private Sprite knightSprite;
    [SerializeField] private Sprite archerSprite;
    [SerializeField] private Sprite artillerySprite;
    [SerializeField] private Sprite bikerSprite;
    [SerializeField] private Sprite battlemageLumpSprite;
    private Dictionary<ArmyData.UnitType, UnitListSlot> unitList;
    private float initialHeight;
    //prefabs
    [SerializeField] private GameObject unitListSlotPrefab;
    //public properties

    //methods
    #region public methods

    public void SetUnitCount(ArmyData.UnitType unitType, int newCount, bool removeZeros)
    {
        Debug.Log("[ArmyListHolder:SetUnitCount] Setting " + unitType + " count to " + newCount);
        Debug.Log(unitList.Count);
        if (unitList.ContainsKey(unitType))
        {
            if (newCount == 0 && removeZeros)
            {
                var garbage = unitList[unitType].gameObject;
                unitList.Remove(unitType);
                this.GetComponent<ListLayoutGroup>().Remove(garbage);
                //GameObject.Destroy(garbage);
                ResizeRectHeight();
            }
            else
            {
                unitList[unitType].SetCount(newCount);
            }
        }
        else
        {
            if (newCount != 0)
            {
                GameObject newSlot = GameObject.Instantiate(unitListSlotPrefab);
                this.GetComponent<ListLayoutGroup>().Add(newSlot);
                newSlot.GetComponent<UnitListSlot>().SetCount(newCount);
                newSlot.GetComponent<UnitListSlot>().SetIcon(GetUnitSprite(unitType));
                newSlot.GetComponent<UnitListSlot>().SetUnitType(unitType);
                unitList.Add(unitType, newSlot.GetComponent<UnitListSlot>());
                ResizeRectHeight();
            }
        }
    }

    public bool ContainsUnit(ArmyData.UnitType type)
    {
        return unitList.ContainsKey(type);
    }

    public Vector3 GetImageLocation(ArmyData.UnitType type)
    {
        // We're intentionally having this blow up if type doesn't exist. We're too lazy to make a try/catch
        // MAYBEDO: Make a try/catch
        return unitList[type].iconPos;
    }
  
    #endregion

    #region private methods
    private Sprite GetUnitSprite(ArmyData.UnitType unitType)
    {
        switch (unitType)
        {
            case ArmyData.UnitType.PAWN:
                return pawnSprite;
            case ArmyData.UnitType.KNIGHT:
                return knightSprite;
            case ArmyData.UnitType.ARCHER:
                return archerSprite;
            case ArmyData.UnitType.ARTILLERY:
                return artillerySprite;
            case ArmyData.UnitType.BIKER:
                return bikerSprite;
            case ArmyData.UnitType.BATTLEMAGE_LUMP:
                return battlemageLumpSprite;
            default:
                return null;
        }
    }

    private void ResizeRectHeight()
    {
        var newHeight = Mathf.Abs(GetComponent<ListLayoutGroup>().offset.y) * unitList.Count;
        Debug.Log("[ArmyListHolder:ResizeRectHeight] Resizing to " + newHeight);
        if (initialHeight < newHeight)
        {
            Debug.Log("[ArmyListHolder:ResizeRectHeight] Modifying to new value");
            GetComponent<RectTransform>().offsetMin += Vector2.down * (newHeight - GetComponent<RectTransform>().rect.height);
        }
        else
        {
            Debug.Log("[ArmyListHolder:ResizeRectHeight] Returning to initial value (" + initialHeight + ")");
            GetComponent<RectTransform>().offsetMin += Vector2.down * (initialHeight - GetComponent<RectTransform>().rect.height);
        }
    }
    #endregion

    #region monobehaviors
    void Awake()
    {
        Debug.Log("[ArmyListHolder:Awake]");
        unitList = new Dictionary<ArmyData.UnitType, UnitListSlot>();
        initialHeight = GetComponent<RectTransform>().rect.height;
        Debug.Log("[ArmyListHolder:Awake] height = " + initialHeight);
    }
    #endregion
}
