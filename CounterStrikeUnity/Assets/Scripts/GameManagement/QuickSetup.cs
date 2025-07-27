using UnityEngine;
using UnityEditor;

/// <summary>
/// 快速设置脚本 - 一键配置Counter Strike Unity游戏场景
/// 在Unity编辑器中通过菜单 "CS Unity/Quick Setup" 来使用
/// </summary>
public class QuickSetup : MonoBehaviour
{
    #if UNITY_EDITOR
    [MenuItem("CS Unity/Quick Setup Game Scene")]
    public static void SetupGameScene()
    {
        Debug.Log("开始快速设置Counter Strike Unity游戏场景...");
        
        // 1. 创建游戏管理器
        CreateGameManagers();
        
        // 2. 创建玩家
        CreatePlayer();
        
        // 3. 设置场景
        SetupScene();
        
        // 4. 创建UI
        CreateUISystem();
        
        Debug.Log("游戏场景设置完成！请记得烘焙NavMesh (Window → AI → Navigation → Bake)");
    }
    
    [MenuItem("CS Unity/Create Player Only")]
    public static void CreatePlayerOnly()
    {
        CreatePlayer();
        Debug.Log("玩家对象创建完成！");
    }
    
    [MenuItem("CS Unity/Create UI System")]
    public static void CreateUISystemOnly()
    {
        CreateUISystem();
        Debug.Log("UI系统创建完成！");
    }
    
    [MenuItem("CS Unity/Setup Tags and Layers")]
    public static void SetupTagsAndLayers()
    {
        // 创建必要的标签
        CreateTag("Player");
        CreateTag("Enemy");
        CreateTag("TerroristSpawn");
        CreateTag("CounterTerroristSpawn");
        CreateTag("BombSite");
        
        Debug.Log("标签设置完成！");
    }
    
    static void CreateGameManagers()
    {
        // 创建GameManager
        GameObject gameManagerObj = new GameObject("GameManager");
        gameManagerObj.AddComponent<GameManager>();
        
        // 创建EconomySystem
        GameObject economySystemObj = new GameObject("EconomySystem");
        economySystemObj.AddComponent<EconomySystem>();
        
        // 创建SceneSetup
        GameObject sceneSetupObj = new GameObject("SceneSetup");
        sceneSetupObj.AddComponent<SceneSetup>();
        
        Debug.Log("游戏管理器创建完成");
    }
    
    static void CreatePlayer()
    {
        // 创建玩家对象
        GameObject player = new GameObject("Player");
        player.tag = "Player";
        
        // 添加CharacterController
        CharacterController characterController = player.AddComponent<CharacterController>();
        characterController.height = 1.8f;
        characterController.radius = 0.4f;
        characterController.center = new Vector3(0, 0.9f, 0);
        
        // 添加AudioSource
        AudioSource audioSource = player.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 1.0f; // 3D音效
        
        // 添加脚本
        PlayerController playerController = player.AddComponent<PlayerController>();
        WeaponSystem weaponSystem = player.AddComponent<WeaponSystem>();
        
        // 设置相机
        GameObject cameraObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (cameraObj == null)
        {
            cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            cameraObj.AddComponent<Camera>();
            cameraObj.AddComponent<AudioListener>();
        }
        
        // 将相机设为玩家子对象
        cameraObj.transform.SetParent(player.transform);
        cameraObj.transform.localPosition = new Vector3(0, 1.6f, 0);
        cameraObj.transform.localRotation = Quaternion.identity;
        
        // 设置相机参数
        Camera camera = cameraObj.GetComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 1000f;
        
        // 连接组件引用
        playerController.playerCamera = camera;
        weaponSystem.playerCamera = camera;
        
        // 创建武器发射点
        GameObject firePoint = new GameObject("FirePoint");
        firePoint.transform.SetParent(cameraObj.transform);
        firePoint.transform.localPosition = new Vector3(0, -0.2f, 0.5f);
        weaponSystem.firePoint = firePoint.transform;
        
        // 设置玩家位置
        player.transform.position = new Vector3(0, 1, 0);
        
        Debug.Log("玩家对象创建完成");
    }
    
    static void SetupScene()
    {
        // 创建地面
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = Vector3.zero;
        floor.transform.localScale = new Vector3(10, 1, 10);
        floor.isStatic = true;
        
        // 创建光照
        GameObject light = new GameObject("Directional Light");
        Light lightComponent = light.AddComponent<Light>();
        lightComponent.type = LightType.Directional;
        lightComponent.color = Color.white;
        lightComponent.intensity = 1f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        
        // 设置环境光
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.5f, 0.7f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.4f, 0.4f, 0.4f);
        RenderSettings.ambientGroundColor = new Color(0.2f, 0.2f, 0.2f);
        
        Debug.Log("基础场景设置完成");
    }
    
    static void CreateUISystem()
    {
        // 创建UIManager
        GameObject uiManagerObj = new GameObject("UIManager");
        UIManager uiManager = uiManagerObj.AddComponent<UIManager>();
        
        // 创建主HUD Canvas
        GameObject hudCanvasObj = new GameObject("HUD Canvas");
        Canvas hudCanvas = hudCanvasObj.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        hudCanvas.sortingOrder = 0;
        
        CanvasScaler hudScaler = hudCanvasObj.AddComponent<CanvasScaler>();
        hudScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        hudScaler.referenceResolution = new Vector2(1920, 1080);
        
        hudCanvasObj.AddComponent<GraphicRaycaster>();
        
        // 创建基础HUD元素
        CreateHUDElements(hudCanvasObj, uiManager);
        
        // 创建购买菜单Canvas
        GameObject buyMenuCanvasObj = new GameObject("Buy Menu Canvas");
        Canvas buyMenuCanvas = buyMenuCanvasObj.AddComponent<Canvas>();
        buyMenuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        buyMenuCanvas.sortingOrder = 10;
        
        CanvasScaler buyMenuScaler = buyMenuCanvasObj.AddComponent<CanvasScaler>();
        buyMenuScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        buyMenuScaler.referenceResolution = new Vector2(1920, 1080);
        
        buyMenuCanvasObj.AddComponent<GraphicRaycaster>();
        buyMenuCanvasObj.SetActive(false);
        
        // 创建消息Canvas
        GameObject messageCanvasObj = new GameObject("Message Canvas");
        Canvas messageCanvas = messageCanvasObj.AddComponent<Canvas>();
        messageCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        messageCanvas.sortingOrder = 20;
        
        CanvasScaler messageScaler = messageCanvasObj.AddComponent<CanvasScaler>();
        messageScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        messageScaler.referenceResolution = new Vector2(1920, 1080);
        
        messageCanvasObj.AddComponent<GraphicRaycaster>();
        
        // 连接UI引用
        uiManager.hudCanvas = hudCanvas;
        uiManager.buyMenuCanvas = buyMenuCanvas;
        uiManager.messageCanvas = messageCanvas;
        
        // 创建EventSystem
        if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        Debug.Log("UI系统创建完成");
    }
    
    static void CreateHUDElements(GameObject hudCanvas, UIManager uiManager)
    {
        // 创建健康值文本
        GameObject healthTextObj = new GameObject("Health Text");
        healthTextObj.transform.SetParent(hudCanvas.transform, false);
        UnityEngine.UI.Text healthText = healthTextObj.AddComponent<UnityEngine.UI.Text>();
        healthText.text = "Health: 100";
        healthText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        healthText.fontSize = 24;
        healthText.color = Color.white;
        
        RectTransform healthRect = healthTextObj.GetComponent<RectTransform>();
        healthRect.anchorMin = new Vector2(0, 0);
        healthRect.anchorMax = new Vector2(0, 0);
        healthRect.anchoredPosition = new Vector2(20, 20);
        healthRect.sizeDelta = new Vector2(200, 30);
        
        uiManager.healthText = healthText;
        
        // 创建金钱文本
        GameObject moneyTextObj = new GameObject("Money Text");
        moneyTextObj.transform.SetParent(hudCanvas.transform, false);
        UnityEngine.UI.Text moneyText = moneyTextObj.AddComponent<UnityEngine.UI.Text>();
        moneyText.text = "Money: $800";
        moneyText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        moneyText.fontSize = 24;
        moneyText.color = Color.green;
        
        RectTransform moneyRect = moneyTextObj.GetComponent<RectTransform>();
        moneyRect.anchorMin = new Vector2(0, 1);
        moneyRect.anchorMax = new Vector2(0, 1);
        moneyRect.anchoredPosition = new Vector2(20, -20);
        moneyRect.sizeDelta = new Vector2(200, 30);
        
        uiManager.moneyText = moneyText;
        
        // 创建弹药文本
        GameObject ammoTextObj = new GameObject("Ammo Text");
        ammoTextObj.transform.SetParent(hudCanvas.transform, false);
        UnityEngine.UI.Text ammoText = ammoTextObj.AddComponent<UnityEngine.UI.Text>();
        ammoText.text = "30 / 90";
        ammoText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        ammoText.fontSize = 28;
        ammoText.color = Color.white;
        ammoText.alignment = TextAnchor.MiddleRight;
        
        RectTransform ammoRect = ammoTextObj.GetComponent<RectTransform>();
        ammoRect.anchorMin = new Vector2(1, 0);
        ammoRect.anchorMax = new Vector2(1, 0);
        ammoRect.anchoredPosition = new Vector2(-20, 20);
        ammoRect.sizeDelta = new Vector2(200, 30);
        
        uiManager.ammoText = ammoText;
        
        // 创建计时器文本
        GameObject timerTextObj = new GameObject("Timer Text");
        timerTextObj.transform.SetParent(hudCanvas.transform, false);
        UnityEngine.UI.Text timerText = timerTextObj.AddComponent<UnityEngine.UI.Text>();
        timerText.text = "01:30";
        timerText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        timerText.fontSize = 32;
        timerText.color = Color.yellow;
        timerText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform timerRect = timerTextObj.GetComponent<RectTransform>();
        timerRect.anchorMin = new Vector2(0.5f, 1);
        timerRect.anchorMax = new Vector2(0.5f, 1);
        timerRect.anchoredPosition = new Vector2(0, -20);
        timerRect.sizeDelta = new Vector2(200, 40);
        
        uiManager.timerText = timerText;
        
        // 创建准星
        GameObject crosshairObj = new GameObject("Crosshair");
        crosshairObj.transform.SetParent(hudCanvas.transform, false);
        UnityEngine.UI.Image crosshair = crosshairObj.AddComponent<UnityEngine.UI.Image>();
        crosshair.color = Color.white;
        
        // 创建简单的十字准星贴图
        Texture2D crosshairTexture = new Texture2D(32, 32);
        for (int x = 0; x < 32; x++)
        {
            for (int y = 0; y < 32; y++)
            {
                if ((x == 15 || x == 16) && (y >= 12 && y <= 19) || 
                    (y == 15 || y == 16) && (x >= 12 && x <= 19))
                {
                    crosshairTexture.SetPixel(x, y, Color.white);
                }
                else
                {
                    crosshairTexture.SetPixel(x, y, Color.clear);
                }
            }
        }
        crosshairTexture.Apply();
        
        Sprite crosshairSprite = Sprite.Create(crosshairTexture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        crosshair.sprite = crosshairSprite;
        
        RectTransform crosshairRect = crosshairObj.GetComponent<RectTransform>();
        crosshairRect.anchorMin = new Vector2(0.5f, 0.5f);
        crosshairRect.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairRect.anchoredPosition = Vector2.zero;
        crosshairRect.sizeDelta = new Vector2(32, 32);
        
        uiManager.crosshair = crosshair;
    }
    
    static void CreateTag(string tagName)
    {
        // 使用SerializedObject来添加标签
        UnityEngine.Object[] asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if ((asset != null) && (asset.Length > 0))
        {
            SerializedObject so = new SerializedObject(asset[0]);
            SerializedProperty tags = so.FindProperty("tags");
            
            for (int i = 0; i < tags.arraySize; ++i)
            {
                if (tags.GetArrayElementAtIndex(i).stringValue == tagName)
                {
                    return; // 标签已存在
                }
            }
            
            tags.InsertArrayElementAtIndex(0);
            tags.GetArrayElementAtIndex(0).stringValue = tagName;
            so.ApplyModifiedProperties();
            so.Update();
        }
    }
    
    [MenuItem("CS Unity/Help/Show Controls")]
    public static void ShowControls()
    {
        string controls = @"
Counter Strike Unity - 控制说明

移动控制:
WASD - 移动
鼠标 - 视角控制
Shift - 静步
Ctrl - 蹲下
Space - 跳跃

武器控制:
左键 - 射击
右键 - 开镜(AWP)
R - 换弹
1-2 - 切换武器
滚轮 - 武器切换

游戏功能:
B - 打开购买菜单
E - 拆除炸弹
Esc - 释放鼠标/暂停

设置完成后记得:
1. 烘焙NavMesh (Window → AI → Navigation → Bake)
2. 设置玩家和AI的Layer和Tag
3. 测试所有功能是否正常
        ";
        
        EditorUtility.DisplayDialog("控制说明", controls, "确定");
    }
    
    [MenuItem("CS Unity/Help/Setup Guide")]
    public static void ShowSetupGuide()
    {
        string guide = @"
Counter Strike Unity - 设置指南

快速开始:
1. 点击 'CS Unity/Quick Setup Game Scene' 创建完整场景
2. 点击 'CS Unity/Setup Tags and Layers' 设置标签
3. 打开 Window → AI → Navigation，点击 Bake 烘焙NavMesh
4. 按Play开始游戏

手动设置:
1. 使用 'CS Unity/Create Player Only' 只创建玩家
2. 使用 'CS Unity/Create UI System' 只创建UI
3. 手动添加AI和场景元素

注意事项:
- 确保使用Unity 2021.3 LTS
- 所有墙壁和地面需要设置为Navigation Static
- AI对象需要NavMeshAgent组件
- 玩家需要CharacterController组件

遇到问题请查看README.md文件
        ";
        
        EditorUtility.DisplayDialog("设置指南", guide, "确定");
    }
    #endif
}