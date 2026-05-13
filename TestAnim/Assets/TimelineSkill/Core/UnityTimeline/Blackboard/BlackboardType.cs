using System;
using UnityEngine;

namespace UnityTimeline.Blackboard
{
    /// <summary>黑板支持的值类型枚举</summary>
    public enum BlackboardType
    {
        Bool,
        Int,
        Float,
        String,
        Vector2,
        Vector3,
    }

    /// <summary>BlackboardType 工具扩展方法</summary>
    public static class BlackboardTypeExtensions
    {
        private static readonly Color32 BoolColor = new(210, 210, 210, 255);
        private static readonly Color32 IntColor = new(148, 129, 230, 255);
        private static readonly Color32 FloatColor = new(132, 228, 231, 255);
        private static readonly Color32 StringColor = new(252, 218, 110, 255);
        private static readonly Color32 Vector2Color = new(154, 239, 146, 255);
        private static readonly Color32 Vector3Color = new(246, 255, 154, 255);

        public static Type ToSystemType(this BlackboardType type) => type switch
        {
            BlackboardType.Bool => typeof(bool),
            BlackboardType.Int => typeof(int),
            BlackboardType.Float => typeof(float),
            BlackboardType.String => typeof(string),
            BlackboardType.Vector2 => typeof(Vector2),
            BlackboardType.Vector3 => typeof(Vector3),
            _ => null,
        };

        public static object GetDefaultValue(this BlackboardType type) => type switch
        {
            BlackboardType.Bool => false,
            BlackboardType.Int => 0,
            BlackboardType.Float => 0f,
            BlackboardType.String => "",
            BlackboardType.Vector2 => Vector2.zero,
            BlackboardType.Vector3 => Vector3.zero,
            _ => null,
        };

        /// <summary>复用现有 PropertyColor 配色方案（与 ExposedPropertyView 一致）</summary>
        public static Color GetTypeColor(this BlackboardType type) => type switch
        {
            BlackboardType.Bool => BoolColor,
            BlackboardType.Int => IntColor,
            BlackboardType.Float => FloatColor,
            BlackboardType.String => StringColor,
            BlackboardType.Vector2 => Vector2Color,
            BlackboardType.Vector3 => Vector3Color,
            _ => Color.white,
        };
    }
}
