using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;

public class OptionsMenuPanel : MonoBehaviour {
    //enums

    //subclasses

    //consts and static data

    //public data

    //private data
    [SerializeField] private Toggle battleAnimationsToggle;
    [SerializeField] private Toggle movementAnimationsToggle;
    [SerializeField] private Toggle autoEndTurnToggle;
    [SerializeField] private Slider soundVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    private bool soundVolumeChanged = false;

    [SerializeField] private GameObject parentMenu;

#if UNITY_IOS || UNITY_ANDROID
    private int touches = 0;
#endif

    //public properties

    //methods
    #region public methods
    public void OnBattleAnimationsToggle()
    {

    }

    public void OnSoundVolumeChange()
    {
        soundVolumeChanged = true;
    }

    public void OnMusicVolumeChange()
    {
        MusicPlayer.singleton.PlayMusicVolume(musicVolumeSlider.value);
    }

    public void OKButtonPressed()
    {
        SaveOptions();
        ExitMenu();
    }

    public void CancelButtonPressed()
    {
        MusicPlayer.singleton.PlayMusicVolume(OptionsManager.singleton.musicVolume);
        ExitMenu();
    }

    public void SaveOptions()
    {
        SerializableOptions options = new SerializableOptions();

        options.battleAnimationsEnabled = battleAnimationsToggle.isOn;
        options.movementAnimationsEnabled = movementAnimationsToggle.isOn;
        options.autoEndTurnEnabled = autoEndTurnToggle.isOn;
        options.soundVolume = soundVolumeSlider.value;
        options.musicVolume = musicVolumeSlider.value;

        OptionsManager.singleton.SetOptions(options);
        OptionsManager.singleton.SaveOptions();
    }

    public void PopulateFields()
    {
        SetBattleAnimationToggle(OptionsManager.singleton.battleAnimationsEnabled);
        SetMovementAnimationToggle(OptionsManager.singleton.movementAnimationsEnabled);
        SetAutoEndTurnToggle(OptionsManager.singleton.autoEndTurnEnabled);
        SetSoundVolumeSlider(OptionsManager.singleton.soundVolume);
        soundVolumeChanged = false;
        SetMusicVolumeSlider(OptionsManager.singleton.musicVolume);
    }
    #endregion

    #region private methods
    private void SetBattleAnimationToggle(bool state)
    {
        battleAnimationsToggle.isOn = state;
    }

    private void SetMovementAnimationToggle(bool state)
    {
        movementAnimationsToggle.isOn = state;
    }

    private void SetAutoEndTurnToggle(bool state)
    {
        autoEndTurnToggle.isOn = state;
    }

    private void SetSoundVolumeSlider(float volume)
    {
        soundVolumeSlider.value = volume;
    }

    private void SetMusicVolumeSlider(float volume)
    {
        musicVolumeSlider.value = volume;
    }

    private void ExitMenu()
    {
        if (parentMenu.GetComponent<MainMenu>() != null)
        {
            parentMenu.GetComponent<MainMenu>().BackButtonPressed();
        }
        else if (parentMenu.GetComponent<LimitedOptionsPopup>() != null)
        {
            parentMenu.GetComponent<LimitedOptionsPopup>().BackButtonResponse();
        }
    }

    private void PlayTestSound()
    {
        Debug.Log("[OptionsMenuPanel:PlayTestSound] testSound! DING!");
        AudioManager.singleton.PlayTestSound(soundVolumeSlider.value);
    }
    #endregion

    #region monobehaviors

    void Update()
    {
        if (soundVolumeChanged)
        {
#if UNITY_STANDALONE
            if (!Input.GetMouseButton(0))
            {
                soundVolumeChanged = false;
                PlayTestSound();
            }
#endif
#if UNITY_IOS || UNITY_ANDROID
            if(Input.touchCount < touches && soundVolumeChanged)
            {
                soundVolumeChanged = false;
                PlayTestSound();
            }
            touches = Input.touchCount;
#endif
        }
    }

    #endregion
}
