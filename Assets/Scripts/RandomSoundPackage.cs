using UnityEngine;
using System.Collections;

namespace StageNine
{
    public class RandomSoundPackage
    {
        //public data
        public AudioClip clip;
        public float pitchLow;
        public float pitchHigh;
        public float volumeLow;
        public float volumeHigh;
        //private data

        //properties

        //methods

        public RandomSoundPackage()
        {

        }

        public RandomSoundPackage(RandomSoundPackage package)
        {
            clip = package.clip;
            pitchLow = package.pitchLow;
            pitchHigh = package.pitchHigh;
            volumeLow = package.volumeLow;
            volumeHigh = package.volumeHigh;
        }

        public RandomSoundPackage(AudioClip inClip, float inPitchLow, float inPitchHigh, float inVolumeLow, float inVolumeHigh)
        {
            clip = inClip;
            pitchLow = inPitchLow;
            pitchHigh = inPitchHigh;
            volumeLow = inVolumeLow;
            volumeHigh = inVolumeHigh;
        }

        public void Play(AudioSource source)
        {
            source.pitch = Random.Range(pitchLow, pitchHigh);
            source.priority = AudioManager.singleton.soundPriority;
            source.PlayOneShot(clip, Random.Range(volumeLow, volumeHigh) * OptionsManager.singleton.soundVolume);
        }
    }
}
