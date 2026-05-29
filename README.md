# Ratchemy-Defense
유니티 중간 프로젝트 프로토타입

---

## 작업 로그

### 2026-05-29 — Member_Yoon

#### FlameSlime 적 유닛 개선 (T2 / T3)

---

##### 1. 폭발 딜레이 조정
- `FlameSlimeData_T3` SO의 `explosionDelay` 값을 **3초 → 1초**로 변경

---

##### 2. 오라 공격 범위 시각화 (FlameSlimeTier2)
- 런타임에 `LineRenderer`로 **주황색 원**을 그려 실제 오라 공격 판정 범위를 시각적으로 표시
- `AuraEffect` 자식 오브젝트의 `lossyScale.x` 값을 기준으로 반지름 자동 설정
  - 프리팹에서 `AuraEffect Scale X` = 원하는 반지름 값으로 설정하면 자동 동기화
- 오라 데미지 발동 시 원이 **흰색으로 번쩍**이는 펄스 피드백 추가
- `#if UNITY_EDITOR || DEVELOPMENT_BUILD` 조건으로 **릴리즈 빌드에서 자동 제거**
- Inspector `Show Debug Circles` 토글로 에디터에서도 켜고 끄기 가능 (기본값: OFF)

---

##### 3. 오라 이펙트 범위 동기화 (FlameSlimeTier2)
- `auraEffectTransform`을 `protected`로 변경해 T3에서 접근 가능하도록 수정
- 실제 공격 판정(`Physics2D.OverlapCircleAll`)도 `runtimeAuraRadius` 기반으로 통일
  - SO의 `auraRadius` 대신 AuraEffect Scale에서 읽은 값 사용

---

##### 4. 자폭 시 AuraEffect 비활성화 (FlameSlimeTier3)
- `PreExplode` 트리거 직전 `AuraEffect` GameObject를 비활성화
- Fuming → Explode 구간 동안 오라 이펙트가 표시되지 않음
- `Destroy(gameObject)` 이후 자동 제거되므로 별도 복원 처리 없음

---

##### 5. 폭발 범위 시각화 (FlameSlimeTier3)
- 자폭 시작(Fuming 진입) 시 **빨간색 원**으로 폭발 판정 범위 표시
- 오라 범위 원과 동일하게 에디터/개발 빌드 전용, `Show Debug Circles` 토글 공유
- `CreateDebugCircle()` 헬퍼 메서드를 `FlameSlimeTier2`에 `protected`로 선언해 T3에서 재사용

---

##### 6. 폭발 판정 중심점 오프셋 (FlameSlimeTier3)
- `FlameSlimeTier3Data`에 `explosionOffset` (Vector2) 필드 추가
- 슬라임 피벗이 발 아래에 있을 경우 Y 오프셋을 줘서 폭발 판정 중심을 위로 조정 가능
- 디버그 빨간 원도 동일한 오프셋 적용 → 시각적 위치와 실제 판정 위치 일치

---

##### 변경된 파일 목록
| 파일 | 변경 내용 |
|---|---|
| `FlameSlimeTier2.cs` | 오라 범위 시각화, AuraEffect 동기화, 디버그 원 |
| `FlameSlimeTier3.cs` | AuraEffect 비활성화, 폭발 범위 원, 오프셋 적용 |
| `FlameSlimeTier3Data.cs` | `explosionOffset` 필드 추가 |
| `FlameSlimeData_T3.asset` | `explosionDelay` 1초, `explosionOffset` 기본값 설정 |
