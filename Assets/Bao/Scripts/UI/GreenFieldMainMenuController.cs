using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GreenField.UI
{
    public sealed class GreenFieldMainMenuController : MonoBehaviour
    {
        private const string MasterVolumeKey = "GreenField.Audio.MasterVolume";
        private const string MutedKey = "GreenField.Audio.Muted";
        private const string LanguageKey = "GreenField.General.Language";
        private const string QualityKey = "GreenField.Graphics.Quality";
        private const string ResolutionWidthKey = "GreenField.Graphics.Width";
        private const string ResolutionHeightKey = "GreenField.Graphics.Height";
        private const string FullscreenKey = "GreenField.Graphics.Fullscreen";
        private const string VSyncKey = "GreenField.Graphics.VSync";

        private const float DefaultMasterVolume = 0.8f;
        private const int VietnameseLanguage = 0;

        [Header("Scene sẽ được mở từ menu")]
        [SerializeField] private string farmSceneName = "MAPMAIN";

        [Header("Cửa sổ cài đặt")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject generalSettingsPage;
        [SerializeField] private GameObject audioSettingsPage;
        [SerializeField] private GameObject graphicsSettingsPage;
        [SerializeField] private GameObject controlsSettingsPage;

        [Header("Nút tab")]
        [SerializeField] private Button generalTabButton;
        [SerializeField] private Button audioTabButton;
        [SerializeField] private Button graphicsTabButton;
        [SerializeField] private Button controlsTabButton;

        [Header("Cài đặt chung")]
        [SerializeField] private Dropdown languageDropdown;

        [Header("Cài đặt âm thanh")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Text masterVolumeValueText;
        [SerializeField] private Toggle muteToggle;

        [Header("Cài đặt đồ họa")]
        [SerializeField] private Dropdown resolutionDropdown;
        [SerializeField] private Dropdown qualityDropdown;
        [SerializeField] private Toggle fullscreenToggle;
        [SerializeField] private Toggle vSyncToggle;

        private readonly List<Vector2Int> availableResolutions =
            new List<Vector2Int>();

        private float currentMasterVolume = DefaultMasterVolume;
        private bool isMuted;
        private int currentLanguage = VietnameseLanguage;
        private int currentQuality;
        private int currentResolutionIndex;
        private bool isFullscreen;
        private bool isVSyncEnabled;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplySavedSettingsOnStartup()
        {
            float savedVolume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume)
            );
            bool savedMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;
            AudioListener.volume = savedMuted ? 0f : savedVolume;

            int qualityCount = QualitySettings.names.Length;
            if (qualityCount > 0)
            {
                int savedQuality = Mathf.Clamp(
                    PlayerPrefs.GetInt(
                        QualityKey,
                        QualitySettings.GetQualityLevel()
                    ),
                    0,
                    qualityCount - 1
                );
                QualitySettings.SetQualityLevel(savedQuality, true);
            }

            bool savedVSync = PlayerPrefs.GetInt(
                VSyncKey,
                QualitySettings.vSyncCount > 0 ? 1 : 0
            ) == 1;
            QualitySettings.vSyncCount = savedVSync ? 1 : 0;

            bool savedFullscreen = PlayerPrefs.GetInt(
                FullscreenKey,
                Screen.fullScreen ? 1 : 0
            ) == 1;
            int savedWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
            int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);

            if (savedWidth > 0 && savedHeight > 0)
            {
                Screen.SetResolution(savedWidth, savedHeight, savedFullscreen);
            }
            else
            {
                Screen.fullScreen = savedFullscreen;
            }
        }

        private void Awake()
        {
            PopulateSettingsDropdowns();
            LoadSettings();
            ShowGeneralTab();

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }

            RefreshSaveButtons();
        }

        public void StartNewGame()
        {
            if (!CanLoadGameplayScene())
                return;

            SaveManager.BeginNewGame();
            SceneManager.LoadScene(farmSceneName, LoadSceneMode.Single);
        }

        public void ContinueGame()
        {
            LoadSavedGame();
        }

        public void LoadGame()
        {
            LoadSavedGame();
        }

        public void OpenSettings()
        {
            if (settingsPanel == null)
            {
                Debug.LogWarning("[Green Field] Chưa gắn cửa sổ Cài đặt.", this);
                return;
            }

            SyncSettingsUI();
            ShowGeneralTab();
            settingsPanel.SetActive(true);
        }

        public void CloseSettings()
        {
            SaveAllSettings();

            if (settingsPanel != null)
            {
                settingsPanel.SetActive(false);
            }
        }

        public void ShowGeneralTab() => ShowSettingsPage(0);
        public void ShowAudioTab() => ShowSettingsPage(1);
        public void ShowGraphicsTab() => ShowSettingsPage(2);
        public void ShowControlsTab() => ShowSettingsPage(3);

        public void SetLanguage(int languageIndex)
        {
            currentLanguage = Mathf.Clamp(languageIndex, 0, 1);
            PlayerPrefs.SetInt(LanguageKey, currentLanguage);
        }

        public void SetMasterVolume(float volume)
        {
            currentMasterVolume = Mathf.Clamp01(volume);
            PlayerPrefs.SetFloat(MasterVolumeKey, currentMasterVolume);
            ApplyCurrentAudioSettings();
            UpdateVolumeValueText();
        }

        public void SetMuted(bool muted)
        {
            isMuted = muted;
            PlayerPrefs.SetInt(MutedKey, isMuted ? 1 : 0);
            ApplyCurrentAudioSettings();
            UpdateVolumeValueText();
        }

        public void SetResolution(int resolutionIndex)
        {
            if (availableResolutions.Count == 0)
            {
                return;
            }

            currentResolutionIndex = Mathf.Clamp(
                resolutionIndex,
                0,
                availableResolutions.Count - 1
            );
            Vector2Int resolution = availableResolutions[currentResolutionIndex];

            PlayerPrefs.SetInt(ResolutionWidthKey, resolution.x);
            PlayerPrefs.SetInt(ResolutionHeightKey, resolution.y);
            Screen.SetResolution(resolution.x, resolution.y, isFullscreen);
        }

        public void SetQuality(int qualityIndex)
        {
            int qualityCount = QualitySettings.names.Length;
            if (qualityCount == 0)
            {
                return;
            }

            currentQuality = Mathf.Clamp(qualityIndex, 0, qualityCount - 1);
            PlayerPrefs.SetInt(QualityKey, currentQuality);
            QualitySettings.SetQualityLevel(currentQuality, true);
        }

        public void SetFullscreen(bool fullscreen)
        {
            isFullscreen = fullscreen;
            PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
            Screen.fullScreen = isFullscreen;
        }

        public void SetVSync(bool enabled)
        {
            isVSyncEnabled = enabled;
            PlayerPrefs.SetInt(VSyncKey, isVSyncEnabled ? 1 : 0);
            QualitySettings.vSyncCount = isVSyncEnabled ? 1 : 0;
        }

        public void ResetAudioSettings()
        {
            currentMasterVolume = DefaultMasterVolume;
            isMuted = false;
            ApplyCurrentAudioSettings();
            SyncSettingsUI();
            SaveAllSettings();
        }

        public void ResetAllSettings()
        {
            currentLanguage = VietnameseLanguage;
            currentMasterVolume = DefaultMasterVolume;
            isMuted = false;
            isFullscreen = true;
            isVSyncEnabled = true;

            int qualityCount = QualitySettings.names.Length;
            currentQuality = qualityCount > 0
                ? Mathf.Clamp(2, 0, qualityCount - 1)
                : 0;

            currentResolutionIndex = FindResolutionIndex(
                Screen.currentResolution.width,
                Screen.currentResolution.height
            );

            ApplyCurrentAudioSettings();
            ApplyCurrentGraphicsSettings();
            SyncSettingsUI();
            SaveAllSettings();
        }

        public void ReloadCurrentScene()
        {
            Scene currentScene = gameObject.scene;
            SceneManager.LoadScene(currentScene.name, LoadSceneMode.Single);
        }

        public void ExitGame()
        {
            Debug.Log("[Green Field] Thoát game.", this);

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnDisable() => SaveAllSettings();

        private void PopulateSettingsDropdowns()
        {
            PopulateLanguageDropdown();
            PopulateQualityDropdown();
            PopulateResolutionDropdown();
        }

        private void PopulateLanguageDropdown()
        {
            if (languageDropdown == null)
            {
                return;
            }

            languageDropdown.ClearOptions();
            languageDropdown.AddOptions(
                new List<string> { "Tiếng Việt", "English" }
            );
        }

        private void PopulateQualityDropdown()
        {
            if (qualityDropdown == null)
            {
                return;
            }

            qualityDropdown.ClearOptions();
            qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
        }

        private void PopulateResolutionDropdown()
        {
            availableResolutions.Clear();
            List<string> options = new List<string>();

            foreach (Resolution resolution in Screen.resolutions)
            {
                Vector2Int size = new Vector2Int(
                    resolution.width,
                    resolution.height
                );

                if (availableResolutions.Contains(size))
                {
                    continue;
                }

                availableResolutions.Add(size);
                options.Add(size.x + " x " + size.y);
            }

            if (availableResolutions.Count == 0)
            {
                Vector2Int current = new Vector2Int(Screen.width, Screen.height);
                availableResolutions.Add(current);
                options.Add(current.x + " x " + current.y);
            }

            if (resolutionDropdown != null)
            {
                resolutionDropdown.ClearOptions();
                resolutionDropdown.AddOptions(options);
            }
        }

        private void LoadSettings()
        {
            currentLanguage = Mathf.Clamp(
                PlayerPrefs.GetInt(LanguageKey, VietnameseLanguage),
                0,
                1
            );
            currentMasterVolume = Mathf.Clamp01(
                PlayerPrefs.GetFloat(MasterVolumeKey, DefaultMasterVolume)
            );
            isMuted = PlayerPrefs.GetInt(MutedKey, 0) == 1;

            int qualityCount = QualitySettings.names.Length;
            currentQuality = qualityCount > 0
                ? Mathf.Clamp(
                    PlayerPrefs.GetInt(QualityKey, QualitySettings.GetQualityLevel()),
                    0,
                    qualityCount - 1
                )
                : 0;

            isFullscreen = PlayerPrefs.GetInt(
                FullscreenKey,
                Screen.fullScreen ? 1 : 0
            ) == 1;
            isVSyncEnabled = PlayerPrefs.GetInt(
                VSyncKey,
                QualitySettings.vSyncCount > 0 ? 1 : 0
            ) == 1;

            int savedWidth = PlayerPrefs.GetInt(ResolutionWidthKey, Screen.width);
            int savedHeight = PlayerPrefs.GetInt(ResolutionHeightKey, Screen.height);
            currentResolutionIndex = FindResolutionIndex(savedWidth, savedHeight);

            ApplyCurrentAudioSettings();
            SyncSettingsUI();
        }

        private void ShowSettingsPage(int pageIndex)
        {
            SetPageActive(generalSettingsPage, pageIndex == 0);
            SetPageActive(audioSettingsPage, pageIndex == 1);
            SetPageActive(graphicsSettingsPage, pageIndex == 2);
            SetPageActive(controlsSettingsPage, pageIndex == 3);

            SetTabSelected(generalTabButton, pageIndex == 0);
            SetTabSelected(audioTabButton, pageIndex == 1);
            SetTabSelected(graphicsTabButton, pageIndex == 2);
            SetTabSelected(controlsTabButton, pageIndex == 3);
        }

        private static void SetPageActive(GameObject page, bool active)
        {
            if (page != null)
            {
                page.SetActive(active);
            }
        }

        private static void SetTabSelected(Button button, bool selected)
        {
            if (button != null)
            {
                button.interactable = !selected;
            }
        }

        private void SyncSettingsUI()
        {
            languageDropdown?.SetValueWithoutNotify(currentLanguage);
            masterVolumeSlider?.SetValueWithoutNotify(currentMasterVolume);
            muteToggle?.SetIsOnWithoutNotify(isMuted);
            resolutionDropdown?.SetValueWithoutNotify(currentResolutionIndex);
            qualityDropdown?.SetValueWithoutNotify(currentQuality);
            fullscreenToggle?.SetIsOnWithoutNotify(isFullscreen);
            vSyncToggle?.SetIsOnWithoutNotify(isVSyncEnabled);
            UpdateVolumeValueText();
        }

        private void ApplyCurrentAudioSettings()
        {
            AudioListener.volume = isMuted ? 0f : currentMasterVolume;
        }

        private void ApplyCurrentGraphicsSettings()
        {
            if (QualitySettings.names.Length > 0)
            {
                QualitySettings.SetQualityLevel(currentQuality, true);
            }

            QualitySettings.vSyncCount = isVSyncEnabled ? 1 : 0;

            if (availableResolutions.Count > 0)
            {
                Vector2Int resolution = availableResolutions[
                    Mathf.Clamp(
                        currentResolutionIndex,
                        0,
                        availableResolutions.Count - 1
                    )
                ];
                Screen.SetResolution(
                    resolution.x,
                    resolution.y,
                    isFullscreen
                );
            }
            else
            {
                Screen.fullScreen = isFullscreen;
            }
        }

        private int FindResolutionIndex(int width, int height)
        {
            for (int index = 0; index < availableResolutions.Count; index++)
            {
                Vector2Int resolution = availableResolutions[index];
                if (resolution.x == width && resolution.y == height)
                {
                    return index;
                }
            }

            return Mathf.Max(0, availableResolutions.Count - 1);
        }

        private void UpdateVolumeValueText()
        {
            if (masterVolumeValueText == null)
            {
                return;
            }

            masterVolumeValueText.text = isMuted
                ? "TẮT"
                : Mathf.RoundToInt(currentMasterVolume * 100f) + "%";
        }

        private void SaveAllSettings()
        {
            PlayerPrefs.SetInt(LanguageKey, currentLanguage);
            PlayerPrefs.SetFloat(MasterVolumeKey, currentMasterVolume);
            PlayerPrefs.SetInt(MutedKey, isMuted ? 1 : 0);
            PlayerPrefs.SetInt(QualityKey, currentQuality);
            PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
            PlayerPrefs.SetInt(VSyncKey, isVSyncEnabled ? 1 : 0);

            if (availableResolutions.Count > 0)
            {
                Vector2Int resolution = availableResolutions[
                    Mathf.Clamp(
                        currentResolutionIndex,
                        0,
                        availableResolutions.Count - 1
                    )
                ];
                PlayerPrefs.SetInt(ResolutionWidthKey, resolution.x);
                PlayerPrefs.SetInt(ResolutionHeightKey, resolution.y);
            }

            PlayerPrefs.Save();
        }

        private void LoadSavedGame()
        {
            if (!SaveManager.HasSaveGame)
            {
                Debug.LogWarning("[Green Field] Chưa có dữ liệu lưu.", this);
                RefreshSaveButtons();
                return;
            }

            if (!CanLoadGameplayScene())
                return;

            SaveManager.RequestLoadOnNextGameplayScene();
            SceneManager.LoadScene(farmSceneName, LoadSceneMode.Single);
        }

        private void RefreshSaveButtons()
        {
            bool hasSave = SaveManager.HasSaveGame;
            Button[] buttons = GetComponentsInChildren<Button>(true);

            foreach (Button button in buttons)
            {
                if (button != null &&
                    (button.name == "Button_Continue" ||
                     button.name == "Button_Load"))
                {
                    button.interactable = hasSave;
                }
            }
        }

        private bool CanLoadGameplayScene()
        {
            if (string.IsNullOrWhiteSpace(farmSceneName))
            {
                Debug.LogError("[Green Field] Farm Scene Name đang để trống.");
                return false;
            }

            if (!Application.CanStreamedLevelBeLoaded(farmSceneName))
            {
                Debug.LogError(
                    "[Green Field] Không thể load scene '" + farmSceneName + "'. " +
                    "Hãy chạy Tools > Green Field > Build Main Menu để thêm MAPMAIN vào Build Settings."
                );
                return false;
            }

            return true;
        }
    }
}
