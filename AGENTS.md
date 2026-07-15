# Game Project Rules & System Specifications

이 문서는 프로젝트 개발 규칙, 단계별 학습 지침, 그리고 유니티 6 플랫포머 액션 게임의 핵심 시스템(멀티플 씬, 안전 필터링, 자석 흡수 시스템)에 대한 협업 규약 및 명세를 정의합니다.

---

## 1. 프로젝트 개발 규칙 & 가이드라인 (Project Rules)

### A. 인코딩 규칙 (Encoding Constraints)
- 이 프로젝트의 C++ 소스 파일(`.cpp`, `.h`)들은 한국어 Windows 환경에서 작성되어 **CP949 (EUC-KR)** 인코딩을 사용합니다.
- 파일을 수정하거나 새로 작성할 때 한글 주석이 깨지지 않도록 인코딩을 항상 보존하여 편집해야 합니다.
- 터미널을 통해 파일을 읽을 때는 인코딩 변환(CP949 -> UTF-8)을 수행하여 출력의 가독성을 유지해야 합니다.
- **단, 마크다운 문서 파일(`.md`)은 에디터 호환성을 위해 UTF-8로 보존합니다.**

### B. 단계별 학습 지침 (Learning-Oriented Interaction)
- 사용자는 완성형 코드를 단순히 덮어쓰는 대신, 핵심 로직 클래스(예: `CollisionManager.cpp`)를 비우고 스스로 구현하며 학습하는 것을 선호합니다.
- 코드 전체를 한 번에 제공하지 않고, 논리적 흐름에 따라 여러 단계(Milestones)로 나누어 개념 설명, 힌트, 부분 코드를 제공하면서 점진적으로 유도해야 합니다.
- 각 단계마다 사용자가 직접 작성하거나 이해할 수 있도록 대화식으로 피드백을 주고받아야 합니다.

### C. 강사 파일 참조 및 업데이트 감지 (Reference Path & Update Check)
- 코드를 학습/구현할 때 항상 강사님의 원본 폴더인 `D:\WinAPI\Main\BounceBall` 내의 관련 파일을 확인하고, 그곳에 업데이트된 내용이나 구조가 있다면 이를 학습 지침에 반영하여 유도해야 합니다.

### D. 수정 범위 (Scope of Modifications)
- 게임 루프나 창 초기화 등 핵심 엔진/프레임워크 코드(`Game`, `Scene`, `InputManager`, `TimeManager` 등)는 명시적인 요청이 없는 한 수정하지 않고 그대로 보존합니다.
- 게임 콘텐츠 및 충돌/물리 처리 등 플레이 패턴에 밀접한 로직 위주로 실습과 수정이 이루어집니다.

---

## 2. 팀 개발 협업 규약 & 시스템 기획 명세 (System Specifications)

### A. 멀티플 씬 & 아키텍처 가이드라인 (Multi-Scene Architecture)
모든 오브젝트를 단일 씬에 배치하는 방식을 지양하고, 협업 효율과 런타임 최적화를 위해 중첩 씬 로드(Additive Scene Loading) 방식을 기본 표준으로 채택합니다.

```
[Scene_Permanent] (상시 유지 마스터 씬)
  ├── GameManager, PaletteUIManager
  └── Screen-Space UI (HP, 물감 인벤토리 스택 등)
         ▲
         ├─ [Additive Load] ─► [Scene_Stage_01] (타일맵 지형, 몬스터, 아이템 배치)
         └─ [Additive Load] ─► [Scene_Stage_02] (2스테이지 레벨 디자인 영역)
```

- **Scene_Permanent (상시 유지 씬)**:
  - 게임이 실행된 순간부터 종료될 때까지 메모리에 상주하며 절대 파괴되지 않는 물리적 심장부입니다.
  - `GameManager`, `SoundManager` 등 싱글톤 매니저와 플레이어의 핵심 데이터(HP, 물감 획득 스택)를 나타내는 Screen Space - Overlay Canvas를 관리합니다.
- **Scene_Stage_XX (스테이지 독립 씬)**:
  - 레벨 디자이너와 타일맵 담당자가 작업하는 구역입니다.
  - 각 스테이지의 순수 맵 지형, 몬스터 스폰 배치, 드롭될 물감 아이템 오브젝트들만 배치합니다.
- **이점**: 팀원들이 각자 스테이지 씬을 나누어 작업하므로, 동일한 씬을 동시에 수정하다 발생하는 GitHub 머지 충돌을 100% 방지할 수 있습니다.

### B. 코드 상호 존중 및 안전 필터링 규약 (Safety Filter)
- 본 프로젝트의 `PlayerController2D.cs`는 사다리(Ladder) 등반, 행거(Hanger) 매달리기 오프셋, 점프 물리 가속도, 스프라이트 플립(Flip) 등 핵심 물리와 애니메이션이 매우 민감하게 결합된 심장부 코드입니다.
- 기존 팀원들의 코드를 손상시키지 않고 새로운 기믹(예: 자석 흡수 시스템)을 안전하게 결합하기 위해 **"얼리 리턴(Early Return) 기반의 예외 필터링 규칙"**을 적용합니다.

```csharp
// PlayerController2D.cs 내 충돌 함수 예시
private void OnTriggerEnter2D(Collider2D collision)
{
    // [안전 필터 1] 물감 아이템과의 가짜 충돌은 즉시 패스하여 기존 플랫포머 기믹 완벽 보호
    if (collision.GetComponent<ColorDropItem>() != null) return;

    // [안전 필터 2] 몬스터의 감지용 투명 레이더(Trigger)에 의한 억까 피격 오작동 완벽 필터링
    if (collision.gameObject == this.gameObject && collision.isTrigger) return;

    // -------------------------------------------------------------------------
    // 기존 팀원들이 작성한 고유 플랫포머 기믹 로직은 단 한 줄도 건드리지 않고 그대로 흘러감
    // -------------------------------------------------------------------------
    if (collision.CompareTag("Ladder")) { ... }
    else if (collision.CompareTag("Enemy")) { ... }
}
```

### C. 공용 자석 흡수 시스템 기획 (Magnet Pull System)
- 셰이더 및 머티리얼 최적화와 리소스 관리를 위해, 색상별 아이템 프리팹을 따로 구워 사용하지 않고 **단 하나의 공용 아이템 프리팹 구조**를 지향합니다.

```
🧍 [Player (본체)]
    └── 🔍 [MagnetSensor (자식 오브젝트)] (Radius: 4f, Is Trigger: ON)
             │
             ├── (범위 내 ColorDropItem 감지)
             └── ⚡ [StartMagnet(target, speed)] 원격 호출 시전!
                     │
                     ▼
             🔮 [ColorDropItem] 둥둥 연출 즉시 정지 ➡️ 플레이어 본체 좌표로 가속 돌진 및 흡수
```

- **시스템 인스펙터 구조 약속 (팀원 공유용)**:
  - **Player 본체**: 태그 `"Player"`, 레이어 `"Player"` 필수 지정.
  - **MagnetSensor (Player의 자식 빈 오브젝트)**:
    - Circle Collider 2D 장착 -> Is Trigger 체크 필수(ON) -> Radius는 4 내외로 설정.
    - `PlayerMagnet.cs` 스크립트 탑재.
  - **ColorDropItem (공용 물감 아이템 프리팹)**:
    - 태그 `"Item"`, 레이어 `"Item"` 필수 지정.
    - Collider 2D 장착 -> Is Trigger 체크 필수(ON).
    - Rigidbody 2D 장착 -> Body Type을 Kinematic으로 필수 지정 (중력의 영향을 차단하여 비행 연출 유연화).
    - `ColorDropItem.cs` 스크립트 탑재 후 인스펙터의 Item Color에 첫 글자 대문자로 "Red", "Green", "Blue" 등 지정.

### D. 핵심 스크립트 명세
- **① `ColorDropItem.cs` (공용 아이템)**:
  - 평소에는 수학적 사인파(sin)를 그리며 제자리에서 부드럽게 둥둥 떠다니다가, 플레이어 센서가 원격으로 자석 모드를 켜주면 플레이어 본체로 속도가 점진적으로 가속되며 정밀 흡수됩니다.
- **② `PlayerMagnet.cs` (자석 센서 제어기)**:
  - 플레이어 하위 센서 오브젝트에 장착되어 범위 안의 아이템을 감지합니다.
  - **[팀원 배려 최적화]** 게임이 시작되자마자 코드를 통해 Player 레이어와 Item 레이어 간의 물리 충돌을 자동으로 무시하도록 설정하여 불필요한 몸통 판정 꼬임 현상을 원천 차단합니다.
- **③ `PlayerController2D.cs` (안전 예외 처리 탑재본)**:
  - 기존의 다중 기믹(사다리, 행거 등) 오프셋과 애니메이션 연산을 철저하게 보장하면서, 날아온 아이템 충돌이나 몬스터의 원거리 감지용 범위 콜라이더 닿음으로 인해 플레이어가 억울하게 제자리에서 피격 모션을 뿜어내던 기형적인 버그를 완치합니다.
