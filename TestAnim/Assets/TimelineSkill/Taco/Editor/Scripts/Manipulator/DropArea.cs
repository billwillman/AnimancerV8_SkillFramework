using System;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace Taco.Editor
{
    public class DropArea
    {
        VisualElement m_Container;
        VisualElement m_Target;
        public event Action onDragStartEvent;
        public event Action<DragUpdatedEvent> onDragUpdateEvent;
        public event Action<DragLeaveEvent> onDragLeaveEvent;
        public event Action<DragPerformEvent> onDragPerformEvent;
        public event Action<DragExitedEvent> onDragExitEvent;

        public Func<bool> DragValid;

        bool m_Dragged;
        bool m_Enter;

        public void Init(VisualElement target,VisualElement container)
        {
            m_Container = container;
            m_Container.AddToClassList("droparea");
            m_Container.RegisterCallback<AttachToPanelEvent>(OnAttach);
            m_Container.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            m_Container.RegisterCallback<DragEnterEvent>(OnDragEnterEvent);
            m_Container.RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
            m_Container.RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            m_Container.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            m_Container.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            m_Target = target;
        }
        public void Init(VisualElement target)
        {
            m_Container = target;
            m_Container.AddToClassList("droparea");
            m_Container.RegisterCallback<AttachToPanelEvent>(OnAttach);
            m_Container.RegisterCallback<DetachFromPanelEvent>(OnDetach);
            m_Container.RegisterCallback<DragEnterEvent>(OnDragEnterEvent);
            m_Container.RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
            m_Container.RegisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            m_Container.RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            m_Container.RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            m_Target = target;
        }
        public void Dispose()
        {
            m_Container?.RemoveFromClassList("droparea");
            m_Container?.UnregisterCallback<AttachToPanelEvent>(OnAttach);
            m_Container?.UnregisterCallback<DetachFromPanelEvent>(OnDetach);
            m_Container?.UnregisterCallback<DragEnterEvent>(OnDragEnterEvent);
            m_Container?.UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);
            m_Container?.UnregisterCallback<DragLeaveEvent>(OnDragLeaveEvent);
            m_Container?.UnregisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            m_Container?.UnregisterCallback<DragPerformEvent>(OnDragPerformEvent);
            m_Container = null;
            m_Target = null;

            onDragStartEvent = null;
            onDragUpdateEvent = null;
            onDragLeaveEvent = null;
            onDragPerformEvent = null;
            onDragExitEvent = null;
        }

        void OnDragEnterEvent(DragEnterEvent e)
        {
            if (!m_Enter)
                m_Enter = true;
            m_Container?.AddToClassList("dragover");
        }
        void OnDragExitedEvent(DragExitedEvent e)
        {
            if (m_Dragged)
            {
                m_Dragged = false;
                m_Enter = false;
                if(!string.IsNullOrEmpty(DragHandle.DragName) && DragAndDrop.GetGenericData(DragHandle.DragName) is IDragableVisualElement dragableVisualElement)
                    dragableVisualElement.StopDrag();
                m_Container?.RemoveFromClassList("dragover");
                onDragExitEvent?.Invoke(e);
            }
        }
        void OnDragLeaveEvent(DragLeaveEvent e)
        {
            m_Container?.RemoveFromClassList("dragover");
            onDragLeaveEvent?.Invoke(e);
        }
        void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            if (!m_Dragged && !m_Enter)
            {
                m_Dragged = true;
                onDragStartEvent?.Invoke();
                if (!string.IsNullOrEmpty(DragHandle.DragName))
                    (DragAndDrop.GetGenericData(DragHandle.DragName) as IDragableVisualElement)?.StartDrag();
                else if (DragAndDrop.objectReferences.Length > 0 && DragValid())
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                }
            }
            if (!string.IsNullOrEmpty(DragHandle.DragName))
                (DragAndDrop.GetGenericData(DragHandle.DragName) as IDragableVisualElement)?.UpdateDrag(e, m_Target);
            m_Container?.AddToClassList("dragover");
            onDragUpdateEvent?.Invoke(e);
        }
        void OnDragPerformEvent(DragPerformEvent e)
        {
            DragAndDrop.AcceptDrag();
            if (!string.IsNullOrEmpty(DragHandle.DragName) && DragAndDrop.GetGenericData(DragHandle.DragName) is IDragableVisualElement dragableVisualElement)
                dragableVisualElement.PerformDrag(e, m_Target);
            onDragPerformEvent?.Invoke(e);
        }
        void OnAttach(AttachToPanelEvent e)
        {
            e.destinationPanel.visualTree.RegisterCallback<DragExitedEvent>(OnDragExitedEvent);
        }
        void OnDetach(DetachFromPanelEvent e)
        {
            e.originPanel.visualTree.UnregisterCallback<DragExitedEvent>(OnDragExitedEvent);
        }
    }
}