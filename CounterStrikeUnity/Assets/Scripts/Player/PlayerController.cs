using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float crouchSpeed = 2.5f;
    public float jumpForce = 5f;
    public float mouseSensitivity = 2f;
    
    [Header("Camera Settings")]
    public Camera playerCamera;
    public float cameraHeight = 1.6f;
    public float crouchHeight = 0.8f;
    public float bobAmount = 0.1f;
    public float bobSpeed = 10f;
    
    [Header("Health & Armor")]
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float maxArmor = 100f;
    public float currentArmor = 0f;
    
    // Private variables
    private CharacterController characterController;
    private Vector3 moveDirection;
    private Vector3 velocity;
    private float xRotation = 0f;
    private bool isGrounded;
    private bool isCrouching = false;
    private bool isWalking = false;
    
    // Camera bob variables
    private float bobTimer = 0f;
    private Vector3 originalCameraPosition;
    
    // Input variables
    private float horizontal;
    private float vertical;
    private bool jumpInput;
    private bool crouchInput;
    private bool walkInput;
    
    // References
    private WeaponSystem weaponSystem;
    private AudioSource audioSource;
    
    void Start()
    {
        // Initialize components
        characterController = GetComponent<CharacterController>();
        weaponSystem = GetComponent<WeaponSystem>();
        audioSource = GetComponent<AudioSource>();
        
        // Setup camera
        if (playerCamera == null)
            playerCamera = Camera.main;
            
        originalCameraPosition = playerCamera.transform.localPosition;
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        
        // Initialize health and armor
        currentHealth = maxHealth;
    }
    
    void Update()
    {
        HandleInput();
        HandleMouseLook();
        HandleMovement();
        HandleCameraBob();
        CheckGrounded();
    }
    
    void HandleInput()
    {
        // Movement input
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        
        // Action inputs
        jumpInput = Input.GetButtonDown("Jump");
        crouchInput = Input.GetKey(KeyCode.LeftControl);
        walkInput = Input.GetKey(KeyCode.LeftShift);
        
        // Weapon inputs
        if (weaponSystem != null)
        {
            if (Input.GetButton("Fire1"))
                weaponSystem.StartFiring();
            else
                weaponSystem.StopFiring();
                
            if (Input.GetButtonDown("Fire2"))
                weaponSystem.ToggleScope();
                
            if (Input.GetKeyDown(KeyCode.R))
                weaponSystem.Reload();
                
            // Weapon switching
            if (Input.GetKeyDown(KeyCode.Alpha1))
                weaponSystem.SwitchWeapon(0);
            else if (Input.GetKeyDown(KeyCode.Alpha2))
                weaponSystem.SwitchWeapon(1);
                
            // Scroll wheel weapon switching
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0f)
                weaponSystem.SwitchToNextWeapon();
            else if (scroll < 0f)
                weaponSystem.SwitchToPreviousWeapon();
        }
        
        // Toggle cursor lock
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
    
    void HandleMouseLook()
    {
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            // Rotate the player body horizontally
            transform.Rotate(Vector3.up * mouseX);
            
            // Rotate the camera vertically
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -80f, 80f);
            playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
    }
    
    void HandleMovement()
    {
        // Handle crouching
        if (crouchInput && !isCrouching)
        {
            StartCrouch();
        }
        else if (!crouchInput && isCrouching)
        {
            StopCrouch();
        }
        
        // Determine movement speed
        float currentSpeed = walkSpeed;
        isWalking = walkInput && !isCrouching;
        
        if (isCrouching)
            currentSpeed = crouchSpeed;
        else if (isWalking)
            currentSpeed = walkSpeed * 0.5f; // Walking is slower
        else
            currentSpeed = runSpeed;
        
        // Calculate movement direction
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        
        moveDirection = (forward * vertical + right * horizontal).normalized * currentSpeed;
        
        // Handle jumping
        if (jumpInput && isGrounded && !isCrouching)
        {
            velocity.y = jumpForce;
        }
        
        // Apply gravity
        if (!isGrounded)
        {
            velocity.y += Physics.gravity.y * Time.deltaTime;
        }
        else if (velocity.y < 0)
        {
            velocity.y = -2f; // Small downward force to keep grounded
        }
        
        // Combine movement and velocity
        Vector3 finalMovement = moveDirection + Vector3.up * velocity.y;
        
        // Move the character
        characterController.Move(finalMovement * Time.deltaTime);
        
        // Play footstep sounds
        if (isGrounded && moveDirection.magnitude > 0.1f)
        {
            PlayFootstepSound();
        }
    }
    
    void HandleCameraBob()
    {
        if (isGrounded && moveDirection.magnitude > 0.1f && !isCrouching)
        {
            bobTimer += Time.deltaTime * bobSpeed;
            float bobOffset = Mathf.Sin(bobTimer) * bobAmount;
            
            Vector3 newPosition = originalCameraPosition;
            newPosition.y += bobOffset;
            
            if (isCrouching)
                newPosition.y = crouchHeight;
            else
                newPosition.y = cameraHeight + bobOffset;
                
            playerCamera.transform.localPosition = newPosition;
        }
        else
        {
            bobTimer = 0f;
            Vector3 targetPosition = originalCameraPosition;
            targetPosition.y = isCrouching ? crouchHeight : cameraHeight;
            
            playerCamera.transform.localPosition = Vector3.Lerp(
                playerCamera.transform.localPosition,
                targetPosition,
                Time.deltaTime * 5f
            );
        }
    }
    
    void CheckGrounded()
    {
        isGrounded = characterController.isGrounded;
    }
    
    void StartCrouch()
    {
        isCrouching = true;
        characterController.height = crouchHeight;
        characterController.center = new Vector3(0, crouchHeight / 2, 0);
    }
    
    void StopCrouch()
    {
        // Check if there's enough space to stand up
        if (!Physics.CheckSphere(transform.position + Vector3.up * cameraHeight, 0.4f))
        {
            isCrouching = false;
            characterController.height = cameraHeight;
            characterController.center = new Vector3(0, cameraHeight / 2, 0);
        }
    }
    
    void PlayFootstepSound()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            // Play different sounds based on movement type
            float pitch = isWalking ? 0.8f : 1.0f;
            audioSource.pitch = pitch;
            // audioSource.PlayOneShot(footstepClip); // Would need audio clip
        }
    }
    
    public void TakeDamage(float damage)
    {
        // Apply armor reduction
        if (currentArmor > 0)
        {
            float armorDamage = damage * 0.5f;
            float healthDamage = damage * 0.5f;
            
            currentArmor -= armorDamage;
            if (currentArmor < 0)
            {
                healthDamage += Mathf.Abs(currentArmor);
                currentArmor = 0;
            }
            
            currentHealth -= healthDamage;
        }
        else
        {
            currentHealth -= damage;
        }
        
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Apply screen effect for damage
        ApplyDamageEffect();
        
        // Check if player is dead
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void ApplyDamageEffect()
    {
        // Add screen flash or camera shake effect
        StartCoroutine(DamageScreenEffect());
    }
    
    System.Collections.IEnumerator DamageScreenEffect()
    {
        // Simple camera shake
        Vector3 originalPosition = playerCamera.transform.localPosition;
        
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomOffset = Random.insideUnitSphere * 0.1f;
            playerCamera.transform.localPosition = originalPosition + randomOffset;
            yield return new WaitForSeconds(0.02f);
        }
        
        playerCamera.transform.localPosition = originalPosition;
    }
    
    void Die()
    {
        // Handle player death
        GameManager.Instance?.OnPlayerDeath();
        
        // Disable movement
        enabled = false;
    }
    
    public void Heal(float amount)
    {
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
    }
    
    public void AddArmor(float amount)
    {
        currentArmor = Mathf.Clamp(currentArmor + amount, 0, maxArmor);
    }
    
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    public float GetArmorPercentage()
    {
        return currentArmor / maxArmor;
    }
}