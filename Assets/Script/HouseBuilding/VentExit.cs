using PurrNet;
using PurrNet.Logging;
using UnityEngine;

namespace Script.HouseBuilding
{
    public class VentExit : NetworkBehaviour
    {
        [SerializeField] [Tooltip("Where the ghost appear on exit")] private Transform m_exitTransform;

        private void Awake()
        {
            if (m_exitTransform == null)
                PurrLogger.LogWarning($"{name} exit transform is null", this);
        }
        
        public Transform GetExitTransform
        {
            get
            {
                return m_exitTransform;
            }
        }
    }
}
