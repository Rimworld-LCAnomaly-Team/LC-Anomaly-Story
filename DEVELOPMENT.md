# LC Anomaly Story 开发说明

## 当前范围

本项目只维护 RimWorld 1.6。`LC Anomaly Core` 提供异常实体、收容、工作、PE-Box、E.G.O 与研究令牌；本项目维护公司发展、任务状态、统计、解锁桥接和玩家界面。

首个可玩闭环由三个数据驱动节点组成：

1. 建立收容业务：建成收容平台并登记首个异想体。
2. 形成运营经验：节点开放后新增 5 次有效工作、20 点 PE-Box，并使任意异想体达到二级研究。
3. 批准基础能源管理：玩家主动批准后，在后台完成 Core 的 `ExtractEnkephalin` 兼容令牌并开放控制部状态。

当前接管 Core 的九个研究令牌。零阶段经营许可授予基础提取、Cogito 和收容能力；后续令牌严格按照 Core 原前置链映射到五阶段发展节点，避免 `FinishProject` 递归造成越级解锁。

系统目前包含：

- 公司里程碑、部门任务和阶段考核三类发展节点；
- 独立的限时动态指令；
- 主动开始、失败冷却、恢复重试的综合考核；
- 可由 submod 扩展的考核 Worker 生命周期、组合 Effect 和尝试级存档状态；
- 正式部门 Def、开放状态和可更换的短期优先路线；
- 从经营许可到公司终局的五阶段数据框架。

## 架构边界

- `GameComponent_CompanyDevelopment` 是公司进度的唯一权威来源。
- Core 研究完成状态只作为内容解锁兼容令牌，不反推公司节点完成状态。
- XML Def 描述节点、条件与奖励；业务内容不得集中写入按 `defName` 分支的大型 `switch`。
- UI 只读取组件状态，并通过组件公开方法申请节点完成。
- Harmony Patch 只采集明确事件或封锁带 `ManagedResearchExtension` 标记的研究项目。
- 考核 Worker 必须保持无状态；每次尝试的数据写入 `ExaminationRuntime` 基础类型状态槽，复杂对象交给内容包自己的组件保存。
- `Core16LegacyTypes` 仅为 Core 1.6 遗留的 `ITabDef` 和短类型名提供兼容垫片；不得在其中承载 Story 业务状态。
- 统计增量条件以节点激活时的基准为起点，避免中途加入旧存档时立即完成累计任务。
- 节点完成、奖励和信件分别保存幂等状态。失败的奖励会在后续低频校验中重试。

## 目录

```text
1.6/
  Assemblies/                         编译输出
  Defs/CompanyDevelopmentDefs/        公司发展节点
  Defs/MainButtonDefs/                主界面入口
  Languages/                          英文与简体中文
  Patches/                            Core Def 标记
Source/1.6/
  LCAnomalyStory/
    Compatibility/                    Core 研究桥
    Components/                       权威状态与存档
    Conditions/                       可扩展条件
    Defs/                             Def 模型
    Examinations/                     考核 Worker、Context 与 Effect 扩展 API
    Patches/                          事件采集与研究拦截
    Rewards/                          可扩展奖励
    UI/                               只读发展界面
    Debug/                            开发者模式诊断
Tools/                                本地验证脚本
```

## 本地构建

复制 `Source/1.6/LocalDllPaths.props.example` 为 `Source/1.6/LocalDllPaths.props`，并设置：

- `RimWorldManagedDir`
- `HarmonyAssembliesDir`，RimWorld 1.6 应指向 Harmony 的 `Current/Assemblies`
- `LCAnomalyCoreAssembliesDir`，必须指向 Core 的 `1.6/Assemblies`

运行：

```powershell
./Tools/Validate.ps1
```

脚本会解析全部 XML，并使用 Visual Studio MSBuild 构建 Release。机器私有的 `LocalDllPaths.props` 已被 Git 忽略。

在 Core、Story 和 Harmony 已挂载到 RimWorld 本地 `Mods`，且 `.runtime-test/Config/ModsConfig.xml` 已配置时，可以运行隔离冒烟测试：

```powershell
./Tools/RuntimeSmokeTest.ps1
```

脚本使用独立存档目录和隐藏的图形窗口，检测到 Story 初始化后关闭自己的 RimWorld 测试进程，并检查 Def、交叉引用、翻译、程序集和 Harmony 错误。

## 存档字段

当前数据版本为 `3`，保存以下内容：

- 已完成节点；
- 已成功应用的奖励；
- 已发送的完成信件；
- 已开放部门；
- 可扩展统计字典；
- 节点激活 Tick 与统计基准；
- 综合考核的尝试、期限、冷却、Worker Tick、结果原因、独立基准和尝试级基础类型状态；
- 当前动态指令、期限、冷却和独立基准；
- 当前优先发展部门；
- 数据版本和下次低频校验 Tick。

新增或重命名 Def 时必须同步迁移逻辑，不得依赖已删除 Def 的强制交叉引用来恢复旧存档。

## 下一阶段

1. 增加更多部门专属指令与综合考核内容。
2. 将第四、第五阶段扩展为具有实际危机机制和叙事结果的内容。
3. 增加动态难度缩放、失败惩罚和部门永久升级。
4. 建立自动化旧存档样本和游戏内回归测试。
5. 完成美术资源、信息层级和最终 UI 布局。
