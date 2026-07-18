using System.Reflection;
using UnityEngine;

public class MonsterDataBridge : MonoBehaviour
{
    private MonsterAI monsterAI;
    private string uniqueId;
    
    // MonsterAI 내부의 private/protected 필드들을 캐싱할 리플렉션 포인터들
    private FieldInfo hpField;
    private FieldInfo maxHpField;
    private FieldInfo elementField;
    private FieldInfo patrolSpeedField;
    private FieldInfo chaseSpeedField;
    private FieldInfo detectRangeField;
    private FieldInfo attackRangeField;
    private FieldInfo attackDamageField;

    private void Awake()
    {
        monsterAI = GetComponent<MonsterAI>();
        
        // MonsterAI 내부의 private/protected/public 필드들을 리플렉션으로 안전하게 획득
        hpField = typeof(MonsterAI).GetField("currentHp", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        maxHpField = typeof(MonsterAI).GetField("maxHp", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        elementField = typeof(MonsterAI).GetField("currentElement", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        
        patrolSpeedField = typeof(MonsterAI).GetField("patrolSpeed", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        chaseSpeedField = typeof(MonsterAI).GetField("chaseSpeed", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        detectRangeField = typeof(MonsterAI).GetField("detectRange", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        attackRangeField = typeof(MonsterAI).GetField("attackRange", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        attackDamageField = typeof(MonsterAI).GetField("attackDamage", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
    }

    private void Start()
    {
        if (monsterAI == null) return;

        // 몬스터의 초기 생성 좌표를 유일한 고유 ID 키로 사용 (씬 내부 유일성 확보)
        uniqueId = $"Monster_{transform.position.x:F2}_{transform.position.y:F2}";

        if (DataManager.Instance != null)
        {
            // DataManager에 저장되어 있던 몬스터 데이터가 있는지 확인
            MonsterStat savedStat = DataManager.Instance.GetMonsterStat(uniqueId);

            if (savedStat != null)
            {
                if (savedStat.currentHp <= 0)
                {
                    // 이미 사망한 몬스터 처리
                    Debug.Log($"[MonsterDataBridge] 몬스터 '{uniqueId}'는 이미 사망한 상태이므로 즉석 소각합니다.");
                    
                    return;
                }
                else
                {
                    // 살아있는 부상 상태라면 체력을 복원 주입
                    if (hpField != null)
                    {
                        hpField.SetValue(monsterAI, savedStat.currentHp);
                    }
                    
                    // 속성 복원 (MonsterAI의 ChangeElement 함수가 존재한다면 호출)
                    if (savedStat.currentElement != "None" && savedStat.currentElement != null)
                    {
                        if (System.Enum.TryParse(savedStat.currentElement, out ElementType elType))
                        {
                            monsterAI.ChangeElement(elType);
                        }
                    }
                    Debug.Log($"[MonsterDataBridge] 몬스터 '{uniqueId}' 체력 복원 성공: {savedStat.currentHp}/{savedStat.maxHp}");
                }
            }
        }
    }

    private void Update()
    {
        // 실시간으로 몬스터의 private 수치들을 감시하여 DataManager에 갱신
        if (monsterAI != null && DataManager.Instance != null && hpField != null && maxHpField != null)
        {
            int currentHp = (int)hpField.GetValue(monsterAI);
            int maxHp = (int)maxHpField.GetValue(monsterAI);
            
            string elementStr = "None";
            if (elementField != null)
            {
                var elValue = elementField.GetValue(monsterAI);
                if (elValue != null) elementStr = elValue.ToString();
            }

            // 리플렉션을 통해 private 스탯들의 현재 실시간 값을 획득
            float patrolSpeed = patrolSpeedField != null ? (float)patrolSpeedField.GetValue(monsterAI) : 0f;
            float chaseSpeed = chaseSpeedField != null ? (float)chaseSpeedField.GetValue(monsterAI) : 0f;
            float detectRange = detectRangeField != null ? (float)detectRangeField.GetValue(monsterAI) : 0f;
            float attackRange = attackRangeField != null ? (float)attackRangeField.GetValue(monsterAI) : 0f;
            int attackDamage = attackDamageField != null ? (int)attackDamageField.GetValue(monsterAI) : 0;

            // DataManager의 몬스터 데이터 뱅크 갱신
            DataManager.Instance.UpdateMonsterStat(
                uniqueId, 
                currentHp, 
                maxHp, 
                patrolSpeed, 
                chaseSpeed, 
                detectRange, 
                attackRange, 
                attackDamage, 
                elementStr
            );
        }
    }
}
