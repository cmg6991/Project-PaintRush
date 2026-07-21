using UnityEngine;

public class ShootParticle : MonoBehaviour
{
    [SerializeField] private ParticleSystem smokeParticle;

    public void PlayParticle(Color bulletColor)
    {
        if (smokeParticle == null)
            return;
        smokeParticle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var mainModule = smokeParticle.main;

        Color pastelColor = Color.Lerp(bulletColor, Color.white, 0.01f);
        pastelColor.a = 1f;

        mainModule.startColor = pastelColor;
        smokeParticle.Play();
    }
}
