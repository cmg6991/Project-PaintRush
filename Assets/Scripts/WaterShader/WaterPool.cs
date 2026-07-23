using UnityEngine;

public class WaterPool : PoolableObject
{
    private ParticleSystem ps;

    private void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    public override void OnSpawn()
    {
        ps.Clear();
        ps.Play();

        CancelInvoke();
        Invoke(nameof(ReturnPool), 1f); // 또는 파티클 시간
    }

    public override void OnRelease()
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
