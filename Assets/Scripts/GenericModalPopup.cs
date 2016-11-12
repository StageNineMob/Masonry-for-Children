using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine.Events;

public class GenericModalPopup : ModalPopup {

    [SerializeField] protected Text _panelText, _button1Text, _button2Text;
    [SerializeField] protected Button _button1, _button2, _abortButton;
    [SerializeField] protected KeyCode _button1Shortcut, _button2Shortcut;
    #region public methods

#if UNITY_STANDALONE
    public override void KeyboardUpdate()
    {
        base.KeyboardUpdate();
        if (Input.GetKeyDown(_button1Shortcut))
        {
            _button1.onClick.Invoke();
        }
        if (Input.GetKeyDown(_button2Shortcut))
        {
            _button2.onClick.Invoke();
        }
    }
#endif
#if UNITY_IOS || UNITY_ANDROID
    public override void TouchScreenUpdate()
    {
        var touches = Input.touches;
        if (touches.Length == 1)
        {
            //panning or clicking on tiles
        }
        else if (touches.Length == 2)
        {
            //zooming or something fancy
        }
    }
#endif

    public void CustomizePopup(string panelText, string button1Text, UnityAction button1Delegate, KeyCode button1Shortcut, string button2Text, UnityAction button2Delegate, KeyCode button2Shortcut, int defaultButton, int abortButton)
    {
        //TODO: Guard code: Validate Prefab Connections != null
        _panelText.text = panelText;
        _button1Text.text = button1Text;
        _button2Text.text = button2Text;

        _button1.onClick.RemoveAllListeners();
        _button1.onClick.AddListener(button1Delegate);
        _button2.onClick.RemoveAllListeners();
        _button2.onClick.AddListener(button2Delegate);

        _button1Shortcut = button1Shortcut;
        _button2Shortcut = button2Shortcut;

        SetDefault(defaultButton);
        SetAbort(abortButton);
    }

    public override void BackButtonResponse()
    {
        _abortButton.onClick.Invoke();
    }

    public void ShowDynamic(int height, string panelText, string button1Text, UnityAction button1Delegate, KeyCode button1Shortcut, string button2Text, UnityAction button2Delegate, KeyCode button2Shortcut, int defaultButton, int abortButton)
    {
        CustomizePopup(panelText, button1Text, button1Delegate, button1Shortcut, button2Text, button2Delegate, button2Shortcut, defaultButton, abortButton);
        Show(height);
    }

    #endregion

    #region protected methods

    protected virtual void SetDefault(int defaultButton)
    {
        switch (defaultButton)
        {
            case 1:
                defaultSelection = _button1;
                break;
            case 2:
                defaultSelection = _button2;
                break;
            default:
                Debug.LogError("[GenericModalPopup:CustomizePopup] invalid default button");
                break;
        }
    }

    protected virtual void SetAbort(int abortButton)
    {
        switch (abortButton)
        {
            case 1:
                _abortButton = _button1;
                break;
            case 2:
                _abortButton = _button2;
                break;
            default:
                Debug.LogError("[GenericModalPopup:CustomizePopup] invalid abort button");
                break;
        }
    }

    #endregion

    #region monobehaviors
    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
    #endregion
}
