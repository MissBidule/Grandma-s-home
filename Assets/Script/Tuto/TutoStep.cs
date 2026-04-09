using UnityEngine;

[System.Serializable]
public class TutorialStep
{
    [TextArea] public string message;

    public bool waitForAction; //not yet

    public string actionName;

    public float duration = 3f;
}