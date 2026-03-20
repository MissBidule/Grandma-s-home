using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace PurrLobby
{
    /*
     * @brief Settings panel that lets the player adjust all audio-related options.
     * Manages volume sliders for Master, Music, SFX and Voice channels (routed through
     * an optional AudioMixer), plus microphone device selection and voice-chat mode.
     * All values are persisted via PlayerPrefs and listeners are cleaned up on disable.
     */
    public class AudioSettingsPanel : MonoBehaviour
    {
        [Header("Row Prefabs")]
        [SerializeField] private OptionRowSlider m_sliderRowPrefab;
        [SerializeField] private OptionRowDropdown m_dropdownRowPrefab;

        [Header("Audio Mixer (optionnel)")]
        [SerializeField] private AudioMixer audioMixer;

        private OptionRowSlider m_masterRow;
        private OptionRowSlider m_musicRow;
        private OptionRowSlider m_sfxRow;
        private OptionRowSlider m_voiceRow;
        private OptionRowDropdown m_inputDeviceRow;
        private OptionRowDropdown m_voiceModeRow;

        private static readonly string m_KeyMaster = "Settings_VolMaster";
        private static readonly string m_KeyMusic = "Settings_VolMusic";
        private static readonly string m_KeySFX = "Settings_VolSFX";
        private static readonly string m_KeyVoice = "Settings_VolVoice";
        private static readonly string m_KeyInputDevice = "Settings_InputDevice";
        private static readonly string m_KeyVoiceMode = "Settings_VoiceMode";

        private const string m_ParamMaster = "MasterVolume";
        private const string m_ParamMusic = "MusicVolume";
        private const string m_ParamSFX = "SFXVolume";
        private const string m_ParamVoice = "VoiceVolume";

        /*
         * @brief Available voice-chat activation modes.
         */
        public enum VoiceMode
        {
            AlwaysOn = 0,
            PushToTalk = 1,
            Disabled = 2
        }

        private Transform m_container;
        private bool m_built;

        /*
         * @brief Injects prefab references from a parent panel, overriding Inspector values.
         * @param _slider    Prefab used to spawn volume slider rows.
         * @param _dropdown  Prefab used to spawn dropdown rows (device, voice mode).
         */
        public void Initialize(OptionRowSlider _slider, OptionRowDropdown _dropdown = null)
        {
            if (_slider)
            {
                m_sliderRowPrefab = _slider;
            }
            if (_dropdown)
            {
                m_dropdownRowPrefab = _dropdown;
            }
        }

        private void Awake()
        {
            m_container = transform.Find("Scroll View/Viewport/Content");
            if (m_container == null)
            {
                return;
            }

            foreach (Transform child in m_container)
            {
                Destroy(child.gameObject);
            }

            m_masterRow = SpawnSlider("Master Volume");
            m_musicRow = SpawnSlider("Music");
            m_sfxRow = SpawnSlider("Sound Effects");
            m_voiceRow = SpawnSlider("Voice");

            m_inputDeviceRow = SpawnDropdown("Input Device");
            m_voiceModeRow = SpawnDropdown("Microphone Mode");

            m_built = true;
        }

        private System.Collections.IEnumerator ScrollToTop()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            var sr = GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            if (sr)
            {
                sr.verticalNormalizedPosition = 1f;
            }
        }

        private OptionRowSlider SpawnSlider(string _labelText)
        {
            if (!m_sliderRowPrefab)
            {
                return null;
            }
            var row = Instantiate(m_sliderRowPrefab, m_container, false);
            if (row.m_label)
            {
                row.m_label.text = _labelText;
            }
            if (row.m_slider)
            {
                row.m_slider.minValue = 0f;
                row.m_slider.maxValue = 1f;
                row.m_slider.value = 1f;
            }
            return row;
        }

        private OptionRowDropdown SpawnDropdown(string _labelText)
        {
            if (!m_dropdownRowPrefab)
            {
                return null;
            }
            var row = Instantiate(m_dropdownRowPrefab, m_container, false);
            if (row.m_label)
            {
                row.m_label.text = _labelText;
            }
            return row;
        }

        private void OnEnable()
        {
            if (!m_built)
            {
                return;
            }
            StartCoroutine(ScrollToTop());
            LoadAndApply();
        }

        private void LoadAndApply()
        {
            InitSlider(m_masterRow, m_KeyMaster, m_ParamMaster);
            InitSlider(m_musicRow, m_KeyMusic, m_ParamMusic);
            InitSlider(m_sfxRow, m_KeySFX, m_ParamSFX);
            InitSlider(m_voiceRow, m_KeyVoice, m_ParamVoice);

            if (m_masterRow?.m_slider)
            {
                m_masterRow.m_slider.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetFloat(m_KeyMaster, v);
                    ApplyMixer(m_ParamMaster, v);
                    UpdateLabel(m_masterRow, v);
                });
            }
            if (m_musicRow?.m_slider)
            {
                m_musicRow.m_slider.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetFloat(m_KeyMusic, v);
                    ApplyMixer(m_ParamMusic, v);
                    UpdateLabel(m_musicRow, v);
                });
            }
            if (m_sfxRow?.m_slider)
            {
                m_sfxRow.m_slider.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetFloat(m_KeySFX, v);
                    ApplyMixer(m_ParamSFX, v);
                    UpdateLabel(m_sfxRow, v);
                });
            }
            if (m_voiceRow?.m_slider)
            {
                m_voiceRow.m_slider.onValueChanged.AddListener(v =>
                {
                    PlayerPrefs.SetFloat(m_KeyVoice, v);
                    ApplyMixer(m_ParamVoice, v);
                    UpdateLabel(m_voiceRow, v);
                });
            }

            InitInputDeviceDropdown();
            InitVoiceModeDropdown();
        }

        private void InitInputDeviceDropdown()
        {
            if (m_inputDeviceRow?.m_dropdown == null)
            {
                return;
            }
            var dd = m_inputDeviceRow.m_dropdown;
            dd.ClearOptions();

            var devices = Microphone.devices;
            if (devices == null || devices.Length == 0)
            {
                dd.AddOptions(new System.Collections.Generic.List<string> { "No microphone found" });
                dd.value = 0;
                dd.interactable = false;
                return;
            }

            var options = devices.ToList();
            dd.AddOptions(options);

            string saved = PlayerPrefs.GetString(m_KeyInputDevice, "");
            int idx = System.Array.IndexOf(devices, saved);
            dd.value = idx >= 0 ? idx : 0;
            dd.interactable = true;
            dd.onValueChanged.AddListener(i => PlayerPrefs.SetString(m_KeyInputDevice, devices[i]));
        }

        private void InitVoiceModeDropdown()
        {
            if (m_voiceModeRow?.m_dropdown == null)
            {
                return;
            }
            var dd = m_voiceModeRow.m_dropdown;
            dd.ClearOptions();
            dd.AddOptions(new System.Collections.Generic.List<string> { "Always On", "Push to Talk", "Disabled" });

            int saved = PlayerPrefs.GetInt(m_KeyVoiceMode, (int)VoiceMode.AlwaysOn);
            dd.value = saved;
            dd.interactable = true;
            dd.onValueChanged.AddListener(i =>
            {
                PlayerPrefs.SetInt(m_KeyVoiceMode, i);
                // Kari
            });
        }

        /*
         * @brief Returns the microphone device name saved in PlayerPrefs.
         * @return Device name string, or empty string if none was saved.
         */
        public static string GetSelectedInputDevice()
        {
            return PlayerPrefs.GetString(m_KeyInputDevice, "");
        }

        /*
         * @brief Returns the voice-chat mode saved in PlayerPrefs.
         * @return The saved VoiceMode value, defaulting to AlwaysOn.
         */
        public static VoiceMode GetVoiceMode()
        {
            return (VoiceMode)PlayerPrefs.GetInt(m_KeyVoiceMode, (int)VoiceMode.AlwaysOn);
        }

        private void InitSlider(OptionRowSlider _row, string _key, string _mixerParam)
        {
            if (_row?.m_slider == null)
            {
                return;
            }
            float v = PlayerPrefs.GetFloat(_key, 1f);
            _row.m_slider.value = v;
            UpdateLabel(_row, v);
            ApplyMixer(_mixerParam, v);
        }

        private void ApplyMixer(string _param, float _linear)
        {
            if (audioMixer == null)
            {
                return;
            }
            float dB = _linear > 0.0001f ? Mathf.Log10(_linear) * 20f : -80f;
            audioMixer.SetFloat(_param, dB);
        }

        private void UpdateLabel(OptionRowSlider _row, float _v)
        {
            if (_row?.m_valueLabel)
            {
                _row.m_valueLabel.text = Mathf.RoundToInt(_v * 100f) + "%";
            }
        }

        /*
         * @brief Clears all audio PlayerPrefs keys and reloads default values.
         */
        public void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(m_KeyMaster);
            PlayerPrefs.DeleteKey(m_KeyMusic);
            PlayerPrefs.DeleteKey(m_KeySFX);
            PlayerPrefs.DeleteKey(m_KeyVoice);
            PlayerPrefs.DeleteKey(m_KeyInputDevice);
            PlayerPrefs.DeleteKey(m_KeyVoiceMode);
            PlayerPrefs.Save();
            OnDisable();
            LoadAndApply();
        }

        private void OnDisable()
        {
            if (m_masterRow?.m_slider)
            {
                m_masterRow.m_slider.onValueChanged.RemoveAllListeners();
            }
            if (m_musicRow?.m_slider)
            {
                m_musicRow.m_slider.onValueChanged.RemoveAllListeners();
            }
            if (m_sfxRow?.m_slider)
            {
                m_sfxRow.m_slider.onValueChanged.RemoveAllListeners();
            }
            if (m_voiceRow?.m_slider)
            {
                m_voiceRow.m_slider.onValueChanged.RemoveAllListeners();
            }
            if (m_inputDeviceRow?.m_dropdown)
            {
                m_inputDeviceRow.m_dropdown.onValueChanged.RemoveAllListeners();
            }
            if (m_voiceModeRow?.m_dropdown)
            {
                m_voiceModeRow.m_dropdown.onValueChanged.RemoveAllListeners();
            }
        }
    }
}
