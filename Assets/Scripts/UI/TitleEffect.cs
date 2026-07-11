using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TitleEffect : MonoBehaviour
{
    [SerializeField] private ObjectPool brushPool;

    [SerializeField] private float spawnDistance = 10f;

    [SerializeField] private float minScale = 0.15f;
    [SerializeField] private float maxScale = 0.35f;
    [SerializeField] private float colorSpeed = 0.08f;

    private float hue = 0f;
    private Vector3 lastMouse;

    private void Start()
    {
        lastMouse = Input.mousePosition;
    }

    private void Update()
    {
        Vector3 mouse = Input.mousePosition;

        if (Vector3.Distance(mouse, lastMouse) < spawnDistance)
            return;

        lastMouse = mouse;

        Spawn(mouse);

        hue += Time.deltaTime * colorSpeed;

        if (hue > 1f)
            hue -= 1f;
    }

    void Spawn(Vector3 mouse)
    {
        UIBrush brush = brushPool.Get<UIBrush>();

        Vector3 world = Camera.main.ScreenToWorldPoint(mouse);

        world.z = 0;

        brush.transform.position = world;

        brush.transform.rotation =
            Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        brush.transform.localScale =
            Vector3.one * Random.Range(minScale, maxScale);

        brush.SetColor(Color.HSVToRGB(hue, 0.9f, 1));

        brush.OnSpawn();
    }
}