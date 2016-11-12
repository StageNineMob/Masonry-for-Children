using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using StageNine;
using Random = UnityEngine.Random;

public class AnimatedBannerPanel : ModalPopup
{

    private const float BANNER_DISPLAY_TIME = 0.71f;
    //private members
    private const float SCREEN_LEFT = -9f;
    private const float SCREEN_RIGHT = 9f;
    private static bool _animationRunning = false;
    private float startTime, endTime;
    private bool firstFrame = false;
    public const float SCREEN_CENTER = 0f;
    [SerializeField] private Text bannerTextObject;

    private float panelWidth;

#if UNITY_IOS || UNITY_ANDROID
    private int numTouches = 0;
#endif

    public static bool animationRunning
    {
        get
        {
            return _animationRunning;
        }
    }

    //public methods
    #region public methods

#if UNITY_STANDALONE
    public override void KeyboardUpdate()
    {
        base.KeyboardUpdate();
        if (!firstFrame)
        {
            if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            {
                EndAnimation();
            }
        }
        else
        {
            firstFrame = false;
        }
    }
#endif
#if UNITY_IOS || UNITY_ANDROID
    public override void TouchScreenUpdate()
    {
        if (Input.touches.Length > numTouches)
        {
            EndAnimation();
        }
        else
        {
            numTouches = Input.touches.Length;
        }
    }
#endif

    public override void BackButtonResponse()
    {
        EndAnimation();
    }

    public void EndAnimation()
    {
        _animationRunning = false;
        EventManager.singleton.ReturnFocus();
        CombatManager.singleton.LogEndOfPause("Animated banner was displaying");
    }

    public void StartAnimation(float duration, string bannerText)
    {
        if(!_animationRunning)
        {
            EventManager.singleton.GrantFocus(this);
            firstFrame = true;

            Debug.Log("[AnimatedBannerPanel:StartAnimation] Displaying Banner with a duration of " + duration + ", and the text of : " + bannerText);
            //take a duration
            startTime = Time.time;
            endTime = startTime + duration;
            _animationRunning = true;

            bannerTextObject.text = bannerText;
            bannerTextObject.transform.localPosition = new Vector3(-panelWidth, 0, 0);

            StartCoroutine(BannerSlideAnimation());

#if UNITY_IOS || UNITY_ANDROID
            numTouches = Input.touches.Length;
#endif
        }
    }
    #endregion

    #region private methods

    private IEnumerator BannerSlideAnimation()
    {
        float centerTime = (startTime + endTime) * .5f;
        float timeMultiplier = 1f / (centerTime - startTime);
        float curTime = Time.time;
        Vector3 startPos = bannerTextObject.transform.localPosition;
        while(curTime < endTime)
        {
            float tt = (centerTime - curTime) * timeMultiplier;
            tt = tt * tt * tt; //WHY
            bannerTextObject.transform.localPosition = tt * startPos;
            yield return null;
            curTime = Time.time;
        }
    }
    #endregion

    #region monobehaviors
    // Use this for initialization
    void Awake()
    {
        panelWidth = GetComponent<RectTransform>().rect.width;
    }

    // Update is called once per frame
    void Update()
    {
        if (_animationRunning)
        {
            //draw sprites

            if (Time.time > endTime)
                EndAnimation();
            else
            {
            }
        }
    }
    #endregion
}
