using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using System;

public class ColorSelectionArea : MonoBehaviour, IPointerDownHandler
{

    //enums

    //subclasses

    //consts and static data

    //public data

    //private data
    private bool isDragging = false;

    //public properties

    //methods
    #region public methods
    public void OnPointerDown(PointerEventData eventData)
    {
        isDragging = true;
    }
    #endregion

    #region private methods

    #endregion

    #region monobehaviors
    public void Update()
    {
        if (isDragging)
        {

#if UNITY_STANDALONE
            if (Input.GetMouseButton(0))
            {
                ColorPickerPopup.singleton.PressedColorPicker();
            }
            else
            {
                isDragging = false;
            }
#endif
#if UNITY_ANDROID || UNITY_IOS
            var touches = Input.touches;
            if(touches.Length != 0)
            {
                ColorPickerPopup.singleton.PressedColorPicker();
            }
            else
            {
                isDragging = false;
            }
#endif
        }
    }
    #endregion

}
