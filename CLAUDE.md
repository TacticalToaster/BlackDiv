# BlackDiv Mod 修改项目

## 项目概述
基于 [TacticalToaster/BlackDiv](https://github.com/TacticalToaster/BlackDiv) (v1.0.1) 的修改版本，目标是自定义 Black Division 阵营在 SPT 4.0.13 中的行为。

## 用户信息
- SPT 安装路径: `C:\EFT\SPT`
- 用户角色: USEC
- 源码路径: `C:\Users\Larkl\OneDrive\文档\Claude\Projects\EFT\BlackDiv-modified`

## 已完成的修改

### 1. 刷新率提升 (Server/Controllers/SpawnController.cs)
- Labs 常规刷新: 15% → **80%**
- Labs 撤离点 Gate1 事件触发: 20% → **80%**
- Labs 撤离点 Gate2 事件触发: 20% → **80%**
- 其他 10 张地图的 hunt 刷新本身就是 100%，未改

### 2. 服务端阵营关系 (Server/Mod.cs)
- 删除了 `factionService.AddEnemyByFaction(typeList, "usec")` — BD 不再视 USEC 阵营为敌
- 删除了 `factionService.AddEnemyByFaction("usec", "blackdiv")` — USEC 阵营不再视 BD 为敌
- 新增 `factionService.AddFriendlyByFaction(typeList, "usec")` — BD 主动对 USEC 友好
- 新增 `factionService.AddFriendlyByFaction("usec", "blackdiv")` — USEC 主动对 BD 友好
- BEAR 和其他阵营（savage, rogues, infected）的敌对关系保持不变
- 效果：Hunt 系统驱动 BD 跑向玩家，到达后不攻击，自然形成护卫队效果

### 3. Bot AI 行为配置 (Server/db/bots/sharedTypes/blackDiv.json)
- 所有难度（easy/normal/hard/impossible）的 `DEFAULT_USEC_BEHAVIOUR` 改为 **"Neutral"**
  - 原始值：easy/hard 为 `Warn`，normal/impossible 为 `AlwaysEnemies`
- 所有难度的 `DEFAULT_BEAR_BEHAVIOUR` 保持 **"AlwaysEnemies"**
- 效果：BD 对 USEC 玩家完全无视，不警告，不攻击

### 4. Hunt 追猎触发 (Plugin/Components/HuntManager.cs)
- 触发概率: 8% → **100%**（去掉了随机判定）
- 初始等待时间: 240 秒 → **30 秒**
- 循环检查间隔: 240 秒 → **30 秒**
- Hunt 系统只控制导航（GoToPoint 移动到目标位置），实际交战由阵营关系决定
- 追猎目标保留 USEC 和 BEAR — BD 会跑向 USEC 玩家但不攻击，形成自动护卫队

### 5. 编译配置修改
- **Server/BlackDiv.csproj**: PostBuild 输出路径从 `..\..\..\ SPT\user\mods\` 改为 `$(ProjectDir)..\output\`
- **Plugin/Plugin.csproj**: 所有 HintPath 从相对路径改为绝对路径指向 `C:\EFT\`；PostBuild 输出到 `..\output\`
- **Prepatch/Prepatch.csproj**: 同上改为绝对路径 `C:\EFT\`；AssemblyName 从 `BlackDiv` 改为 `BlackDivPrepatch`（解决 NuGet Ambiguous project name 报错）

## 编译与部署

### Server 端 (BlackDivServer.dll)
```bash
cd BlackDiv-modified\Server
dotnet build -c Release
# DLL 生成在: Server\bin\Release\BlackDiv\BlackDivServer.dll
# 替换到: C:\EFT\SPT\user\mods\BlackDivServer\BlackDivServer.dll
```

### Client 端 (BlackDiv.dll)
```bash
cd BlackDiv-modified\Plugin
dotnet build Plugin.csproj -c Release
# DLL 生成在: Plugin\bin\Release\net471\BlackDiv.dll
# 替换到: C:\EFT\BepInEx\plugins\BlackDiv\BlackDiv.dll
```

### 数据文件 (不需要编译)
```bash
# blackDiv.json 直接覆盖:
copy "Server\db\bots\sharedTypes\blackDiv.json" "C:\EFT\SPT\user\mods\BlackDivServer\db\bots\sharedTypes\blackDiv.json"
```

## 已部署的文件
- [x] `BlackDivServer.dll` — 已编译并复制到 SPT（含 AddFriendlyByFaction）
- [x] `BlackDiv.dll` — 已编译并复制到 SPT
- [x] `blackDiv.json` — 已部署（DEFAULT_USEC_BEHAVIOUR 全部 Neutral）

## 待验证/待办
- 验证 `"Neutral"` + `AddFriendlyByFaction` 在游戏中是否符合预期（BD 不警告、不攻击 USEC 玩家）
- 验证 BD Hunt 触发后能主动跑向玩家（护卫队效果）
- 验证 BD 不误伤 USEC 玩家（交叉火力/手雷场景）
- 刷新率改动已验证成功

## MoreBotsAPI FactionService 参考
通过反编译 `Reference/MoreBotsServer/MoreBotsServer.dll` 确认的可用方法：

| 方法 | 说明 |
|---|---|
| `AddEnemyByFaction(types, faction)` | 设为敌对（对应 JSON: AlwaysEnemies） |
| `AddWarnByFaction(types, faction)` | 设为警告（对应 JSON: Warn） |
| `AddFriendlyByFaction(types, faction)` | 设为友好（共享战斗感知，不互相攻击） |
| `AddRevengeByFaction(types, faction)` | 被攻击才还手 |

JSON `DEFAULT_*_BEHAVIOUR` 可用值（从 SPT 数据库实际枚举）：
- `"AlwaysEnemies"` — 始终敌对
- `"Warn"` — 靠近警告，不主动打
- `"Neutral"` — 完全无视
- ~~`"AlwaysFriendly"`~~ — **不存在**

## 同目录下的另一个项目
`Tarkov-1.0-Backport-main` — WTT Content Backport 项目，之前修改了 `BackportJunkDisabler.cs` 的 `_itemsToBlacklist`，去掉了 9 个 Black Division 物品（胸挂/板甲 4 个、夜视 1 个、armband 4 个），只保留 3 个非 BD 条目（LP Gamma, Battle Worn Gamma, LP Fanny），让 BD 物品不被加入奖励黑名单。
