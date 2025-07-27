using UnityEngine;
using System.Collections;

public class BombSite : MonoBehaviour
{
    [Header("Bomb Site Settings")]
    public string siteName = "A";
    public float interactionRange = 3f;
    public GameObject bombModel;
    public ParticleSystem explosionEffect;
    public AudioSource audioSource;
    
    [Header("Visual Indicators")]
    public GameObject siteIndicator;
    public Color normalColor = Color.green;
    public Color plantedColor = Color.red;
    public Color defusingColor = Color.yellow;
    
    // Bomb state
    private bool isBombPlanted = false;
    private bool isBeingDefused = false;
    private float defuseProgress = 0f;
    private GameObject plantedBomb = null;
    
    // Interaction
    private PlayerController playerInRange = null;
    private AIController aiInRange = null;
    
    // Components
    private Renderer siteRenderer;
    private Collider siteCollider;
    
    void Start()
    {
        InitializeBombSite();
    }
    
    void InitializeBombSite()
    {
        // Get components
        siteCollider = GetComponent<Collider>();
        if (siteCollider == null)
        {
            // Add a trigger collider if none exists
            siteCollider = gameObject.AddComponent<BoxCollider>();
            siteCollider.isTrigger = true;
        }
        
        // Setup visual indicator
        if (siteIndicator != null)
        {
            siteRenderer = siteIndicator.GetComponent<Renderer>();
            if (siteRenderer != null)
            {
                siteRenderer.material.color = normalColor;
            }
        }
        
        // Hide bomb model initially
        if (bombModel != null)
        {
            bombModel.SetActive(false);
        }
        
        // Setup audio source
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; // 3D sound
    }
    
    void Update()
    {
        HandlePlayerInteraction();
        UpdateVisuals();
    }
    
    void HandlePlayerInteraction()
    {
        if (playerInRange != null && !isBombPlanted)
        {
            // Check if player can plant bomb (if they're terrorist with bomb)
            // For now, we'll assume only AI can plant bombs as per the game design
        }
        
        if (playerInRange != null && isBombPlanted && !isBeingDefused)
        {
            // Player can defuse bomb
            if (Input.GetKey(KeyCode.E))
            {
                StartDefusing(playerInRange);
            }
        }
        
        if (isBeingDefused && playerInRange != null)
        {
            if (Input.GetKey(KeyCode.E))
            {
                ContinueDefusing();
            }
            else
            {
                StopDefusing();
            }
        }
    }
    
    void UpdateVisuals()
    {
        if (siteRenderer != null)
        {
            if (isBeingDefused)
            {
                siteRenderer.material.color = defusingColor;
            }
            else if (isBombPlanted)
            {
                siteRenderer.material.color = plantedColor;
            }
            else
            {
                siteRenderer.material.color = normalColor;
            }
        }
        
        // Update bomb model visibility
        if (bombModel != null)
        {
            bombModel.SetActive(isBombPlanted);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Check if player entered
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            playerInRange = player;
            
            // Show interaction prompt
            if (isBombPlanted && !isBeingDefused)
            {
                UIManager.Instance?.ShowInteractionPrompt("Hold E to defuse bomb");
            }
        }
        
        // Check if AI entered
        AIController ai = other.GetComponent<AIController>();
        if (ai != null)
        {
            aiInRange = ai;
        }
    }
    
    void OnTriggerExit(Collider other)
    {
        // Check if player left
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player == playerInRange)
        {
            playerInRange = null;
            
            // Hide interaction prompt
            UIManager.Instance?.HideInteractionPrompt();
            
            // Stop defusing if in progress
            if (isBeingDefused)
            {
                StopDefusing();
            }
        }
        
        // Check if AI left
        AIController ai = other.GetComponent<AIController>();
        if (ai != null && ai == aiInRange)
        {
            aiInRange = null;
        }
    }
    
    public void PlantBomb()
    {
        if (isBombPlanted) return;
        
        isBombPlanted = true;
        
        // Show bomb model
        if (bombModel != null)
        {
            bombModel.SetActive(true);
        }
        
        // Play planting sound
        PlayPlantSound();
        
        // Create planted bomb reference
        // plantedBomb = Instantiate(bombPrefab, transform.position, transform.rotation);
        
        // Notify UI
        UIManager.Instance?.ShowBombPlantedMessage();
        
        Debug.Log($"Bomb planted at site {siteName}");
    }
    
    public void DefuseBomb()
    {
        if (!isBombPlanted) return;
        
        isBombPlanted = false;
        isBeingDefused = false;
        defuseProgress = 0f;
        
        // Hide bomb model
        if (bombModel != null)
        {
            bombModel.SetActive(false);
        }
        
        // Destroy planted bomb
        if (plantedBomb != null)
        {
            Destroy(plantedBomb);
            plantedBomb = null;
        }
        
        // Play defuse sound
        PlayDefuseSound();
        
        // Notify game manager
        GameManager.Instance?.OnBombDefused();
        
        Debug.Log($"Bomb defused at site {siteName}");
    }
    
    void StartDefusing(PlayerController player)
    {
        if (!isBombPlanted || isBeingDefused) return;
        
        isBeingDefused = true;
        defuseProgress = 0f;
        
        // Play defusing sound loop
        PlayDefusingSound();
        
        // Show defusing UI
        UIManager.Instance?.ShowDefusingProgress(0f);
        
        Debug.Log($"Started defusing bomb at site {siteName}");
    }
    
    void ContinueDefusing()
    {
        if (!isBeingDefused) return;
        
        // Increase defuse progress
        float defuseSpeed = 1f / 5f; // 5 seconds to defuse
        defuseProgress += defuseSpeed * Time.deltaTime;
        
        // Update UI
        UIManager.Instance?.ShowDefusingProgress(defuseProgress);
        
        // Check if defusing is complete
        if (defuseProgress >= 1f)
        {
            CompleteDefusing();
        }
    }
    
    void StopDefusing()
    {
        if (!isBeingDefused) return;
        
        isBeingDefused = false;
        defuseProgress = 0f;
        
        // Stop defusing sound
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // Hide defusing UI
        UIManager.Instance?.HideDefusingProgress();
        
        Debug.Log($"Stopped defusing bomb at site {siteName}");
    }
    
    void CompleteDefusing()
    {
        // Stop defusing sound
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
        
        // Hide defusing UI
        UIManager.Instance?.HideDefusingProgress();
        
        // Defuse the bomb
        DefuseBomb();
    }
    
    public void ExplodeBomb()
    {
        if (!isBombPlanted) return;
        
        // Create explosion effect
        if (explosionEffect != null)
        {
            explosionEffect.Play();
        }
        
        // Play explosion sound
        PlayExplosionSound();
        
        // Apply explosion damage to nearby players/AI
        ApplyExplosionDamage();
        
        // Reset bomb site
        isBombPlanted = false;
        isBeingDefused = false;
        defuseProgress = 0f;
        
        if (bombModel != null)
        {
            bombModel.SetActive(false);
        }
        
        if (plantedBomb != null)
        {
            Destroy(plantedBomb);
            plantedBomb = null;
        }
        
        Debug.Log($"Bomb exploded at site {siteName}");
    }
    
    void ApplyExplosionDamage()
    {
        float explosionRadius = 10f;
        float explosionDamage = 500f; // Enough to kill anyone nearby
        
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        
        foreach (Collider col in colliders)
        {
            // Damage player
            PlayerController player = col.GetComponent<PlayerController>();
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius);
                float finalDamage = explosionDamage * damageMultiplier;
                
                player.TakeDamage(finalDamage);
            }
            
            // Damage AI
            AIController ai = col.GetComponent<AIController>();
            if (ai != null)
            {
                float distance = Vector3.Distance(transform.position, col.transform.position);
                float damageMultiplier = 1f - (distance / explosionRadius);
                float finalDamage = explosionDamage * damageMultiplier;
                
                ai.TakeDamage(finalDamage);
            }
            
            // Apply physics force to rigidbodies
            Rigidbody rb = col.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 explosionDirection = (col.transform.position - transform.position).normalized;
                float explosionForce = 1000f;
                rb.AddForce(explosionDirection * explosionForce);
            }
        }
    }
    
    void PlayPlantSound()
    {
        if (audioSource != null)
        {
            // audioSource.PlayOneShot(plantSound);
            Debug.Log("Playing plant sound");
        }
    }
    
    void PlayDefusingSound()
    {
        if (audioSource != null)
        {
            // audioSource.clip = defusingSound;
            // audioSource.loop = true;
            // audioSource.Play();
            Debug.Log("Playing defusing sound");
        }
    }
    
    void PlayDefuseSound()
    {
        if (audioSource != null)
        {
            // audioSource.PlayOneShot(defuseSound);
            Debug.Log("Playing defuse sound");
        }
    }
    
    void PlayExplosionSound()
    {
        if (audioSource != null)
        {
            // audioSource.PlayOneShot(explosionSound);
            Debug.Log("Playing explosion sound");
        }
    }
    
    // Public getters
    public bool IsBombPlanted()
    {
        return isBombPlanted;
    }
    
    public bool IsBeingDefused()
    {
        return isBeingDefused;
    }
    
    public float GetDefuseProgress()
    {
        return defuseProgress;
    }
    
    public string GetSiteName()
    {
        return siteName;
    }
    
    public bool IsPlayerInRange()
    {
        return playerInRange != null;
    }
    
    public bool IsAIInRange()
    {
        return aiInRange != null;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw interaction range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
        
        // Draw explosion radius (for visualization)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, 10f);
    }
}