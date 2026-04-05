using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;

public class ShowDebugManager : MonoBehaviour
{
    static ShowDebugManager s_Instance;
    public static ShowDebugManager Instance => s_Instance;

    public TextMeshProUGUI Text;

    Tween tween;

    private void Awake()
    {
        s_Instance = this;
    }

    public void Show(string text,float duration = 0.5f,float easeOut = 0.5f)
    {
        Text.text = text;
        tween?.Kill();
        Text.alpha = 1;
        tween = Text.DOFade(0, easeOut).SetDelay(duration);
    }
}
