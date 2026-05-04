#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityTimeline;

namespace SkillCharacterControllerEditor
{
    [CustomEditor(typeof(SkillCharacterController)), CanEditMultipleObjects]
    public class SkillCharacterControllerEditor : Editor
    {
        #region Serialized Properties

        private SerializedProperty _moveActionProp;
        private SerializedProperty _jumpActionProp;
        private SerializedProperty _orientationReferenceProp;

        private SerializedProperty _groundRootMotionModeProp;
        private SerializedProperty _airRootMotionModeProp;

        private SerializedProperty _jumpModeProp;
        private SerializedProperty _jumpUpSpeedProp;
        private SerializedProperty _jumpScalableForwardSpeedProp;
        private SerializedProperty _jumpPreGroundingGraceTimeProp;
        private SerializedProperty _jumpPostGroundingGraceTimeProp;
        private SerializedProperty _allowJumpingWhenSlidingProp;

        private SerializedProperty _maxStableMoveSpeedProp;
        private SerializedProperty _stableMovementSharpnessProp;
        private SerializedProperty _orientationSharpnessProp;
        private SerializedProperty _orientationMethodProp;
        private SerializedProperty _rotationLockAngleProp;

        private SerializedProperty _maxAirMoveSpeedProp;
        private SerializedProperty _airAccelerationSpeedProp;
        private SerializedProperty _dragProp;

        private SerializedProperty _gravityProp;
        private SerializedProperty _meshRootProp;
        private SerializedProperty _ignoredCollidersProp;

        #endregion

        #region Foldout State

        private bool _inputFoldout = true;
        private bool _rootMotionFoldout = true;
        private bool _jumpFoldout = true;
        private bool _stableFoldout = true;
        private bool _airFoldout = true;
        private bool _miscFoldout = false;
        private bool _compFoldout = true;

        #endregion

        #region Styles

        private GUIStyle _headerLabelStyle;
        private GUIStyle _subLabelStyle;
        private GUIStyle _foldoutBoldStyle;
        private GUIStyle _modeDescStyle;

        private static readonly Color C_HEADER_BG  = new Color(0.12f, 0.38f, 0.62f, 1f);
        private static readonly Color C_INPUT      = new Color(0.18f, 0.42f, 0.72f, 0.10f);
        private static readonly Color C_ROOTMOTION = new Color(0.52f, 0.22f, 0.58f, 0.10f);
        private static readonly Color C_JUMP       = new Color(0.18f, 0.52f, 0.28f, 0.10f);
        private static readonly Color C_MOVEMENT   = new Color(0.68f, 0.42f, 0.12f, 0.10f);
        private static readonly Color C_MISC       = new Color(0.42f, 0.42f, 0.42f, 0.10f);
        private static readonly Color C_COMP       = new Color(0.62f, 0.18f, 0.32f, 0.10f);

        private SkillCharacterController _ctrl;

        #endregion

        #region OnEnable / OnInspectorGUI

        private void OnEnable()
        {
            _ctrl = (SkillCharacterController)target;

            _moveActionProp           = serializedObject.FindProperty("_moveAction");
            _jumpActionProp           = serializedObject.FindProperty("_jumpAction");
            _orientationReferenceProp  = serializedObject.FindProperty("_orientationReference");

            _groundRootMotionModeProp  = serializedObject.FindProperty("_groundRootMotionMode");
            _airRootMotionModeProp     = serializedObject.FindProperty("_airRootMotionMode");

            _jumpModeProp              = serializedObject.FindProperty("_jumpMode");
            _jumpUpSpeedProp           = serializedObject.FindProperty("_jumpUpSpeed");
            _jumpScalableForwardSpeedProp = serializedObject.FindProperty("_jumpScalableForwardSpeed");
            _jumpPreGroundingGraceTimeProp  = serializedObject.FindProperty("_jumpPreGroundingGraceTime");
            _jumpPostGroundingGraceTimeProp = serializedObject.FindProperty("_jumpPostGroundingGraceTime");
            _allowJumpingWhenSlidingProp   = serializedObject.FindProperty("_allowJumpingWhenSliding");

            _maxStableMoveSpeedProp     = serializedObject.FindProperty("_maxStableMoveSpeed");
            _stableMovementSharpnessProp = serializedObject.FindProperty("_stableMovementSharpness");
            _orientationSharpnessProp   = serializedObject.FindProperty("_orientationSharpness");
            _orientationMethodProp      = serializedObject.FindProperty("_orientationMethod");
            _rotationLockAngleProp      = serializedObject.FindProperty("_rotationLockAngle");

            _maxAirMoveSpeedProp        = serializedObject.FindProperty("_maxAirMoveSpeed");
            _airAccelerationSpeedProp   = serializedObject.FindProperty("_airAccelerationSpeed");
            _dragProp                   = serializedObject.FindProperty("_drag");

            _gravityProp                = serializedObject.FindProperty("_gravity");
            _meshRootProp               = serializedObject.FindProperty("_meshRoot");
            _ignoredCollidersProp       = serializedObject.FindProperty("_ignoredColliders");

        }

        public override void OnInspectorGUI()
        {
            if (_headerLabelStyle == null)
                InitStyles();

            serializedObject.Update();

            DrawHeaderBar();
            EditorGUILayout.Space(3);

            DrawInputSection();
            DrawRootMotionSection();
            DrawJumpSection();
            DrawStableSection();
            DrawAirSection();
            DrawMiscSection();
            DrawCompensationSection();

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Style Init

        private void InitStyles()
        {
            _headerLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };

            _subLabelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel) { fontSize = 9 };
            _foldoutBoldStyle = new GUIStyle(EditorStyles.foldout)
            { fontStyle = FontStyle.Bold, fontSize = 11 };
            _modeDescStyle = new GUIStyle(EditorStyles.label)
            { fontSize = 10, fontStyle = FontStyle.Italic,
              normal = { textColor = new Color(0.65f, 0.65f, 0.65f) } };
        }

        #endregion

        #region Header

        private void DrawHeaderBar()
        {
            var r = GUILayoutUtility.GetRect(1, 42);
            EditorGUI.DrawRect(r, C_HEADER_BG);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(8);
                EditorGUI.DrawTextureTransparent(GUILayoutUtility.GetRect(22, 22),
                    EditorGUIUtility.IconContent("d_Animation Icon").image);
                using (new GUILayout.VerticalScope())
                {
                    GUILayout.Label("Skill Character Controller", _headerLabelStyle);
                    GUILayout.Label("KCC + InputSystem + RootMotion", _subLabelStyle);
                }
                GUILayout.FlexibleSpace();

                GUI.backgroundColor = Application.isPlaying
                    ? new Color(0.2f, 0.75f, 0.25f)
                    : new Color(0.5f, 0.5f, 0.5f);
                GUILayout.Button(Application.isPlaying ? "Running" : "Editing",
                    GUILayout.Width(64), GUILayout.Height(22));
                GUI.backgroundColor = Color.white;
                GUILayout.Space(8);
            }
        }

        #endregion

        #region Section: Input

        private void DrawInputSection()
        {
            BeginSection(C_INPUT, ref _inputFoldout,
                "Input Settings", "\u8F93\u5165\u7CFB\u7EDF\u914D\u7F6E");

            if (_inputFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPropertyWithHelp(_moveActionProp,
                    new GUIContent("Move Action", "\u79FB\u52A8\u8F93\u5165 Action (Vector2)\uFF0C\u4ECE Input Action Asset \u62D6\u5165"),
                    "Move Action - \u79FB\u52A8\u8F93\u5165",
                    "\u3010\u4F5C\u7528\u3011\u7ED1\u5B9A InputSystem \u4E2D\u7684 Move Action\uFF0C\u7C7B\u578B\u4E3A Vector2\u3002\n" +
                    "\u63A7\u5236\u89D2\u8272\u7684\u5730\u9762/\u7A7A\u4E2D\u79FB\u52A8\u65B9\u5411\u3002\n\n" +
                    "\u3010\u4F7F\u7528\u65B9\u6CD5\u3011\n" +
                    "\u4ECE Input Action Asset \u4E2D\u62D6\u5165\u5BF9\u5E94\u7684 Move Action Reference\u3002\n" +
                    "\u901A\u5E38\u7ED1\u5B9A\u6447\u6746/WASD\uFF0C\u8F93\u51FA\u4E3A\u5F52\u4E00\u5316\u7684\u4E8C\u7EF4\u5411\u91CF\u3002\n\n" +
                    "\u3010\u6CE8\u610F\u3011\n" +
                    "\u5982\u679C\u672A\u7ED1\u5B9A\uFF0C\u89D2\u8272\u5C06\u65E0\u6CD5\u79FB\u52A8\u3002");

                // 运行时显示 MoveAction 当前 Vector2 值
                if (Application.isPlaying && _ctrl != null
                    && _ctrl.MoveAction != null && _ctrl.MoveAction.action != null)
                {
                    using (new EditorGUI.DisabledGroupScope(true))
                    {
                        Vector2 moveValue = _ctrl.MoveAction.action.ReadValue<Vector2>();
                        EditorGUILayout.Vector2Field("  Move Value (Runtime)", moveValue);
                    }
                    Repaint();
                }

                DrawPropertyWithHelp(_jumpActionProp,
                    new GUIContent("Jump Action", "\u8DF3\u8DC3\u8F93\u5165 Action (Button)\uFF0C\u4ECE Input Action Asset \u62D6\u5165"),
                    "Jump Action - \u8DF3\u8DC3\u8F93\u5165",
                    "\u3010\u4F5C\u7528\u3011\u7ED1\u5B9A InputSystem \u4E2D\u7684 Jump Action\uFF0C\u7C7B\u578B\u4E3A Button\u3002\n" +
                    "\u89E6\u53D1\u89D2\u8272\u8DF3\u8DC3\u64CD\u4F5C\u3002\n\n" +
                    "\u3010\u4F7F\u7528\u65B9\u6CD5\u3011\n" +
                    "\u4ECE Input Action Asset \u4E2D\u62D6\u5165\u5BF9\u5E94\u7684 Jump Action Reference\u3002\n" +
                    "\u901A\u5E38\u7ED1\u5B9A\u7A7A\u683C\u952E/\u624B\u67C4A\u952E\u3002\n\n" +
                    "\u3010\u6CE8\u610F\u3011\n" +
                    "\u5982\u679C\u672A\u7ED1\u5B9A\uFF0C\u89D2\u8272\u5C06\u65E0\u6CD5\u8DF3\u8DC3\u3002\n" +
                    "\u8DF3\u8DC3\u884C\u4E3A\u8FD8\u53D6\u51B3\u4E8E Jump Mode \u7684\u8BBE\u7F6E\u3002");

                DrawPropertyWithHelp(_orientationReferenceProp,
                    new GUIContent("Orientation Reference",
                        "\u65B9\u5411\u53C2\u7167\u7269 Transform\uFF08\u901A\u5E38\u4E3A\u76F8\u673A\uFF09\uFF0C\u4E3A\u7A7A\u65F6\u4F7F\u7528\u89D2\u8272\u81EA\u8EAB\u671D\u5411"),
                    "Orientation Reference - \u65B9\u5411\u53C2\u7167",
                    "\u3010\u4F5C\u7528\u3011\u63D0\u4F9B\u79FB\u52A8\u65B9\u5411\u7684\u53C2\u7167\u5750\u6807\u7CFB\u3002\n" +
                    "\u89D2\u8272\u7684\u201C\u524D\u8FDB\u201D\u65B9\u5411\u5C06\u57FA\u4E8E\u6B64 Transform \u7684\u671D\u5411\u3002\n\n" +
                    "\u3010\u5178\u578B\u7528\u6CD5\u3011\n" +
                    "\u2022 \u62D6\u5165\u76F8\u673A\uFF1A\u89D2\u8272\u79FB\u52A8\u65B9\u5411\u8DDF\u968F\u76F8\u673A\u671D\u5411\uFF08\u7B2C\u4E09\u4EBA\u79F0\u89C6\u89D2\uFF09\n" +
                    "\u2022 \u7559\u7A7A\uFF1A\u89D2\u8272\u79FB\u52A8\u57FA\u4E8E\u81EA\u8EAB forward \u65B9\u5411\n\n" +
                    "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                    "\u7B2C\u4E09\u4EBA\u79F0\u6E38\u620F\u901A\u5E38\u62D6\u5165\u4E3B\u76F8\u673A\uFF0C\u8FD9\u6837\u201C\u524D\u201D\u5C31\u662F\u5C4F\u5E55\u5185\u7684\u524D\u65B9\u3002");

                if (_orientationReferenceProp.objectReferenceValue == null && !Application.isPlaying)
                    EditorGUILayout.HelpBox(
                        "\u672A\u8BBE\u7F6E Orientation Reference \u65F6\uFF0C\u79FB\u52A8\u671D\u5411\u5C06\u57FA\u4E8E\u89D2\u8272\u81EA\u8EAB forward\u3002" +
                        "\u5982\u9700\u76F8\u673A\u8DDF\u968F\u65B9\u5411\uFF0C\u8BF7\u62D6\u5165\u76F8\u673A GameObject\u3002",
                        MessageType.Info);
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Section: Root Motion

        private void DrawRootMotionSection()
        {
            BeginSection(C_ROOTMOTION, ref _rootMotionFoldout,
                "Root Motion Mode", "\u5730\u9762/\u7A7A\u4E2D\u72EC\u7ACB\u914D\u7F6E\u52A8\u753B\u9A71\u52A8\u6A21\u5F0F");

            if (_rootMotionFoldout)
            {
                EditorGUI.indentLevel++;

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawPropertyWithHelp(_groundRootMotionModeProp,
                        new GUIContent("Ground Mode", "\u5730\u9762\u65F6\u7684 RootMotion \u5904\u7406\u65B9\u5F0F"),
                        "Ground Mode - \u5730\u9762 RootMotion \u6A21\u5F0F",
                        "\u3010\u4F5C\u7528\u3011\u63A7\u5236\u89D2\u8272\u5728\u5730\u9762\u65F6\u5982\u4F55\u5904\u7406\u52A8\u753B\u7684 Root Motion \u6570\u636E\u3002\n\n" +
                        "\u3010\u6A21\u5F0F\u8BF4\u660E\u3011\n" +
                        "\u2022 FullRootMotion\uFF1A\u5B8C\u5168\u7531\u52A8\u753B\u9A71\u52A8\u4F4D\u79FB\u548C\u65CB\u8F6C\uFF0C\u8F93\u5165\u4E0D\u63A7\u5236\u901F\u5EA6\u3002\n" +
                        "  \u9002\u5408\uFF1A\u52A8\u4F5C\u6E38\u620F\u3001\u9700\u8981\u7CBE\u786E\u5339\u914D\u52A8\u753B\u8DDD\u79BB\u7684\u573A\u666F\u3002\n\n" +
                        "\u2022 IgnoreRootMotion\uFF1A\u5FFD\u7565\u52A8\u753B\u4F4D\u79FB/\u65CB\u8F6C\uFF0C\u7528\u8F93\u5165\u63A7\u5236\u901F\u5EA6\u548C\u671D\u5411\u3002\n" +
                        "  \u9002\u5408\uFF1A\u6838\u5FC3\u73A9\u6CD5\u9700\u8981\u7CBE\u786E\u63A7\u5236\u79FB\u52A8\u901F\u5EA6\u7684\u573A\u666F\u3002\n\n" +
                        "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                        "\u5982\u679C\u52A8\u753B\u8D44\u6E90\u5305\u542B\u8DB3\u90E8\u4F4D\u79FB\u6570\u636E\uFF0C\u7528 FullRootMotion \u6548\u679C\u6700\u597D\u3002\n" +
                        "\u5982\u679C\u52A8\u753B\u662F\u539F\u5730\u64AD\u653E\uFF0C\u9009 IgnoreRootMotion \u5E76\u914D\u5408 Stable Movement \u53C2\u6570\u3002");
                    GUILayout.Label(DescRM((RootMotionMode)_groundRootMotionModeProp.enumValueIndex, true), _modeDescStyle);
                }
                EditorGUILayout.Space(2);

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawPropertyWithHelp(_airRootMotionModeProp,
                        new GUIContent("Air Mode", "\u7A7A\u4E2D\u65F6\u7684 RootMotion \u5904\u7406\u65B9\u5F0F"),
                        "Air Mode - \u7A7A\u4E2D RootMotion \u6A21\u5F0F",
                        "\u3010\u4F5C\u7528\u3011\u63A7\u5236\u89D2\u8272\u5728\u7A7A\u4E2D\u65F6\u5982\u4F55\u5904\u7406\u52A8\u753B\u7684 Root Motion \u6570\u636E\u3002\n\n" +
                        "\u3010\u6A21\u5F0F\u8BF4\u660E\u3011\n" +
                        "\u2022 FullRootMotion\uFF1A\u7A7A\u4E2D\u65CB\u8F6C\u7531\u52A8\u753B\u9A71\u52A8\uFF0C\u4F4D\u79FB\u90E8\u5206\u4F7F\u7528\u52A8\u753B\u6216 fallback\u3002\n" +
                        "  \u9002\u5408\uFF1A\u7A7A\u4E2D\u6280\u80FD\u52A8\u753B\u9700\u8981\u7CBE\u786E\u4F4D\u79FB\u7684\u573A\u666F\u3002\n\n" +
                        "\u2022 IgnoreRootMotion\uFF1A\u5FFD\u7565\u52A8\u753B\uFF0C\u7A7A\u4E2D\u4F7F\u7528\u8F93\u5165\u52A0\u901F + \u91CD\u529B + \u963B\u529B\u3002\n" +
                        "  \u9002\u5408\uFF1A\u5E38\u89C4\u7A7A\u4E2D\u79FB\u52A8\u63A7\u5236\u3002\n\n" +
                        "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                        "\u5927\u591A\u6570\u60C5\u51B5\u7528 IgnoreRootMotion \u5373\u53EF\u3002\n" +
                        "\u4EC5\u5728\u7A7A\u4E2D\u6280\u80FD\u52A8\u753B\u9700\u8981\u7CBE\u786E\u4F4D\u79FB\u65F6\u624D\u7528 FullRootMotion\u3002");
                    GUILayout.Label(DescRM((RootMotionMode)_airRootMotionModeProp.enumValueIndex, false), _modeDescStyle);
                }

                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        private static string DescRM(RootMotionMode m, bool ground)
        {
            return m switch
            {
                RootMotionMode.FullRootMotion => ground
                    ? "\u2726 \u5B8C\u5168\u753A\u52A8\u753B\u4F4D\u79FB\u548C\u65CB\u8F6C\u9A71\u52A8\uFF0C\u5FFD\u7565\u8F93\u5165\u901F\u5EA6\u63A7\u5236"
                    : "\u2726 \u7A7A\u4E2D\u65CB\u8F6C\u753A\u52A8\u753B\u9A71\u52A8\uFF0C\u4F4D\u79BF\u90E8\u5206\u4F7F\u7528\u52A8\u753B\u6216 fallback",
                RootMotionMode.IgnoreRootMotion => ground
                    ? "\u2726 \u5FFD\u7565\u52A8\u753B\u4F4D\u79FB/\u65CB\u8F6C\uFF0C\u5B8C\u5168\u753A\u8F93\u5165\u63A7\u5236\u901F\u5EA6\u548C\u671D\u5411"
                    : "\u2726 \u5FFD\u7565\u52A8\u753B\u4F4D\u79FB/\u65CB\u8F6C\uFF0C\u7A7A\u4E2D\u4F7F\u7528\u8F93\u5165\u52A0\u901F+\u91CD\u529B+\u963B\u529B",
                _ => ""
            };
        }

        #endregion

        #region Section: Jump

        private void DrawJumpSection()
        {
            BeginSection(C_JUMP, ref _jumpFoldout,
                "Jump Settings", "\u8DF3\u8DC3\u903B\u8F91\u6A21\u5F0F\u4E0E\u53C2\u6570");

            if (_jumpFoldout)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(_jumpModeProp,
                    new GUIContent("Jump Mode", "\u8DF3\u8DC3\u6A21\u5F0F\u9009\u62E9"));

                if ((JumpMode)_jumpModeProp.enumValueIndex == JumpMode.BuiltIn)
                {
                    EditorGUILayout.HelpBox(
                        "BuiltIn: \u4F7F\u7528 KCC \u5185\u7F6E\u8DF3\u8DC3\u903B\u8F91\uFF0C\u4EE5\u4E0B\u53C2\u6570\u751F\u6548",
                        MessageType.Info);
                    EditorGUILayout.Space(2);

                    // 一键设置推荐参数按钮
                    GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
                    if (GUILayout.Button("\u2605 \u4E00\u952E\u8BBE\u7F6E\u63A8\u8350\u8D77\u8DF3\u53C2\u6570", GUILayout.Height(24)))
                    {
                        if (EditorUtility.DisplayDialog("\u4E00\u952E\u8BBE\u7F6E\u63A8\u8350\u8DF3\u8DC3\u53C2\u6570",
                            "\u5C06\u8BBE\u7F6E\u4EE5\u4E0B\u63A8\u8350\u503C\uFF1A\n\n" +
                            "\u2022 Jump Up Speed = 10\n" +
                            "\u2022 Scalable Forward Speed = 2\n" +
                            "\u2022 Pre-Grounding Grace Time = 0.15s\n" +
                            "\u2022 Post-Grounding Grace Time = 0.1s\n" +
                            "\u2022 Allow Jump When Sliding = true\n\n" +
                            "\u8FD9\u7EC4\u53C2\u6570\u53EF\u63D0\u4F9B\u7075\u654F\u4E14\u624B\u611F\u826F\u597D\u7684\u8DF3\u8DC3\u4F53\u9A8C\u3002",
                            "\u786E\u8BA4\u8BBE\u7F6E", "\u53D6\u6D88"))
                        {
                            ApplyRecommendedJumpPreset();
                        }
                    }
                    GUI.backgroundColor = Color.white;
                    EditorGUILayout.Space(4);

                    using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        DrawPropertyWithHelp(_jumpUpSpeedProp,
                            new GUIContent("Jump Up Speed", "\u8D77\u8DF3\u521D\u59CB\u5411\u4E0A\u901F\u5EA6"),
                            "Jump Up Speed - \u8D77\u8DF3\u5411\u4E0A\u901F\u5EA6",
                            "\u3010\u4F5C\u7528\u3011\u51B3\u5B9A\u89D2\u8272\u8D77\u8DF3\u77AC\u95F4\u7684\u521D\u59CB\u5411\u4E0A\u901F\u5EA6\uFF0C\u76F4\u63A5\u5F71\u54CD\u8DF3\u8DC3\u9AD8\u5EA6\u3002\n\n" +
                            "\u3010\u5178\u578B\u53D6\u503C\u3011\n" +
                            "\u2022 \u8F7B\u8DF3\uFF1A5~8\n" +
                            "\u2022 \u6807\u51C6\u8DF3\uFF1A10~12\n" +
                            "\u2022 \u5927\u8DF3\uFF1A15~20\n\n" +
                            "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                            "\u5B9E\u9645\u8DF3\u8DC3\u9AD8\u5EA6\u8FD8\u53D7\u91CD\u529B\u5F71\u54CD\uFF08Gravity \u53C2\u6570\uFF09\u3002\u5982\u679C\u89D2\u8272\u8DF3\u5F97\u592A\u4F4E/\u592A\u9AD8\uFF0C\u4F18\u5148\u8C03\u8FD9\u4E2A\u53C2\u6570\u3002\n" +
                            "\u63A8\u8350\u503C\uFF1A10");

                        DrawPropertyWithHelp(_jumpScalableForwardSpeedProp,
                            new GUIContent("Scalable Forward Speed", "\u8D77\u8DF3\u65F6\u7684\u524D\u8FDB\u901F\u5EA6"),
                            "Scalable Forward Speed - \u8D77\u8DF3\u524D\u8FDB\u901F\u5EA6",
                            "\u3010\u4F5C\u7528\u3011\u8D77\u8DF3\u77AC\u95F4\u6309\u5F53\u524D\u79FB\u52A8\u65B9\u5411\u9644\u52A0\u7684\u524D\u8FDB\u901F\u5EA6\u3002\u503C\u8D8A\u5927\uFF0C\u8DF3\u8DC3\u65F6\u5411\u524D\u51B2\u8D8A\u8FDC\u3002\n\n" +
                            "\u3010\u5178\u578B\u53D6\u503C\u3011\n" +
                            "\u2022 \u539F\u5730\u8DF3\uFF1A0\n" +
                            "\u2022 \u5FAE\u91CF\u524D\u51B2\uFF1A2~5\n" +
                            "\u2022 \u660E\u663E\u524D\u8DF3\uFF1A8~12\n\n" +
                            "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                            "\u5982\u679C\u4E0D\u5E0C\u671B\u8DF3\u8DC3\u65F6\u6709\u524D\u51B2\u611F\uFF0C\u8BBE\u4E3A 0~2\u3002\n" +
                            "\u5BF9\u4E8E\u5E73\u53F0\u8DF3\u8DC3\u73A9\u6CD5\u53EF\u9002\u5F53\u63D0\u9AD8\u3002\n" +
                            "\u63A8\u8350\u503C\uFF1A2");

                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("\u5BBD\u5BB9\u65F6\u95F4 (Grace Time)", EditorStyles.boldLabel);

                        DrawPropertyWithHelp(_jumpPreGroundingGraceTimeProp,
                            new GUIContent("Pre-Grounding", "\u8D77\u8DF3\u524D\u79BB\u5730\u5BBD\u5BB9\u65F6\u95F4"),
                            "Pre-Grounding Grace Time - \u571F\u72FC\u65F6\u95F4 (Coyote Time)",
                            "\u3010\u4F5C\u7528\u3011\u89D2\u8272\u79BB\u5F00\u5730\u9762\u540E\uFF0C\u5728\u6B64\u65F6\u95F4\u7A97\u53E3\u5185\u4ECD\u5141\u8BB8\u8DF3\u8DC3\u3002\n" +
                            "\u7C7B\u4F3C\u52A8\u753B\u7247\u4E2D\u89D2\u8272\u8D70\u51FA\u60AC\u5D16\u540E\u77ED\u6682\u6EDE\u7A7A\u7684\u6548\u679C\u3002\n\n" +
                            "\u3010\u4E3A\u4EC0\u4E48\u91CD\u8981\u3011\n" +
                            "\u503C\u4E3A 0 \u65F6\uFF0C\u89D2\u8272\u79BB\u5730\u7684\u77AC\u95F4\u5C31\u65E0\u6CD5\u8DF3\u8DC3\uFF0C\u73A9\u5BB6\u4F1A\u89C9\u5F97\u6309\u952E\u201C\u4E0D\u7075\u201D\u3002\n\n" +
                            "\u3010\u5178\u578B\u53D6\u503C\u3011\n" +
                            "\u2022 \u65E0\u5BBD\u5BB9\uFF1A0\n" +
                            "\u2022 \u6807\u51C6\u5BBD\u5BB9\uFF1A0.1~0.2\u79D2\n" +
                            "\u2022 \u5BBD\u677E\u5BBD\u5BB9\uFF1A0.3\u79D2\n\n" +
                            "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                            "\u5F3A\u70C8\u5EFA\u8BAE\u8BBE\u7F6E\u4E3A 0.1~0.2\uFF0C\u8FD9\u662F\u6539\u5584\u8DF3\u8DC3\u624B\u611F\u7684\u6700\u5173\u952E\u53C2\u6570\uFF01\n" +
                            "\u63A8\u8350\u503C\uFF1A0.15");

                        DrawPropertyWithHelp(_jumpPostGroundingGraceTimeProp,
                            new GUIContent("Post-Grounding", "\u8D77\u8DF3\u540E\u843D\u5730\u5BBD\u5BB9\u65F6\u95F4"),
                            "Post-Grounding Grace Time - \u8DF3\u8DC3\u7F13\u51B2 (Jump Buffer)",
                            "\u3010\u4F5C\u7528\u3011\u89D2\u8272\u5C1A\u672A\u843D\u5730\u65F6\u63D0\u524D\u6309\u4E0B\u8DF3\u8DC3\u952E\uFF0C\u5728\u843D\u5730\u540E\u81EA\u52A8\u6267\u884C\u8DF3\u8DC3\u3002\n" +
                            "\u5728\u6B64\u65F6\u95F4\u7A97\u53E3\u5185\u7684\u63D0\u524D\u6309\u952E\u4F1A\u88AB\u7F13\u5B58\u3002\n\n" +
                            "\u3010\u4E3A\u4EC0\u4E48\u91CD\u8981\u3011\n" +
                            "\u503C\u4E3A 0 \u65F6\uFF0C\u5FC5\u987B\u7CBE\u786E\u5728\u843D\u5730\u540E\u624D\u80FD\u6309\u8DF3\u8DC3\uFF0C\u7EFC\u5408\u4F53\u9A8C\u4E0D\u4F73\u3002\n\n" +
                            "\u3010\u5178\u578B\u53D6\u503C\u3011\n" +
                            "\u2022 \u65E0\u7F13\u51B2\uFF1A0\n" +
                            "\u2022 \u6807\u51C6\u7F13\u51B2\uFF1A0.08~0.15\u79D2\n" +
                            "\u2022 \u5BBD\u677E\u7F13\u51B2\uFF1A0.2\u79D2\n\n" +
                            "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                            "\u8BBE\u7F6E 0.1 \u53EF\u8BA9\u73A9\u5BB6\u5728\u5FEB\u8981\u843D\u5730\u65F6\u63D0\u524D\u6309\u8DF3\u4E5F\u80FD\u89E6\u53D1\uFF0C\u4F53\u9A8C\u66F4\u6D41\u7545\u3002\n" +
                            "\u63A8\u8350\u503C\uFF1A0.1");

                        EditorGUILayout.Space(4);

                        DrawPropertyWithHelp(_allowJumpingWhenSlidingProp,
                            new GUIContent("Allow Jump When Sliding", "\u659C\u5761\u6ED1\u52A8\u65F6\u662F\u5426\u5141\u8BB8\u8D77\u8DF3"),
                            "Allow Jump When Sliding - \u659C\u5761\u6ED1\u52A8\u8DF3\u8DC3",
                            "\u3010\u4F5C\u7528\u3011\u5F53\u89D2\u8272\u5728\u9661\u5CE1\u659C\u5761\u4E0A\u6ED1\u52A8\u65F6\uFF0C\u662F\u5426\u5141\u8BB8\u6267\u884C\u8DF3\u8DC3\u3002\n\n" +
                            "\u3010\u5F00\u542F(true)\u3011\n" +
                            "\u89D2\u8272\u5373\u4F7F\u5728\u6ED1\u5761\u72B6\u6001\u4E5F\u80FD\u8DF3\uFF0C\u53EF\u7528\u4E8E\u901C\u5761\u6216\u8131\u79BB\u659C\u5761\u3002\n\n" +
                            "\u3010\u5173\u95ED(false)\u3011\n" +
                            "\u89D2\u8272\u5728\u659C\u5761\u6ED1\u52A8\u65F6\u4E0D\u80FD\u8DF3\u8DC3\uFF0C\u9002\u5408\u60F3\u8981\u9650\u5236\u73A9\u5BB6\u5728\u9661\u5CE1\u5730\u5F62\u7684\u884C\u52A8\u80FD\u529B\u3002\n\n" +
                            "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                            "\u5927\u591A\u6570\u60C5\u51B5\u4E0B\u5EFA\u8BAE\u5F00\u542F\uFF0C\u907F\u514D\u73A9\u5BB6\u89C9\u5F97\u6309\u8DF3\u201C\u6CA1\u53CD\u5E94\u201D\u3002\n" +
                            "\u63A8\u8350\u503C\uFF1Atrue");
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "ExternalControlled: \u8DF3\u8DC3\u7531\u5916\u90E8\u6280\u80FD\u7CFB\u7EDF\uFF08Timeline / Animancer\uFF09\u63A7\u5236\uFF0C" +
                        "\u5185\u7F6E\u8DF3\u8DC3\u53C2\u6570\u4E0D\u751F\u6548\u3002\n\n" +
                        "\u53EF\u901A\u8FC7 Animator \u89E6\u53D1\u8DF3\u8DC3\u52A8\u753B\u6216\u5916\u90E8 AddVelocity \u5B9E\u73B0\u3002",
                        MessageType.Info);
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Section: Stable Movement

        private void DrawStableSection()
        {
            bool active = _groundRootMotionModeProp.enumValueIndex == (int)RootMotionMode.IgnoreRootMotion;
            Color bg = active ? C_MOVEMENT : new Color(0.35f, 0.35f, 0.35f, 0.07f);

            BeginSection(bg, ref _stableFoldout,
                "Stable Movement",
                active ? "Ignore Root Motion \u65F6\u7684\u5730\u9762\u8FD0\u52A8\u53C2\u6570"
                       : "\u4EC5\u5728 Ignore Root Motion(Ground) \u6A21\u5F0F\u4E0B\u53EF\u7528",
                !active);

            if (_stableFoldout && active)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawPropertyWithHelp(_maxStableMoveSpeedProp,
                        new GUIContent("Max Move Speed", "\u6700\u5927\u5730\u9762\u79FB\u52A8\u901F\u5EA6"),
                        "Max Move Speed - \u6700\u5927\u5730\u9762\u79FB\u52A8\u901F\u5EA6",
                        "\u3010\u4F5C\u7528\u3011\u89D2\u8272\u5728\u5730\u9762\u79FB\u52A8\u65F6\u7684\u6700\u5927\u901F\u5EA6\u4E0A\u9650\u3002\n\n" +
                        "\u3010\u5178\u578B\u53D6\u503C\u3011\n" +
                        "\u2022 \u6162\u901F\uFF1A3~5\n" +
                        "\u2022 \u6B63\u5E38\u884C\u8D70\uFF1A6~8\n" +
                        "\u2022 \u5FEB\u901F\u79FB\u52A8\uFF1A10~15\n\n" +
                        "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                        "\u4EC5\u5728 IgnoreRootMotion \u6A21\u5F0F\u4E0B\u751F\u6548\u3002\n" +
                        "\u6839\u636E\u89D2\u8272\u5C3A\u5BF8\u548C\u6E38\u620F\u8282\u594F\u8C03\u6574\u3002");

                    DrawPropertyWithHelp(_stableMovementSharpnessProp,
                        new GUIContent("Movement Sharpness", "\u901F\u5EA6\u53D8\u5316\u9510\u5EA6\uFF08\u8D8A\u9AD8\u8D8A\u7075\u654F\uFF09"),
                        "Movement Sharpness - \u79FB\u52A8\u9510\u5EA6",
                        "\u3010\u4F5C\u7528\u3011\u63A7\u5236\u89D2\u8272\u4ECE\u5F53\u524D\u901F\u5EA6\u8D8B\u5411\u76EE\u6807\u901F\u5EA6\u7684\u63D2\u503C\u901F\u7387\u3002\n" +
                        "\u503C\u8D8A\u9AD8\uFF0C\u89D2\u8272\u54CD\u5E94\u8D8A\u7075\u654F\uFF08\u51E0\u4E4E\u77AC\u79FB\uFF09\uFF1B\u503C\u8D8A\u4F4E\uFF0C\u52A0\u901F/\u51CF\u901F\u8D8A\u5E73\u6ED1\u3002\n\n" +
                        "\u3010\u5178\u578B\u53D6\u503C\u3011\n" +
                        "\u2022 \u7EF5\u8F6F/\u6ED1\u6E8E\uFF1A3~5\n" +
                        "\u2022 \u6807\u51C6\u7075\u654F\uFF1A10~15\n" +
                        "\u2022 \u6781\u5EA6\u7075\u654F\uFF1A20+\n\n" +
                        "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                        "\u52A8\u4F5C\u6E38\u620F\u901A\u5E38 10~15\uFF0C\u4F11\u95F2\u6E38\u620F\u53EF\u964D\u4F4E\u5230 5 \u4EE5\u4E0B\u83B7\u5F97\u67D4\u548C\u624B\u611F\u3002");

                    DrawPropertyWithHelp(_orientationSharpnessProp,
                        new GUIContent("Orientation Sharpness", "\u65CB\u8F6C\u671D\u5411\u63D2\u503C\u9510\u5EA6"),
                        "Orientation Sharpness - \u65CB\u8F6C\u9510\u5EA6",
                        "\u3010\u4F5C\u7528\u3011\u63A7\u5236\u89D2\u8272\u671D\u5411\u65CB\u8F6C\u7684\u63D2\u503C\u901F\u7387\u3002\n" +
                        "\u503C\u8D8A\u9AD8\u8F6C\u5411\u8D8A\u5FEB\uFF0C\u503C\u8D8A\u4F4E\u8F6C\u5411\u8D8A\u5E73\u6ED1\u3002\n\n" +
                        "\u3010\u5178\u578B\u53D6\u503C\u3011\n" +
                        "\u2022 \u7F13\u6162\u8F6C\u5411\uFF1A5~8\n" +
                        "\u2022 \u6807\u51C6\u8F6C\u5411\uFF1A10~15\n" +
                        "\u2022 \u7ACB\u5373\u8F6C\u5411\uFF1A20+\n\n" +
                        "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                        "\u914D\u5408 Movement Sharpness \u4E00\u8D77\u8C03\u6574\uFF0C\u4FDD\u6301\u4E24\u8005\u6BD4\u4F8B\u76F8\u8FD1\u4F53\u9A8C\u66F4\u81EA\u7136\u3002");

                    DrawPropertyWithHelp(_orientationMethodProp,
                        new GUIContent("Orientation Method",
                            "\u671D\u5411\u7B56\u7565\uFF1A\u671D\u5411\u53C2\u7167\u7269 \u6216 \u671D\u5411\u79FB\u52A8\u65B9\u5411"),
                        "Orientation Method - \u671D\u5411\u7B56\u7565",
                        "\u3010\u4F5C\u7528\u3011\u51B3\u5B9A\u89D2\u8272\u9762\u671D\u54EA\u4E2A\u65B9\u5411\u3002\n\n" +
                        "\u3010\u6A21\u5F0F\u8BF4\u660E\u3011\n" +
                        "\u2022 TowardsReference\uFF1A\u89D2\u8272\u59CB\u7EC8\u671D\u5411 Orientation Reference \uFF0C\u652F\u6301\u5DE6\u53F3\u6A2A\u79FB\uFF08\u4E0D\u8F6C\u5411\uFF09\u3002\n" +
                        "  \u9002\u5408\uFF1A\u5C04\u51FB\u6E38\u620F\u3001\u9501\u5B9A\u89C6\u89D2\u3001\u6A2A\u7248\u52A8\u4F5C\u3002\n\n" +
                        "\u2022 TowardsMovement\uFF1A\u89D2\u8272\u671D\u5411\u5F53\u524D\u79FB\u52A8\u65B9\u5411\uFF0C\u8FB9\u8D70\u8FB9\u8F6C\u3002\n" +
                        "  \u9002\u5408\uFF1A\u7B2C\u4E09\u4EBA\u79F0\u52A8\u4F5C\u6E38\u620F\u3002\n\n" +
                        "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                        "\u7B2C\u4E09\u4EBA\u79F0\u52A8\u4F5C\u6E38\u620F\u901A\u5E38\u7528 TowardsMovement\uFF0C\u5C04\u51FB/\u6A2A\u7248\u7528 TowardsReference\u3002");

                    // RotationLockAngle 仅在编辑器非运行 + TowardsMovement 时显示
                    bool isTowardsRef = _orientationMethodProp.enumValueIndex == (int)OrientationMethod.TowardsReference;
                    if (!Application.isPlaying && !isTowardsRef)
                    {
                        DrawPropertyWithHelp(_rotationLockAngleProp,
                        new GUIContent("Rotation Lock Angle",
                            "\u89D2\u8272\u671D\u5411\u4E0E\u76EE\u6807\u65B9\u5419\u5939\u89D2\u8D85\u8FC7\u6B64\u503C\u65F6\uFF0C\u5FC5\u987B\u5148\u65CB\u8F6C\u5230\u4F4D\u518D\u79FB\u52A8\u30020 = \u7981\u7528"),
                        "Rotation Lock Angle - \u65CB\u8F6C\u9501\u5B9A\u89D2\u5EA6",
                        "\u3010\u4F5C\u7528\u3011\u5F53\u89D2\u8272\u5F53\u524D\u671D\u5411\u4E0E\u76EE\u6807\u65B9\u5411\u7684\u5939\u89D2\u8D85\u8FC7\u6B64\u503C\u65F6\uFF0C\n" +
                        "\u89D2\u8272\u4F1A\u5148\u539F\u5730\u65CB\u8F6C\u5230\u76EE\u6807\u65B9\u5411\uFF0C\u518D\u5F00\u59CB\u79FB\u52A8\u3002\n\n" +
                        "\u3010\u5178\u578B\u53D6\u503C\u3011\n" +
                        "\u2022 0\uFF1A\u7981\u7528\u6B64\u529F\u80FD\uFF0C\u89D2\u8272\u59CB\u7EC8\u8FB9\u8D70\u8FB9\u8F6C\n" +
                        "\u2022 90~120\uFF1A\u4EC5\u5F53\u8F6C\u5411\u8D85\u8FC7 90\u00B0 \u65F6\u624D\u5148\u539F\u5730\u8F6C\n" +
                        "\u2022 30~60\uFF1A\u8F83\u5C0F\u89D2\u5EA6\u5C31\u4F1A\u89E6\u53D1\u539F\u5730\u8F6C\u5411\n\n" +
                        "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                        "\u8BBE\u4E3A 0 \u7981\u7528\u6B64\u529F\u80FD\u3002\u5982\u679C\u5E0C\u671B\u89D2\u8272\u5728\u5927\u89D2\u5EA6\u8F6C\u5411\u65F6\u66F4\u81EA\u7136\uFF0C\u8BBE\u4E3A 90~120\u3002");
                    }
                    else if (isTowardsRef)
                    {
                        EditorGUILayout.HelpBox(
                            "Rotation Lock Angle 在 TowardsReference 模式下不可用。\n" +
                            "此模式下角色固定朝向参照物，移动方向与旋转解耦。",
                            MessageType.Info);
                    }
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Section: Air Movement

        private void DrawAirSection()
        {
            bool active = _airRootMotionModeProp.enumValueIndex == (int)RootMotionMode.IgnoreRootMotion;
            Color bg = active ? C_MOVEMENT : new Color(0.35f, 0.35f, 0.35f, 0.07f);

            BeginSection(bg, ref _airFoldout,
                "Air Movement",
                active ? "Ignore Root Motion \u65F6\u7684\u7A7A\u4E2D\u8FD0\u52A8\u53C2\u6570"
                       : "\u4EC5\u5728 Ignore Root Motion(Air) \u6A21\u5F0F\u4E0B\u53EF\u7528",
                !active);

            if (_airFoldout && active)
            {
                EditorGUI.indentLevel++;
                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    DrawPropertyWithHelp(_maxAirMoveSpeedProp,
                        new GUIContent("Max Air Speed", "\u6700\u5927\u7A7A\u4E2D\u901F\u5EA6"),
                        "Max Air Speed - \u6700\u5927\u7A7A\u4E2D\u901F\u5EA6",
                        "\u3010\u4F5C\u7528\u3011\u89D2\u8272\u5728\u7A7A\u4E2D\u65F6\u7684\u6700\u5927\u6C34\u5E73\u79FB\u52A8\u901F\u5EA6\u4E0A\u9650\u3002\n\n" +
                        "\u3010\u5178\u578B\u53D6\u503C\u3011\n" +
                        "\u2022 \u65E0\u7A7A\u4E2D\u63A7\u5236\uFF1A0\n" +
                        "\u2022 \u8F7B\u5FAE\u7A7A\u4E2D\u63A7\u5236\uFF1A3~5\n" +
                        "\u2022 \u5B8C\u5168\u7A7A\u4E2D\u63A7\u5236\uFF1A\u4E0E\u5730\u9762\u901F\u5EA6\u76F8\u540C\n\n" +
                        "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                        "\u4EC5\u5728 IgnoreRootMotion(Air) \u6A21\u5F0F\u4E0B\u751F\u6548\u3002\n" +
                        "\u901A\u5E38\u8BBE\u4E3A\u5730\u9762\u901F\u5EA6\u7684 50%~80%\uFF0C\u8BA9\u7A7A\u4E2D\u63A7\u5236\u6709\u9650\u4F46\u4E0D\u662F\u5B8C\u5168\u4E0D\u80FD\u63A7\u5236\u3002");

                    DrawPropertyWithHelp(_airAccelerationSpeedProp,
                        new GUIContent("Acceleration", "\u7A7A\u4E2D\u52A0\u901F\u5EA6"),
                        "Air Acceleration - \u7A7A\u4E2D\u52A0\u901F\u5EA6",
                        "\u3010\u4F5C\u7528\u3011\u89D2\u8272\u5728\u7A7A\u4E2D\u65F6\u6C34\u5E73\u65B9\u5411\u7684\u52A0\u901F\u80FD\u529B\u3002\n" +
                        "\u503C\u8D8A\u5927\uFF0C\u7A7A\u4E2D\u8F6C\u5411/\u52A0\u901F\u8D8A\u5FEB\u3002\n\n" +
                        "\u3010\u5178\u578B\u53D6\u503C\u3011\n" +
                        "\u2022 \u5FAE\u5F31\u7A7A\u4E2D\u63A7\u5236\uFF1A5~10\n" +
                        "\u2022 \u6807\u51C6\u7A7A\u4E2D\u63A7\u5236\uFF1A15~25\n" +
                        "\u2022 \u5F3A\u7A7A\u4E2D\u63A7\u5236\uFF1A30+\n\n" +
                        "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                        "\u592A\u9AD8\u4F1A\u8BA9\u7A7A\u4E2D\u79FB\u52A8\u611F\u89C9\u201C\u98D8\u201D\uFF0C\u592A\u4F4E\u4F1A\u89C9\u5F97\u201C\u8DF3\u8D77\u6765\u5C31\u63A7\u5236\u4E0D\u4E86\u201D\u3002\n" +
                        "\u5EFA\u8BAE\u4ECE 15 \u5F00\u59CB\u8C03\u8BD5\u3002");

                    DrawPropertyWithHelp(_dragProp,
                        new GUIContent("Drag", "\u7A7A\u6C14\u963B\u529B"),
                        "Drag - \u7A7A\u6C14\u963B\u529B",
                        "\u3010\u4F5C\u7528\u3011\u7A7A\u4E2D\u6C34\u5E73\u901F\u5EA6\u7684\u8870\u51CF\u7CFB\u6570\u3002\n" +
                        "\u503C\u8D8A\u5927\uFF0C\u89D2\u8272\u7A7A\u4E2D\u6C34\u5E73\u901F\u5EA6\u4E22\u5931\u8D8A\u5FEB\u3002\n\n" +
                        "\u3010\u5178\u578B\u53D6\u503C\u3011\n" +
                        "\u2022 \u51E0\u4E4E\u65E0\u963B\u529B\uFF1A0~0.02\n" +
                        "\u2022 \u8F7B\u5FAE\u963B\u529B\uFF1A0.05~0.1\n" +
                        "\u2022 \u660E\u663E\u963B\u529B\uFF1A0.2~0.5\n\n" +
                        "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                        "\u914D\u5408 Acceleration \u4E00\u8D77\u8C03\u3002\u5982\u679C\u52A0\u901F\u9AD8\u4F46\u963B\u529B\u4F4E\uFF0C\u89D2\u8272\u4F1A\u5728\u7A7A\u4E2D\u201C\u6ED1\u201D\u5F88\u8FDC\u3002\n" +
                        "\u5982\u679C\u5E0C\u671B\u8D77\u8DF3\u540E\u4FDD\u6301\u60EF\u6027\uFF0C\u8BBE\u4E3A\u8F83\u4F4E\u503C\u3002");
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Section: Misc

        private void DrawMiscSection()
        {
            BeginSection(C_MISC, ref _miscFoldout,
                "Misc", "\u91CD\u529B\u3001\u6A21\u578B\u3001\u78B0\u649E\u4F53\u7B49\u6742\u9879");

            if (_miscFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPropertyWithHelp(_gravityProp,
                    new GUIContent("Gravity", "\u91CD\u5411\u91CF"),
                    "Gravity - \u91CD\u529B\u5411\u91CF",
                    "\u3010\u4F5C\u7528\u3011\u5B9A\u4E49\u4F5C\u7528\u4E8E\u89D2\u8272\u7684\u91CD\u529B\u65B9\u5411\u548C\u5927\u5C0F\u3002\n" +
                    "\u901A\u5E38\u4E3A (0, -Y, 0) \u5F62\u5F0F\uFF0CY \u503C\u8D8A\u5927\u4E0B\u843D\u8D8A\u5FEB\u3002\n\n" +
                    "\u3010\u5178\u578B\u53D6\u503C\u3011\n" +
                    "\u2022 \u7269\u7406\u771F\u5B9E\uFF1A(0, -9.81, 0)\n" +
                    "\u2022 \u6E38\u620F\u5E38\u7528\uFF1A(0, -20, 0) \u5230 (0, -40, 0)\n" +
                    "\u2022 \u6708\u7403\u91CD\u529B\uFF1A(0, -1.6, 0)\n\n" +
                    "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                    "\u6E38\u620F\u4E2D\u901A\u5E38\u4F7F\u7528\u6BD4\u771F\u5B9E\u66F4\u5927\u7684\u91CD\u529B\uFF08-20~-40\uFF09\uFF0C\u8BA9\u8DF3\u8DC3\u624B\u611F\u66F4\u7D27\u51D1\u3002\n" +
                    "\u8DF3\u8DC3\u9AD8\u5EA6 = JumpUpSpeed\u00B2 / (2 * |Gravity.y|)\uFF0C\u8C03\u8282\u8DF3\u8DC3\u624B\u611F\u65F6\u9700\u914D\u5408 JumpUpSpeed \u4E00\u8D77\u8C03\u3002");

                DrawPropertyWithHelp(_meshRootProp,
                    new GUIContent("Mesh Root", "\u89D2\u8272\u6A21\u578B\u6839\u8282\u70B9\uFF08\u53EF\u9009\uFF09"),
                    "Mesh Root - \u6A21\u578B\u6839\u8282\u70B9",
                    "\u3010\u4F5C\u7528\u3011\u6307\u5B9A\u89D2\u8272\u6A21\u578B\u7684\u6839 Transform\u3002\n" +
                    "\u7528\u4E8E RootMotion \u8865\u507F\u8BA1\u7B97\u548C\u6A21\u578B\u4F4D\u7F6E\u6821\u6B63\u3002\n\n" +
                    "\u3010\u4F7F\u7528\u65B9\u6CD5\u3011\n" +
                    "\u62D6\u5165\u89D2\u8272\u6A21\u578B\u7684\u6839\u8282\u70B9\uFF08\u901A\u5E38\u662F\u5305\u542B Animator \u7684\u5B50\u7269\u4F53\uFF09\u3002\n\n" +
                    "\u3010\u6CE8\u610F\u3011\n" +
                    "\u53EF\u9009\u5B57\u6BB5\u3002\u5982\u679C\u4E0D\u7528 RootMotion \u8865\u507F\u529F\u80FD\u53EF\u4EE5\u7559\u7A7A\u3002");

                DrawPropertyWithHelp(_ignoredCollidersProp,
                    new GUIContent("Ignored Colliders", "\u5FFD\u7565\u7684\u78B0\u649E\u4F53\u5217\u8868"),
                    "Ignored Colliders - \u5FFD\u7565\u78B0\u649E\u4F53",
                    "\u3010\u4F5C\u7528\u3011\u5217\u51FA KCC \u5728\u79FB\u52A8\u65F6\u5E94\u5FFD\u7565\u7684 Collider\u3002\n" +
                    "\u8FD9\u4E9B Collider \u4E0D\u4F1A\u963B\u6321\u89D2\u8272\u79FB\u52A8\u3002\n\n" +
                    "\u3010\u5178\u578B\u7528\u9014\u3011\n" +
                    "\u2022 \u89D2\u8272\u81EA\u8EAB\u7684\u6B66\u5668/\u88C5\u5907\u78B0\u649E\u4F53\n" +
                    "\u2022 \u89E6\u53D1\u5668\u533A\u57DF\uFF08Trigger Collider\uFF09\n" +
                    "\u2022 \u5176\u4ED6\u4E0D\u5E94\u5F71\u54CD\u79FB\u52A8\u7684\u78B0\u649E\u4F53\n\n" +
                    "\u3010\u8C03\u6574\u5EFA\u8BAE\u3011\n" +
                    "\u5982\u679C\u89D2\u8272\u88AB\u81EA\u5DF1\u7684\u5B50\u7269\u4F53\u78B0\u649E\u4F53\u5361\u4F4F\uFF0C\u628A\u5B83\u4EEC\u52A0\u5230\u8FD9\u91CC\u3002");

                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Section: Compensation Debug

        private void DrawCompensationSection()
        {
            BeginSection(C_COMP, ref _compFoldout,
                "Compensation Debug", "\u8865\u507F\u72B6\u6001\u8FD0\u884C\u65F6\u67E5\u770B");

            if (_compFoldout)
            {
                EditorGUI.indentLevel++;
                if (Application.isPlaying && _ctrl != null)
                {
                    using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        using (new EditorGUI.DisabledGroupScope(true))
                        {
                            EditorGUILayout.ToggleLeft("Compensation Enabled", _ctrl.CompensationEnabled);
                            EditorGUILayout.Vector3Field("Current Position", _ctrl.transform.position);
                            EditorGUILayout.Vector3Field("Current Rotation (Euler)",
                                _ctrl.transform.rotation.eulerAngles);
                        }
                    }
                    EditorGUILayout.HelpBox(
                        "\u5916\u90E8 API:\n  SetCompensationPosition(pos)\n  SetCompensationRotation(euler)\n  SetCompensation(pos, rot)\n  ClearCompensation()",
                        MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "\u8FD0\u884C\u540E\u663E\u793A\u8865\u507F\u72B6\u6001\u3002\n\n" +
                        "\u8865\u507F API:\n" +
                        "  SetCompensationPosition(Vector3)\n" +
                        "  SetCompensationRotation(Vector3 euler)\n" +
                        "  SetCompensation(Vector3 pos, Vector3 rotEuler)\n" +
                        "  ClearCompensation()\n\n" +
                        "\u9ED8\u8BA4 CompensationFrames = 2 (\u8DF3\u9488\u673A\u5236)",
                        MessageType.Info);
                }
                EditorGUI.indentLevel--;
            }
            EndSection();
        }

        #endregion

        #region Helpers – PropertyWithHelp & Preset

        /// <summary>绘制带 ? 帮助按钮的属性字段</summary>
        private void DrawPropertyWithHelp(SerializedProperty prop, GUIContent label,
            string helpTitle, string helpMessage)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(prop, label);
            if (GUILayout.Button("?", EditorStyles.miniButton, GUILayout.Width(20), GUILayout.Height(18)))
            {
                EditorUtility.DisplayDialog(helpTitle, helpMessage, "确定");
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>一键设置推荐跳跃参数</summary>
        private void ApplyRecommendedJumpPreset()
        {
            _jumpUpSpeedProp.floatValue = 10f;
            _jumpScalableForwardSpeedProp.floatValue = 2f;
            _jumpPreGroundingGraceTimeProp.floatValue = 0.15f;
            _jumpPostGroundingGraceTimeProp.floatValue = 0.1f;
            _allowJumpingWhenSlidingProp.boolValue = true;
            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Layout Helpers

        private void BeginSection(Color bgColor, ref bool foldout, string title, string subtitle,
            bool dimmed = false)
        {
            var r = GUILayoutUtility.GetRect(1, foldout ? 20 : 20);
            EditorGUI.DrawRect(r, bgColor);

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(6);
                var style = dimmed
                    ? new GUIStyle(_foldoutBoldStyle)
                      { normal = { textColor = new Color(0.45f, 0.45f, 0.45f) } }
                    : _foldoutBoldStyle;
                foldout = EditorGUILayout.Foldout(foldout, title, true, style);
                GUILayout.FlexibleSpace();

                if (!string.IsNullOrEmpty(subtitle))
                {
                    var sub = new GUIStyle(EditorStyles.miniLabel)
                    {
                        fontSize = 9, fontStyle = FontStyle.Italic,
                        normal = { textColor = dimmed
                            ? new Color(0.4f, 0.4f, 0.4f)
                            : new Color(0.5f, 0.5f, 0.5f) }
                    };
                    GUILayout.Label(subtitle, sub);
                    GUILayout.Space(6);
                }
            }

            var line = GUILayoutUtility.GetRect(1, 1);
            EditorGUI.DrawRect(line, new Color(0.25f, 0.25f, 0.25f, 0.25f));
            if (foldout) GUILayout.Space(3);
        }

        private static void EndSection() => EditorGUILayout.Space(3);

        #endregion
    }
}
#endif
