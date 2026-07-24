# Game Project Rules & System Specifications

이 문서는 프로젝트 개발 규칙, 단계별 학습 지침, 그리고 유니티 6 플랫포머 액션 게임의 핵심 시스템(멀티플 씬, 안전 필터링, 자석 흡수 시스템)에 대한 협업 규약 및 명세를 정의합니다.

---

## 1. 프로젝트 개발 규칙 & 가이드라인 (Project Rules)

### A. 인코딩 규칙 (Encoding Constraints)
- 이 프로젝트의 C++ 소스 파일(`.cpp`, `.h`)들은 한국어 Windows 환경에서 작성되어 **CP949 (EUC-KR)** 인코딩을 사용합니다.
- 파일을 수정하거나 새로 작성할 때 한글 주석이 깨지지 않도록 인코딩을 항상 보존하여 편집해야 합니다.
- 터미널을 통해 파일을 읽을 때는 인코딩 변환(CP949 -> UTF-8)을 수행하여 출력의 가독성을 유지해야 합니다.
- **단, 마크다운 문서 파일(`.md`)은 에디터 호환성을 위해 UTF-8로 보존합니다.**

# 모호한 요청 명확화

## 언제 발동하나

다음 중 하나라도 해당하면, 코드를 바로 수정/구현하기 전에 이 스킬을 따른다.

- 지시어만 있고 대상이 없음: "이거 고쳐줘", "더 낫게 해줘", "정리해줘"
- 대상은 있으나 범위가 불명확: 어떤 씬/폴더/스크립트인지, 얼마나 넓게 손댈지 불특정
- 구현 방식이 여러 갈래로 갈리고 각각 결과물이 달라짐 (예: 저장 위치, 네이밍 규칙, UI 방식)
- 이 변경이 다른 코드/문서/팀원 작업에 영향을 줄 수 있는데 그 영향 범위가 불확실

이 스킬은 **추천하기 전에, 애초에 무엇을 추천해야 할지조차 불확실한 경우**를 다룬다.

반대로 다음은 발동 대상이 아니다: 요청이 구체적이고 대상/범위가 명확한 경우, 컨벤션 문서를 보면 답이 정해지는 경우. 이때는 바로 실행한다 - 매번 확인 질문을 던지면 오히려 방해가 된다.

## 방법

1. **추측으로 채우지 않는다.** 불명확한 부분을 그럴듯한 기본값으로 임의 해석해서 진행하지 않는다.
2. **무엇이 불명확한지 먼저 코드/문서를 읽고 좁힌다.** 탐색 없이 바로 사용자에게 되묻지 않는다 - 예/아니오로 답할 수 있는 질문은 최대한 스스로 조사해서 없앤 뒤, 정말 사용자만 결정할 수 있는 지점만 질문으로 남긴다.
3. **AskUserQuestion 툴로 구체적 선택지를 제시한다.** "어떻게 할까요?" 식의 열린 질문 대신, 조사한 내용을 바탕으로 2~4개의 실제 대안을 제시하고 근거를 덧붙인다. 추천 대안이 있으면 첫 번째 옵션에 "(권장)"을 붙인다.
4. **질문은 실행에 필요한 만큼만.** 한 번에 필요 이상으로 여러 질문을 쏟아내지 않는다 - 결정이 서로 독립적이면 한 번에 묶어 물어도 되지만, 뒤 질문이 앞 질문의 답에 따라 달라지면 순서대로 나눈다.
5. **답을 받으면 그 결정을 반영해 계획/코드를 진행한다.** 같은 사안을 다시 묻지 않는다.

## 예시

```
사용자: "이거 커밋 메시지 형식 좀 정리해줘"

❌ 바로 임의로 Conventional Commits 형식으로 재작성
✅ 기존 커밋 로그/문서를 먼저 확인 -> 이미 확립된 관례가 없음을 파악
   -> AskUserQuestion으로 "타입 태그 스타일(영어 vs 한글)",
      "scope 표기 방식" 등 실제로 결과물이 갈리는 지점만 질문
```
### C. 수정 범위 (Scope of Modifications)
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

---

## 3. 맵 에디터 & 타일 관리 및 스탯 시스템 현황 (System Implementation Status)

### A. 기획 배경 (Background & Needs)
* **협업 생산성 극대화**: 팀원들이 각자 디자인한 맵을 단일 씬 파일에 직접 마구잡이로 배치하다 보면 Git 충돌(Merge Conflict)이 100% 발생합니다. 이를 원천 차단하기 위해 맵 정보를 런타임에 JSON 파일로 동적 로드하는 시스템을 구축했습니다.
* **에디터 편의성 및 데이터 이관**: 기존 `MapGenerator` 및 `MapTestLauncher` 구성을 `MapEditor` / `MapEditorCustom`과 `TileManager`로 역할을 통합 이관하여 에디터 씬 뷰 드로잉 기반 배치와 런타임 자동 로딩/색상 배분 시스템으로 단순화했습니다.

### B. 핵심 시스템 구조 및 역할 분담 (Core Architecture)

#### ① 스탯 전용 싱글톤 매니저 ([DataManager.cs](file:///d:/GitHub/Project-PaintRush/Assets/Scripts/Manager/DataManager.cs))
* **역할 전술적 축소**: 기존 맵 JSON 세이브/로드 책임을 전면 분리하고, **플레이어 및 몬스터 동적 스탯 관리 전용 매니저**로 직무를 명확히 다듬었습니다.
* **`PlayerStat`**: 플레이어의 HP (`currentHp`, `maxHp`), 잉크 보유량 (`redInk`, `greenInk`, `blueInk`), 총의 물감 충전 정보(`currentColorHex`, `colorAmount`) 관리.
* **`MonsterStat`**: 몬스터 ID별 체력, 이동/추격 속도, 감지/공격 범위, 공격력 및 속성 타입(`currentElement`)을 딕셔너리로 관리하고 런타임 동기화 API 제공.

#### ② 씬 뷰 에디터 & JSON 엑스포터 ([MapEditor.cs](file:///d:/GitHub/Project-PaintRush/Assets/Scripts/Map/MapEditor.cs) / [MapEditorCustom.cs](file:///d:/GitHub/Project-PaintRush/Assets/Scripts/Editor/MapEditorCustom.cs))
* **에디터 배치/지우개 모드**: `MapEditorCustom`을 통해 씬 뷰 상에서 그리드 단위 스냅 프리뷰 지원, 배치 모드(`isPaintMode`) 및 지우개 모드(`isEraserMode` / `Shift+클릭`) 제공.
* **JSON 저장/로드 연동**: 씬 내 배치된 타일들의 위치, 회전, 스케일, 스프라이트 색상을 직렬화하여 `Resources/Maps/{mapName}.json` 파일로 물리 저장(`SaveSceneToJson`) 및 에디터 씬 복원(`LoadJsonToEditorScene`) 처리.

#### ③ 런타임 맵 로더 & 동적 색상 배분 ([TileManager.cs](file:///d:/GitHub/Project-PaintRush/Assets/Scripts/Manager/TileManager.cs))
* **다중 경로 맵 로드**: `persistentDataPath`, `Resources/Maps/`, `Resources.Load` 순서로 JSON 데이터를 안전하게 읽어와 런타임 씬에 타일 프리팹을 동적 생성.
* **유연한 프리팹 매칭 (Fuzzy Match)**: 복사본 접미사(` (1)` 등) 및 대소문자 차이를 흡수하여 안전하게 타일 개별 생성.
* **기믹 분류 및 `ColorMinus` 자동 부여**: 사다리(`Ladder`), 행거(`Grab`/`Hanger`), 함정(`Spike`) 등 물리 특수 타일을 예외 처리하고 일반 블록에만 `ColorMinus` 컴포넌트 자동 주입.
* **Fisher-Yates 무작위 색상 배분**: 주입된 밸런스 비율(`redPercent`, `bluePercent`, `yellowPercent`)에 맞춰 블록들의 색상을 무작위 셔플 후 머티리얼 및 스프라이트 색상 자동 적용.

### C. 마이그레이션 완료 사항 (Migration Summary)
* 기존 `MapGenerator.cs` 및 `MapTestLauncher.cs`는 삭제/정리되었으며, 맵 생성 및 세이브 기능은 `MapEditor` / `MapEditorCustom`으로, 런타임 타일 조립 및 기믹 처리는 `TileManager`로 성공적으로 이관 통합 완료되었습니다.

