using UnityEngine;

public class TimeMananger : MonoBehaviour
{
    static TimeMananger s_Instance;
    public static TimeMananger Instance => s_Instance;

    TimeScaler scaler;

    private void Awake()
    {
        s_Instance = this;
        scaler = new TimeScaler();
    }
    public void Update()
    {
        if (scaler.Active)
            scaler.Update(Time.unscaledDeltaTime);
    }

    public void ChangeTimeScale(float scale, float duration, float blendIn, float blendOut)
    {
        scaler.Set(scale, duration, blendIn, blendOut);
    }


    class TimeScaler
    {
        public float OriginScale;
        public float TargetScale;
        public float Duration;
        public float BlendIn;
        public float BlendOut;

        public bool Active;
        public float Timer;
        public float CurrentScale { get => Time.timeScale; set => Time.timeScale = value; }

        public void Set(float scale, float duration, float blendIn, float blendOut)
        {
            OriginScale = CurrentScale;
            TargetScale = scale;
            Duration = Mathf.Max(0, duration);
            BlendIn = Mathf.Clamp(blendIn, 0, Duration);
            BlendOut = Mathf.Clamp(blendOut, 0, Duration - BlendIn);

            Active = true;
            Timer = 0;
        }

        public void Update(float deltaTime)
        {
            Timer += deltaTime;

            if (Timer < BlendIn)
                CurrentScale = Mathf.Lerp(OriginScale, TargetScale, Timer / BlendIn);
            else if (BlendIn <= Timer && Timer <= Duration - BlendOut)
                CurrentScale = TargetScale;
            else if (Duration - BlendOut < Timer && Timer <= Duration)
                CurrentScale = Mathf.Lerp(TargetScale, 1, (Timer - (Duration - BlendOut)) / BlendOut);
            else
            {
                Active = false;
                CurrentScale = 1;
            }
        }
    }
}
