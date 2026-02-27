using System.Collections.Generic;
using UnityEngine;

namespace Kiqqi.Framework
{
    /// <summary>
    /// Centralized audio controller for all Kiqqi games.
    /// Handles global mute, background music, and common sound effects.
    /// </summary>
    public class KiqqiAudioManager : MonoBehaviour
    {
        [Header("Background Music")]
        [Tooltip("Looped background track to play when game starts.")]
        public AudioClip backgroundMusic;
        [Range(0f, 1f)] public float musicVolume = 0.6f;

        [Header("Common Sound Effects")]
        [Tooltip("Register your sound effects here (id > clip).")]
        public List<AudioEntry> soundEffects = new();

        [System.Serializable]
        public class AudioEntry
        {
            public string id;
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
        }

        private Dictionary<string, AudioEntry> sfxLookup;
        private AudioSource musicSource;
        private AudioSource sfxSource;
        private bool muted;

        // --------------------------------------------------------------
        public void Initialize(KiqqiDataManager data)
        {
            muted = data.GetInt("audio_muted", 0) == 1;

            // Set up AudioSources
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;

            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.loop = false;
            sfxSource.playOnAwake = false;

            // Build dictionary for fast lookup
            sfxLookup = new Dictionary<string, AudioEntry>();
            foreach (var entry in soundEffects)
            {
                if (!string.IsNullOrEmpty(entry.id) && entry.clip != null)
                    sfxLookup[entry.id] = entry;
            }

            ApplyMute();

            // Auto-start background music if assigned
            if (backgroundMusic != null)
                PlayMusic(backgroundMusic, true);

            Debug.Log($"[KiqqiAudioManager] Initialized (muted={muted})");
        }

        // --------------------------------------------------------------
        public void ToggleMute()
        {
            muted = !muted;
            KiqqiAppManager.Instance.Data.SetInt("audio_muted", muted ? 1 : 0);
            ApplyMute();
        }

        private void ApplyMute()
        {
            AudioListener.pause = muted;
            AudioListener.volume = muted ? 0f : 1f;

            if (musicSource)
            {
                musicSource.mute = muted;
                musicSource.volume = muted ? 0f : musicVolume;

                if (!muted && musicSource.clip != null && !musicSource.isPlaying)
                {
                    musicSource.Play();
                }
            }
            if (sfxSource)
            {
                sfxSource.mute = muted;
            }
        }

        // --------------------------------------------------------------
        // Background Music Control
        public void PlayMusic(AudioClip clip, bool loop = true)
        {
            if (!clip) return;

            musicSource.clip = clip;
            musicSource.loop = loop;
            musicSource.volume = musicVolume;
            if (!muted)
                musicSource.Play();
        }

        public void StopMusic()
        {
            if (musicSource && musicSource.isPlaying)
                musicSource.Stop();
        }

        // --------------------------------------------------------------
        // Sound Effect Helpers
        public void PlaySfx(string id)
        {
            if (muted || sfxSource == null) return;
            if (sfxLookup == null || !sfxLookup.TryGetValue(id, out var entry))
            {
                Debug.LogWarning($"[KiqqiAudioManager] No SFX found for id: {id}");
                return;
            }
            sfxSource.PlayOneShot(entry.clip, entry.volume);
        }

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (muted || sfxSource == null || clip == null) return;
            sfxSource.PlayOneShot(clip, volume);
        }
    }
}
