using UnityEngine;
using System.Collections;
using System;

public class LimitedOptionsPopup : ModalPopup
{
    public override void BackButtonResponse()
    {
        EventManager.singleton.ReturnFocus();
    }
#if UNITY_IOS || UNITY_ANDROID
    public override void TouchScreenUpdate()
    {
    }
#endif
}
