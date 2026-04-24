using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet;
using PurrNet.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace PurrLobby
{

    public class LobbyManager : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour currentProvider;
        private ILobbyProvider _currentProvider;

        private readonly Queue<Action> _delayedActions = new Queue<Action>();
        private int _taskLock;

        [SerializeField] private TextMeshProUGUI m_usernameField;


        public CreateRoomArgs createRoomArgs = new();
        public SerializableDictionary<string, string> searchRoomArgs = new();

        // Events exposed by the manager
        public UnityEvent<Lobby> OnRoomJoined = new UnityEvent<Lobby>();
        public UnityEvent<string> OnRoomJoinFailed = new UnityEvent<string>();
        public UnityEvent OnRoomLeft = new UnityEvent();
        public UnityEvent<Lobby> OnRoomUpdated = new UnityEvent<Lobby>();
        public UnityEvent<List<LobbyUser>> OnPlayerListUpdated = new UnityEvent<List<LobbyUser>>();
        public UnityEvent<List<Lobby>> OnRoomSearchResults = new UnityEvent<List<Lobby>>();
        public UnityEvent<List<FriendUser>> OnFriendListPulled = new UnityEvent<List<FriendUser>>();
        public UnityEvent OnAllReady = new UnityEvent();
        public UnityEvent<string> OnError = new UnityEvent<string>();

        public UnityEvent onInitialized = new UnityEvent();
        public UnityEvent onShutdown = new UnityEvent();

        public ILobbyProvider CurrentProvider => currentProvider as ILobbyProvider;

        private bool _restartGame = false;
        private bool _restartAsked = false;

        private Lobby _currentLobby
        {
            get
            {
                if (!_lobbyDataHolder)
                    return default;
                return _lobbyDataHolder.CurrentLobby;
            }
            set
            {
                _lobbyDataHolder.SetCurrentLobby(value);
            }
        }

        private Lobby _lastKnownState;
        public Lobby CurrentLobby => _currentLobby;
        private LobbyDataHolder _lobbyDataHolder;
        private SceneMenuNavigator _viewManager;

        private bool IsStarting = false;
        public float _refreshRate = 5f;
        private float _elapsedTime = 0;

        [SerializeField] private Button m_readyButton;
        [SerializeField] private Button m_leaveButton;
        [SerializeField] private Button m_serverType;
        [SerializeField] private TMP_InputField m_playerCount;
        [SerializeField] private TMP_InputField m_serverName;
        [SerializeField] private GameObject m_roleKeeperPrefab;
        public Canvas m_loadingCanvas;
        private bool m_loading = false;
        SceneSwitcher m_sceneSwitcher;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            var LobbyManagerSecurity = FindObjectsByType<LobbyManager>(FindObjectsSortMode.InstanceID);
            if (LobbyManagerSecurity.Length > 1) 
                Destroy(LobbyManagerSecurity[0].gameObject);
            _lastKnownState = new Lobby { IsValid = false };
            _viewManager = FindAnyObjectByType<SceneMenuNavigator>();

            SetupRoleKeeper();
            SetupDataHolder();

            if (CurrentProvider != null)
            {
                SetProvider(CurrentProvider);
            }
            else
            {
                PurrLogger.LogWarning("No lobby provider assigned to LobbyManager.");
            }
            m_sceneSwitcher = GetComponent<SceneSwitcher>();
        }

        public bool isPlayerHost(string _playerId)
        {
            EnsureProviderSet();
            return _currentProvider.IsPlayerHost(_playerId);
        }

        public void showHostObjects(bool isHost)
        {
            m_serverName.interactable = isHost;
            m_playerCount.interactable = isHost;
            m_serverType.interactable = isHost;
        }

        void Start()
        {
            m_usernameField.text = FindAnyObjectByType<PersistentDataManager>().LoadUsername();  
            EnsureProviderSet();
            Application.wantsToQuit += WantsToQuit;
        }

        private void SetupDataHolder()
        {
            _lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if (!_lobbyDataHolder)
            {
                var newObject = new GameObject("LobbyDataHolder");
                _lobbyDataHolder = newObject.AddComponent<LobbyDataHolder>();
                return;
            }

            if (_lobbyDataHolder.CurrentLobby.IsValid)
            {
                //Here we reloaded the scene while still being a lobby so we can reconnect again
                Debug.Log("Valid lobby found in data holder on Awake, rejoining room...");
                _restartAsked = true;
            }
        }

        private void SetupRoleKeeper()
        {
            var roleKeeper = FindFirstObjectByType<RoleKeeper>();
            if (!roleKeeper)
            {
                var newObject = Instantiate(m_roleKeeperPrefab);
            }
        }

        public async Task<string> CleanLobby() {
            EnsureProviderSet();
            int previousMaxPlayers = _currentLobby.MaxPlayers;
            string previousLobbyName = _currentLobby.Name;
            Dictionary<string, string> roomProperties = createRoomArgs.roomProperties.ToDictionary();
            await _currentProvider.CleanLobby();
            var room = await _currentProvider.CreateLobbyAsync(previousMaxPlayers, roomProperties, true, previousLobbyName);
            _currentLobby = room;
            return _currentLobby.LobbyId;
        }

        private async Task ReconnectToLobbyAsync()
        {
            FindAnyObjectByType<RoleKeeper>().DeleteList();
            EnsureProviderSet(); 
            
            if (_lobbyDataHolder.CurrentLobby.IsOwner) 
            { 
                await _currentProvider.OnLobbyUpdateData(_currentLobby.LobbyId);         
                m_loadingCanvas.gameObject.SetActive(false); 
                _viewManager.BackToLobby(); 
                UpdateLobbyOnScreen();     
            } 
            else 
            { 
                var room = await _currentProvider.JoinLobbyAsync(_currentLobby.LobbyId); 
                if (room.IsValid) 
                { 
                    _viewManager.BackToLobby();
                    UpdateLobbyOnScreen();    
                    m_loading = true; 
                    _currentLobby = room; 
                    OnRoomJoined?.Invoke(room); 
                } 
                else 
                { 
                    m_loading = false; 
                    OnRoomJoinFailed?.Invoke($"Failed to join room {_currentLobby.LobbyId}"); 
                } 
            } 
            
            //refresh lobby info
            _elapsedTime = _refreshRate;
            _restartGame = false;
        }

        public void UpdateLobbyOnScreen()
        {
            if (IsStarting) return;
            if(m_sceneSwitcher._isTuto)
            {
                SetIsReady(true);
                foreach(GameObject obj in FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                {
                    if(obj.name == "Canvas_Lobby")
                    {
                        obj.SetActive(false);
                    }  
                }
            }

            m_serverName.transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text = _currentLobby.Name.ToUpper();
            m_serverType.GetComponentInChildren<TextMeshProUGUI>().text = _currentLobby.IsPrivate ? "Private" : "Public";     
            m_playerCount.transform.GetChild(0).GetComponentInChildren<TextMeshProUGUI>().text = "Max players (" + _currentLobby.MaxPlayers + ")";
            FindAnyObjectByType<CodeButton>()?.Init(_currentLobby.LobbyId);
        }

        private void Update()
        {
            if (_restartAsked)
            {
                m_loadingCanvas.gameObject.SetActive(true);
                if (_lobbyDataHolder.CurrentLobby.IsOwner) 
                { 
                    _ = ReconnectToLobbyAsync();
                }
                else
                {
                    Invoke("ReconnectToLobbyAsync", .5f);
                }
                _restartAsked = false;
                _restartGame = true;
            }
            if (_restartGame)
            {
                return;
            }
            while (_delayedActions.Count > 0)
            {
                _delayedActions.Dequeue()?.Invoke();
            }
            if (_currentLobby.IsValid)
            {
                _elapsedTime += Time.deltaTime;
                if (_elapsedTime >= _refreshRate)
                {
                    _elapsedTime = 0;
                    EnsureProviderSet();
                    _currentProvider.TriggerLobbyUpdated();
                }
            }
        }

        private void InvokeDelayed(Action action)
        {
            try
            {
                _delayedActions.Enqueue(action);
            }
            catch (Exception ex)
            {
                PurrLogger.LogError($"Error in InvokeDelayed: {ex.Message}");
            }
        }

        /// <summary>
        /// Set or switch the current provider
        /// </summary>
        /// <param name="provider"></param>
        public void SetProvider(ILobbyProvider provider)
        {
            if (_currentProvider != null)
            {
                UnsubscribeFromProviderEvents();
                _currentProvider.Shutdown();
            }

            _currentProvider = provider;

            if (_currentProvider != null)
            {
                SubscribeToProviderEvents();
                RunTask(async () =>
                {
                    await _currentProvider.InitializeAsync();
                    InvokeDelayed(() => onInitialized?.Invoke());
                });
            }
        }

        // Subscribe to provider events
        private void SubscribeToProviderEvents()
        {
            _currentProvider.OnLobbyJoinFailed += message => InvokeDelayed(() => OnRoomJoinFailed.Invoke(message));
            _currentProvider.OnLobbyLeft += () => InvokeDelayed(() =>
            {
                _lastKnownState = default;
                _currentLobby = default;
                OnRoomLeft?.Invoke();
            });
            
            _currentProvider.OnLobbyUpdated += room => InvokeDelayed(() =>
            {
                if(!_lastKnownState.HasChanged(room) || room.Members.Count <= 0 || !room.IsValid) return;

                _lastKnownState = room;
                _currentLobby = room;
                
                // Update LobbyDataHolder with player count
                if (_lobbyDataHolder != null && _viewManager != null)
                {
                    PurrLogger.Log($"Updating player count: {room.Members.Count}", this);
                    _lobbyDataHolder.setNumber_of_player_in_lobby(room.Members.Count);
                }
                
                OnRoomUpdated?.Invoke(room);

                if (!IsStarting && room.Members.TrueForAll(x => x.IsReady))
                {
                    IsStarting = true; //Prevent calling ready again if lobby is updated after all ready
                    m_loadingCanvas.gameObject.SetActive(true);
                    CallOnAllReady();
                }
            });

            _currentProvider.OnLobbyPlayerListUpdated += players => InvokeDelayed(() => OnPlayerListUpdated.Invoke(players));
            _currentProvider.OnError += error => InvokeDelayed(() => OnError.Invoke(error));
            
            _currentProvider.OnLobbyUpdated += room =>
            {
                if (room.IsValid && m_loading)
                {
                    m_loading = false;
                    InvokeDelayed(() => OnRoomJoined?.Invoke(room));
                }
            };
        }

        // Unsubscribe from provider events
        private void UnsubscribeFromProviderEvents()
        {
            _currentProvider.OnLobbyJoinFailed -= message => InvokeDelayed(() => OnRoomJoinFailed.Invoke(message));
            _currentProvider.OnLobbyLeft -= () => InvokeDelayed(() => OnRoomLeft.Invoke());
            _currentProvider.OnLobbyUpdated -= room => InvokeDelayed(() => OnRoomUpdated.Invoke(room));
            _currentProvider.OnLobbyPlayerListUpdated -= players => InvokeDelayed(() => OnPlayerListUpdated.Invoke(players));
            _currentProvider.OnError -= error => InvokeDelayed(() => OnError.Invoke(error));

            // ReSharper disable once EventUnsubscriptionViaAnonymousDelegate
            _currentProvider.OnLobbyUpdated -= room =>
            {
                if (room.IsValid && m_loading)
                {
                    m_loading = false;
                    InvokeDelayed(() => OnRoomJoined?.Invoke(room));
                }
            };
        }

        /// <summary>
        /// Shuts down and clears the current provider
        /// </summary>
        public void Shutdown()
        {
            EnsureProviderSet();
            _currentProvider.Shutdown();
            onShutdown?.Invoke();
        }

        /// <summary>
        /// Prompts the provider to pull friends from the platform's friend list.
        /// </summary>
        public void PullFriends(FriendFilter filter)
        {
            RunTask(async () =>
            {
                EnsureProviderSet();
                var friends = await _currentProvider.GetFriendsAsync(filter);
                OnFriendListPulled?.Invoke(friends);
            });
        }

        /// <summary>
        /// Invite the given user to the current lobby.
        /// </summary>
        /// <param name="user"></param>
        public void InviteFriend(FriendUser user)
        {
            RunTask(async () =>
            {
                EnsureProviderSet();
                await _currentProvider.InviteFriendAsync(user);
            });
        }

        public async Task<string> GetPlayer()
        {
            EnsureProviderSet();
            return await _currentProvider.GetPlayer();
        }

        /// <summary>
        /// Creates a room using the inspector CreateRoomArgs values.
        /// </summary>
        public void CreateRoom()
        {
            m_loading = true;
            CreateRoom(createRoomArgs.maxPlayers, createRoomArgs.roomProperties.ToDictionary());
        }
        
        /// <summary>
        /// Creates a room using custom settings set through code
        /// </summary>
        public void CreateRoom(int maxPlayers, Dictionary<string, string> roomProperties = null)
        {
            RunTask(async () =>
            {
                EnsureProviderSet();
                var room = await _currentProvider.CreateLobbyAsync(maxPlayers, roomProperties);
                _currentLobby = room;
                OnRoomUpdated?.Invoke(room);
            });
        }

        /// <summary>
        /// Leave the lobby
        /// </summary>
        public void LeaveLobby()
        {
            RunTask(async () =>
            {
                EnsureProviderSet();
                await _currentProvider.LeaveLobbyAsync();
                OnRoomLeft?.Invoke();
            });
        }

        /// <summary>
        /// Leave a specific lobby
        /// </summary>
        public void LeaveLobby(string lobbyId)
        {
            RunTask(async () =>
            {
                EnsureProviderSet();
                await _currentProvider.LeaveLobbyAsync(lobbyId);
                OnRoomLeft?.Invoke();
            });
        }

        /// <summary>
        /// Join the lobby with the given ID
        /// </summary>
        /// <param name="roomId">ID of the lobby to join</param>
        public void JoinLobby(string roomId)
        {
            m_loadingCanvas.gameObject.SetActive(true);
            if (string.IsNullOrEmpty(roomId))
            {
                OnRoomJoinFailed?.Invoke("Null or empty room ID.");
                return;
            }
            
            m_loading = true;
            RunTask(async () =>
            {
                EnsureProviderSet();
                var room = await _currentProvider.JoinLobbyAsync(roomId);
                if (room.IsValid)
                {
                    _currentLobby = room;
                    OnRoomJoined?.Invoke(room);
                }
                else
                {
                    m_loading = false;
                    OnRoomJoinFailed?.Invoke($"Failed to join room {roomId}");
                }
            });
        }

        /// <summary>
        /// Prompts the provider to search lobbies with given filters
        /// </summary>
        /// <param name="maxRoomsToFind">Max amount of rooms to find</param>
        /// <param name="filters">Filters to use for search - only works if the provider supports it</param>
        public void SearchLobbies(int maxRoomsToFind = 10, Dictionary<string, string> filters = null)
        {
            if(filters == null)
                filters = searchRoomArgs.ToDictionary();
            
            RunTask(async () =>
            {
                Debug.Log("Searching for rooms...");
                EnsureProviderSet();
                var rooms = await _currentProvider.SearchLobbiesAsync(maxRoomsToFind, filters);
                OnRoomSearchResults?.Invoke(rooms);
            });
        }
        
        /// <summary>
        /// Set's the given User to Ready
        /// </summary>
        /// <param name="isReady">Ready state to set</param>
        public void SetIsReady(bool isReady)
        {
            Debug.Log($"Setting ready state to {isReady}");
            RunTask(async () =>
            {
                EnsureProviderSet();
                await _currentProvider.SetIsReadyAsync(isReady);
            });
        }

        public void SetSkinAndRoleAsync(int _skinAndRole)
        {
            //Ghost skins are between 0 and 4, childs skins are between 5 and 9, so we can determine the role by checking if the skin index is below 5 or not

            bool isGhost = (int)_skinAndRole < 5 ? true : false;

            int skin = (int)_skinAndRole % 5;
            
            RunTask(async () =>
            {
                EnsureProviderSet();
                await _currentProvider.SetSkinAndRoleAsync(isGhost, skin);
            });
        }

        /// <summary>
        /// Set the given User to Ghost
        /// </summary>
        /// <param name="isGhost">Role state to set</param>
        public void SetIsGhost(bool isGhost)
        {
            RunTask(async () =>
            {
                EnsureProviderSet();
                await _currentProvider.SetIsGhostAsync(isGhost);
            });
        }

        /// <summary>
        /// Set the given User to Ghost
        /// </summary>
        /// <param name="isGhost">Role state to set</param>
        public void SetSkin(int skin)
        {
            RunTask(async () =>
            {
                EnsureProviderSet();
                await _currentProvider.SetSkinAsync(skin);
            });
        }
        
        /// <summary>
        /// Sets meta data on the current lobby we're in
        /// </summary>
        /// <param name="key">Key/Identifier of the meta data</param>
        /// <param name="value">The value of the meta data to be stored</param>
        public void SetLobbyData(string key, string value)
        {
            RunTask(async () =>
            {
                EnsureProviderSet();
                await _currentProvider.SetLobbyDataAsync(key, value);
            });
        }

        /// <summary>
        /// Update Lobby Name
        /// </summary>
        public void UpdateLobbyName(string _lobbyName)
        {
            EnsureProviderSet();
            _currentProvider.UpdateLobbyName(_lobbyName);
            _lobbyDataHolder.SetName(_lobbyName);
        }
        
        /// <summary>
        /// Update Lobby Type
        /// </summary>
        public void UpdateLobbyType(bool _isPrivate)
        {
            EnsureProviderSet();
            _currentProvider.UpdateLobbyType(_isPrivate);
            _lobbyDataHolder.SetPrivate(_isPrivate);
        }

        /// <summary>
        /// Update max players
        /// </summary>
        public void UpdateLobbyMaxPlayer(int _maxPlayers)
        {
            if (!_lobbyDataHolder)
                return;
            _lobbyDataHolder.SetMaxPlayer(_maxPlayers);
            EnsureProviderSet();
            _currentProvider.UpdateLobbyMaxPlayers(_maxPlayers);
        }
        
        /// <summary>
        /// Gets meta data from the current lobby we're in
        /// </summary>
        /// <param name="key">Key/Identifier of the meta data we want</param>
        /// <returns>Value of the meta data we get</returns>
        public async Task<String> GetLobbyData(string key) 
        {
            EnsureProviderSet();
            return await _currentProvider.GetLobbyDataAsync(key);
        }

        /// <summary>
        /// Toggles the local users ready state automatically
        /// </summary>
        public void ToggleLocalReady()
        {
            if (!_currentLobby.IsValid)
            {
                PurrLogger.LogError($"Can't toggle ready state, current lobby is invalid.");
                return;
            }

            Debug.Log($"Toggling ready state for local user in lobby {_currentLobby.LobbyId}");
            
            var localUserId = _currentProvider.GetLocalUserIdAsync().Result;
            if (string.IsNullOrEmpty(localUserId))
            {
                PurrLogger.LogError($"Can't toggle ready state, local user ID is null or empty.");
                return;
            }

            Debug.Log($"toggle successful");
            
            var localLobbyUser = _currentLobby.Members.Find(x => x.Id == localUserId);
            SetIsReady(!localLobbyUser.IsReady);
        }

        /// <summary>
        /// Toggles the local users role state automatically
        /// </summary>
        public void ToggleLocalRole(bool isGhost)
        {
            if (!_currentLobby.IsValid)
            {
                PurrLogger.LogError($"Can't toggle role state, current lobby is invalid.");
                return;
            }

            var localUserId = _currentProvider.GetLocalUserIdAsync().Result;
            if (string.IsNullOrEmpty(localUserId))
            {
                PurrLogger.LogError($"Can't toggle role state, local user ID is null or empty.");
                return;
            }

            var localLobbyUser = _currentLobby.Members.Find(x => x.Id == localUserId);
            SetIsGhost(isGhost);
        }

        public void CycleLocalRole()
        {
            if (!_currentLobby.IsValid) { PurrLogger.LogError("Can't cycle role, lobby invalid."); return; }
            var localUserId = _currentProvider.GetLocalUserIdAsync().Result;
            if (string.IsNullOrEmpty(localUserId)) return;
            var me = _currentLobby.Members.Find(x => x.Id == localUserId);
            ToggleLocalRole(!me.IsGhost);
        }

        /// <summary>
        /// Changes the skin of the local user
        /// </summary>
        public void ChangeSkin(int skin)
        {
            if (!_currentLobby.IsValid)
            {
                PurrLogger.LogError($"Can't change skin, current lobby is invalid.");
                return;
            }
            Debug.Log($"Changing skin for local user to {skin}");

            SetSkin(skin);
        }

        private void OnDestroy()
        {
            _currentProvider = null;
            _lobbyDataHolder = null;
        }

        private bool WantsToQuit()
        {
            Debug.Log("LobbyManager OnDestroy called, shutting down provider.");
            if (CurrentLobby.IsValid)
            {
                StartCoroutine(RemovePlayerAndQuit());
                return false;
            }
            return true;
        }

        public IEnumerator RemovePlayerAndDestroy() {
            EnsureProviderSet();
            yield return _currentProvider.LeaveLobbyAsync();
            FindAnyObjectByType<RoleKeeper>().DeleteList();
            Destroy(gameObject);
        }

        private IEnumerator RemovePlayerAndQuit()
        {
            EnsureProviderSet();
            yield return _currentProvider.LeaveLobbyAsync();
            OnRoomLeft?.Invoke();
            Application.Quit();
        }

        private async void CallOnAllReady()
        {
            await WaitForAllTasksAsync();
            if(_currentLobby.IsValid && _currentLobby.Members.TrueForAll(x => x.IsReady))
            {
                LockReady();
                UpdateLobbyType(true);
                await _currentProvider.SetAllReadyAsync();

                OnAllReady?.Invoke();
            }
        }

        public void LockReady()
        {
            m_readyButton.interactable = false;
            m_leaveButton.interactable = false;
        }
        
        public async Task WaitForAllTasksAsync()
        {
            while (_taskLock > 0)
            {
                await Task.Yield();
            }
        }

        private async void RunTask(Func<Task> taskFunc)
        {
            if (taskFunc == null || _currentProvider == null) return;

            _taskLock++;
            try
            {
                await taskFunc();
            }
            catch (Exception ex)
            {
                PurrLogger.LogError($"Task Error: {ex.Message}");
            }
            finally
            {
                _taskLock--;
                if (_taskLock < 0)
                    _taskLock = 0;
            }
        }

        private void EnsureProviderSet()
        {
            if (_currentProvider == null)
                throw new InvalidOperationException("No lobby provider has been set.");
        }

        public void SetLobbyStarted()
        {
            _currentProvider.SetLobbyStartedAsync();
        }

        [System.Serializable]
        public class CreateRoomArgs
        {
            public int maxPlayers = 5;
            public SerializableDictionary<string, string> roomProperties = null;
        }
        
        [System.Serializable]
        public enum FriendFilter
        {
            InThisGame,
            Online,
            All
        }
    }
}