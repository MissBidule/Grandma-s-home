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
            public string m_username;
            public string m_roleId;
            public bool m_isGhost;
            public bool m_isLocal;
            public int m_connectionID;
            public bool m_isDisconnected;
        };

        [SerializeField] private List<Role> m_roles = new List<Role>();

        public void AddRole(string _roleId, string _username, bool _isGhost, bool _isLocal)
        {
            m_roles.Add(new Role() { m_roleId = _roleId, m_username = _username, m_isGhost = _isGhost, m_isLocal = _isLocal });
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
                string keepUsername = m_roles[i].m_username;
                if (m_roles[i].m_roleId.Equals(_roleId))
                {
                    m_roles[i] = new Role()
                    {
                        m_roleId = _roleId,
                        m_username = keepUsername,
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

        public string GetUsername(int _connectionID)
        {
            for (int i = 0; i < m_roles.Count; i++)
            {
                if (m_roles[i].m_connectionID == _connectionID)
                {
                    return m_roles[i].m_username;
                }
            }
            return "";
        }

        public string GetMemberID(int _connectionID)
        {
            for (int i = 0; i < m_roles.Count; i++)
            {
                if (m_roles[i].m_connectionID == _connectionID)
                {
                    return m_roles[i].m_roleId;
                }
            }
            return "";
        }

        void Start()
        {
            DontDestroyOnLoad(gameObject);
        }

        public List<string> GetDisconnectedPlayers()
        {
            List <string> disconnectedList = new List<string>();
            foreach (var player in m_roles)
            {
                if (player.m_isDisconnected)
                {
                    disconnectedList.Add(player.m_roleId);
                }
            }
            return disconnectedList;
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

        public string getLocalUsername()
        {
            for (int i = 0; i < m_roles.Count; i++)
            {
                if (m_roles[i].m_isLocal)
                {
                    return m_roles[i].m_username;
                }
            }
            return null;
        }

        public void setConnectionID(string _roleID, int _connectionID)
        {
            for (int i = 0; i < m_roles.Count; i++)
            {
                string keepRoleID = m_roles[i].m_roleId;
                string keepUsername = m_roles[i].m_username;
                bool keepRole = m_roles[i].m_isGhost;
                bool keepLocal = m_roles[i].m_isLocal;
                if (m_roles[i].m_roleId == _roleID)
                {
                    m_roles[i] = new Role()
                    {
                        m_roleId = keepRoleID,
                        m_username = keepUsername,
                        m_isGhost = keepRole,
                        m_isLocal = keepLocal,
                        m_connectionID = _connectionID
                    };
                    break;
                }
            }
        }

        public void setMemberDisconnected(string _roleID)
        {
            for (int i = 0; i < m_roles.Count; i++)
            {
                string keepRoleID = m_roles[i].m_roleId;
                string keepUsername = m_roles[i].m_username;
                bool keepRole = m_roles[i].m_isGhost;
                bool keepLocal = m_roles[i].m_isLocal;
                int keepConnection = m_roles[i].m_connectionID;
                if (m_roles[i].m_roleId == _roleID)
                {
                    m_roles[i] = new Role()
                    {
                        m_roleId = keepRoleID,
                        m_username = keepUsername,
                        m_isGhost = keepRole,
                        m_isLocal = keepLocal,
                        m_connectionID = keepConnection,
                        m_isDisconnected = true
                    };
                    break;
                }
            }
        }
    }
}
