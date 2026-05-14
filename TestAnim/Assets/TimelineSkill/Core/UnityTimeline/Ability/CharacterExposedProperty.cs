using System;
using EasyCharacterMovement;

namespace TreeDesigner
{
    /// <summary>
    /// ECM2 Character 组件的 ExposedProperty 类型。
    /// 用于 CommonBlackboard EPMap 存储角色组件引用。
    /// </summary>
    [Serializable]
    public class CharacterExposedProperty : BaseExposedProperty<Character>
    {
    }
}
