using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    
    [Header("HUD Elements")]
    public Canvas hudCanvas;
    public Text healthText;
    public Text armorText;
    public Text moneyText;
    public Text ammoText;
    public Text timerText;
    public Text roundInfoText;
    public Image healthBar;
    public Image armorBar;
    public Image crosshair;
    
    [Header("Buy Menu")]
    public Canvas buyMenuCanvas;
    public Transform weaponButtonContainer;
    public Transform equipmentButtonContainer;
    public Button buyMenuCloseButton;
    public GameObject weaponButtonPrefab;
    
    [Header("Game Messages")]
    public Canvas messageCanvas;
    public Text roundEndText;
    public Text matchEndText;
    public Text bombTimerText;
    public Text interactionPromptText;
    public GameObject defusingProgressPanel;
    public Slider defusingProgressSlider;
    
    [Header("Crosshair Settings")]
    public float crosshairSize = 20f;
    public Color crosshairColor = Color.white;
    
    // References
    private PlayerController playerController;
    private WeaponSystem weaponSystem;
    private EconomySystem economySystem;
    private GameManager gameManager;
    
    // Buy menu
    private List<Button> weaponButtons = new List<Button>();
    private bool isBuyMenuOpen = false;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        InitializeUI();
        FindReferences();
        SetupBuyMenu();
    }
    
    void InitializeUI()
    {
        // Initialize all UI elements
        if (buyMenuCanvas != null)
            buyMenuCanvas.gameObject.SetActive(false);
            
        if (messageCanvas != null)
            messageCanvas.gameObject.SetActive(true);
            
        if (defusingProgressPanel != null)
            defusingProgressPanel.SetActive(false);
            
        // Setup crosshair
        if (crosshair != null)
        {
            crosshair.color = crosshairColor;
            UpdateCrosshair(crosshairSize);
        }
        
        // Hide message texts initially
        if (roundEndText != null) roundEndText.gameObject.SetActive(false);
        if (matchEndText != null) matchEndText.gameObject.SetActive(false);
        if (bombTimerText != null) bombTimerText.gameObject.SetActive(false);
        if (interactionPromptText != null) interactionPromptText.gameObject.SetActive(false);
    }
    
    void FindReferences()
    {
        // Find game components
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            weaponSystem = player.GetComponent<WeaponSystem>();
        }
        
        economySystem = EconomySystem.Instance;
        gameManager = GameManager.Instance;
    }
    
    void SetupBuyMenu()
    {
        if (weaponButtonContainer == null || weaponButtonPrefab == null)
            return;
            
        // Create weapon buttons
        if (economySystem != null)
        {
            var shopItems = economySystem.GetShopItems();
            foreach (var item in shopItems)
            {
                CreateWeaponButton(item.Key, item.Value);
            }
        }
        
        // Setup equipment buttons
        CreateEquipmentButtons();
        
        // Setup close button
        if (buyMenuCloseButton != null)
        {
            buyMenuCloseButton.onClick.AddListener(CloseBuyMenu);
        }
    }
    
    void CreateWeaponButton(string weaponName, WeaponData weaponData)
    {
        if (weaponButtonPrefab == null || weaponButtonContainer == null)
            return;
            
        GameObject buttonObj = Instantiate(weaponButtonPrefab, weaponButtonContainer);
        Button button = buttonObj.GetComponent<Button>();
        Text buttonText = buttonObj.GetComponentInChildren<Text>();
        
        if (buttonText != null)
        {
            buttonText.text = $"{weaponName} - ${weaponData.price}";
        }
        
        if (button != null)
        {
            button.onClick.AddListener(() => BuyWeapon(weaponName));
            weaponButtons.Add(button);
        }
    }
    
    void CreateEquipmentButtons()
    {
        if (equipmentButtonContainer == null || weaponButtonPrefab == null)
            return;
            
        // Armor button
        GameObject armorButtonObj = Instantiate(weaponButtonPrefab, equipmentButtonContainer);
        Button armorButton = armorButtonObj.GetComponent<Button>();
        Text armorText = armorButtonObj.GetComponentInChildren<Text>();
        
        if (armorText != null)
            armorText.text = "Armor - $650";
            
        if (armorButton != null)
            armorButton.onClick.AddListener(BuyArmor);
        
        // Grenade buttons
        CreateGrenadeButton("HE Grenade", 300);
        CreateGrenadeButton("Flashbang", 200);
        CreateGrenadeButton("Smoke Grenade", 300);
    }
    
    void CreateGrenadeButton(string grenadeName, int price)
    {
        GameObject buttonObj = Instantiate(weaponButtonPrefab, equipmentButtonContainer);
        Button button = buttonObj.GetComponent<Button>();
        Text buttonText = buttonObj.GetComponentInChildren<Text>();
        
        if (buttonText != null)
            buttonText.text = $"{grenadeName} - ${price}";
            
        if (button != null)
            button.onClick.AddListener(() => BuyGrenade(grenadeName));
    }
    
    void Update()
    {
        UpdateHUD();
        UpdateCrosshairSize();
        HandleInput();
    }
    
    void UpdateHUD()
    {
        // Update health and armor
        if (playerController != null)
        {
            if (healthText != null)
                healthText.text = $"Health: {Mathf.RoundToInt(playerController.currentHealth)}";
                
            if (armorText != null)
                armorText.text = $"Armor: {Mathf.RoundToInt(playerController.currentArmor)}";
                
            if (healthBar != null)
                healthBar.fillAmount = playerController.GetHealthPercentage();
                
            if (armorBar != null)
                armorBar.fillAmount = playerController.GetArmorPercentage();
        }
        
        // Update money
        if (economySystem != null && moneyText != null)
        {
            moneyText.text = $"Money: ${economySystem.GetCurrentMoney()}";
        }
        
        // Update ammo
        if (weaponSystem != null && ammoText != null)
        {
            WeaponData currentWeapon = weaponSystem.GetCurrentWeapon();
            if (currentWeapon != null)
            {
                ammoText.text = $"{currentWeapon.currentAmmo} / {currentWeapon.reserveAmmo}";
            }
        }
        
        // Update timer
        if (gameManager != null && timerText != null)
        {
            float currentTimer = gameManager.GetCurrentTimer();
            int minutes = Mathf.FloorToInt(currentTimer / 60);
            int seconds = Mathf.FloorToInt(currentTimer % 60);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }
    
    void UpdateCrosshairSize()
    {
        if (weaponSystem != null && crosshair != null)
        {
            float currentSize = weaponSystem.GetCrosshairSize();
            UpdateCrosshair(currentSize);
        }
    }
    
    void UpdateCrosshair(float size)
    {
        if (crosshair != null)
        {
            RectTransform rectTransform = crosshair.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.sizeDelta = new Vector2(size, size);
            }
        }
    }
    
    void HandleInput()
    {
        // Toggle buy menu with B key
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (gameManager != null && gameManager.GetCurrentState() == GameState.BuyPhase)
            {
                ToggleBuyMenu();
            }
        }
        
        // Close buy menu with Escape
        if (Input.GetKeyDown(KeyCode.Escape) && isBuyMenuOpen)
        {
            CloseBuyMenu();
        }
    }
    
    public void ShowBuyMenu(bool show)
    {
        if (buyMenuCanvas != null)
        {
            buyMenuCanvas.gameObject.SetActive(show);
            isBuyMenuOpen = show;
            
            // Update cursor state
            if (show)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            
            UpdateBuyMenuButtons();
        }
    }
    
    void ToggleBuyMenu()
    {
        ShowBuyMenu(!isBuyMenuOpen);
    }
    
    void CloseBuyMenu()
    {
        ShowBuyMenu(false);
    }
    
    void UpdateBuyMenuButtons()
    {
        if (economySystem == null) return;
        
        int currentMoney = economySystem.GetCurrentMoney();
        
        // Update weapon buttons
        foreach (Button button in weaponButtons)
        {
            Text buttonText = button.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                string weaponName = buttonText.text.Split(' ')[0];
                WeaponData weaponData = WeaponSystem.GetWeaponData(weaponName);
                
                if (weaponData != null)
                {
                    bool canAfford = currentMoney >= weaponData.price;
                    button.interactable = canAfford;
                    
                    // Change color based on affordability
                    ColorBlock colors = button.colors;
                    colors.normalColor = canAfford ? Color.white : Color.gray;
                    button.colors = colors;
                }
            }
        }
    }
    
    void BuyWeapon(string weaponName)
    {
        if (economySystem != null)
        {
            bool success = economySystem.BuyWeapon(weaponName);
            if (success)
            {
                Debug.Log($"Purchased {weaponName}");
                UpdateBuyMenuButtons();
            }
            else
            {
                Debug.Log($"Cannot afford {weaponName}");
            }
        }
    }
    
    void BuyArmor()
    {
        if (economySystem != null)
        {
            bool success = economySystem.BuyArmor();
            if (success)
            {
                Debug.Log("Purchased armor");
                UpdateBuyMenuButtons();
            }
            else
            {
                Debug.Log("Cannot afford armor");
            }
        }
    }
    
    void BuyGrenade(string grenadeType)
    {
        if (economySystem != null)
        {
            bool success = economySystem.BuyGrenade(grenadeType);
            if (success)
            {
                Debug.Log($"Purchased {grenadeType}");
                UpdateBuyMenuButtons();
            }
            else
            {
                Debug.Log($"Cannot afford {grenadeType}");
            }
        }
    }
    
    public void UpdateTimer(int seconds)
    {
        if (timerText != null)
        {
            int minutes = seconds / 60;
            int remainingSeconds = seconds % 60;
            timerText.text = $"{minutes:00}:{remainingSeconds:00}";
        }
    }
    
    public void UpdateRoundInfo(int round, int tScore, int ctScore)
    {
        if (roundInfoText != null)
        {
            roundInfoText.text = $"Round {round} | T: {tScore} - CT: {ctScore}";
        }
    }
    
    public void UpdateMoneyDisplay(int money)
    {
        if (moneyText != null)
        {
            moneyText.text = $"Money: ${money}";
        }
    }
    
    public void ShowRoundEndMessage(string message)
    {
        if (roundEndText != null)
        {
            roundEndText.text = message;
            roundEndText.gameObject.SetActive(true);
            
            // Hide after a few seconds
            StartCoroutine(HideMessageAfterDelay(roundEndText.gameObject, 3f));
        }
    }
    
    public void ShowMatchEndMessage(string message)
    {
        if (matchEndText != null)
        {
            matchEndText.text = message;
            matchEndText.gameObject.SetActive(true);
        }
    }
    
    public void ShowBombPlantedMessage()
    {
        ShowRoundEndMessage("Bomb has been planted!");
        
        if (bombTimerText != null)
        {
            bombTimerText.gameObject.SetActive(true);
        }
    }
    
    public void UpdateBombTimer(int seconds)
    {
        if (bombTimerText != null)
        {
            bombTimerText.text = $"Bomb: {seconds}s";
        }
    }
    
    public void ShowInteractionPrompt(string message)
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.text = message;
            interactionPromptText.gameObject.SetActive(true);
        }
    }
    
    public void HideInteractionPrompt()
    {
        if (interactionPromptText != null)
        {
            interactionPromptText.gameObject.SetActive(false);
        }
    }
    
    public void ShowDefusingProgress(float progress)
    {
        if (defusingProgressPanel != null)
        {
            defusingProgressPanel.SetActive(true);
            
            if (defusingProgressSlider != null)
            {
                defusingProgressSlider.value = progress;
            }
        }
    }
    
    public void HideDefusingProgress()
    {
        if (defusingProgressPanel != null)
        {
            defusingProgressPanel.SetActive(false);
        }
    }
    
    System.Collections.IEnumerator HideMessageAfterDelay(GameObject messageObject, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (messageObject != null)
        {
            messageObject.SetActive(false);
        }
    }
    
    // Public methods for external access
    public bool IsBuyMenuOpen()
    {
        return isBuyMenuOpen;
    }
    
    public void SetCrosshairColor(Color color)
    {
        crosshairColor = color;
        if (crosshair != null)
        {
            crosshair.color = color;
        }
    }
    
    public void SetCrosshairSize(float size)
    {
        crosshairSize = size;
        UpdateCrosshair(size);
    }
}