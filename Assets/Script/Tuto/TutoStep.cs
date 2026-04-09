using UnityEngine;

[System.Serializable]
public class TutorialStep
{
    [TextArea] public string message;

    public bool waitForAction;

    public string actionName;
}