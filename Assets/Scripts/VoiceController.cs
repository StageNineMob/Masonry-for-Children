using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class VoiceController : MonoBehaviour {

    //notes
    //TODO: panels and texts wanted
    //Script on object parenting all of the ui objects so that UI layer can move and scroll appropriately

    //enums

    //subclasses

    //consts and static data
    public static VoiceController singleton;
    public const float DEFAULT_CAMERA_SIZE = 5f;
    //public data

    //private data

    [SerializeField] private GameObject voicePrefab;
    private Stack<GameObject> employedVoices;
    private Stack<GameObject> unemployedVoices;

    //public properties

    //methods
    #region public methods
    public void RequestVoice(Vector3 screenPos, string text)
    {
        var voice = HireVoice();
        employedVoices.Push(voice);
        voice.transform.position = Camera.main.ScreenToWorldPoint(screenPos);
        voice.transform.localScale = Vector3.one;
        voice.transform.GetChild(0).GetComponent<Text>().text = text;
    }

    public void FireAllVoices()
    {
        Debug.Log("[VoiceController:FireAllVoices]");
        while(employedVoices.Count > 0)
        {
            FireVoice(employedVoices.Pop());
        }
    }
    #endregion

    #region private methods
    private GameObject HireVoice()
    {
        //check if there are any off-duty actors in the pool.
        if (unemployedVoices.Count > 0)
        {
            //if there are, pop it off of the pool and return it.
            var newVoice = unemployedVoices.Pop();
            newVoice.SetActive(true);
            return newVoice;
        }
        else
        {
            var newVoice = Instantiate(voicePrefab) as GameObject;
            newVoice.transform.SetParent(transform);
            return newVoice;
        }
    }

    private void FireVoice(GameObject voice)
    {
        voice.SetActive(false);
        unemployedVoices.Push(voice);
    }

    private void InitializeFields()
    {
        employedVoices = new Stack<GameObject>();
        unemployedVoices = new Stack<GameObject>();
    }
    #endregion

    #region monobehaviors
    void Awake()
    {
        Debug.Log("[VoiceController:Awake]");
        if (singleton == null)
        {
            Debug.Log("VoiceController checking in.");
            DontDestroyOnLoad(gameObject);
            singleton = this;
            InitializeFields();
        }
        else
        {
            //if anything needs to be re:init, have this call the singleton.
            Debug.Log("VoiceController checking out.");
            GameObject.Destroy(gameObject);
        }
        // _currentMapFileName = defaultMapFileName;
    }

    #endregion
}
