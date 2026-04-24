# SkillCharacterController

基于 **KinematicCharacterController (KCC)** + **Unity InputSystem** + **RootMotion 补偿** 的一体化角色控制器。

## 文件结构

```
Assets/TimelineSkill/KCC/
├── SkillCharacterController.cs              ← 控制器本体
├── SkillCharacterControllerEditor.cs        ← 自定义 Inspector 编辑器
└── Editor/
    └── SkillCharacterControllerEditor.cs    ← (同上，Editor 目录)
```

## 快速开始

1. 创建 GameObject → 添加组件 **Skill Character Controller**
2. Motor 和 Animator 会**自动挂载并配置**
3. Inspector 中配置：
   - **Move Action**: Input Action Asset 的移动 Vector2 Action
   - **Jump Action**: Input Action Asset 的跳跃 Button Action
   - **Orientation Reference**: 相机 GameObject（可选）
4. 调整 Ground/Air RootMotion 模式和 Jump 模式

## 公共 API

```csharp
// 补偿（类似 TimelineRedirectRootMotion）
controller.SetCompensationPosition(position);
controller.SetCompensationRotation(eulerAngles);
controller.SetCompensation(position, rotationEuler);
controller.ClearCompensation();
controller.CompensationEnabled = true/false;

// 输入
controller.SetInputs(inputs);
```

## Inspector 分组

| 分组 | 颜色 | 说明 |
|------|------|------|
| Input Settings | 蓝 | InputActionReference 拖拽字段 |
| Root Motion Mode | 紫 | Ground/Air 独立 Full/Ignore 模式 |
| Jump Settings | 绿 | BuiltIn(含参数) / ExternalControlled |
| Stable Movement | 橙 | IgnoreRM 时地面运动参数 |
| Air Movement | 橙 | IgnoreRM 时空中运动参数 |
| Misc | 灰 | 重力、模型、忽略碰撞体 |
| Compensation Debug | 红 | 运行时补偿状态 |
