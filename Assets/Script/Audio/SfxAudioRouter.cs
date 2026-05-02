using System.Collections;
using PurrLobby;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/*
 * @brief Auto-routes every AudioSource in the scene to the GameMixer's SFX or Music group at runtime.
 */
public class SfxAudioRouter : MonoBehaviour
{
    private const float m_periodicInterval = 1.5f;

    private static SfxAudioRouter m_instance;
    private AudioMixerGroup m_sfxGroup;
    private AudioMixerGroup m_musicGroup;
    private float m_periodicTimer;

    /*
     * @brief Creates the singleton router GameObject right after the first scene loads.
     */
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (m_instance != null) return;
        var go = new GameObject("[SfxAudioRouter]");
        DontDestroyOnLoad(go);
        m_instance = go.AddComponent<SfxAudioRouter>();
    }

    /*
     * @brief Loads the GameMixer from Resources, caches the SFX and Music groups, performs the first
     * routing pass, and schedules the initial volume prefs application for the next frame.
     */
    private void Awake()
    {
        var mixer = Resources.Load<AudioMixer>("GameMixer");
        if (mixer == null)
        {
            Debug.LogError("[SfxAudioRouter] GameMixer not found in Resources");
            enabled = false;
            return;
        }
        var sfx = mixer.FindMatchingGroups("SFX");
        var music = mixer.FindMatchingGroups("Music");
        if (sfx.Length == 0 || music.Length == 0)
        {
            Debug.LogError($"[SfxAudioRouter] Missing groups sfx={sfx.Length} music={music.Length}");
            enabled = false;
            return;
        }
        m_sfxGroup = sfx[0];
        m_musicGroup = music[0];
        SceneManager.sceneLoaded += OnSceneLoaded;
        RouteAll();
        StartCoroutine(ApplyPrefsNextFrame());
    }

    /*
     * @brief Waits one frame then re-applies saved volume prefs. AudioMixer.SetFloat does not
     * always take effect during the first RuntimeInitializeOnLoadMethod pass, so this is
     * the standard workaround to make the initial slider values apply on launch.
     * @return IEnumerator for coroutine
     */
    private IEnumerator ApplyPrefsNextFrame()
    {
        yield return null;
        AudioVolumeManager.ApplyFromPrefs();
    }

    /*
     * @brief Unsubscribes from the scene-loaded callback so we do not leak when the router is destroyed.
     */
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /*
     * @brief Re-routes all AudioSources whenever a new scene finishes loading.
     * @param _scene: the scene that was just loaded (unused, required by the SceneManager callback signature)
     * @param _mode: the load mode (unused, required by the SceneManager callback signature)
     */
    private void OnSceneLoaded(Scene _scene, LoadSceneMode _mode) => RouteAll();

    /*
     * @brief Periodically retriggers RouteAll to catch AudioSources spawned at runtime by
     * networked or pooled prefabs that did not exist when the scene loaded. It's not pretty, I know, but it works so please accept the PR
     */
    private void Update()
    {
        m_periodicTimer += Time.deltaTime;
        if (m_periodicTimer >= m_periodicInterval)
        {
            m_periodicTimer = 0f;
            RouteAll();
        }
    }

    /*
     * @brief Iterates every AudioSource in the active scenes and assigns its outputAudioMixerGroup
     * to either the Music group (if the source is nested under a MusicLooper) or the SFX
     * group otherwise. Sources that already have a group are skipped.
     */
    private void RouteAll()
    {
        var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var src in sources)
        {
            if (src.outputAudioMixerGroup != null) continue;
            bool isMusic = src.GetComponentInParent<Script.Audio.MusicLooper>(true) != null;
            src.outputAudioMixerGroup = isMusic ? m_musicGroup : m_sfxGroup;
        }
    }
}
