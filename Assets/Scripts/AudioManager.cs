using UnityEngine;
using System.Collections;
using StageNine;

public class AudioManager : MonoBehaviour
{
    //enums

    //subclasses

    //consts and static data
    public static AudioManager singleton;
    public const int DEFAULT_SOUND_PRIORITY = 255;
    public const int MAXIMUM_SOUND_PRIORITY = 17;

    //public data
    public RandomSoundPackage hitRSP;
    public RandomSoundPackage artilleryHitRSP;
    public RandomSoundPackage bikerChargeRSP;
    public RandomSoundPackage archerFireRSP;
    public RandomSoundPackage dieRSP;

    //private data
    private int _soundPriority = DEFAULT_SOUND_PRIORITY;

    [SerializeField] private AudioClip hitAC;
    [SerializeField] private float hitPitchLow = 1f;
    [SerializeField] private float hitPitchHigh = 1f;
    [SerializeField] private float hitVolumeLow = 1f;
    [SerializeField] private float hitVolumeHigh = 1f;

    [SerializeField] private AudioClip artilleryHitAC;
    [SerializeField] private float artilleryHitPitchLow = 1f;
    [SerializeField] private float artilleryHitPitchHigh = 1f;
    [SerializeField] private float artilleryHitVolumeLow = 1f;
    [SerializeField] private float artilleryHitVolumeHigh = 1f;

    [SerializeField] private AudioClip bikerChargeAC;
    [SerializeField] private float bikerChargePitchLow = 1f;
    [SerializeField] private float bikerChargePitchHigh = 1f;
    [SerializeField] private float bikerChargeVolumeLow = 1f;
    [SerializeField] private float bikerChargeVolumeHigh = 1f;

    [SerializeField] private AudioClip archerFireAC;
    [SerializeField] private float archerFirePitchLow = 1f;
    [SerializeField] private float archerFirePitchHigh = 1f;
    [SerializeField] private float archerFireVolumeLow = 1f;
    [SerializeField] private float archerFireVolumeHigh = 1f;

    [SerializeField] private AudioClip dieAC;
    [SerializeField] private float diePitchLow = 1f;
    [SerializeField] private float diePitchHigh = 1f;
    [SerializeField] private float dieVolumeLow = 1f;
    [SerializeField] private float dieVolumeHigh = 1f;

    [SerializeField] private AudioClip testAC;
    [SerializeField] private float testVolume = 1f;

    //public properties

    public int soundPriority
    {
        get
        {
            int output = _soundPriority--;
            if(_soundPriority < MAXIMUM_SOUND_PRIORITY)
            {
                _soundPriority = DEFAULT_SOUND_PRIORITY;
            }
            return output;
        }
    }



    //methods
    #region public methods
    public void PlayTestSound(float volume)
    {
        if(testAC != null)
        {
            GetComponent<AudioSource>().PlayOneShot(testAC, testVolume * volume);
        }
        else
        {
            Debug.LogError("[AudioManager:PlayTestSound] testAC not assigned!");
        }
    }

    public void ResetSoundPriority()
    {
        _soundPriority = DEFAULT_SOUND_PRIORITY;
    }

    #endregion

    #region private methods

    private void InitializeFields()
    {
        hitRSP = new RandomSoundPackage(hitAC, hitPitchLow, hitPitchHigh, hitVolumeLow, hitVolumeHigh);
        artilleryHitRSP = new RandomSoundPackage(artilleryHitAC, artilleryHitPitchLow, artilleryHitPitchHigh, artilleryHitVolumeLow, artilleryHitVolumeHigh);
        bikerChargeRSP = new RandomSoundPackage(bikerChargeAC, bikerChargePitchLow, bikerChargePitchHigh, bikerChargeVolumeLow, bikerChargeVolumeHigh);
        archerFireRSP = new RandomSoundPackage(archerFireAC, archerFirePitchLow, archerFirePitchHigh, archerFireVolumeLow, archerFireVolumeHigh);
        dieRSP = new RandomSoundPackage(dieAC, diePitchLow, diePitchHigh, dieVolumeLow, dieVolumeHigh);
    }

    #endregion

    #region monobehaviors

    void Start()
    {

    }

    void Awake()
    {
        Debug.Log("[AudioManager:Awake]");
        if (singleton == null)
        {
            Debug.Log("AudioManager checking in.");
            DontDestroyOnLoad(gameObject);
            singleton = this;
            InitializeFields();
        }
        else
        {
            //if anything needs to be re:init, have this call the singleton.
            Debug.Log("AudioManager checking out.");
            GameObject.Destroy(gameObject);
        }
    }

    #endregion
}
