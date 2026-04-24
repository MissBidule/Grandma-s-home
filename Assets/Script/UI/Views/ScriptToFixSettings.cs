using UnityEngine;

//je commente pas l'utilite du script tellement son existence meme est d'une logique deconcertante
public class ScriptToFixSettings : MonoBehaviour
{
    [SerializeField] private SettingsCanvasController settingsCanvasController; //ref pour forcer l'appel de Awake() dans SettingsCanvasController pour init les volumes et eviter les erreurs null au moment du switch de cam (+ bon sang utiliser des prefabs au lieu de tout mettre dans le code. comprendra qui pourra.)

    void Awake()
    {
        settingsCanvasController?.ForceAwake(); 
    }

    void Update()
    {
        
    }
}
