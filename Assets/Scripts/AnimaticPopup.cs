using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using StageNine;
using Random = UnityEngine.Random;

public class AnimaticPopup : ModalPopup {

    //Enums
    public enum PlacementType
    {
        DEFAULT,
        LINE,
        PACK,
        TRI_FORMATION,
        SCATTERED_LINE
    };

    private const float HEALTH_BAR_ANIMATION_TIME = 1.0f;
    private const float DAMAGE_NUMBER_FLOAT_TIME = 1f;
    private const float DAMAGE_NUMBER_INITIAL_Y = 1f;
    private const float DAMAGE_NUMBER_FLOAT_Y = 2f;
    private const float DAMAGE_NUMBER_ALPHA_MULTIPLIER = 2f;
    private const float SCREEN_WIPE_TIME = 0.71f;
    //private members
    private const int DEFAULT_POOL_SIZE = 17;
    static readonly Vector3 ATTACKER_POS = new Vector3(-4.75f, -.75f, 0f);
    static readonly Vector3 ATTACKER_SCALE = new Vector3(-2f,2f,1f);
    public static readonly Vector3 DEFENDER_POS = new Vector3(4.75f, -.75f, 0f);
    static readonly Vector3 DEFENDER_SCALE = new Vector3(2f, 2f, 1f);
    public static readonly Vector3 GRID_X = new Vector3(1.5f, 0f, 0f);
    static readonly Vector3 GRID_Y = new Vector3(0f, 1.5f, 0f);
    static readonly Vector3 LINE_Y = new Vector3(0f, 0.75f, 0f);
    static readonly Vector3 SCATTERED_LINE_X = new Vector3(1f, 0f, 0f);
    static readonly Vector3 TRI_X = new Vector3(1f, 0f, 0f);
    static readonly Vector3 TRI_Y = new Vector3(0f, 1f, 0f);
    public const int GRID_SIZE = 2;
    public const int LINE_SIZE = 4;
    public const int TRI_SIZE = 4;
    //@deprecated    private const float ATTACK_ANIMATION_TIME = 0.4f;
    private const float DAMAGE_BLINK_TIME = 0.37f;
    public const float MAXIMUM_ANIMATION_DELAY = 0.17f;
    private const float SCREEN_LEFT = -9f;
    private const float SCREEN_RIGHT = 9f;
    public static bool _animationRunning = false;
    private float startTime, endTime;
    public const float SCREEN_CENTER = 0f;
    private bool firstFrame = false;


    [SerializeField] private GameObject actorPrefab;
    [SerializeField] private Color damageFlashA;
    [SerializeField] private Color damageFlashB;
    [SerializeField] private GameObject attackerHealthBar;
    [SerializeField] private GameObject defenderHealthBar;
    [SerializeField] private GameObject battleAnimaticBox;
    [SerializeField] private Text attackerHealthValue;
    [SerializeField] private Text defenderHealthValue;
    [SerializeField] private Text attackerDamageValue;
    [SerializeField] private Text defenderDamageValue;
    [SerializeField] private Image attackerBackground;
    [SerializeField] private Image defenderBackground;

    private Stack<GameObject> attackerActors;
    private Stack<GameObject> defenderActors;
    private Stack<GameObject> extraActors;
    private Stack<GameObject> unemployedActors;
    private int victimIndex = 0;

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

    public void OurInit()
    {
        if (null == unemployedActors)
        {
            attackerActors = new Stack<GameObject>();
            defenderActors = new Stack<GameObject>();
            extraActors = new Stack<GameObject>();
            unemployedActors = new Stack<GameObject>();
            Debug.Log("[AnimaticPopup:OurInit] Initializing actor pool");
            while (unemployedActors.Count < DEFAULT_POOL_SIZE)
            {
                var newActor = CreateActor();
                newActor.SetActive(false);
                unemployedActors.Push(newActor);
            }
            panelWidth = GetComponent<RectTransform>().rect.width;
        }
    }

    public void FindVictim(float direction, Sprite weaponSprite, RandomSoundPackage hitSound, ActorAnimation hitAnimation)
    {
        Stack<GameObject> targets;

        if (direction > 0)
        {
            targets = defenderActors;
        }
        else
        {
            targets = attackerActors;
        }
        if (targets.Count > 0)
        {
            var victim = targets.ToArray()[victimIndex++ % targets.Count];

            StartCoroutine(
            hitAnimation.Play(this, victim, null, hitSound, weaponSprite)
            );
        }
    }

    public override void BackButtonResponse()
    {
        EndAnimation();
    }

    public void EndAnimation()
    {
        _animationRunning = false;
        EventManager.singleton.ReturnFocus();
        CombatManager.singleton.ResolveDamage();
        while (attackerActors.Count > 0)
        {
            FireActor(attackerActors.Pop());
        }
        while (defenderActors.Count > 0)
        {
            FireActor(defenderActors.Pop());
        }
        while (extraActors.Count > 0)
        {
            FireActor(extraActors.Pop());
        }
        CombatManager.singleton.LogEndOfPause("Animatic was running");
    }

    public void StartAnimation(float duration, GameObject attackingUnit, GameObject defendingUnit)
    {
        if (!_animationRunning)
        {
            EventManager.singleton.GrantFocus(this);
            AudioManager.singleton.ResetSoundPriority();
            firstFrame = true;
            Debug.Log("[AnimaticPopup:StartAnimation] starting animatic with a duration of " + duration);
            //take a duration
            _animationRunning = true;
            startTime = Time.time;
            endTime = startTime + duration + SCREEN_WIPE_TIME * 2f;

            transform.localPosition = new Vector3(panelWidth, 0, 0);
            battleAnimaticBox.transform.localPosition = new Vector3(-panelWidth, 0, 0);

            attackerBackground.sprite = attackingUnit.GetComponent<UnitHandler>().location.GetComponent<TileListener>().animaticBackground;
            defenderBackground.sprite = defendingUnit.GetComponent<UnitHandler>().location.GetComponent<TileListener>().animaticBackground;

            var attackerHPStart = (float)attackingUnit.GetComponent<UnitHandler>().curHP / attackingUnit.GetComponent<UnitHandler>().maxHP;
            var defenderHPStart = (float)defendingUnit.GetComponent<UnitHandler>().curHP / defendingUnit.GetComponent<UnitHandler>().maxHP;
            var attackerHPEnd = (float)attackingUnit.GetComponent<UnitHandler>().queuedHP / attackingUnit.GetComponent<UnitHandler>().maxHP;
            var defenderHPEnd = (float)defendingUnit.GetComponent<UnitHandler>().queuedHP / defendingUnit.GetComponent<UnitHandler>().maxHP;
            UnitHandler.SetHealthBar(attackerHealthBar, attackerHPStart);
            UnitHandler.SetHealthBar(defenderHealthBar, defenderHPStart);
            attackerHealthValue.text = attackingUnit.GetComponent<UnitHandler>().curHP.ToString();
            defenderHealthValue.text = defendingUnit.GetComponent<UnitHandler>().curHP.ToString();

            attackerDamageValue.text = attackingUnit.GetComponent<UnitHandler>().queuedDamage.ToString();
            defenderDamageValue.text = defendingUnit.GetComponent<UnitHandler>().queuedDamage.ToString();
            attackerDamageValue.gameObject.SetActive(false);
            defenderDamageValue.gameObject.SetActive(false);

            int attackerCountStart = attackingUnit.GetComponent<UnitHandler>().DetermineActorCount(attackerHPStart);
            //        int attackerCountEnd = attackingUnit.GetComponent<UnitHandler>().DetermineActorCount(attackerHPEnd);

            ChooseUnitPlacement(attackingUnit, attackerCountStart, ATTACKER_POS, ATTACKER_SCALE, attackerActors);

            int defenderCountStart = defendingUnit.GetComponent<UnitHandler>().DetermineActorCount(defenderHPStart);
            //        int defenderCountEnd = defendingUnit.GetComponent<UnitHandler>().DetermineActorCount(defenderHPEnd);
            ChooseUnitPlacement(defendingUnit, defenderCountStart, DEFENDER_POS, DEFENDER_SCALE, defenderActors);

            StartCoroutine(UnitAnimationCoroutine(attackerActors, defenderActors, attackerHPStart, attackerHPEnd, attackingUnit.GetComponent<UnitHandler>(), defenderHPStart, defenderHPEnd, defendingUnit.GetComponent<UnitHandler>(), duration));

#if UNITY_IOS || UNITY_ANDROID
            numTouches = Input.touches.Length;
#endif
        }
    }

    private void ChooseUnitPlacement(GameObject unit, int countStart, Vector3 centerPos, Vector3 actorScale, Stack<GameObject> actorStack)
    {
        switch (unit.GetComponent<UnitHandler>().placementType)
        {
            case AnimaticPopup.PlacementType.LINE:
                {
                    LineUnitPlacement(unit, countStart, centerPos, actorScale, actorStack);
                    break;
                }
            //case AnimaticPopup.PlacementType.PACK:
            //    {
            //        break;
            //    }
            case AnimaticPopup.PlacementType.TRI_FORMATION:
                {
                    TriUnitPlacement(unit, countStart, centerPos, actorScale, actorStack);
                    break;
                }
            case AnimaticPopup.PlacementType.SCATTERED_LINE:
                {
                    ScatteredLineUnitPlacement(unit, countStart, centerPos, actorScale, actorStack);
                    break;
                }
            default:
                {
                    DefaultUnitPlacement(unit, countStart, centerPos, actorScale, actorStack);
                    break;
                }
        }
    }

    private void DefaultUnitPlacement(GameObject unit, int countStart, Vector3 centerPos, Vector3 actorScale, Stack<GameObject> actorStack)
    {
        var positionList = DefaultPositionList();

        for (int ii = 0; ii < countStart; ii++)
        {
            var actor = HireActor();
            actor.GetComponent<Image>().sprite = unit.GetComponent<UnitHandler>().animaticSprite;
            actor.GetComponent<Image>().color = CombatManager.singleton.GetColorOfFaction(unit.GetComponent<UnitHandler>().faction);
            IntVector2 position = positionList[Random.Range(0, positionList.Count)];
            actor.transform.localPosition = centerPos + position.x * GRID_X + position.y * GRID_Y;
            positionList.Remove(position);
            actor.transform.localScale = actorScale;
            actor.transform.rotation = Quaternion.identity;
            actorStack.Push(actor);
        }
    }

    private void LineUnitPlacement(GameObject unit, int countStart, Vector3 centerPos, Vector3 actorScale, Stack<GameObject> actorStack)
    {
        var positionList = LinePositionList();

        for (int ii = 0; ii < countStart; ii++)
        {
            var actor = HireActor();
            actor.GetComponent<Image>().sprite = unit.GetComponent<UnitHandler>().animaticSprite;
            actor.GetComponent<Image>().color = CombatManager.singleton.GetColorOfFaction(unit.GetComponent<UnitHandler>().faction);
            IntVector2 position = positionList[Random.Range(0, positionList.Count)];
            actor.transform.localPosition = centerPos + position.y * LINE_Y;
            positionList.Remove(position);
            actor.transform.localScale = actorScale;
            actor.transform.rotation = Quaternion.identity;
            actorStack.Push(actor);
        }
    }

    private void ScatteredLineUnitPlacement(GameObject unit, int countStart, Vector3 centerPos, Vector3 actorScale, Stack<GameObject> actorStack)
    {
        LineUnitPlacement(unit, countStart, centerPos, actorScale, actorStack);
        foreach(var actor in actorStack)
        {
            actor.transform.position += SCATTERED_LINE_X * Random.Range(-1f, 1f);
        }
    }

    private void TriUnitPlacement(GameObject unit, int countStart, Vector3 centerPos, Vector3 actorScale, Stack<GameObject> actorStack)
    {
        var positionList = TriPositionList();
        var sign = -1f * Mathf.Sign(centerPos.x);

        for (int ii = 0; ii < countStart; ii++)
        {
            var actor = HireActor();
            actor.GetComponent<Image>().sprite = unit.GetComponent<UnitHandler>().animaticSprite;
            actor.GetComponent<Image>().color = CombatManager.singleton.GetColorOfFaction(unit.GetComponent<UnitHandler>().faction);
            // TODO: Can we get it to prioritize the center of the third line for 4, but the edges for 5? Can we do that here or would it be better to do that when generating the positionlist?
            IntVector2 position = positionList[Random.Range(0, countStart - ii)];
            actor.transform.localPosition = centerPos + position.x * TRI_X * sign + position.y * TRI_Y;
            positionList.Remove(position);
            actor.transform.localScale = actorScale;
            actor.transform.rotation = Quaternion.identity;
            actorStack.Push(actor);
        }
    }

    public GameObject RequestExtra()
    {
        GameObject extra = HireActor();
        extraActors.Push(extra);
        return extra;
    }

    #endregion

    #region private methods

    private List<IntVector2> DefaultPositionList()
    {
        List<IntVector2> output = new List<IntVector2>();

        for(int xx = -GRID_SIZE; xx <= GRID_SIZE; ++xx)
        {
            for(int yy = -GRID_SIZE; yy <= GRID_SIZE; ++yy)
            {
                output.Add(new IntVector2(xx, yy));
            }
        }

        return output;
    }

    private List<IntVector2> LinePositionList()
    {
        List<IntVector2> output = new List<IntVector2>();

        for (int yy = -LINE_SIZE; yy <= LINE_SIZE; ++yy)
        {
            output.Add(new IntVector2(0, yy));
        }

        return output;
    }

    private List<IntVector2> TriPositionList()
    {
        List<IntVector2> output = new List<IntVector2>();

        for (int ii = 0; ii < TRI_SIZE; ++ii)
        {
            for (int jj = 0; jj <= ii; ++jj)
            {
                output.Add(new IntVector2(TRI_SIZE-1-(2* ii), ii-(2* jj)));
            }
        }

        return output;
    }

    /*
    private IEnumerator AttackAnimationCoroutine(GameObject actor)
    {
        yield return new WaitForSeconds(Random.Range(0f, MAXIMUM_ANIMATION_DELAY));
        var startPos = actor.transform.position;
        var offsetPos = Vector3.right * -.1f * actor.transform.localScale.x;//flips animation depending on facing
        int count;
        for (count = 0; count < 6; count++)
        {
            switch (count % 4)
            {
                case 0:
                    actor.transform.position += offsetPos;
                    break;
                case 1:
                    actor.transform.position -= offsetPos;
                    break;
                case 2:
                    actor.transform.position -= offsetPos;
                    break;
                case 3:
                    actor.transform.position += offsetPos;
                    break;
            }
            yield return new WaitForSeconds(.05f);
        }
    }
    */
    private IEnumerator DamageBlinkCoroutine(GameObject actor)
    {
        yield return new WaitForSeconds(Random.Range(0f, MAXIMUM_ANIMATION_DELAY));
        var tempColor = actor.GetComponent<Image>().color;
        int count;
        for (count = 0; count < 4; count++)
        {
            switch (count % 2)
            {
                case 0:
                    actor.GetComponent<Image>().color = damageFlashA;
                    break;
                case 1:
                    actor.GetComponent<Image>().color = damageFlashB;
                    break;
            }
            yield return new WaitForSeconds(.05f);
        }
        actor.GetComponent<Image>().color = tempColor;
    }

    private IEnumerator DamageNumberCoroutine(Text damageNumber)
    {
        damageNumber.gameObject.SetActive(true);
        float sumTime = 0f;
        while(sumTime < DAMAGE_NUMBER_FLOAT_TIME)
        {
            damageNumber.color = new Color(0,0,0, DAMAGE_NUMBER_ALPHA_MULTIPLIER * (DAMAGE_NUMBER_FLOAT_TIME - sumTime));
            damageNumber.transform.position = new Vector3(damageNumber.transform.position.x, DAMAGE_NUMBER_INITIAL_Y + DAMAGE_NUMBER_FLOAT_Y * sumTime, damageNumber.transform.position.z);
            sumTime += Time.deltaTime;
            yield return null;
        }
        damageNumber.gameObject.SetActive(false);
    }

    private IEnumerator AnimateDeathCoroutine(GameObject actor, RandomSoundPackage deathSound)
    {
        yield return new WaitForSeconds(Random.Range(0f, MAXIMUM_ANIMATION_DELAY));
        if (deathSound != null)
        {
            deathSound.Play(actor.GetComponent<AudioSource>());
        }
        var velocity = Vector3.right * 8f * actor.transform.localScale.x + Vector3.up;//flips animation depending on facing
        var angularVelocity = actor.transform.localScale.x;
        while (actor.transform.localPosition.x > SCREEN_LEFT && actor.transform.localPosition.x < SCREEN_RIGHT && _animationRunning)
        {
            actor.transform.localPosition += velocity * Time.deltaTime;
            actor.transform.Rotate(Vector3.back, angularVelocity);
            yield return null;
        }
        actor.SetActive(false);
        //FireActor(actor);
    }

    private IEnumerator ScreenWipeTransition()
    {
        float sumTime = 0;
        float lerps = 0;
        var initialPos = transform.localPosition;
        var boxInitialPos = battleAnimaticBox.transform.localPosition;

        while(sumTime < SCREEN_WIPE_TIME)
        {
            sumTime += Time.deltaTime;
            lerps = Mathf.Min(Mathf.Lerp(0, panelWidth, sumTime/SCREEN_WIPE_TIME), panelWidth);
            transform.localPosition = initialPos + Vector3.left * lerps;
            battleAnimaticBox.transform.localPosition = boxInitialPos + Vector3.right * lerps;
            yield return null;
        }
    }

    private IEnumerator UnitAnimationCoroutine(Stack<GameObject> attackers, Stack<GameObject> defenders, float attackerStartHP, float attackerEndHP, UnitHandler attackerUH, float defenderStartHP, float defenderEndHP, UnitHandler defenderUH, float totalTime)
    {
        StartCoroutine(ScreenWipeTransition());
        yield return new WaitForSeconds(SCREEN_WIPE_TIME);

        victimIndex = 0;
        foreach(var actor in attackers)
        {
            StartCoroutine(attackerUH.attackAnimation.Play(this, actor, attackerUH.animaticAttackStartSound, attackerUH.animaticAttackHitSound, attackerUH.weaponSprite));
        }
        yield return new WaitForSeconds(attackerUH.attackAnimation.estimatedDuration);

        StartCoroutine(DamageNumberCoroutine(defenderDamageValue));
        StartCoroutine(UnitHandler.HealthBarAnimationCoroutine(defenderHealthBar, defenderStartHP, defenderEndHP, HEALTH_BAR_ANIMATION_TIME, defenderHealthValue, defenderUH.maxHP));
        foreach (var actor in defenders)
        {
            StartCoroutine(DamageBlinkCoroutine(actor));
        }
        while(defenderActors.Count > defenderUH.DetermineActorCount(defenderEndHP))
        {
            // animation coroutine goes here
            extraActors.Push(defenderActors.Pop());
            StartCoroutine(AnimateDeathCoroutine(extraActors.Peek(), defenderUH.animaticDieSound));
        }
        
        if (attackerEndHP < attackerStartHP)
        {
            yield return new WaitForSeconds(DAMAGE_BLINK_TIME);

            victimIndex = 0;
            foreach (var actor in defenders)
            {
                StartCoroutine(defenderUH.attackAnimation.Play(this, actor, defenderUH.animaticAttackStartSound, defenderUH.animaticAttackHitSound, defenderUH.weaponSprite));
            }
            yield return new WaitForSeconds(defenderUH.attackAnimation.estimatedDuration);

            StartCoroutine(DamageNumberCoroutine(attackerDamageValue));
            StartCoroutine(UnitHandler.HealthBarAnimationCoroutine(attackerHealthBar, attackerStartHP, attackerEndHP, HEALTH_BAR_ANIMATION_TIME, attackerHealthValue, attackerUH.maxHP));
            foreach (var actor in attackers)
            {
                StartCoroutine(DamageBlinkCoroutine(actor));
            }
            while (attackerActors.Count > attackerUH.DetermineActorCount(attackerEndHP))
            {
                // animation coroutine goes here
                extraActors.Push(attackerActors.Pop());
                StartCoroutine(AnimateDeathCoroutine(extraActors.Peek(), attackerUH.animaticDieSound));
            }

            yield return new WaitForSeconds(totalTime - (attackerUH.attackAnimation.estimatedDuration + defenderUH.attackAnimation.estimatedDuration) - DAMAGE_BLINK_TIME);
            
        }
        else
        {
            yield return new WaitForSeconds(totalTime - attackerUH.attackAnimation.estimatedDuration);

        }
        //Debug.Log("[AnimaticPopup: UnitAnimationCoroutine] Reached end of Animation");
        StartCoroutine(ScreenWipeTransition());
    }

    private GameObject HireActor()
    {
        GameObject newActor;
        //check if there are any off-duty actors in the pool.
        if(unemployedActors.Count > 0)
        {
            //if there are, pop it off of the pool and return it.
            newActor = unemployedActors.Pop();
            newActor.SetActive(true);
        }
        else
        {
            newActor = CreateActor();
        }

        newActor.transform.SetParent(battleAnimaticBox.transform);
        attackerDamageValue.transform.SetAsLastSibling();
        defenderDamageValue.transform.SetAsLastSibling();
        newActor.GetComponent<Image>().enabled = true;
        return newActor;
    }

    private GameObject CreateActor()
    {
        var newActor = Instantiate(actorPrefab) as GameObject;
        newActor.GetComponent<RectTransform>().pivot = Vector2.one * 0.5f;
        return newActor;
    }

    private void FireActor(GameObject actor)
    {
        actor.SetActive(false);
        unemployedActors.Push(actor);
    }

    #endregion

    #region monobehaviors
    // Use this for initialization
    void Awake()
    {
        OurInit();
    }


    // Update is called once per frame
    void Update()
    {
        if (_animationRunning)
        {
            //draw sprites

            if (Time.time > endTime)
                EndAnimation();
        }
    }
    #endregion
}
