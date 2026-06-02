# Ratchemy-Defense
유니티 중간 프로젝트 프로토타입

---

## 작업 로그

### 2026-06-02 (오전) — Member_Yoon : 적 코드 리팩 + InsulatorRat 연동

| 항목 | 내용 |
|---|---|
| **EnemyCombatUtility** | 범위 데미지·플레이어 탐지 공통 유틸 (`DamagePlayersInRadius`, `TryGetPlayer`) |
| **EnemyUnit** | `CastData<T>()`, `BeginDeath()` / `isDying` 베이스화, `AttackCooldown`, 원거리 `isAttacking` + `pendingTarget` |
| **버그 예방** | HP 0 이후 추가 피격 무시, T2 오라 사망 중 1틱 방지, 원거리 공격 애니 중 재호출 차단 |
| **InsulatorRat 연동** | `LightningProjectile` — 번개 데미지 50%, InsulatorRat 적중 시 체인 차단 |
| **리소스** | chain-Lightning 이펙트 프리팹·애니·스프라이트 추가 |

#### 리팩 적용 범위
- 오라·폭발·보스 투사체 → `EnemyCombatUtility` 사용
- SO 캐스팅 → `CastData<T>()` 통일 (FlameSlime / ThunderLizard / Boss 전 티어)
- T3·Thunder T2/T3 원거리 공격 → `BeginRangedAttack` / `ClearRangedAttack` 패턴

#### 미완 / 보류
- `EnemyBaseDamage.TryReachBase()` 호출 연동 (기지 트리거 — Shin 쪽)
- Boss `OnBossDead` / `RemoveAuraBuff()` WaveManager 연동
- 첫 공격 즉시 (`attackTimer` 초기값) — 선택적 밸런스 조정

---

### 2026-06-01 (15:00~) — Member_Yoon : 보스 시스템 + 버그 수정 + 기지 도착 처리

| 항목 | 내용 |
|---|---|
| **BossBase** | `EnemyUnit` 상속, 고정형 보스, 사망 이벤트(`OnBossDead`) |
| **BossProjectile** | 포물선 투사체 → 착탄 범위 데미지 + 폭발 이펙트 (애니메이션/파티클) |
| **오라 버프** | 시작 시 `AuraEffect` ON, `RemoveAuraBuff()`로 OFF + 주기적 화염 데미지 |
| **EnemyBaseDamage** | 적 기지 도착 시 온도 상승 처리, 중복 방지 후 제거 |
| **프리팹/SO** | Boss 프리팹, BossData SO, 적 6종 프리팹에 EnemyBaseDamage 연동 |
| **테스트 씬** | `JH_TEST2_Scene.unity` 추가 |

#### 버그 수정 / 코드 정리
- `ThunderLizard_Idle.anim` 잘못된 `LightningProjectile` 이벤트 제거
- 플레이어 감지 `GetComponentInParent<PlayerUnitBase>()` 통일
- `EnemyUnit` SO null 가드, `hp`/`maxHp` 불일치 수정
- Member_Yoon 전체 디버그 `Debug.Log` 제거
- develop 병합 (Member_Jeon PlayerUnit, Member_Shin WaveManager 반영)
- `TagManager` `Player`(L6) / `Enemy`(L7) 레이어 추가

---

### 2026-06-01 — Member_Yoon : ThunderLizard 적 유닛 시스템 구현

| 항목 | 내용 |
|---|---|
| **ThunderLizard T1** | 근접 공격 + 사망 애니메이션 (기존 구현) |
| **ThunderLizard T2** | T1 계승 + AttackPoint 기준 번개 투사체 원거리 단일 공격 |
| **ThunderLizard T3** | T2 계승 + 체인 라이트닝 (첫 타겟 적중 후 chainRange 내 최대 chainCount명 연쇄 데미지) |
| **LightningProjectile** | 투사체 직선 이동, 체인 파라미터 포함 초기화 |
| **ScriptableObject** | T2 / T3 단계별 데이터 에셋 분리 |
| **애니메이션** | T3 Attack / Move / Die 클립 및 컨트롤러 제작 |
| **프리팹** | ThunderLizard T1 / T2 / T3 프리팹 제작 |

#### 버그 수정
- `ThunderLizard.isDying` 접근 제한자 `private` → `protected` 수정 — 자식 클래스에서 `OnDie` 오버라이드 시 동일 필드 재선언으로 발생하는 Unity 직렬화 충돌(`The same field name is serialized multiple times`) 사전 방지

---

### 2026-05-29 — Member_Yoon : FlameSlime 적 유닛 시스템 구현

| 항목 | 내용 |
|---|---|
| **EnemyUnit** | 적 유닛 공통 기반 클래스 (이동, 공격, 피격, 사망) |
| **FlameSlime T1** | 근접 공격 + 사망 애니메이션 + 폭발 봉인 디버프 시스템 |
| **FlameSlime T2** | T1 계승 + 주기적 화염 오라 범위 데미지 |
| **FlameSlime T3** | T2 계승 + 원거리 투사체 공격 + 사망 시 자폭 시퀀스 |
| **FlameProjectile** | T3 투사체 (직선 이동, 플레이어 충돌 시 데미지) |
| **ScriptableObject** | T1 / T2 / T3 각 단계별 데이터 에셋 분리 |
| **애니메이션** | Idle, Move, Attack, Fire, Fuming, Explode, Die, Aura 클립 및 컨트롤러 제작 |
| **스프라이트** | 슬라임 전 동작 스프라이트 시트 + 이펙트 이미지 |
| **테스트 씬** | `JH_TEST_Scene.unity` 구성 |

#### 작업 중 개선 사항
- 자폭 딜레이 3초 → **1초**로 조정
- 오라 / 폭발 공격 범위를 런타임에 원으로 시각화 (에디터 전용, Inspector 토글)
- AuraEffect 스케일과 실제 공격 판정 반지름 **자동 동기화**
- T3 자폭 시 AuraEffect 자동 비활성화
- 폭발 판정 중심점 오프셋(`explosionOffset`) 필드 추가
