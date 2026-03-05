using System.Collections;
using TMPro;
using UnityEngine;

namespace PurrLobby
{
    public class LobbyNameButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text lobbyText;

        private string _roomName;
        private Coroutine _clickEffectCoroutine;
        
        public void Init(string roomName)
        {
            _roomName = roomName;
            lobbyText.text = _roomName;
        }

        public void CopyName()
        {
            GUIUtility.systemCopyBuffer = _roomName;
            
            if(_clickEffectCoroutine != null)
                StopCoroutine(_clickEffectCoroutine);
            _clickEffectCoroutine = StartCoroutine(ClickEffect());
            
        }

        private WaitForSeconds _wait = new (0.13f);
        private WaitForSeconds _waitToReturn = new (1f);
        private IEnumerator ClickEffect()
        {
            lobbyText.text = "Copied!"; 

            yield return _waitToReturn;
            
            lobbyText.text = "";
            for (int i = 0; i < _roomName.Length; i++)
            {
                lobbyText.text += _roomName[i];
                yield return _wait;
            }
        }
    }
}
