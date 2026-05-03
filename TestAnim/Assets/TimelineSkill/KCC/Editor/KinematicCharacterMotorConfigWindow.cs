#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace KCCEditorTool
{
    /// <summary>
    /// KCC Motor 配置工具窗口 — 对 KinematicCharacterMotor 的全部参数进行中文解释、可视化编辑、预设配置
    ///
    /// 核心特性：
    ///   - 缓冲编辑模式：修改只影响窗口缓存，需点"应用"才写入组件，"回退"丢弃未保存修改
    ///   - 预设系统：提供多种典型角色配置一键填充（鼠标悬停显示 Tips）
    ///   - 中文标注：每个属性带中文标签 + tooltip + ? 帮助按钮
    ///   - 运行时监控：PlayMode 下实时显示 Velocity/GroundingStatus 等运行时属性
    ///
    /// 菜单: Tools/KCC/Motor Config Tool
    /// </summary>
    public class KinematicCharacterMotorConfigWindow : EditorWindow
    {
        #region Presets

        public struct MotorPreset
        {
            public string Name;
            public string Description;
            public Color AccentColor;

            // === Capsule Settings ===
            public float CapsuleRadius;
            public float CapsuleHeight;
            public float CapsuleYOffset;

            // === Grounding Settings ===
            public float GroundDetectionExtraDistance;
            public float MaxStableSlopeAngle;

            // === Step Settings ===
            public float MaxStepHeight;

            // === Ledge Settings ===
            public float MaxStableDistanceFromLedge;
            public float MaxVelocityForLedgeSnap;
            public float MaxStableDenivelationAngle;

            // === Rigidbody Interaction ===
            public bool InteractiveRigidbodyHandling;
            public float SimulatedCharacterMass;
            public bool PreserveAttachedRigidbodyMomentum;

            // === Other Settings ===
            public int MaxMovementIterations;
            public int MaxDecollisionIterations;

            public static readonly MotorPreset[] All = new[]
            {
                new MotorPreset
                {
                    Name = "标准人形",
                    Description = "标准人形角色默认配置。适合大多数第三人称/第一人称游戏。\n胶囊半径0.4，高度1.8，标准斜坡角60°。",
                    AccentColor = new Color(0.25f, 0.55f, 0.85f),
                    CapsuleRadius = 0.4f, CapsuleHeight = 1.8f, CapsuleYOffset = 0.9f,
                    GroundDetectionExtraDistance = 0f, MaxStableSlopeAngle = 60f,
                    MaxStepHeight = 0.5f,
                    MaxStableDistanceFromLedge = 0.5f, MaxVelocityForLedgeSnap = 0f, MaxStableDenivelationAngle = 180f,
                    InteractiveRigidbodyHandling = true, SimulatedCharacterMass = 1f, PreserveAttachedRigidbodyMomentum = true,
                    MaxMovementIterations = 5, MaxDecollisionIterations = 1,
                },
                new MotorPreset
                {
                    Name = "矮胖角色",
                    Description = "矮胖体型角色。宽大的胶囊体(半径0.6)，较低高度(1.4)。\n适合矮壮敌人、Q版角色或特殊体型NPC。",
                    AccentColor = new Color(0.85f, 0.55f, 0.20f),
                    CapsuleRadius = 0.6f, CapsuleHeight = 1.4f, CapsuleYOffset = 0.7f,
                    GroundDetectionExtraDistance = 0f, MaxStableSlopeAngle = 55f,
                    MaxStepHeight = 0.35f,
                    MaxStableDistanceFromLedge = 0.45f, MaxVelocityForLedgeSnap = 0f, MaxStableDenivelationAngle = 180f,
                    InteractiveRigidbodyHandling = true, SimulatedCharacterMass = 3f, PreserveAttachedRigidbodyMomentum = true,
                    MaxMovementIterations = 5, MaxDecollisionIterations = 1,
                },
                new MotorPreset
                {
                    Name = "高速移动",
                    Description = "高速移动型角色配置。更大的地面检测范围、更宽松的斜坡角和迭代次数。\n适合赛车、竞速、高速动作游戏。",
                    AccentColor = new Color(0.90f, 0.30f, 0.30f),
                    CapsuleRadius = 0.35f, CapsuleHeight = 1.8f, CapsuleYOffset = 0.9f,
                    GroundDetectionExtraDistance = 0.2f, MaxStableSlopeAngle = 75f,
                    MaxStepHeight = 0.6f,
                    MaxStableDistanceFromLedge = 0.6f, MaxVelocityForLedgeSnap = 10f, MaxStableDenivelationAngle = 90f,
                    InteractiveRigidbodyHandling = true, SimulatedCharacterMass = 1.5f, PreserveAttachedRigidbodyMomentum = true,
                    MaxMovementIterations = 8, MaxDecollisionIterations = 2,
                },
                new MotorPreset
                {
                    Name = "平台跳跃",
                    Description = "平台跳跃游戏专用配置。精确的边缘检测、适中的台阶高度、敏捷参数。\n适合 Mario 式 / 银河战士式平台游戏。",
                    AccentColor = new Color(0.95f, 0.60f, 0.10f),
                    CapsuleRadius = 0.3f, CapsuleHeight = 1.6f, CapsuleYOffset = 0.8f,
                    GroundDetectionExtraDistance = 0.05f, MaxStableSlopeAngle = 65f,
                    MaxStepHeight = 0.4f,
                    MaxStableDistanceFromLedge = 0.3f, MaxVelocityForLedgeSnap = 8f, MaxStableDenivelationAngle = 120f,
                    InteractiveRigidbodyHandling = true, SimulatedCharacterMass = 1f, PreserveAttachedRigidbodyMomentum = true,
                    MaxMovementIterations = 6, MaxDecollisionIterations = 1,
                },
                new MotorPreset
                {
                    Name = "重型坦克",
                    Description = "重型/坦克型角色。大质量、高迭代、稳定优先。\n适合 Boss 角色、载具、重型机甲。",
                    AccentColor = new Color(0.50f, 0.40f, 0.30f),
                    CapsuleRadius = 0.7f, CapsuleHeight = 2.2f, CapsuleYOffset = 1.1f,
                    GroundDetectionExtraDistance = 0.1f, MaxStableSlopeAngle = 50f,
                    MaxStepHeight = 0.8f,
                    MaxStableDistanceFromLedge = 0.7f, MaxVelocityForLedgeSnap = 0f, MaxStableDenivelationAngle = 180f,
                    InteractiveRigidbodyHandling = true, SimulatedCharacterMass = 10f, PreserveAttachedRigidbodyMomentum = true,
                    MaxMovementIterations = 10, MaxDecollisionIterations = 3,
                },
                new MotorPreset
                {
                    Name = "敏捷刺客",
                    Description = "轻型敏捷角色。小胶囊体、低质量、高灵敏度。\n适合忍者、刺客、快速反应类角色。",
                    AccentColor = new Color(0.40f, 0.70f, 0.40f),
                    CapsuleRadius = 0.25f, CapsuleHeight = 1.7f, CapsuleYOffset = 0.85f,
                    GroundDetectionExtraDistance = 0.05f, MaxStableSlopeAngle = 70f,
                    MaxStepHeight = 0.45f,
                    MaxStableDistanceFromLedge = 0.35f, MaxVelocityForLedgeSnap = 12f, MaxStableDenivelationAngle = 100f,
                    InteractiveRigidbodyHandling = true, SimulatedCharacterMass = 0.5f, PreserveAttachedRigidbodyMomentum = true,
                    MaxMovementIterations = 7, MaxDecollisionIterations = 1,
                },
            };
        }

        #endregion

        #region State

        private GameObject m_TargetObj;
        private KinematicCharacterController.KinematicCharacterMotor m_Motor;
        private SerializedObject m_MotorSO;
        private Vector2 m_ScrollPos;
        private int m_SelectedPresetIndex = -1;

        // Foldout states
        private bool m_PresetFoldout = true;
        private bool m_ComponentsFoldout = true;
        private bool m_CapsuleFoldout = true;
        private bool m_GroundingFoldout = true;
        private bool m_StepFoldout = true;
        private bool m_LedgeFoldout = true;
        private bool m_RigidbodyFoldout = true;
        private bool m_ConstraintsFoldout = true;
        private bool m_OtherFoldout = false;
        private bool m_RuntimeFoldout = true;

        #endregion

        #region Styles

        private GUIStyle m_HeaderStyle;
        private GUIStyle m_SubLabelStyle;
        private GUIStyle m_PresetBtnStyle;
        private GUIStyle m_PresetDescStyle;

        private static readonly Color C_HEADER_BG       = new Color(0.13f, 0.32f, 0.58f, 1f);
        private static readonly Color C_PRESET_BG       = new Color(0.22f, 0.38f, 0.62f, 0.10f);
        private static readonly Color C_COMPONENTS_BG   = new Color(0.55f, 0.38f, 0.15f, 0.10f);
        private static readonly Color C_CAPSULE_BG      = new Color(0.70f, 0.55f, 0.15f, 0.10f);
        private static readonly Color C_GROUNDING_BG   = new Color(0.20f, 0.55f, 0.30f, 0.10f);
        private static readonly Color C_STEP_BG         = new Color(0.20f, 0.45f, 0.52f, 0.10f);
        private static readonly Color C_LEDGE_BG        = new Color(0.48f, 0.28f, 0.58f, 0.10f);
        private static readonly Color C_RIGIDBODY_BG   = new Color(0.58f, 0.28f, 0.22f, 0.10f);
        private static readonly Color C_CONSTRAINTS_BG  = new Color(0.28f, 0.32f, 0.58f, 0.10f);
        private static readonly Color C_OTHER_BG        = new Color(0.42f, 0.42f, 0.42f, 0.10f);
        private static readonly Color C_RUNTIME_BG      = new Color(0.18f, 0.26f, 0.34f, 0.15f);

        private void InitStyles()
        {
            m_HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
                { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            m_SubLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 9 };
            m_PresetBtnStyle = new GUIStyle(EditorStyles.miniButton)
            { fontSize = 11, fontStyle = FontStyle.Bold, padding = new RectOffset(8, 8, 8, 8),
              normal = { textColor = Color.white },
              hover = { textColor = Color.white },
              active = { textColor = new Color(0.9f, 0.9f, 0.95f) } };
            m_PresetDescStyle = new GUIStyle(EditorStyles.label)
            { fontSize = 10, fontStyle = FontStyle.Italic, wordWrap = true,
              padding = new RectOffset(4, 2, 2, 0),
              normal = { textColor = new Color(0.95f, 0.93f, 0.88f) } };
        }

        #endregion

        #region Menu & Open

        [MenuItem("Tools/KCC/Motor Config Tool", false, 201)]
        public static void OpenWindow()
        {
            var window = GetWindow<KinematicCharacterMotorConfigWindow>("KCC Motor 配置");
            window.minSize = new Vector2(420, 520);
            window.maxSize = new Vector2(580, 999);
        }

        #endregion

        #region Lifecycle

        void OnEnable()
        {
            InitStyles();
            Selection.selectionChanged += OnSelectionChange;
        }

        void OnDisable()
        {
            Selection.selectionChanged -= OnSelectionChange;
        }

        void OnSelectionChange() => Repaint();

        void Update()
        {
            if (m_TargetObj == null && Selection.activeGameObject != null)
                TryResolveTarget(Selection.activeGameObject);
            if (EditorApplication.isCompiling)
                Close();
            // PlayMode 时持续 Repaint 以刷新运行时属性
            if (Application.isPlaying && m_Motor != null)
                Repaint();
        }

        #endregion

        #region Main GUI

        void OnGUI()
        {
            if (m_HeaderStyle == null) InitStyles();

            DrawHeader();
            EditorGUILayout.Space(4);
            DrawTargetPicker();
            EditorGUILayout.Space(4);

            if (m_Motor != null && m_MotorSO != null)
            {
                // 每帧从组件同步到缓存（缓冲编辑模式）
                m_MotorSO.Update();

                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
                {
                    DrawPresetSection();
                    EditorGUILayout.Space(3);
                    DrawComponentsSection();
                    EditorGUILayout.Space(3);
                    DrawCapsuleSection();
                    EditorGUILayout.Space(3);
                    DrawGroundingSection();
                    EditorGUILayout.Space(3);
                    DrawStepSection();
                    EditorGUILayout.Space(3);
                    DrawLedgeSection();
                    EditorGUILayout.Space(3);
                    DrawRigidbodySection();
                    EditorGUILayout.Space(3);
                    DrawConstraintsSection();
                    EditorGUILayout.Space(3);
                    DrawOtherSection();
                    EditorGUILayout.Space(3);

                    if (Application.isPlaying)
                        DrawRuntimeSection();
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(4);
                DrawActionBar();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "请在场景中选择一个挂载了 KinematicCharacterMotor 的 GameObject，\n" +
                    "或通过上方 ObjectField 手动指定目标。\n\n" +
                    "提示：选中场景中的 KCC 角色会自动关联。\n\n" +
                    "菜单路径: Tools/KCC/Motor Config Tool",
                    MessageType.Info);
            }
        }

        #endregion

        #region Header

        private void DrawHeader()
        {
            var r = GUILayoutUtility.GetRect(1, 36);
            EditorGUI.DrawRect(r, C_HEADER_BG);
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                var iconLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                    { fontSize = 16, alignment = TextAnchor.MiddleCenter, fixedWidth = 20 };
                GUILayout.Label("🎮", iconLabelStyle);
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("KCC Motor 配置工具", m_HeaderStyle);
                    GUILayout.Label("参数中文说明 · 缓冲编辑 · 预设配置", m_SubLabelStyle);
                }
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = m_Motor != null ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
                GUILayout.Button(m_Motor != null ? "已连接" : "未连接",
                    GUILayout.Width(56), GUILayout.Height(22));
                GUI.backgroundColor = Color.white;
                GUILayout.Space(8);
            }
        }

        #endregion

        #region Target Picker

        private void DrawTargetPicker()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                m_TargetObj = EditorGUILayout.ObjectField(
                    new GUIContent("目标对象", "挂载 KinematicCharacterMotor 的 GameObject"),
                    m_TargetObj, typeof(GameObject), true) as GameObject;
                if (EditorGUI.EndChangeCheck())
                    TryResolveTarget(m_TargetObj);

                if (GUILayout.Button("选中", EditorStyles.miniButton, GUILayout.Width(44)))
                {
                    if (m_Motor != null)
                        Selection.activeGameObject = m_Motor.gameObject;
                }
                EditorGUILayout.EndHorizontal();

                if (m_Motor != null)
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.TextField("组件名称", m_Motor.GetType().Name);

                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.TextField("所属 GameObject", m_Motor.gameObject.name);

                    EditorGUILayout.HelpBox(
                        $"胶囊: R={m_Motor.Capsule?.radius ?? 0} H={m_Motor.Capsule?.height ?? 0}\n" +
                        $"修改参数后点击「应用到组件」生效，「回退修改」可撤销未保存的更改。",
                        MessageType.Info);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void TryResolveTarget(GameObject go)
        {
            m_Motor = null;
            m_MotorSO = null;

            if (go == null) return;

            m_Motor = go.GetComponent<KinematicCharacterController.KinematicCharacterMotor>();
            if (m_Motor == null)
                m_Motor = go.GetComponentInChildren<KinematicCharacterController.KinematicCharacterMotor>(true);

            if (m_Motor != null)
            {
                m_MotorSO = new SerializedObject(m_Motor);
            }
        }

        #endregion

        #region Preset Section

        private void DrawPresetSection()
        {
            BeginSection(C_PRESET_BG, ref m_PresetFoldout, "🎯 预设配置 (Presets)", "一键应用典型角色参数");
            if (m_PresetFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(2);

                var presets = MotorPreset.All;
                int cols = Mathf.Min(3, presets.Length);
                for (int i = 0; i < presets.Length; i += cols)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int j = 0; j < cols && i + j < presets.Length; j++)
                    {
                        DrawPresetButton(presets[i + j], i + j);
                    }
                    EditorGUILayout.EndHorizontal();
                    if (i + cols < presets.Length) EditorGUILayout.Space(2);
                }

                // 选中预设的描述
                if (m_SelectedPresetIndex >= 0 && m_SelectedPresetIndex < presets.Length)
                {
                    GUILayout.Space(1);
                    GUILayout.Label(presets[m_SelectedPresetIndex].Description, m_PresetDescStyle);
                }

                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        private void DrawPresetButton(MotorPreset preset, int index)
        {
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = m_SelectedPresetIndex == index ? preset.AccentColor * 1.3f : preset.AccentColor * 0.75f;

            var content = new GUIContent(preset.Name, preset.Description);
            bool clicked = GUILayout.Button(content, m_PresetBtnStyle, GUILayout.Height(36));
            GUI.backgroundColor = prevColor;

            if (clicked)
            {
                m_SelectedPresetIndex = index;
                ApplyPreset(preset);
            }
        }

        private void ApplyPreset(MotorPreset p)
        {
            if (m_Motor == null || m_MotorSO == null) return;

            SetPropValue("CapsuleRadius", p.CapsuleRadius);
            SetPropValue("CapsuleHeight", p.CapsuleHeight);
            SetPropValue("CapsuleYOffset", p.CapsuleYOffset);

            SetPropValue("GroundDetectionExtraDistance", p.GroundDetectionExtraDistance);
            SetPropValue("MaxStableSlopeAngle", p.MaxStableSlopeAngle);

            SetPropValue("MaxStepHeight", p.MaxStepHeight);

            SetPropValue("MaxStableDistanceFromLedge", p.MaxStableDistanceFromLedge);
            SetPropValue("MaxVelocityForLedgeSnap", p.MaxVelocityForLedgeSnap);
            SetPropValue("MaxStableDenivelationAngle", p.MaxStableDenivelationAngle);

            SetPropValueBool("InteractiveRigidbodyHandling", p.InteractiveRigidbodyHandling);
            SetPropValue("SimulatedCharacterMass", p.SimulatedCharacterMass);
            SetPropValueBool("PreserveAttachedRigidbodyMomentum", p.PreserveAttachedRigidbodyMomentum);

            SetPropIntValue("MaxMovementIterations", p.MaxMovementIterations);
            SetPropIntValue("MaxDecollisionIterations", p.MaxDecollisionIterations);
        }

        #endregion

        #region Components Section

        private void DrawComponentsSection()
        {
            BeginSection(C_COMPONENTS_BG, ref m_ComponentsFoldout, "📦 组件引用 (Components)", "碰撞体等组件引用");
            if (m_ComponentsFoldout)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    var capsuleProp = m_MotorSO.FindProperty("Capsule");
                    if (capsuleProp != null)
                    {
                        using (new EditorGUI.DisabledScope(true))
                            EditorGUILayout.PropertyField(capsuleProp,
                                new GUIContent("胶囊碰撞体 (Capsule)", "角色的胶囊碰撞体引用（自动获取，只读）"));
                    }
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Capsule Section

        private void DrawCapsuleSection()
        {
            BeginSection(C_CAPSULE_BG, ref m_CapsuleFoldout, "⭕ 胶囊设置 (Capsule Settings)", "角色碰撞体的形状参数");
            if (m_CapsuleFoldout)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawPropertyWithHelp(m_MotorSO, "CapsuleRadius",
                        "胶囊半径 (CapsuleRadius)", "普通:0.3~0.5 | 矮胖:0.6~0.8 | 瘦小:0.2~0.3", "CapsuleRadius");
                    DrawPropertyWithHelp(m_MotorSO, "CapsuleHeight",
                        "胶囊高度 (CapsuleHeight)", "标准:1.8~2.0 | 矮小:1.2~1.5 | 高大:2.5~3.0", "CapsuleHeight");
                    DrawPropertyWithHelp(m_MotorSO, "CapsuleYOffset",
                        "Y轴偏移 (CapsuleYOffset)", "通常为 Height/2（胶囊中心在正中）", "CapsuleYOffset");
                    DrawPropertyWithHelp(m_MotorSO, "CapsulePhysicsMaterial",
                        "物理材质 (PhysicsMaterial)", "不影响自身移动，仅影响其他物体与角色碰撞时的表现", "CapsulePhysicsMaterial");
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Grounding Section

        private void DrawGroundingSection()
        {
            BeginSection(C_GROUNDING_BG, ref m_GroundingFoldout, "🌍 接地设置 (Grounding Settings)", "地面检测与稳定性判定");
            if (m_GroundingFoldout)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawPropertyWithHelp(m_MotorSO, "GroundDetectionExtraDistance",
                        "检测额外距离 (ExtraDistance)", "高速移动时增加此值防止脱地 (正常:0 | 高速:0.1~0.25)", "GroundDetectionExtraDistance");
                    DrawPropertyWithHelp(m_MotorSO, "MaxStableSlopeAngle",
                        "最大稳定斜坡角度 (MaxSlopeAngle)", "超过此角度会滑落 (严格:30~45 | 标准:50~60 | 宽松:70~80)", "MaxStableSlopeAngle");
                    DrawPropertyWithHelp(m_MotorSO, "StableGroundLayers",
                        "稳定地面层 (GroundLayers)", "定义哪些 Layer 被视为可站立的稳定地面", "StableGroundLayers");
                    DrawPropertyWithHelp(m_MotorSO, "DiscreteCollisionEvents",
                        "离散碰撞事件 (DiscreteEvents)", "是否通知控制器离散碰撞事件（有轻微性能开销）", "DiscreteCollisionEvents");
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Step Section

        private void DrawStepSection()
        {
            BeginSection(C_STEP_BG, ref m_StepFoldout, "🪜 台阶设置 (Step Settings)", "台阶攀爬相关参数");
            if (m_StepFoldout)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawPropertyWithHelp(m_MotorSO, "StepHandling",
                        "台阶处理方式 (StepHandling)", "None=不处理 | Standard=标准检测 | Extra=精确但耗性能", "StepHandling");
                    DrawPropertyWithHelp(m_MotorSO, "MaxStepHeight",
                        "最大台阶高度 (MaxStepHeight)", "低:0.2~0.3 | 标准:0.4~0.5 | 高:0.6~0.8", "MaxStepHeight");
                    DrawPropertyWithHelp(m_MotorSO, "AllowSteppingWithoutStableGrounding",
                        "不稳定时允许上台阶 (AllowNoStable)", "空中或不稳时是否仍能触发上台阶逻辑（通常关闭）", "AllowSteppingWithoutStableGrounding");
                    DrawPropertyWithHelp(m_MotorSO, "MinRequiredStepDepth",
                        "最小台阶深度 (MinStepDepth)", "Extra模式下台阶顶面必须至少多深才算有效（通常0.1）", "MinRequiredStepDepth");
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Ledge Section

        private void DrawLedgeSection()
        {
            BeginSection(C_LEDGE_BG, ref m_LedgeFoldout, "🏔️ 边缘设置 (Ledge Settings)", "悬崖边缘检测与高低差处理");
            if (m_LedgeFoldout)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawPropertyWithHelp(m_MotorSO, "LedgeAndDenivelationHandling",
                        "边缘检测开关 (LedgeHandling)", "启用后正确处理悬崖边接地状态（有性能开销，建议开启）", "LedgeAndDenivelationHandling");
                    DrawPropertyWithHelp(m_MotorSO, "MaxStableDistanceFromLedge",
                        "边缘最大稳定距离 (DistFromLedge)", "距中心轴多远仍稳定（通常等于或略小于 Radius）", "MaxStableDistanceFromLedge");
                    DrawPropertyWithHelp(m_MotorSO, "MaxVelocityForLedgeSnap",
                        "边缘吸附速度阈值 (VelForSnap)", "超速时不被吸回地面而是飞出 (0=始终吸附 | 高速:5~10)", "MaxVelocityForLedgeSnap");
                    DrawPropertyWithHelp(m_MotorSO, "MaxStableDenivelationAngle",
                        "最大向下坡度变化角 (DenivelationAngle)", "走到陡坡边缘是否飞出 (180=永远吸附 | 自然飞出:60~90)", "MaxStableDenivelationAngle");
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Rigidbody Section

        private void DrawRigidbodySection()
        {
            BeginSection(C_RIGIDBODY_BG, ref m_RigidbodyFoldout, "💥 刚体交互 (Rigidbody Interaction)", "与其他刚体/移动平台的交互行为");
            if (m_RigidbodyFoldout)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawPropertyWithHelp(m_MotorSO, "InteractiveRigidbodyHandling",
                        "启用刚体交互 (InteractiveRB)", "能否推动物体/站在移动平台上（无平台时可关以省性能）", "InteractiveRigidbodyHandling");
                    DrawPropertyWithHelp(m_MotorSO, "RigidbodyInteractionType",
                        "刚体交互模式 (InteractionType)", "Kinematic=推力无限 | SimulatedDynamic=受反作用力", "RigidbodyInteractionType");
                    DrawPropertyWithHelp(m_MotorSO, "SimulatedCharacterMass",
                        "模拟质量 (SimulatedMass)", "推动物体的力度 (轻:0.5~1 | 标准:1~2 | 重型:5~10)", "SimulatedCharacterMass");
                    DrawPropertyWithHelp(m_MotorSO, "PreserveAttachedRigidbodyMomentum",
                        "保留平台动量 (PreserveMomentum)", "离开移动平台时是否继承平台速度（建议开启否则突然停住）", "PreserveAttachedRigidbodyMomentum");
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Constraints Section

        private void DrawConstraintsSection()
        {
            BeginSection(C_CONSTRAINTS_BG, ref m_ConstraintsFoldout, "🔒 约束设置 (Constraints)", "平面约束（用于2D横版或固定轨道）");
            if (m_ConstraintsFoldout)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawPropertyWithHelp(m_MotorSO, "HasPlanarConstraint",
                        "平面约束开关 (PlanarConstraint)", "3D保持关闭，2D横版/固定轨道开启", "HasPlanarConstraint");
                    DrawPropertyWithHelp(m_MotorSO, "PlanarConstraintAxis",
                        "约束轴方向 (ConstraintAxis)", "移动约束在垂直于此向量的平面上 ((0,0,1)=XY平面=2D横版)", "PlanarConstraintAxis");
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Other Section

        private void DrawOtherSection()
        {
            BeginSection(C_OTHER_BG, ref m_OtherFoldout, "⚙️ 其他设置 (Other Settings)", "迭代次数与安全限制");
            if (m_OtherFoldout)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawPropertyWithHelp(m_MotorSO, "MaxMovementIterations",
                        "最大移动迭代次数 (MoveIter)", "碰撞重试上限 (简单:3 | 标准:5 | 复杂:8~10)", "MaxMovementIterations", true);
                    DrawPropertyWithHelp(m_MotorSO, "MaxDecollisionIterations",
                        "最大去重叠迭代次数 (DeColIter)", "穿模修正重试 (通常1足够 | 经常卡墙:2~3)", "MaxDecollisionIterations", true);
                    DrawPropertyWithHelp(m_MotorSO, "CheckMovementInitialOverlaps",
                        "初始重叠检测 (InitOverlap)", "防穿透检查（建议开启）", "CheckMovementInitialOverlaps");
                    DrawPropertyWithHelp(m_MotorSO, "KillVelocityWhenExceedMaxMovementIterations",
                        "超限清零速度 (KillVelocity)", "达到迭代上限时清零速度（避免卡墙异常加速）", "KillVelocityWhenExceedMaxMovementIterations");
                    DrawPropertyWithHelp(m_MotorSO, "KillRemainingMovementWhenExceedMaxMovementIterations",
                        "超限丢弃余量 (KillRemain)", "达到迭代上限时放弃本帧剩余移动（配合上一项使用）", "KillRemainingMovementWhenExceedMaxMovementIterations");
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Runtime Section

        private void DrawRuntimeSection()
        {
            BeginSection(C_RUNTIME_BG, ref m_RuntimeFoldout, "📊 运行时属性 (PlayMode Only)", "仅运行时可见的实时数据 — 只读", true);
            if (m_RuntimeFoldout)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new EditorGUI.DisabledScope(true))
                    {
                        // GroundingStatus
                        EditorGUILayout.LabelField("接地状态 (GroundingStatus)", EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;
                        EditorGUILayout.ToggleLeft("稳定着地 (IsStableOnGround)", m_Motor.GroundingStatus.IsStableOnGround);
                        EditorGUILayout.ToggleLeft("检测到地面 (FoundAnyGround)", m_Motor.GroundingStatus.FoundAnyGround);
                        EditorGUILayout.Vector3Field("地面法线 (GroundNormal)", m_Motor.GroundingStatus.GroundNormal);
                        EditorGUI.indentLevel--;

                        EditorGUILayout.Space(2);

                        // Velocities
                        EditorGUILayout.LabelField("速度信息", EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;
                        EditorGUILayout.Vector3Field("总速度 (Velocity)", m_Motor.Velocity);
                        EditorGUILayout.Vector3Field("基础速度 (BaseVelocity)", m_Motor.BaseVelocity);
                        EditorGUILayout.Vector3Field("附着刚体速度 (AttachRBVel)", m_Motor.AttachedRigidbodyVelocity);
                        EditorGUI.indentLevel--;

                        EditorGUILayout.Space(2);

                        // Transforms
                        EditorGUILayout.LabelField("瞬态位置/旋转", EditorStyles.boldLabel);
                        EditorGUI.indentLevel++;
                        EditorGUILayout.Vector3Field("目标位置 (TransientPos)", m_Motor.TransientPosition);
                        EditorGUILayout.Vector3Field("上方向 (CharacterUp)", m_Motor.CharacterUp);
                        EditorGUI.indentLevel--;
                    }
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Action Bar

        private void DrawActionBar()
        {
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();

                GUI.backgroundColor = new Color(0.2f, 0.6f, 0.3f);
                if (GUILayout.Button("✅ 应用到组件", GUILayout.Width(120), GUILayout.Height(26)))
                    ApplyToComponent();
                GUI.backgroundColor = Color.white;

                GUILayout.Space(6);

                GUI.backgroundColor = new Color(0.7f, 0.5f, 0.2f);
                if (GUILayout.Button("↩️ 回退修改", GUILayout.Width(110), GUILayout.Height(26)))
                    RevertChanges();
                GUI.backgroundColor = Color.white;

                GUILayout.Space(6);

                if (GUILayout.Button("◎ Ping 对象", GUILayout.Width(90), GUILayout.Height(26)))
                {
                    if (m_Motor != null)
                        EditorGUIUtility.PingObject(m_Motor);
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void ApplyToComponent()
        {
            if (m_Motor == null || m_MotorSO == null) return;

            Undo.RecordObject(m_Motor, "应用 KCC Motor 参数修改");
            m_MotorSO.ApplyModifiedProperties();
            EditorUtility.SetDirty(m_Motor);
        }

        private void RevertChanges()
        {
            if (m_Motor == null || m_MotorSO == null) return;
            m_MotorSO.Update(); // 从组件重新读取，丢弃所有 GUI 未应用的修改
        }

        #endregion

        #region Help System

        private static GUIStyle s_HelpBtnStyle = null;

        private static readonly Dictionary<string, string> s_HelpTexts = new Dictionary<string, string>
        {
            ["CapsuleRadius"] =
                "【胶囊半径 — CapsuleRadius】\n\n" +
                "作用：角色胶囊碰撞体的半径。\n\n" +
                "典型取值：\n" +
                "• 普通人形：0.3 ~ 0.5\n" +
                "• 较胖角色：0.6 ~ 0.8\n" +
                "• 瘦小角色：0.2 ~ 0.3\n\n" +
                "调整建议：应与角色模型身体宽度匹配。过大会导致无法通过窄通道，过小会导致穿模。",

            ["CapsuleHeight"] =
                "【胶囊高度 — CapsuleHeight】\n\n" +
                "作用：角色胶囊碰撞体的总高度。\n\n" +
                "典型取值：\n" +
                "• 标准人形：1.8 ~ 2.0\n" +
                "• 矮小角色：1.2 ~ 1.5\n" +
                "• 高大角色：2.5 ~ 3.0\n\n" +
                "调整建议：应匹配角色模型从脚底到头顶的高度。",

            ["CapsuleYOffset"] =
                "【Y轴偏移 — CapsuleYOffset】\n\n" +
                "作用：胶囊碰撞体中心相对 Transform 原点的 Y 偏移。\n\n" +
                "调整建议：\n" +
                "• Transform 在脚底 → 设为 Height/2\n" +
                "• Transform 在身体中心 → 设为 0",

            ["CapsulePhysicsMaterial"] =
                "【物理材质 — PhysicsMaterial】\n\n" +
                "作用：胶囊碰撞体的物理材质。\n\n" +
                "说明：不影响角色自身的移动行为，仅影响其他物体与角色碰撞时的物理表现（弹跳、摩擦）。\n\n" +
                "建议：大多数情况留空。需要特殊碰撞反馈时再设置。",

            ["GroundDetectionExtraDistance"] =
                "【地面检测额外距离 — ExtraDistance】\n\n" +
                "作用：增加地面检测的范围，防止高速移动时脱地。\n\n" +
                "典型取值：\n" +
                "• 正常速度：0\n" +
                "• 高速移动/快速下坡：0.1 ~ 0.25\n\n" +
                "建议：如果角色快速移动时偶尔\"弹起\"，适当增加此值。",

            ["MaxStableSlopeAngle"] =
                "【最大稳定斜坡角度 — MaxSlopeAngle】\n\n" +
                "作用：角色能稳定站立的最大斜坡角度（0 ~ 89°）。\n超过此角度会滑落。\n\n" +
                "典型取值：\n" +
                "• 严格地形：30° ~ 45°\n" +
                "• 标准设置：50° ~ 60°\n" +
                "• 宽松设置：70° ~ 80°",

            ["StableGroundLayers"] =
                "【稳定地面层 — StableGroundLayers】\n\n" +
                "作用：定义哪些 Layer 上的表面被视为可站立的稳定地面。\n\n" +
                "建议：默认为所有层。需要排除某些层（水面、特效层）时取消勾选。",

            ["DiscreteCollisionEvents"] =
                "【离散碰撞事件 — DiscreteEvents】\n\n" +
                "作用：启用后通过 ICharacterController 接口通知控制器离散碰撞事件。\n\n" +
                "建议：仅在需要监听碰撞事件时开启，有轻微性能开销。",

            ["StepHandling"] =
                "【台阶处理方式 — StepHandling】\n\n" +
                "模式说明：\n" +
                "• None：不处理台阶，被阻挡\n" +
                "• Standard：标准检测，自动上台阶（推荐）\n" +
                "• Extra：精确检测，性能开销更大\n\n" +
                "建议：大多数场景用 Standard。无台阶可设为 None 省性能。",

            ["MaxStepHeight"] =
                "【最大台阶高度 — MaxStepHeight】\n\n" +
                "作用：角色能自动攀爬的最大台阶高度。\n\n" +
                "典型取值：\n" +
                "• 低台阶：0.2 ~ 0.3\n" +
                "• 标准台阶：0.4 ~ 0.5\n" +
                "• 高台阶：0.6 ~ 0.8",

            ["AllowSteppingWithoutStableGrounding"] =
                "【不稳定时允许上台阶 — AllowNoStable】\n\n" +
                "作用：不在稳定地面时是否也能触发上台阶逻辑。\n\n" +
                "建议：大多数情况保持关闭。仅在特殊玩法需求时开启。",

            ["MinRequiredStepDepth"] =
                "【最小台阶深度 — MinStepDepth】\n\n" +
                "作用：Extra 模式下台阶顶部平面的最小深度要求。\n防止角色站在极窄边缘上。\n\n" +
                "建议：通常 0.1 即可。",

            ["LedgeAndDenivelationHandling"] =
                "【边缘检测开关 — LedgeHandling】\n\n" +
                "作用：启用悬崖边缘和高低差检测处理。\n防止角色\"半挂\"在悬崖边。\n\n" +
                "建议：保持开启。仅无悬崖且极度追求性能时考虑关闭。",

            ["MaxStableDistanceFromLedge"] =
                "【边缘最大稳定距离 — DistFromLedge】\n\n" +
                "作用：站在边缘时距中心轴多远仍视为稳定。\n\n" +
                "建议：通常等于或略小于 CapsuleRadius。\n值越小越容易滑落，越大能站得越靠边。",

            ["MaxVelocityForLedgeSnap"] =
                "【边缘吸附速度阈值 — VelForSnap】\n\n" +
                "作用：超速经过边缘时不被吸回地面而是飞出去。\n\n" +
                "取值：\n" +
                "• 0 = 始终吸附地面\n" +
                "• 5 ~ 10 = 高速时自然飞出",

            ["MaxStableDenivelationAngle"] =
                "【最大向下坡度变化角 — DenivelationAngle】\n\n" +
                "作用：向下的坡度变化超过此角度时脱离地面（1 ~ 180°）。\n180° = 永远吸附地面。\n\n" +
                "建议：希望走到陡坡边缘自然飞出 → 60° ~ 90°；永远贴地 → 180°",

            ["InteractiveRigidbodyHandling"] =
                "【启用刚体交互 — InteractiveRB】\n\n" +
                "作用：开启后角色可以：\n" +
                "• 被 PhysicsMover 移动平台携带\n" +
                "• 推动动态刚体\n" +
                "• 站在动态刚体上\n\n" +
                "建议：无移动平台和可推物体时可关闭以节省性能。",

            ["RigidbodyInteractionType"] =
                "【刚体交互模式 — InteractionType】\n\n" +
                "• Kinematic：角色以无限力量推物体（不受反作用力）\n" +
                "• SimulatedDynamic：模拟动态刚体，会被物体反推\n\n" +
                "建议：大多数情况用 Kinematic。希望角色被巨石撞飞等效果用 SimulatedDynamic。",

            ["SimulatedCharacterMass"] =
                "【模拟质量 — SimulatedMass】\n\n" +
                "作用：推动物体时使用的质量值。质量越大推力越强。\n\n" +
                "典型取值：\n" +
                "• 轻量：0.5 ~ 1\n" +
                "• 标准：1 ~ 2\n" +
                "• 重型：5 ~ 10",

            ["PreserveAttachedRigidbodyMomentum"] =
                "【保留平台动量 — PreserveMomentum】\n\n" +
                "作用：离开移动平台时是否继承平台的速度。\n\n" +
                "建议：保持开启！否则从移动平台跳下时会感觉\"突然停住\"。",

            ["HasPlanarConstraint"] =
                "【平面约束开关 — PlanarConstraint】\n\n" +
                "作用：启用后将移动约束在指定平面上。\n\n" +
                "建议：3D 游戏保持关闭。2D 横版或固定轨道游戏开启。",

            ["PlanarConstraintAxis"] =
                "【约束轴方向 — ConstraintAxis】\n\n" +
                "作用：移动约束在垂直于此向量的平面上。\n\n" +
                "常见值：\n" +
                "• (0, 0, 1) = XY 平面（标准 2D 横版）\n" +
                "• (1, 0, 0) = YZ 平面",

            ["MaxMovementIterations"] =
                "【最大移动迭代次数 — MoveIter】\n\n" +
                "作用：碰到障碍后沿表面滑动并重新检测的最大重试次数。\n\n" +
                "典型取值：\n" +
                "• 简单场景：3\n" +
                "• 标准：5（推荐）\n" +
                "• 复杂碰撞环境：8 ~ 10\n\n" +
                "注意：值越大越精确但性能开销越高。",

            ["MaxDecollisionIterations"] =
                "【最大去重叠迭代次数 — DeColIter】\n\n" +
                "作用：穿模修正的重试次数（将角色推出重叠区域）。\n\n" +
                "建议：1 通常足够。经常卡进物体时可增加到 2 ~ 3。",

            ["CheckMovementInitialOverlaps"] =
                "【初始重叠检测 — InitOverlap】\n\n" +
                "作用：移动前检查是否已与其他物体重叠，防止穿透。\n\n" +
                "建议：保持开启！仅在极度追求性能且确保无穿模风险时关闭。",

            ["KillVelocityWhenExceedMaxMovementIterations"] =
                "【超限清零速度 — KillVelocity】\n\n" +
                "作用：移动迭代达上限时将速度归零。\n防止卡墙时积累异常速度。\n\n" +
                "建议：保持开启。",

            ["KillRemainingMovementWhenExceedMaxMovementIterations"] =
                "【超限丢弃余量 — KillRemain】\n\n" +
                "作用：迭代达上限时放弃本帧剩余移动距离。\n防止累积导致下一帧\"弹射\"。\n\n" +
                "建议：保持开启。",
        };

        private GUIStyle GetHelpButtonStyle()
        {
            if (s_HelpBtnStyle != null) return s_HelpBtnStyle;

            s_HelpBtnStyle = new GUIStyle(EditorStyles.miniButton)
            {
                fixedWidth = 18,
                fixedHeight = 18,
                fontSize = 10,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(2, 0, 2, 0)
            };
            return s_HelpBtnStyle;
        }

        /// <summary>绘制 PropertyField + 右侧 ? 帮助按钮</summary>
        private void DrawPropertyWithHelp(SerializedObject so, string propertyPath, string label, string tooltip, string helpKey, bool isInt = false)
        {
            SerializedProperty prop = so.FindProperty(propertyPath);
            if (prop == null) return;

            EditorGUILayout.BeginHorizontal();
            if (isInt)
                EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip));
            else
                EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip));
            if (GUILayout.Button("?", GetHelpButtonStyle()))
            {
                string helpText = s_HelpTexts.ContainsKey(helpKey) ? s_HelpTexts[helpKey] : "暂无详细说明。";
                EditorUtility.DisplayDialog($"帮助 — {label}", helpText, "知道了");
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Helpers —— 属性赋值快捷方法

        private void SetPropValue(string path, float value)
        {
            var prop = m_MotorSO?.FindProperty(path);
            if (prop != null) prop.floatValue = value;
        }

        private void SetPropIntValue(string path, int value)
        {
            var prop = m_MotorSO?.FindProperty(path);
            if (prop != null) prop.intValue = value;
        }

        private void SetPropValueBool(string path, bool value)
        {
            var prop = m_MotorSO?.FindProperty(path);
            if (prop != null) prop.boolValue = value;
        }

        #endregion

        #region Section UI Helpers

        private void BeginSection(Color bgColor, ref bool foldout, string title, string subtitle, bool dimmed = false)
        {
            var r = GUILayoutUtility.GetRect(1, foldout ? 22 : 22);
            EditorGUI.DrawRect(r, bgColor);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(6);
                var style = dimmed
                    ? new GUIStyle(EditorStyles.foldoutHeader)
                      { normal = { textColor = new Color(0.50f, 0.50f, 0.50f) } }
                    : EditorStyles.foldoutHeader;
                foldout = EditorGUILayout.Foldout(foldout, title, true, style);
                GUILayout.FlexibleSpace();
                if (!string.IsNullOrEmpty(subtitle))
                {
                    var subStyle = new GUIStyle(EditorStyles.miniLabel)
                    { fontSize = 8, fontStyle = FontStyle.Italic,
                      normal = { textColor = dimmed ? new Color(0.42f, 0.42f, 0.42f) : new Color(0.50f, 0.50f, 0.50f) } };
                    GUILayout.Label(subtitle, subStyle);
                    GUILayout.Space(6);
                }
            }
            var line = GUILayoutUtility.GetRect(1, 1);
            EditorGUI.DrawRect(line, new Color(0.2f, 0.2f, 0.2f, 0.25f));
            if (foldout) GUILayout.Space(2);
        }

        private static void EndSection() => EditorGUILayout.Space(2);

        #endregion
    }
}
#endif
