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
        public Button roleButton;
        public Button readyButton;

        public bool _isGhost;
        public int _skin;
        private Color _defaultColor;
        private string _memberId;
        public string _ownId;
        public string MemberId => _memberId;
        public LobbyManager _lobbyManager;
        private RoleKeeper _roleKeeper;

        public void Init(LobbyUser _user)
        {
            _roleKeeper = FindAnyObjectByType<RoleKeeper>();

            //cosmetic
            userName.text = _user.DisplayName;
            _defaultColor = userName.color;
            SetReady(_user.IsReady);

            //role
            _isGhost = _user.IsGhost;
            _skin = _user.Skin;
            //avatar.texture = _roleKeeper.GetSkinImage(_memberId);

            //RoleButton
            _memberId = _user.Id;
            if (_ownId == _memberId) LockReady(false);
            roleButton.interactable = true;
            readyButton.onClick.AddListener(delegate {
                roleButton.interactable = !roleButton.interactable;
            });
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

        public void SetRole(bool isGhost, int skin)
        {
            _isGhost = isGhost;
            _skin = skin;
            FindAnyObjectByType<RoleKeeper>().SwitchRole(_memberId, isGhost, skin);
            avatar.texture = _roleKeeper.GetSkinImage(_memberId);
        }
    }
}
