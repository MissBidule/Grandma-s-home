using System;
using System.Collections.Generic;
using UnityEngine;

namespace PurrLobby
{
    public class RoleKeeper : MonoBehaviour
    {
        [Serializable]
        private struct Role
        {
            public string m_roleId;
            public bool m_isGhost;
        };

        [SerializeField] private List<Role> m_roles = new List<Role>();

        public void AddRole(string _roleId, bool _isGhost)
        {
            m_roles.Add(new Role() { m_roleId = _roleId, m_isGhost = _isGhost });
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
                if (m_roles[i].m_roleId.Equals(_roleId))
                {
                    m_roles[i] = new Role()
                    {
                        m_roleId = _roleId,
                        m_isGhost = _isGhost
                    };
                    break;
                }
            }
        }

        public bool IsGhost(int _playerNb)
        {
            //player Nb starts at 1
            return m_roles[_playerNb - 1].m_isGhost;
        }

        void Start()
        {
            DontDestroyOnLoad(gameObject);
        }
    }
}
