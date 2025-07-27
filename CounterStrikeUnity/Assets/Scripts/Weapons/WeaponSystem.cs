using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class WeaponData
{
    public string weaponName;
    public float damage;
    public float fireRate;
    public float range;
    public int maxAmmo;
    public int currentAmmo;
    public int reserveAmmo;
    public float reloadTime;
    public float recoilAmount;
    public bool isAutomatic;
    public bool hasScope;
    public int price;
    public WeaponType weaponType;
    
    public WeaponData(string name, float dmg, float rate, float rng, int maxAmm, int resAmm, 
                     float reloadT, float recoil, bool auto, bool scope, int cost, WeaponType type)
    {
        weaponName = name;
        damage = dmg;
        fireRate = rate;
        range = rng;
        maxAmmo = maxAmm;
        currentAmmo = maxAmm;
        reserveAmmo = resAmm;
        reloadTime = reloadT;
        recoilAmount = recoil;
        isAutomatic = auto;
        hasScope = scope;
        price = cost;
        weaponType = type;
    }
}

public enum WeaponType
{
    Pistol,
    Rifle,
    Sniper,
    Grenade
}

public class WeaponSystem : MonoBehaviour
{
    [Header("Weapon Settings")]
    public Transform firePoint;
    public Camera playerCamera;
    public LayerMask enemyLayers;
    public GameObject muzzleFlash;
    public GameObject bulletHole;
    public GameObject bloodEffect;
    
    [Header("Crosshair")]
    public float baseCrosshairSize = 20f;
    public float maxCrosshairSize = 100f;
    public float crosshairExpansion = 5f;
    public float crosshairRecovery = 2f;
    
    [Header("Recoil Settings")]
    public float recoilRecoverySpeed = 5f;
    public float maxRecoilAngle = 30f;
    
    // Current weapon data
    private List<WeaponData> weapons = new List<WeaponData>();
    private int currentWeaponIndex = 0;
    private WeaponData currentWeapon;
    
    // Firing variables
    private bool isFiring = false;
    private bool canFire = true;
    private float nextFireTime = 0f;
    private bool isReloading = false;
    private bool isScoped = false;
    
    // Recoil variables
    private Vector2 currentRecoil = Vector2.zero;
    private Vector2 targetRecoil = Vector2.zero;
    private float recoilPattern = 0f;
    
    // Crosshair variables
    private float currentCrosshairSize;
    
    // Audio
    private AudioSource audioSource;
    
    // References
    private PlayerController playerController;
    
    void Start()
    {
        InitializeWeapons();
        audioSource = GetComponent<AudioSource>();
        playerController = GetComponent<PlayerController>();
        currentCrosshairSize = baseCrosshairSize;
        
        if (weapons.Count > 0)
        {
            currentWeapon = weapons[0];
        }
    }
    
    void InitializeWeapons()
    {
        // Initialize weapon data - CT starts with USP, T starts with Glock
        weapons.Add(new WeaponData("USP", 35f, 0.15f, 100f, 12, 24, 2.5f, 2f, false, false, 0, WeaponType.Pistol));
        weapons.Add(new WeaponData("Knife", 50f, 0.8f, 2f, 1, 0, 0f, 0f, false, false, 0, WeaponType.Pistol));
        
        currentWeapon = weapons[0];
    }
    
    void Update()
    {
        HandleRecoil();
        UpdateCrosshair();
        
        if (isFiring && canFire && !isReloading)
        {
            Fire();
        }
    }
    
    public void StartFiring()
    {
        if (!isReloading && currentWeapon.currentAmmo > 0)
        {
            isFiring = true;
            
            if (!currentWeapon.isAutomatic)
            {
                if (canFire)
                {
                    Fire();
                }
            }
        }
        else if (currentWeapon.currentAmmo <= 0 && currentWeapon.reserveAmmo > 0)
        {
            Reload();
        }
    }
    
    public void StopFiring()
    {
        isFiring = false;
    }
    
    void Fire()
    {
        if (Time.time < nextFireTime || isReloading)
            return;
            
        if (currentWeapon.currentAmmo <= 0)
        {
            // Play empty clip sound
            return;
        }
        
        // Set next fire time
        nextFireTime = Time.time + currentWeapon.fireRate;
        canFire = false;
        StartCoroutine(ResetFireRate());
        
        // Consume ammo
        currentWeapon.currentAmmo--;
        
        // Apply recoil
        ApplyRecoil();
        
        // Expand crosshair
        currentCrosshairSize = Mathf.Min(currentCrosshairSize + crosshairExpansion, maxCrosshairSize);
        
        // Perform raycast
        PerformRaycast();
        
        // Visual and audio effects
        ShowMuzzleFlash();
        PlayFireSound();
        
        // Auto reload if empty
        if (currentWeapon.currentAmmo <= 0 && currentWeapon.reserveAmmo > 0)
        {
            StartCoroutine(AutoReload());
        }
    }
    
    void PerformRaycast()
    {
        Vector3 rayOrigin = playerCamera.transform.position;
        Vector3 rayDirection = playerCamera.transform.forward;
        
        // Add spread based on crosshair size
        float spread = (currentCrosshairSize / baseCrosshairSize) * 0.1f;
        rayDirection += Random.insideUnitSphere * spread;
        rayDirection.Normalize();
        
        RaycastHit hit;
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, currentWeapon.range))
        {
            // Check if hit enemy
            if (hit.collider.CompareTag("Enemy"))
            {
                AIController enemy = hit.collider.GetComponent<AIController>();
                if (enemy != null)
                {
                    // Calculate damage based on hit location
                    float damage = currentWeapon.damage;
                    if (hit.collider.name.Contains("Head"))
                        damage *= 2f; // Headshot multiplier
                    
                    enemy.TakeDamage(damage);
                    
                    // Show blood effect
                    if (bloodEffect != null)
                    {
                        GameObject blood = Instantiate(bloodEffect, hit.point, Quaternion.LookRotation(hit.normal));
                        Destroy(blood, 2f);
                    }
                    
                    // Award money for kill
                    if (enemy.currentHealth <= 0)
                    {
                        EconomySystem.Instance?.AddMoney(500);
                    }
                }
            }
            else
            {
                // Create bullet hole
                if (bulletHole != null)
                {
                    GameObject hole = Instantiate(bulletHole, hit.point, Quaternion.LookRotation(hit.normal));
                    Destroy(hole, 10f);
                }
            }
            
            // Apply impact force if rigidbody
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(-hit.normal * 500f);
            }
        }
    }
    
    void ApplyRecoil()
    {
        // Calculate recoil pattern
        float horizontalRecoil = Random.Range(-currentWeapon.recoilAmount * 0.5f, currentWeapon.recoilAmount * 0.5f);
        float verticalRecoil = currentWeapon.recoilAmount;
        
        // Add to recoil pattern for continuous fire
        recoilPattern += 1f;
        verticalRecoil += recoilPattern * 0.5f;
        
        targetRecoil += new Vector2(horizontalRecoil, verticalRecoil);
        targetRecoil.y = Mathf.Clamp(targetRecoil.y, 0, maxRecoilAngle);
    }
    
    void HandleRecoil()
    {
        // Apply recoil to camera
        currentRecoil = Vector2.Lerp(currentRecoil, targetRecoil, Time.deltaTime * 10f);
        playerCamera.transform.localRotation = Quaternion.Euler(-currentRecoil.y, currentRecoil.x, 0);
        
        // Recover from recoil
        if (!isFiring)
        {
            targetRecoil = Vector2.Lerp(targetRecoil, Vector2.zero, Time.deltaTime * recoilRecoverySpeed);
            recoilPattern = Mathf.Lerp(recoilPattern, 0f, Time.deltaTime * 3f);
        }
    }
    
    void UpdateCrosshair()
    {
        // Recover crosshair size
        if (!isFiring)
        {
            currentCrosshairSize = Mathf.Lerp(currentCrosshairSize, baseCrosshairSize, Time.deltaTime * crosshairRecovery);
        }
    }
    
    public void Reload()
    {
        if (isReloading || currentWeapon.reserveAmmo <= 0 || currentWeapon.currentAmmo >= currentWeapon.maxAmmo)
            return;
            
        StartCoroutine(ReloadCoroutine());
    }
    
    IEnumerator ReloadCoroutine()
    {
        isReloading = true;
        
        // Play reload animation/sound
        PlayReloadSound();
        
        yield return new WaitForSeconds(currentWeapon.reloadTime);
        
        // Calculate ammo to reload
        int ammoNeeded = currentWeapon.maxAmmo - currentWeapon.currentAmmo;
        int ammoToReload = Mathf.Min(ammoNeeded, currentWeapon.reserveAmmo);
        
        currentWeapon.currentAmmo += ammoToReload;
        currentWeapon.reserveAmmo -= ammoToReload;
        
        isReloading = false;
    }
    
    IEnumerator AutoReload()
    {
        yield return new WaitForSeconds(0.5f);
        if (currentWeapon.currentAmmo <= 0 && currentWeapon.reserveAmmo > 0)
        {
            Reload();
        }
    }
    
    IEnumerator ResetFireRate()
    {
        yield return new WaitForSeconds(currentWeapon.fireRate);
        canFire = true;
    }
    
    public void SwitchWeapon(int weaponIndex)
    {
        if (weaponIndex >= 0 && weaponIndex < weapons.Count && weaponIndex != currentWeaponIndex)
        {
            currentWeaponIndex = weaponIndex;
            currentWeapon = weapons[currentWeaponIndex];
            
            // Reset firing state
            isFiring = false;
            isReloading = false;
            
            // Play weapon switch sound
            PlaySwitchSound();
        }
    }
    
    public void SwitchToNextWeapon()
    {
        int nextIndex = (currentWeaponIndex + 1) % weapons.Count;
        SwitchWeapon(nextIndex);
    }
    
    public void SwitchToPreviousWeapon()
    {
        int prevIndex = (currentWeaponIndex - 1 + weapons.Count) % weapons.Count;
        SwitchWeapon(prevIndex);
    }
    
    public void AddWeapon(WeaponData newWeapon)
    {
        // Check if weapon already exists
        for (int i = 0; i < weapons.Count; i++)
        {
            if (weapons[i].weaponName == newWeapon.weaponName)
            {
                // Update existing weapon
                weapons[i] = newWeapon;
                return;
            }
        }
        
        // Add new weapon
        weapons.Add(newWeapon);
    }
    
    public void ToggleScope()
    {
        if (currentWeapon.hasScope)
        {
            isScoped = !isScoped;
            
            if (isScoped)
            {
                // Zoom in
                playerCamera.fieldOfView = 30f;
            }
            else
            {
                // Zoom out
                playerCamera.fieldOfView = 60f;
            }
        }
    }
    
    void ShowMuzzleFlash()
    {
        if (muzzleFlash != null)
        {
            muzzleFlash.SetActive(true);
            StartCoroutine(HideMuzzleFlash());
        }
    }
    
    IEnumerator HideMuzzleFlash()
    {
        yield return new WaitForSeconds(0.05f);
        if (muzzleFlash != null)
            muzzleFlash.SetActive(false);
    }
    
    void PlayFireSound()
    {
        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            // audioSource.PlayOneShot(fireSound);
        }
    }
    
    void PlayReloadSound()
    {
        if (audioSource != null)
        {
            // audioSource.PlayOneShot(reloadSound);
        }
    }
    
    void PlaySwitchSound()
    {
        if (audioSource != null)
        {
            // audioSource.PlayOneShot(switchSound);
        }
    }
    
    // Public getters
    public WeaponData GetCurrentWeapon()
    {
        return currentWeapon;
    }
    
    public float GetCrosshairSize()
    {
        return currentCrosshairSize;
    }
    
    public bool IsReloading()
    {
        return isReloading;
    }
    
    public bool IsScoped()
    {
        return isScoped;
    }
    
    // Static weapon data for shop
    public static WeaponData GetWeaponData(string weaponName)
    {
        switch (weaponName)
        {
            case "M4A1":
                return new WeaponData("M4A1", 33f, 0.09f, 150f, 30, 90, 3.1f, 2f, true, false, 2500, WeaponType.Rifle);
            case "AK47":
                return new WeaponData("AK47", 36f, 0.1f, 150f, 30, 90, 2.5f, 3f, true, false, 2700, WeaponType.Rifle);
            case "AWP":
                return new WeaponData("AWP", 115f, 1.5f, 200f, 10, 30, 3.7f, 5f, false, true, 4750, WeaponType.Sniper);
            case "Desert Eagle":
                return new WeaponData("Desert Eagle", 54f, 0.27f, 100f, 7, 35, 2.2f, 3f, false, false, 650, WeaponType.Pistol);
            case "Glock":
                return new WeaponData("Glock", 28f, 0.15f, 100f, 20, 40, 2.2f, 1.5f, false, false, 0, WeaponType.Pistol);
            default:
                return null;
        }
    }
}