# Ratchemy-Defense
유니티 중간 프로젝트 프로토타입

---

## 작업 로그

### 2026-06-05 (오후) — Member_Yoon : 버그 수정 + 보스 웨이브 체력 감소 + 리팩토링

| 항목 | 내용 |
|---|---|
| **BossBase** | 웨이브 클리어마다 보스 최대 체력 20% 감소 — `WaveManager.CurrentWaveIndex` 감시 + WaveUI "BOSS" 텍스트로 4번째 클리어 감지 |
| **BossBase** | 게임 시작 시 즉시 체력 감소 버그 수정 — `WaveWatchLoop` 1프레임 대기 후 초기 인덱스 기준 설정 |
| **EnemyUnit** | `SortingOrderBase` 추가 — 유닛 타입별 정렬 우선순위 지정 가능 |
| **ThunderLizard** | `SortingOrderBase = 20` override — 슬라임과 겹칠 때 리자드가 항상 앞에 표시 |
| **ThunderLizardTier2** | `ValidateLightningFire()` 추출 — Tier2/3 중복 가드 로직 통합 |
| **ThunderLizardTier3** | `ValidateLightningFire()` 재사용으로 중복 코드 제거 |
| **FlameSlimeTier3** | 투사체 방향 계산 `transform.position` → `bounds.center` 수정 (피벗 기준 발사 버그 수정) |
| **FlameSlimeTier3** | 죽은 플레이어에게 투사체 발사되는 버그 수정 — `CurrentHp <= 0` 체크 추가 |

---

### 2026-06-05 — Member_Yoon : 체인 라이트닝 위치 보정 + Y축 정렬 적용

| 항목 | 내용 |
|---|---|
| **LightningProjectile** | `bodyHeightOffset` SerializeField 추가 — 체인 라이트닝 라인이 유닛 발 밑 대신 몸 중앙에서 시작되도록 보정 |
| **EnemyUnit** | Y축 위치 기반 `sortingOrder` 자동 갱신 추가 — 화면 아래쪽 유닛이 앞에 그려짐, `GetInstanceID()` 보조 기준으로 동일 Y 깜빡임 방지 |

---

### 2026-06-04 (오후) — Member_Yoon : HP바 UI + 투사체 사운드 + 버그 수정 + 리팩

| 항목 | 내용 |
|---|---|
| **EnemyHPBar** | `EnemyHPBar.cs` 신규 생성 — World Space Canvas + Image Filled 방식 HP바 제어 컴포넌트 |
| **EnemyUnit** | `hpBar` SerializeField 추가, `Start()` / `OnHealthChanged()` / `OnDie()` 에서 HPBar 갱신·숨김 연결 |
| **BossProjectile** | `flySound` (루프 AudioSource) + `explosionSound` (이펙트 오브젝트에 동적 AddComponent) 추가 |
| **LightningProjectile** | `audioSource` 미사용 필드 제거 |
| **LightningProjectile** | `Start()` 안전망 추가 — `Initialize()` 미호출 시 `lifetime` 기본값으로 자동 파괴 |
| **FlameSlimeTier3** | `dieSealedSound` / `explosionChargeSound` / `explosionBangSound` / `blinkInterval` 필드를 클래스 상단으로 이동 (Inspector 편의) |

#### 버그 수정

| 항목 | 내용 |
|---|---|
| **EnemyCombatUtility** | `DamagePlayersInRadius()` — HP 0 이하 플레이어 데미지 제외 |
| **FlameProjectile** | `OnTriggerEnter2D()` — HP 0 이하 플레이어 데미지 제외 |
| **LightningProjectile** | `OnTriggerEnter2D()` — HP 0 이하 플레이어 데미지 제외 |
| **LightningProjectile** | `ChainLightning()` — HP 0 이하 플레이어 체인 대상 및 데미지 제외 |

#### 미완 / 보류
- Inspector에서 각 적 프리팹에 `EnemyHPBar` 슬롯 연결 필요
- Inspector에서 `BossProjectile` 프리팹에 `flySound` / `explosionSound` 슬롯 연결 필요
- `BossBase.OnHealthChanged()` — `base.OnHealthChanged()` 누락 (보스 EnemyHPBar 미갱신, `_Project` 반영 시 수정 예정)
- `ThunderLizardTier2.LightningProjectile()` 메서드명 변경 — Unity Editor Animation Event 동시 수정 필요

---

### 2026-06-04 (오전~) — Member_Yoon : 사운드 시스템 추가 + 버그 수정

| 항목 | 내용 |
|---|---|
| **FlameSlime (T1~T3 공통)** | `attackSound` / `dieSound` / `hitSound` 필드 추가, `audioSource` (`protected`) 초기화 |
| **FlameSlime** | `PlayAttackSound()` — Animation Event 연동, `isDying` 시 재생 차단 |
| **FlameSlime** | `OnHealthChanged()` 오버라이드 — 피격 사운드 (T1~T3 공통, 사망 시 제외) |
| **FlameSlime** | `DieRoutine()` — 사망 사운드 추가 (T1·T2 공통) |
| **FlameSlimeTier3** | `dieSealedSound` / `explosionChargeSound` / `explosionBangSound` 필드 추가 |
| **FlameSlimeTier3** | `PlaySealedDeath()` — 봉인 사망 사운드, `PlayExplosionSequence()` — 예고·폭발 사운드 |
| **FlameSlimeTier3** | `attackPoint` 필드 추가 — 투사체 발사 위치를 AttackPoint 기준으로 변경 |
| **ThunderLizard (T1~T3 공통)** | FlameSlime과 동일 구조로 사운드 시스템 추가 |
| **ThunderLizardTier2/3** | `LightningProjectile()` — 투사체 발사 직전 `PlayAttackSound()` 호출 |
| **LightningProjectile** | `flySound` 필드 추가, `Start()`에서 생성 시 1회 재생 (`PlayClipAtPoint` — 카메라 위치 기준) |
| **LightningProjectile** | `flySoundVolume` SerializeField 추가 |

#### 버그 수정

| 항목 | 내용 |
|---|---|
| **EnemyCombatUtility** | `TryFindClosestPlayer()` — HP 0 이하 플레이어 타겟 제외 (죽은 유닛 재공격 방지) |
| **EnemyUnit** | `ClearRangedAttack()` — `attackTimer = 0f` 추가 (투사체 발사 후 즉시 재공격 방지) |
| **ThunderLizardTier2/3** | `LightningProjectile()` — `activeInHierarchy` 체크 추가 (비활성 타겟 발사 차단) |
| **FlameSlimeTier3** | `FireProjectile()` — `activeInHierarchy` 체크 추가 |

#### 미완 / 보류
- T3 ThunderLizard 공격 시 잠깐 사라지는 현상 — Animator Controller Attack 클립 확인 필요
- Inspector에서 각 프리팹에 `AudioSource` 컴포넌트 및 사운드 슬롯 연결 필요

---

### 2026-06-02 (오후) — Member_Yoon : BossHealthUI 연동 + BossProjectile 화염 이펙트

| 항목 | 내용 |
|---|---|
| **EnemyUnit** | `OnHealthChanged()` 가상 메서드 추가 — `TakeDamage` / `Heal` 호출 시 자동 실행 |
| **BossBase** | `BossHealthUI` 필드 추가, `OnHealthChanged()` 오버라이드 → 체력 변화 시 UI 자동 갱신 |
| **초기화** | `Start()`에서 `bossHealthUI?.SetHealth(currentHp, maxHp)` 호출 — 씬 로드 시 UI 즉시 반영 |
| **BossProjectile** | 프리팹·애니메이터 컨트롤러 업데이트 |
| **화염 이펙트** | `fire.png` 스프라이트 + `BossFireProjectile.anim` 추가 |
| **BossData SO** | BossData 에셋 업데이트 |

#### 미완 / 보류
- Inspector에서 보스 프리팹에 `BossHealthUI` 슬롯 연결 필요 (Shin 쪽 UI 오브젝트)

---

### 2026-06-02 (추가) — Member_Yoon : 타겟팅·T3 폭발 봉인·DeathExplosion 리팩

| 항목 | 내용 |
|---|---|
| **가장 가까운 타겟** | `TryFindClosestPlayer` — 사거리 내 Player 유닛 중 최근접 1명 공격 (동률 시 InstanceID) |
| **원거리 가드** | `CanFireRangedAttack` — `pendingTarget` 파괴 시 발사 취소 + `ClearRangedAttack` |
| **CoolantRat 폭발 봉인** | T3 `DeathExplosion` 1프레임 대기 → 스킬 **즉사** 시에도 폭발·데미지 차단 |
| **DeathExplosion 리팩** | `PlaySealedDeath` / `PlayExplosionSequence` 분리 (동작 동일) |

#### 미완 / 보류
- `EnemyBaseDamage.TryReachBase()` 호출 연동 (기지 트리거 — Shin 쪽)
- Boss `OnBossDead` / `RemoveAuraBuff()` WaveManager 연동
- 첫 공격 즉시 (`attackTimer` 초기값) — 선택적 밸런스 조정

---

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
