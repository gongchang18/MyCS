using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public enum GameState
{
    WaitingForPlayers,
    BuyPhase,
    RoundActive,
    RoundEnd,
    MatchEnd
}

public enum WinCondition
{
    Elimination,
    BombExploded,
    BombDefused,
    TimeExpired
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("Round Settings")]
    public float buyPhaseTime = 15f;
    public float roundTime = 90f;
    public float roundEndTime = 3f;
    public int maxRounds = 15;
    public int roundsToWin = 8;
    
    [Header("Bomb Settings")]
    public float bombTimer = 45f;
    public GameObject bombPrefab;
    
    [Header("Spawn Points")]
    public Transform[] terroristSpawns;
    public Transform[] counterTerroristSpawns;
    
    // Game state
    private GameState currentState = GameState.WaitingForPlayers;
    private int currentRound = 0;
    private int terroristScore = 0;
    private int counterTerroristScore = 0;
    
    // Round variables
    private float currentTimer = 0f;
    private bool isBombPlanted = false;
    private BombSite plantedBombSite = null;
    private Coroutine bombTimerCoroutine = null;
    
    // Player and AI references
    private PlayerController player;
    private List<AIController> terroristAI = new List<AIController>();
    private List<AIController> counterTerroristAI = new List<AIController>();
    
    // Team assignment
    private bool playerIsCounterTerrorist = true;
    
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
        InitializeGame();
    }
    
    void InitializeGame()
    {
        // Find player
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO != null)
        {
            player = playerGO.GetComponent<PlayerController>();
        }
        
        // Find AI controllers
        AIController[] allAI = FindObjectsOfType<AIController>();
        foreach (AIController ai in allAI)
        {
            if (ai.team == AITeam.Terrorist)
            {
                terroristAI.Add(ai);
            }
            else
            {
                counterTerroristAI.Add(ai);
            }
        }
        
        // Assign bomb to one terrorist
        if (terroristAI.Count > 0)
        {
            int randomIndex = Random.Range(0, terroristAI.Count);
            terroristAI[randomIndex].hasBomb = true;
        }
        
        // Start first round
        StartNewRound();
    }
    
    void Update()
    {
        UpdateGameState();
        CheckWinConditions();
    }
    
    void UpdateGameState()
    {
        switch (currentState)
        {
            case GameState.BuyPhase:
                UpdateBuyPhase();
                break;
            case GameState.RoundActive:
                UpdateRoundActive();
                break;
            case GameState.RoundEnd:
                UpdateRoundEnd();
                break;
        }
    }
    
    void UpdateBuyPhase()
    {
        currentTimer -= Time.deltaTime;
        
        // Update UI timer
        UIManager.Instance?.UpdateTimer(Mathf.CeilToInt(currentTimer));
        
        if (currentTimer <= 0)
        {
            EndBuyPhase();
        }
    }
    
    void UpdateRoundActive()
    {
        currentTimer -= Time.deltaTime;
        
        // Update UI timer
        UIManager.Instance?.UpdateTimer(Mathf.CeilToInt(currentTimer));
        
        if (currentTimer <= 0 && !isBombPlanted)
        {
            EndRound(WinCondition.TimeExpired);
        }
    }
    
    void UpdateRoundEnd()
    {
        currentTimer -= Time.deltaTime;
        
        if (currentTimer <= 0)
        {
            if (IsMatchFinished())
            {
                EndMatch();
            }
            else
            {
                StartNewRound();
            }
        }
    }
    
    void StartNewRound()
    {
        currentRound++;
        currentState = GameState.BuyPhase;
        currentTimer = buyPhaseTime;
        isBombPlanted = false;
        plantedBombSite = null;
        
        // Reset players and AI
        RespawnAllPlayers();
        ResetAllAI();
        
        // Assign bomb to terrorist
        AssignBombToTerrorist();
        
        // Update UI
        UIManager.Instance?.UpdateRoundInfo(currentRound, terroristScore, counterTerroristScore);
        UIManager.Instance?.ShowBuyMenu(true);
        
        Debug.Log($"Round {currentRound} started - Buy Phase");
    }
    
    void EndBuyPhase()
    {
        currentState = GameState.RoundActive;
        currentTimer = roundTime;
        
        // Hide buy menu
        UIManager.Instance?.ShowBuyMenu(false);
        
        // Enable player movement (if it was restricted during buy phase)
        if (player != null)
        {
            player.enabled = true;
        }
        
        Debug.Log("Buy phase ended - Round active");
    }
    
    void EndRound(WinCondition winCondition)
    {
        currentState = GameState.RoundEnd;
        currentTimer = roundEndTime;
        
        // Stop bomb timer if running
        if (bombTimerCoroutine != null)
        {
            StopCoroutine(bombTimerCoroutine);
            bombTimerCoroutine = null;
        }
        
        // Determine winner
        bool terroristsWin = false;
        string winMessage = "";
        
        switch (winCondition)
        {
            case WinCondition.Elimination:
                terroristsWin = IsPlayerAlive() ? false : true; // If player (CT) is alive, CTs win
                winMessage = terroristsWin ? "Terrorists Win - All Counter-Terrorists Eliminated" : "Counter-Terrorists Win - All Terrorists Eliminated";
                break;
            case WinCondition.BombExploded:
                terroristsWin = true;
                winMessage = "Terrorists Win - Bomb Exploded";
                break;
            case WinCondition.BombDefused:
                terroristsWin = false;
                winMessage = "Counter-Terrorists Win - Bomb Defused";
                break;
            case WinCondition.TimeExpired:
                terroristsWin = false;
                winMessage = "Counter-Terrorists Win - Time Expired";
                break;
        }
        
        // Update scores
        if (terroristsWin)
        {
            terroristScore++;
        }
        else
        {
            counterTerroristScore++;
        }
        
        // Update economy
        bool playerWon = (playerIsCounterTerrorist && !terroristsWin) || (!playerIsCounterTerrorist && terroristsWin);
        EconomySystem.Instance?.OnRoundEnd(playerWon);
        
        // Update UI
        UIManager.Instance?.ShowRoundEndMessage(winMessage);
        UIManager.Instance?.UpdateRoundInfo(currentRound, terroristScore, counterTerroristScore);
        
        Debug.Log($"Round {currentRound} ended: {winMessage}");
    }
    
    void CheckWinConditions()
    {
        if (currentState != GameState.RoundActive) return;
        
        // Check elimination
        bool allTerroristsAlive = IsAnyTerroristAlive();
        bool playerAlive = IsPlayerAlive();
        
        if (!allTerroristsAlive && !isBombPlanted)
        {
            EndRound(WinCondition.Elimination);
        }
        else if (!playerAlive && !allTerroristsAlive)
        {
            // Both sides eliminated - rare case
            EndRound(WinCondition.Elimination);
        }
        else if (!playerAlive && counterTerroristAI.Count == 0)
        {
            // All CTs dead
            if (!isBombPlanted)
            {
                EndRound(WinCondition.Elimination);
            }
        }
    }
    
    bool IsAnyTerroristAlive()
    {
        foreach (AIController ai in terroristAI)
        {
            if (ai != null && ai.IsAlive())
            {
                return true;
            }
        }
        return false;
    }
    
    bool IsPlayerAlive()
    {
        return player != null && player.IsAlive();
    }
    
    bool IsMatchFinished()
    {
        return terroristScore >= roundsToWin || counterTerroristScore >= roundsToWin || currentRound >= maxRounds;
    }
    
    void EndMatch()
    {
        currentState = GameState.MatchEnd;
        
        string matchResult = "";
        if (terroristScore > counterTerroristScore)
        {
            matchResult = "Terrorists Win the Match!";
        }
        else if (counterTerroristScore > terroristScore)
        {
            matchResult = "Counter-Terrorists Win the Match!";
        }
        else
        {
            matchResult = "Match Tied!";
        }
        
        UIManager.Instance?.ShowMatchEndMessage(matchResult);
        
        Debug.Log($"Match ended: {matchResult} (T:{terroristScore} - CT:{counterTerroristScore})");
    }
    
    void RespawnAllPlayers()
    {
        // Respawn player
        if (player != null)
        {
            Transform spawnPoint = playerIsCounterTerrorist ? 
                counterTerroristSpawns[Random.Range(0, counterTerroristSpawns.Length)] :
                terroristSpawns[Random.Range(0, terroristSpawns.Length)];
                
            player.transform.position = spawnPoint.position;
            player.transform.rotation = spawnPoint.rotation;
            player.currentHealth = player.maxHealth;
            player.enabled = true;
        }
        
        // Respawn AI
        RespawnAI(terroristAI, terroristSpawns);
        RespawnAI(counterTerroristAI, counterTerroristSpawns);
    }
    
    void RespawnAI(List<AIController> aiList, Transform[] spawnPoints)
    {
        for (int i = 0; i < aiList.Count; i++)
        {
            if (aiList[i] != null)
            {
                Transform spawnPoint = spawnPoints[i % spawnPoints.Length];
                aiList[i].transform.position = spawnPoint.position;
                aiList[i].transform.rotation = spawnPoint.rotation;
                aiList[i].currentHealth = aiList[i].maxHealth;
                aiList[i].enabled = true;
                
                // Reset NavMeshAgent
                if (aiList[i].GetComponent<UnityEngine.AI.NavMeshAgent>() != null)
                {
                    aiList[i].GetComponent<UnityEngine.AI.NavMeshAgent>().enabled = true;
                }
            }
        }
    }
    
    void ResetAllAI()
    {
        // Reset all AI to patrol state
        foreach (AIController ai in terroristAI)
        {
            if (ai != null)
            {
                // Reset AI state - this would need to be implemented in AIController
                // ai.ResetToPatrol();
            }
        }
        
        foreach (AIController ai in counterTerroristAI)
        {
            if (ai != null)
            {
                // Reset AI state
                // ai.ResetToPatrol();
            }
        }
    }
    
    void AssignBombToTerrorist()
    {
        // Remove bomb from all terrorists first
        foreach (AIController ai in terroristAI)
        {
            if (ai != null)
            {
                ai.hasBomb = false;
            }
        }
        
        // Assign to random alive terrorist
        List<AIController> aliveTerrorists = new List<AIController>();
        foreach (AIController ai in terroristAI)
        {
            if (ai != null && ai.IsAlive())
            {
                aliveTerrorists.Add(ai);
            }
        }
        
        if (aliveTerrorists.Count > 0)
        {
            int randomIndex = Random.Range(0, aliveTerrorists.Count);
            aliveTerrorists[randomIndex].hasBomb = true;
        }
    }
    
    public void OnBombPlanted()
    {
        isBombPlanted = true;
        
        // Start bomb timer
        bombTimerCoroutine = StartCoroutine(BombTimerCoroutine());
        
        // Award money to terrorists
        EconomySystem.Instance?.OnBombPlant();
        
        // Update UI
        UIManager.Instance?.ShowBombPlantedMessage();
        
        Debug.Log("Bomb planted!");
    }
    
    public void OnBombDefused()
    {
        isBombPlanted = false;
        plantedBombSite = null;
        
        // Stop bomb timer
        if (bombTimerCoroutine != null)
        {
            StopCoroutine(bombTimerCoroutine);
            bombTimerCoroutine = null;
        }
        
        // Award money to counter-terrorists
        EconomySystem.Instance?.OnBombDefuse();
        
        // End round
        EndRound(WinCondition.BombDefused);
        
        Debug.Log("Bomb defused!");
    }
    
    public void OnPlayerDeath()
    {
        Debug.Log("Player died!");
        
        // Check if round should end
        if (currentState == GameState.RoundActive)
        {
            // Let the normal win condition check handle this
        }
    }
    
    public void OnAIKilled(AITeam team)
    {
        // Award money for kill
        EconomySystem.Instance?.OnKill();
        
        Debug.Log($"{team} AI killed!");
    }
    
    IEnumerator BombTimerCoroutine()
    {
        float timer = bombTimer;
        
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            
            // Update bomb timer UI
            UIManager.Instance?.UpdateBombTimer(Mathf.CeilToInt(timer));
            
            yield return null;
        }
        
        // Bomb exploded
        OnBombExploded();
    }
    
    void OnBombExploded()
    {
        // Create explosion effect
        if (plantedBombSite != null)
        {
            // Create explosion at bomb site
            // GameObject explosion = Instantiate(explosionPrefab, plantedBombSite.transform.position, Quaternion.identity);
        }
        
        // End round
        EndRound(WinCondition.BombExploded);
        
        Debug.Log("Bomb exploded!");
    }
    
    public void RestartMatch()
    {
        // Reset scores
        terroristScore = 0;
        counterTerroristScore = 0;
        currentRound = 0;
        
        // Reset economy
        EconomySystem.Instance?.ResetForNewMatch();
        
        // Start new round
        StartNewRound();
    }
    
    // Getters
    public GameState GetCurrentState()
    {
        return currentState;
    }
    
    public int GetCurrentRound()
    {
        return currentRound;
    }
    
    public int GetTerroristScore()
    {
        return terroristScore;
    }
    
    public int GetCounterTerroristScore()
    {
        return counterTerroristScore;
    }
    
    public bool IsBombPlanted()
    {
        return isBombPlanted;
    }
    
    public float GetCurrentTimer()
    {
        return currentTimer;
    }
    
    public bool IsPlayerCounterTerrorist()
    {
        return playerIsCounterTerrorist;
    }
}