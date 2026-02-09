using UnityEngine;

public enum PlayerType
{
    Child,
    Ghost
};

/**
 * @brief       Player informations
 */
public class PlayerBehavior : MonoBehaviour
{
    public PlayerType m_playerType = PlayerType.Child;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
