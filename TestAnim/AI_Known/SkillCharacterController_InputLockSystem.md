# SkillCharacterController 输入锁定系统 (Input Lock System)

> 实现文件：`Assets/TimelineSkill/KCC/SkillCharacterController.cs`  
> 实现日期：2026-04-27

## 概述

为 `SkillCharacterController` 提供三合一输入禁止功能，支持不同粒度的输入控制需求：
- **方案A**：简单布尔开关（门面API，内部调用方案B的 `All`）
- **方案B**：位掩码分级控制（Movement / Jump 独立锁定）
- **方案C**：多标记系统（key-value 字典，支持多持有者叠加）

三套 API 共享同一底层存储：`Dictionary<string, InputLockFlags>`。

---

## 枚举定义

```csharp
/// <summary>输入锁定标志位，支持分级控制</summary>
[System.Flags]
public enum InputLockFlags
{
    None     = 0,
    Movement = 1 << 0,  // 锁定移动输入 (MoveAxisForward / MoveAxisRight)
    Jump     = 1 << 1,  // 锁定跳跃输入 (JumpDown)
    All      = Movement | Jump,
}
```

---

## 底层存储

```csharp
private const string kBuiltinLockKey = "__builtin__";
private readonly Dictionary<string, InputLockFlags> _inputLockSources = new();
```

- **key**：持有者标识字符串（如 `"SkillPlay"`, `"Dialogue"`, `"Cutscene"`）
- **value**：该持有者要求的锁标志位（支持 OR 组合）
- 多个 key 的锁通过 **OR 聚合** 生效 —— 所有 key 的锁都释放后输入才真正恢复

---

## API 详解

### 方案 A — 布尔开关（门面）

| API | 类型 | 说明 |
|-----|------|------|
| `InputEnabled` | `bool` 属性 | `false`=全部锁定, `true`=解除内置锁 |

**内部实现**：操作内置 key (`__builtin__`) 的 `All` 标志。

```csharp
// 使用示例
controller.InputEnabled = false;   // 技能播放期间全锁
controller.InputEnabled = true;    // 结束后恢复
```

> **注意**：此属性仅操作 builtin key。如果有其他 key 持有的锁，即使 `InputEnabled=true` 输入仍可能被锁定。需完全解锁请使用 `ClearAllInputLocks()` 或逐个 `RemoveInputLock(key)`。

### 方案 B — 位掩码分级控制

| API | 返回值/参数 | 说明 |
|-----|------------|------|
| `SetInputLock(InputLockFlags flags)` | void | 直接设置锁标志（覆盖式，仅影响 builtin key） |
| `IsInputLocked()` | bool | 查询当前是否有任何锁定 |
| `IsInputLocked(InputLockFlags flags)` | bool | 查询指定通道是否被锁定 |

```csharp
// 只锁移动，允许跳跃取消
controller.SetInputLock(InputLockFlags.Movement);
controller.SetInputLock(InputLockFlags.None);  // 解锁

// 查询状态
if (controller.IsInputLocked(InputLockFlags.Jump))
    Debug.Log("跳跃被锁定");
```

### 方案 C — 多标记系统

| API | 参数 | 说明 |
|-----|------|------|
| `AddInputLock(string key, InputLockFlags flags)` | key + flags | 为指定 key 添加/覆盖锁 |
| `RemoveInputLock(string key)` | key | 移除指定 key 的所有锁 |
| `HasInputLockKey(string key)` | key → bool | 检查某 key 是否当前持锁 |
| `ClearAllInputLocks()` | 无 | 清除所有来源的锁（强制恢复） |

```csharp
// 多系统叠加场景
controller.AddInputLock("SkillPlay", InputLockFlags.All);
controller.AddInputLock("Dialogue", InputLockFlags.Movement);

controller.RemoveInputLock("SkillPlay");       // Dialogue 的锁仍生效！
controller.RemoveInputLock("Dialogue");        // 现在才真正恢复
```

---

## ReadInput 改动

`ReadInput()` 方法在读取每个输入通道前检查锁状态：

```
Movement 被锁 → 跳过 _moveAction 读取 → MoveAxisForward/Right 保持 0
Jump 被锁     → 跳过 _jumpAction 读取 → JumpDown 保持 false
CameraRotation → 不受锁影响            → 朝向参照始终有效
```

核心逻辑：

```csharp
private void ReadInput()
{
    _inputs = default;
    InputLockFlags locks = GetEffectiveLocks();

    // 移动输入（受 Movement 锁控制）
    if ((locks & InputLockFlags.Movement) == 0)
    {
        // 正常读取 MoveAxisForward / MoveAxisRight ...
    }

    // 跳跃输入（受 Jump 锁控制）
    if ((locks & InputLockFlags.Jump) == 0)
    {
        // 正常读取 JumpDown ...
    }

    // CameraRotation 不受锁影响
}
```

---

## 内部方法

```csharp
/// <summary>
/// 聚合所有 key 的 flags，返回当前实际生效的总锁。
/// </summary>
private InputLockFlags GetEffectiveLocks()
{
    InputLockFlags result = InputLockFlags.None;
    foreach (var kvp in _inputLockSources)
        result |= kvp.Value;
    return result;
}
```

---

## 典型使用场景

| 场景 | 推荐方案 | 代码示例 |
|------|---------|---------|
| 技能播放期间完全锁住操作 | 方案A | `controller.InputEnabled = false;` |
| 对话时锁移动但允许跳过 | 方案B | `SetInputLock(Movement)` |
| 过场+对话+技能叠加 | 方案C | 各自用独立 key 的 `AddInputLock` |
| 某些技能锁定移动但不锁取消键 | 方案B | `AddInputLock("Roll", Movement)` |
| 强制恢复（如角色死亡后重生） | - | `ClearAllInputLocks()` |

---

## 设计决策记录

1. **CameraRotation 不受锁控制**：朝向参照物是外部系统（相机等）提供的方向基准，与玩家按键输入无关，不应被锁定。
2. **方案A 使用 builtin key 而非单独字段**：这样方案A和方案C可以共存且不冲突，`InputEnabled=true` 不会意外清除其他系统的锁。
3. **字典而非引用计数**：用户明确要求多标记设计（方案C改进），每个 holder 有独立标识符，更易调试和排查。
4. **新增字段为运行时非序列化**：`_inputLockSources` 是普通 Dictionary，不参与 Unity 序列化，每次运行从空开始。
5. **可扩展性**：未来新增输入类型（如 Roll、Dash、Skill 键）只需在枚举中加新的 Flags 值，并在 ReadInput 中加对应的分支判断。
