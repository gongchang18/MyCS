using UnityEngine;
using UnityEngine.AI;

public class SceneSetup : MonoBehaviour
{
    [Header("Map Generation")]
    public bool autoGenerateMap = true;
    public Material wallMaterial;
    public Material floorMaterial;
    public Material bombSiteMaterial;
    
    [Header("Spawn Points")]
    public int terroristSpawnCount = 5;
    public int counterTerroristSpawnCount = 5;
    
    [Header("AI Setup")]
    public GameObject aiPrefab;
    public int terroristAICount = 4;
    public int counterTerroristAICount = 4;
    
    void Start()
    {
        if (autoGenerateMap)
        {
            GenerateBasicMap();
        }
        
        SetupSpawnPoints();
        SetupBombSites();
        SetupNavMesh();
        SpawnAI();
    }
    
    void GenerateBasicMap()
    {
        // Create basic dust2-inspired layout
        CreateWalls();
        CreateFloor();
        CreateCover();
    }
    
    void CreateWalls()
    {
        // Create perimeter walls
        CreateWall(new Vector3(0, 2.5f, 25), new Vector3(50, 5, 1)); // North wall
        CreateWall(new Vector3(0, 2.5f, -25), new Vector3(50, 5, 1)); // South wall
        CreateWall(new Vector3(25, 2.5f, 0), new Vector3(1, 5, 50)); // East wall
        CreateWall(new Vector3(-25, 2.5f, 0), new Vector3(1, 5, 50)); // West wall
        
        // Create internal walls for map layout
        CreateWall(new Vector3(10, 2.5f, 10), new Vector3(1, 5, 10)); // Mid separator
        CreateWall(new Vector3(-10, 2.5f, -10), new Vector3(10, 5, 1)); // B site wall
        CreateWall(new Vector3(15, 2.5f, 15), new Vector3(8, 5, 1)); // A site wall
    }
    
    void CreateWall(Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "Wall";
        wall.transform.position = position;
        wall.transform.localScale = scale;
        wall.layer = LayerMask.NameToLayer("Default");
        
        if (wallMaterial != null)
        {
            wall.GetComponent<Renderer>().material = wallMaterial;
        }
        else
        {
            // Create basic concrete material
            Material concrete = new Material(Shader.Find("Standard"));
            concrete.color = new Color(0.7f, 0.7f, 0.7f);
            concrete.SetFloat("_Roughness", 0.8f);
            wall.GetComponent<Renderer>().material = concrete;
        }
        
        // Add to static geometry for NavMesh
        wall.isStatic = true;
    }
    
    void CreateFloor()
    {
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(10, 1, 10); // 100x100 units
        floor.layer = LayerMask.NameToLayer("Default");
        
        if (floorMaterial != null)
        {
            floor.GetComponent<Renderer>().material = floorMaterial;
        }
        else
        {
            // Create basic floor material
            Material floorMat = new Material(Shader.Find("Standard"));
            floorMat.color = new Color(0.8f, 0.7f, 0.6f);
            floor.GetComponent<Renderer>().material = floorMat;
        }
        
        floor.isStatic = true;
    }
    
    void CreateCover()
    {
        // Create wooden boxes for cover
        CreateBox(new Vector3(15, 1, 15), "A Site Box 1");
        CreateBox(new Vector3(17, 1, 13), "A Site Box 2");
        CreateBox(new Vector3(13, 1, 17), "A Site Box 3");
        
        CreateBox(new Vector3(-10, 1, -8), "B Site Box 1");
        CreateBox(new Vector3(-12, 1, -10), "B Site Box 2");
        CreateBox(new Vector3(-8, 1, -12), "B Site Box 3");
        
        // Mid boxes
        CreateBox(new Vector3(5, 1, 0), "Mid Box 1");
        CreateBox(new Vector3(-5, 1, 5), "Mid Box 2");
    }
    
    void CreateBox(Vector3 position, string boxName)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = boxName;
        box.transform.position = position;
        box.transform.localScale = new Vector3(2, 2, 2);
        
        // Create wood material
        Material wood = new Material(Shader.Find("Standard"));
        wood.color = new Color(0.6f, 0.4f, 0.2f);
        box.GetComponent<Renderer>().material = wood;
        
        // Add Rigidbody for physics interaction
        Rigidbody rb = box.AddComponent<Rigidbody>();
        rb.mass = 50f; // Heavy boxes
        
        box.isStatic = false; // Can be moved by explosions
    }
    
    void SetupSpawnPoints()
    {
        // Create terrorist spawn points
        GameObject tSpawnParent = new GameObject("Terrorist Spawns");
        for (int i = 0; i < terroristSpawnCount; i++)
        {
            GameObject spawn = new GameObject($"T_Spawn_{i + 1}");
            spawn.transform.parent = tSpawnParent.transform;
            spawn.transform.position = new Vector3(-20 + i * 2, 0.5f, -20);
            spawn.transform.rotation = Quaternion.Euler(0, 45, 0);
            spawn.tag = "TerroristSpawn";
        }
        
        // Create counter-terrorist spawn points
        GameObject ctSpawnParent = new GameObject("CounterTerrorist Spawns");
        for (int i = 0; i < counterTerroristSpawnCount; i++)
        {
            GameObject spawn = new GameObject($"CT_Spawn_{i + 1}");
            spawn.transform.parent = ctSpawnParent.transform;
            spawn.transform.position = new Vector3(20 - i * 2, 0.5f, 20);
            spawn.transform.rotation = Quaternion.Euler(0, 225, 0);
            spawn.tag = "CounterTerroristSpawn";
        }
    }
    
    void SetupBombSites()
    {
        // Create A site
        GameObject aSite = new GameObject("Bomb Site A");
        aSite.transform.position = new Vector3(15, 0, 15);
        BombSite aSiteComponent = aSite.AddComponent<BombSite>();
        aSiteComponent.siteName = "A";
        
        // Create visual indicator for A site
        GameObject aSiteIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        aSiteIndicator.name = "A Site Indicator";
        aSiteIndicator.transform.parent = aSite.transform;
        aSiteIndicator.transform.localPosition = Vector3.zero;
        aSiteIndicator.transform.localScale = new Vector3(6, 0.1f, 6);
        
        if (bombSiteMaterial != null)
        {
            aSiteIndicator.GetComponent<Renderer>().material = bombSiteMaterial;
        }
        else
        {
            Material siteMat = new Material(Shader.Find("Standard"));
            siteMat.color = Color.green;
            siteMat.SetFloat("_Metallic", 0f);
            aSiteIndicator.GetComponent<Renderer>().material = siteMat;
        }
        
        aSiteComponent.siteIndicator = aSiteIndicator;
        
        // Create B site
        GameObject bSite = new GameObject("Bomb Site B");
        bSite.transform.position = new Vector3(-10, 0, -10);
        BombSite bSiteComponent = bSite.AddComponent<BombSite>();
        bSiteComponent.siteName = "B";
        
        // Create visual indicator for B site
        GameObject bSiteIndicator = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bSiteIndicator.name = "B Site Indicator";
        bSiteIndicator.transform.parent = bSite.transform;
        bSiteIndicator.transform.localPosition = Vector3.zero;
        bSiteIndicator.transform.localScale = new Vector3(6, 0.1f, 6);
        bSiteIndicator.GetComponent<Renderer>().material = aSiteIndicator.GetComponent<Renderer>().material;
        
        bSiteComponent.siteIndicator = bSiteIndicator;
    }
    
    void SetupNavMesh()
    {
        // This would typically be done in the Unity Editor
        // For runtime, we can use NavMeshBuilder (requires NavMeshComponents package)
        Debug.Log("NavMesh should be baked in Unity Editor for AI navigation");
    }
    
    void SpawnAI()
    {
        if (aiPrefab == null)
        {
            Debug.LogWarning("AI Prefab not assigned, creating basic AI objects");
            CreateBasicAI();
            return;
        }
        
        // Spawn terrorist AI
        GameObject[] tSpawns = GameObject.FindGameObjectsWithTag("TerroristSpawn");
        for (int i = 0; i < terroristAICount && i < tSpawns.Length; i++)
        {
            GameObject ai = Instantiate(aiPrefab, tSpawns[i].transform.position, tSpawns[i].transform.rotation);
            ai.name = $"Terrorist_AI_{i + 1}";
            
            AIController aiController = ai.GetComponent<AIController>();
            if (aiController != null)
            {
                aiController.team = AITeam.Terrorist;
            }
        }
        
        // Spawn counter-terrorist AI
        GameObject[] ctSpawns = GameObject.FindGameObjectsWithTag("CounterTerroristSpawn");
        for (int i = 0; i < counterTerroristAICount && i < ctSpawns.Length; i++)
        {
            GameObject ai = Instantiate(aiPrefab, ctSpawns[i].transform.position, ctSpawns[i].transform.rotation);
            ai.name = $"CounterTerrorist_AI_{i + 1}";
            
            AIController aiController = ai.GetComponent<AIController>();
            if (aiController != null)
            {
                aiController.team = AITeam.CounterTerrorist;
            }
        }
    }
    
    void CreateBasicAI()
    {
        // Create basic AI objects if no prefab is provided
        GameObject[] tSpawns = GameObject.FindGameObjectsWithTag("TerroristSpawn");
        for (int i = 0; i < terroristAICount && i < tSpawns.Length; i++)
        {
            GameObject ai = CreateBasicAIObject($"Terrorist_AI_{i + 1}", tSpawns[i].transform.position, AITeam.Terrorist);
        }
        
        GameObject[] ctSpawns = GameObject.FindGameObjectsWithTag("CounterTerroristSpawn");
        for (int i = 0; i < counterTerroristAICount && i < ctSpawns.Length; i++)
        {
            GameObject ai = CreateBasicAIObject($"CounterTerrorist_AI_{i + 1}", ctSpawns[i].transform.position, AITeam.CounterTerrorist);
        }
    }
    
    GameObject CreateBasicAIObject(string name, Vector3 position, AITeam team)
    {
        GameObject ai = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        ai.name = name;
        ai.transform.position = position;
        ai.tag = "Enemy";
        
        // Color based on team
        Material aiMaterial = new Material(Shader.Find("Standard"));
        aiMaterial.color = team == AITeam.Terrorist ? Color.red : Color.blue;
        ai.GetComponent<Renderer>().material = aiMaterial;
        
        // Add components
        ai.AddComponent<NavMeshAgent>();
        ai.AddComponent<AudioSource>();
        
        AIController aiController = ai.AddComponent<AIController>();
        aiController.team = team;
        
        // Create fire point
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.parent = ai.transform;
        firePoint.transform.localPosition = new Vector3(0, 0.5f, 0.5f);
        aiController.firePoint = firePoint.transform;
        
        return ai;
    }
    
    void SetupLighting()
    {
        // Create basic lighting
        GameObject sun = new GameObject("Directional Light");
        Light sunLight = sun.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.color = Color.white;
        sunLight.intensity = 1f;
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        
        // Set ambient lighting
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.7f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.4f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f);
    }
}