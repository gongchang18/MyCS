using UnityEngine;
using System.Collections.Generic;

public class EconomySystem : MonoBehaviour
{
    public static EconomySystem Instance { get; private set; }
    
    [Header("Economy Settings")]
    public int startingMoney = 800;
    public int winReward = 3000;
    public int loseReward = 1400;
    public int killReward = 500;
    public int bombPlantReward = 800;
    public int bombDefuseReward = 250;
    public int maxMoney = 16000;
    
    [Header("Round Economy")]
    public int consecutiveLossBonus = 500;
    public int maxLossBonus = 3400;
    
    // Current money
    private int currentMoney;
    private int consecutiveLosses = 0;
    private bool lastRoundWon = false;
    
    // Shop items
    private Dictionary<string, WeaponData> shopItems;
    
    // References
    private WeaponSystem weaponSystem;
    private PlayerController playerController;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeEconomy();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // Find references
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            weaponSystem = player.GetComponent<WeaponSystem>();
            playerController = player.GetComponent<PlayerController>();
        }
    }
    
    void InitializeEconomy()
    {
        currentMoney = startingMoney;
        InitializeShop();
    }
    
    void InitializeShop()
    {
        shopItems = new Dictionary<string, WeaponData>();
        
        // Add weapons to shop
        shopItems.Add("M4A1", WeaponSystem.GetWeaponData("M4A1"));
        shopItems.Add("AK47", WeaponSystem.GetWeaponData("AK47"));
        shopItems.Add("AWP", WeaponSystem.GetWeaponData("AWP"));
        shopItems.Add("Desert Eagle", WeaponSystem.GetWeaponData("Desert Eagle"));
        
        // Add equipment
        // Note: Armor and utilities would be handled separately
    }
    
    public void AddMoney(int amount)
    {
        currentMoney += amount;
        currentMoney = Mathf.Clamp(currentMoney, 0, maxMoney);
        
        // Update UI
        UIManager.Instance?.UpdateMoneyDisplay(currentMoney);
    }
    
    public void SpendMoney(int amount)
    {
        currentMoney -= amount;
        currentMoney = Mathf.Max(currentMoney, 0);
        
        // Update UI
        UIManager.Instance?.UpdateMoneyDisplay(currentMoney);
    }
    
    public bool CanAfford(int price)
    {
        return currentMoney >= price;
    }
    
    public bool BuyWeapon(string weaponName)
    {
        if (shopItems.ContainsKey(weaponName))
        {
            WeaponData weapon = shopItems[weaponName];
            
            if (CanAfford(weapon.price))
            {
                SpendMoney(weapon.price);
                
                // Add weapon to player's inventory
                if (weaponSystem != null)
                {
                    weaponSystem.AddWeapon(weapon);
                }
                
                return true;
            }
        }
        
        return false;
    }
    
    public bool BuyArmor()
    {
        int armorPrice = 650;
        
        if (CanAfford(armorPrice))
        {
            SpendMoney(armorPrice);
            
            // Add armor to player
            if (playerController != null)
            {
                playerController.AddArmor(100f);
            }
            
            return true;
        }
        
        return false;
    }
    
    public bool BuyGrenade(string grenadeType)
    {
        int grenadePrice = GetGrenadePrice(grenadeType);
        
        if (CanAfford(grenadePrice))
        {
            SpendMoney(grenadePrice);
            
            // Add grenade to inventory
            // This would need to be implemented in a grenade system
            
            return true;
        }
        
        return false;
    }
    
    int GetGrenadePrice(string grenadeType)
    {
        switch (grenadeType)
        {
            case "HE Grenade":
                return 300;
            case "Flashbang":
                return 200;
            case "Smoke Grenade":
                return 300;
            default:
                return 0;
        }
    }
    
    public void OnRoundEnd(bool won)
    {
        lastRoundWon = won;
        
        if (won)
        {
            AddMoney(winReward);
            consecutiveLosses = 0;
        }
        else
        {
            consecutiveLosses++;
            int lossBonus = Mathf.Min(consecutiveLosses * consecutiveLossBonus, maxLossBonus);
            AddMoney(loseReward + lossBonus);
        }
    }
    
    public void OnKill()
    {
        AddMoney(killReward);
    }
    
    public void OnBombPlant()
    {
        AddMoney(bombPlantReward);
    }
    
    public void OnBombDefuse()
    {
        AddMoney(bombDefuseReward);
    }
    
    public void ResetForNewMatch()
    {
        currentMoney = startingMoney;
        consecutiveLosses = 0;
        lastRoundWon = false;
    }
    
    // Getters
    public int GetCurrentMoney()
    {
        return currentMoney;
    }
    
    public Dictionary<string, WeaponData> GetShopItems()
    {
        return shopItems;
    }
    
    public int GetConsecutiveLosses()
    {
        return consecutiveLosses;
    }
    
    public bool GetLastRoundResult()
    {
        return lastRoundWon;
    }
}