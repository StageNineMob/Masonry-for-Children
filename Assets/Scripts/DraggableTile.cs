using UnityEngine;
using System.Collections;

public class DraggableTile : Draggable
{
	[SerializeField] private GameObject prefab;
	
	#region public methods

	public override void OnDropOntoTile(GameObject tile)
	{
        MapManager.singleton.OverwriteTile(tile, prefab);
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
