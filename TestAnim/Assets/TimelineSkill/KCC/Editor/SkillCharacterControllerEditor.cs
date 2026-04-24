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

            InitStyles();
        }

        public override void OnInspectorGUI()
        {
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
                EditorGUILayout.PropertyField(_moveActionProp,
                    new GUIContent("Move Action", "\u79FB\u52A8\u8F93\u5165 Action (Vector2)\uFF0C\u4ECE Input Action Asset \u62D6\u5165"));
                EditorGUILayout.PropertyField(_jumpActionProp,
                    new GUIContent("Jump Action", "\u8DF3\u8DC3\u8F93\u5165 Action (Button)\uFF0C\u4ECE Input Action Asset \u62D6\u5165"));
                EditorGUILayout.PropertyField(_orientationReferenceProp,
                    new GUIContent("Orientation Reference",
                        "\u65B9\u5411\u53C2\u7167\u7269 Transform\uFF08\u901A\u5E38\u4E3A\u76F8\u673A\uFF09\uFF0C\u4E3A\u7A7A\u65F6\u4F7F\u7528\u89D2\u8272\u81EA\u8EAB\u671D\u5411"));

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
                    EditorGUILayout.PropertyField(_groundRootMotionModeProp,
                        new GUIContent("Ground Mode", "\u5730\u9762\u65F6\u7684 RootMotion \u5904\u7406\u65B9\u5F0F"));
                    GUILayout.Label(DescRM((RootMotionMode)_groundRootMotionModeProp.enumValueIndex, true), _modeDescStyle);
                }
                EditorGUILayout.Space(2);

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.PropertyField(_airRootMotionModeProp,
                        new GUIContent("Air Mode", "\u7A7A\u4E2D\u65F6\u7684 RootMotion \u5904\u7406\u65B9\u5F0F"));
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

                    using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.PropertyField(_jumpUpSpeedProp,
                            new GUIContent("Jump Up Speed", "\u8D77\u8DF3\u521D\u59CB\u5411\u4E0A\u901F\u5EA6"));
                        EditorGUILayout.PropertyField(_jumpScalableForwardSpeedProp,
                            new GUIContent("Scalable Forward Speed", "\u8D77\u8DF3\u65F6\u7684\u524D\u8FDB\u901F\u5EA6"));
                        EditorGUILayout.Space(4);
                        EditorGUILayout.LabelField("\u5BBD\u5BB9\u65F6\u95F4 (Grace Time)", EditorStyles.boldLabel);
                        EditorGUILayout.PropertyField(_jumpPreGroundingGraceTimeProp,
                            new GUIContent("Pre-Grounding", "\u8D77\u8DF3\u524D\u79BB\u5730\u5BBD\u5BB9\u65F6\u95F4"));
                        EditorGUILayout.PropertyField(_jumpPostGroundingGraceTimeProp,
                            new GUIContent("Post-Grounding", "\u8D77\u8DF3\u540E\u843D\u5730\u5BBD\u5BB9\u65F6\u95F4"));
                        EditorGUILayout.Space(4);
                        EditorGUILayout.PropertyField(_allowJumpingWhenSlidingProp,
                            new GUIContent("Allow Jump When Sliding", "\u659C\u5761\u6ED1\u52A8\u65F6\u662F\u5426\u5141\u8BB8\u8D77\u8DF3"));
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
                    EditorGUILayout.PropertyField(_maxStableMoveSpeedProp,
                        new GUIContent("Max Move Speed", "\u6700\u5927\u5730\u9762\u79FB\u52A8\u901F\u5EA6"));
                    EditorGUILayout.PropertyField(_stableMovementSharpnessProp,
                        new GUIContent("Movement Sharpness", "\u901F\u5EA6\u53D8\u5316\u9510\u5EA6\uFF08\u8D8A\u9AD8\u8D8A\u7075\u654F\uFF09"));
                    EditorGUILayout.PropertyField(_orientationSharpnessProp,
                        new GUIContent("Orientation Sharpness", "\u65CB\u8F6C\u671D\u5411\u63D2\u503C\u9510\u5EA6"));
                    EditorGUILayout.PropertyField(_orientationMethodProp,
                        new GUIContent("Orientation Method",
                            "\u671D\u5411\u7B56\u7565\uFF1A\u671D\u5411\u53C2\u7167\u7269 \u6216 \u671D\u5411\u79FB\u52A8\u65B9\u5411"));
                    EditorGUILayout.PropertyField(_rotationLockAngleProp,
                        new GUIContent("Rotation Lock Angle",
                            "\u89D2\u8272\u671D\u5411\u4E0E\u76EE\u6807\u65B9\u5419\u5939\u89D2\u8D85\u8FC7\u6B64\u503C\u65F6\uFF0C\u5FC5\u987B\u5148\u65CB\u8F6C\u5230\u4F4D\u518D\u79FB\u52A8\u30020 = \u7981\u7528"));
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
                    EditorGUILayout.PropertyField(_maxAirMoveSpeedProp,
                        new GUIContent("Max Air Speed", "\u6700\u5927\u7A7A\u4E2D\u901F\u5EA6"));
                    EditorGUILayout.PropertyField(_airAccelerationSpeedProp,
                        new GUIContent("Acceleration", "\u7A7A\u4E2D\u52A0\u901F\u5EA6"));
                    EditorGUILayout.PropertyField(_dragProp,
                        new GUIContent("Drag", "\u7A7A\u6C14\u963B\u529B"));
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
                EditorGUILayout.PropertyField(_gravityProp, new GUIContent("Gravity", "\u91CD\u5411\u91CF"));
                EditorGUILayout.PropertyField(_meshRootProp, new GUIContent("Mesh Root", "\u89D2\u8272\u6A21\u578B\u6839\u8282\u70B9\uFF08\u53EF\u9009\uFF09"));
                EditorGUILayout.PropertyField(_ignoredCollidersProp, new GUIContent("Ignored Colliders", "\u5FFD\u7565\u7684\u78B0\u649E\u4F53\u5217\u8868"));
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
