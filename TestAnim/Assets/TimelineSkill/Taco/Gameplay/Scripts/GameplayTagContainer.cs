using System;
using System.Collections.Generic;

namespace Taco.Gameplay
{
    [Serializable]
    public partial class GameplayTagContainer
    {
        public List<string> TagGuids = new List<string>();
        public string ReferencePath = "CustomData";

        List<string> m_Tags;
        public List<string> Tags
        {
            get
            {
                if (m_Tags == null)
                {
                    Init();
                }
                else if (m_Tags.Count == 0 && TagGuids != null && TagGuids.Count > 0)
                {
                    // 自愈：m_Tags 被意外清空但 TagGuids 仍有数据，重新初始化
                    m_Tags = null;
                    Init();
                }
                return m_Tags;
            }
            set => m_Tags = value;
        }

        public Action OnValueChanged;
        GameplayTagData m_GameplayTagData => GameplayTagUtility.GameplayTagData;

        public void Init()
        {
            if (m_GameplayTagData == null)
                GameplayTagUtility.RuntimeInit();

            if (m_GameplayTagData == null)
                return;

            m_GameplayTagData.EnsureInitialized();

            if (!m_GameplayTagData.IsInitialized)
                return;

            m_Tags = new List<string>();
            for (int i = TagGuids.Count - 1; i >= 0; i--)
            {
                string tagGuid = TagGuids[i];
                string tag = m_GameplayTagData.GuidToName(tagGuid);
                if (!string.IsNullOrEmpty(tag))
                    m_Tags.Add(tag);
            }
            OnValueChanged?.Invoke();
        }

        public void AddTagRuntime(string tag)
        {
            if (!IsChildOf(tag))
            {
                for (int i = Tags.Count - 1; i >= 0; i--)
                {
                    if (tag.StartTagIs(Tags[i]))
                        Tags.RemoveAt(i);
                }
                Tags.Add(tag);
                OnValueChanged?.Invoke();
            }
        }
        public void RemoveTagRuntime(string tag)
        {
            if (Tags.Contains(tag))
            {
                Tags.Remove(tag);
                OnValueChanged?.Invoke();
            }
        }
        public void RemoveTagWithChildRuntime(string tag)
        {
            for (int i = Tags.Count - 1; i >= 0; i--)
            {
                if (Tags[i].StartTagIs(tag))
                    RemoveTagRuntime(Tags[i]);
            }
        }
        public void ClearTagRuntime()
        {
            Tags.Clear();
            OnValueChanged?.Invoke();
        }


        /// <summary>
        /// 所选tag是否包含输入tag，或者包含输入tag的父tag
        /// </summary>
        /// <param name="childTag"></param>
        /// <returns></returns>
        public bool IsParentOf(string childTag)
        {
            foreach (var tag in Tags)
            {
                if (childTag.StartTagIs(tag))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 所选tag是否包含输入tag，或者包含输入tag的子tag
        /// </summary>
        /// <param name="childTag"></param>
        /// <returns></returns>
        public bool IsChildOf(string parentTag)
        {
            foreach (var tag in Tags)
            {
                if (tag.StartTagIs(parentTag))
                    return true;
            }
            return false;
        }
    }
}