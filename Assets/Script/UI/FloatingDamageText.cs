using UnityEngine;
using TMPro;

/**
@brief       Displays a floating "-XX$" text above a broken object
@details     Spawned locally on each client via Spawn(). The text rises and
             fades out over a fixed duration. Billboard facing the local
             player's camera (same approach as GhostDeathIndicator).
*/
public class FloatingDamageText : MonoBehaviour
{
    private const float Duration  = 2f;
    private const float RiseSpeed = 0.8f;

    private TextMeshPro m_text;
    private float       m_timer;
    private Color       m_baseColor;
    private Transform   m_cameraTransform;

    public static void Spawn(Vector3 worldPos, int value)
    {
        var go = new GameObject("FloatingDamageText");
        go.transform.position = worldPos + Vector3.up * 0.6f;
        go.AddComponent<FloatingDamageText>().Init(value);
    }

    private void Init(int value)
    {
        m_text                   = gameObject.AddComponent<TextMeshPro>();
        m_text.text              = $"-{value}$";
        m_text.fontSize          = 2f;
        m_text.fontStyle         = FontStyles.Bold;
        m_text.alignment         = TextAlignmentOptions.Center;
        m_text.color             = new Color(1f, 0.25f, 0.25f);
        m_baseColor              = m_text.color;
        m_text.isOrthographic    = false;

        foreach (var player in FindObjectsByType<PlayerControllerCore>(FindObjectsSortMode.None))
        {
            if (player.isOwner && player.m_playerCamera != null)
            {
                m_cameraTransform = player.m_playerCamera.transform;
                break;
            }
        }
    }

    private void Update()
    {
        m_timer += Time.deltaTime;

        transform.position += Vector3.up * RiseSpeed * Time.deltaTime;

        if (m_cameraTransform != null)
            transform.rotation = m_cameraTransform.rotation;

        float alpha = Mathf.Clamp01(1f - m_timer / Duration);
        m_text.color = new Color(m_baseColor.r, m_baseColor.g, m_baseColor.b, alpha);

        if (m_timer >= Duration)
            Destroy(gameObject);
    }
}
