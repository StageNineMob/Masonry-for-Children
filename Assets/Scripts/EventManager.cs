using UnityEngine;
using System.Collections.Generic;
using StageNine.Events;
using StageNine;
using UnityEngine.Events;

public class EventManager : MonoBehaviour {

    public static EventManager singleton;

    public GameObject draggablePuppet;
    public Camera previewCamera;

#if UNITY_STANDALONE
    public KeyCode
    shortcutDeploymentAuto = KeyCode.A,
    shortcutDeploymentRandom =  KeyCode.R,
    shortcutEndTurn = KeyCode.E,
    shortcutMove = KeyCode.V, // more accessible than M, but less intuitive
    shortcutAttack = KeyCode.A,
    shortcutBattleMagic = KeyCode.B,
    shortcutUndo = KeyCode.Z, // more accessible than U, but maybe less intuitive
    shortcutGameplayCancel = KeyCode.C,
    shortcutDeselect = KeyCode.Escape,
    shortcutCycle = KeyCode.Tab,
    shortcutCameraPanRight = KeyCode.RightArrow,
    shortcutCameraPanLeft = KeyCode.LeftArrow,
    shortcutCameraPanUp = KeyCode.UpArrow,
    shortcutCameraPanDown = KeyCode.DownArrow,
    shortcutCameraZoomIn = KeyCode.KeypadPlus,
    shortcutCameraZoomOut = KeyCode.KeypadMinus,
    shortcutToggleDebugPanel = KeyCode.BackQuote,
    shortcutMenuCancel = KeyCode.Escape,
    shortcutOpenMenu = KeyCode.Escape;

    public float pixelPanSpeed = 2f;
    public float keyboardZoomSpeed = 0.1f;
#endif

#if UNITY_IOS || UNITY_ANDROID


#endif

    private List<IEventListener> eventListeners;

    private int lastScreenHeight;
    private int lastScreenWidth;

    [SerializeField] private ModalBlockingLayer _modalBlockingLayer;
    [SerializeField] private GenericErrorPopup _genericErrorPopup;
    [SerializeField] private GenericModalPopup _genericModalPopup;
    [SerializeField] private Generic3ButtonPopup _generic3ButtonPopup;

    private Stack<IModalFocusHolder> controlFocus;

#region public methods

    public bool ConnectListener(IEventListener listener)
    {
        if(!eventListeners.Contains(listener))
        {
            eventListeners.Add(listener);
            return true;
        }
        return false;
    }

    public bool DisconnectListener(IEventListener listener)
    {
        if(eventListeners.Contains(listener))
        {
            eventListeners.Remove(listener);
            return true;
        }
        return false;
    }

    public void Broadcast(StageNine.Events.EventType eType)
    {
        foreach(var listener in eventListeners)
        {
            listener.HandleEvent(eType);
        }
    }

    public void ShowErrorPopup(string panelText, string button1Text, UnityAction button1Delegate, KeyCode button1Shortcut)
    {
        _genericErrorPopup.CustomizePopup(panelText, button1Text, button1Delegate, button1Shortcut);
        GrantFocus(_genericErrorPopup);
    }

    public void ShowDynamicPopup(string panelText, string button1Text, UnityAction button1Delegate, KeyCode button1Shortcut, string button2Text, UnityAction button2Delegate, KeyCode button2Shortcut, int defaultButton, int abortButton)
    {
        _genericModalPopup.CustomizePopup(panelText, button1Text, button1Delegate, button1Shortcut, button2Text, button2Delegate, button2Shortcut, defaultButton, abortButton);
        GrantFocus(_genericModalPopup);
    }

    public void ShowDynamicPopup(string panelText, string button1Text, UnityAction button1Delegate, KeyCode button1Shortcut, string button2Text, UnityAction button2Delegate, KeyCode button2Shortcut, string button3Text, UnityAction button3Delegate, KeyCode button3Shortcut, int defaultButton, int abortButton)
    {
        _generic3ButtonPopup.CustomizePopup(panelText, button1Text, button1Delegate, button1Shortcut, button2Text, button2Delegate, button2Shortcut, button3Text, button3Delegate, button3Shortcut, defaultButton, abortButton);
        GrantFocus(_generic3ButtonPopup);
    }

    public void ResetControlFocus(IModalFocusHolder context)
    {
        //Empty out controlFocus Stack
        controlFocus.Clear();
        //disable the blocking layer
        _modalBlockingLayer.AdjustModalHeight(1);
        //institute a new base
        controlFocus.Push(context);
    }

    public void GrantFocus(ModalPopup popup)
    {
        controlFocus.Push(popup);
        popup.Show(controlFocus.Count);
        _modalBlockingLayer.AdjustModalHeight(controlFocus.Count);
    }

    public void ReturnFocus()
    {
        if (controlFocus.Count < 2)
        {
            if (controlFocus.Count == 1)
            {
                Debug.LogError("[EventManager:ReturnFocus] trying to return focus from the base scene context!?");
            }
            else
            {
                Debug.LogError("[EventManager:ReturnFocus] controlFocus not even initialized properly?!");
            }
            return;
        }
        IModalFocusHolder lastFocus = controlFocus.Pop();
        if(lastFocus is ModalPopup)
        {
            (lastFocus as ModalPopup).Hide();
        }
        _modalBlockingLayer.AdjustModalHeight(controlFocus.Count);
    }

    public bool HasControlFocus(IModalFocusHolder focus)
    {
        return controlFocus.Peek() == focus;
    }

#endregion

#region private methods

    private void CheckScreenResizeEvent()
    {
        if(Screen.height != lastScreenHeight || Screen.width != lastScreenWidth)
        {
            Debug.Log("[EventManager:CheckScreenResizeEvent] Resize detected");
            Broadcast(StageNine.Events.EventType.RESIZE_SCREEN);
            GetScreenSize();
        }
    }

    private void GetScreenSize()
    {
        lastScreenHeight = Screen.height;
        lastScreenWidth = Screen.width;
    }

    private void InitializeManager()
    {
        eventListeners = new List<IEventListener>();
        controlFocus = new Stack<IModalFocusHolder>();
        GetScreenSize();
    }

#endregion

#region monobehaviors

    // Use this for initialization
    void Awake()
    {
        Debug.Log("[EventManager:Awake]");
        if (singleton == null)
        {
            Debug.Log("Event Manager checking in!");
            //DontDestroyOnLoad(gameObject);
            singleton = this;
            InitializeManager();
        }
        else
        {
            Debug.Log("Event Manager checking out!");
            singleton.InitializeManager();
            GameObject.Destroy(gameObject);
        }
    }

    void Start ()
    {
        Debug.Log("[EventManager:Start]");
    }

    // Update is called once per frame
    void Update ()
    {
        CheckScreenResizeEvent();
#if UNITY_STANDALONE
        controlFocus.Peek().KeyboardUpdate();
#else
    #if UNITY_IOS || UNITY_ANDROID
        controlFocus.Peek().TouchScreenUpdate();
    #endif
#endif

    }

#endregion
}
