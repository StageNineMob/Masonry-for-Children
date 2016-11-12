using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using StageNine;
public class BoardEditor : UIEconomyTracker {

    public static BoardEditor singleton;

    public CombatManager.Faction curFaction = CombatManager.Faction.NONE;

    //private Dictionary<ArmyData.UnitType, DraggableUnit>

    [SerializeField] private GameObject factionChangeButton;
    [SerializeField] private GameObject[] unitIcons;


#region public methods

	public void ClickedFactionButton()
	{
		switch(curFaction)
		{
			case CombatManager.Faction.RED:
				curFaction = CombatManager.Faction.BLUE;
				break;
			case CombatManager.Faction.BLUE:
			default:
				curFaction = CombatManager.Faction.RED;
				break;
		}
        SetAllFactions(curFaction);
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void ToggleVisibility()
    {
        gameObject.SetActive(!gameObject.activeSelf);
    }

#endregion

#region private methods

    //private void ChangeIconColor(GameObject icon, Color newColor)
    //{
    //    icon.GetComponent<Image>().color = newColor;
    //}

    //private void ChangeAllColors(Color newColor)
    //{
    //    ChangeIconColor(factionChangeButton, newColor);
    //    foreach(var icon in unitIcons)
    //    {
    //        ChangeIconColor(icon, newColor);
    //    }
    //}

    private void SetAllFactions(CombatManager.Faction faction)
    {
        foreach (var icon in unitIcons)
        {
            icon.GetComponent<DraggableUnit>().SetFaction(faction);
        }
    }

#endregion

#region MonoBehaviours
	// Use this for initialization
	protected override void Start () {
		if (singleton == null)
		{
			Debug.Log("Hello, world!");
			singleton = this;
            base.Start();
            gameObject.SetActive(false);
		}
		else
		{
			Debug.Log("Goodbye, cruel world!");
			GameObject.Destroy(gameObject);
		}
	}
	
#endregion

}
