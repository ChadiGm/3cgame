using System.Collections;
using UnityEngine;

namespace WaterBlob
{
    [DisallowMultipleComponent]
    public class OneDropBackgroundMusic2D : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private OneDropGameManager2D gameManager;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip backgroundMusicClip;

        [Header("Playback")]
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.5f;
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool pauseWithGame = false;
        [SerializeField] private bool stopOnGameOver = true;
        [SerializeField, Min(0f)] private float gameOverFadeOutDuration = 0.5f;

        private bool isGameOver;
        private bool adoptedSceneMusicSource;
        private Coroutine fadeRoutine;

        private void Awake()
        {
            ResolveReferences();
            ConfigureSource();
        }

        private void OnEnable()
        {
            ResolveReferences();
            ConfigureSource();
            SubscribeToGameManager();
            OneDropAudioSettings2D.VolumesChanged += HandleGlobalVolumeChanged;
            ApplyCurrentMusicVolume();

            if (playOnStart)
            {
                PlayMusic();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromGameManager();
            OneDropAudioSettings2D.VolumesChanged -= HandleGlobalVolumeChanged;
            StopFadeRoutine();
        }

        private void OnDestroy()
        {
            UnsubscribeFromGameManager();
            OneDropAudioSettings2D.VolumesChanged -= HandleGlobalVolumeChanged;
            StopFadeRoutine();
        }

        private void Update()
        {
            ApplyCurrentMusicVolume();
        }

        public void PlayMusic()
        {
            if (musicSource == null)
            {
                return;
            }

            if (backgroundMusicClip == null)
            {
                backgroundMusicClip = musicSource.clip;
            }

            if (backgroundMusicClip == null)
            {
                return;
            }

            StopFadeRoutine();
            if (musicSource.clip != backgroundMusicClip)
            {
                musicSource.clip = backgroundMusicClip;
            }

            musicSource.volume = OneDropAudioSettings2D.ApplyMusic(musicVolume);
            if (!musicSource.isPlaying)
            {
                musicSource.Play();
            }
        }

        private void HandlePauseStateChanged(bool paused)
        {
            if (!pauseWithGame || musicSource == null || isGameOver)
            {
                return;
            }

            if (paused)
            {
                musicSource.Pause();
            }
            else if (backgroundMusicClip != null || musicSource.clip != null)
            {
                musicSource.UnPause();
            }
        }

        private void HandleGameOverTriggered(float _)
        {
            isGameOver = true;
            if (!stopOnGameOver || musicSource == null)
            {
                return;
            }

            if (gameOverFadeOutDuration <= 0f)
            {
                musicSource.Stop();
                return;
            }

            StopFadeRoutine();
            fadeRoutine = StartCoroutine(FadeOutAndStop(gameOverFadeOutDuration));
        }

        private IEnumerator FadeOutAndStop(float duration)
        {
            if (musicSource == null)
            {
                yield break;
            }

            float startVolume = musicSource.volume;
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.0001f, duration);

            while (elapsed < safeDuration && musicSource != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                musicSource.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            if (musicSource != null)
            {
                musicSource.Stop();
                musicSource.volume = OneDropAudioSettings2D.ApplyMusic(musicVolume);
            }

            fadeRoutine = null;
        }

        private void ResolveReferences()
        {
            if (gameManager == null)
            {
                gameManager = GetComponent<OneDropGameManager2D>();
                if (gameManager == null)
                {
                    gameManager = FindFirstObjectByType<OneDropGameManager2D>();
                }
            }

            if (musicSource == null)
            {
                musicSource = GetComponent<AudioSource>();
            }

            if (musicSource == null)
            {
                musicSource = FindBestSceneMusicSource();
                adoptedSceneMusicSource = musicSource != null;
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                adoptedSceneMusicSource = false;
            }

            if (backgroundMusicClip == null && musicSource != null && musicSource.clip != null)
            {
                backgroundMusicClip = musicSource.clip;
            }
        }

        private void ConfigureSource()
        {
            if (musicSource == null)
            {
                return;
            }

            if (!adoptedSceneMusicSource)
            {
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }

            musicSource.ignoreListenerPause = true;
            musicSource.volume = OneDropAudioSettings2D.ApplyMusic(musicVolume);
        }

        private AudioSource FindBestSceneMusicSource()
        {
            AudioSource[] allSources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            AudioSource bestSource = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < allSources.Length; i++)
            {
                AudioSource candidate = allSources[i];
                if (candidate == null || candidate == musicSource || candidate.gameObject == gameObject || candidate.clip == null)
                {
                    continue;
                }

                int candidateScore = ScoreCandidate(candidate);
                if (candidateScore > bestScore)
                {
                    bestScore = candidateScore;
                    bestSource = candidate;
                }
            }

            return bestScore >= 20 ? bestSource : null;
        }

        private static int ScoreCandidate(AudioSource candidate)
        {
            int score = 0;
            AudioClip clip = candidate.clip;
            if (clip == null)
            {
                return score;
            }

            string sourceName = candidate.gameObject.name;
            if (!string.IsNullOrEmpty(sourceName))
            {
                string lowered = sourceName.ToLowerInvariant();
                if (lowered.Contains("music") || lowered.Contains("bgm") || lowered.Contains("theme") || lowered.Contains("ambient"))
                {
                    score += 30;
                }
            }

            if (clip.length >= 20f)
            {
                score += 40;
            }
            else if (clip.length >= 8f)
            {
                score += 20;
            }
            else
            {
                score += 5;
            }

            if (candidate.loop)
            {
                score += 25;
            }

            if (candidate.playOnAwake)
            {
                score += 10;
            }

            if (candidate.isPlaying)
            {
                score += 20;
            }

            if (candidate.spatialBlend <= 0.05f)
            {
                score += 5;
            }

            return score;
        }

        private void SubscribeToGameManager()
        {
            if (gameManager == null)
            {
                return;
            }

            gameManager.PauseStateChanged += HandlePauseStateChanged;
            gameManager.GameOverTriggered += HandleGameOverTriggered;
            isGameOver = gameManager.IsGameOver;
        }

        private void UnsubscribeFromGameManager()
        {
            if (gameManager == null)
            {
                return;
            }

            gameManager.PauseStateChanged -= HandlePauseStateChanged;
            gameManager.GameOverTriggered -= HandleGameOverTriggered;
        }

        private void StopFadeRoutine()
        {
            if (fadeRoutine == null)
            {
                return;
            }

            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        private void HandleGlobalVolumeChanged()
        {
            ApplyCurrentMusicVolume();
        }

        private void ApplyCurrentMusicVolume()
        {
            if (musicSource == null || fadeRoutine != null)
            {
                return;
            }

            musicSource.volume = OneDropAudioSettings2D.ApplyMusic(musicVolume);
        }
    }
}
