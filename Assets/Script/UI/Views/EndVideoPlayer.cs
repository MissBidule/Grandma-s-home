using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class EndVideoPlayer : MonoBehaviour
{
    public static EndVideoPlayer Instance { get; private set; }

    private VideoPlayer m_videoPlayer;
    [SerializeField] private RawImage m_screen;

    [Header("Child Win")]
    [SerializeField] private VideoClip m_childWinIntro;
    [SerializeField] private VideoClip m_childWinLoop;

    [Header("Ghost Win")]
    [SerializeField] private VideoClip m_ghostWinIntro;
    [SerializeField] private VideoClip m_ghostWinLoop;

    private void Awake()
    {
        Instance = this;
        m_videoPlayer = GetComponent<VideoPlayer>();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Play(bool _childWin)
    {
        gameObject.SetActive(true);
        VideoClip intro = _childWin ? m_childWinIntro : m_ghostWinIntro;
        VideoClip loop = _childWin ? m_childWinLoop : m_ghostWinLoop;
        StartCoroutine(PlaySequence(intro, loop));
    }

    private IEnumerator PlaySequence(VideoClip _intro, VideoClip _loop)
    {
        m_videoPlayer.isLooping = false;
        m_videoPlayer.clip = _intro;
        m_videoPlayer.Prepare();
        yield return new WaitUntil(() => m_videoPlayer.isPrepared);
        m_videoPlayer.Play();
        yield return new WaitForSeconds((float)m_videoPlayer.length);

        m_videoPlayer.isLooping = true;
        m_videoPlayer.clip = _loop;
        m_videoPlayer.Prepare();
        yield return new WaitUntil(() => m_videoPlayer.isPrepared);
        m_videoPlayer.Play();
    }
}
