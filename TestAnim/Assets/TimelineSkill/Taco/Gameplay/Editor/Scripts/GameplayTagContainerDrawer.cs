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
            GameplayTagContainerView gameplayTagContainerView = new GameplayTagContainerView(property.displayName, gameplayTag, property.serializedObject.targetObject);
            return gameplayTagContainerView;
        }
    }
}