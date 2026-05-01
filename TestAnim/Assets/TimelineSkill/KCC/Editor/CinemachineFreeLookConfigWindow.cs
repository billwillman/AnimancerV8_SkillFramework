#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CinemachineEditorTool
{
    /// <summary>
    /// CinemachineFreeLook 配置工具窗口 — 提供游戏类型预设参数一键配置
    /// 适配 Cinemachine 2.10.7 (com.unity.cinemachine@2.10.7)
    ///
    /// 架构说明:
    ///   FreeLook 自身持有: m_XAxis, m_YAxis, m_Lens, m_Orbits[] 等
    ///   每个子级 Rig (TopRig/MiddleRig/BottomRig) 各自持有:
    ///     - CinemachineOrbitalTransposer (m_XAxis 被从属, m_YawDamping)
    ///     - CinemachineComposer (m_HorizontalDamping, m_VerticalDamping,
    ///         m_DeadZoneWidth/Height, m_ScreenX/Y, m_SoftZoneWidth/Height)
    ///
    /// 菜单: Tools/Cinemachine/FreeLook Config Tool
    /// </summary>
    public class CinemachineFreeLookConfigWindow : EditorWindow
    {
        #region Presets

        public struct GameTypePreset
        {
            public string Name;
            public string Description;
            public Color AccentColor;

            // === X Axis (m_XAxis on FreeLook) — 水平旋转 ===
            public float XMaxSpeed;       // AxisState.m_MaxSpeed
            public float XAccelTime;       // AxisState.m_AccelTime
            public float XDecelTime;       // AxisState.m_DecelTime

            // === Y Axis (m_YAxis on FreeLook) — Rig 混合 (0..1) ===
            public float YMaxSpeed;       // Rig 切换速度
            public float YAccelTime;
            public float YDecelTime;

            // === Composer (每个子级 Rig 的 CinemachineComposer) ===
            public float HorizontalDamping;  // m_HorizontalDamping
            public float VerticalDamping;    // m_VerticalDamping
            public float DeadZoneWidth;      // m_DeadZoneWidth  (0~2)
            public float DeadZoneHeight;     // m_DeadZoneHeight (0~2)
            public float ScreenX;            // m_ScreenX (-0.5~1.5)
            public float ScreenY;            // m_ScreenY (-0.5~1.5)

            // === OrbitalTransposer (每个子级 Rig) ===
            public float YawDamping;         // m_YawDamping

            public static readonly GameTypePreset[] All = new[]
            {
                new GameTypePreset
                {
                    Name = "动作战斗 (Action)",
                    Description = "快速响应，适合战斗/动作类游戏。相机紧随角色转向，几乎无延迟。",
                    AccentColor = new Color(0.85f, 0.30f, 0.25f),
                    XMaxSpeed = 400f, XAccelTime = 0.03f, XDecelTime = 0.03f,
                    YMaxSpeed = 3f,   YAccelTime = 0.04f, YDecelTime = 0.05f,
                    HorizontalDamping = 0.2f, VerticalDamping = 0.3f,
                    DeadZoneWidth = 0.15f, DeadZoneHeight = 0.12f,
                    ScreenX = 0.5f, ScreenY = 0.55f,
                    YawDamping = 0.1f,
                },
                new GameTypePreset
                {
                    Name = "RPG 探索",
                    Description = "平稳舒适，适合开放世界/RPG探索。相机过渡柔和，长时间游玩不晕眩。",
                    AccentColor = new Color(0.25f, 0.55f, 0.85f),
                    XMaxSpeed = 200f, XAccelTime = 0.12f, XDecelTime = 0.15f,
                    YMaxSpeed = 1.5f, YAccelTime = 0.15f, YDecelTime = 0.2f,
                    HorizontalDamping = 0.6f, VerticalDamping = 0.6f,
                    DeadZoneWidth = 0.2f, DeadZoneHeight = 0.15f,
                    ScreenX = 0.5f, ScreenY = 0.55f,
                    YawDamping = 0.4f,
                },
                new GameTypePreset
                {
                    Name = "第三人称射击 (TPS)",
                    Description = "精准跟瞄，相机响应快但有适度阻尼。适合越肩视角射击类游戏。",
                    AccentColor = new Color(0.45f, 0.70f, 0.30f),
                    XMaxSpeed = 320f, XAccelTime = 0.06f, XDecelTime = 0.08f,
                    YMaxSpeed = 2.5f, YAccelTime = 0.08f, YDecelTime = 0.1f,
                    HorizontalDamping = 0.35f, VerticalDamping = 0.35f,
                    DeadZoneWidth = 0.12f, DeadZoneHeight = 0.10f,
                    ScreenX = 0.5f, ScreenY = 0.52f,
                    YawDamping = 0.2f,
                },
                new GameTypePreset
                {
                    Name = "模拟驾驶",
                    Description = "重惯性，模拟真实物理手感。相机有明显的加速/减速过程，适合载具/飞行类。",
                    AccentColor = new Color(0.80f, 0.60f, 0.15f),
                    XMaxSpeed = 120f, XAccelTime = 0.35f, XDecelTime = 0.4f,
                    YMaxSpeed = 1f,   YAccelTime = 0.3f,  YDecelTime = 0.35f,
                    HorizontalDamping = 0.75f, VerticalDamping = 0.75f,
                    DeadZoneWidth = 0.25f, DeadZoneHeight = 0.20f,
                    ScreenX = 0.5f, ScreenY = 0.5f,
                    YawDamping = 0.6f,
                },
                new GameTypePreset
                {
                    Name = "电影镜头 (Cinematic)",
                    Description = "缓慢飘逸，强调画面美感。相机移动如丝般顺滑，适合过场/剧情/观景模式。",
                    AccentColor = new Color(0.60f, 0.35f, 0.70f),
                    XMaxSpeed = 65f,  XAccelTime = 0.5f,  XDecelTime = 0.7f,
                    YMaxSpeed = 0.8f, YAccelTime = 0.45f, YDecelTime = 0.6f,
                    HorizontalDamping = 0.9f, VerticalDamping = 0.9f,
                    DeadZoneWidth = 0.3f, DeadZoneHeight = 0.25f,
                    ScreenX = 0.5f, ScreenY = 0.5f,
                    YawDamping = 0.8f,
                },
                new GameTypePreset
                {
                    Name = "平台跳跃 (Platformer)",
                    Description = "垂直灵敏+水平适中，精确控制俯仰角。适合需要判断落点的平台跳跃游戏。",
                    AccentColor = new Color(0.95f, 0.55f, 0.10f),
                    XMaxSpeed = 250f, XAccelTime = 0.08f, XDecelTime = 0.1f,
                    YMaxSpeed = 4f,   YAccelTime = 0.05f, YDecelTime = 0.07f,
                    HorizontalDamping = 0.25f, VerticalDamping = 0.25f,
                    DeadZoneWidth = 0.18f, DeadZoneHeight = 0.14f,
                    ScreenX = 0.5f, ScreenY = 0.45f,
                    YawDamping = 0.15f,
                },
            };
        }

        #endregion

        #region State

        private GameObject m_TargetObj;
        private Cinemachine.CinemachineFreeLook m_FreeLook;
        private SerializedObject m_FreeLookSO;
        private SerializedObject[] m_RigSOs;           // 每个子级 rig 的 SerializedObject
        private Vector2 m_ScrollPos;
        private int m_SelectedPresetIndex = -1;

        private bool m_PresetFoldout = true;
        private bool m_XAxisFoldout = true;
        private bool m_YAxisFoldout = true;
        private bool m_ComposerFoldout = true;
        private bool m_OrbitFoldout = false;
        private bool m_AdvancedFoldout = false;

        #endregion

        #region Styles

        private GUIStyle m_HeaderStyle;
        private GUIStyle m_SubLabelStyle;
        private GUIStyle m_PresetBtnStyle;
        private GUIStyle m_PresetDescStyle;

        private static readonly Color C_HEADER_BG = new Color(0.13f, 0.32f, 0.58f, 1f);
        private static readonly Color C_PRESET_BG = new Color(0.22f, 0.38f, 0.62f, 0.10f);
        private static readonly Color C_XAXIS_BG  = new Color(0.58f, 0.35f, 0.22f, 0.10f);
        private static readonly Color C_YAXIS_BG  = new Color(0.22f, 0.55f, 0.38f, 0.10f);
        private static readonly Color C_COMP_BG    = new Color(0.55f, 0.35f, 0.55f, 0.10f);
        private static readonly Color C_ORBIT_BG  = new Color(0.35f, 0.35f, 0.58f, 0.10f);
        private static readonly Color C_ADV_BG    = new Color(0.42f, 0.42f, 0.42f, 0.10f);

        private void InitStyles()
        {
            m_HeaderStyle = new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 14, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            m_SubLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 9 };
            m_PresetBtnStyle = new GUIStyle(EditorStyles.miniButton)
            { fontSize = 14, fontStyle = FontStyle.Bold, padding = new RectOffset(10, 10, 12, 12),
              normal = { textColor = Color.white },
              hover = { textColor = Color.white },
              active = { textColor = new Color(0.9f, 0.9f, 0.95f) } };
            m_PresetDescStyle = new GUIStyle(EditorStyles.label)
            { fontSize = 11, fontStyle = FontStyle.Italic, wordWrap = true,
              padding = new RectOffset(4, 2, 2, 0),
              normal = { textColor = new Color(0.95f, 0.93f, 0.88f) } };
        }

        #endregion

        #region Menu & Open

        [MenuItem("Tools/Cinemachine/FreeLook Config Tool", false, 201)]
        public static void OpenWindow()
        {
            var window = GetWindow<CinemachineFreeLookConfigWindow>("CM FreeLook 配置");
            window.minSize = new Vector2(430, 560);
            window.maxSize = new Vector2(550, 900);
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

            if (m_FreeLook != null)
            {
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
                {
                    DrawPresetSection();
                    EditorGUILayout.Space(3);
                    DrawXAxisSection();
                    EditorGUILayout.Space(3);
                    DrawYAxisSection();
                    EditorGUILayout.Space(3);
                    DrawComposerSection();
                    EditorGUILayout.Space(3);
                    DrawOrbitSection();
                    EditorGUILayout.Space(3);
                    DrawAdvancedSection();
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(4);
                DrawActionBar();
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "请在场景中选择一个挂载了 CinemachineFreeLook 的 GameObject，\n" +
                    "或通过上方 ObjectField 手动指定目标。\n\n" +
                    "提示：选中场景中的 FreeLook 相机会自动关联。",
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
                // 用文字标签替代图标，避免 IconContent 在不同 Unity 版本中图标名不存在导致报错
                var iconLabelStyle = new GUIStyle(EditorStyles.boldLabel)
                    { fontSize = 16, alignment = TextAnchor.MiddleCenter, fixedWidth = 20 };
                GUILayout.Label("🎬", iconLabelStyle);
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("Cinemachine FreeLook 配置工具", m_HeaderStyle);
                    GUILayout.Label("Game Type Presets · 一键参数配置", m_SubLabelStyle);
                }
                GUILayout.FlexibleSpace();
                GUI.backgroundColor = m_FreeLook != null ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.5f, 0.5f, 0.5f);
                GUILayout.Button(m_FreeLook != null ? "已连接" : "未连接",
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
                    new GUIContent("目标对象", "挂载 CinemachineFreeLook 的 GameObject"),
                    m_TargetObj, typeof(GameObject), true) as GameObject;
                if (EditorGUI.EndChangeCheck())
                    TryResolveTarget(m_TargetObj);

                if (GUILayout.Button("选中", EditorStyles.miniButton, GUILayout.Width(44)))
                {
                    if (m_FreeLook != null)
                        Selection.activeObject = m_FreeLook;
                }
                EditorGUILayout.EndHorizontal();

                if (m_FreeLook != null)
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUILayout.TextField("组件名称", m_FreeLook.name);

                    int rigCount = CountValidRigs();
                    EditorGUILayout.HelpBox(
                        $"已检测到 {rigCount}/3 个子级 Rig (TopRig / MiddleRig / BottomRig)\n" +
                        $"Cinemachine 版本: 2.10.7",
                        rigCount == 3 ? MessageType.Info : MessageType.Warning);
                }
            }
            EditorGUILayout.EndVertical();
        }

        private void TryResolveTarget(GameObject go)
        {
            m_FreeLook = null;
            m_FreeLookSO = null;
            m_RigSOs = null;

            if (go == null) return;

            m_FreeLook = go.GetComponent<Cinemachine.CinemachineFreeLook>();
            if (m_FreeLook == null)
                m_FreeLook = go.GetComponentInChildren<Cinemachine.CinemachineFreeLook>(true);

            if (m_FreeLook != null)
            {
                m_FreeLookSO = new SerializedObject(m_FreeLook);
                CacheRigSerializedObjects();
            }
        }

        /// <summary>缓存所有子级 rig 的 SerializedObject</summary>
        private void CacheRigSerializedObjects()
        {
            if (m_FreeLook == null) return;

            var rigs = new System.Collections.Generic.List<SerializedObject>();
            for (int i = 0; i < 3; i++)
            {
                var rig = m_FreeLook.GetRig(i);
                if (rig != null)
                    rigs.Add(new SerializedObject(rig));
            }
            m_RigSOs = rigs.ToArray();
        }

        /// <summary>刷新所有 SerializedObject（在修改前调用）</summary>
        private void RefreshAllSOs()
        {
            m_FreeLookSO?.UpdateIfDirtyOrScript();
            if (m_RigSOs != null)
                foreach (var so in m_RigSOs)
                    so?.UpdateIfDirtyOrScript();
        }

        /// <summary>应用所有 SerializedObject 的修改</summary>
        private void ApplyAllSOs()
        {
            m_FreeLookSO?.ApplyModifiedProperties();
            if (m_RigSOs != null)
                foreach (var so in m_RigSOs)
                    so?.ApplyModifiedProperties();
        }

        private int CountValidRigs()
        {
            if (m_FreeLook == null) return 0;
            int count = 0;
            for (int i = 0; i < 3; i++)
                if (m_FreeLook.GetRig(i) != null) count++;
            return count;
        }

        #endregion

        #region Preset Section

        private void DrawPresetSection()
        {
            BeginSection(C_PRESET_BG, ref m_PresetFoldout, "预设选择 (Presets)", "一键应用常用游戏类型参数");
            if (m_PresetFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.Space(2);

                var presets = GameTypePreset.All;
                int cols = Mathf.Min(2, presets.Length);
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

                // 在水平布局块外部显示选中预设的描述
                if (m_SelectedPresetIndex >= 0 && m_SelectedPresetIndex < presets.Length)
                {
                    GUILayout.Space(1);
                    GUILayout.Label(presets[m_SelectedPresetIndex].Description, m_PresetDescStyle);
                }

                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        private void DrawPresetButton(GameTypePreset preset, int index)
        {
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = m_SelectedPresetIndex == index ? preset.AccentColor * 1.3f : preset.AccentColor * 0.85f;

            bool clicked = GUILayout.Button(preset.Name, m_PresetBtnStyle, GUILayout.Height(44));
            GUI.backgroundColor = prevColor;

            if (clicked)
            {
                m_SelectedPresetIndex = index;
                ApplyPreset(preset);
            }
        }

        private void ApplyPreset(GameTypePreset p)
        {
            if (m_FreeLook == null || m_FreeLookSO == null) return;

            Undo.RecordObject(m_FreeLook, $"Apply Preset: {p.Name}");
            // 同时记录所有 rig 以支持撤销
            for (int i = 0; i < 3; i++)
            {
                var rig = m_FreeLook.GetRig(i);
                if (rig != null) Undo.RecordObject(rig, $"Apply Preset: {p.Name} (Rig{i})");
            }

            RefreshAllSOs();

            // ====== X Axis (FreeLook 自身的 m_XAxis) ======
            SetFreeLookAxisState("m_XAxis", p.XMaxSpeed, p.XAccelTime, p.XDecelTime);

            // ====== Y Axis (FreeLook 自身的 m_YAxis) ======
            SetFreeLookAxisState("m_YAxis", p.YMaxSpeed, p.YAccelTime, p.YDecelTime);

            // ====== 每个 Rig 的 Composer 设置 ======
            ForEachRigComposer((composerProp) =>
            {
                FindRelative(composerProp, "m_HorizontalDamping").floatValue = p.HorizontalDamping;
                FindRelative(composerProp, "m_VerticalDamping").floatValue = p.VerticalDamping;
                FindRelative(composerProp, "m_DeadZoneWidth").floatValue = p.DeadZoneWidth;
                FindRelative(composerProp, "m_DeadZoneHeight").floatValue = p.DeadZoneHeight;
                FindRelative(composerProp, "m_ScreenX").floatValue = p.ScreenX;
                FindRelative(composerProp, "m_ScreenY").floatValue = p.ScreenY;
            });

            // ====== 每个 Rig 的 OrbitalTransposer YawDamping ======
            ForEachRigOrbital((orbitalProp) =>
            {
                FindRelative(orbitalProp, "m_YawDamping").floatValue = p.YawDamping;
            });

            ApplyAllSOs();
            for (int i = 0; i < 3; i++)
            {
                var rig = m_FreeLook.GetRig(i);
                if (rig != null) EditorUtility.SetDirty(rig);
            }
            EditorUtility.SetDirty(m_FreeLook);
        }

        #endregion

        #region X Axis Section (m_XAxis on FreeLook)

        private void DrawXAxisSection()
        {
            BeginSection(C_XAXIS_BG, ref m_XAxisFoldout, "X轴 — 水平旋转 (m_XAxis)", "FreeLook 自身属性，控制左右环绕旋转的响应");
            if (m_XAxisFoldout)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawFreeLookAxisFields("m_XAxis",
                        "水平旋转最大速度 (deg/s)",
                        "从静止到目标速度的过渡时间",
                        "从运动到停止的缓冲时间");
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Y Axis Section (m_YAxis on FreeLook)

        private void DrawYAxisSection()
        {
            BeginSection(C_YAXIS_BG, ref m_YAxisFoldout, "Y轴 — Rig混合 (m_YAxis)", "FreeLook 自身属性，控制 Top/Middle/Bottom Rig 之间的混合速度 (值域 0..1)");
            if (m_YAxisFoldout)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawFreeLookAxisFields("m_YAxis",
                        "Rig 混合速度 (单位/秒)",
                        "垂直方向加速响应时间",
                        "垂直方向减速缓冲时间");

                    EditorGUILayout.Space(2);
                    // 显示当前 Y 值
                    if (m_FreeLookSO != null)
                    {
                        var yValueProp = m_FreeLookSO.FindProperty("m_YAxis.m_Value");
                        if (yValueProp != null)
                        {
                            using (new EditorGUI.DisabledScope(true))
                                EditorGUILayout.FloatField("当前 Y 值 (运行时)", yValueProp.floatValue);
                        }
                    }
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Composer Section (on child Rigs)

        private void DrawComposerSection()
        {
            BeginSection(C_COMP_BG, ref m_ComposerFoldout, "Composer (瞄准/跟随阻尼)", "每个子级 Rig 的 CinemachineComposer 属性");
            if (m_ComposerFoldout)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    // 从第一个 rig 读取并编辑 Composer 属性
                    bool anyFound = false;
                    ForEachRigComposer((composerProp) =>
                    {
                        if (!anyFound)
                        {
                            EditorGUILayout.PropertyField(FindRelative(composerProp, "m_HorizontalDamping"),
                                new GUIContent("Horizontal Damping", "水平方向跟随目标的阻尼 (0=灵敏, 20=极慢)"));
                            EditorGUILayout.PropertyField(FindRelative(composerProp, "m_VerticalDamping"),
                                new GUIContent("Vertical Damping", "垂直方向跟随目标的阻尼 (0=灵敏, 20=极慢)"));
                            EditorGUILayout.Space(2);
                            EditorGUILayout.PropertyField(FindRelative(composerProp, "m_DeadZoneWidth"),
                                new GUIContent("死区宽度", "屏幕中心水平不触发相机移动范围 (0~2)"));
                            EditorGUILayout.PropertyField(FindRelative(composerProp, "m_DeadZoneHeight"),
                                new GUIContent("死区高度", "屏幕中心垂直不触发相机移动范围 (0~2)"));
                            EditorGUILayout.Space(2);
                            EditorGUILayout.PropertyField(FindRelative(composerProp, "m_ScreenX"),
                                new GUIContent("注视点 X", "屏幕上目标位置 X (-0.5左 ~ 1.5右)"));
                            EditorGUILayout.PropertyField(FindRelative(composerProp, "m_ScreenY"),
                                new GUIContent("注视点 Y", "屏幕上目标位置 Y (-0.5下 ~ 1.5上)"));

                            EditorGUILayout.Space(2);
                            EditorGUILayout.PropertyField(FindRelative(composerProp, "m_SoftZoneWidth"),
                                new GUIContent("Soft Zone 宽", "软区域宽度，区域内相机逐渐重新对齐"));
                            EditorGUILayout.PropertyField(FindRelative(composerProp, "m_SoftZoneHeight"),
                                new GUIContent("Soft Zone 高", "软区域高度"));

                            anyFound = true;
                        }
                    });

                    if (!anyFound)
                        EditorGUILayout.HelpBox("未找到任何 Rig 的 Composer 组件", MessageType.Warning);

                    ApplyAllSOs();
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Orbit Section (m_Orbits)

        private void DrawOrbitSection()
        {
            BeginSection(C_ORBIT_BG, ref m_OrbitFoldout, "轨道参数 (Orbits)", "三个 Rig 的高度和半径");
            if (m_OrbitFoldout)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (m_FreeLookSO != null)
                    {
                        string[] names = { "TopRig (顶)", "MiddleRig (中)", "BottomRig (底)" };
                        for (int i = 0; i < 3; i++)
                        {
                            EditorGUILayout.LabelField(names[i], EditorStyles.boldLabel);
                            EditorGUI.indentLevel++;
                            var hProp = m_FreeLookSO.FindProperty($"m_Orbits.Array.data[{i}].m_Height");
                            var rProp = m_FreeLookSO.FindProperty($"m_Orbits.Array.data[{i}].m_Radius");
                            EditorGUILayout.PropertyField(hProp, new GUIContent("高度", "相对于目标的高度偏移"));
                            EditorGUILayout.PropertyField(rProp, new GUIContent("半径", "轨道半径"));
                            EditorGUI.indentLevel--;
                            if (i < 2) EditorGUILayout.Space(2);
                        }
                        m_FreeLookSO.ApplyModifiedProperties();
                    }
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Advanced Section

        private void DrawAdvancedSection()
        {
            BeginSection(C_ADV_BG, ref m_AdvancedFoldout, "高级设置 (Advanced)", "镜头、绑定模式、Spline 等全局参数");
            if (m_AdvancedFoldout)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    if (m_FreeLookSO != null)
                    {
                        EditorGUILayout.PropertyField(m_FreeLookSO.FindProperty("m_CommonLens"),
                            new GUIContent("Common Lens", "统一镜头设置应用到所有 Rig"));

                        EditorGUILayout.PropertyField(m_FreeLookSO.FindProperty("m_BindingMode"),
                            new GUIContent("Binding Mode", "坐标空间模式"));

                        EditorGUILayout.PropertyField(m_FreeLookSO.FindProperty("m_SplineCurvature"),
                            new GUIContent("Spline Curvature", "Rig 间曲线张力 (0~1)"));

                        // Lens Settings
                        EditorGUILayout.Space(2);
                        EditorGUILayout.LabelField("镜头设置 (Lens)", EditorStyles.boldLabel);
                        var lensProp = m_FreeLookSO.FindProperty("m_Lens");
                        if (lensProp != null && lensProp.hasVisibleChildren)
                        {
                            EditorGUI.indentLevel++;
                            lensProp.isExpanded = EditorGUILayout.Foldout(lensProp.isExpanded, "Lens Settings");
                            if (lensProp.isExpanded)
                            {
                                var fov = lensProp.FindPropertyRelative("m_FieldOfView");
                                if (fov != null) EditorGUILayout.PropertyField(fov, new GUIContent("FOV"));
                                var near = lensProp.FindPropertyRelative("m_NearClipPlane");
                                if (near != null) EditorGUILayout.PropertyField(near, new GUIContent("Near Clip"));
                                var far = lensProp.FindPropertyRelative("m_FarClipPlane");
                                if (far != null) EditorGUILayout.PropertyField(far, new GUIContent("Far Clip"));
                            }
                            EditorGUI.indentLevel--;
                        }

                        // Yaw Damping per rig
                        EditorGUILayout.Space(2);
                        bool yawShown = false;
                        ForEachRigOrbital((orbitalProp) =>
                        {
                            if (!yawShown)
                            {
                                EditorGUILayout.PropertyField(
                                    FindRelative(orbitalProp, "m_YawDamping"),
                                    new GUIContent("Yaw Damping (Rigs)", "OrbitalTransposer 的旋转阻尼"));
                                yawShown = true;
                            }
                        });

                        m_FreeLookSO.ApplyModifiedProperties();
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
                if (GUILayout.Button("Reset to Default", GUILayout.Width(140)))
                {
                    if (m_FreeLook != null &&
                        EditorUtility.DisplayDialog("确认重置",
                            $"确定要将 \"{m_FreeLook.name}\" 的所有参数恢复为默认值吗？\n\n此操作可以撤销 (Ctrl+Z)。",
                            "确认重置", "取消"))
                    {
                        ResetToDefault();
                    }
                }
                GUILayout.Space(8);
                if (GUILayout.Button("Ping Camera", GUILayout.Width(100)))
                {
                    if (m_FreeLook != null)
                        EditorGUIUtility.PingObject(m_FreeLook);
                }
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Helpers —— FreeLook 自身 AxisState 操作

        /// <summary>设置 FreeLook 自身上的 AxisState 字段 (m_XAxis 或 m_YAxis)</summary>
        private void SetFreeLookAxisState(string axisPath, float maxSpeed, float accelTime, float decelTime)
        {
            if (m_FreeLookSO == null) return;
            var axisProp = m_FreeLookSO.FindProperty(axisPath);
            if (axisProp == null) return;

            SetAxisStateValues(axisProp, maxSpeed, accelTime, decelTime);
        }

        /// <summary>绘制 FreeLook 自身上 AxisState 的字段</summary>
        private void DrawFreeLookAxisFields(string axisPath, string speedHint, string accelHint, string decelHint)
        {
            if (m_FreeLookSO == null) return;
            var axisProp = m_FreeLookSO.FindProperty(axisPath);
            if (axisProp == null)
            {
                EditorGUILayout.HelpBox($"未找到属性: {axisPath}", MessageType.Warning);
                return;
            }

            EditorGUILayout.PropertyField(axisProp.FindPropertyRelative("m_MaxSpeed"),
                new GUIContent("Max Speed", speedHint));
            EditorGUILayout.PropertyField(axisProp.FindPropertyRelative("m_AccelTime"),
                new GUIContent("Accel Time", accelHint));
            EditorGUILayout.PropertyField(axisProp.FindPropertyRelative("m_DecelTime"),
                new GUIContent("Decel Time", decelHint));

            m_FreeLookSO.ApplyModifiedProperties();
        }

        /// <summary>通用：给 AxisState 的 SerializedProperty 赋值</summary>
        private static void SetAxisStateValues(SerializedProperty axisProp, float maxSpeed, float accelTime, float decelTime)
        {
            var sp = axisProp.FindPropertyRelative("m_MaxSpeed");
            if (sp != null) sp.floatValue = maxSpeed;
            sp = axisProp.FindPropertyRelative("m_AccelTime");
            if (sp != null) sp.floatValue = accelTime;
            sp = axisProp.FindPropertyRelative("m_DecelTime");
            if (sp != null) sp.floatValue = decelTime;
        }

        #endregion

        #region Helpers —— 子级 Rig 遍历操作

        delegate void RigComponentAction(SerializedProperty componentProp);

        /// <summary>遍历所有有效 Rig 的 Composer 组件属性</summary>
        private void ForEachRigComposer(RigComponentAction action)
        {
            if (m_RigSOs == null) return;
            foreach (var rigSO in m_RigSOs)
            {
                if (rigSO == null) continue;
                // Composer 在组件 pipeline 中，通过 CinemachineVirtualCamera.m_ComponentPipeline
                // 但更简单的方式是直接查找序列化的子属性
                // Composer 字段名取决于它如何被序列化
                var composer = FindComponentInPipeline(rigSO, "CinemachineComposer");
                if (composer != null)
                    action(composer);
            }
        }

        /// <summary>遍历所有有效 Rig 的 OrbitalTransposer 组件属性</summary>
        private void ForEachRigOrbital(RigComponentAction action)
        {
            if (m_RigSOs == null) return;
            foreach (var rigSO in m_RigSOs)
            {
                if (rigSO == null) continue;
                var orbital = FindComponentInPipeline(rigSO, "CinemachineOrbitalTransposer");
                if (orbital != null)
                    action(orbital);
            }
        }

        /// <summary>
        /// 在 VirtualCamera 的序列化数据中查找指定类型的组件。
        /// CinemachineVirtualCamera 将组件存储在 m_ComponentPipeline 数组中。
        /// </summary>
        private SerializedProperty FindComponentInPipeline(SerializedObject vcamSo, string componentTypeName)
        {
            // 尝试直接通过脚本引用查找
            // CinemachineVirtualCamera 的组件通常作为子属性或通过特定路径访问
            // 由于 Composer/OrbitalTransposer 是附加组件，需要遍历 m_ComponentPipeline
            var pipeline = vcamSo?.FindProperty("m_ComponentPipeline");
            if (pipeline == null || !pipeline.isArray) return null;

            for (int i = 0; i < pipeline.arraySize; i++)
            {
                var elem = pipeline.GetArrayElementAtIndex(i);
                if (elem == null) continue;
                // 检查 m_Script 引用的类型名
                var scriptProp = elem.FindPropertyRelative("m_Script");
                if (scriptProp != null && scriptProp.objectReferenceValue is MonoBehaviour mb)
                {
                    if (mb.GetType().Name == componentTypeName)
                        return elem;
                }
            }

            // Fallback: 直接搜索已知路径（某些版本可能不同）
            // 尝试用 MonoBehaviour 引用方式找
            return null;
        }

        /// <summary>更健壮的方式：直接从 rig GameObject 上获取组件并用反射式赋值</summary>
        private void SetOnAllComposers(System.Action<Cinemachine.CinemachineComposer> action)
        {
            if (m_FreeLook == null) return;
            for (int i = 0; i < 3; i++)
            {
                var rig = m_FreeLook.GetRig(i);
                if (rig == null) continue;
                var composer = rig.GetComponent<Cinemachine.CinemachineComposer>();
                if (composer != null)
                {
                    Undo.RecordObject(composer, "Modify Composer");
                    action(composer);
                    EditorUtility.SetDirty(composer);
                }
            }
        }

        private void SetOnAllOrbitals(System.Action<Cinemachine.CinemachineOrbitalTransposer> action)
        {
            if (m_FreeLook == null) return;
            for (int i = 0; i < 3; i++)
            {
                var rig = m_FreeLook.GetRig(i);
                if (rig == null) continue;
                var orbital = rig.GetComponent<Cinemachine.CinemachineOrbitalTransposer>();
                if (orbital != null)
                {
                    Undo.RecordObject(orbital, "Modify OrbitalTransposer");
                    action(orbital);
                    EditorUtility.SetDirty(orbital);
                }
            }
        }

        private static SerializedProperty FindRelative(SerializedProperty parent, string relativePath)
        {
            return parent?.FindPropertyRelative(relativePath);
        }

        #endregion

        #region Reset

        private void ResetToDefault()
        {
            if (m_FreeLook == null) return;

            Undo.RecordObject(m_FreeLook, "Reset CM FreeLook Defaults");
            RefreshAllSOs();

            // X Axis 默认值 (来自源码构造函数: -180, 180, true, false, 300f, 0.1f, 0.1f)
            SetFreeLookAxisState("m_XAxis", 300f, 0.1f, 0.1f);
            // Y Axis 默认值 (0, 1, false, true, 2f, 0.2f, 0.1f)
            SetFreeLookAxisState("m_YAxis", 2f, 0.2f, 0.1f);

            // 重置所有 Composer 为默认值
            SetOnAllComposers(c =>
            {
                c.m_HorizontalDamping = 0.5f;
                c.m_VerticalDamping = 0.5f;
                c.m_DeadZoneWidth = 0f;
                c.m_DeadZoneHeight = 0f;
                c.m_ScreenX = 0.5f;
                c.m_ScreenY = 0.5f;
                c.m_SoftZoneWidth = 0.8f;
                c.m_SoftZoneHeight = 0.8f;
            });

            // 重置所有 OrbitalTransposer
            SetOnAllOrbitals(o => o.m_YawDamping = 0f);

            ApplyAllSOs();
            EditorUtility.SetDirty(m_FreeLook);
            m_SelectedPresetIndex = -1;

            // 重建 rig SO 缓存以反映新值
            CacheRigSerializedObjects();
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
                      { normal = { textColor = new Color(0.45f, 0.45f, 0.45f) } }
                    : EditorStyles.foldoutHeader;
                foldout = EditorGUILayout.Foldout(foldout, title, true, style);
                GUILayout.FlexibleSpace();
                if (!string.IsNullOrEmpty(subtitle))
                {
                    var subStyle = new GUIStyle(EditorStyles.miniLabel)
                    { fontSize = 8, fontStyle = FontStyle.Italic,
                      normal = { textColor = dimmed ? new Color(0.4f, 0.4f, 0.4f) : new Color(0.5f, 0.5f, 0.5f) } };
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