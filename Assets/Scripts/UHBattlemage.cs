using UnityEngine;
using System.Collections.Generic;
using StageNine;

public abstract class UHBattlemage : UnitHandler {


    //enums

    //subclasses

    //consts and static data

    //public data
    public List<UnitSpecialAbility> battleMagic;

    //private data

    //protected data

    //public properties
    public bool battleMagicReady
    {
        get
        {
            foreach(var usa in battleMagic)
            {
                if(usa.buttonInteractable)
                {
                    return true;
                }
            }
            return false;
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

    #endregion

    #region private methods

    #endregion

    #region monobehaviors

    #endregion

}
