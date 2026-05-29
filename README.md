# Ratchemy-Defense
유니티 중간 프로젝트 프로토타입

---

## 작업 로그

### 2026-05-29 — Member_Yoon

#### FlameSlime 적 유닛 시스템 구현 및 개선

---

### 구현 목록

#### 1. EnemyUnit 기반 클래스 (`EnemyUnit.cs`)
- 모든 적 유닛의 공통 기반 클래스
- `EnemyUnitData` ScriptableObject 연동 (HP, 공격력, 이동속도, 공격범위, 공격속도)
- `Update()`에서 `Physics2D.OverlapCircle`로 Player 레이어 감지 → 범위 내 공격, 범위 밖 이동
- `TakeDamage()` / `Heal()` / `IsDead()` 공통 메서드 제공
- `OnDie()` 가상 메서드 → 자식 클래스에서 오버라이드하여 사망 처리 커스터마이즈 가능

---

#### 2. 화염 슬라임 1단계 (`FlameSlime.cs` + `FlameSlimeData.cs`)
- `EnemyUnit`을 상속하는 T1 슬라임
- Animator 연동: `IsWalking`(bool), `Attack`(trigger), `Die`(trigger) 파라미터 사용
- 이동 시 `IsWalking = true`, 공격 시 `IsWalking = false` 및 `Attack` 트리거
- 사망 시 `Die` 트리거 → `dieAnimDuration`(Inspector 설정) 후 `Destroy(gameObject)`
- **`FlameSlimeDebuff` 시스템**: `ExplosionDisabled` 디버프로 T3 폭발 봉인 가능
  - `ApplyDebuff()` / `RemoveDebuff()` 메서드 제공
- `FlameSlimeData` : T1 전용 데이터 ScriptableObject (`EnemyUnitData` 상속)

---

#### 3. 화염 슬라임 2단계 (`FlameSlimeTier2.cs` + `FlameSlimeTier2Data.cs`)
- `FlameSlime`을 상속하는 T2 슬라임
- **화염 오라 데미지**: `auraInterval`(기본 3초)마다 반지름 `auraRadius` 범위 내 플레이어에게 `auraDamage` 적용
- `AuraEffect` 자식 오브젝트의 Animator에 `AuraPulse` 트리거로 데미지와 이펙트 애니메이션 동기화
- **오라 반지름 자동 동기화**: `AuraEffect`의 `lossyScale.x` 값을 `runtimeAuraRadius`로 사용
  - 프리팹에서 `AuraEffect Scale X` = 원하는 반지름으로 설정하면 공격 판정도 자동 일치
- **디버그 범위 시각화** (에디터/개발 빌드 전용):
  - `LineRenderer`로 주황색 원(`AuraRangeCircle`) 생성
  - 오라 데미지 발동 시 원이 흰색으로 펄스 피드백
  - `Inspector > Show Debug Circles` 토글로 ON/OFF (기본값: OFF)
  - `#if UNITY_EDITOR || DEVELOPMENT_BUILD`로 릴리즈 빌드에서 자동 제거
- `FlameSlimeTier2Data` : T2 전용 데이터 (`auraRadius`, `auraDamage`, `auraInterval`)

---

#### 4. 화염 슬라임 3단계 (`FlameSlimeTier3.cs` + `FlameSlimeTier3Data.cs`)
- `FlameSlimeTier2`를 상속하는 T3 슬라임 (T2 오라 능력 유지)
- **원거리 투사체 공격**: 근접 공격 대신 `FlameProjectile` 프리팹을 타겟 방향으로 발사
  - 공격 타이밍은 애니메이션 이벤트(`FireProjectile()` 함수 연결)로 정확하게 제어
- **사망 시 자폭 시퀀스** (`isSkillDisabled = false`인 경우):
  1. 이동/공격 정지, Collider 비활성화 (타겟 불가 처리)
  2. `AuraEffect` 비활성화 (Fuming 구간부터 오라 이펙트 숨김)
  3. `PreExplode` 트리거 → Fuming 애니메이션 재생
  4. `explosionDelay`(현재 1초) 동안 스프라이트 깜박임 (`blinkInterval` = 0.1초)
  5. `Explode` 트리거 → 폭발 애니메이션 재생
  6. `explosionAnimDuration` 후 `explosionRadius` 범위 내 `explosionDamage` 데미지
  7. `Destroy(gameObject)`
- **`isSkillDisabled = true`** 시: 폭발 없이 일반 Die 애니메이션 후 제거
- **폭발 중심점 오프셋**: `explosionOffset`(Vector2)로 판정 위치 조정 가능 (피벗이 발 아래일 때 유용)
- **폭발 범위 시각화** (에디터/개발 빌드 전용):
  - `CreateDebugCircle()` (T2에서 `protected` 상속)로 빨간색 원(`ExplosionRangeCircle`) 생성
  - `showDebugCircles` 토글 공유
- `FlameSlimeTier3Data` : T3 전용 데이터 (`explosionDelay`, `explosionRadius`, `explosionDamage`, `explosionOffset`, `projectilePrefab`, `projectileSpeed`)

---

#### 5. 화염 투사체 (`FlameProjectile.cs`)
- T3 슬라임이 발사하는 투사체 스크립트
- `Initialize(dir, speed, damage)` 호출로 방향/속도/데미지 설정
- `Update()`에서 World Space 기준 직선 이동
- `OnTriggerEnter2D`에서 Player 컴포넌트 감지 시 `TakeDamage()` 후 `Destroy(gameObject)`
- `lifetime`(기본 5초) 초과 시 자동 제거

---

#### 6. 애니메이션 / 아트 리소스
- **Animator Controller**: `FireSlime.controller` (T1/T2), `FireSlime_T3.controller` (T3), `AuraEffect.controller`, `FireProjectiles.controller`
- **Animation Clips**: `Idle`, `Move`, `Attack`, `Fire`, `Fuming`, `Explode`, `Die`, `Aura`, `Aura_Idle`
- **Sprites**: 슬라임 애니메이션 스프라이트 시트 (`slime_idle1~3`, `slime_move`, `slime_Attack`, `slime_jump`, `slime_die`, `slime_hit`, `Aura`), 이펙트 스프라이트
- **프리팹**: `FireProjectile.prefab`
- **ScriptableObject 에셋**: `FlameSlimeData_T1.asset`, `FlameSlimeData_T2.asset`, `FlameSlimeData_T3.asset`
- **테스트 씬**: `JH_TEST_Scene.unity`

---

#### 7. 오늘 개선/수정 사항 (작업 중 발생한 이슈 대응)
| 항목 | 내용 |
|---|---|
| 폭발 딜레이 | `explosionDelay` 3초 → 1초 |
| 오라 범위 시각화 | 주황 LineRenderer 원, 데미지 펄스 피드백 추가 |
| 오라 이펙트 동기화 | AuraEffect Scale X → 공격 판정 반지름 자동 연동 |
| AuraEffect 비활성화 | T3 Fuming 진입 시 AuraEffect 꺼짐 |
| 폭발 범위 시각화 | 빨간 LineRenderer 원, Show Debug Circles 토글 |
| 폭발 판정 오프셋 | `explosionOffset` 필드로 중심점 조정 가능 |
| 인게임 원 숨김 | `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD` 로 릴리즈 빌드 자동 제거 |

---

### 변경 파일 목록
```
Assets/_Workspaces/Member_Yoon/1.Scripts/
  EnemyUnit.cs
  FlameSlime.cs / .meta
  FlameSlimeData.cs / .meta
  FlameSlimeTier2.cs / .meta
  FlameSlimeTier2Data.cs / .meta
  FlameSlimeTier3.cs / .meta
  FlameSlimeTier3Data.cs / .meta
  FlameProjectile.cs / .meta

Assets/_Workspaces/Member_Yoon/2.Prefabs/
  FireProjectile.prefab / .meta

Assets/_Workspaces/Member_Yoon/3.Art/
  Animations/ (AuraEffect, FireProjectiles, FireSlime, FireSlime_T3 컨트롤러 및 클립 전체)
  Sprite/Effect/ (02.png, 03.png, GandalfHardcore Projectiles4.png)
  Sprite/Slimes/ (Aura, slime_Attack, slime_die, slime_hit, slime_idle1~3, slime_jump, slime_move)

Assets/_Workspaces/Member_Yoon/4.Scenes/
  JH_TEST_Scene.unity / .meta

Assets/_Workspaces/Member_Yoon/6.SO/
  FlameSlimeData_T1.asset / .meta
  FlameSlimeData_T2.asset / .meta
  FlameSlimeData_T3.asset / .meta
```
