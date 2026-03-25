using System;
using System.Collections.Generic;
using System.Linq;
using PurrNet.Logging;
using UnityEngine;
using UnityEngine.UI;

namespace PurrLobby
{
    public class LobbyMemberList : MonoBehaviour
    {
        [SerializeField] private MemberEntry memberEntryPrefab;
        [SerializeField] private Transform content;
        [SerializeField] private Button readyButton;
        private bool m_isSomeoneInGame = false;
        private RoleKeeper m_roleKeeper;
        private bool m_lastInGameState = false;

        void Start()
        {
            m_roleKeeper = FindAnyObjectByType<RoleKeeper>();  
        }

        public void LobbyDataUpdate(Lobby room)
        {
            if(!room.IsValid)
                return;

            HandleExistingMembers(room);
            HandleNewMembers(room);
            HandleLeftMembers(room);
            HandleInGameLock(room);
        }

        public void OnLobbyLeave()
        {
            m_roleKeeper.DeleteList();
            foreach (Transform child in content)
                Destroy(child.gameObject);
        }

        private void HandleExistingMembers(Lobby room)
        {
            foreach (Transform child in content)
            {
                if (!child.TryGetComponent(out MemberEntry member))
                    continue;

                var matchingMember = room.Members.Find(x => x.Id == member.MemberId);
                if (!string.IsNullOrEmpty(matchingMember.Id))
                {
                    member.SetReady(matchingMember.IsReady);
                    member.SetRole(matchingMember.IsGhost);
                    HandleHostOptions(member, room);
                }
            }
        }

        private void HandleHostOptions(MemberEntry _member, Lobby _room)
        {
            if (_member.SetHost())
            {
                FindAnyObjectByType<ViewManager>().showHostObjects(true);
                int readyMembers = _room.Members.Count(x => x.IsReady);
                if (readyMembers < _room.Members.Count - 1)
                {
                    _member.LockReady(true);
                }
                else if (readyMembers == _room.Members.Count - 1)
                {
                    _member.LockReady(false);
                }
            }
        }

        private async void HandleNewMembers(Lobby room)
        {
            var existingMembers = content.GetComponentsInChildren<MemberEntry>();
    
            foreach (var member in room.Members)
            {
                if (Array.Exists(existingMembers, x => x.MemberId == member.Id))
                    continue;

                var entry = Instantiate(memberEntryPrefab, content);
                entry.readyButton = readyButton;
                entry._lobbyManager = FindAnyObjectByType<LobbyManager>();
                entry._ownId = await entry._lobbyManager.GetPlayer();
                entry.Init(member);
                m_roleKeeper.AddRole(member.Id, member.DisplayName, member.IsGhost, entry._ownId == member.Id);
                HandleHostOptions(entry, room);
            }
        }

        private void HandleLeftMembers(Lobby room)
        {
            var childrenToRemove = new List<Transform>();

            for (int i = 0; i < content.childCount; i++)
            {
                var child = content.GetChild(i);
                if (!child.TryGetComponent(out MemberEntry member))
                    continue;

                if (!room.Members.Exists(x => x.Id == member.MemberId))
                {
                    m_roleKeeper.RemoveRole(member.MemberId);
                    childrenToRemove.Add(child);
                }
            }

            foreach (var child in childrenToRemove)
            {
                Destroy(child.gameObject);
            }
        }

        private void HandleInGameLock(Lobby room)
        {
            m_isSomeoneInGame = room.Members.Exists(x => x.IsInGame == true);
            if (m_isSomeoneInGame == m_lastInGameState)
                return;
            m_lastInGameState = m_isSomeoneInGame;

            var existingMembers = content.GetComponentsInChildren<MemberEntry>();
            foreach (var member in existingMembers)
            {
                member.LockReady(m_isSomeoneInGame);
            }
        }
    }
}
