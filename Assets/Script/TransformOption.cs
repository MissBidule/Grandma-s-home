using UnityEngine;

/*
 * @brief Contains class declaration for TransformOption
 * @details The TransformOption class holds data for each transformation option available to the player.
 */
[System.Serializable]
public class TransformOption
{
    public GameObject prefab;
    public Sprite icon;

    /*
     * @brief Constructor for TransformOption
     * Initializes the transform option with a prefab and an optional icon.
     * @param _prefab: The prefab GameObject to store
     * @param _icon: The icon Sprite to display (optional, defaults to null)
     */
    public TransformOption(GameObject _prefab, Sprite _icon = null)
    {
        prefab = _prefab;
        icon = _icon;
    }

    /*
     * @brief Checks if the transform option is empty
     * A transform option is considered empty if it has no prefab or no icon.
     * @return True if the prefab or icon is null, false otherwise
     */
    public bool IsEmpty()
    {
        return prefab == null || icon == null;
    }
}
