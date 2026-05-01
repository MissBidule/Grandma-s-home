using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class IntroVideoPlayer : MonoBehaviour
{
    public static IntroVideoPlayer Instance { get; private set; }
    public bool IsDone { get; private set; }

    [SerializeField] private VideoPlayer m_videoPlayer;
    [SerializeField] private RawImage m_screen;

    private void Awake()
    {
        Instance = this;
        IsDone = false;
    }

    private System.Collections.IEnumerator Start()
    {
        m_videoPlayer.Prepare();
        yield return new WaitUntil(() => m_videoPlayer.isPrepared);
        m_videoPlayer.Play();
        yield return new WaitForSeconds((float)m_videoPlayer.length);
        IsDone = true;
        m_videoPlayer.Stop();
        if (m_screen != null) m_screen.color = Color.clear;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
