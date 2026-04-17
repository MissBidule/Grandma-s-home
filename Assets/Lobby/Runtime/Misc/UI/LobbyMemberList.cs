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
        [SerializeField] private Button roleButton;
        private bool m_isSomeoneInGame = false;
        private RoleKeeper m_roleKeeper;
        private bool m_lastInGameState = false;
        // ? FIX BUG #2: Cache LobbyManager reference instead of calling FindAnyObjectByType in loop
        private LobbyManager m_lobbyManager;
        // ? FIX PLAYER OVERFLOW: Dictionary cache for member entries to prevent duplicates
        private Dictionary<string, MemberEntry> m_memberEntries = new Dictionary<string, MemberEntry>();

        void Start()
        {
            m_roleKeeper = FindAnyObjectByType<RoleKeeper>();
            // ? FIX BUG #2: Get LobbyManager once at start
            m_lobbyManager = FindAnyObjectByType<LobbyManager>();
            
            // ? FIX PLAYER OVERFLOW: Setup layout group for proper UI organization
            SetupLayoutGroup();
        }
        
        private void SetupLayoutGroup()
        {
            VerticalLayoutGroup vlg = content.gameObject.GetComponent<VerticalLayoutGroup>();
            if (vlg == null)
            {
                vlg = content.gameObject.AddComponent<VerticalLayoutGroup>();
                vlg.childForceExpandHeight = false;
                vlg.childForceExpandWidth = false;
                vlg.spacing = 5f;
                vlg.padding = new RectOffset(10, 10, 10, 10);
            }
            
            ContentSizeFitter csf = content.gameObject.GetComponent<ContentSizeFitter>();
            if (csf == null)
            {
                csf = content.gameObject.AddComponent<ContentSizeFitter>();
                csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            }
        }

        public void LobbyDataUpdate(Lobby room)
        {
            if(!room.IsValid)
                return;

            if (m_roleKeeper == null)
                m_roleKeeper = FindAnyObjectByType<RoleKeeper>();
            
            // ? FIX BUG #2: Cache if not already cached
            if (m_lobbyManager == null)
                m_lobbyManager = FindAnyObjectByType<LobbyManager>();

            HandleExistingMembers(room);
            HandleNewMembers(room);
            HandleLeftMembers(room);
            HandleInGameLock(room);
        }

        public void OnLobbyLeave()
        {
            m_roleKeeper.DeleteList();
            // ? FIX PLAYER OVERFLOW: Clear dictionary cache
            m_memberEntries.Clear();
            foreach (Transform child in content)
                Destroy(child.gameObject);
        }

        private void HandleExistingMembers(Lobby room)
        {
            if (room.Members.Count(x => x.IsReady) == room.Members.Count)  
            {
                roleButton.interactable = false;
            }
            MemberEntry hostEntry = null;
            foreach (Transform child in content)
            {
                Debug.Log("foreach");
                if (!child.TryGetComponent(out MemberEntry member))
                    continue;

                var matchingMember = room.Members.Find(x => x.Id == member.MemberId);
                if (!string.IsNullOrEmpty(matchingMember.Id))
                {
                    member.SetReady(matchingMember.IsReady);
                    member.SetRole(matchingMember.IsGhost, matchingMember.Skin);
                    if (member.SetHost()) hostEntry = member;    
                }
            }
            HandleHostOptions(hostEntry, room);
        }

        private void HandleHostOptions(MemberEntry _member, Lobby _room)
        {
            if (_member != null)
            {
                _member._lobbyManager.showHostObjects(true);
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
            // ? FIX PLAYER OVERFLOW: Use dictionary cache instead of GetComponentsInChildren
            foreach (var member in room.Members)
            {
                // ? Check in DICTIONARY instead of array.exists for better accuracy
                if (m_memberEntries.ContainsKey(member.Id))
                    continue;

                var entry = Instantiate(memberEntryPrefab, content);
                entry.readyButton = readyButton;
                entry.roleButton = roleButton;
                // ? FIX BUG #2: Use cached reference instead of FindAnyObjectByType
                entry._lobbyManager = m_lobbyManager;
                
                try {
                    entry._ownId = await entry._lobbyManager.GetPlayer();
                }
                catch (Exception ex) {
                    PurrLogger.LogError($"Failed to get player for member {member.Id}: {ex.Message}", this);
                    continue;
                }
                
                entry.Init(member);
                
                // ? FIX PLAYER OVERFLOW: Add to cache immediately
                m_memberEntries[member.Id] = entry;
                
                m_roleKeeper.AddRole(member.Id, member.DisplayName, member.IsGhost, member.Skin, entry._ownId == member.Id);
                entry.SetRole(member.IsGhost, member.Skin);
                if (entry.SetHost()) HandleHostOptions(entry, room);
            }
        }

        private void HandleLeftMembers(Lobby room)
        {
            // ? FIX PLAYER OVERFLOW: Iterate over dictionary keys instead of scene hierarchy
            var membersToRemove = new List<string>();
            
            foreach (var memberId in m_memberEntries.Keys)
            {
                if (!room.Members.Exists(x => x.Id == memberId))
                {
                    membersToRemove.Add(memberId);
                }
            }

            foreach (var memberId in membersToRemove)
            {
                if (m_memberEntries.TryGetValue(memberId, out var entry))
                {
                    m_roleKeeper.RemoveRole(memberId);
                    // ? FIX PLAYER OVERFLOW: Immediate destroy + remove from cache
                    Destroy(entry.gameObject);
                    m_memberEntries.Remove(memberId);
                }
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
                //tedious if but if it works
                if (!member._lobbyManager.isPlayerHost(member._ownId))
                    member.LockReady(m_isSomeoneInGame);
            }
        }
    }
}
