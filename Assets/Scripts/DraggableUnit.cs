using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class DraggableUnit : Draggable
{
	[SerializeField] private GameObject prefab;
    private CombatManager.Faction curFaction;

	#region public methods

    public void SetFaction(CombatManager.Faction newFaction)
    {
        curFaction = newFaction;
        Color newColor = CombatManager.singleton.GetColorOfFaction(newFaction);
        gameObject.GetComponent<Image>().color = newColor;
    }

    public override void OnDropOntoTile(GameObject tile)
	{
		if (!tile.GetComponent<TileListener>().occupied && curFaction != CombatManager.Faction.NONE) 
		{
			CombatManager.singleton.InstantiateUnit(prefab, curFaction, tile.GetComponent<TileListener>().gridPos);
		}
	}

    public override void OnDropOntoUI(UnityEngine.EventSystems.PointerEventData eventData)
    {
     /*   if (IsUIObjectAtMouse(eventData, "Army Deployment Unit Scroll Area"))
        {

        }
      */
    }

	#endregion

	#region private methods
	#endregion

	#region monobehaviors
	#endregion
}
