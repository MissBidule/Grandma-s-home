using System.Threading.Tasks;
using PurrNet.Logging;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PurrLobby
{
    public class MemberEntry : MonoBehaviour
    {
        [SerializeField] private TMP_Text userName;
        [SerializeField] private RawImage avatar;
        [SerializeField] private RawImage hostIcon;
        [SerializeField] private Color readyColor;
        [SerializeField] private Button roleButton;
        [SerializeField] public Button readyButton;

        public bool _isGhost;
        private Color _defaultColor;
        private string _memberId;
        public string _ownId;
        public string MemberId => _memberId;
        public LobbyManager _lobbyManager;

        public void Init(LobbyUser _user)
        {
            //cosmetic
            userName.text = _user.DisplayName;
            _defaultColor = userName.color;
            if (_user.Avatar != null) avatar.texture = _user.Avatar;
            SetReady(_user.IsReady);

            //role
            _isGhost = _user.IsGhost;
            roleButton.GetComponentInChildren<TextMeshProUGUI>().text = _isGhost ? "G" : "C";

            //RoleButton
            _memberId = _user.Id;
            if (_ownId != _memberId) roleButton.interactable = false;
            else
            {
                LockReady(false);
                roleButton.interactable = true;
                roleButton.onClick.AddListener(delegate {
                    _lobbyManager.ToggleLocalRole();
                });
                readyButton.onClick.AddListener(delegate {
                    roleButton.interactable = !roleButton.interactable;
                });
            }
        }

        public bool SetHost()
        {
            if (_lobbyManager.isPlayerHost(_memberId)) {
                hostIcon.enabled = true;
                if (_memberId == _ownId)
                {
                    readyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start Game";
                    return true;
                }
                else readyButton.GetComponentInChildren<TextMeshProUGUI>().text = "Ready";
            }
            return false;
        }
        
        public void SetReady(bool isReady)
        {
            userName.color = isReady ? readyColor : _defaultColor;
        }

        public void LockReady(bool isLocked)
        {
            readyButton.interactable = !isLocked;
        }

        public void SetRole(bool isGhost)
        {
            _isGhost = isGhost;
            roleButton.GetComponentInChildren<TextMeshProUGUI>().text = _isGhost ? "G" : "C";
            FindAnyObjectByType<RoleKeeper>().SwitchRole(MemberId, isGhost);
        }
    }
}
