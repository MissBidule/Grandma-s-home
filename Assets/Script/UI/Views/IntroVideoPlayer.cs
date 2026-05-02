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
        m_videoPlayer.Prepare();
        yield return new WaitUntil(() => m_videoPlayer.isPrepared);
        m_videoPlayer.Play();
        yield return new WaitForSeconds((float)m_videoPlayer.length);
        IsDone = true;
        m_videoPlayer.Stop();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        gameObject.SetActive(false);
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
