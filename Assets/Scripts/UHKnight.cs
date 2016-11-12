using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using StageNine;
using UnityEngine.Events;
using System;

public class UHKnight : UHPawn 
{
	public class Defend : UnitSpecialAbility, TileModifier.IModifierSource
	{
		public List<TileModifier> modifiers;

		public override bool buttonInteractable
		{
			get
			{
				return parentUnit.GetComponent<UnitHandler>().GetAttacksCur() > 0;
			}
		}

        #region constructors 
		public Defend()
		{
		}

		public Defend(GameObject parent)
		{
            hotKey = KeyCode.D;
			parentUnit = parent;
			buttonLabel = "[D]EFEND";
			modifiers = new List<TileModifier>();
		}
        #endregion

        #region public methods
        public override void SpecialStateEnter ()
		{
            /* TODO: Make a confirmation screen rather than immediately beginning defense. This is dependent on modal popups stuff.
			 * While the confirmation dialog is open, highlight areas that will be defended.
			 */
            CombatManager.singleton.SelectGameState(CombatManager.GameState.CONFIRMING, parentUnit, this);
        }

        public override void ConfirmStateEnter()
        {
            //highlight and focus on defended tiles
            List<GameObject> tileFocusList = new List<GameObject>();
            tileFocusList.Add(MapManager.singleton.GetTileAt(parentUnit.GetComponent<UnitHandler>().gridPos));
            var tiles = tileFocusList[0].GetComponent<TileListener>().neighbors;
            foreach(var tile in tiles)
            {
                tileFocusList.Add(tile.tile);
                tile.tile.GetComponent<TileListener>().HighlightAttack();
            }
            CombatManager.singleton.ShowInformationPopup("This knight will defend this area.", new UnityAction(() =>
            {
                ExecuteAbility();
            }));
            MapManager.singleton.MoveCameraFocus(tileFocusList);
        }

        public override void ConfirmStateExit()
        {
            MapManager.singleton.RestoreAllTileColors();
            CombatManager.singleton.HideInformationPopup();
        }

        public override void ExecuteAbility()
        {
            parentUnit.GetComponent<UHKnight>().BeginDefend();
            CombatManager.singleton.SelectGameState(CombatManager.GameState.ORDERING, parentUnit);
        }

        public void TileEnter(TileModifier triggeringModifier, GameObject triggeringUnit)
		{
            int curAttacks = parentUnit.GetComponent<UnitHandler>().attacksCur;
            int curMove = parentUnit.GetComponent<UnitHandler>().movementCur;
            
			parentUnit.GetComponent<UnitHandler>().StartAttack(triggeringUnit.GetComponent<UnitHandler>());
            
            triggeringUnit.GetComponent<UnitHandler>().SetMovement(0);
            triggeringUnit.GetComponent<UnitHandler>().SetAttacks(0);

            //Knight doesn't spend its turn defending.
            parentUnit.GetComponent<UnitHandler>().SetAttacks(curAttacks);
            parentUnit.GetComponent<UnitHandler>().SetMovement(curMove);
            
            // TODO: Also prevent units from taking miscellaneous actions that have not yet been implemented.
        }

        public bool WillUnitTrigger(GameObject triggeringUnit, bool isAttack)
        {
            //Defend should only affect enemy units, and not enemy attacks
            if(!isAttack && 
                triggeringUnit != null && 
                triggeringUnit.GetComponent<UnitHandler>().faction != parentUnit.GetComponent<UnitHandler>().faction)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

		public void AddModifier(TileModifier newMod)
		{
            if (modifiers != null)
            {
                if (!modifiers.Contains(newMod))
                {
                    modifiers.Add(newMod);
                }
            }
		}
		
		public void RemoveModifier(TileModifier modToRemove)
		{
			modifiers.Remove(modToRemove);
		}

		public void ClearModifiers()
		{
			for(int ii = modifiers.Count-1; ii >= 0; ii--)
			{
				modifiers[ii].Cleanup();
			}
        }
        #endregion
    }

    [Serializable]
    public class AdditionalKnightProperties : SerializableUnit.AdditionalUnitProperties
    {
        public bool isDefending;
    }

    protected bool undoDefending; // was defending prior to moving, for undo purposes
    protected List<TileModifier> defendedLocations;

    [SerializeField] private Sprite shieldIcon;
    [SerializeField] private Color shieldColor;

    private bool _isDefending;

    //properties
	public bool isDefending
	{
		get
		{
			return _isDefending;
		}
		set
		{
			_isDefending = value;
			if(_isDefending)
			{
				// verify we establish modifiers in adjacent tiles
				if( ((Defend)specialAbility).modifiers.Count > 0 )
				{
					((Defend)specialAbility).ClearModifiers();
				}
				foreach(var tile in MapManager.singleton.AdjacentTiles(gridPos))
				{
					new TileModifier(TileModifier.PathingType.STOP, (Defend)specialAbility, tile);
				}
                AddStatus(new StatusIconData(shieldIcon, shieldColor));
			}
			else
			{
				// verify clean up modifiers sourced to this knight
				((Defend)specialAbility).ClearModifiers();
                RemoveStatus(new StatusIconData(shieldIcon, shieldColor));
			}
		}
	}

    public override ArmyData.UnitType unitType
    {
        get { return ArmyData.UnitType.KNIGHT; }
    }



    #region public methods
    public void BeginDefend()
	{
		// Hammer these values to 0 so that the knight can't act afterwards.
		SetMovement(0);
		SetAttacks(0);
		isDefending = true;
	}

	public override void OurInit()
	{
		base.OurInit();
		specialAbility = new Defend(gameObject);
		isDefending = false;
		undoDefending = false;
	}
    //@deprecated
    //public override void MoveToXY(int xx, int yy)
    //{
    //	base.MoveToXY(xx,yy);
    //	// Did they successfully move?
    //	// This assumes they weren't ordered to move to their current location. Is this a safe assumption?
    //	if(gridX == xx && gridY == yy)
    //	{
    //		// Taking an action with a knight breaks their current defense, if active
    //		undoDefending = isDefending;
    //		isDefending = false;
    //	}
    //}

    public override void MoveTo(IntVector2 pos)
    {
        base.MoveTo(pos);
        // Did they successfully move?
        // This assumes they weren't ordered to move to their current location. Is this a safe assumption?
        if (gridPos == pos)
        {
            // Taking an action with a knight breaks their current defense, if active
            undoDefending = isDefending;
            isDefending = false;
        }
    }

    public override void CleanUpModifiers()
    {
        base.CleanUpModifiers();
        isDefending = false;
    }

    public override void UndoMovement()
	{
		if(undoLocation == null)
		{
			// wig out?
			Debug.LogError("no undo location");
		}
		else
		{
			base.UndoMovement();
			// if movement was successfully undone...
			if(undoLocation == null)
			{
				// ...undo defense state change as well
				isDefending = undoDefending;
			}
		}
	}

	public override void StartAttack(UnitHandler defender)
	{
		base.StartAttack(defender);
		// Taking an action with a knight breaks their current defense, if active
		isDefending = false;
    }

    public override SerializableUnit Serialize()
    {
        SerializableUnit data = base.Serialize();
        var properties = new AdditionalKnightProperties();
        properties.isDefending = isDefending;
        data.properties = properties;

        return data;
    }

    public override void LoadDeserializedData(SerializableUnit data)
    {
        base.LoadDeserializedData(data);
        AdditionalKnightProperties properties = (data.properties) as AdditionalKnightProperties;
        if(properties.isDefending)
        {
            isDefending = true;
        }
    }
    #endregion

    #region monobehaviors
    #endregion
}

