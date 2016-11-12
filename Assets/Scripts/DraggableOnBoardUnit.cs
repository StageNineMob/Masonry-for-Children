using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
public class DraggableOnBoardUnit : Draggable
{

    #region public methods

    public override void OnDropOntoTile(GameObject tile)
    {
        if(CombatManager.singleton.GetGameState() == CombatManager.GameState.ARMY_PLACEMENT)
        {
            var tileFrom = MapManager.singleton.GetTileAt(GetComponent<UnitHandler>().gridPos).GetComponent<TileListener>();
            var tileTo = tile.GetComponent<TileListener>();
            if (tileFrom.deployable == tileTo.deployable)
            {
                CombatManager.singleton.ExchangeTileOccupants(tileFrom.gridPos, tileTo.gridPos);
            }
        }
    }

    public override void OnDropOntoUI(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (IsUIObjectAtMouse(eventData, "Army Deployment Unit Scroll Area"))
        {
            // TODO: Guard code check to make sure UnitHandler is not null... if it is, BLOW UP EVERYTHING
            CombatManager.singleton.ReturnToBank(GetComponent<UnitHandler>());
        }
    }

    #endregion

    #region private methods
    #endregion

    #region monobehaviors
    #endregion
}
