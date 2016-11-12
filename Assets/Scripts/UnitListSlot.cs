using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using StageNine;

public class UnitListSlot : MonoBehaviour
{
    //enums

    //subclasses

    //consts and static data

    //public data

    //private data
    [SerializeField] Text _countText;
    [SerializeField] Image _unitImage;
    ArmyData.UnitType _unitType;

    //properties
    public Vector3 iconPos
    {
        get
        {
            Debug.Log("World Position " + _unitImage.transform.position.ToString());
            return _unitImage.transform.position;
        }
    }

    //methods
    #region public methods

    public void OnButtonPush()
    {
        MainMenu.singleton.RemoveUnitButton(_unitType);
    }

    public void SetCount(int count)
    {   
        _countText.text = "x" + count;
    }

    public void SetIcon(Sprite icon)
    {
        _unitImage.sprite = icon;
    }

    public void SetUnitType(ArmyData.UnitType type)
    {
        _unitType = type;
    }

    #endregion

    #region private methods

    #endregion

    #region monobehaviors

    #endregion
}
