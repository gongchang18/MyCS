using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject creditsPanel;
    
    [Header("Main Menu Buttons")]
    public Button playButton;
    public Button settingsButton;
    public Button creditsButton;
    public Button quitButton;
    
    [Header("Settings")]
    public Slider mouseSensitivitySlider;
    public Slider masterVolumeSlider;
    public Toggle fullscreenToggle;
    public Dropdown resolutionDropdown;
    public Button backButton;
    
    [Header("Game Settings")]
    public string gameSceneName = "GameScene";
    
    // Settings values
    private float mouseSensitivity = 2f;
    private float masterVolume = 1f;
    private bool isFullscreen = true;
    
    void Start()
    {
        InitializeMenu();
        LoadSettings();
        SetupButtons();
        SetupResolutionDropdown();
    }
    
    void InitializeMenu()
    {
        // Show main menu, hide others
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        
        // Set cursor state
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    void SetupButtons()
    {
        // Main menu buttons
        if (playButton != null)
            playButton.onClick.AddListener(StartGame);
            
        if (settingsButton != null)
            settingsButton.onClick.AddListener(ShowSettings);
            
        if (creditsButton != null)
            creditsButton.onClick.AddListener(ShowCredits);
            
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
            
        // Settings buttons
        if (backButton != null)
            backButton.onClick.AddListener(ShowMainMenu);
            
        // Settings sliders
        if (mouseSensitivitySlider != null)
        {
            mouseSensitivitySlider.value = mouseSensitivity;
            mouseSensitivitySlider.onValueChanged.AddListener(OnMouseSensitivityChanged);
        }
        
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = masterVolume;
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }
        
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = isFullscreen;
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenToggled);
        }
        
        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
        }
    }
    
    void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null) return;
        
        resolutionDropdown.ClearOptions();
        
        Resolution[] resolutions = Screen.resolutions;
        System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();
        
        int currentResolutionIndex = 0;
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);
            
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }
        
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }
    
    void LoadSettings()
    {
        // Load settings from PlayerPrefs
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", 2f);
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        isFullscreen = PlayerPrefs.GetInt("Fullscreen", 1) == 1;
        
        // Apply settings
        AudioListener.volume = masterVolume;
        Screen.fullScreen = isFullscreen;
    }
    
    void SaveSettings()
    {
        // Save settings to PlayerPrefs
        PlayerPrefs.SetFloat("MouseSensitivity", mouseSensitivity);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    public void StartGame()
    {
        // Save settings before starting game
        SaveSettings();
        
        // Load game scene
        SceneManager.LoadScene(gameSceneName);
    }
    
    public void ShowSettings()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }
    
    public void ShowCredits()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }
    
    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        
        // Save settings when going back
        SaveSettings();
    }
    
    public void QuitGame()
    {
        SaveSettings();
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    void OnMouseSensitivityChanged(float value)
    {
        mouseSensitivity = value;
    }
    
    void OnMasterVolumeChanged(float value)
    {
        masterVolume = value;
        AudioListener.volume = value;
    }
    
    void OnFullscreenToggled(bool value)
    {
        isFullscreen = value;
        Screen.fullScreen = value;
    }
    
    void OnResolutionChanged(int resolutionIndex)
    {
        Resolution resolution = Screen.resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
    }
    
    // Public methods for accessing settings
    public float GetMouseSensitivity()
    {
        return mouseSensitivity;
    }
    
    public float GetMasterVolume()
    {
        return masterVolume;
    }
    
    public bool GetFullscreenSetting()
    {
        return isFullscreen;
    }
}