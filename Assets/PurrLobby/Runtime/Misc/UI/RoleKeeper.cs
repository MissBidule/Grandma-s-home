using System;
using System.Collections.Generic;
using UnityEngine;

namespace PurrLobby
{
    /*
    * @brief  Contains class declaration for RoleKeeper
    * @details Script that handles the data processed during the lobby step of the game
    */
    public class RoleKeeper : MonoBehaviour
    {
        [Serializable]
        private struct Role
        {
            public string m_roleId;
            public bool m_isGhost;
            public bool m_isLocal;
            public int m_connectionID;
        };

        [SerializeField] private List<Role> m_roles = new List<Role>();

        public void AddRole(string _roleId, bool _isGhost, bool _isLocal)
        {
            m_roles.Add(new Role() { m_roleId = _roleId, m_isGhost = _isGhost, m_isLocal = _isLocal });
        }
        
        public void RemoveRole(string _roleId)
        {
            foreach (Role role in m_roles)
            {
                if (role.m_roleId == _roleId) {
                    m_roles.Remove(role);
                    return;
                }
            }
        }

        public void SwitchRole(string _roleId, bool _isGhost)
        {
            for (int i = 0; i < m_roles.Count; i++)
            {
                bool keepLocal = m_roles[i].m_isLocal;
                if (m_roles[i].m_roleId.Equals(_roleId))
                {
                    m_roles[i] = new Role()
                    {
                        m_roleId = _roleId,
                        m_isGhost = _isGhost,
                        m_isLocal = keepLocal
                    };
                    break;
                }
            }
        }

        public bool IsGhost(int _connectionID)
        {
            for (int i = 0; i < m_roles.Count; i++)
            {
                if (m_roles[i].m_connectionID == _connectionID)
                {
                    return m_roles[i].m_isGhost;
                }
            }
            return false;
        }

        void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void DeleteList()
        {
            m_roles = new List<Role>();
        }

        public string getLocalMemberID()
        {
            for (int i = 0; i < m_roles.Count; i++)
            {
                if (m_roles[i].m_isLocal)
                {
                    return m_roles[i].m_roleId;
                }
            }
            return null;
        }

        public void setConnectionID(string _roleID, int _connectionID)
        {
            for (int i = 0; i < m_roles.Count; i++)
            {
                string keepRoleID = m_roles[i].m_roleId;
                bool keepRole = m_roles[i].m_isGhost;
                if (m_roles[i].m_roleId == _roleID)
                {
                    m_roles[i] = new Role()
                    {
                        m_roleId = keepRoleID,
                        m_isGhost = keepRole,
                        m_connectionID = _connectionID
                    };
                    break;
                }
            }
        }
    }
}
