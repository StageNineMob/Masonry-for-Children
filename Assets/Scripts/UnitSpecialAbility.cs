using UnityEngine;
using System.Collections;

namespace StageNine{
	public abstract class UnitSpecialAbility{

		public string buttonLabel;
        public KeyCode hotKey;

        protected GameObject parentUnit;

        public virtual bool buttonInteractable
		{
			get{return true;} //treat this as abstract, always override.
		}

        #region constructors

        public UnitSpecialAbility(){}

        #endregion

        #region public methods

        public virtual void SpecialStateEnter(){}

		public virtual void SpecialStateExit(){}

        public virtual void ConfirmStateEnter(){}

        public virtual void ConfirmStateExit(){}

        public virtual void UnitClick(GameObject unit, CombatManager.GameState gameState){}

        public virtual void TileClick(GameObject tile, CombatManager.GameState gameState)
        {
            CombatManager.singleton.selectedUnit.GetComponent<UnitHandler>().ClearTarget();
        }

        public abstract void ExecuteAbility();

        #endregion

    }
}