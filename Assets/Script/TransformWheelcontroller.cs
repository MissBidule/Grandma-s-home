using System;
using UnityEngine;
using UnityEngine.InputSystem;

/*
 * @brief Contains class declaration for TransformWheelcontroller
 * @details The TransformWheelcontroller class manages the transformation wheel UI and handles input to open/close it.
 */
public class TransformWheelcontroller : MonoBehaviour
{
    public static TransformWheelcontroller m_Instance;
    [SerializeField] private Animator m_anim;
    [SerializeField] private TransformPreviewGhost m_previewGhost;
    [SerializeField] private PlayerGhost m_playerGhost;
    [NonSerialized] public GameObject m_selectedPrefab;

    /*
     * @brief Awake is called when the script instance is being loaded
     * Sets the instance and gets the animator if not assigned.
     * @return void
     */
    void Awake()
    {
        m_Instance = this;
        if (m_anim == null)
        {
            m_anim = GetComponent<Animator>();
        }
    }

    /*
     * @brief Update is called every frame
     * Checks for Tab key input to toggle the transformation wheel.
     * @return void
     */
    void Update()
    {
        // New Input System: read Tab key
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            bool open = !m_anim.GetBool("OpenTransformWheel");
            m_anim.SetBool("OpenTransformWheel", open);
        }
    }

    /*
     * @brief Selects the prefab for transformation
     * Sets the selected prefab and activates the preview ghost.
     * @param _prefab: The prefab GameObject to select.
     * @return void
     */
    public void SelectPrefab(GameObject _prefab)
    {
        m_selectedPrefab = _prefab;
        m_previewGhost.SetPreview(_prefab);
        m_previewGhost.gameObject.SetActive(true);
    }

    /*
     * @brief Clears the current selection
     * Resets the selected prefab and deactivates the preview ghost.
     * @return void
     */
    public void ClearSelection()
    {
        m_selectedPrefab = null;
        m_previewGhost.gameObject.SetActive(false);

        UnityEngine.EventSystems.EventSystem eventSystem = UnityEngine.EventSystems.EventSystem.current;
        if (eventSystem != null)
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }
}
