# Input Lock 树节点 (Input Lock Tree Nodes)

> 实现文件：见下方各节点路径  
> 实现日期：2026-04-27  
> 依赖：[SkillCharacterController 输入锁定系统](./SkillCharacterController_InputLockSystem.md)

## 概述

为 **AnimancerAbilityTree** 和 **UnityTimelineTree** 两套行为树系统各创建 3 个输入锁定控制节点，封装 `SkillCharacterController` 的方案 C 多标记 API（`AddInputLock` / `RemoveInputLock` / `ClearAllInputLocks`），共 **6 个新节点**。

所有节点均为**即执即毕模式**（只重写 `DoAction()`，不重写 `OnUpdate()`），执行完立即返回 Success。

---

## 基类改动

### AnimancerAbility.cs — 新增 SkillCharacterController 懒缓存属性

```csharp
/// <summary>
/// 缓存的 SkillCharacterController，首次访问时从 User 上 GetComponent 并缓存
/// </summary>
private SkillCharacterController m_SkillController;
public SkillCharacterController SkillCharacterController
{
    get
    {
        if (m_SkillController == null)
        {
            var userComponent = User as UnityEngine.Component;
            if (userComponent != null)
                m_SkillController = userComponent.GetComponent<SkillCharacterController>();
        }
        return m_SkillController;
    }
}
```

同时在 `DisposeTree()` 中清理缓存：

```csharp
public override void DisposeTree()
{
    base.DisposeTree();
    // ... 已有清理 ...
    m_SkillController = null;  // 新增
}
```

### AnimancerAbilityActionNode 基类 — 新增 GetSkillController() 辅助方法

```csharp
// 位于 AnimancerAbility.Nodes.cs
/// <summary>返回 AnimancerAbility 上缓存的 SkillCharacterController</summary>
protected SkillCharacterController GetSkillController() => (Owner as AnimancerAbility)?.SkillCharacterController;
```

---

## UnityTimelineTree 节点

> 文件位置：`Assets/.../Tree/Nodes/Feature/`
> 基类：`UnityTimelineActionNode`
> 访问 Controller 方式：`AbilityLinker?.SkillCharacterController`

### 1. SetInputLockNode

| 属性 | 值 |
|------|-----|
| NodeName | `SetInputLock` |
| NodePath | `UnityTimeline/Action/SetInputLock` |

| Inspector 字段 | 类型 | 默认值 | 说明 |
|----------------|------|--------|------|
| LockKey | string | `"SkillPlay"` | 持有者标识（PropertyPort 输入端口） |
| LockFlags | int | `3 (All)` | 锁定通道: None=0, Movement=1, Jump=2, All=3 |

```csharp
protected override void DoAction()
{
    var controller = AbilityLinker?.SkillCharacterController;
    if (controller == null) return;

    string key = m_LockKey.Value;
    if (!string.IsNullOrEmpty(key))
        controller.AddInputLock(key, (InputLockFlags)m_LockFlags.Value);
}
```

### 2. RemoveInputLockNode

| 属性 | 值 |
|------|-----|
| NodeName | `RemoveInputLock` |
| NodePath | `UnityTimeline/Action/RemoveInputLock` |

| Inspector 字段 | 类型 | 默认值 | 说明 |
|----------------|------|--------|------|
| LockKey | string | `"SkillPlay"` | 要移除的持有者标识 |

```csharp
protected override void DoAction()
{
    var controller = AbilityLinker?.SkillCharacterController;
    if (controller == null) return;

    string key = m_LockKey.Value;
    if (!string.IsNullOrEmpty(key))
        controller.RemoveInputLock(key);
}
```

### 3. ClearAllInputLocksNode

| 属性 | 值 |
|------|-----|
| NodeName | `ClearAllInputLocks` |
| NodePath | `UnityTimeline/Action/ClearAllInputLocks` |

无 Inspector 配置字段。

```csharp
protected override void DoAction()
{
    var controller = AbilityLinker?.SkillCharacterController;
    if (controller == null) return;

    controller.ClearAllInputLocks();
}
```

---

## AnimancerAbilityTree 节点

> 文件位置：`Assets/.../Ability/Nodes/`
> 基类：`AnimancerAbilityActionNode`
> 访问 Controller 方式：`GetSkillController()` → 走 Owner → AnimancerAbility.SkillCharacterController 懒缓存

### 4. AA_SetInputLockNode

| 属性 | 值 |
|------|-----|
| NodeName | `SetInputLock` |
| NodePath | `AnimancerAbility/Action/SetInputLock` |

Inspector 字段与 UT 版完全一致（LockKey + LockFlags）。

```csharp
protected override void DoAction()
{
    var controller = GetSkillController();  // ★ 通过辅助方法访问
    if (controller == null) return;

    string key = m_LockKey.Value;
    if (!string.IsNullOrEmpty(key))
        controller.AddInputLock(key, (InputLockFlags)m_LockFlags.Value);
}
```

### 5. AA_RemoveInputLockNode

| 属性 | 值 |
|------|-----|
| NodeName | `RemoveInputLock` |
| NodePath | `AnimancerAbility/Action/RemoveInputLock` |

Inspector 字段与 UT 版一致（LockKey）。

```csharp
protected override void DoAction()
{
    var controller = GetSkillController();
    if (controller == null) return;

    string key = m_LockKey.Value;
    if (!string.IsNullOrEmpty(key))
        controller.RemoveInputLock(key);
}
```

### 6. AA_ClearAllInputLocksNode

| 属性 | 值 |
|------|-----|
| NodeName | `ClearAllInputLocks` |
| NodePath | `AnimancerAbility/Action/ClearAllInputLocks` |

无配置字段。

```csharp
protected override void DoAction()
{
    var controller = GetSkillController();
    if (controller == null) return;

    controller.ClearAllInputLocks();
}
```

---

## 文件清单

```
Assets/TimelineSkill/Core/UnityTimeline/
├── Ability/
│   ├── AnimancerAbility.cs                          ← 新增 SkillCharacterController 属性
│   ├── AnimancerAbility.Nodes.cs                    ← 新增 GetSkillController()
│   └── Nodes/
│       ├── AA_SetInputLockNode.cs                   [NEW]
│       ├── AA_RemoveInputLockNode.cs                [NEW]
│       └── AA_ClearAllInputLocksNode.cs             [NEW]
└── Tree/Nodes/Feature/
    ├── SetInputLockNode.cs                          [NEW]
    ├── RemoveInputLockNode.cs                       [NEW]
    └── ClearAllInputLocksNode.cs                    [NEW]
```

---

## 使用示例（树编辑器中）

### 场景一：技能播放期间锁定全部输入

```
技能开始
  └─→ [SetInputLock Key="Attack" Flags=3(All)]
      └─→ [PlayAnimacerTimeline] (播放攻击动画)
          └─→ [RemoveInputLock Key="Attack"]
              └─→ 技能结束
```

### 场景二：只锁移动不锁跳跃（翻滚类技能）

```
翻滚开始
  └─→ [SetInputLock Key="Roll" Flags=1(Movement)]
      └─→ [PlayAnimacerTimeline] (播放翻滚动画)
          └─→ [RemoveInputLock Key="Roll"]
              └─→ 翻滚结束
```

### 场景三：强制解锁（角色死亡重生）

任意树位置：
```
  └─→ [ClearAllInputLocks]
      └─→ 后续逻辑...
```

---

## 设计说明

| 决策项 | 选择 | 原因 |
|--------|------|------|
| 封装的 API 方案 | **方案 C**（多标记 key-value） | 粒度最灵活，支持多持有者叠加；方案 A/B 可通过相同模式自行实现 |
| 节点执行模式 | **即执即毕**（仅 DoAction） | 与 AddForceNode、StopAnimacerNode 等控制性节点语义一致 |
| LockFlags 字段类型 | **int PropertyPort** | 行为树编辑器对枚举类型支持有限，使用 int + Tooltip 注释替代 |
| 默认 Key 值 | **"SkillPlay"** | 符合最常见场景（技能播放锁定） |
| AA_ 前缀命名 | **是** | 避免两套树的节点在编辑器搜索时混淆 |
| Controller 访问方式 | UT 用 `AbilityLinker`，AA 用 `GetSkillController()` | 各自遵循本系统的已有惯例，UT 通过 AbilityLinker 直连，AA 通过 Owner 链懒缓存 |
