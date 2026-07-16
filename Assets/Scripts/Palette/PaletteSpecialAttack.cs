using UnityEngine;
using UnityEngine.InputSystem;

public class PaletteSpecialAttack : MonoBehaviour
{
    [Header("참조")]
    [SerializeField] private PaletteInventory inventory;
    [SerializeField] private MonsterManager monsterManager;

    [Header("공격")]
    [SerializeField, Min(1)] private int damage = 999;
    [SerializeField, Min(1)] private int consumeCount = 1;
    [SerializeField] private bool consumeWhenNoMonster = false;

    [Header("연출")]
    [SerializeField] private GameObject effectPrefab;
    [SerializeField] private Transform effectSpawnPoint;

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = GetComponent<PaletteInventory>();
        }

        if (monsterManager == null)
        {
            monsterManager = MonsterManager.Instance;

            if (monsterManager == null)
            {
                monsterManager = FindAnyObjectByType<MonsterManager>();
            }
        }
    }

    // PlayerInput의 Behavior가 Send Messages일 때
    // "PaletteAttack" 액션을 만들면 이 함수가 자동 호출될 수 있다.
    public void OnPaletteAttack(InputValue value)
    {
        if (value.isPressed)
        {
            TryActivate();
        }
    }

    // UI Button의 OnClick에도 직접 연결할 수 있다.
    public bool TryActivate()
    {
        if (inventory == null)
        {
            Debug.LogWarning("PaletteInventory가 없습니다.");
            return false;
        }

        if (monsterManager == null)
        {
            Debug.LogWarning("MonsterManager가 없습니다.");
            return false;
        }

        if (!inventory.HasPalette)
        {
            Debug.Log("보유한 팔레트가 없습니다.");
            return false;
        }

        int aliveCount = monsterManager.AliveCount;

        if (aliveCount <= 0 && !consumeWhenNoMonster)
        {
            Debug.Log("공격할 몬스터가 없어 팔레트를 소비하지 않습니다.");
            return false;
        }

        if (!inventory.TryConsume(consumeCount))
        {
            return false;
        }

        int attackedCount = monsterManager.DamageAll(
            damage,
            Color.white,
            gameObject,
            true
        );

        SpawnEffect();

        Debug.Log(
            $"팔레트 특수 공격 발동! 공격 대상: {attackedCount}"
        );

        return true;
    }

    private void SpawnEffect()
    {
        if (effectPrefab == null)
        {
            return;
        }

        Vector3 spawnPosition =
            effectSpawnPoint != null
                ? effectSpawnPoint.position
                : transform.position;

        Instantiate(
            effectPrefab,
            spawnPosition,
            Quaternion.identity
        );
    }
}
