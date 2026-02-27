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
        [SerializeField] private Color readyColor;
        [SerializeField] private Button roleButton;
        [SerializeField] public Button readyButton;

        public bool _isGhost;
        private Color _defaultColor;
        private string _memberId;
        public string MemberId => _memberId;

        public async Task Init(LobbyUser user)
        {
            _isGhost = user.IsGhost;
            roleButton.GetComponentInChildren<TextMeshProUGUI>().text = _isGhost ? "G" : "C";
            _defaultColor = userName.color;
            _memberId = user.Id;
            avatar.texture = user.Avatar;
            userName.text = user.DisplayName;
            SetReady(user.IsReady);
            string ownId = await FindAnyObjectByType<LobbyManager>().GetPlayer();
            if (ownId != _memberId) roleButton.enabled = false;
            else
            {
                roleButton.onClick.AddListener(delegate {
                    FindAnyObjectByType<LobbyManager>().ToggleLocalRole();
                });
                readyButton.onClick.AddListener(delegate {
                        roleButton.enabled = !roleButton.enabled;
                });
            }
        }
        
        public void SetReady(bool isReady)
        {
            userName.color = isReady ? readyColor : _defaultColor;
        }

        public void SetRole(bool isGhost)
        {
            _isGhost = isGhost;
            roleButton.GetComponentInChildren<TextMeshProUGUI>().text = _isGhost ? "G" : "C";
            FindAnyObjectByType<RoleKeeper>().SwitchRole(MemberId, isGhost);
        }
    }
}
