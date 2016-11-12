using UnityEngine;
using Random = UnityEngine.Random;
using System.Collections;
using System;
using StageNine;

public class UHPawn : UnitHandler
{
    public override ArmyData.UnitType unitType
    {
        get { return ArmyData.UnitType.PAWN; }
    }

    public override ActorAnimation attackAnimation
    {
        get
        {
            return ActorAnimation.meleeAttackAnimation;
        }
    }

    public override RandomSoundPackage animaticAttackHitSound
    {
        get
        {
            return AudioManager.singleton.hitRSP;
        }
    }

    public override RandomSoundPackage animaticDieSound
    {
        get
        {
            return AudioManager.singleton.dieRSP;
        }
    }
    #region public methods

    public override void StartAttack(UnitHandler defender)
    {
        float tempAttack = queuedHP * attackVal * attackModifier * TargetDefense(defender);
        int curAttack = RandomDamage(tempAttack);

        defender.QueueDamage(curAttack); 
        attacksCur--;

        defender.Retaliate(this);
        
        // TODO: Duration shouldn't be a magic number
        CombatManager.singleton.ShowAnimatic(gameObject, defender.gameObject, 3);

        // Can't move after attacking
        movementCur = 0;
    }

    public override void Retaliate(UnitHandler attacker)
    {
        if(queuedHP > 0)
        {
            float tempAttack = queuedHP * attackVal * attackModifier * TargetDefense(attacker);
            int curAttack = RandomDamage(tempAttack);
            attacker.QueueDamage(curAttack);
        }
    }

    public override float GetAttackPower()
    {
        return _curHP * attackVal;
    }

    public override float PredictAttackerDamageOutput(UnitHandler defender)
    {
        float tempAttack = _curHP * attackVal * attackModifier * TargetDefense(defender);
        if (tempAttack < 1)
        {
            return 1;
        }
        return tempAttack;
    }

    public override float PredictDefenderDamageOutput(UnitHandler defender)
    {
        float attackValue = PredictAttackerDamageOutput(defender);
        int lowAttack = Mathf.FloorToInt(attackValue);
        int highAttack = lowAttack + 1;
        float averageDamage = defender.PredictDamageAfterInjury(this, lowAttack) * (highAttack - attackValue);
        averageDamage += defender.PredictDamageAfterInjury(this, highAttack) * (attackValue - lowAttack);
        return averageDamage;
    }

    public override float PredictDamageAfterInjury(UnitHandler defender, int damageTaken)
    {
        if (damageTaken >= _curHP)
        {
            return 0;
        }
        float tempAttack = (_curHP - damageTaken) * attackVal * attackModifier * TargetDefense(defender);
        if (tempAttack < 1)
        {
            return 1;
        }
        return tempAttack;
    }

    #endregion

    #region monobehaviors
    #endregion
}
