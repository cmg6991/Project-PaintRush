using UnityEngine;
using System.Collections;

public class UIBrush : PoolableObject
{
    [SerializeField] private float lifeTime = 0.1f;

    private SpriteRenderer sr;

    private Coroutine coroutine;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public override void OnSpawn()
    {
        if (coroutine != null)
            StopCoroutine(coroutine);

        coroutine = StartCoroutine(LifeRoutine());
    }

    IEnumerator LifeRoutine()
    {
        yield return new WaitForSeconds(lifeTime);

        ReturnPool();
    }

    public void SetColor(Color color)
    {
        sr.color = color;
    }
}
