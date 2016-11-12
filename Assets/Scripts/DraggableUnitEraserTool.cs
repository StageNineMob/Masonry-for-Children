using UnityEngine;
using System.Collections;

public class DraggableUnitEraserTool : Draggable
{

	#region public methods

	public override void OnDropOntoTile(GameObject tile)
	{
		if (tile.GetComponent<TileListener>().occupied) 
		{
			CombatManager.singleton.DeleteUnit(tile.GetComponent<TileListener>().occupant);
		}
	}

    public override void OnDropOntoUI(UnityEngine.EventSystems.PointerEventData eventData)
    {
    }

    #endregion

	#region private methods
	#endregion

	#region monobehaviors
	#endregion
}
