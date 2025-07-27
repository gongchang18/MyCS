# Counter Strike Unity - 3D第一人称反恐精英风格游戏

## 项目概述

这是一个基于Unity 2021.3 LTS开发的3D第一人称射击游戏，模拟经典反恐精英（Counter-Strike）的核心玩法。游戏包含完整的炸弹模式、经济系统、AI对手和武器系统。

## 技术规格

- **引擎**: Unity 2021.3 LTS
- **编程语言**: C#
- **目标平台**: Windows (.exe)
- **渲染管线**: Built-in Render Pipeline
- **物理系统**: Unity Physics
- **AI导航**: Unity NavMesh System

## 核心功能

### 🎮 游戏模式
- **炸弹模式**: 5v5 CT vs T对抗
- **购买阶段**: 15秒武器购买时间
- **战斗阶段**: 90秒回合时间
- **炸弹机制**: 45秒爆炸倒计时，5秒拆除时间

### 🔫 武器系统
- **主武器**: M4A1 (CT)、AK47 (T)、AWP (狙击枪)
- **副武器**: USP (CT起始)、Glock (T起始)、沙漠之鹰
- **投掷物**: 高爆手雷、闪光弹、烟雾弹
- **后坐力系统**: 真实的射击反馈和准星扩散
- **换弹系统**: 不同武器的换弹时间和动画

### 🤖 AI系统
- **状态机**: 巡逻 → 警戒 → 交战 → 目标执行
- **智能寻路**: 基于NavMesh的路径规划
- **视觉检测**: FOV和视线遮挡检测
- **听觉系统**: 脚步声和枪声反应
- **战术行为**: 掩体使用、侧移射击、团队配合

### 💰 经济系统
- **起始金钱**: 800$
- **击杀奖励**: 500$
- **胜利奖励**: 3000$
- **连败补偿**: 最高3400$额外奖励
- **武器商店**: 完整的购买界面和价格体系

### 🎯 第一人称体验
- **真实移动**: 走路、跑步、蹲下、静步
- **相机摇摆**: 跑步时的真实晃动效果
- **视角控制**: 鼠标灵敏度调节，80°垂直限制
- **准星系统**: 动态准星大小反映射击精度

## 项目结构

```
CounterStrikeUnity/
├── Assets/
│   ├── Scripts/
│   │   ├── Player/           # 玩家控制相关
│   │   │   └── PlayerController.cs
│   │   ├── Weapons/          # 武器系统
│   │   │   └── WeaponSystem.cs
│   │   ├── AI/               # AI控制器
│   │   │   └── AIController.cs
│   │   ├── Economy/          # 经济系统
│   │   │   └── EconomySystem.cs
│   │   ├── GameManagement/   # 游戏管理
│   │   │   ├── GameManager.cs
│   │   │   ├── BombSite.cs
│   │   │   └── SceneSetup.cs
│   │   └── UI/               # 用户界面
│   │       ├── UIManager.cs
│   │       └── MainMenu.cs
│   ├── Scenes/               # 游戏场景
│   ├── Materials/            # 材质资源
│   ├── Prefabs/             # 预制体
│   └── Sounds/              # 音频资源
└── ProjectSettings/         # Unity项目设置
```

## 安装与配置

### 系统要求
- **操作系统**: Windows 10/11 64位
- **Unity版本**: Unity 2021.3 LTS
- **内存**: 最低4GB RAM，推荐8GB
- **显卡**: 支持DirectX 11的独立显卡
- **存储空间**: 至少2GB可用空间

### 安装步骤

1. **下载Unity Hub**
   ```
   https://unity3d.com/get-unity/download
   ```

2. **安装Unity 2021.3 LTS**
   - 打开Unity Hub
   - 点击"Installs" → "Install Editor"
   - 选择"2021.3 LTS"版本
   - 勾选"Windows Build Support (IL2CPP)"

3. **导入项目**
   - 打开Unity Hub
   - 点击"Projects" → "Open"
   - 选择`CounterStrikeUnity`文件夹
   - 等待Unity导入所有资源

4. **配置NavMesh**
   - 打开Unity编辑器
   - 选择所有地面和墙壁对象
   - 在Inspector中勾选"Navigation Static"
   - 打开Window → AI → Navigation
   - 点击"Bake"生成导航网格

## 游戏设置

### 创建游戏场景

1. **创建主场景**
   ```csharp
   // 在Hierarchy中创建空对象并添加以下脚本：
   - GameManager (挂载GameManager.cs)
   - EconomySystem (挂载EconomySystem.cs)
   - UIManager (挂载UIManager.cs)
   - SceneSetup (挂载SceneSetup.cs)
   ```

2. **设置玩家对象**
   ```csharp
   // 创建Player对象：
   - 添加CharacterController组件
   - 添加AudioSource组件
   - 添加PlayerController.cs脚本
   - 添加WeaponSystem.cs脚本
   - 设置Tag为"Player"
   ```

3. **配置相机**
   ```csharp
   // 将Main Camera设为Player的子对象：
   - Position: (0, 1.6, 0)
   - Rotation: (0, 0, 0)
   - Field of View: 60
   ```

### 控制设置

| 按键 | 功能 |
|------|------|
| WASD | 移动 |
| 鼠标 | 视角控制 |
| 左键 | 射击 |
| 右键 | 开镜(AWP) |
| R | 换弹 |
| Shift | 静步 |
| Ctrl | 蹲下 |
| Space | 跳跃 |
| 1-2 | 切换武器 |
| 滚轮 | 武器切换 |
| B | 打开购买菜单 |
| E | 拆除炸弹 |
| Esc | 释放鼠标/暂停 |

## 编译与发布

### 构建Windows可执行文件

1. **打开Build Settings**
   - File → Build Settings
   - 选择"PC, Mac & Linux Standalone"
   - Target Platform: Windows
   - Architecture: x86_64

2. **配置Player Settings**
   - Company Name: CS Unity Studio
   - Product Name: Counter Strike Unity
   - Icon: 设置游戏图标
   - Resolution: 默认1920x1080

3. **构建游戏**
   - 点击"Build"
   - 选择输出文件夹
   - 等待编译完成

4. **测试可执行文件**
   - 运行生成的.exe文件
   - 测试所有核心功能
   - 检查性能和稳定性

## 游戏玩法

### 基础玩法
1. **回合开始**: 进入15秒购买阶段
2. **购买装备**: 按B键打开商店，购买武器和装备
3. **战斗阶段**: 90秒内完成目标
4. **胜利条件**:
   - 消灭所有敌人
   - 成功安放并引爆炸弹(T)
   - 成功拆除炸弹(CT)
   - 时间耗尽(CT胜利)

### 进阶技巧
- **经济管理**: 合理分配金钱，考虑连败奖励
- **位置控制**: 利用掩体和地图优势
- **团队配合**: 与AI队友协调进攻/防守
- **武器选择**: 根据经济情况选择合适武器
- **时机把握**: 掌握购买、进攻、撤退时机

## 故障排除

### 常见问题

1. **AI不移动**
   - 检查NavMesh是否正确烘焙
   - 确认AI对象有NavMeshAgent组件
   - 验证目标点在NavMesh范围内

2. **射击无效果**
   - 检查武器的firePoint是否正确设置
   - 确认LayerMask配置正确
   - 验证敌人标签为"Enemy"

3. **UI不显示**
   - 检查Canvas的Render Mode设置
   - 确认UI元素在正确的层级
   - 验证Camera的Culling Mask

4. **性能问题**
   - 减少AI数量
   - 降低材质质量
   - 优化光照设置
   - 使用LOD系统

### 调试工具
```csharp
// 在Console中查看调试信息：
Debug.Log("Player Health: " + playerController.currentHealth);
Debug.Log("Current Weapon: " + weaponSystem.GetCurrentWeapon().weaponName);
Debug.Log("AI State: " + aiController.GetCurrentState());
```

## 扩展开发

### 添加新武器
```csharp
// 在WeaponSystem.cs的GetWeaponData方法中添加：
case "MP5":
    return new WeaponData("MP5", 26f, 0.08f, 120f, 30, 120, 2.5f, 1.8f, true, false, 1500, WeaponType.Rifle);
```

### 创建新地图
```csharp
// 修改SceneSetup.cs中的地图生成逻辑：
void CreateCustomMap() {
    // 添加自定义墙壁、掩体和炸弹点
}
```

### 自定义AI行为
```csharp
// 在AIController.cs中添加新的AI状态：
public enum AIState {
    Patrol, Alert, Combat, PlantBomb, DefuseBomb, Retreat, Dead
}
```

## 许可证

本项目仅供学习和教育用途。请勿用于商业目的。

## 技术支持

如遇到技术问题，请检查：
1. Unity版本是否为2021.3 LTS
2. 所有必要组件是否正确添加
3. NavMesh是否正确烘焙
4. 脚本是否有编译错误

## 更新日志

### v1.0.0 (初始版本)
- ✅ 完整的第一人称控制系统
- ✅ 武器射击和后坐力系统
- ✅ AI状态机和寻路系统
- ✅ 经济系统和商店界面
- ✅ 炸弹模式和回合管理
- ✅ 基础UI和HUD系统
- ✅ Windows构建支持

---

**开发团队**: CS Unity Studio  
**开发时间**: 2024年  
**引擎版本**: Unity 2021.3 LTS  
**目标平台**: Windows PC