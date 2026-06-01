# Ratchemy Defense

Unity 팀 프로젝트 — 공장 배경 디펜스 게임 프로토타입

## 프로젝트 구조

```
2.Assets/
├── _Project/          # 최종 통합 폴더 (팀장 merge 담당)
└── _Workspaces/       # 팀원 개인 작업 폴더
    ├── Member_Jeon/   # 플레이어 유닛, 카드 UI
    ├── Member_Yoon/   # 적 유닛 (EnemyUnit, FlameSlime 등)
    └── Member_Shin/   # 스폰, UI, 카메라
```

## 최근 업데이트 — Player Unit (`feature/player-unit`)

Member_Jeon에서 아군 플레이어 유닛 시스템을 구현했습니다.

### 구현 유닛

| 유닛 | 스크립트 | 역할 |
|------|----------|------|
| 절연체 쥐 | `InsulatorRat` | 근접 단일 공격 |
| 냉각 처리쥐 | `CoolantRat` | 원거리 + 범위 스킬, FlameSlime 디버프 |
| 안전관리소장 쥐 | `SafetyManagerRat` | 창 근접, 빠른 이동 |
| 탱커 쥐 | `TankRat` | 고체력 + 밀치기 스킬 |

### 핵심 스크립트

- `PlayerUnitBase` — 아군 공통 베이스 (체력, 이동, 공격)
- `UnitCard` / `UnitCardData` — 카드 UI 및 ScriptableObject 데이터

### 테스트 방법

1. `Assets/_Workspaces/Member_Jeon/4.Scenes/Jsr.unity` 또는 `SSH 1.unity` 열기
2. 플레이어 프리팹: `2.Prefabs/player/`
3. 적 프리팹(Member_Yoon) Layer를 **Enemy**로 설정
4. Play → 적이 없으면 오른쪽 이동, 사거리 내 적 있으면 공격

### Unity 레이어

| 레이어 | 용도 |
|--------|------|
| Player (6) | 아군 유닛 |
| Enemy (7) | 적 유닛 |

### 상세 문서

→ [`Assets/_Workspaces/Member_Jeon/5.Notes/README_PlayerUnit.md`](Assets/_Workspaces/Member_Jeon/5.Notes/README_PlayerUnit.md)

## 브랜치

| 브랜치 | 설명 |
|--------|------|
| `main` | 제출용 안정 버전 |
| `develop` | 개발 통합 |
| `feature/player-unit` | 아군 플레이어 유닛 작업 |
| `feature/enemyUnit` | 적 유닛 작업 (Member_Yoon) |
| `feature/UnitsSpawn` | 유닛 스폰·UI (Member_Shin) |

## 팀

- 팀장 / Merge: 전쇼릅
- Member_Jeon: 전
- Member_Yoon: 윤
- Member_Shin: 신

## TODO

- [ ] UnitCard `UseCard()` — CostManager + 유닛 소환
- [ ] `feature/player-unit` → `develop` PR 및 merge
- [ ] `_Project` 최종 반영 (팀장)
