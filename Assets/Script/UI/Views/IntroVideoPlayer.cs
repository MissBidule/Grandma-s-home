using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroVideoPlayer : MonoBehaviour
{
    public static IntroVideoPlayer Instance { get; private set; }
    public bool IsDone { get; private set; }

    [SerializeField] private VideoPlayer m_videoPlayer;
    [SerializeField] private RawImage m_screen;
    public bool m_canLockCursor = false;

    private bool m_errorReceived = false;
    private string m_lastError = string.Empty;

    private void Awake()
    {
        Instance = this;
        IsDone = false;
    }

    private void LateUpdate()
    {
        if (IsDone) return;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private System.Collections.IEnumerator Start()
    {
        // On Linux skip the intro video entirely (avoid broken VideoPlayer on some Linux setups)
        if (Application.platform == RuntimePlatform.LinuxPlayer || Application.platform == RuntimePlatform.LinuxEditor)
        {
            Debug.LogWarning("IntroVideoPlayer: Detected Linux platform — skipping intro video.");
            IsDone = true;
            if (m_canLockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            m_videoPlayer.Stop();
            gameObject.SetActive(false);
            yield break;
        }

        m_videoPlayer.renderMode = VideoRenderMode.APIOnly;
        m_videoPlayer.errorReceived += OnVideoErrorReceived;

        if (m_screen != null)
            m_screen.texture = m_videoPlayer.texture;

        // Route audio if an AudioSource exists on the same GameObject
        var audio = GetComponent<AudioSource>();
        if (audio != null)
        {
            m_videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            m_videoPlayer.SetTargetAudioSource(0, audio);
        }

        m_videoPlayer.Play();

        yield return new WaitForSeconds((float)m_videoPlayer.length);
        IsDone = true;
        m_videoPlayer.Stop();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        gameObject.SetActive(false);
    }

    private void OnVideoErrorReceived(VideoPlayer source, string message)
    {
        m_errorReceived = true;
        m_lastError = message;
        Debug.LogError("VideoPlayer error: " + message);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void SkipVideo()
    {
        StopCoroutine(Start());
        IsDone = true;
        if (m_canLockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        m_videoPlayer.Stop();
        gameObject.SetActive(false);
    }
}
