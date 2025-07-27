using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

public enum AIState
{
    Patrol,
    Alert,
    Combat,
    PlantBomb,
    DefuseBomb,
    Dead
}

public enum AITeam
{
    Terrorist,
    CounterTerrorist
}

public class AIController : MonoBehaviour
{
    [Header("AI Settings")]
    public AITeam team = AITeam.Terrorist;
    public float maxHealth = 100f;
    public float currentHealth = 100f;
    public float detectionRange = 15f;
    public float attackRange = 30f;
    public float hearingRange = 20f;
    public float fieldOfView = 60f;
    
    [Header("Combat Settings")]
    public float fireRate = 0.15f;
    public float accuracy = 0.7f;
    public float damage = 25f;
    public Transform firePoint;
    public LayerMask playerLayer;
    public LayerMask obstacleLayer;
    
    [Header("Movement Settings")]
    public float walkSpeed = 3f;
    public float runSpeed = 5f;
    public float rotationSpeed = 5f;
    
    [Header("Bomb Settings")]
    public bool hasBomb = false;
    public float plantTime = 5f;
    public float defuseTime = 5f;
    
    // AI State
    private AIState currentState = AIState.Patrol;
    private AIState previousState;
    
    // Components
    private NavMeshAgent navAgent;
    private AudioSource audioSource;
    private Animator animator;
    
    // Targets and positions
    private Transform player;
    private Transform currentTarget;
    private Vector3 lastKnownPlayerPosition;
    private Vector3 patrolDestination;
    private Vector3[] patrolPoints;
    private int currentPatrolIndex = 0;
    
    // Combat variables
    private float nextFireTime = 0f;
    private bool hasLineOfSight = false;
    private float lastSeenPlayerTime = 0f;
    private float combatTimer = 0f;
    
    // Bomb variables
    private BombSite targetBombSite;
    private bool isPlanting = false;
    private bool isDefusing = false;
    private float bombActionTimer = 0f;
    
    // Search variables
    private float searchTimer = 0f;
    private float maxSearchTime = 10f;
    private Vector3 searchCenter;
    
    void Start()
    {
        InitializeComponents();
        InitializePatrolPoints();
        SetState(AIState.Patrol);
        
        // Find player
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.transform;
        }
        
        // Initialize health
        currentHealth = maxHealth;
    }
    
    void InitializeComponents()
    {
        navAgent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        
        if (navAgent != null)
        {
            navAgent.speed = walkSpeed;
        }
    }
    
    void InitializePatrolPoints()
    {
        // Create basic patrol points based on team
        if (team == AITeam.Terrorist)
        {
            patrolPoints = new Vector3[]
            {
                new Vector3(0, 0, 0),      // T spawn
                new Vector3(10, 0, 5),     // Mid
                new Vector3(15, 0, 15),    // A site
                new Vector3(-10, 0, 10),   // B site
                new Vector3(-5, 0, -5)     // Back to spawn area
            };
        }
        else
        {
            patrolPoints = new Vector3[]
            {
                new Vector3(20, 0, 20),    // CT spawn
                new Vector3(15, 0, 15),    // A site
                new Vector3(10, 0, 5),     // Mid
                new Vector3(-10, 0, 10),   // B site
                new Vector3(5, 0, 15)      // Rotate back
            };
        }
        
        if (patrolPoints.Length > 0)
        {
            patrolDestination = patrolPoints[0];
        }
    }
    
    void Update()
    {
        if (currentHealth <= 0)
        {
            SetState(AIState.Dead);
            return;
        }
        
        UpdateState();
        CheckForPlayer();
        CheckForSounds();
    }
    
    void UpdateState()
    {
        switch (currentState)
        {
            case AIState.Patrol:
                UpdatePatrol();
                break;
            case AIState.Alert:
                UpdateAlert();
                break;
            case AIState.Combat:
                UpdateCombat();
                break;
            case AIState.PlantBomb:
                UpdatePlantBomb();
                break;
            case AIState.DefuseBomb:
                UpdateDefuseBomb();
                break;
            case AIState.Dead:
                UpdateDead();
                break;
        }
    }
    
    void UpdatePatrol()
    {
        // Move to patrol destination
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.speed = walkSpeed;
            navAgent.SetDestination(patrolDestination);
            
            // Check if reached destination
            if (Vector3.Distance(transform.position, patrolDestination) < 2f)
            {
                // Move to next patrol point
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                patrolDestination = patrolPoints[currentPatrolIndex];
            }
        }
        
        // Check for bomb objectives
        if (team == AITeam.Terrorist && hasBomb)
        {
            BombSite nearestSite = FindNearestBombSite();
            if (nearestSite != null && Vector3.Distance(transform.position, nearestSite.transform.position) < 5f)
            {
                targetBombSite = nearestSite;
                SetState(AIState.PlantBomb);
            }
        }
    }
    
    void UpdateAlert()
    {
        searchTimer += Time.deltaTime;
        
        if (navAgent != null && navAgent.enabled)
        {
            navAgent.speed = runSpeed;
            
            // Move towards last known position
            if (Vector3.Distance(transform.position, lastKnownPlayerPosition) > 1f)
            {
                navAgent.SetDestination(lastKnownPlayerPosition);
            }
            else
            {
                // Search around the area
                Vector3 randomSearch = lastKnownPlayerPosition + Random.insideUnitSphere * 5f;
                randomSearch.y = transform.position.y;
                navAgent.SetDestination(randomSearch);
            }
        }
        
        // Return to patrol if search time exceeded
        if (searchTimer > maxSearchTime)
        {
            SetState(AIState.Patrol);
        }
    }
    
    void UpdateCombat()
    {
        if (currentTarget == null)
        {
            SetState(AIState.Alert);
            return;
        }
        
        combatTimer += Time.deltaTime;
        
        // Face the target
        Vector3 directionToTarget = (currentTarget.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(directionToTarget.x, 0, directionToTarget.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
        
        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
        
        // Move to optimal combat range
        if (navAgent != null && navAgent.enabled)
        {
            if (distanceToTarget > attackRange * 0.8f)
            {
                // Move closer
                navAgent.speed = runSpeed;
                navAgent.SetDestination(currentTarget.position);
            }
            else if (distanceToTarget < attackRange * 0.3f)
            {
                // Move away
                Vector3 retreatPosition = transform.position - directionToTarget * 5f;
                navAgent.SetDestination(retreatPosition);
            }
            else
            {
                // Stay in position and strafe
                Vector3 strafeDirection = Vector3.Cross(directionToTarget, Vector3.up).normalized;
                Vector3 strafePosition = transform.position + strafeDirection * Random.Range(-3f, 3f);
                navAgent.SetDestination(strafePosition);
            }
        }
        
        // Shoot at target
        if (hasLineOfSight && Time.time >= nextFireTime)
        {
            FireAtTarget();
            nextFireTime = Time.time + fireRate;
        }
        
        // Lose target if not seen for too long
        if (Time.time - lastSeenPlayerTime > 5f)
        {
            SetState(AIState.Alert);
        }
    }
    
    void UpdatePlantBomb()
    {
        if (!hasBomb || targetBombSite == null)
        {
            SetState(AIState.Patrol);
            return;
        }
        
        // Move to bomb site
        float distanceToBombSite = Vector3.Distance(transform.position, targetBombSite.transform.position);
        
        if (distanceToBombSite > 2f)
        {
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.SetDestination(targetBombSite.transform.position);
            }
        }
        else
        {
            // Start planting
            if (!isPlanting)
            {
                StartPlanting();
            }
            else
            {
                bombActionTimer += Time.deltaTime;
                if (bombActionTimer >= plantTime)
                {
                    CompletePlanting();
                }
            }
        }
    }
    
    void UpdateDefuseBomb()
    {
        if (targetBombSite == null || !targetBombSite.IsBombPlanted())
        {
            SetState(AIState.Patrol);
            return;
        }
        
        // Move to bomb site
        float distanceToBombSite = Vector3.Distance(transform.position, targetBombSite.transform.position);
        
        if (distanceToBombSite > 2f)
        {
            if (navAgent != null && navAgent.enabled)
            {
                navAgent.SetDestination(targetBombSite.transform.position);
            }
        }
        else
        {
            // Start defusing
            if (!isDefusing)
            {
                StartDefusing();
            }
            else
            {
                bombActionTimer += Time.deltaTime;
                if (bombActionTimer >= defuseTime)
                {
                    CompleteDefusing();
                }
            }
        }
    }
    
    void UpdateDead()
    {
        // Disable AI components
        if (navAgent != null)
            navAgent.enabled = false;
            
        // Play death animation
        if (animator != null)
            animator.SetBool("Dead", true);
            
        // Disable this script
        enabled = false;
    }
    
    void CheckForPlayer()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Check if player is within detection range
        if (distanceToPlayer <= detectionRange)
        {
            Vector3 directionToPlayer = (player.position - transform.position).normalized;
            float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer);
            
            // Check if player is within field of view
            if (angleToPlayer <= fieldOfView / 2f)
            {
                // Check line of sight
                RaycastHit hit;
                if (Physics.Raycast(transform.position + Vector3.up, directionToPlayer, out hit, detectionRange, playerLayer | obstacleLayer))
                {
                    if (hit.collider.CompareTag("Player"))
                    {
                        // Player spotted!
                        OnPlayerSpotted(player);
                        hasLineOfSight = true;
                        lastSeenPlayerTime = Time.time;
                        return;
                    }
                }
            }
        }
        
        hasLineOfSight = false;
    }
    
    void CheckForSounds()
    {
        if (player == null) return;
        
        // Check if player is moving (making noise)
        PlayerController playerController = player.GetComponent<PlayerController>();
        if (playerController != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // If player is close and moving, AI can hear them
            if (distanceToPlayer <= hearingRange && IsPlayerMakingNoise(playerController))
            {
                if (currentState == AIState.Patrol)
                {
                    OnSoundHeard(player.position);
                }
            }
        }
    }
    
    bool IsPlayerMakingNoise(PlayerController playerController)
    {
        // Player makes noise when moving fast or running
        // This would need to be implemented based on PlayerController's movement state
        return true; // Simplified for now
    }
    
    void OnPlayerSpotted(Transform playerTransform)
    {
        currentTarget = playerTransform;
        lastKnownPlayerPosition = playerTransform.position;
        
        if (currentState != AIState.Combat)
        {
            SetState(AIState.Combat);
        }
        
        // Alert nearby AI
        AlertNearbyAI(playerTransform.position);
    }
    
    void OnSoundHeard(Vector3 soundPosition)
    {
        lastKnownPlayerPosition = soundPosition;
        searchCenter = soundPosition;
        
        if (currentState == AIState.Patrol)
        {
            SetState(AIState.Alert);
        }
    }
    
    void AlertNearbyAI(Vector3 alertPosition)
    {
        Collider[] nearbyAI = Physics.OverlapSphere(transform.position, 20f);
        
        foreach (Collider col in nearbyAI)
        {
            AIController ai = col.GetComponent<AIController>();
            if (ai != null && ai != this && ai.team == team)
            {
                ai.OnAlerted(alertPosition);
            }
        }
    }
    
    public void OnAlerted(Vector3 alertPosition)
    {
        if (currentState == AIState.Patrol)
        {
            lastKnownPlayerPosition = alertPosition;
            SetState(AIState.Alert);
        }
    }
    
    void FireAtTarget()
    {
        if (currentTarget == null || firePoint == null) return;
        
        Vector3 directionToTarget = (currentTarget.position - firePoint.position).normalized;
        
        // Add some inaccuracy based on accuracy setting
        float inaccuracy = (1f - accuracy) * 2f;
        directionToTarget += Random.insideUnitSphere * inaccuracy;
        directionToTarget.Normalize();
        
        // Perform raycast
        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, directionToTarget, out hit, attackRange, playerLayer | obstacleLayer))
        {
            if (hit.collider.CompareTag("Player"))
            {
                // Hit player
                PlayerController playerController = hit.collider.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    playerController.TakeDamage(damage);
                }
            }
        }
        
        // Play fire sound and effects
        PlayFireSound();
        ShowMuzzleFlash();
    }
    
    void StartPlanting()
    {
        isPlanting = true;
        bombActionTimer = 0f;
        
        // Stop movement
        if (navAgent != null)
            navAgent.isStopped = true;
            
        // Play planting sound
        PlayPlantingSound();
    }
    
    void CompletePlanting()
    {
        if (targetBombSite != null)
        {
            targetBombSite.PlantBomb();
            hasBomb = false;
            isPlanting = false;
            
            // Resume movement
            if (navAgent != null)
                navAgent.isStopped = false;
                
            SetState(AIState.Patrol);
            
            // Notify game manager
            GameManager.Instance?.OnBombPlanted();
        }
    }
    
    void StartDefusing()
    {
        isDefusing = true;
        bombActionTimer = 0f;
        
        // Stop movement
        if (navAgent != null)
            navAgent.isStopped = true;
            
        // Play defusing sound
        PlayDefusingSound();
    }
    
    void CompleteDefusing()
    {
        if (targetBombSite != null)
        {
            targetBombSite.DefuseBomb();
            isDefusing = false;
            
            // Resume movement
            if (navAgent != null)
                navAgent.isStopped = false;
                
            SetState(AIState.Patrol);
            
            // Notify game manager
            GameManager.Instance?.OnBombDefused();
        }
    }
    
    BombSite FindNearestBombSite()
    {
        BombSite[] bombSites = FindObjectsOfType<BombSite>();
        BombSite nearest = null;
        float nearestDistance = Mathf.Infinity;
        
        foreach (BombSite site in bombSites)
        {
            float distance = Vector3.Distance(transform.position, site.transform.position);
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = site;
            }
        }
        
        return nearest;
    }
    
    void SetState(AIState newState)
    {
        previousState = currentState;
        currentState = newState;
        
        // Reset timers when changing state
        searchTimer = 0f;
        combatTimer = 0f;
        
        // Handle state-specific setup
        switch (newState)
        {
            case AIState.Alert:
                if (navAgent != null)
                    navAgent.speed = runSpeed;
                break;
            case AIState.Combat:
                if (navAgent != null)
                    navAgent.speed = runSpeed;
                break;
            case AIState.Patrol:
                if (navAgent != null)
                {
                    navAgent.speed = walkSpeed;
                    navAgent.isStopped = false;
                }
                break;
        }
    }
    
    public void TakeDamage(float damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            // React to damage
            if (currentState == AIState.Patrol)
            {
                SetState(AIState.Alert);
            }
        }
    }
    
    void Die()
    {
        SetState(AIState.Dead);
        
        // Drop bomb if carrying one
        if (hasBomb)
        {
            // Create bomb pickup at this location
            // GameObject bombPickup = Instantiate(bombPickupPrefab, transform.position, Quaternion.identity);
            hasBomb = false;
        }
        
        // Notify game manager
        GameManager.Instance?.OnAIKilled(team);
    }
    
    void PlayFireSound()
    {
        if (audioSource != null)
        {
            // audioSource.PlayOneShot(fireSound);
        }
    }
    
    void PlayPlantingSound()
    {
        if (audioSource != null)
        {
            // audioSource.PlayOneShot(plantingSound);
        }
    }
    
    void PlayDefusingSound()
    {
        if (audioSource != null)
        {
            // audioSource.PlayOneShot(defusingSound);
        }
    }
    
    void ShowMuzzleFlash()
    {
        // Show muzzle flash effect
        if (firePoint != null)
        {
            // Create muzzle flash effect
        }
    }
    
    // Public getters
    public AIState GetCurrentState()
    {
        return currentState;
    }
    
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
    
    public float GetHealthPercentage()
    {
        return currentHealth / maxHealth;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw field of view
        Gizmos.color = Color.blue;
        Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfView / 2f, 0) * transform.forward * detectionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldOfView / 2f, 0) * transform.forward * detectionRange;
        
        Gizmos.DrawRay(transform.position, leftBoundary);
        Gizmos.DrawRay(transform.position, rightBoundary);
    }
}