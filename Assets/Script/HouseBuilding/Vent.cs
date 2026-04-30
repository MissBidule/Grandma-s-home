using PurrNet;
using PurrNet.Logging;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Script.HouseBuilding
{
    public class Vent : NetworkBehaviour, IInteractable
    {

        [Header("Highlight")]
        [SerializeField] private List<Renderer> m_highlightRenderers = new List<Renderer>();
        [SerializeField] private Color m_highlightColor = new Color(0f, 1f, 1f, 1f);
        [SerializeField] private float m_pulseSpeed = 3f;
        [SerializeField] private float m_minIntensity = 0.2f;
        [SerializeField] private float m_maxIntensity = 0.6f;

        [Header("Interaction")]
        [SerializeField] private string m_promptLabelVENT = "Vent";
        
        private bool m_isFocused;
        private Coroutine m_pulseCoroutine;
        private MaterialPropertyBlock m_propertyBlock;
        
        [Header("Exit")]
        [SerializeField] private SyncVar<VentExit> m_exit;
        private VentExit m_hidenVentExit;
        
        private void Start()
        {
            m_propertyBlock = new MaterialPropertyBlock();
            m_highlightRenderers.Add(GetComponentInChildren<Renderer>());

            if (m_highlightRenderers.Count > 0)
            {
                foreach (Renderer r in m_highlightRenderers)
                {
                    foreach (Material m in r.materials)
                    {
                        if (m != null)
                            m.EnableKeyword("_EMISSION");
                    }
                }
            }
            SetHighlight(false);
        }

        [ServerRpc(requireOwnership: false)]
        public void LinkExit(VentExit _exit)
        {
            if (!isServer)
                return;
            PurrLogger.Log($"Linking Vent Exit {m_exit.isControllingSyncVar}", this);
            m_exit.value = _exit;
            m_hidenVentExit = _exit;
        }

        public void OnFocus(Interact _player)
        {
            m_isFocused = true;
            if (_player.m_isGhost)
            {
                InteractPromptUI.m_Instance.Show(
                    InputBindingHelper.BuildPrompt(
                        "Ghost",
                        "Interact",
                        m_promptLabelVENT)
                );
            }
            else
            {
                InteractPromptUI.m_Instance.Show(
                    InputBindingHelper.BuildPrompt(
                        "Child",
                        "Interact",
                        m_promptLabelVENT)
                );
            }
            SetHighlight(true);
        }
        
        public void OnUnfocus(Interact _player)
        {
            m_isFocused = false;
            InteractPromptUI.m_Instance.Hide();
            SetHighlight(false);
        }
        
        public void OnInteract(Interact _player)
        {
            PlayerControllerCore playerController = _player.GetComponentInParent<PlayerControllerCore>();
            TP_Player(playerController.gameObject);
        }

        [ServerRpc(requireOwnership: false)]
        private void TP_Player(GameObject _player)
        {
            if (m_exit.value == null)
            {
                _player.transform.position = m_hidenVentExit.GetExitTransform.position;
                return;
            }
            _player.transform.position = m_exit.value.GetExitTransform.position;
        }
        
        public void OnStopInteract(Interact _player) { }
        
        /*
         * @brief Starts or stops the pulsing highlight coroutine on the highlight renderer
         * Resets emission to black when disabled
         * @param _enabled: Whether the highlight should be active
         * @return void
         */
        private void SetHighlight(bool _enabled)
        {
            if (m_highlightRenderers.Count == 0)
            {
                return;
            }

            if (_enabled)
            {
                if (m_pulseCoroutine != null)
                {
                    StopCoroutine(m_pulseCoroutine);
                }
                m_pulseCoroutine = StartCoroutine(PulseHighlight());
            }
            else
            {
                if (m_pulseCoroutine != null)
                {
                    StopCoroutine(m_pulseCoroutine);
                    m_pulseCoroutine = null;
                }


                foreach (Renderer r in m_highlightRenderers)
                {
                    r.GetPropertyBlock(m_propertyBlock);
                    m_propertyBlock.SetColor("_EmissionColor", Color.black);
                    r.SetPropertyBlock(m_propertyBlock);
                }
            }
        }
        
        /*
         * @brief Animates the highlight renderer with a pulsing emission effect
         * @return IEnumerator for coroutine
         */
        private IEnumerator PulseHighlight()
        {
            float time = 0f;

            while (true)
            {
                float pulse = Mathf.Lerp(m_minIntensity, m_maxIntensity,
                    (Mathf.Sin(time * m_pulseSpeed) + 1f) * 0.5f);

                foreach (Renderer r in m_highlightRenderers)
                {
                    r.GetPropertyBlock(m_propertyBlock);
                    m_propertyBlock.SetColor("_EmissionColor", m_highlightColor * pulse);
                    r.SetPropertyBlock(m_propertyBlock);
                }

                time += Time.deltaTime;
                yield return null;
            }
        }
    }
}
