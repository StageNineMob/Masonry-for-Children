using UnityEngine;
using System.Collections;
using System;
using UnityEngine.UI;
using UnityEngine.Events;

public class GenericErrorPopup : ModalPopup
{

    [SerializeField]
    protected Text _panelText, _button1Text;
    [SerializeField]
    protected Button _button1;
    [SerializeField]
    protected KeyCode _button1Shortcut;
    #region public methods

#if UNITY_STANDALONE
    public override void KeyboardUpdate()
    {
        base.KeyboardUpdate();
        if (Input.GetKeyDown(_button1Shortcut))
        {
            _button1.onClick.Invoke();
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

    public void CustomizePopup(string panelText, string button1Text, UnityAction button1Delegate, KeyCode button1Shortcut)
    {
        //TODO: Guard code: Validate Prefab Connections != null
        _panelText.text = panelText;
        _button1Text.text = button1Text;

        _button1.onClick.RemoveAllListeners();
        _button1.onClick.AddListener(button1Delegate);

        _button1Shortcut = button1Shortcut;

        SetDefault(1);
    }

    public override void BackButtonResponse()
    {
        _button1.onClick.Invoke();
    }

    public void ShowDynamic(int height, string panelText, string button1Text, UnityAction button1Delegate, KeyCode button1Shortcut)
    {
        CustomizePopup(panelText, button1Text, button1Delegate, button1Shortcut);
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
            default:
                Debug.LogError("[GenericErrorPopup:CustomizePopup] invalid default button");
                break;
        }
    }

    #endregion

    #region monobehaviors
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    #endregion
}
