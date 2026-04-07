using PurrNet;
using PurrNet.Logging;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Script.HouseBuilding
{
    /*
     * @brief Represents a location where a prop can be spawned inside a room.
     * @description
     * Prop anchors define predefined placement points for props. During room
     * population, some of these anchors will be randomly selected to instantiate
     * their assigned prop prefab.
     */
    public class PropAnchor : NetworkBehaviour
    {
        [SerializeField] [Tooltip("Prefab that will be instantiated at this anchor during room generation.")] private GameObject m_propPrefab;

        protected override void OnSpawned()
        {
            base.OnSpawned();
            if (m_propPrefab == null)
                PurrLogger.LogWarning($"PropAnchor {name} (prefab is null)", this);
        }
        
        /*
         * @brief Instantiates the prop assigned to this anchor.
         * @description
         * The prop prefab is instantiated as a child of this anchor transform,
         * ensuring correct position, rotation, and hierarchy organization.
         */
        public void Initialize()
        {
            if (m_propPrefab == null)
            {
                PurrLogger.LogWarning($"PropAnchor {name} Network initialization failed (prefab is null)", this);
                return;
            }
            Instantiate(m_propPrefab, transform);
        }

        public void NetworkInitialize(Transform _parent)
        {
            if (m_propPrefab == null)
            {
                PurrLogger.LogWarning($"PropAnchor {name} Network initialization failed (prefab is null)", this);
                return;
            }
            UnityProxy.Instantiate(m_propPrefab, _parent);
        }
    }
}