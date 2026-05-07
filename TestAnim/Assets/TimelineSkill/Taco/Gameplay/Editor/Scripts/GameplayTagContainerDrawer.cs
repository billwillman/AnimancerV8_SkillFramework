using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;

namespace Taco.Gameplay.Editor
{
    [CustomPropertyDrawer(typeof(GameplayTagContainer))]
    public class GameplayTagContainerDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var gameplayTag = property.GetValue<GameplayTagContainer>();

            // 获取字段上的 Tooltip 特性
            string tooltipText = null;
            if (fieldInfo != null)
            {
                var tooltipAttr = System.Attribute.GetCustomAttribute(fieldInfo, typeof(TooltipAttribute)) as TooltipAttribute;
                if (tooltipAttr != null)
                    tooltipText = tooltipAttr.tooltip;
            }

            GameplayTagContainerView gameplayTagContainerView = new GameplayTagContainerView(
                property.displayName, gameplayTag, property.serializedObject.targetObject, tooltipText);
            return gameplayTagContainerView;
        }
    }
}