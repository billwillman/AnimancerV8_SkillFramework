#if CINEMACHINE_UNITY_INPUTSYSTEM
using UnityEngine;
using UnityEditor;
using Cinemachine;

[CustomEditor(typeof(CinemachineTransposerZoom))]
[CanEditMultipleObjects]
public class CinemachineTransposerZoomEditor : Editor
{
    private const string INFO_MESSAGE =
        "此组件仅支持 Body 为以下模式的 VirtualCamera：\n" +
        "  • Transposer\n" +
        "  • Orbital Transposer\n\n" +
        "通过 Z Axis 输入（如鼠标滚轮）控制 FollowOffset 距离，实现相机拉近/拉远。\n" +
        "其他 Body 模式（如 Framing Transposer、Do Nothing 等）不受支持。";

    private const string USAGE_MESSAGE =
        "使用方法：\n" +
        "1. 将此组件挂载到 CinemachineVirtualCamera 上（替代 CinemachineInputProvider）\n" +
        "2. 确保 VirtualCamera 的 Body 设置为 Transposer 或 Orbital Transposer\n" +
        "3. 在 Z Axis 字段中拖入对应的 InputActionReference（如 Mouse ScrollWheel）\n" +
        "4. 调整 MinOffset / MaxOffset 设定缩放范围";

    private SerializedProperty m_PlayerIndex;
    private SerializedProperty m_AutoEnableInputs;
    private SerializedProperty m_ZAxis;
    private SerializedProperty m_ZoomSpeed;
    private SerializedProperty m_MinOffset;
    private SerializedProperty m_MaxOffset;
    private SerializedProperty m_SmoothTime;

    private void OnEnable()
    {
        m_PlayerIndex = serializedObject.FindProperty("PlayerIndex");
        m_AutoEnableInputs = serializedObject.FindProperty("AutoEnableInputs");
        m_ZAxis = serializedObject.FindProperty("ZAxis");
        m_ZoomSpeed = serializedObject.FindProperty("ZoomSpeed");
        m_MinOffset = serializedObject.FindProperty("MinOffset");
        m_MaxOffset = serializedObject.FindProperty("MaxOffset");
        m_SmoothTime = serializedObject.FindProperty("SmoothTime");
    }

    public override void OnInspectorGUI()
    {
        // 顶部信息框：支持说明
        EditorGUILayout.HelpBox(INFO_MESSAGE, MessageType.Info);

        EditorGUILayout.Space(4);

        // 检测当前 Body 模式是否兼容
        var zoom = (CinemachineTransposerZoom)target;
        var vcam = zoom.GetComponent<CinemachineVirtualCamera>();
        if (vcam != null)
        {
            var transposer = vcam.GetCinemachineComponent<CinemachineTransposer>();
            if (transposer == null)
                transposer = vcam.GetCinemachineComponent<CinemachineOrbitalTransposer>();

            if (transposer == null)
            {
                EditorGUILayout.HelpBox(
                    "⚠ 当前 VirtualCamera 的 Body 不是 Transposer 或 Orbital Transposer！\n" +
                    "此组件将不会生效。请切换 Body 模式。",
                    MessageType.Warning);
                EditorGUILayout.Space(4);
            }
            else
            {
                string bodyType = transposer is CinemachineOrbitalTransposer
                    ? "Orbital Transposer"
                    : "Transposer";
                EditorGUILayout.HelpBox(
                    $"✓ 当前 Body 模式：{bodyType}（兼容）",
                    MessageType.None);
                EditorGUILayout.Space(4);
            }
        }

        serializedObject.Update();

        // --- Input Settings（只显示 Z Axis 相关，隐藏 XY Axis） ---
        EditorGUILayout.LabelField("Input Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_PlayerIndex);
        EditorGUILayout.PropertyField(m_AutoEnableInputs);
        EditorGUILayout.PropertyField(m_ZAxis, new GUIContent("Z Axis (Scroll Wheel)", "用于缩放的输入 Action（Float 类型，如鼠标滚轮）"));

        EditorGUILayout.Space(8);

        // --- Zoom Settings ---
        EditorGUILayout.LabelField("Zoom Settings", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(m_ZoomSpeed, new GUIContent("Zoom Speed", "缩放速度倍率"));
        EditorGUILayout.PropertyField(m_MinOffset, new GUIContent("Min Offset", "最小 Follow Offset 距离（相机最近距离）"));
        EditorGUILayout.PropertyField(m_MaxOffset, new GUIContent("Max Offset", "最大 Follow Offset 距离（相机最远距离）"));
        EditorGUILayout.PropertyField(m_SmoothTime, new GUIContent("Smooth Time", "缩放平滑时间（越大越平滑）"));

        serializedObject.ApplyModifiedProperties();

        EditorGUILayout.Space(8);

        // 底部折叠使用说明
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("使用说明", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(USAGE_MESSAGE, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndVertical();
    }
}
#endif
