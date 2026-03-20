using System.Collections;
using JamesFrowen.SimpleWeb;
using TMPro;
using UnityEngine;

public class InfoPromptUI : MonoBehaviour
{
    [SerializeField] private Transform m_group;
    [SerializeField] private GameObject m_sabotObject;
    [SerializeField] private GameObject m_repairObject;
    [SerializeField] private GameObject m_caughtObject;
    [SerializeField] private GameObject m_resObject;
    [SerializeField] private float m_fadeDuration = .5f;
    [SerializeField] private float m_VisibleTime = 5f;
    
    /*
    * @brief Displays which ghost sabotage something
    */
    public void GhostSabotage(string _ghostName)
    {
        var newSabot = Instantiate(m_sabotObject);
        var message = _ghostName + " sabotaged an object";
        InstantiateObject(newSabot, message);
    }

    /*
    * @brief Displays which child repaired something
    */
    public void ChildRepair(string _childName)
    {
        var newRepair = Instantiate(m_repairObject);
        var message = _childName + " repaired an object";
        InstantiateObject(newRepair, message);
    }
    
    /*
    * @brief Displays which child caught which ghost
    */
    public void ChildCaughtGhost(string _childName, string _ghostName)
    {
        var newCaught = Instantiate(m_caughtObject);
        var message = _childName + " caught " + _ghostName;
        InstantiateObject(newCaught, message);
    }

    /*
    * @brief Displays which ghost resurrected which ghost
    */
    public void GhostResGhost(string _ghostSavior, string _ghostSaved)
    {
        var newRes = Instantiate(m_resObject);
        var message = _ghostSavior + " freed " + _ghostSaved;
        InstantiateObject(newRes, message);
    }

    private void InstantiateObject(GameObject _gameObject, string _message) {
        _gameObject.SetActive(true);
        _gameObject.transform.SetParent(m_group);
        _gameObject.transform.SetAsFirstSibling();
        _gameObject.GetComponentInChildren<TextMeshProUGUI>().text = _message;
        StartCoroutine(FadeOut(_gameObject));
    }

    /*
    * @brief Fade Out the gameobject
    */
    private IEnumerator FadeOut(GameObject _info)
    {
        yield return new WaitForSeconds(m_VisibleTime);
        CanvasGroup canvasGroup = _info.GetComponent<CanvasGroup>();
        float elapsed = 0f;
        float startAlpha = canvasGroup.alpha;
    
        while (elapsed < m_fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / m_fadeDuration);
            yield return null;
        }
    
        canvasGroup.alpha = 0f;
        Destroy(_info, .5f);
    }
}
