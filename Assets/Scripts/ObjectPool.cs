using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    [SerializeField] private PoolableObject prefab;
    [SerializeField] private int startCount = 50;

    private Queue<PoolableObject> pool = new();

    private void Awake()
    {
        for (int i = 0; i < startCount; i++)
            CreateObject();
    }

    private PoolableObject CreateObject()
    {
        PoolableObject obj = Instantiate(prefab, transform);

        obj.SetPool(this);
        obj.gameObject.SetActive(false);

        pool.Enqueue(obj);

        return obj;
    }

    public T Get<T>() where T : PoolableObject
    {
        if (pool.Count == 0)
            CreateObject();

        PoolableObject obj = pool.Dequeue();

        obj.gameObject.SetActive(true);

        return obj as T;
    }

    public void Release(PoolableObject obj)
    {
        obj.gameObject.SetActive(false);
        pool.Enqueue(obj);
    }
}
