using FourE.Core;
using FourE.Events;
using FourE.Network;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace FourE.UI
{
    /// <summary>
    /// Gestisce musica, ambiente e SFX della partita collegandosi agli eventi di gioco.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GameAudioController : MonoBehaviour
    {
        private const string AudioRootPath = "Assets/Audio";

        [Header("Audio Sources")]
        [SerializeField] private AudioSource _musicSource;
        [SerializeField] private AudioSource _ambienceSource;
        [SerializeField] private AudioSource _sfxSource;

        [Header("Music & Ambience")]
        [SerializeField] private AudioClip _mainSoundtrack;
        [SerializeField] private AudioClip _crowd;
        [SerializeField] private bool _autoPlayMainSoundtrack = true;
        [SerializeField] private bool _autoPlayCrowdInThisScene;
        [SerializeField] private string[] _crowdSceneNames = { "MainMenu", "CommanderSelection" };

        [Header("Gameplay SFX")]
        [SerializeField] private AudioClip _cash;
        [SerializeField] private AudioClip _campanella;
        [SerializeField] private AudioClip _victory;
        [SerializeField] private AudioClip _over;
        [SerializeField] private AudioClip _card;
        [SerializeField] private AudioClip _verifica;

        private bool _hasPlayedGameOverSfx;

        /// <summary>Istanza persistente del controller audio.</summary>
        public static GameAudioController Instance { get; private set; }

        /// <summary>
        /// Inizializza i canali audio locali e imposta i loop base.
        /// </summary>
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureAudioSources();
            ConfigureSources();
        }

        /// <summary>
        /// Si iscrive agli eventi gameplay e avvia le tracce automatiche.
        /// </summary>
        private void OnEnable()
        {
            EventBus.Subscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Subscribe<VerificaPlayedEvent>(OnVerificaPlayed);
            EventBus.Subscribe<CardBoughtEvent>(OnCardBought);
            EventBus.Subscribe<PhaseChangedEvent>(OnPhaseChanged);
            EventBus.Subscribe<GameStateSyncedEvent>(OnGameStateSynced);
            SceneManager.sceneLoaded += OnSceneLoaded;

            _hasPlayedGameOverSfx = false;
            if (_autoPlayMainSoundtrack)
            {
                PlayLoop(_musicSource, _mainSoundtrack);
            }

            UpdateCrowdForScene(SceneManager.GetActiveScene());
        }

        /// <summary>
        /// Disiscrive tutti gli handler dall'EventBus.
        /// </summary>
        private void OnDisable()
        {
            EventBus.Unsubscribe<CardPlayedEvent>(OnCardPlayed);
            EventBus.Unsubscribe<VerificaPlayedEvent>(OnVerificaPlayed);
            EventBus.Unsubscribe<CardBoughtEvent>(OnCardBought);
            EventBus.Unsubscribe<PhaseChangedEvent>(OnPhaseChanged);
            EventBus.Unsubscribe<GameStateSyncedEvent>(OnGameStateSynced);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// Placeholder per il menu principale: abilita crowd ambience.
        /// </summary>
        public void EnterMainMenuAudioContext()
        {
            PlayLoop(_musicSource, _mainSoundtrack);
            PlayLoop(_ambienceSource, _crowd);
        }

        /// <summary>
        /// Placeholder per la schermata di selezione comandante: mantiene crowd ambience.
        /// </summary>
        public void EnterCommanderSelectionAudioContext()
        {
            PlayLoop(_musicSource, _mainSoundtrack);
            PlayLoop(_ambienceSource, _crowd);
        }

        /// <summary>
        /// Placeholder per l'ingresso in match: mantiene la musica principale e spegne crowd.
        /// </summary>
        public void EnterMatchAudioContext()
        {
            PlayLoop(_musicSource, _mainSoundtrack);
            StopLoop(_ambienceSource);
        }

        /// <summary>
        /// Riproduce l'esito della partita dallo snapshot ricevuto dal trasporto.
        /// Questo garantisce il suono corretto anche sui client non host.
        /// </summary>
        /// <param name="sync">Snapshot sincronizzato della partita.</param>
        private void OnGameStateSynced(GameStateSyncedEvent sync)
        {
            if (!sync.State.IsGameOver)
            {
                _hasPlayedGameOverSfx = false;
                return;
            }

            if (_hasPlayedGameOverSfx)
            {
                return;
            }

            _hasPlayedGameOverSfx = true;
            bool localWon = !sync.State.IsDraw
                && sync.State.WinnerActorNumber == sync.LocalActorNumber;
            PlaySfx(localWon ? _victory : _over);
        }

        /// <summary>
        /// Riproduce il SFX standard quando una carta viene giocata.
        /// </summary>
        /// <param name="evt">Evento carta giocata.</param>
        private void OnCardPlayed(CardPlayedEvent evt)
        {
            PlaySfx(_card);
        }

        /// <summary>
        /// Riproduce il SFX dedicato quando viene giocata una Verifica.
        /// </summary>
        /// <param name="evt">Evento Verifica giocata.</param>
        private void OnVerificaPlayed(VerificaPlayedEvent evt)
        {
            PlaySfx(_verifica);
        }

        /// <summary>
        /// Riproduce il SFX di acquisto carta.
        /// </summary>
        /// <param name="evt">Evento carta acquistata.</param>
        private void OnCardBought(CardBoughtEvent evt)
        {
            PlaySfx(_cash);
        }

        /// <summary>
        /// Riproduce la campanella all'inizio dell'intervallo (ingresso in SHOP).
        /// </summary>
        /// <param name="evt">Evento cambio fase.</param>
        private void OnPhaseChanged(PhaseChangedEvent evt)
        {
            if (evt.Phase == GamePhase.Shop)
            {
                PlaySfx(_campanella);
            }
        }

        /// <summary>
        /// Aggiorna il crowd ambience dopo il caricamento di una scena.
        /// </summary>
        /// <param name="scene">Scena appena caricata.</param>
        /// <param name="mode">Modalita di caricamento.</param>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            UpdateCrowdForScene(scene);
        }

        /// <summary>
        /// Attiva il crowd nel menu e nella selezione comandante, spegnendolo nel match.
        /// </summary>
        /// <param name="scene">Scena di cui valutare il contesto audio.</param>
        private void UpdateCrowdForScene(Scene scene)
        {
            bool shouldPlayCrowd = _autoPlayCrowdInThisScene
                || System.Array.Exists(
                    _crowdSceneNames ?? System.Array.Empty<string>(),
                    sceneName => string.Equals(sceneName, scene.name, System.StringComparison.Ordinal));

            if (shouldPlayCrowd)
            {
                PlayLoop(_ambienceSource, _crowd);
            }
            else
            {
                StopLoop(_ambienceSource);
            }
        }

        /// <summary>
        /// Garantisce la presenza dei tre canali audio richiesti dal controller.
        /// </summary>
        private void EnsureAudioSources()
        {
            if (_musicSource == null)
            {
                _musicSource = gameObject.AddComponent<AudioSource>();
            }

            if (_ambienceSource == null)
            {
                _ambienceSource = gameObject.AddComponent<AudioSource>();
            }

            if (_sfxSource == null)
            {
                _sfxSource = gameObject.AddComponent<AudioSource>();
            }
        }

        /// <summary>
        /// Configura comportamento base dei canali (loop e spatial blend 2D).
        /// </summary>
        private void ConfigureSources()
        {
            _musicSource.loop = true;
            _musicSource.playOnAwake = false;
            _musicSource.spatialBlend = 0f;

            _ambienceSource.loop = true;
            _ambienceSource.playOnAwake = false;
            _ambienceSource.spatialBlend = 0f;

            _sfxSource.loop = false;
            _sfxSource.playOnAwake = false;
            _sfxSource.spatialBlend = 0f;
        }

        /// <summary>
        /// Avvia un loop su una sorgente, se clip e sorgente sono validi.
        /// </summary>
        private static void PlayLoop(AudioSource source, AudioClip clip)
        {
            if (source == null || clip == null)
            {
                return;
            }

            if (source.isPlaying && source.clip == clip)
            {
                return;
            }

            source.clip = clip;
            source.Play();
        }

        /// <summary>
        /// Arresta un loop se in riproduzione.
        /// </summary>
        private static void StopLoop(AudioSource source)
        {
            if (source == null || !source.isPlaying)
            {
                return;
            }

            source.Stop();
        }

        /// <summary>
        /// Riproduce un effetto sonoro one-shot.
        /// </summary>
        private void PlaySfx(AudioClip clip)
        {
            if (_sfxSource == null || clip == null)
            {
                return;
            }

            _sfxSource.PlayOneShot(clip);
        }

        /// <summary>
        /// Rilascia il singleton quando il controller persistente viene distrutto.
        /// </summary>
        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Auto-compila i riferimenti clip quando il componente viene aggiunto dalla Inspector.
        /// </summary>
        private void Reset()
        {
            EnsureAudioSources();
            ConfigureSources();
            AutoAssignClipsIfMissing();
        }

        /// <summary>
        /// Mantiene sincronizzati i riferimenti clip durante l'editing.
        /// </summary>
        private void OnValidate()
        {
            EnsureAudioSources();
            ConfigureSources();
            AutoAssignClipsIfMissing();
        }

        /// <summary>
        /// Tenta il binding automatico dei clip in Assets/Audio se i campi sono vuoti.
        /// </summary>
        private void AutoAssignClipsIfMissing()
        {
            _mainSoundtrack ??= LoadClipByName("main_soundtrack");
            _crowd ??= LoadClipByName("crowd");
            _cash ??= LoadClipByName("cash");
            _campanella ??= LoadClipByName("campanella");
            _victory ??= LoadClipByName("victory");
            _over ??= LoadClipByName("over");
            _card ??= LoadClipByName("card");
            _verifica ??= LoadClipByName("verifica");
        }

        /// <summary>
        /// Cerca un AudioClip per nome file dentro Assets/Audio.
        /// </summary>
        /// <param name="clipName">Nome file senza estensione.</param>
        /// <returns>Clip trovata, altrimenti null.</returns>
        private static AudioClip LoadClipByName(string clipName)
        {
            string[] guids = AssetDatabase.FindAssets($"{clipName} t:AudioClip", new[] { AudioRootPath });
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
#endif
    }
}
