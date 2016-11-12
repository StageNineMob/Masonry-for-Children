using UnityEngine;
using System.Collections;

public class MusicPlayer : MonoBehaviour {

    //consts and static data
    public static MusicPlayer singleton;

    //private data
    [SerializeField] private AudioClip musicAC;
    private AudioSource audioSource;

    #region public methods
    public void PlayMusicVolume(float volume)
    {
        audioSource.volume = volume;
        if(!audioSource.isPlaying)
        {
            audioSource.Play();
        } 
    }
    #endregion

    #region private methods
    private void Initialize()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = musicAC;
        audioSource.loop = true;
    }
    #endregion


    #region monobehaviours

    // Use this for initialization
    void Awake()
    {
        Debug.Log("[MusicPlayer:Awake]");
        if (singleton == null)
        {
            Debug.Log("MusicPlayer checking in.");
            DontDestroyOnLoad(gameObject);
            singleton = this;
            Initialize();
        }
        else
        {
            //if anything needs to be re:init, have this call the singleton.
            Debug.Log("MusicPlayer checking out.");
            GameObject.Destroy(gameObject);
        }
    }

    void Start()
    {
        //PlayMusicVolume(OptionsManager.singleton.musicVolume);
    }

    // Update is called once per frame
    void Update () {
	
	}

    #endregion
}
