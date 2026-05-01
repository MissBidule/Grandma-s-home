using System.Linq;
using UnityEngine;

namespace PurrLobby
{
    /*
     * @brief Settings panel for audio options.
     */
    public class AudioSettingsPanel : MonoBehaviour
    {
        [Header("Row Prefabs")]
        [SerializeField] private OptionRowSlider m_sliderRowPrefab;
        [SerializeField] private OptionRowDropdown m_dropdownRowPrefab;

        private OptionRowSlider m_masterRow;
        private OptionRowSlider m_musicRow;
        private OptionRowSlider m_sfxRow;
        private OptionRowDropdown m_inputDeviceRow;
        private OptionRowDropdown m_voiceModeRow;

        private static readonly string m_KeyMaster      = "Settings_VolMaster";
        private static readonly string m_KeyMusic       = "Settings_VolMusic";
        private static readonly string m_KeySFX         = "Settings_VolSFX";
        private static readonly string m_KeyInputDevice = "Settings_InputDevice";
        private static readonly string m_KeyVoiceMode   = "Settings_VoiceMode";

        private Transform m_container;
        private bool m_built;

        public void Initialize(OptionRowSlider _slider, OptionRowDropdown _dropdown = null)
        {
            if (_slider)   m_sliderRowPrefab   = _slider;
            if (_dropdown) m_dropdownRowPrefab = _dropdown;
        }

        private void Awake()
        {
            m_container = transform.Find("Scroll View/Viewport/Content");
            if (m_container == null) return;

            foreach (Transform child in m_container)
                Destroy(child.gameObject);

            m_masterRow     = SpawnSlider("Master Volume");
            m_musicRow      = SpawnSlider("Music");
            m_sfxRow        = SpawnSlider("Sound Effects");
            m_inputDeviceRow = SpawnDropdown("Input Device");
            m_voiceModeRow  = SpawnDropdown("Microphone Mode");
            m_built = true;
        }

        private void OnEnable()
        {
            if (!m_built) return;
            StartCoroutine(ScrollToTop());
            LoadAndApply();
        }

        private void LoadAndApply()
        {
            InitSlider(m_masterRow, m_KeyMaster);
            InitSlider(m_musicRow,  m_KeyMusic);
            InitSlider(m_sfxRow,    m_KeySFX);

            AudioVolumeManager.ApplyFromPrefs();

            if (m_masterRow?.m_slider) m_masterRow.m_slider.onValueChanged.AddListener(v => { PlayerPrefs.SetFloat(m_KeyMaster, v); UpdateLabel(m_masterRow, v); AudioVolumeManager.SetMaster(v); });
            if (m_musicRow?.m_slider)  m_musicRow.m_slider.onValueChanged.AddListener(v =>  { PlayerPrefs.SetFloat(m_KeyMusic,  v); UpdateLabel(m_musicRow,  v); AudioVolumeManager.SetMusic(v);  });
            if (m_sfxRow?.m_slider)    m_sfxRow.m_slider.onValueChanged.AddListener(v =>    { PlayerPrefs.SetFloat(m_KeySFX,    v); UpdateLabel(m_sfxRow,    v); AudioVolumeManager.SetSFX(v);    });

            InitInputDeviceDropdown();
            InitVoiceModeDropdown();
        }

        private void OnDisable()
        {
            if (m_masterRow?.m_slider)          m_masterRow.m_slider.onValueChanged.RemoveAllListeners();
            if (m_musicRow?.m_slider)           m_musicRow.m_slider.onValueChanged.RemoveAllListeners();
            if (m_sfxRow?.m_slider)             m_sfxRow.m_slider.onValueChanged.RemoveAllListeners();
            if (m_inputDeviceRow?.m_dropdown)   m_inputDeviceRow.m_dropdown.onValueChanged.RemoveAllListeners();
            if (m_voiceModeRow?.m_dropdown)     m_voiceModeRow.m_dropdown.onValueChanged.RemoveAllListeners();
        }

        public void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(m_KeyMaster);
            PlayerPrefs.DeleteKey(m_KeyMusic);
            PlayerPrefs.DeleteKey(m_KeySFX);
            PlayerPrefs.DeleteKey(m_KeyInputDevice);
            PlayerPrefs.DeleteKey(m_KeyVoiceMode);
            PlayerPrefs.Save();
            OnDisable();
            LoadAndApply();
        }

        private void InitInputDeviceDropdown()
        {
            if (m_inputDeviceRow?.m_dropdown == null) return;
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

            dd.AddOptions(devices.ToList());
            string saved = PlayerPrefs.GetString(m_KeyInputDevice, "");
            int idx = System.Array.IndexOf(devices, saved);
            dd.value = idx >= 0 ? idx : 0;
            dd.interactable = true;
            dd.onValueChanged.AddListener(i =>
            {
                PlayerPrefs.SetString(m_KeyInputDevice, devices[i]);
                // TODO: transmettre le device sélectionné à PurrVoice
            });
        }

        private void InitVoiceModeDropdown()
        {
            if (m_voiceModeRow?.m_dropdown == null) return;
            var dd = m_voiceModeRow.m_dropdown;
            dd.ClearOptions();
            dd.AddOptions(new System.Collections.Generic.List<string> { "Always On", "Push to Talk", "Disabled" });
            dd.value = PlayerPrefs.GetInt(m_KeyVoiceMode, 0);
            dd.interactable = true;
            dd.onValueChanged.AddListener(i =>
            {
                PlayerPrefs.SetInt(m_KeyVoiceMode, i);
                // TODO: appliquer le voice mode à PurrVoice
            });
        }

        public static string GetSelectedInputDevice() => PlayerPrefs.GetString("Settings_InputDevice", "");
        public static int GetVoiceMode() => PlayerPrefs.GetInt("Settings_VoiceMode", 1);

        private void InitSlider(OptionRowSlider _row, string _key)
        {
            if (_row?.m_slider == null) return;
            float v = PlayerPrefs.GetFloat(_key, 1f);
            _row.m_slider.value = v;
            UpdateLabel(_row, v);
        }

        private OptionRowSlider SpawnSlider(string _labelText)
        {
            if (!m_sliderRowPrefab) return null;
            var row = Instantiate(m_sliderRowPrefab, m_container, false);
            if (row.m_label) row.m_label.text = _labelText;
            if (row.m_slider) { row.m_slider.minValue = 0f; row.m_slider.maxValue = 1f; row.m_slider.value = 1f; }
            return row;
        }

        private OptionRowDropdown SpawnDropdown(string _labelText)
        {
            if (!m_dropdownRowPrefab) return null;
            var row = Instantiate(m_dropdownRowPrefab, m_container, false);
            if (row.m_label) row.m_label.text = _labelText;
            return row;
        }

        private void UpdateLabel(OptionRowSlider _row, float _v)
        {
            if (_row?.m_valueLabel) _row.m_valueLabel.text = Mathf.RoundToInt(_v * 100f) + "%";
        }

        private System.Collections.IEnumerator ScrollToTop()
        {
            yield return null;
            Canvas.ForceUpdateCanvases();
            var sr = GetComponentInChildren<UnityEngine.UI.ScrollRect>(true);
            if (sr) sr.verticalNormalizedPosition = 1f;
        }
    }
}
