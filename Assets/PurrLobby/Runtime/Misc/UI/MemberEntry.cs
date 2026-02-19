using System.Threading.Tasks;
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
        [SerializeField] private TextMeshProUGUI roleButton;

        private bool _isGhost;
        private Color _defaultColor;
        private string _memberId;
        public string MemberId => _memberId;

        public async Task Init(LobbyUser user)
        {
            _isGhost = Random.Range(0, 2) == 0;
            roleButton.text = _isGhost ? "G" : "C";
            _defaultColor = userName.color;
            _memberId = user.Id;
            avatar.texture = user.Avatar;
            userName.text = user.DisplayName;
            SetReady(user.IsReady);
            string ownId = await FindAnyObjectByType<LobbyManager>().GetPlayer();
            if (ownId != _memberId) roleButton.GetComponentInParent<Button>().enabled = false;
            Debug.Log(ownId);
        }

        public void ChangeRole()
        {
            _isGhost = !_isGhost;
            roleButton.text = _isGhost ? "G" : "C";
            Debug.Log(_memberId);
        }
        
        public void SetReady(bool isReady)
        {
            userName.color = isReady ? readyColor : _defaultColor;
        }
    }
}
