using PurrNet;
using PurrNet.Logging;
using UnityEngine;

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
        }

        /*
         * @brief Instantiates the prop assigned to this anchor.
         * @description
         * The prop prefab is instantiated as a child of this anchor transform,
         * ensuring correct position, rotation, and hierarchy organization.
         */
        public void Initialize()
        {
            Instantiate(m_propPrefab, transform);
        }
        
        /*
         * @brief Instantiates the prop assigned to this anchor. The Network version
         * @description
         * The prop prefab is instantiated as a child of this anchor transform,
         * ensuring correct position, rotation, and hierarchy organization.
         */
        public void NetworkInitialize(Transform _parentTransform)
        {
            if (!isServer)
                return;
            
            PurrLogger.Log("Network Prop Initialize");
            UnityProxy.Instantiate(m_propPrefab, _parentTransform);
            //Instantiate(m_propPrefab, transform);
        }
    }
}