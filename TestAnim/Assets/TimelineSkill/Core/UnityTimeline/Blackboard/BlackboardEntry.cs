using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityTimeline.Blackboard
{
    /// <summary>
    /// 黑板变量条目：定义一个变量的名称、类型、默认值。
    /// 用于：公共区域定义 / 全局 Schema 模板 / 局部 Schema / 运行时动态创建。
    /// </summary>
    [Serializable]
    public class BlackboardEntry
    {
        public string Name = "";
        public BlackboardType Type = BlackboardType.Int;

        // Unity 不支持序列化 object，拆分为各类型字段
        public bool DefaultValueBool;
        public int DefaultValueInt;
        public float DefaultValueFloat;
        public string DefaultValueString = "";
        public Vector2 DefaultValueVector2;
        public Vector3 DefaultValueVector3;

        public object GetDefaultValue() => Type.GetDefaultValue();

        public object GetTypedDefaultValue() => Type switch
        {
            BlackboardType.Bool => DefaultValueBool,
            BlackboardType.Int => DefaultValueInt,
            BlackboardType.Float => DefaultValueFloat,
            BlackboardType.String => DefaultValueString ?? "",
            BlackboardType.Vector2 => DefaultValueVector2,
            BlackboardType.Vector3 => DefaultValueVector3,
            _ => null,
        };

        public bool Validate() => !string.IsNullOrWhiteSpace(Name);

        // ═══════════════════════════════════════════
        //  快捷工厂（用于运行时动态创建）
        // ═══════════════════════════════════════════

        /// <summary>通过类型枚举 + 默认值创建</summary>
        public static BlackboardEntry Create(string name, BlackboardType type, object defaultValue)
        {
            var entry = new BlackboardEntry { Name = name, Type = type };
            switch (type)
            {
                case BlackboardType.Bool: entry.DefaultValueBool = (bool)defaultValue; break;
                case BlackboardType.Int: entry.DefaultValueInt = (int)defaultValue; break;
                case BlackboardType.Float: entry.DefaultValueFloat = (float)defaultValue; break;
                case BlackboardType.String: entry.DefaultValueString = (string)defaultValue; break;
                case BlackboardType.Vector2: entry.DefaultValueVector2 = (Vector2)defaultValue; break;
                case BlackboardType.Vector3: entry.DefaultValueVector3 = (Vector3)defaultValue; break;
            }
            return entry;
        }

        /// <summary>通过类型枚举创建（使用类型默认值）</summary>
        public static BlackboardEntry Create(string name, BlackboardType type)
        {
            return new BlackboardEntry { Name = name, Type = type };
        }

        /// <summary>泛型快捷创建（使用 default(T) 作为默认值）</summary>
        public static BlackboardEntry Create<T>(string name)
        {
            var entry = new BlackboardEntry { Name = name };
            entry.Type = ResolveType(typeof(T));
            return entry;
        }

        // ═══════════════════════════════════════════
        //  内部工具
        // ═══════════════════════════════════════════

        internal static BlackboardType ResolveType(Type t)
        {
            if (t == typeof(bool)) return BlackboardType.Bool;
            if (t == typeof(int)) return BlackboardType.Int;
            if (t == typeof(float)) return BlackboardType.Float;
            if (t == typeof(string)) return BlackboardType.String;
            if (t == typeof(Vector2)) return BlackboardType.Vector2;
            if (t == typeof(Vector3)) return BlackboardType.Vector3;
            return BlackboardType.Int;
        }

        /// <summary>深拷贝（用于 Schema 合并时不污染原对象）</summary>
        public BlackboardEntry Clone()
        {
            return new BlackboardEntry
            {
                Name = this.Name,
                Type = this.Type,
                DefaultValueBool = this.DefaultValueBool,
                DefaultValueInt = this.DefaultValueInt,
                DefaultValueFloat = this.DefaultValueFloat,
                DefaultValueString = this.DefaultValueString ?? "",
                DefaultValueVector2 = this.DefaultValueVector2,
                DefaultValueVector3 = this.DefaultValueVector3,
            };
        }
    }
}
