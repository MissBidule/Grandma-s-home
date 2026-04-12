/*
 * @brief This code is used to scroll through the instructions
*/
using UnityEngine;
using TMPro;

public class TutoInstructions : MonoBehaviour
{
    [Header("Prefab UI")]
    [SerializeField] private GameObject m_canvasPrefab;

    [Header("Steps")]
    [SerializeField] private TutorialStep[] m_steps;

    private GameObject m_canvasInstance;
    private TMP_Text m_text;

    private int m_currentStep = 0;
    private float m_timer = 5f;
    public bool m_hasStarted = false;

    void StartTuto()
    {
        m_canvasInstance = Instantiate(m_canvasPrefab);
        m_text = m_canvasInstance.GetComponentInChildren<TMP_Text>();

        if (m_steps == null || m_steps.Length == 0) return;

        //m_currentStep = 0;
        ShowStep();
    }

    void Update()
    {
        if (!m_hasStarted) return;
        if (m_currentStep >= m_steps.Length) return;

        var step = m_steps[m_currentStep];

        if (step.waitForAction) //not yet
        {
            //if (Input.GetButtonDown(step.actionName))
            //{
                NextStep();
            //}
        }
        else
        {
            m_timer += Time.deltaTime;

            if (m_timer >= step.duration)
            {
                NextStep();
            }
        }
    }

    void ShowStep()
    {
        if (m_currentStep >= m_steps.Length) return;
        var step = m_steps[m_currentStep];

        m_text.text = step.message;

        m_timer = 0f;
    }

    void NextStep()
    {
        m_currentStep++;

        if (m_currentStep >= m_steps.Length)
        {
            Destroy(m_canvasInstance);
            return;
        }

        ShowStep();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (m_hasStarted) return;

        if (other.CompareTag("Tuto"))
        {
            m_hasStarted = true;
            Debug.Log("il trouve un tag");
            StartTuto();
        }
    }
}