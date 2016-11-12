using UnityEngine;
using System.Collections.Generic;

namespace StageNine{
	public class TileEdge{
		//Helper edge class, unidirection graph edge, stores associated move cost.
		public GameObject tile;
//		public int cost;
        public Dictionary<UnitHandler.PathingType,int> cost;

        #region constructors
		public TileEdge(){
			cost = new Dictionary<UnitHandler.PathingType, int>();
			tile = null;
		}

		public TileEdge(GameObject head, Dictionary<UnitHandler.PathingType, int> moveCost){
			if(head != null){
				tile = head;
			} 
			else
			{
				//throw exception
				Debug.LogError("NULL tile");
			}

			cost = moveCost;
		}
        #endregion

        #region public methods

        #endregion
        #region private methods

        #endregion
	}	
}

