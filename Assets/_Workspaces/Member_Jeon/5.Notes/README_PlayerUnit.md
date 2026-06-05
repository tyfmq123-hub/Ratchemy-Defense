# Member_Jeon — Player Unit 작업 (feature/player-unit)

## 개요

아군 플레이어 유닛 베이스 클래스와 4종 유닛, 카드 UI 데이터 구조를 구현한 브랜치입니다.

## 폴더 구조

```
Member_Jeon/
├── 1.Scripts/
│   ├── PlayerUnitBase.cs          # 아군 공통 베이스
│   ├── PlayerUnit/
│   │   ├── InsulatorRat.cs        # 절연체 쥐 (근접)
│   │   ├── CoolantRat.cs          # 냉각 처리쥐 (원거리 + 스킬)
│   │   ├── SafetyManagerRat.cs    # 안전관리소장 쥐 (창 근접)
│   │   └── TankRat.cs             # 탱커 쥐 (고체력 + 밀치기 스킬)
│   └── UnitCard/
│       ├── UnitCard.cs            # 카드 UI
│       └── UnitCardData.cs        # 카드 ScriptableObject 데이터
├── 2.Prefabs/
│   ├── player/                    # 유닛 프리팹
│   └── Data/                      # UnitCardData 에셋
├── 3.Art/                         # 스프라이트·애니메이션
└── 4.Scenes/                      # 테스트 씬 (Jsr.unity 등)
```

## 주요 구현

### PlayerUnitBase
- `maxHp` / `currentHp` Inspector 직접 수정 가능
- `OnValidate()`로 체력 범위 자동 보정
- `attackPower`는 `int` (EnemyUnit.TakeDamage와 타입 통일)
- 기본 이동: 오른쪽 직진 (`Move()`)

### 유닛별 특징

| 유닛 | 역할 | 이동 조건 |
|------|------|-----------|
| InsulatorRat | 근접 단일 공격 | 사거리 내 적 없을 때 전진 |
| CoolantRat | 원거리 + 범위 스킬, FlameSlime 디버프 | 적 없을 때 전진 |
| SafetyManagerRat | 창 근접, 빠른 이동 | 적 없을 때 전진 |
| TankRat | 고체력, 밀치기 스킬 | 적 없을 때 전진 |

> **공통:** `attackRange` 안에 Enemy 레이어 적이 있으면 이동하지 않고 공격/스킬만 수행합니다.

### UnitCard
- `UnitCardData` (ScriptableObject)로 카드 정보 관리
- `UseCard()` — CostManager 연동 및 소환 로직 TODO

## Unity 설정

- **Player** 레이어: 아군 유닛
- **Enemy** 레이어: 적 유닛 (Member_Yoon EnemyUnit과 연동)
- 유닛 프리팹: Layer **Player**, Rigidbody2D **Kinematic**

## 테스트 방법

1. `4.Scenes/Jsr.unity` 또는 `SSH 1.unity` 열기
2. 플레이어 유닛 프리팹(`2.Prefabs/player/`)을 씬에 배치
3. 적(Member_Yoon Enemy 프리팹) Layer를 **Enemy**로 설정
4. Play → 적이 없으면 오른쪽 이동, 사거리 내 적 있으면 공격

## 의존성

- Member_Yoon `EnemyUnit.cs` — `TakeDamage(int)` 호출
- Member_Yoon `FlameSlime` — CoolantRat 스킬 디버프 연동

## TODO

- [ ] UnitCard `UseCard()` — CostManager + Instantiate 구현
- [ ] SafetyManagerRat 주석 스타일 `//` 통일
- [ ] develop 병합 후 `_Project` 반영 (팀장 담당)

## 작성자

Member_Jeon (전)
