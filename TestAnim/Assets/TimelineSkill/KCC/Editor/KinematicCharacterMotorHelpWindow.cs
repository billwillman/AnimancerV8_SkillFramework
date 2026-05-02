#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace KCCHelpWindow
{
    public class KinematicCharacterMotorHelpWindow : EditorWindow
    {
        [MenuItem("Tools/KCC/KinematicCharacterMotor 参数说明")]
        public static void ShowWindow()
        {
            var win = GetWindow<KinematicCharacterMotorHelpWindow>("KCC Motor 参数说明");
            win.minSize = new Vector2(720, 700);
            // 首次打开时设置一个较大的默认尺寸
            win.position = new Rect(win.position.x, win.position.y, 960, 800);
        }

        #region Data

        private struct ParamInfo
        {
            public string category;
            public string name;
            public string type;
            public string defaultValue;
            public string description;
        }

        private static readonly List<ParamInfo> _allParams = new List<ParamInfo>
        {
            // ===== Components =====
            new ParamInfo
            {
                category = "Components",
                name = "Capsule",
                type = "CapsuleCollider",
                defaultValue = "自动获取",
                description = "【作用】角色的胶囊碰撞体引用。\n\n" +
                    "【说明】\nKCC 使用此胶囊体进行所有碰撞检测和移动计算。\n系统会自动获取同 GameObject 上的 CapsuleCollider。\n\n" +
                    "【注意】\n不要手动修改此碰撞体的 radius/height/center，应通过下方 Capsule Settings 参数修改。"
            },

            // ===== Capsule Settings =====
            new ParamInfo
            {
                category = "Capsule Settings",
                name = "CapsuleRadius",
                type = "float",
                defaultValue = "0.5",
                description = "【作用】胶囊碰撞体的半径。\n\n" +
                    "【典型取值】\n• 普通人形：0.3~0.5\n• 较胖角色：0.6~0.8\n• 瘦小角色：0.2~0.3\n\n" +
                    "【调整建议】\n应与角色模型身体宽度匹配。过大会导致角色无法通过窄通道，过小会导致穿模。"
            },
            new ParamInfo
            {
                category = "Capsule Settings",
                name = "CapsuleHeight",
                type = "float",
                defaultValue = "2",
                description = "【作用】胶囊碰撞体的总高度。\n\n" +
                    "【典型取值】\n• 标准人形：1.8~2.0\n• 矮小角色：1.2~1.5\n• 高大角色：2.5~3.0\n\n" +
                    "【调整建议】\n应与角色模型从脚底到头顶的高度匹配。影响角色能否通过低矮通道。"
            },
            new ParamInfo
            {
                category = "Capsule Settings",
                name = "CapsuleYOffset",
                type = "float",
                defaultValue = "1",
                description = "【作用】胶囊碰撞体中心相对角色 Transform 原点的 Y 轴偏移。\n\n" +
                    "【典型取值】\n通常为 CapsuleHeight 的一半（即胶囊中心在角色正中）。\n\n" +
                    "【调整建议】\n如果角色 Transform 在脚底，设为 Height/2。\n如果 Transform 在身体中心，设为 0。"
            },
            new ParamInfo
            {
                category = "Capsule Settings",
                name = "CapsulePhysicsMaterial",
                type = "PhysicsMaterial",
                defaultValue = "None",
                description = "【作用】胶囊碰撞体的物理材质。\n\n" +
                    "【说明】\n不影响角色自身的移动行为，仅影响其他物体与角色碰撞时的物理表现（如弹跳、摩擦）。\n\n" +
                    "【调整建议】\n大多数情况下可留空。如果需要其他物体撞到角色后有特殊弹跳/摩擦效果，可设置对应物理材质。"
            },

            // ===== Grounding Settings =====
            new ParamInfo
            {
                category = "Grounding Settings",
                name = "GroundDetectionExtraDistance",
                type = "float",
                defaultValue = "0",
                description = "【作用】增加地面检测的额外距离范围。\n\n" +
                    "【说明】\n在高速移动时，角色可能在帧间跨过地面检测范围导致\"脱地\"。增加此值可让地面吸附更可靠。\n\n" +
                    "【典型取值】\n• 正常速度：0\n• 高速移动/快速下坡：0.1~0.25\n\n" +
                    "【调整建议】\n如果角色在快速移动时偶尔\"弹起\"或失去地面状态，适当增加此值。"
            },
            new ParamInfo
            {
                category = "Grounding Settings",
                name = "MaxStableSlopeAngle",
                type = "float",
                defaultValue = "60",
                description = "【作用】角色能稳定站立的最大斜坡角度（度）。\n\n" +
                    "【范围】0~89°\n\n" +
                    "【说明】\n超过此角度的斜坡会被视为\"不稳定地面\"，角色会开始滑落。\n\n" +
                    "【典型取值】\n• 严格地形：30~45°\n• 标准设置：50~60°\n• 宽松设置：70~80°\n\n" +
                    "【调整建议】\n根据关卡设计决定。如果场景有很多斜坡且希望角色能攀爬，设高一些。"
            },
            new ParamInfo
            {
                category = "Grounding Settings",
                name = "StableGroundLayers",
                type = "LayerMask",
                defaultValue = "Everything (-1)",
                description = "【作用】定义哪些 Layer 上的碰撞体被视为\"稳定地面\"。\n\n" +
                    "【说明】\n只有在这些 Layer 上的表面才会让角色进入 Grounded 状态。\n\n" +
                    "【调整建议】\n默认为所有层。如果需要某些层（如水面、特效）不被视为地面，取消勾选对应层。"
            },
            new ParamInfo
            {
                category = "Grounding Settings",
                name = "DiscreteCollisionEvents",
                type = "bool",
                defaultValue = "false",
                description = "【作用】启用离散碰撞事件通知。\n\n" +
                    "【说明】\n开启后，KCC 会在检测到离散碰撞时通过 ICharacterController 接口通知控制器。\n\n" +
                    "【调整建议】\n仅在需要监听碰撞事件时开启，有轻微性能开销。"
            },

            // ===== Step Settings =====
            new ParamInfo
            {
                category = "Step Settings",
                name = "StepHandling",
                type = "StepHandlingMethod",
                defaultValue = "Standard",
                description = "【作用】台阶处理方式。\n\n" +
                    "【模式说明】\n• None：不处理台阶，角色会被台阶阻挡\n• Standard：标准台阶检测，能自动上台阶\n• Extra：额外台阶检测，更精确但性能开销更大\n\n" +
                    "【调整建议】\n大多数情况用 Standard 即可。如果场景无台阶可设为 None 节省性能。"
            },
            new ParamInfo
            {
                category = "Step Settings",
                name = "MaxStepHeight",
                type = "float",
                defaultValue = "0.5",
                description = "【作用】角色能自动攀爬的最大台阶高度。\n\n" +
                    "【典型取值】\n• 低台阶：0.2~0.3\n• 标准台阶：0.4~0.5\n• 高台阶：0.6~0.8\n\n" +
                    "【调整建议】\n应与关卡中台阶/门槛的实际高度匹配。设得太高可能导致角色\"跳上\"不合理的表面。"
            },
            new ParamInfo
            {
                category = "Step Settings",
                name = "AllowSteppingWithoutStableGrounding",
                type = "bool",
                defaultValue = "false",
                description = "【作用】角色不在稳定地面时是否仍能攀爬台阶。\n\n" +
                    "【说明】\n默认关闭，意味着角色必须在稳定地面上才能上台阶。开启后即使在空中/不稳定状态也可触发上台阶逻辑。\n\n" +
                    "【调整建议】\n大多数情况保持关闭。特殊玩法需要时才开启。"
            },
            new ParamInfo
            {
                category = "Step Settings",
                name = "MinRequiredStepDepth",
                type = "float",
                defaultValue = "0.1",
                description = "【作用】台阶表面的最小深度要求（用于 Extra 模式）。\n\n" +
                    "【说明】\n台阶顶部平面必须至少有这么深才会被识别为可站立的台阶。用于防止角色站在极窄的边缘上。\n\n" +
                    "【调整建议】\n通常 0.1 即可。如果角色需要站在很窄的平台上，适当减小。"
            },

            // ===== Ledge Settings =====
            new ParamInfo
            {
                category = "Ledge Settings",
                name = "LedgeAndDenivelationHandling",
                type = "bool",
                defaultValue = "true",
                description = "【作用】启用边缘（Ledge）和高低差检测处理。\n\n" +
                    "【说明】\n开启后能正确检测角色站在边缘时的接地状态，防止角色\"半挂\"在悬崖边。\n有一定性能开销。\n\n" +
                    "【调整建议】\n建议保持开启。仅在性能极度紧张且场景无边缘时考虑关闭。"
            },
            new ParamInfo
            {
                category = "Ledge Settings",
                name = "MaxStableDistanceFromLedge",
                type = "float",
                defaultValue = "0.5",
                description = "【作用】角色站在边缘时，距胶囊中心轴多远仍视为稳定。\n\n" +
                    "【说明】\n角色站在悬崖边时，如果脚下支撑点距离中心轴超过此值，角色会失去稳定状态并可能滑落。\n\n" +
                    "【典型取值】\n通常等于或略小于 CapsuleRadius。\n\n" +
                    "【调整建议】\n值越小，角色越容易从边缘滑落。值越大，角色能站得越靠边。"
            },
            new ParamInfo
            {
                category = "Ledge Settings",
                name = "MaxVelocityForLedgeSnap",
                type = "float",
                defaultValue = "0",
                description = "【作用】在边缘处防止地面吸附的速度阈值。\n\n" +
                    "【说明】\n当角色速度超过此值时，不会在经过边缘时被\"吸\"回地面，而是飞出去。\n设为 0 表示始终允许在边缘吸附地面。\n\n" +
                    "【调整建议】\n如果角色快速跑过悬崖边时应该飞出去而非沿地面下滑，设置一个合理速度阈值（如 5~10）。"
            },
            new ParamInfo
            {
                category = "Ledge Settings",
                name = "MaxStableDenivelationAngle",
                type = "float",
                defaultValue = "180",
                description = "【作用】角色能保持地面吸附的最大向下坡度变化角度。\n\n" +
                    "【范围】1~180°\n\n" +
                    "【说明】\n当地面向下的角度变化超过此值时，角色会脱离地面（如走到悬崖边）。\n180° = 永远吸附地面。\n\n" +
                    "【调整建议】\n如果希望角色走到陡坡边缘时自然飞出，设为较小值（如 60~90°）。"
            },

            // ===== Rigidbody Interaction Settings =====
            new ParamInfo
            {
                category = "Rigidbody Interaction",
                name = "InteractiveRigidbodyHandling",
                type = "bool",
                defaultValue = "true",
                description = "【作用】启用与刚体和 PhysicsMover 的交互处理。\n\n" +
                    "【说明】\n开启后角色能：\n• 被 PhysicsMover（移动平台）正确携带\n• 推动动态刚体\n• 站在动态刚体上\n\n" +
                    "【调整建议】\n如果场景无移动平台和可推动物体，可关闭以节省性能。"
            },
            new ParamInfo
            {
                category = "Rigidbody Interaction",
                name = "RigidbodyInteractionType",
                type = "RigidbodyInteractionType",
                defaultValue = "Kinematic",
                description = "【作用】角色与非运动学刚体的交互方式。\n\n" +
                    "【模式说明】\n• Kinematic：角色作为运动学体推动其他刚体，不受反作用力\n• SimulatedDynamic：模拟动态刚体行为，角色会受到推力影响\n\n" +
                    "【调整建议】\n大多数情况用 Kinematic。如果希望角色被物体推动（如被巨石撞飞），用 SimulatedDynamic。"
            },
            new ParamInfo
            {
                category = "Rigidbody Interaction",
                name = "SimulatedCharacterMass",
                type = "float",
                defaultValue = "1",
                description = "【作用】角色推动其他刚体时使用的质量值。\n\n" +
                    "【说明】\n决定角色推动物体的\"力度\"。质量越大推力越强。\n\n" +
                    "【典型取值】\n• 轻量角色：0.5~1\n• 标准角色：1~2\n• 重型角色：5~10\n\n" +
                    "【调整建议】\n根据游戏设计决定角色的\"重量感\"。"
            },
            new ParamInfo
            {
                category = "Rigidbody Interaction",
                name = "PreserveAttachedRigidbodyMomentum",
                type = "bool",
                defaultValue = "true",
                description = "【作用】角色离开移动平台时是否保留平台的动量。\n\n" +
                    "【说明】\n开启后，角色从移动平台跳下时会继承平台的速度方向和大小，物理感更真实。\n关闭后，离开平台的瞬间会丢失平台速度。\n\n" +
                    "【调整建议】\n建议保持开启，否则从移动平台跳下会感觉\"突然停住\"。"
            },

            // ===== Constraints Settings =====
            new ParamInfo
            {
                category = "Constraints",
                name = "HasPlanarConstraint",
                type = "bool",
                defaultValue = "false",
                description = "【作用】是否启用平面约束。\n\n" +
                    "【说明】\n开启后角色的移动会被约束在指定平面上，用于制作 2D 横版游戏或固定轨道移动。\n\n" +
                    "【调整建议】\n3D 游戏保持关闭。2D 横版或固定视角游戏开启并设置约束轴。"
            },
            new ParamInfo
            {
                category = "Constraints",
                name = "PlanarConstraintAxis",
                type = "Vector3",
                defaultValue = "(0, 0, 1)",
                description = "【作用】定义平面约束的法线方向。\n\n" +
                    "【说明】\n角色的移动会被约束在垂直于此向量的平面上。\n• (0,0,1) = XY 平面（标准 2D 横版）\n• (1,0,0) = YZ 平面\n\n" +
                    "【调整建议】\n2D 横版游戏通常设为 (0,0,1) 即 Forward 方向。"
            },

            // ===== Other Settings =====
            new ParamInfo
            {
                category = "Other Settings",
                name = "MaxMovementIterations",
                type = "int",
                defaultValue = "5",
                description = "【作用】每次更新中移动 Sweep 的最大迭代次数。\n\n" +
                    "【说明】\n角色碰到障碍后会沿表面滑动并重新检测，此值限制最大重试次数。\n\n" +
                    "【典型取值】\n• 简单场景：3\n• 标准：5\n• 复杂碰撞环境：8~10\n\n" +
                    "【调整建议】\n值越大碰撞处理越精确但性能开销越高。5 对大多数场景足够。"
            },
            new ParamInfo
            {
                category = "Other Settings",
                name = "MaxDecollisionIterations",
                type = "int",
                defaultValue = "1",
                description = "【作用】每次更新中去重叠（Decollision）的最大迭代次数。\n\n" +
                    "【说明】\n当角色与其他碰撞体重叠时，系统尝试将其\"推出\"的最大重试次数。\n\n" +
                    "【调整建议】\n1 通常足够。如果角色经常被卡进物体中，可增加到 2~3。"
            },
            new ParamInfo
            {
                category = "Other Settings",
                name = "CheckMovementInitialOverlaps",
                type = "bool",
                defaultValue = "true",
                description = "【作用】移动前检查初始重叠状态。\n\n" +
                    "【说明】\n确保即使角色已经与几何体相交（穿模），也能检测到碰撞并修正。\n防止角色穿过碰撞体。\n\n" +
                    "【调整建议】\n建议保持开启。仅在极度追求性能且确保无穿模风险时才关闭。"
            },
            new ParamInfo
            {
                category = "Other Settings",
                name = "KillVelocityWhenExceedMaxMovementIterations",
                type = "bool",
                defaultValue = "true",
                description = "【作用】超过最大移动迭代次数时是否清零速度。\n\n" +
                    "【说明】\n当移动计算达到迭代上限时，将角色当前速度设为零。\n防止角色在复杂碰撞中持续积累不合理的速度。\n\n" +
                    "【调整建议】\n建议保持开启，避免卡墙时速度异常。"
            },
            new ParamInfo
            {
                category = "Other Settings",
                name = "KillRemainingMovementWhenExceedMaxMovementIterations",
                type = "bool",
                defaultValue = "true",
                description = "【作用】超过最大移动迭代次数时是否丢弃剩余移动量。\n\n" +
                    "【说明】\n当迭代达到上限时，放弃本帧尚未完成的移动距离。\n配合上一个选项使用，防止移动累积导致下一帧\"弹射\"。\n\n" +
                    "【调整建议】\n建议保持开启。"
            },

            // ===== Runtime Properties =====
            new ParamInfo
            {
                category = "运行时属性（只读）",
                name = "GroundingStatus",
                type = "CharacterGroundingReport",
                defaultValue = "—",
                description = "【作用】当前帧的接地状态报告。\n\n" +
                    "【包含信息】\n• IsStableOnGround：是否稳定站在地面上\n• FoundAnyGround：是否检测到任何地面\n• GroundNormal：地面法线方向\n• InnerGroundNormal / OuterGroundNormal：内外地面法线\n• GroundCollider / GroundPoint：碰撞体和接触点\n\n" +
                    "【用途】\n在 ICharacterController 回调中判断角色是否着地、地面角度等。"
            },
            new ParamInfo
            {
                category = "运行时属性（只读）",
                name = "BaseVelocity",
                type = "Vector3",
                defaultValue = "—",
                description = "【作用】角色自身的移动速度（不含附着刚体速度）。\n\n" +
                    "【说明】\n通过 UpdateVelocity 回调中设置 ref velocity 来控制。\n这是角色\"主动\"移动的速度。\n\n" +
                    "【与 Velocity 的区别】\nVelocity = BaseVelocity + AttachedRigidbodyVelocity（总速度）。"
            },
            new ParamInfo
            {
                category = "运行时属性（只读）",
                name = "AttachedRigidbodyVelocity",
                type = "Vector3",
                defaultValue = "—",
                description = "【作用】角色因站在刚体/移动平台上而获得的附加速度。\n\n" +
                    "【说明】\n当角色站在 PhysicsMover 或动态刚体上时，该速度反映平台的移动速度。\n\n" +
                    "【用途】\n用于判断角色的\"真实\"对地速度。"
            },
            new ParamInfo
            {
                category = "运行时属性（只读）",
                name = "Velocity",
                type = "Vector3",
                defaultValue = "—",
                description = "【作用】角色的总速度 = BaseVelocity + AttachedRigidbodyVelocity。\n\n" +
                    "【说明】\n反映角色在世界空间中的实际移动速度。\n用于 UI 显示、音效强度计算等。"
            },
            new ParamInfo
            {
                category = "运行时属性（只读）",
                name = "CollidableLayers",
                type = "LayerMask",
                defaultValue = "Everything (-1)",
                description = "【作用】指定角色移动算法能检测碰撞的层。\n\n" +
                    "【说明】\n与 StableGroundLayers 不同，这个控制的是\"碰撞检测\"而非\"接地判定\"。\n不在此 LayerMask 中的碰撞体对角色完全透明。\n\n" +
                    "【用途】\n运行时动态修改可实现穿墙、幽灵状态等效果。"
            },
            new ParamInfo
            {
                category = "运行时属性（只读）",
                name = "CharacterUp",
                type = "Vector3",
                defaultValue = "—",
                description = "【作用】角色当前的\"上\"方向。\n\n" +
                    "【说明】\n通常为 (0,1,0)，但如果角色旋转了则会相应变化。\n所有地面检测和重力方向都基于此向量。"
            },
            new ParamInfo
            {
                category = "运行时属性（只读）",
                name = "TransientPosition",
                type = "Vector3",
                defaultValue = "—",
                description = "【作用】角色在移动计算过程中的目标位置。\n\n" +
                    "【说明】\n在 UpdateVelocity/AfterCharacterUpdate 等回调中可读取。\n代表角色在本帧移动计算完成后将到达的位置。"
            },
            new ParamInfo
            {
                category = "运行时属性（只读）",
                name = "TransientRotation",
                type = "Quaternion",
                defaultValue = "—",
                description = "【作用】角色在移动计算过程中的目标旋转。\n\n" +
                    "【说明】\n在 UpdateRotation 回调中通过 ref currentRotation 来修改。\n代表角色在本帧旋转计算完成后的朝向。"
            },
        };

        #endregion

        #region GUI State

        private string _searchText = "";
        private Vector2 _scrollPos;
        private int _selectedCategory = 0;

        private static readonly string[] _categories = new string[]
        {
            "全部",
            "Components",
            "Capsule Settings",
            "Grounding Settings",
            "Step Settings",
            "Ledge Settings",
            "Rigidbody Interaction",
            "Constraints",
            "Other Settings",
            "运行时属性（只读）"
        };

        #endregion

        private void OnGUI()
        {
            // Title
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("KinematicCharacterMotor 参数参考手册",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 14, alignment = TextAnchor.MiddleCenter });
            EditorGUILayout.Space(2);
            EditorGUILayout.LabelField("支持模糊搜索参数名，中文显示参数作用与调整建议",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 10 });
            EditorGUILayout.Space(6);

            // Search bar
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("搜索:", GUILayout.Width(36));
            _searchText = EditorGUILayout.TextField(_searchText, EditorStyles.toolbarSearchField);
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(22)))
                _searchText = "";
            EditorGUILayout.EndHorizontal();

            // Category tabs
            EditorGUILayout.Space(2);
            _selectedCategory = GUILayout.Toolbar(_selectedCategory, _categories,
                EditorStyles.toolbarButton, GUILayout.Height(20));
            EditorGUILayout.Space(4);

            // Filter
            var filtered = GetFilteredParams();

            // Results count
            EditorGUILayout.LabelField($"  显示 {filtered.Count} / {_allParams.Count} 个参数",
                EditorStyles.miniLabel);
            EditorGUILayout.Space(2);

            // Scroll area
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);

            if (filtered.Count == 0)
            {
                EditorGUILayout.HelpBox("未找到匹配的参数。请调整搜索关键词或分类。", MessageType.Info);
            }
            else
            {
                string lastCategory = "";
                foreach (var p in filtered)
                {
                    // Category header
                    if (p.category != lastCategory)
                    {
                        lastCategory = p.category;
                        EditorGUILayout.Space(6);
                        var headerRect = GUILayoutUtility.GetRect(1, 20);
                        EditorGUI.DrawRect(headerRect, new Color(0.2f, 0.4f, 0.6f, 0.2f));
                        EditorGUI.LabelField(headerRect, "  ■ " + p.category,
                            new GUIStyle(EditorStyles.boldLabel) { fontSize = 11 });
                        EditorGUILayout.Space(2);
                    }

                    // Parameter card
                    DrawParamCard(p);
                }
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.EndScrollView();
        }

        private List<ParamInfo> GetFilteredParams()
        {
            var result = new List<ParamInfo>();
            string search = _searchText.Trim().ToLowerInvariant();
            string categoryFilter = _selectedCategory == 0 ? null : _categories[_selectedCategory];

            foreach (var p in _allParams)
            {
                // Category filter
                if (categoryFilter != null && p.category != categoryFilter)
                    continue;

                // Search filter (fuzzy: check if all chars in search appear in order in name)
                if (!string.IsNullOrEmpty(search))
                {
                    if (!FuzzyMatch(p.name.ToLowerInvariant(), search) &&
                        !p.description.ToLowerInvariant().Contains(search) &&
                        !p.category.ToLowerInvariant().Contains(search))
                        continue;
                }

                result.Add(p);
            }
            return result;
        }

        private static bool FuzzyMatch(string source, string pattern)
        {
            int patternIdx = 0;
            for (int i = 0; i < source.Length && patternIdx < pattern.Length; i++)
            {
                if (source[i] == pattern[patternIdx])
                    patternIdx++;
            }
            return patternIdx == pattern.Length;
        }

        private void DrawParamCard(ParamInfo p)
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                // Header line: name + type + default
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label(p.name, new GUIStyle(EditorStyles.boldLabel) { fontSize = 12 });
                GUILayout.FlexibleSpace();
                GUILayout.Label($"[{p.type}]",
                    new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.4f, 0.7f, 0.9f) } });
                if (p.defaultValue != "—")
                {
                    GUILayout.Label($"默认: {p.defaultValue}",
                        new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.7f, 0.7f, 0.4f) } });
                }
                EditorGUILayout.EndHorizontal();

                // Description
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField(p.description,
                    new GUIStyle(EditorStyles.wordWrappedLabel) { fontSize = 11, richText = true });
            }
            EditorGUILayout.Space(2);
        }
    }
}
#endif
