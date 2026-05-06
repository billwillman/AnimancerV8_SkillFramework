#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CinemachineEditorTool
{
    /// <summary>
    /// CinemachineVirtualCamera 配置工具窗口 — 提供游戏类型预设参数一键配置
    /// 适配 Cinemachine 2.10.7 (com.unity.cinemachine@2.10.7)
    ///
    /// 架构说明:
    ///   VirtualCamera 使用 Pipeline 模式, Body(如 CinemachineTransposer) 和 Aim(如 CinemachineComposer)
    ///   通过 GetCinemachineComponent 获取
    ///
    /// 菜单: Tools/Cinemachine/VirtualCamera Config Tool
    /// </summary>
    public class CinemachineVirtualCameraConfigWindow : EditorWindow
    {
        #region Presets

        public struct VCamPreset
        {
            public string Name;
            public string Description;
            public Color AccentColor;

            // === Body / Transposer ===
            public Vector3 FollowOffset;         // m_FollowOffset
            public float XDamping;               // m_XDamping
            public float YDamping;               // m_YDamping
            public float ZDamping;               // m_ZDamping
            public int BindingMode;              // m_BindingMode enum index

            // === Aim / Composer ===
            public float HorizontalDamping;      // m_HorizontalDamping
            public float VerticalDamping;        // m_VerticalDamping
            public float DeadZoneWidth;          // m_DeadZoneWidth
            public float DeadZoneHeight;         // m_DeadZoneHeight
            public float ScreenX;                // m_ScreenX
            public float ScreenY;                // m_ScreenY
            public float SoftZoneWidth;          // m_SoftZoneWidth
            public float SoftZoneHeight;         // m_SoftZoneHeight

            // === Lens ===
            public float FOV;                    // m_Lens.FieldOfView
            public float NearClip;               // m_Lens.NearClipPlane
            public float FarClip;                // m_Lens.FarClipPlane

            public static readonly VCamPreset[] All = new[]
            {
                // 1. RTS / 策略 — 俯视高空
                new VCamPreset
                {
                    Name = "RTS / 策略 (俯视)",
                    Description = "高空俯视固定角度，适合 RTS、策略、城建类游戏。大 FOV 展示全局，无旋转跟随。",
                    AccentColor = new Color(0.20f, 0.60f, 0.45f),
                    FollowOffset = new Vector3(0f, 20f, -12f),
                    XDamping = 1.5f, YDamping = 1.5f, ZDamping = 1.5f,
                    BindingMode = 4, // WorldSpace
                    HorizontalDamping = 1.0f, VerticalDamping = 1.0f,
                    DeadZoneWidth = 0.4f, DeadZoneHeight = 0.4f,
                    ScreenX = 0.5f, ScreenY = 0.5f,
                    SoftZoneWidth = 0.8f, SoftZoneHeight = 0.8f,
                    FOV = 60f, NearClip = 0.3f, FarClip = 2000f,
                },
                // 2. 俯视角动作 (ARPG / 暗黑类)
                new VCamPreset
                {
                    Name = "俯视角动作 (ARPG)",
                    Description = "45° 俯视跟随角色，适合暗黑、ARPG、Moba 类游戏。适中 FOV，响应快。",
                    AccentColor = new Color(0.70f, 0.40f, 0.15f),
                    FollowOffset = new Vector3(0f, 12f, -8f),
                    XDamping = 0.8f, YDamping = 0.8f, ZDamping = 0.8f,
                    BindingMode = 4, // WorldSpace
                    HorizontalDamping = 0.5f, VerticalDamping = 0.5f,
                    DeadZoneWidth = 0.15f, DeadZoneHeight = 0.15f,
                    ScreenX = 0.5f, ScreenY = 0.5f,
                    SoftZoneWidth = 0.7f, SoftZoneHeight = 0.7f,
                    FOV = 50f, NearClip = 0.3f, FarClip = 1000f,
                },
                // 3. 第三人称动作
                new VCamPreset
                {
                    Name = "第三人称动作 (TPA)",
                    Description = "经典第三人称背后跟随，适合动作冒险类游戏。紧密跟随角色旋转。",
                    AccentColor = new Color(0.85f, 0.30f, 0.25f),
                    FollowOffset = new Vector3(0f, 2.5f, -5f),
                    XDamping = 0.3f, YDamping = 0.5f, ZDamping = 0.5f,
                    BindingMode = 1, // LockToTargetWithWorldUp
                    HorizontalDamping = 0.3f, VerticalDamping = 0.4f,
                    DeadZoneWidth = 0.1f, DeadZoneHeight = 0.08f,
                    ScreenX = 0.5f, ScreenY = 0.55f,
                    SoftZoneWidth = 0.7f, SoftZoneHeight = 0.7f,
                    FOV = 55f, NearClip = 0.1f, FarClip = 1000f,
                },
                // 4. 第三人称射击 (TPS) — 越肩
                new VCamPreset
                {
                    Name = "第三人称射击 (TPS)",
                    Description = "越肩视角，Screen X 偏移，精准瞄准手感。适合射击/潜行类游戏。",
                    AccentColor = new Color(0.45f, 0.70f, 0.30f),
                    FollowOffset = new Vector3(1f, 2f, -4f),
                    XDamping = 0.2f, YDamping = 0.3f, ZDamping = 0.3f,
                    BindingMode = 1, // LockToTargetWithWorldUp
                    HorizontalDamping = 0.2f, VerticalDamping = 0.2f,
                    DeadZoneWidth = 0.05f, DeadZoneHeight = 0.05f,
                    ScreenX = 0.35f, ScreenY = 0.55f,
                    SoftZoneWidth = 0.6f, SoftZoneHeight = 0.6f,
                    FOV = 50f, NearClip = 0.1f, FarClip = 1000f,
                },
                // 5. 横版 / 2.5D
                new VCamPreset
                {
                    Name = "横版 / 2.5D",
                    Description = "侧面视角，相机在世界空间固定 Z 偏移。适合横版过关、平台跳跃类。",
                    AccentColor = new Color(0.95f, 0.55f, 0.10f),
                    FollowOffset = new Vector3(0f, 1.5f, -10f),
                    XDamping = 0.5f, YDamping = 0.8f, ZDamping = 0f,
                    BindingMode = 4, // WorldSpace
                    HorizontalDamping = 0.3f, VerticalDamping = 0.6f,
                    DeadZoneWidth = 0.2f, DeadZoneHeight = 0.3f,
                    ScreenX = 0.5f, ScreenY = 0.45f,
                    SoftZoneWidth = 0.8f, SoftZoneHeight = 0.8f,
                    FOV = 50f, NearClip = 0.1f, FarClip = 500f,
                },
                // 6. 电影镜头
                new VCamPreset
                {
                    Name = "电影镜头 (Cinematic)",
                    Description = "缓慢飘逸，大死区+高阻尼。适合过场动画/剧情/观景模式。",
                    AccentColor = new Color(0.60f, 0.35f, 0.70f),
                    FollowOffset = new Vector3(0f, 2f, -6f),
                    XDamping = 2f, YDamping = 2f, ZDamping = 2f,
                    BindingMode = 1, // LockToTargetWithWorldUp
                    HorizontalDamping = 1.5f, VerticalDamping = 1.5f,
                    DeadZoneWidth = 0.3f, DeadZoneHeight = 0.25f,
                    ScreenX = 0.5f, ScreenY = 0.5f,
                    SoftZoneWidth = 0.9f, SoftZoneHeight = 0.9f,
                    FOV = 45f, NearClip = 0.1f, FarClip = 1000f,
                },
            };
        }

        #endregion

        #region State

        private GameObject m_TargetObj;
        private Cinemachine.CinemachineVirtualCamera m_VCam;
        private SerializedObject m_VCamSO;
        private SerializedObject m_TransposerSO;   // Body: CinemachineTransposer
        private SerializedObject m_ComposerSO;     // Aim:  CinemachineComposer
        private Vector2 m_ScrollPos;
        private int m_SelectedPresetIndex = -1;

        private bool m_PresetFoldout = true;
        private bool m_BodyFoldout = true;
        private bool m_ComposerFoldout = true;
        private bool m_LensFoldout = true;
        private bool m_AdvancedFoldout = false;

        #endregion

        #region Styles

        private GUIStyle m_HeaderStyle;
        private GUIStyle m_SubLabelStyle;
        private GUIStyle m_PresetBtnStyle;
        private GUIStyle m_PresetDescStyle;

        private static readonly Color C_HEADER_BG = new Color(0.15f, 0.28f, 0.52f, 1f);
        private static readonly Color C_PRESET_BG = new Color(0.22f, 0.38f, 0.62f, 0.10f);
        private static readonly Color C_BODY_BG   = new Color(0.22f, 0.55f, 0.38f, 0.10f);
        private static readonly Color C_COMP_BG   = new Color(0.55f, 0.35f, 0.55f, 0.10f);
        private static readonly Color C_LENS_BG   = new Color(0.58f, 0.35f, 0.22f, 0.10f);
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

        [MenuItem("Tools/Cinemachine/VirtualCamera Config Tool", false, 202)]
        public static void OpenWindow()
        {
            var window = GetWindow<CinemachineVirtualCameraConfigWindow>("CM VCam 配置");
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

            if (m_VCam != null)
            {
                m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
                {
                    DrawPresetSection();
                    EditorGUILayout.Space(3);
                    DrawBodySection();
                    EditorGUILayout.Space(3);
                    DrawComposerSection();
                    EditorGUILayout.Space(3);
                    DrawLensSection();
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
                    "请在上方拖入或选择一个带有 CinemachineVirtualCamera 组件的 GameObject。",
                    MessageType.Info);
            }
        }

        #endregion

        #region Header & Target

        private void DrawHeader()
        {
            var rect = GUILayoutUtility.GetRect(0, 36, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, C_HEADER_BG);
            var oldColor = GUI.contentColor;
            GUI.contentColor = Color.white;
            GUI.Label(rect, "Cinemachine VirtualCamera 配置工具", m_HeaderStyle);
            GUI.contentColor = oldColor;

            var subRect = GUILayoutUtility.GetRect(0, 14, GUILayout.ExpandWidth(true));
            GUI.Label(subRect, "Cinemachine 2.10.7 · CinemachineTransposer + Composer", m_SubLabelStyle);
        }

        private void DrawTargetPicker()
        {
            EditorGUI.BeginChangeCheck();
            m_TargetObj = (GameObject)EditorGUILayout.ObjectField(
                "目标 VirtualCamera", m_TargetObj, typeof(GameObject), true);
            if (EditorGUI.EndChangeCheck() && m_TargetObj != null)
                TryResolveTarget(m_TargetObj);
        }

        private void TryResolveTarget(GameObject go)
        {
            m_VCam = go.GetComponent<Cinemachine.CinemachineVirtualCamera>();
            if (m_VCam == null) m_VCam = go.GetComponentInChildren<Cinemachine.CinemachineVirtualCamera>();
            if (m_VCam != null)
            {
                m_TargetObj = m_VCam.gameObject;
                m_VCamSO = new SerializedObject(m_VCam);

                // Resolve Body (Transposer)
                var transposer = m_VCam.GetCinemachineComponent<Cinemachine.CinemachineTransposer>();
                m_TransposerSO = transposer != null ? new SerializedObject(transposer) : null;

                // Resolve Aim (Composer)
                var composer = m_VCam.GetCinemachineComponent<Cinemachine.CinemachineComposer>();
                m_ComposerSO = composer != null ? new SerializedObject(composer) : null;
            }
            else
            {
                m_TargetObj = null;
                m_VCamSO = null;
                m_TransposerSO = null;
                m_ComposerSO = null;
            }
        }

        #endregion

        #region Preset Section

        private void DrawPresetSection()
        {
            BeginSection("游戏类型预设", ref m_PresetFoldout, C_PRESET_BG);
            if (m_PresetFoldout)
            {
                EditorGUILayout.LabelField("选择一种游戏类型，一键应用推荐参数：",
                    EditorStyles.wordWrappedMiniLabel);
                EditorGUILayout.Space(4);

                var presets = VCamPreset.All;
                int columns = 2;
                for (int i = 0; i < presets.Length; i += columns)
                {
                    EditorGUILayout.BeginHorizontal();
                    for (int c = 0; c < columns && i + c < presets.Length; c++)
                    {
                        int idx = i + c;
                        var p = presets[idx];
                        var bgColor = GUI.backgroundColor;
                        GUI.backgroundColor = p.AccentColor;
                        if (GUILayout.Button(new GUIContent(p.Name, p.Description), m_PresetBtnStyle, GUILayout.Height(40)))
                        {
                            m_SelectedPresetIndex = idx;
                            ApplyPreset(p);
                        }
                        GUI.backgroundColor = bgColor;
                    }
                    EditorGUILayout.EndHorizontal();
                }

                // 描述
                if (m_SelectedPresetIndex >= 0 && m_SelectedPresetIndex < presets.Length)
                {
                    EditorGUILayout.Space(4);
                    var descRect = GUILayoutUtility.GetRect(0, 38, GUILayout.ExpandWidth(true));
                    EditorGUI.DrawRect(descRect, new Color(0.15f, 0.15f, 0.18f, 0.8f));
                    GUI.Label(descRect, presets[m_SelectedPresetIndex].Description, m_PresetDescStyle);
                }
            }
            EndSection();
        }

        private void ApplyPreset(VCamPreset p)
        {
            Undo.RecordObject(m_VCam, "Apply VCam Preset");

            // Body (Transposer)
            var transposer = m_VCam.GetCinemachineComponent<Cinemachine.CinemachineTransposer>();
            if (transposer != null)
            {
                Undo.RecordObject(transposer, "Apply VCam Preset - Body");
                if (m_TransposerSO != null)
                {
                    m_TransposerSO.Update();
                    SetProp(m_TransposerSO, "m_FollowOffset", p.FollowOffset);
                    SetFloat(m_TransposerSO, "m_XDamping", p.XDamping);
                    SetFloat(m_TransposerSO, "m_YDamping", p.YDamping);
                    SetFloat(m_TransposerSO, "m_ZDamping", p.ZDamping);
                    SetInt(m_TransposerSO, "m_BindingMode", p.BindingMode);
                    m_TransposerSO.ApplyModifiedProperties();
                }
            }

            // Aim (Composer)
            var composer = m_VCam.GetCinemachineComponent<Cinemachine.CinemachineComposer>();
            if (composer != null)
            {
                Undo.RecordObject(composer, "Apply VCam Preset - Aim");
                if (m_ComposerSO != null)
                {
                    m_ComposerSO.Update();
                    SetFloat(m_ComposerSO, "m_HorizontalDamping", p.HorizontalDamping);
                    SetFloat(m_ComposerSO, "m_VerticalDamping", p.VerticalDamping);
                    SetFloat(m_ComposerSO, "m_DeadZoneWidth", p.DeadZoneWidth);
                    SetFloat(m_ComposerSO, "m_DeadZoneHeight", p.DeadZoneHeight);
                    SetFloat(m_ComposerSO, "m_ScreenX", p.ScreenX);
                    SetFloat(m_ComposerSO, "m_ScreenY", p.ScreenY);
                    SetFloat(m_ComposerSO, "m_SoftZoneWidth", p.SoftZoneWidth);
                    SetFloat(m_ComposerSO, "m_SoftZoneHeight", p.SoftZoneHeight);
                    m_ComposerSO.ApplyModifiedProperties();
                }
            }

            // Lens
            if (m_VCamSO != null)
            {
                m_VCamSO.Update();
                SetFloat(m_VCamSO, "m_Lens.FieldOfView", p.FOV);
                SetFloat(m_VCamSO, "m_Lens.NearClipPlane", p.NearClip);
                SetFloat(m_VCamSO, "m_Lens.FarClipPlane", p.FarClip);
                m_VCamSO.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(m_VCam);
            Repaint();
        }

        #endregion

        #region Body Section

        private void DrawBodySection()
        {
            BeginSection("Body (Transposer)", ref m_BodyFoldout, C_BODY_BG);
            if (m_BodyFoldout)
            {
                if (m_TransposerSO == null)
                {
                    EditorGUILayout.HelpBox(
                        "未检测到 CinemachineTransposer 组件。请确认 VirtualCamera 的 Body 设为 Transposer。",
                        MessageType.Warning);
                }
                else
                {
                    m_TransposerSO.Update();

                    var offsetProp = m_TransposerSO.FindProperty("m_FollowOffset");
                    if (offsetProp != null)
                        DrawPropertyWithHelp(offsetProp, "Follow Offset", "跟随偏移 (X,Y,Z)", "Follow Offset");

                    EditorGUILayout.Space(2);
                    DrawPropertyWithHelp(m_TransposerSO.FindProperty("m_XDamping"),
                        "X Damping", "X 方向跟随阻尼", "X Damping");
                    DrawPropertyWithHelp(m_TransposerSO.FindProperty("m_YDamping"),
                        "Y Damping", "Y 方向跟随阻尼", "Y Damping");
                    DrawPropertyWithHelp(m_TransposerSO.FindProperty("m_ZDamping"),
                        "Z Damping", "Z 方向跟随阻尼", "Z Damping");

                    m_TransposerSO.ApplyModifiedProperties();
                }
            }
            EndSection();
        }

        #endregion

        #region Composer Section

        private void DrawComposerSection()
        {
            BeginSection("Aim (Composer)", ref m_ComposerFoldout, C_COMP_BG);
            if (m_ComposerFoldout)
            {
                if (m_ComposerSO == null)
                {
                    EditorGUILayout.HelpBox(
                        "未检测到 CinemachineComposer 组件。请确认 VirtualCamera 的 Aim 设为 Composer。",
                        MessageType.Warning);
                }
                else
                {
                    m_ComposerSO.Update();

                    DrawPropertyWithHelp(m_ComposerSO.FindProperty("m_HorizontalDamping"),
                        "Horizontal Damping", "水平方向跟随目标的阻尼", "Horizontal Damping");
                    DrawPropertyWithHelp(m_ComposerSO.FindProperty("m_VerticalDamping"),
                        "Vertical Damping", "垂直方向跟随目标的阻尼", "Vertical Damping");

                    EditorGUILayout.Space(2);
                    DrawPropertyWithHelp(m_ComposerSO.FindProperty("m_DeadZoneWidth"),
                        "死区宽度", "屏幕中心水平不触发相机移动范围", "Dead Zone Width");
                    DrawPropertyWithHelp(m_ComposerSO.FindProperty("m_DeadZoneHeight"),
                        "死区高度", "屏幕中心垂直不触发相机移动范围", "Dead Zone Height");

                    EditorGUILayout.Space(2);
                    DrawPropertyWithHelp(m_ComposerSO.FindProperty("m_ScreenX"),
                        "注视点 X", "屏幕上目标位置 X", "Screen X");
                    DrawPropertyWithHelp(m_ComposerSO.FindProperty("m_ScreenY"),
                        "注视点 Y", "屏幕上目标位置 Y", "Screen Y");

                    EditorGUILayout.Space(2);
                    DrawPropertyWithHelp(m_ComposerSO.FindProperty("m_SoftZoneWidth"),
                        "Soft Zone 宽", "软区域宽度", "Soft Zone Width");
                    DrawPropertyWithHelp(m_ComposerSO.FindProperty("m_SoftZoneHeight"),
                        "Soft Zone 高", "软区域高度", "Soft Zone Height");

                    m_ComposerSO.ApplyModifiedProperties();
                }
            }
            EndSection();
        }

        #endregion

        #region Lens Section

        private void DrawLensSection()
        {
            BeginSection("Lens (镜头)", ref m_LensFoldout, C_LENS_BG);
            if (m_LensFoldout)
            {
                if (m_VCamSO != null)
                {
                    m_VCamSO.Update();

                    DrawPropertyWithHelp(m_VCamSO.FindProperty("m_Lens.FieldOfView"),
                        "FOV", "视场角（垂直方向角度）", "FOV");
                    DrawPropertyWithHelp(m_VCamSO.FindProperty("m_Lens.NearClipPlane"),
                        "Near Clip", "近裁剪面距离", "Near Clip");
                    DrawPropertyWithHelp(m_VCamSO.FindProperty("m_Lens.FarClipPlane"),
                        "Far Clip", "远裁剪面距离", "Far Clip");

                    m_VCamSO.ApplyModifiedProperties();
                }
            }
            EndSection();
        }

        #endregion

        #region Advanced Section

        private void DrawAdvancedSection()
        {
            BeginSection("高级设置", ref m_AdvancedFoldout, C_ADV_BG);
            if (m_AdvancedFoldout)
            {
                if (m_TransposerSO != null)
                {
                    m_TransposerSO.Update();
                    DrawPropertyWithHelp(m_TransposerSO.FindProperty("m_BindingMode"),
                        "Binding Mode", "绑定模式", "Binding Mode");
                    m_TransposerSO.ApplyModifiedProperties();
                }
                else
                {
                    EditorGUILayout.HelpBox("Binding Mode 需要 Transposer 组件。", MessageType.Info);
                }
            }
            EndSection();
        }

        #endregion

        #region Action Bar

        private void DrawActionBar()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping 目标", GUILayout.Height(24)))
            {
                if (m_VCam != null)
                    EditorGUIUtility.PingObject(m_VCam.gameObject);
            }
            if (GUILayout.Button("重置为默认", GUILayout.Height(24)))
            {
                if (EditorUtility.DisplayDialog("重置确认",
                    "是否将所有参数重置为 Cinemachine 默认值？此操作可通过 Ctrl+Z 撤销。", "重置", "取消"))
                {
                    ApplyPreset(new VCamPreset
                    {
                        FollowOffset = new Vector3(0, 2.5f, -5f),
                        XDamping = 1f, YDamping = 1f, ZDamping = 1f,
                        BindingMode = 1,
                        HorizontalDamping = 0.5f, VerticalDamping = 0.5f,
                        DeadZoneWidth = 0f, DeadZoneHeight = 0f,
                        ScreenX = 0.5f, ScreenY = 0.5f,
                        SoftZoneWidth = 0.8f, SoftZoneHeight = 0.8f,
                        FOV = 60f, NearClip = 0.1f, FarClip = 1000f,
                    });
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        #endregion

        #region Section Helpers

        private void BeginSection(string title, ref bool foldout, Color bgColor)
        {
            var rect = GUILayoutUtility.GetRect(0, 22, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(rect, bgColor);
            var oldColor = GUI.contentColor;
            GUI.contentColor = new Color(1f, 1f, 1f, 0.95f);
            foldout = EditorGUI.Foldout(rect, foldout, " " + title, true,
                new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold, fontSize = 12 });
            GUI.contentColor = oldColor;
            if (foldout) EditorGUI.indentLevel++;
        }

        private void EndSection()
        {
            EditorGUI.indentLevel = Mathf.Max(0, EditorGUI.indentLevel - 1);
            EditorGUILayout.Space(2);
        }

        #endregion

        #region Property Helpers

        /// <summary>绘制属性字段 + ? 帮助按钮</summary>
        private void DrawPropertyWithHelp(SerializedProperty prop, string label, string tooltip, string helpKey)
        {
            if (prop == null) return;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prop, new GUIContent(label, tooltip));
            if (GUILayout.Button("?", GUILayout.Width(22), GUILayout.Height(18)))
            {
                if (s_HelpTexts.TryGetValue(helpKey, out string helpText))
                    EditorUtility.DisplayDialog($"帮助 — {label}", helpText, "确定");
                else
                    EditorUtility.DisplayDialog($"帮助 — {label}", tooltip, "确定");
            }
            EditorGUILayout.EndHorizontal();
        }

        private static void SetFloat(SerializedObject so, string path, float value)
        {
            var prop = so.FindProperty(path);
            if (prop != null) prop.floatValue = value;
        }

        private static void SetInt(SerializedObject so, string path, int value)
        {
            var prop = so.FindProperty(path);
            if (prop != null) prop.intValue = value;
        }

        private static void SetProp(SerializedObject so, string path, Vector3 value)
        {
            var prop = so.FindProperty(path);
            if (prop != null) prop.vector3Value = value;
        }

        #endregion

        #region Help Texts

        private static readonly Dictionary<string, string> s_HelpTexts = new Dictionary<string, string>
        {
            // ====== Body / Transposer ======
            ["Follow Offset"] =
                "【Follow Offset — 跟随偏移】\n\n" +
                "作用：相机相对于跟随目标的位置偏移（局部坐标或世界坐标取决于 Binding Mode）。\n\n" +
                "三个分量：\n" +
                "• X — 水平偏移。正值 = 右偏，适合越肩视角\n" +
                "• Y — 垂直偏移。正值 = 抬高相机，俯视效果\n" +
                "• Z — 前后偏移。负值 = 相机在目标后方\n\n" +
                "典型配置：\n" +
                "• 第三人称：(0, 2.5, -5)\n" +
                "• 越肩(TPS)：(1, 2, -4)\n" +
                "• RTS俯视：(0, 20, -12)\n" +
                "• ARPG 45°：(0, 12, -8)\n\n" +
                "建议：Y 和 Z 的比值决定俯仰角度。Y/Z ≈ tan(俯角)。",

            ["X Damping"] =
                "【X Damping — X 方向跟随阻尼】\n\n" +
                "作用：目标在 X 方向移动时，相机跟随的延迟程度。\n" +
                "取值范围：0 ~ 20\n" +
                "• 0 = 无延迟，立即跟随\n" +
                "• 较大值 → 相机跟随更慢，有「漂浮」感\n\n" +
                "建议：动作游戏 0.2-0.5，RPG 0.5-1.5，RTS 1-3。",

            ["Y Damping"] =
                "【Y Damping — Y 方向跟随阻尼】\n\n" +
                "作用：目标在垂直方向移动时（如跳跃），相机跟随的延迟。\n" +
                "取值范围：0 ~ 20\n" +
                "• 0 = 垂直方向立即跟随\n" +
                "• 较大值 → 跳跃时相机不会剧烈抖动\n\n" +
                "建议：平台跳跃 0.5-1.0（避免频繁抖动），动作 0.3-0.5，RTS 1-2。",

            ["Z Damping"] =
                "【Z Damping — Z 方向跟随阻尼】\n\n" +
                "作用：目标在前后方向移动时，相机跟随的延迟。\n" +
                "取值范围：0 ~ 20\n" +
                "• 0 = 前后方向无延迟\n" +
                "• 较大值 → 加速/减速时相机有滞后感\n\n" +
                "建议：一般与 X Damping 保持接近。横版游戏可设 0（锁定 Z 距离）。",

            // ====== Aim / Composer ======
            ["Horizontal Damping"] =
                "【Horizontal Damping — 水平跟随阻尼】\n\n" +
                "作用：目标在屏幕水平方向移动时，相机重新对齐的延迟程度。\n" +
                "取值范围：0 ~ 20\n" +
                "• 0 = 立即跟随，无延迟\n" +
                "• 较大值 → 相机跟随滞后，有\"悬浮\"感\n\n" +
                "建议：动作游戏 0.2-0.5，探索类 0.5-2，电影 1-3。\n" +
                "需配合 Dead Zone 一起调，Dead Zone 内不触发阻尼跟随。",

            ["Vertical Damping"] =
                "【Vertical Damping — 垂直跟随阻尼】\n\n" +
                "作用：目标在屏幕垂直方向移动时，相机重新对齐的延迟程度。\n" +
                "取值范围：0 ~ 20\n" +
                "• 0 = 立即跟随\n" +
                "• 较大值 → 垂直方向跟随更慢\n\n" +
                "建议：通常与 Horizontal Damping 保持接近或略大，\n" +
                "跳跃频繁的游戏可稍大以避免相机剧烈抖动。",

            ["Dead Zone Width"] =
                "【Dead Zone Width — 死区宽度】\n\n" +
                "作用：屏幕中心的水平区域，目标在此范围内移动不会触发相机跟随。\n" +
                "取值范围：0 ~ 2（比例值，0=无死区，1=满屏宽）\n\n" +
                "效果：\n" +
                "• 0 → 目标稍微移动相机就跟随，操控精确但可能晃动\n" +
                "• 0.1-0.3 → 有一定容忍度，角色小幅移动时相机稳定\n\n" +
                "建议：TPS 0-0.05，动作 0.05-0.15，RPG 0.1-0.2，RTS 0.3-0.5。",

            ["Dead Zone Height"] =
                "【Dead Zone Height — 死区高度】\n\n" +
                "作用：屏幕中心的垂直区域，目标在此范围内上下移动不触发相机跟随。\n" +
                "取值范围：0 ~ 2（比例值）\n\n" +
                "效果：\n" +
                "• 0 → 垂直方向立即跟随\n" +
                "• 较大值 → 角色跳跃或上下移动时相机不跟着抖\n\n" +
                "建议：平台跳跃 0.2-0.5，动作 0.05-0.15，RTS 0.3-0.5。",

            ["Screen X"] =
                "【Screen X — 注视点水平位置】\n\n" +
                "作用：目标在屏幕上的期望水平位置。0.5 = 屏幕正中。\n" +
                "取值范围：-0.5 ~ 1.5\n" +
                "• 0.5 = 目标居中\n" +
                "• < 0.5 → 目标偏左，右侧空间更大\n" +
                "• > 0.5 → 目标偏右，左侧空间更大\n\n" +
                "建议：大多数游戏保持 0.5。TPS 越肩视角设 0.3-0.4。",

            ["Screen Y"] =
                "【Screen Y — 注视点垂直位置】\n\n" +
                "作用：目标在屏幕上的期望垂直位置。0.5 = 屏幕正中。\n" +
                "取值范围：-0.5 ~ 1.5\n" +
                "• 0.5 = 目标居中\n" +
                "• < 0.5 → 目标偏下，看到更多天空/上方\n" +
                "• > 0.5 → 目标偏上，看到更多地面/下方\n\n" +
                "建议：第三人称 0.55-0.65（角色略偏下），RTS 0.5。",

            ["Soft Zone Width"] =
                "【Soft Zone Width — 软区域宽度】\n\n" +
                "作用：在死区之外、硬边界之内的水平区域。目标进入此区域时，相机按阻尼\n" +
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

            // ====== Lens ======
            ["FOV"] =
                "【Field of View — 视场角】\n\n" +
                "作用：控制镜头视野宽度（垂直方向角度）。\n" +
                "取值范围：1° ~ 179°\n" +
                "• 较小值（如 30-40）→ 长焦效果，背景压缩，角色突出\n" +
                "• 较大值（如 70-90）→ 广角效果，视野开阔但有畸变\n\n" +
                "典型配置：\n" +
                "• RTS：55-65（展示大场景）\n" +
                "• ARPG 俯视：45-55\n" +
                "• 第三人称：50-60\n" +
                "• TPS：45-55（更聚焦）\n" +
                "• 横版：45-55",

            ["Near Clip"] =
                "【Near Clip Plane — 近裁剪面】\n\n" +
                "作用：距相机小于此距离的物体不渲染。\n" +
                "取值范围：> 0（通常 0.01 ~ 1）\n\n" +
                "注意：值太小会导致深度精度不足（Z-fighting），值太大会裁掉近处物体。\n" +
                "建议：一般设 0.1 ~ 0.3，大型开放世界/RTS 可设 0.3-0.5。",

            ["Far Clip"] =
                "【Far Clip Plane — 远裁剪面】\n\n" +
                "作用：距相机大于此距离的物体不渲染。\n" +
                "取值范围：> Near Clip（通常 100 ~ 5000）\n\n" +
                "注意：与 Near Clip 的比值越大深度精度越差。\n" +
                "建议：室内 100-500，第三人称 500-1000，RTS/开放世界 1000-2000+。",

            // ====== Advanced ======
            ["Binding Mode"] =
                "【Binding Mode — 绑定模式】\n\n" +
                "作用：决定 Transposer 在哪个坐标空间中计算 Follow Offset。\n\n" +
                "常用选项：\n" +
                "• LockToTargetOnAssign (0) — 初始锁定，之后世界空间\n" +
                "• LockToTargetWithWorldUp (1) — 跟随目标旋转，保持世界 Up（第三人称推荐）\n" +
                "• LockToTarget (2) — 完全锁定目标坐标系\n" +
                "• SimpleFollowWithWorldUp (3) — 简单跟随\n" +
                "• WorldSpace (4) — 世界空间固定偏移（RTS/俯视推荐）\n\n" +
                "建议：\n" +
                "• RTS/俯视/横版 → WorldSpace\n" +
                "• 第三人称/TPS → LockToTargetWithWorldUp\n" +
                "• 电影镜头 → LockToTargetWithWorldUp 或 LockToTarget",
        };

        #endregion
    }
}
#endif
