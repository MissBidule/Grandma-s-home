using UnityEngine;

/*
     * @brief  Contains class declaration for the step of the tutorial
     */

[System.Serializable]
public class TutorialStep
{
    [TextArea] public string message;

    public bool waitForAction; //a retirer

    public string actionName;

    public float duration = 3f;
}