using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using System.Collections.Generic;
using System;
using StageNine;

public class UHArtillery : UHArcher
{

    //enums

    //subclasses
    public class ArtilleryAttack : UnitSpecialAbility
    {
        const float PARTIAL_HIGHLIGHT_AMOUNT = 0.65f;

        private GameObject target;

        public override bool buttonInteractable
        {
            get
            {
                return (parentUnit.GetComponent<UnitHandler>().GetAttacksCur() > 0 &&
                   parentUnit.GetComponent<UnitHandler>().GetMovementCur() > 0);
            }
        }

        #region constructors 
        public ArtilleryAttack()
        {
        }

        public ArtilleryAttack(GameObject parent)
        {
            hotKey = KeyCode.A;
            parentUnit = parent;
            buttonLabel = "[A]TTACK";
        }
        #endregion

        #region public methods
        public override void SpecialStateEnter()
        {
            /* TODO: Make a confirmation screen rather than immediately beginning defense. This is dependent on modal popups stuff.
			 * While the confirmation dialog is open, highlight areas that will be defended.
			 */
            UnitHandler puuh = parentUnit.GetComponent<UnitHandler>();
            puuh.ClearTarget();
            List<GameObject> tiles, units;
            puuh.GetAttackableTilesAndUnits(out tiles, out units, true);
            CombatManager.singleton.EnterSpecialAttackState(tiles, units);
        }

        public override void SpecialStateExit()
        {
            MapManager.singleton.RestoreAllTileColors();
            CombatManager.singleton.RestoreUnitHighlights();
        }


        public override void ConfirmStateEnter()
        {
            GameObject tile = target;
            if (target.GetComponent<UnitHandler>() != null)
            {
                tile = MapManager.singleton.GetTileAt(target.GetComponent<UnitHandler>().gridPos);
            }
            CombatManager.singleton.PartialHighlightAttackableTiles(PARTIAL_HIGHLIGHT_AMOUNT);
            CombatManager.singleton.HighlightSplash(tile, SPLASH_RADIUS);
            CombatManager.singleton.TargetReticleOn(target);
            parentUnit.GetComponent<UHArtillery>().DisplaySplashDamage(target);
            CombatManager.singleton.ShowInformationPopup("This attack will damage these units.", new UnityAction(() =>
            {
                ExecuteAbility();
            }));
        }

        public override void ConfirmStateExit()
        {
            MapManager.singleton.RestoreAllTileColors();
            CombatManager.singleton.HideInformationPopup();
            CombatManager.singleton.TargetReticleOff();
            CombatManager.singleton.RestoreUnitHighlights();
            VoiceController.singleton.FireAllVoices();
        }

        public override void UnitClick(GameObject unit, CombatManager.GameState gameState)
        {
            if (gameState == CombatManager.GameState.SPECIAL)
            {
                if (CombatManager.singleton.IsAttackable(unit))
                {
                    target = unit;
                    CombatManager.singleton.SelectGameState(CombatManager.GameState.CONFIRMING, parentUnit, this);
                }
                else
                {
                    target = null;
                }
            }
            else
            {
                if (target == unit)
                {
                    ExecuteAbility();
                }
                else if (CombatManager.singleton.IsAttackable(unit))
                {
                    target = unit;
                    CombatManager.singleton.SelectGameState(CombatManager.GameState.CONFIRMING, parentUnit, this);
                }
                else
                {
                    target = null;
                    CombatManager.singleton.SelectGameState(CombatManager.GameState.SPECIAL, parentUnit, this);
                }
            }
        }

        public override void TileClick(GameObject tile, CombatManager.GameState gameState)
        {
            UnitClick(tile, gameState);
        }

        public override void ExecuteAbility()
        {
            UnitHandler targetUnit = target.GetComponent<UnitHandler>();
            if (targetUnit != null)
            {
                parentUnit.GetComponent<UnitHandler>().StartAttack(targetUnit);
            }
            else
            {
                parentUnit.GetComponent<UHArtillery>().SplashDamage(target.GetComponent<TileListener>().gridPos);
                parentUnit.GetComponent<UnitHandler>().attacksCur--;
                parentUnit.GetComponent<UnitHandler>().movementCur = 0;
                CombatManager.singleton.ResolveDamage();
            }
            CombatManager.singleton.SelectGameState(CombatManager.GameState.ORDERING, parentUnit);
        }

        #endregion
    }

    //consts and static data
    const int SPLASH_RADIUS = 1;
    //public data

    //private data
    [SerializeField] protected float highSplashVal = 0.2f;
    [SerializeField] protected float lowSplashVal = 0.05f;
    //public properties
    public override ArmyData.UnitType unitType
    {
        get { return ArmyData.UnitType.ARTILLERY; }
    }

    public override ActorAnimation attackAnimation
    {
        get
        {
            return ActorAnimation.artilleryAttackAnimation;
        }
    }

    public override bool disableAttackButton
    {
        get
        {
            return true;
        }
    }

    public override RandomSoundPackage animaticAttackHitSound
    {
        get
        {
            return AudioManager.singleton.artilleryHitRSP;
        }
    }

    protected override float animaticSpritesPerUnit
    {
        get
        {
            return 1f;
        }
    }

    //methods

    #region public methods

    public override void StartAttack(UnitHandler defender)
    {
        SplashDamage(defender.gridPos);
        base.StartAttack(defender);
    }

    public override void OurInit()
    {
        base.OurInit();
        specialAbility = new ArtilleryAttack(gameObject);
    }

    private void SplashDamage(IntVector2 center)
    {
        float lowSplash = curHP * lowSplashVal;
        float highSplash = curHP * highSplashVal;
        int distance = (center - _gridPos).magnitude;
        foreach (var tile in MapManager.singleton.GetTileAt(center).GetComponent<TileListener>().GetTargetableTilesFromTile(SPLASH_RADIUS, AttackType.MELEE))
        {
            //We need to check if tile is not the center (center will take main damage), then if tile is occupied, then if tile is closer or further than the center
            if (tile.GetComponent<TileListener>().gridPos != center && tile.GetComponent<TileListener>().occupied)
            {
                GameObject occupant = tile.GetComponent<TileListener>().occupant;

                int splash;
                if((tile.GetComponent<TileListener>().gridPos - _gridPos).magnitude > distance)
                {
                    splash = RandomDamage(highSplash * TargetDefense(occupant.GetComponent<UnitHandler>()));
                }
                else
                {
                    splash = RandomDamage(lowSplash * TargetDefense(occupant.GetComponent<UnitHandler>()));
                }
                //now we wanna queue damage and stuff
                occupant.GetComponent<UnitHandler>().QueueDamage(splash);
            }
        }

    }

    public void DisplaySplashDamage(GameObject target)
    {
        IntVector2 center;
        UnitHandler targetUnit = target.GetComponent<UnitHandler>();
        if(targetUnit == null)
        {
            center = target.GetComponent<TileListener>().gridPos;
        }
        else
        {
            center = targetUnit.gridPos;
        }

        float lowSplash = curHP * lowSplashVal;
        float highSplash = curHP * highSplashVal;
        float fullDamage = curHP * attackVal;
        int distance = (center - _gridPos).magnitude;
        foreach (var tile in MapManager.singleton.GetTileAt(center).GetComponent<TileListener>().GetTargetableTilesFromTile(SPLASH_RADIUS, AttackType.MELEE))
        {
            //We need to check if tile is occupied, if it's the primary target, and if not, if it's closer or further than the center
            if (tile.GetComponent<TileListener>().occupied)
            {
                GameObject occupant = tile.GetComponent<TileListener>().occupant;

                float splash;
                if(occupant == target)
                {
                    splash = Mathf.Max(fullDamage * TargetDefense(occupant.GetComponent<UnitHandler>()), 1f);
                }
                else if ((tile.GetComponent<TileListener>().gridPos - _gridPos).magnitude > distance)
                {
                    splash = Mathf.Max(highSplash * TargetDefense(occupant.GetComponent<UnitHandler>()), 1f);
                }
                else
                {
                    splash = Mathf.Max(lowSplash * TargetDefense(occupant.GetComponent<UnitHandler>()),1f);
                }
                //now we request voices to display these damage predictions
                VoiceController.singleton.RequestVoice(Camera.main.WorldToScreenPoint(occupant.transform.position), splash.ToString("0.00"));
            }
        }

    }

    public override List<GameObject> GetThreatTiles()
    {
        return GetAttackableTiles(attackRangeMax, attackRangeMin, attackType);
    }

    public override bool TryMoveTo(IntVector2 pos)
    {
        if (base.TryMoveTo(pos))
        {
            attacksCur = 0;
            return true;
        }
        return false;
    }

    public override bool CanUndoMovement()
    {
        if (undoLocation != null &&
           !specialCannotUndo)
        {
            return true;
        }
        return false;
    }

    public override void UndoMovement()
    {
        attacksCur = attacksMax;
        base.UndoMovement();
    }

    #endregion

    #region private methods

    protected override float TargetDefense(UnitHandler target)
    {
        if(target.defenseModifier > 1f)
        {
            return (target.defenseModifier - 1f) * 2 + 1f;
        }
        if(target.defenseModifier < 1f)
        {
            return target.defenseModifier * target.defenseModifier;
        }
        return 1f;
    }

    #endregion

    #region monobehaviors

    #endregion
}
