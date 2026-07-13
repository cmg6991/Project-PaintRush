using UnityEngine;

public abstract class PoolableObject : MonoBehaviour
{
    private ObjectPool pool;

    public void SetPool(ObjectPool objectPool)
    {
        pool = objectPool;
    }

    public virtual void OnSpawn()
    {

    }

    public virtual void OnRelease()
    {

    }

    public void ReturnPool()
    {
        OnRelease();

        pool.Release(this);
    }
}