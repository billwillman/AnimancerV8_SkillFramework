using System;
using System.Collections;
using Unity.EditorCoroutines.Editor;
using UnityEngine;

namespace Taco.Editor
{ 
    public static class EditorCoroutineHelper
    {
        public static EditorCoroutine Delay(Action callback, float timer)
        {
            return EditorCoroutineUtility.StartCoroutineOwnerless(DelayIEnumerator(callback, timer));
        }
        public static EditorCoroutine WaitWhile(Action callback,Func<bool> func)
        {
            return EditorCoroutineUtility.StartCoroutineOwnerless(WaitWhileIEnumerator(callback, func));
        }
        public static EditorCoroutine WaitOneFrame(Action callback)
        {
            return EditorCoroutineUtility.StartCoroutineOwnerless(WaitOneFrameIEnumerator(callback));
        }

        static IEnumerator DelayIEnumerator(Action callback, float timer)
        {
            yield return new EditorWaitForSeconds(timer);
            callback?.Invoke();
        }

        static IEnumerator WaitWhileIEnumerator(Action callback, Func<bool> func)
        {
            yield return new WaitWhile(func);
            callback?.Invoke();
        }

        static IEnumerator WaitOneFrameIEnumerator(Action callback)
        {
            yield return null;
            callback?.Invoke();
        }
    }
}