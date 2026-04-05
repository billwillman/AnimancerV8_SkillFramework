using UnityEngine;
using DamageNumbersPro;

public class PopupTextManager : MonoBehaviour
{
    static PopupTextManager s_Instance;
    public static PopupTextManager Instance => s_Instance;


    public DamageNumber DamageNumberPrefab;

    public Transform Transform;
    public Vector2 dnVelocity;

    private void Awake()
    {
        s_Instance = this;
    }

    private void Update()
    {
    }

    public void SpawnPopup(float number, Vector3 position,Transform followTarget, Vector2 dnVelocity)
    {
        DamageNumber newPopup = DamageNumberPrefab.Spawn(position, number);

        newPopup.enableFollowing = true;
        newPopup.followSettings.speed = dnVelocity.magnitude;
        newPopup.followedTarget = followTarget;

        newPopup.lerpSettings.speed = dnVelocity.magnitude;
        newPopup.lerpSettings.minX = newPopup.lerpSettings.maxX = dnVelocity.normalized.x;
        newPopup.lerpSettings.minY = newPopup.lerpSettings.maxY = dnVelocity.normalized.y;
    }
}