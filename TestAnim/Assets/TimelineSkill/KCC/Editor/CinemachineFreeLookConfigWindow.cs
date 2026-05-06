#if UNITY_EDITOR
using System.Collections.Generic;
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

            bool clicked = GUILayout.Button(new GUIContent(preset.Name, preset.Description), m_PresetBtnStyle, GUILayout.Height(44));
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
                            DrawPropertyWithHelp(FindRelative(composerProp, "m_HorizontalDamping"),
                                "Horizontal Damping", "水平方向跟随目标的阻尼 (0=灵敏, 20=极慢)", "Horizontal Damping");
                            DrawPropertyWithHelp(FindRelative(composerProp, "m_VerticalDamping"),
                                "Vertical Damping", "垂直方向跟随目标的阻尼 (0=灵敏, 20=极慢)", "Vertical Damping");
                            EditorGUILayout.Space(2);
                            DrawPropertyWithHelp(FindRelative(composerProp, "m_DeadZoneWidth"),
                                "死区宽度", "屏幕中心水平不触发相机移动范围 (0~2)", "Dead Zone Width");
                            DrawPropertyWithHelp(FindRelative(composerProp, "m_DeadZoneHeight"),
                                "死区高度", "屏幕中心垂直不触发相机移动范围 (0~2)", "Dead Zone Height");
                            EditorGUILayout.Space(2);
                            DrawPropertyWithHelp(FindRelative(composerProp, "m_ScreenX"),
                                "注视点 X", "屏幕上目标位置 X (-0.5左 ~ 1.5右)", "Screen X");
                            DrawPropertyWithHelp(FindRelative(composerProp, "m_ScreenY"),
                                "注视点 Y", "屏幕上目标位置 Y (-0.5下 ~ 1.5上)", "Screen Y");

                            EditorGUILayout.Space(2);
                            DrawPropertyWithHelp(FindRelative(composerProp, "m_SoftZoneWidth"),
                                "Soft Zone 宽", "软区域宽度，区域内相机逐渐重新对齐", "Soft Zone Width");
                            DrawPropertyWithHelp(FindRelative(composerProp, "m_SoftZoneHeight"),
                                "Soft Zone 高", "软区域高度", "Soft Zone Height");

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
                            DrawPropertyWithHelp(hProp, "高度", "相对于目标的高度偏移", "Orbit Height");
                            DrawPropertyWithHelp(rProp, "半径", "轨道半径", "Orbit Radius");
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
                        DrawPropertyWithHelp(m_FreeLookSO.FindProperty("m_CommonLens"),
                            "Common Lens", "统一镜头设置应用到所有 Rig", "Common Lens");

                        DrawPropertyWithHelp(m_FreeLookSO.FindProperty("m_BindingMode"),
                            "Binding Mode", "坐标空间模式", "Binding Mode");

                        DrawPropertyWithHelp(m_FreeLookSO.FindProperty("m_SplineCurvature"),
                            "Spline Curvature", "Rig 间曲线张力 (0~1)", "Spline Curvature");

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
                                if (fov != null) DrawPropertyWithHelp(fov, "FOV", "视场角", "FOV");
                                var near = lensProp.FindPropertyRelative("m_NearClipPlane");
                                if (near != null) DrawPropertyWithHelp(near, "Near Clip", "近裁剪面", "Near Clip");
                                var far = lensProp.FindPropertyRelative("m_FarClipPlane");
                                if (far != null) DrawPropertyWithHelp(far, "Far Clip", "远裁剪面", "Far Clip");
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
                                var yawProp = FindRelative(orbitalProp, "m_YawDamping");
                                if (yawProp != null)
                                {
                                    DrawPropertyWithHelp(yawProp, "Yaw Damping (Rigs)",
                                        "OrbitalTransposer 的旋转阻尼", "Yaw Damping");
                                }
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

        #region Help Button Utilities

        private static GUIStyle s_HelpBtnStyle = null; // lazy init below

        private static readonly Dictionary<string, string> s_HelpTexts = new Dictionary<string, string>
        {
            // ====== Advanced Section ======
            ["Common Lens"] =
                "【Common Lens — 统一镜头】\n\n" +
                "作用：当启用时，三个 Rig（Top / Middle / Bottom）共享同一套镜头参数（FOV、Near/Far Clip 等）。" +
                "关闭后每个 Rig 可独立设置各自的镜头。\n\n" +
                "建议：大多数情况建议开启，保持视觉一致性。仅当你需要不同仰角呈现不同 FOV 效果时才关闭。",

            ["Binding Mode"] =
                "【Binding Mode — 绑定模式】\n\n" +
                "作用：决定 FreeLook 相机在哪个坐标空间中跟随目标旋转。\n\n" +
                "常用选项：\n" +
                "• WorldSpace — 相机在世界空间固定朝向，不随角色旋转\n" +
                "• SimpleFollowWithWorldUp — 跟随角色 Y 轴旋转，保持世界 Up\n" +
                "• LockToTargetWithWorldUp — 锁定目标朝向，适合第三人称\n" +
                "• LockToTarget — 完全锁定目标坐标系\n\n" +
                "建议：第三人称动作游戏推荐 LockToTargetWithWorldUp 或 SimpleFollowWithWorldUp。",

            ["Spline Curvature"] =
                "【Spline Curvature — 曲线张力】\n\n" +
                "作用：控制三个 Rig（Top / Middle / Bottom）之间过渡轨道的曲线弯曲程度。\n" +
                "取值范围 0 ~ 1：\n" +
                "• 0 = 直线插值，Rig 间过渡生硬\n" +
                "• 1 = 最大弯曲，过渡圆滑\n\n" +
                "建议：默认 0.5 适合大部分场景。若相机在仰角切换时出现抖动可适当降低。",

            ["FOV"] =
                "【Field of View — 视场角】\n\n" +
                "作用：控制镜头视野宽度（垂直方向角度）。\n" +
                "取值范围：1° ~ 179°\n" +
                "• 较小值（如 30-40）→ 长焦效果，背景压缩，角色突出\n" +
                "• 较大值（如 70-90）→ 广角效果，视野开阔但有畸变\n\n" +
                "建议：第三人称一般 50-65，FPS 一般 60-90。",

            ["Near Clip"] =
                "【Near Clip Plane — 近裁剪面】\n\n" +
                "作用：距相机小于此距离的物体不渲染。\n" +
                "取值范围：> 0（通常 0.01 ~ 1）\n\n" +
                "注意：值太小会导致深度精度不足（Z-fighting），值太大会裁掉近处物体。\n" +
                "建议：一般设 0.1 ~ 0.3，大型开放世界可设 0.5。",

            ["Far Clip"] =
                "【Far Clip Plane — 远裁剪面】\n\n" +
                "作用：距相机大于此距离的物体不渲染。\n" +
                "取值范围：> Near Clip（通常 100 ~ 5000）\n\n" +
                "注意：与 Near Clip 的比值越大深度精度越差。\n" +
                "建议：室内场景 100-500，室外开放世界 1000-5000。",

            ["Yaw Damping"] =
                "【Yaw Damping — 水平旋转阻尼】\n\n" +
                "作用：当目标旋转时，相机跟随旋转的响应速度（OrbitalTransposer）。\n" +
                "取值：0 = 无延迟立即跟随；数值越大延迟越高、过渡越柔和。\n\n" +
                "典型取值：\n" +
                "• 动作战斗：0 ~ 0.5（快速跟随）\n" +
                "• RPG 探索：1 ~ 3（柔和过渡）\n" +
                "• 赛车/载具：3 ~ 5（平滑感）\n\n" +
                "建议：配合预设的 Damping X 一起调整，确保手感一致。",

            // ====== X Axis Section ======
            ["X Max Speed"] =
                "【X Max Speed — 水平旋转最大速度】\n\n" +
                "作用：控制玩家输入驱动的水平环绕旋转最大角速度（度/秒）。\n" +
                "取值范围：0 ~ 1000+\n" +
                "• 较小值（100-200）→ 旋转缓慢，适合策略/探索类\n" +
                "• 较大值（300-500）→ 旋转灵敏，适合动作/FPS 类\n\n" +
                "建议：动作游戏 300-400，RPG 200-300，根据输入设备（手柄/鼠标）微调。",

            ["X Accel Time"] =
                "【X Accel Time — 水平加速时间】\n\n" +
                "作用：从静止到达 Max Speed 所需的过渡时间（秒）。\n" +
                "取值范围：0 ~ 5\n" +
                "• 0 = 立即达到最大速度，手感直接\n" +
                "• 较大值（0.1-0.5）→ 有加速过程，感觉更平滑\n\n" +
                "建议：鼠标控制设 0 或极小值；手柄控制设 0.1-0.3 获得平滑起步感。",

            ["X Decel Time"] =
                "【X Decel Time — 水平减速时间】\n\n" +
                "作用：松开输入后从当前速度减速到停止的缓冲时间（秒）。\n" +
                "取值范围：0 ~ 5\n" +
                "• 0 = 立即停止，操控精准\n" +
                "• 较大值（0.1-0.5）→ 有惯性滑动，感觉更丝滑\n\n" +
                "建议：动作游戏 0.1-0.2，探索类 0.2-0.5。过大会让玩家感到操控迟钝。",

            // ====== Y Axis Section ======
            ["Y Max Speed"] =
                "【Y Max Speed — Rig 混合速度】\n\n" +
                "作用：控制 Top/Middle/Bottom 三个 Rig 之间切换的速度（单位/秒）。\n" +
                "Y 轴值域 0~1，此值决定每秒能变化多少。\n" +
                "取值范围：0.1 ~ 10\n" +
                "• 较小值（1-2）→ Rig 切换缓慢，适合电影感\n" +
                "• 较大值（3-5）→ Rig 切换灵敏，适合动作类\n\n" +
                "建议：动作游戏 2-4，RPG/慢节奏 1-2。",

            ["Y Accel Time"] =
                "【Y Accel Time — 垂直加速时间】\n\n" +
                "作用：垂直方向（Rig 混合）从静止加速到目标速度的时间（秒）。\n" +
                "取值范围：0 ~ 5\n" +
                "• 0 = 立即响应，无加速过程\n" +
                "• 较大值 → 有渐入感，过渡平滑\n\n" +
                "建议：一般 0.1-0.3，让上下视角切换有轻微的起步过渡感。",

            ["Y Decel Time"] =
                "【Y Decel Time — 垂直减速时间】\n\n" +
                "作用：松开垂直输入后的惯性缓冲时间（秒）。\n" +
                "取值范围：0 ~ 5\n" +
                "• 0 = 立即停止\n" +
                "• 较大值 → 有下滑/惯性效果\n\n" +
                "建议：0.1-0.3，保持与 X Decel Time 一致的手感。过大会导致 Rig 切换过冲。",

            // ====== Composer Section ======
            ["Horizontal Damping"] =
                "【Horizontal Damping — 水平跟随阻尼】\n\n" +
                "作用：目标在屏幕水平方向移动时，相机重新对齐的延迟程度。\n" +
                "取值范围：0 ~ 20\n" +
                "• 0 = 立即跟随，无延迟\n" +
                "• 较大值 → 相机跟随滞后，有\"悬浮\"感\n\n" +
                "建议：动作游戏 0.5-2，探索类 2-5。需配合 Dead Zone 一起调，\n" +
                "Dead Zone 内不触发阻尼跟随。",

            ["Vertical Damping"] =
                "【Vertical Damping — 垂直跟随阻尼】\n\n" +
                "作用：目标在屏幕垂直方向移动时，相机重新对齐的延迟程度。\n" +
                "取值范围：0 ~ 20\n" +
                "• 0 = 立即跟随\n" +
                "• 较大值 → 垂直方向跟随更慢\n\n" +
                "建议：通常与 Horizontal Damping 保持接近或略大（1-3），\n" +
                "跳跃频繁的游戏可稍大以避免相机剧烈抖动。",

            ["Dead Zone Width"] =
                "【Dead Zone Width — 死区宽度】\n\n" +
                "作用：屏幕中心的水平区域，目标在此范围内移动不会触发相机跟随。\n" +
                "取值范围：0 ~ 2（比例值，0=无死区，1=满屏宽）\n\n" +
                "效果：\n" +
                "• 0 → 目标稍微移动相机就跟随，操控精确但可能晃动\n" +
                "• 0.1-0.3 → 有一定容忍度，角色小幅移动时相机稳定\n\n" +
                "建议：动作游戏 0-0.1，RPG 0.1-0.2，策略类 0.2-0.4。",

            ["Dead Zone Height"] =
                "【Dead Zone Height — 死区高度】\n\n" +
                "作用：屏幕中心的垂直区域，目标在此范围内上下移动不触发相机跟随。\n" +
                "取值范围：0 ~ 2（比例值）\n\n" +
                "效果：\n" +
                "• 0 → 垂直方向立即跟随\n" +
                "• 较大值 → 角色跳跃或上下移动时相机不会跟着抖\n\n" +
                "建议：平台跳跃类 0.2-0.5（避免跳跃时相机乱动），动作类 0.04-0.15。",

            ["Screen X"] =
                "【Screen X — 注视点水平位置】\n\n" +
                "作用：目标在屏幕上的期望水平位置。0.5 = 屏幕正中。\n" +
                "取值范围：-0.5 ~ 1.5\n" +
                "• 0.5 = 目标居中\n" +
                "• < 0.5 → 目标偏左，右侧空间更大\n" +
                "• > 0.5 → 目标偏右，左侧空间更大\n\n" +
                "建议：大多数游戏保持 0.5。需要\"越肩视角\"时偏移到 0.3 或 0.7。",

            ["Screen Y"] =
                "【Screen Y — 注视点垂直位置】\n\n" +
                "作用：目标在屏幕上的期望垂直位置。0.5 = 屏幕正中。\n" +
                "取值范围：-0.5 ~ 1.5\n" +
                "• 0.5 = 目标居中\n" +
                "• < 0.5 → 目标偏下，上方空间更大（看到更多天空）\n" +
                "• > 0.5 → 目标偏上，下方空间更大（看到更多地面）\n\n" +
                "建议：第三人称一般 0.55-0.65（角色略偏下，看到更多前方环境）。",

            ["Soft Zone Width"] =
                "【Soft Zone Width — 软区域宽度】\n\n" +
                "作用：在死区之外、硬边界之内的水平区域。目标进入此区域时，相机会按阻尼\n" +
                "逐渐重新对齐，将目标拉回死区。\n" +
                "取值范围：0 ~ 2\n" +
                "• 较大值 → 相机容忍目标偏移更大才开始激进跟随\n" +
                "• 较小值 → 相机更早开始强制回拉\n\n" +
                "建议：通常 0.6-1.0。配合 Damping 调整跟随体感。",

            ["Soft Zone Height"] =
                "【Soft Zone Height — 软区域高度】\n\n" +
                "作用：死区之外的垂直软区域，目标在此范围内相机按阻尼平滑跟随。\n" +
                "取值范围：0 ~ 2\n\n" +
                "建议：与 Soft Zone Width 保持接近，一般 0.6-1.0。\n" +
                "跳跃游戏可设大些避免频繁强制拉回。",

            // ====== Orbit Section ======
            ["Orbit Height"] =
                "【Orbit Height — 轨道高度】\n\n" +
                "作用：该 Rig 相对于跟随目标（Follow Target）的垂直高度偏移（世界单位）。\n\n" +
                "三个 Rig 的典型配置：\n" +
                "• TopRig: 正值（如 4-8），从上方俯视\n" +
                "• MiddleRig: 接近 0 或略正（如 1-3），平视\n" +
                "• BottomRig: 负值或 0（如 -2 到 0），仰视\n\n" +
                "建议：根据角色身高和视角需求调整。TopRig 高度决定俯视极限，BottomRig 决定仰视极限。",

            ["Orbit Radius"] =
                "【Orbit Radius — 轨道半径】\n\n" +
                "作用：相机在该 Rig 上距离目标的水平距离（世界单位）。\n\n" +
                "三个 Rig 的典型配置：\n" +
                "• TopRig: 较小半径（如 1-3），俯视时离角色近\n" +
                "• MiddleRig: 中等半径（如 4-8），平视时适中\n" +
                "• BottomRig: 可大可小，取决于仰视效果\n\n" +
                "建议：半径越大相机离角色越远，视野越开阔。\n" +
                "动作游戏 MiddleRig 3-6，RPG/开放世界 5-10。"
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

        /// <summary>
        /// 绘制 PropertyField + 右侧 ? 帮助按钮
        /// </summary>
        private void DrawPropertyWithHelp(SerializedProperty prop, string label, string tooltip, string helpKey)
        {
            if (prop == null) return;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip));
            if (GUILayout.Button("?", GetHelpButtonStyle()))
            {
                string helpText = s_HelpTexts.ContainsKey(helpKey) ? s_HelpTexts[helpKey] : "暂无详细说明。";
                EditorUtility.DisplayDialog($"帮助 — {label}", helpText, "知道了");
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 仅绘制 ? 帮助按钮（用于不使用 PropertyField 的特殊行）
        /// </summary>
        private void DrawHelpButton(string label, string helpKey)
        {
            if (GUILayout.Button("?", GetHelpButtonStyle()))
            {
                string helpText = s_HelpTexts.ContainsKey(helpKey) ? s_HelpTexts[helpKey] : "暂无详细说明。";
                EditorUtility.DisplayDialog($"帮助 — {label}", helpText, "知道了");
            }
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

            // 根据轴路径确定帮助文本 key 前缀
            string prefix = axisPath == "m_XAxis" ? "X" : "Y";

            DrawPropertyWithHelp(axisProp.FindPropertyRelative("m_MaxSpeed"),
                "Max Speed", speedHint, $"{prefix} Max Speed");
            DrawPropertyWithHelp(axisProp.FindPropertyRelative("m_AccelTime"),
                "Accel Time", accelHint, $"{prefix} Accel Time");
            DrawPropertyWithHelp(axisProp.FindPropertyRelative("m_DecelTime"),
                "Decel Time", decelHint, $"{prefix} Decel Time");

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