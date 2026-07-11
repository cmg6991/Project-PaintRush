using UnityEngine;

public class ShootParticle : MonoBehaviour
{
    [SerializeField] private ParticleSystem smokeParticle;

    public void PlayParticle(Color bulletColor)
    {
        if (smokeParticle == null)
            return;

        var mainModule = smokeParticle.main;

        Color pastelColor = Color.Lerp(bulletColor, Color.white, 0.01f);
        pastelColor.a = 0.5f;
        mainModule.startColor = pastelColor;
        smokeParticle.Play();
    }
}
