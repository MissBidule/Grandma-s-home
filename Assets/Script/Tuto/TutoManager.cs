using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TutoManager : MonoBehaviour
{
    [SerializeField] private GameObject canvas;
    [SerializeField] private TMP_Text text;
    [SerializeField] private Button nextButton;

    [SerializeField] private TutorialStep[] steps;

    private int currentStep = 0;
    private bool waitingForAction = false;

    void Start()
    {
        canvas.SetActive(true);
        nextButton.onClick.AddListener(NextStep);

        ShowStep();
    }

    void Update()
    {
        if (waitingForAction && steps[currentStep].waitForAction)
        {
            if (Input.GetButtonDown(steps[currentStep].actionName)) //je vais me faire taper je sais c pas definitive
            {
                waitingForAction = false;
                nextButton.interactable = true;
            }
        }
    }

    void ShowStep()
    {
        var step = steps[currentStep];

        text.text = step.message;

        if (step.waitForAction)
        {
            waitingForAction = true;
            nextButton.interactable = false;
        }
        else
        {
            waitingForAction = false;
            nextButton.interactable = true;
        }
    }

    public void NextStep()
    {
        currentStep++;

        if (currentStep >= steps.Length)
        {
            canvas.SetActive(false);
            return;
        }

        ShowStep();
    }
}