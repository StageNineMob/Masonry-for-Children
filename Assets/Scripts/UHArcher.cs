using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;
using System;
using StageNine;

public class UHArcher : UHPawn {

    //enums

    //subclasses

    //consts and static data

    //public data
    public float retaliationPenalty = 1.0f;

    //private data

    //public properties
    public override ArmyData.UnitType unitType
    {
        get { return ArmyData.UnitType.ARCHER; }
    }

    public override ActorAnimation attackAnimation
    {
        get
        {
            return ActorAnimation.rangedAttackAnimation;
        }
    }

    public override float attackModifier
    {
        get
        {
            if (CombatManager.singleton.currentPlayer != faction)
            {
                // On other player's turns, archers deal reduced damage
                return retaliationPenalty * ComputeTerrainAttackModifier();
            }
            return ComputeTerrainAttackModifier();
        }
    }

    public override RandomSoundPackage animaticAttackStartSound
    {
        get
        {
            return AudioManager.singleton.archerFireRSP;
        }
    }

    protected override float animaticSpritesPerUnit
    {
        get
        {
            return 4f;
        }
    }

    //methods

    #region public methods

    public override void StartAttack(UnitHandler defender)
    {
        float tempAttack = queuedHP * attackVal * attackModifier * TargetDefense(defender);
        int curAttack = RandomDamage(tempAttack);

        defender.QueueDamage(curAttack);
        attacksCur--;

        // TODO: Implement other situations where retaliation can occur?
        // Defender can retaliate only in melee
        if((defender.gridPos - gridPos).magnitude <= 1)
        {
           	defender.Retaliate(this);
        }

        // TODO: Duration shouldn't be a magic number
        CombatManager.singleton.ShowAnimatic(gameObject, defender.gameObject, 3);

        // Can't move after attacking
        movementCur = 0;
    }

    public override float PredictDefenderDamageOutput(UnitHandler defender)
    {
        if ((defender.gridPos - gridPos).magnitude > 1)
        {
            return 0;
        }
        return base.PredictDefenderDamageOutput(defender);
    }

    #endregion

    #region monobehaviors
    #endregion
}
