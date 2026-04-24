# SkillCharacterController

基于 **KinematicCharacterController (KCC)** + **Unity InputSystem** + **RootMotion 补偿** 的一体化角色控制器。

## 文件结构

```
Assets/TimelineSkill/KCC/
├── SkillCharacterController.cs              ← 控制器本体 (命名空间: UnityTimeline)
├── README.md                                ← 本文档
└── Editor/
    └── SkillCharacterControllerEditor.cs    ← 自定义 Inspector (命名空间: SkillCharacterControllerEditor)
```

> **注意**: Editor 脚本使用独立命名空间 `SkillCharacterControllerEditor` 以避免与 `UnityEditor.Editor` 类型冲突。

---

## 快速开始

1. 创建 GameObject → 添加组件 **Skill Character Controller**
2. `KinematicCharacterMotor` 和 `Animator` 会**自动挂载并配置**（`applyRootMotion = true`）
3. Inspector 中配置输入：
   - **Move Action**: 从 Input Action Asset 拖入移动 Vector2 Action
   - **Jump Action**: 从 Input Action Asset 拖入跳跃 Button Action
   - **Orientation Reference**: 相机/方向参照物 GameObject（可选，见下方说明）
4. 根据游戏需求调整 Ground/Air 的 RootMotion 模式和 Jump 模式

---

## Orientation Reference（朝向参照系）

| 状态 | 行为 | 适用场景 |
|------|------|---------|
| **已设置** (拖了相机等) | 前方 = 参照物前方，右方 = 参照物右方 | 第三人称跟随相机、固定视角游戏 |
| **为空 (null)** | 前方 = 角色自身 forward，右方 = 角色自身 right | 格斗、横版卷轴、俯视角游戏 |

> 无论哪种情况，按下 W 都会让角色往"前方"移动。有相机时前方是屏幕前；无相机时前方是角色当前朝向。

---

## RootMotion 模式详解

### 设计决策：FullRootMotion 下的旋转行为

**重要**：即使选择 `FullRootMotion`（完全由动画驱动），**旋转朝向仍由玩家输入控制**。

原因：绝大多数走/跑/待机动画的 Animator deltaRotation 是 identity（不含有效旋转），如果完全依赖动画旋转，角色永远不会转向。

实际行为：
```
FullRootMotion UpdateRotation():
  1. 先叠加动画的 deltaRotation（支持转身/掉头/受击转身等动画）
  2. 再用输入方向插值修正朝向 ← 关键步骤
```
这符合魂系/MH 等实际游戏的常见做法：**位移由 Root Motion 驱动，朝向由输入控制**。

### Ground / Air 独立配置

| 模式 | 位移 | 旋转 | 典型用途 |
|------|------|------|---------|
| **Ground: FullRootMotion** | 动画 deltaPosition / deltaTime | 动画旋转 + 输入方向插值 | 走跑跳等常规地面动作 |
| **Ground: IgnoreRootMotion** | 输入控制速度 Lerp | 输入方向 Slerp | 程序化移动、无动画位移的场景 |
| **Air: FullRootMotion** | 有动画位移则用 RM，否则保持当前速度 | 同上（RM+输入） | 受击飞行、特定空中动画 |
| **Air: IgnoreRootMotion** | 输入加速 + 重力 + Drag | 输入方向 Slerp | 自由落体、空中可控移动 |

### IgnoreRootMotion 模式参数

当对应阶段选择 IgnoreRootMotion 时，以下参数生效：

**Stable Movement（地面）：**
- `MaxStableMoveSpeed` — 最大地面移动速度 (默认 6)
- `StableMovementSharpness` — 速度变化锐度，越高越灵敏 (默认 15)
- `OrientationSharpness` — 旋转插值锐度 (默认 20)
- `OrientationMethod`:
  - `TowardsReference` — 始终朝向 OrientationReference 前方
  - `TowardsMovement` — 朝向实际移动输入方向（推荐）

**Air Movement（空中）：**
- `MaxAirMoveSpeed` — 最大空中速度 (默认 10)
- `AirAccelerationSpeed` — 空中加速度 (默认 30)
- `Drag` — 空气阻力 (默认 0.5)

---

## Jump 跳跃模式

| 模式 | 说明 | 参数 |
|------|------|------|
| **BuiltIn** | KCC 内置跳跃逻辑 | JumpUpSpeed, ScalableForwardSpeed, Pre/Post-GroundingGraceTime, AllowJumpWhenSliding |
| **ExternalControlled** | 完全由外部系统控制（Timeline / Animancer） | 无内置参数生效，通过 AddVelocity 或 Animator 触发 |

---

## 公共 API

### 补偿 API（对齐 TimelineRedirectRootMotion）

```csharp
// 设置补偿位置偏移（下一帧起 N 帧内锁定 Transform）
controller.SetCompensationPosition(Vector3 position);

// 设置补偿旋转偏移（Euler 角度）
controller.SetCompensationRotation(Vector3 eulerAngles);

// 同时设置位置+旋转
controller.SetCompensation(Vector3 position, Vector3 rotationEuler);

// 清除所有补偿
controller.ClearCompensation();

// 开关
controller.CompensationEnabled = true/false;  // 默认开启
```

**工作原理：**
- 默认 `CompensationFrames = 2`（跳针机制）
- 调用 SetCompensation 后，接下来 N 帧 OnAnimatorMove 锁定 Transform 位姿
- 帧数耗尽后自动 ClearCompensation，恢复正常 RootMotion 累加

**典型用法：**
```csharp
// 技能播放时固定角色位姿
controller.SetCompensation(transform.position, transform.rotation.eulerAngles);

// ... 技能结束后 ...
controller.ClearCompensation();
```

### 输入接口

```csharp
// 内部 PlayerCharacterInputs 结构体
public void SetInputs(PlayerCharacterInputs inputs);
```

| 字段 | 类型 | 说明 |
|------|------|------|
| MoveAxisForward | float | 前后输入 (-1 ~ 1) |
| MoveAxisRight | float | 左右输入 (-1 ~ 1) |
| JumpDown | bool | 本帧跳跃按键 |
| CameraRotation | Quaternion | 参照物 Y 轴旋转 |

### 只读属性

```csharp
controller.Motor              // KinematicCharacterMotor 引用
controller.CharacterAnimator // Animator 引用
controller.MoveAction         // 当前 Move InputActionReference
controller.JumpAction         // 当前 Jump InputActionReference
controller.OrientationReference // 当前方向参照物
controller.GroundRootMotionMode // 地面 RM 模式
controller.AirRootMotionMode    // 空中 RM 模式
controller.CurrentJumpMode      // 当前跳跃模式
```

---

## ICharacterController 接口实现

完整实现 KCC 要求的所有回调：

| 方法 | 用途 |
|------|------|
| `UpdateRotation()` | 根据 RM 模式处理旋转（含 FullRM 下输入插值）|
| `UpdateVelocity()` | 根据 RM 模式处理速度 + 内置重力 |
| `BeforeCharacterUpdate()` | 空（预留给扩展）|
| `AfterCharacterUpdate()` | 重置 RootMotion deltas + 处理跳跃请求状态 |
| `PostGroundingUpdate()` | Grace Time 管理（落地/离地宽容时间）|
| `IsColliderValidForCollisions()` | 过滤 IgnoredColliders 列表 |
| `OnGroundHit()` | 空（预留给扩展）|
| `OnMovementHit()` | 空（预留给扩展）|
| `ProcessHitStabilityReport()` | 空（6参数签名，含 hitNormal + hitPositionInLocalSpace）|
| `OnDiscreteCollisionDetected()` | 空（预留给扩展）|

---

## Inspector 编辑器特性

自定义 Editor (`SkillCharacterControllerEditor`) 提供：

- **彩色分组标题栏** — 7 个可折叠区域，各具颜色背景
- **条件灰显** — Stable/Air Movement 在 IgnoreRM 时高亮，FullRM 时变暗提示不可用
- **模式描述文字** — 切换 RootMotion 模式后显示当前模式的 ✦ 描述
- **Jump 子面板** — BuiltIn 展开参数，External 显示提示信息框
- **运行时调试** — Compensation 区域实时显示 Position/Rotation
- **输入缺失提醒** — OrientationReference 为空时弹出 Info 提示
- **状态徽章** — 顶部标题栏右侧显示 Running(绿)/Editing(灰)

### 分组一览

| 分组 | 颜色 | 内容 |
|------|------|------|
| 🎮 Input Settings | 蓝 | Move/Jump Action, Orientation Reference |
| 🎬 Root Motion Mode | 紫 | Ground Mode + Air Mode 下拉 + 模式说明 |
| 🦘 Jump Settings | 绿 | JumpMode 选择 + BuiltIn 参数 / External 提示 |
| 🏃 Stable Movement | 橙 | MaxSpeed, Sharpness, OrientationMethod (IgnoreRM 时) |
| 🪂 Air Movement | 橙 | MaxAirSpeed, Acceleration, Drag (IgnoreRM 时) |
| ⚙️ Misc | 灰 | Gravity, MeshRoot, IgnoredColliders |
| 🔧 Compensation Debug | 红 | 运行时状态 / 编辑时 API 帮助 |

---

## 已知适配记录

以下是在集成过程中针对项目 KCC 版本做的适配修复：

| 问题 | 修复 |
|------|------|
| `HitStabilityRequest` 类型不存在 | 改用正确的 `ref HitStabilityReport` |
| 方法名 `ProcessHitStabilityRequest` 不匹配接口 | 改正为 `ProcessHitStabilityReport`（6 参数签名）|
| `Motor.GroundingStatus.Sliding` 属性不存在 | 移除该属性引用，简化跳跃条件判断 |
| `TryGetComponent(out Motor)` 属性不能做 out | 改用局部变量接收后再赋值给属性 |
| 命名空间 `UnityTimeline.Editor` 与类型冲突 | Editor 脚本改用 `SkillCharacterControllerEditor` 命名空间 |
| FullRootMotion 下角色不转向 | 即使 FullRM 模式也加入输入方向插值（动画位移驱动+输入旋转控制）|

---

## 依赖要求

- Unity **Input System** 包 (`com.unity.inputsystem`)
- **KinematicCharacterController** 包（通过 Package 安装）
- Animator 组件（自动添加，设置 `applyRootMotion = true`）
