using UnityEngine;
using System.Collections;

public class TitleSplashEffect : MonoBehaviour
{
    [SerializeField] private ObjectPool splashPool;
    [SerializeField] private Transform[] spawnPoints;

    [Header("시간")]
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float spawnInterval = 0.2f;

    [Header("크기")]
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1f;
    [SerializeField] private float popDuration = 0.15f;

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(startDelay);

        foreach (Transform point in spawnPoints)
        {
            Debug.Log($"Point: {point.name} / {point.position}");

            Spawn(point.position);

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void Spawn(Vector3 position)
    {
        UIBrush brush = splashPool.Get<UIBrush>();
        Debug.Log($" / 위치: {position}");

        brush.transform.position = position;

        brush.transform.rotation =
            Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

        brush.SetColor(
            Color.HSVToRGB(Random.value, 0.9f, 1f)
        );

        StartCoroutine(PopRoutine(brush));
    }

    private IEnumerator PopRoutine(UIBrush brush)
    {
        float targetScale = Random.Range(minScale, maxScale);
        float time = 0f;

        brush.transform.localScale = Vector3.zero;

        while (time < popDuration)
        {
            time += Time.deltaTime;

            float t = time / popDuration;
            float scale = Mathf.Lerp(0f, targetScale, t);

            brush.transform.localScale = Vector3.one * scale;

            yield return null;
        }

        brush.transform.localScale = Vector3.one * targetScale;
    }
}
