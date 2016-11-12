using UnityEngine;
using System.Collections;

namespace StageNine
{
    public interface IModalFocusHolder
    {

#if UNITY_STANDALONE
        void KeyboardUpdate();
#endif

#if UNITY_IOS || UNITY_ANDROID
        void TouchScreenUpdate();
#endif
    }
}
