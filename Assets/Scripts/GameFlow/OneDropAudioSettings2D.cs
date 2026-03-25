using System;
using UnityEngine;

namespace WaterBlob
{
    public static class OneDropAudioSettings2D
    {
        private const string MusicVolumeKey = "OneDrop.Audio.MusicVolume";
        private const string SfxVolumeKey = "OneDrop.Audio.SfxVolume";

        private static bool initialized;
        private static float musicVolume = 1f;
        private static float sfxVolume = 1f;

        public static event Action VolumesChanged;

        public static float MusicVolume
        {
            get
            {
                EnsureInitialized();
                return musicVolume;
            }
        }

        public static float SfxVolume
        {
            get
            {
                EnsureInitialized();
                return sfxVolume;
            }
        }

        public static void SetMusicVolume(float value)
        {
            EnsureInitialized();
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(musicVolume, clamped))
            {
                return;
            }

            musicVolume = clamped;
            PlayerPrefs.SetFloat(MusicVolumeKey, musicVolume);
            PlayerPrefs.Save();
            VolumesChanged?.Invoke();
        }

        public static void SetSfxVolume(float value)
        {
            EnsureInitialized();
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Approximately(sfxVolume, clamped))
            {
                return;
            }

            sfxVolume = clamped;
            PlayerPrefs.SetFloat(SfxVolumeKey, sfxVolume);
            PlayerPrefs.Save();
            VolumesChanged?.Invoke();
        }

        public static float ApplyMusic(float baseVolume)
        {
            return Mathf.Clamp01(baseVolume) * MusicVolume;
        }

        public static float ApplySfx(float baseVolume)
        {
            return Mathf.Clamp01(baseVolume) * SfxVolume;
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            musicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MusicVolumeKey, 1f));
            sfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
            initialized = true;
        }
    }
}
