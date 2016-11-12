using UnityEngine;
using System.Collections.Generic;
using StageNine;
using UnityEngine.Events;
using System;

public class UHBMLump : UHBattlemage {

    //enums

    //subclasses

    public class HundredLumpFist : UnitSpecialAbility
    {
        private GameObject target;

        public override bool buttonInteractable
        {
            get
            {
                return parentUnit.GetComponent<UnitHandler>().GetAttacksCur() > 0;
            }
        }

        #region constructors 
        public HundredLumpFist()
        {
        }

        public HundredLumpFist(GameObject parent)
        {
            hotKey = KeyCode.Alpha1;
            parentUnit = parent;
            buttonLabel = "[1]00";
        }
        #endregion

        #region public methods
        public override void SpecialStateEnter()
        {
            List<GameObject> attackableTiles, attackableUnits;
            target = null;
            parentUnit.GetComponent<UnitHandler>().GetAttackableTilesAndUnits(out attackableTiles, out attackableUnits);

            CombatManager.singleton.EnterSpecialAttackState(attackableTiles, attackableUnits);
        }

        public override void SpecialStateExit()
        {
            MapManager.singleton.RestoreAllTileColors();
            CombatManager.singleton.RestoreUnitHighlights();
        }

        public override void ConfirmStateEnter()
        {
            CombatManager.singleton.HighlightAttackableTilesAndUnits();
            CombatManager.singleton.TargetReticleOn(target);
            CombatManager.singleton.ShowInformationPopup("Omae wa mou Shinderu.", new UnityAction(() =>
            {
                ExecuteAbility();
            }));
        }

        public override void ConfirmStateExit()
        {
            MapManager.singleton.RestoreAllTileColors();
            CombatManager.singleton.HideInformationPopup();
            CombatManager.singleton.TargetReticleOff();
        }

        public override void UnitClick(GameObject unit, CombatManager.GameState gameState)
        {
            if(gameState == CombatManager.GameState.SPECIAL)
            {
                if(CombatManager.singleton.IsAttackable(unit))
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
                if(target == unit)
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
            base.TileClick(tile, gameState);
            target = null;
        }

        public override void ExecuteAbility()
        {
            parentUnit.GetComponent<UnitHandler>().attacksCur -= 1;
            target.GetComponent<UnitHandler>().QueueDamage(100);
            CombatManager.singleton.ResolveDamage();
            CombatManager.singleton.SelectGameState(CombatManager.GameState.ORDERING, parentUnit);
        }

        #endregion
    }

    public class DeathExplosion : UnitSpecialAbility
    {
        private GameObject target;
        private List<GameObject> exploded;

        public override bool buttonInteractable
        {
            get
            {
                return parentUnit.GetComponent<UnitHandler>().GetAttacksCur() > 0;
            }
        }

        #region constructors 
        public DeathExplosion()
        {
        }

        public DeathExplosion(GameObject parent)
        {
            hotKey = KeyCode.Alpha2;
            parentUnit = parent;
            buttonLabel = "[2]DEATH EXPLOSION";
            exploded = new List<GameObject>();
        }
        #endregion

        #region public methods
        public override void SpecialStateEnter()
        {
            List<GameObject> attackableTiles, attackableUnits;
            target = null;
            parentUnit.GetComponent<UnitHandler>().GetAttackableTilesAndUnits(out attackableTiles, out attackableUnits);

            CombatManager.singleton.EnterSpecialAttackState(attackableTiles, attackableUnits);
        }

        public override void SpecialStateExit()
        {
            MapManager.singleton.RestoreAllTileColors();
            CombatManager.singleton.RestoreUnitHighlights();
        }

        public override void ConfirmStateEnter()
        {
            CombatManager.singleton.HighlightAttackableTilesAndUnits();
            CombatManager.singleton.TargetReticleOn(target);
            CombatManager.singleton.ShowInformationPopup("Watashi wa, Exprosion desu~.", new UnityAction(() =>
            {
                ExecuteAbility();
            }));
        }

        public override void ConfirmStateExit()
        {
            MapManager.singleton.RestoreAllTileColors();
            CombatManager.singleton.HideInformationPopup();
            CombatManager.singleton.TargetReticleOff();
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
            base.TileClick(tile, gameState);
            target = null;
        }

        public override void ExecuteAbility()
        {
            parentUnit.GetComponent<UnitHandler>().attacksCur -= 1; //Don't Touch
            target.GetComponent<UnitHandler>().QueueDamage(RandomDamage(9f * target.GetComponent<UnitHandler>().defenseModifier));
            if (!target.GetComponent<UnitHandler>().willBeAlive)
            {
                exploded.Clear();
                exploded.Add(target);
                DeathExplosionPart2(target.GetComponent<UnitHandler>().gridPos);
            }
            CombatManager.singleton.ResolveDamage();
            CombatManager.singleton.SelectGameState(CombatManager.GameState.ORDERING, parentUnit); //Don't Touch
        }

        private void DeathExplosionPart2(IntVector2 center)
        {
            int magicNumberExplosionRadius = 1;
            foreach (var tile in MapManager.singleton.GetTileAt(center).GetComponent<TileListener>().GetTargetableTilesFromTile(magicNumberExplosionRadius, AttackType.MELEE))
            {
                //We need to check if tile is occupied, then check what faction that guy is, then, if he picked the wrong side, bad stuff
                if(tile.GetComponent<TileListener>().occupied &&
                    !exploded.Contains(tile.GetComponent<TileListener>().occupant) && tile.GetComponent<TileListener>().occupant != parentUnit) 
                {
                    GameObject occupant = tile.GetComponent<TileListener>().occupant;
                    occupant.GetComponent<UnitHandler>().QueueDamage( RandomDamage(5f * occupant.GetComponent<UnitHandler>().defenseModifier));

                    //This is for Chain explosions, it needs to avoid null references before it can function properly
                    if (!occupant.GetComponent<UnitHandler>().willBeAlive)
                    {
                        exploded.Add(occupant);
                        DeathExplosionPart2(tile.GetComponent<TileListener>().gridPos);
                    }
                }
            }
            
        }
        #endregion
    }

    public class LumpEyeBeam : UnitSpecialAbility
    {
        private GameObject target;

        public override bool buttonInteractable
        {
            get
            {
                return parentUnit.GetComponent<UnitHandler>().GetAttacksCur() > 0;
            }
        }

        #region constructors 
        public LumpEyeBeam()
        {
        }

        public LumpEyeBeam(GameObject parent)
        {
            hotKey = KeyCode.B;
            parentUnit = parent;
            buttonLabel = "EYE [B]EAM";
        }
        #endregion

        #region public methods
        public override void SpecialStateEnter()
        {
            int maxRange = 5;
            int minRange = 1;
            UnitHandler.AttackType beamType = UnitHandler.AttackType.RANGED_DIRECT;

            List<GameObject> attackableTiles, attackableUnits;
            target = null;
            parentUnit.GetComponent<UnitHandler>().GetAttackableTilesAndUnits(out attackableTiles, out attackableUnits, maxRange, minRange, beamType);

            CombatManager.singleton.EnterSpecialAttackState(attackableTiles, attackableUnits);
        }

        public override void SpecialStateExit()
        {
            MapManager.singleton.RestoreAllTileColors();
            CombatManager.singleton.RestoreUnitHighlights();
        }

        public override void ConfirmStateEnter()
        {
            CombatManager.singleton.HighlightAttackableTilesAndUnits();
            CombatManager.singleton.TargetReticleOn(target);
            CombatManager.singleton.ShowInformationPopup("Lumpy Cannon... and stuff.", new UnityAction(() =>
            {
                ExecuteAbility();
            }));
        }

        public override void ConfirmStateExit()
        {
            MapManager.singleton.RestoreAllTileColors();
            CombatManager.singleton.HideInformationPopup();
            CombatManager.singleton.TargetReticleOff();
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
            base.TileClick(tile, gameState);
            target = null;
        }

        public override void ExecuteAbility()
        {
            parentUnit.GetComponent<UnitHandler>().attacksCur -= 1;
            target.GetComponent<UnitHandler>().QueueDamage( RandomDamage(3.7f * target.GetComponent<UnitHandler>().defenseModifier));
            CombatManager.singleton.ResolveDamage();
            CombatManager.singleton.SelectGameState(CombatManager.GameState.ORDERING, parentUnit);
        }

        #endregion
    }


    //consts and static data

    //public data

    //private data

    //public properties
    public override ArmyData.UnitType unitType
    {
        get { return ArmyData.UnitType.BATTLEMAGE_LUMP; }
    }

    public override ActorAnimation attackAnimation
    {
        get
        {
            return ActorAnimation.meleeAttackAnimation;
        }
    }

    //methods
    #region public methods

    public override void StartAttack(UnitHandler defender)
    {
        defender.QueueDamage(1);
        attacksCur--;
        defender.Retaliate(this);
        
        // TODO: Duration shouldn't be a magic number
        CombatManager.singleton.ShowAnimatic(gameObject, defender.gameObject, 3);

        // Can't move after attacking
        movementCur = 0;
    }

    public override float PredictAttackerDamageOutput(UnitHandler defender)
    {
        return 1f;
    }

    public override float PredictDefenderDamageOutput(UnitHandler defender)
    {
        return defender.PredictDamageAfterInjury(this, 1);
    }

    public override float PredictDamageAfterInjury(UnitHandler defender, int damageTaken)
    {
        if (damageTaken >= _curHP)
        {
            return 0f;
        }
        return 1f;
    }

    public override void Retaliate(UnitHandler attacker)
    {
        if(queuedHP > 0)
            attacker.QueueDamage(1);
    }

    public override float GetAttackPower()
    {
        return 1f;
    }

    public override void OurInit()
    {
        base.OurInit();
        battleMagic = new System.Collections.Generic.List<UnitSpecialAbility>();
        battleMagic.Add(new HundredLumpFist(gameObject));
        battleMagic.Add(new DeathExplosion(gameObject));
        battleMagic.Add(new LumpEyeBeam(gameObject));
    }
    #endregion

    #region private methods

    #endregion

    #region monobehaviors

    #endregion

}
