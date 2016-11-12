using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;
using System;
using StageNine;

public class UHBiker : UHPawn{

    public class SpinTires : UnitSpecialAbility
    {
        public override bool buttonInteractable
        {
            get
            {
                return (parentUnit.GetComponent<UnitHandler>().GetAttacksCur() > 0 &&
                   parentUnit.GetComponent<UnitHandler>().GetMovementCur() > 0);
            }
        }

        #region constructors 
        public SpinTires()
        {
        }

        public SpinTires(GameObject parent)
        {
            hotKey = KeyCode.S;
            parentUnit = parent;
            buttonLabel = "[S]PIN TIRES";
        }
        #endregion

        #region public methods
        public override void SpecialStateEnter()
        {
            /* TODO: Make a confirmation screen rather than immediately beginning defense. This is dependent on modal popups stuff.
			 * While the confirmation dialog is open, highlight areas that will be defended.
			 */
            ExecuteAbility();
        }

        public override void ExecuteAbility()
        {
            //MAYBEDO: Spend movement more than one at a time?
            parentUnit.GetComponent<UHBiker>().SpendMovement(1);
            CombatManager.singleton.SelectGameState(CombatManager.GameState.ORDERING, parentUnit);
        }
        #endregion
    }


    public float movementAttackBonus = 0.0f;
	public float movementDefenseBonus = 0.0f;

    public override ArmyData.UnitType unitType
    {
        get { return ArmyData.UnitType.BIKER; }
    }

    public override float defenseModifier
	{
		get
		{
			if(CombatManager.singleton.currentPlayer == faction)
			{
				// On their own turn, Bikers' defense is improved (reduced) by available movement
				return (1.0f - movementCur * movementDefenseBonus) * ComputeTerrainDefensiveModifier();
			}
			return ComputeTerrainDefensiveModifier();
		}
	}

    public override float attackModifier
    {
        get
        {
            if (CombatManager.singleton.currentPlayer == faction)
            {
                // On their own turn, Bikers' attack is improved (increased) by spent movement
                return (1.0f + movementAttackBonus * (movementRange - movementCur)) * ComputeTerrainAttackModifier();
            }
            return ComputeTerrainAttackModifier();
        }
    }

    public override RandomSoundPackage animaticAttackStartSound
    {
        get
        {
            return AudioManager.singleton.bikerChargeRSP;
        }
    }

    protected override float animaticSpritesPerUnit
    {
        get
        {
            return 4f;
        }
    }

    #region public methods

    public override void OurInit()
    {
        base.OurInit();
        specialAbility = new SpinTires(gameObject);
    }

    public override void MoveTo(IntVector2 pos)
    {
        if (TryMoveTo(pos))
        {
            CombatManager.singleton.UpdateUI();
            CombatManager.singleton.SelectGameState(CombatManager.GameState.ORDERING, gameObject);
        }
    }

    public override void StartAttack(UnitHandler defender)
    {
        // Bikers may move after attacking provided they have leftover movement
        int tempMovement = movementCur;
        base.StartAttack(defender);
        movementCur = tempMovement;
    }

    public void SpendMovement(int points)
    {
        if (undoLocation == null)
        {
            undoPos = _gridPos;
            undoLocation = _location;
        }
        if (points > movementCur)
        {
            Debug.LogError("[UHBiker:SpendMovement] points is greater than movementCur");
            movementCur = 0;
        }
        else
        {
            movementCur -= points;
        }
    }

    #endregion

    #region monobehaviors
    #endregion

}
