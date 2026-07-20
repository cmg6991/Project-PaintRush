using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class PlayerStat
{
    public int currentHp;
    public int maxHp;
    public int redInk;
    public int greenInk;
    public int blueInk;

    //  총의 물감 충전(FillColor) 세이브/로드 정보
    public bool hasColor;
    public string currentColorHex;
    public float colorAmount;
}

[System.Serializable]
public class MonsterStat
{
    public int currentHp;
    public int maxHp;
    public float patrolSpeed;
    public float chaseSpeed;
    public float detectRange;
    public float attackRange;
    public int attackDamage;
    public string currentElement; // ElementType enum을 문자열로 호환 보장하며 보존
}

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    public PlayerStat CurrentPlayerStat = new PlayerStat();

    // 몬스터 ID를 키로 하는 스탯 데이터 맵
    private Dictionary<string, MonsterStat> monsterStatMap = new Dictionary<string, MonsterStat>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitDefaultData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitDefaultData()
    {
        CurrentPlayerStat.maxHp = 5;
        CurrentPlayerStat.currentHp = 5;
        CurrentPlayerStat.redInk = 0;
        CurrentPlayerStat.greenInk = 0;
        CurrentPlayerStat.blueInk = 0;
    }

    // 플레이어 HP 갱신
    public void UpdatePlayerHp(int currentHp, int maxHp)
    {
        CurrentPlayerStat.currentHp = currentHp;
        CurrentPlayerStat.maxHp = maxHp;
        Debug.Log($"[DataManager] 플레이어 HP 동기화: {currentHp}/{maxHp}");
    }

    // 플레이어 잉크 보유량 갱신
    public void UpdatePlayerInk(int red, int green, int blue)
    {
        CurrentPlayerStat.redInk = red;
        CurrentPlayerStat.greenInk = green;
        CurrentPlayerStat.blueInk = blue;
    }

    // 몬스터 스탯이 저장되어 있는지 확인
    public bool HasMonsterStat(string id)
    {
        return monsterStatMap.ContainsKey(id);
    }

    // 몬스터 스탯 조회
    public MonsterStat GetMonsterStat(string id)
    {
        if (monsterStatMap.TryGetValue(id, out MonsterStat stat))
        {
            return stat;
        }
        return null;
    }

    // 몬스터 개별 스탯 갱신 API (느슨한 결합 제공)
    public void UpdateMonsterStat(string id, int currentHp, int maxHp, float patrolSpeed, float chaseSpeed, float detectRange, float attackRange, int attackDamage, string element)
    {
        if (!monsterStatMap.TryGetValue(id, out MonsterStat stat))
        {
            stat = new MonsterStat();
            monsterStatMap[id] = stat;
        }

        stat.currentHp = currentHp;
        stat.maxHp = maxHp;
        stat.patrolSpeed = patrolSpeed;
        stat.chaseSpeed = chaseSpeed;
        stat.detectRange = detectRange;
        stat.attackRange = attackRange;
        stat.attackDamage = attackDamage;
        stat.currentElement = element;

        Debug.Log($"[DataManager] 몬스터 '{id}' 동적 스탯/속성 동기화 (HP: {currentHp}/{maxHp}, Element: {element})");
    }
}