using UnityEngine;
using System.Collections;
using StageNine;
using UnityEngine.UI;

public abstract class ModalPopup : MonoBehaviour, IModalFocusHolder
{
    // this is used to space out the visible UI elements so the blocking layer and such
    // can be inserted between them
    public const int VISIBLE_CANVAS_INTERVAL = 2;

    [SerializeField] protected Selectable defaultSelection;
    [SerializeField] private Canvas canvas;

#if UNITY_STANDALONE

    public virtual void KeyboardUpdate()
    {
        if (Input.GetKeyDown(EventManager.singleton.shortcutMenuCancel))
        {
            BackButtonResponse();
        }
    }

#endif
#if UNITY_IOS || UNITY_ANDROID
    public abstract void TouchScreenUpdate();
#endif

    /// <summary>
    /// Used for Android back button or Keyboard ESC key.
    /// </summary>
    public abstract void BackButtonResponse();

    public virtual void Show(int height)
    {
        gameObject.SetActive(true);
        if(defaultSelection != null)
        {
            defaultSelection.Select();
        }
        canvas.sortingOrder = VISIBLE_CANVAS_INTERVAL * height;
    }

    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
}
