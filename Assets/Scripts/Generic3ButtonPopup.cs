using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Events;

public class Generic3ButtonPopup : GenericModalPopup
{
    [SerializeField] protected Text _button3Text;
    [SerializeField] protected Button _button3;
    [SerializeField] protected KeyCode _button3Shortcut;

    #region public methods

#if UNITY_STANDALONE
    public override void KeyboardUpdate()
    {
        base.KeyboardUpdate();
        if (Input.GetKeyDown(_button3Shortcut))
        {
            _button3.onClick.Invoke();
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

    public void CustomizePopup(string panelText, string button1Text, UnityAction button1Delegate, KeyCode button1Shortcut, string button2Text, UnityAction button2Delegate, KeyCode button2Shortcut, string button3Text, UnityAction button3Delegate, KeyCode button3Shortcut, int defaultButton, int abortButton)
    {
        //TODO: Guard code: Validate Prefab Connections != null
        base.CustomizePopup(panelText, button1Text, button1Delegate, button1Shortcut, button2Text, button2Delegate, button2Shortcut, defaultButton, abortButton);
        _button3Text.text = button3Text;

        _button3.onClick.RemoveAllListeners();
        _button3.onClick.AddListener(button3Delegate);

        _button3Shortcut = button3Shortcut;
    }

    public void ShowDynamic(int height, string panelText, string button1Text, UnityAction button1Delegate, KeyCode button1Shortcut, string button2Text, UnityAction button2Delegate, KeyCode button2Shortcut, string button3Text, UnityAction button3Delegate, KeyCode button3Shortcut, int defaultButton, int abortButton)
    {
        CustomizePopup(panelText, button1Text, button1Delegate, button1Shortcut, button2Text, button2Delegate, button2Shortcut, button3Text, button3Delegate, button3Shortcut, defaultButton, abortButton);
        Show(height);
    }

    #endregion

    #region protected methods

    protected override void SetDefault(int defaultButton)
    {
        switch (defaultButton)
        {
            case 1:
                defaultSelection = _button1;
                break;
            case 2:
                defaultSelection = _button2;
                break;
            case 3:
                defaultSelection = _button3;
                break;
            default:
                Debug.LogError("[GenericModalPopup:CustomizePopup] invalid default button");
                break;
        }
    }

    protected override void SetAbort(int abortButton)
    {
        switch (abortButton)
        {
            case 1:
                _abortButton = _button1;
                break;
            case 2:
                _abortButton = _button2;
                break;
            case 3:
                _abortButton = _button3;
                break;
            default:
                Debug.LogError("[GenericModalPopup:CustomizePopup] invalid abort button");
                break;
        }
    }

    #endregion

    // Use this for initialization
    void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
