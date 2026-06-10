# Ratchemy Defense — QA 정리 문서

> **버전:** 1.2 (제출용 결과서 포함)  
> **최종 갱신:** 2026-06-10  
> **브랜치:** `Test` (`a814437`)  
> **Unity:** 6000.4.8f1 · URP 2D · 1920×1080  

---

## 목차

1. [한눈에 보기](#1-한눈에-보기)
2. [테스트 시작 방법](#2-테스트-시작-방법)
3. [빌드 정보](#3-빌드-정보)
4. [스모크 테스트 (필수)](#4-스모크-테스트-필수)
5. [기능별 QA 체크리스트](#5-기능별-qa-체크리스트)
6. [알려진 이슈](#6-알려진-이슈)
7. [최근 변경 / QA 주의](#7-최근-변경--qa-주의)
8. [자산 현황](#8-자산-현황)
9. [QA 결과 기록 (팀 내부용)](#9-qa-결과-기록-팀-내부용)
10. [QA 실행 결과 (제출용)](#10-qa-실행-결과-제출용)
11. [버그 및 이슈 기록 (제출용)](#11-버그-및-이슈-기록-제출용)
12. [최종 QA 판정 (제출용)](#12-최종-qa-판정-제출용)

---

## 1. 한눈에 보기

### 게임 개요

| 항목 | 내용 |
|------|------|
| 장르 | 2D 레인 디펜스 / 웨이브 서바이벌 |
| 핵심 루프 | 스테이지 → 스토리 만화 → 유닛 정보 → 배틀 → 승리 or 패배 |
| 승리 조건 | 보스 처치 (`BossBase.OnBossDead` → `GameManager.Victory`) |
| 패배 조건 | 배터리 130°C 열폭주 (`BaseHealth` → `GameManager.Defeat`) |
| 팀 분업 | Yoon(적/UnitInfo) · Jeon(아군/결과UI) · Shin(플로우/웨이브/기지) |
| 코드 위치 | `Assets/_Workspaces/` (77 스크립트) · `_Project` 미사용 |

### 씬 플로우

```
0.App → 1.Start → 2.Stage → 3.Story → 4.UnitInfo → 5.Battle
                                              ↓
                                    승리(보스) / 패배(130°C)
                                              ↓
                                    만화 컷 → 결과 UI
```

### 구현 완료 vs 미완

| 구분 | 항목 |
|------|------|
| **완료** | Additive 씬 전환, 5종 아군+스킬, 6종 적+보스, 웨이브/코스트, 기지 온도, 승패 결과 UI, Unit Info UI, BGM, 배틀 카메라 |
| **부분** | UnitInfo 스킬/팝업 데이터, 적 사운드·HP바 Inspector, 스테이지 13개 UI(단일 경로 가능) |
| **미완** | App 씬 Inspector, AudioListener, Boss HP바 base 호출, `_Project` 통합 폴더 |

---

## 2. 테스트 시작 방법

### 에디터

1. Unity **6000.4.8f1** 로 프로젝트 열기
2. **`Assets/Scenes/0.App.unity`** 열기
3. Play Mode 실행

### Windows 빌드

```
Builds/Windows/Ratchemy_Defense.exe
```

`Ratchemy_Defense_Data` 폴더와 **같은 경로**에 있어야 합니다.  
배포 시 `Builds/Windows` 폴더 전체를 복사하세요.

### 권장 테스트 순서

1. 에디터 스모크 테스트 (§4)
2. Windows 빌드 동일 스모크
3. 아군 5종 · 적 T1~T3 · 보스 개별 테스트 (§5)
4. 리트라이 / 씬 왕복 3회 이상 (몹 잔존·AudioListener 확인)

---

## 3. 빌드 정보

### 빌드 명령

```powershell
& "C:\Program Files\Unity\Hub\Editor\6000.4.8f1\Editor\Unity.exe" `
  -batchmode -quit -nographics `
  -projectPath "C:\Users\user\Documents\Workspace\Team\Ratchemy_Defense" `
  -executeMethod BuildPlayer.BuildWindows `
  -logFile "Builds\build.log"
```

| 항목 | 값 |
|------|-----|
| 스크립트 | `Assets/Editor/BuildPlayer.cs` |
| 출력 | `Builds/Windows/Ratchemy_Defense.exe` |
| 타겟 | Standalone Windows 64-bit |
| 포함 씬 | 6개 (EditorBuildSettings 전부) |
| **최종 빌드** | **2026-06-10 성공** (~381 MB) |
| 주의 | Unity 에디터가 같은 프로젝트를 열면 배치 빌드 실패 |

> `Builds/` 는 `.gitignore` 대상 — Git에 포함되지 않음

---

## 4. 스모크 테스트 (필수)

릴리즈·merge·빌드 전 **반드시** 확인합니다.

| # | 항목 | Pass |
|---|------|------|
| S-01 | `0.App` Play → 타이틀 화면 진입 | ☐ |
| S-02 | START → Stage → Story → UnitInfo → Battle 전체 진입 | ☐ |
| S-03 | 유닛 카드 5종 각 1회 이상 소환 | ☐ |
| S-04 | 웨이브 1회 클리어 + 보스 스테이지 진입 | ☐ |
| S-05 | 보스 처치 → 승리 만화 → VictoryResultUI | ☐ |
| S-06 | 적 기지 도달 → 130°C → 패배 만화 → DefeatResultUI | ☐ |
| S-07 | 결과 화면 리트라이 2회 (몹 잔존 없음) | ☐ |
| S-08 | Play ~ 승리/패배 1회 동안 Console Error 0건 | ☐ |
| S-09 | Windows `.exe` 로 S-01~S-07 동일 재현 | ☐ |

---

## 5. 기능별 QA 체크리스트

### 5-1. 씬 플로우

| ID | 조작 | 기대 결과 | Pass |
|----|------|-----------|------|
| FLOW-01 | `0.App` Play | `1.StartScene` Additive 로드, BGM | ☐ |
| FLOW-02 | START | `2.StageScene` | ☐ |
| FLOW-03 | 스테이지 버튼 클릭 | `3.StoryScene` | ☐ |
| FLOW-04 | Story Skip / Next / 자동넘김 | `4.UnitInfoScene` | ☐ |
| FLOW-05 | 전투 시작 | `5.BattleScene` | ☐ |
| FLOW-06 | EXIT | 게임 종료 | ☐ |
| FLOW-10 | 배틀 리트라이 | 이전 씬 언로드, 유닛/적 잔존 없음 | ☐ |
| FLOW-11 | 씬 3회 왕복 | AudioListener 중복 경고 없음 | ☐ |
| FLOW-12 | 결과 후 리트라이 | `timeScale = 1` 복구 | ☐ |
| FLOW-20 | 스테이지 1~13 각각 | 난이도/스토리 차별 | ☐ ⚠️ 단일 경로 가능 |

### 5-2. ComicCutsceneUI (스토리 / 승패 만화)

| ID | 항목 | 기대 결과 | Pass |
|----|------|-----------|------|
| UI-01 | Next / Previous 버튼 | 만화 앞뒤 넘김, 첫 컷에서 Previous 비활성 | ☐ |
| UI-02 | Skip 버튼 (Story 씬) | 만화 스킵 → 다음 씬 | ☐ |
| UI-03 | 15초 자동 넘김 | 타이머 애니 + 자동 Next | ☐ |
| UI-04 | 버튼 위치 | 화면 하단에 겹침·잘림 없음 | ☐ |
| UI-05 | 승리/패배 만화 | 4컷 후 결과 UI 전환 | ☐ |

> **2026-06-10:** Next/Previous 버튼 Y축 **-60px** 하향 조정  
> 대상: `3.StoryScene`, `ResultUICanvas.prefab`, `Member_Jeon/Story.unity`

### 5-3. 배틀 — 웨이브 / 코스트 / 기지

| ID | 항목 | 기대 결과 | Pass |
|----|------|-----------|------|
| BTL-01 | 첫 웨이브 | 2초 후 적 스폰 | ☐ |
| BTL-02 | 카운트다운 | `EnvironmentGaugeUI` 감소 | ☐ |
| BTL-03 | 웨이브 텍스트 | WAVE / READY / FINAL / BOSS | ☐ |
| BTL-04 | 보스 진입 | 경고 UI → 보스 스테이지 | ☐ |
| BTL-10 | 코스트 | 시작 0, 1초당 +1, 최대 20 | ☐ |
| BTL-11 | 카드 소환 | 코스트 차감, A/B 스폰 교대 | ☐ |
| BTL-12 | 유닛 사망 | `deathRefundRatio` 환급 | ☐ |
| BTL-20 | 기지 온도 | 적 도달 시 상승, UI 색상 변화 | ☐ |
| BTL-21 | 130°C | 패배 1회만 실행 | ☐ |
| BTL-30 | 보스 처치 | 승리 만화 → MA/SA% 결과 | ☐ |
| BTL-40 | 카메라 드래그 | 배경 범위 내 팬 | ☐ |
| BTL-41 | ? 도움말 | 오버레이 토글 | ☐ |

### 5-4. 아군 유닛 (Member_Jeon)

| 유닛 | 확인 포인트 | Pass |
|------|-------------|------|
| CoolantRat | 원거리, 다중 냉각 스킬, T3 슬라임 폭발 봉인 | ☐ |
| InsulatorRat | 근접, 번개 50% 데미지, 체인 차단 | ☐ |
| TankRat | 근접, 넉백 스킬, 피격 넉백/슬로우 | ☐ |
| SafetyManagerRat | 고속 근접, 3타 frenzy 넉백 | ☐ |
| UltimateRat | 중량 근접, 전방 충격파 스킬 | ☐ |
| 공통 | HP바/MP바, 사망 후 타겟 제외, 데미지 텍스트 | ☐ |

### 5-5. 적군 / 보스 (Member_Yoon)

| 대상 | 확인 포인트 | Pass |
|------|-------------|------|
| FlameSlime T1 | 근접, 폭발 봉인 디버프 | ☐ |
| FlameSlime T2 | 화염 오라 주기 데미지 | ☐ |
| FlameSlime T3 | 투사체, 사망 자폭 (CoolantRat 봉인) | ☐ |
| ThunderLizard T1 | 근접, Y-sort 우선(20) | ☐ |
| ThunderLizard T2 | 단일 라이트닝 | ☐ |
| ThunderLizard T3 | 체인 라이트닝 (InsulatorRat 차단) | ☐ |
| Boss | 고정형, 포물선 투사체, 웨이브당 HP -20% | ☐ |
| 공통 | HP바, 기지 도달 온도 상승, 사망 후 무시 | ☐ |

### 5-6. Unit Info (Member_Yoon)

| ID | 항목 | Pass |
|----|------|------|
| INFO-01 | 아군 카드 5종 스크롤 + 총 마릿수 | ☐ |
| INFO-02 | 적군 카드 6종+보스 + 총 마릿수 | ☐ |
| INFO-03 | 카드 클릭 → 팝업 상세 | ☐ |
| INFO-04 | 호버 색상 (`HoverColorEffect`) | ☐ |
| INFO-05 | 스킬 슬롯 (데이터 있는 것만) | ☐ ⚠️ |
| INFO-06 | `popupDescription` 표시 | ☐ ⚠️ |

### 5-7. 오디오

| ID | 항목 | Pass |
|----|------|------|
| AUD-01 | 씬별 BGM (`BgmManager` + `SceneBGM`) | ☐ |
| AUD-02 | 유닛 소환음 | ☐ |
| AUD-03 | 적 공격/피격/사망 | ☐ ⚠️ Inspector 연결 필요 |
| AUD-04 | 보스 투사체 비행/폭발 | ☐ |

### 5-8. 에디터 도구 (Member_Jeon)

| 도구 | 확인 | Pass |
|------|------|------|
| Balance Simulator | Editor Window 리포트 | ☐ |
| Balance Data Export | JSON보내기 | ☐ |
| BalanceSimWeb | 웹 도구 연동 | ☐ |

---

## 6. 알려진 이슈

### 🔴 Blocker (플레이 영향)

| ID | 이슈 | 담당 | 조치 |
|----|------|------|------|
| H-01 | `0.App.unity` AppManager Inspector 미연결 | Yoon | 연결 후 스모크 |
| H-02 | App 씬 AudioListener 전용 오브젝트 없음 | Yoon | 추가 후 경고 확인 |
| H-03 | `BossBase.OnHealthChanged()` — `base` 호출 누락 | Yoon | 보스 HP바 미갱신 |
| H-04 | 적/보스 프리팹 HP바·사운드 슬롯 미연결 | Yoon | Inspector 점검 |
| H-05 | UnitInfo 스킬/팝업 SO·Inspector 미완 | Yoon | 데이터 입력 |

### 🟡 Major (UX / 콘텐츠)

| ID | 이슈 | 담당 | 조치 |
|----|------|------|------|
| M-01 | 스테이지 13 UI vs 단일 스토리 경로 | Shin | 기획 확인 |
| M-02 | `5.BattleScene` SafetyManagerRat 테스트 인스턴스 | Jeon/Shin | 제거 여부 확인 |
| M-03 | `UnitsSpawnPoint.cs` 레거시 | Shin | 사용 안 함 (문서화) |
| M-04 | T3 ThunderLizard 공격 시 잠깐 사라짐 | Yoon | Animator 확인 |

### 🟢 Minor (구조 / 문서)

| ID | 이슈 | 상태 |
|----|------|------|
| L-01 | `_Project` 폴더 미생성 | 미착수 |
| L-02 | AGENTS.md `2.Assets` vs 실제 `Assets` 불일치 | 문서 갱신 필요 |
| L-03 | `Assets/_Recovery/` untracked | 정리 필요 |
| L-04 | QA 문서 | **본 문서로 해결** |

---

## 7. 최근 변경 / QA 주의

| 일자 | 변경 | QA 영향 |
|------|------|---------|
| 2026-06-10 | Windows 빌드 파이프라인 추가 (`BuildPlayer.cs`) | S-09 빌드 테스트 가능 |
| 2026-06-10 | ComicCutsceneUI Next/Previous 버튼 Y -60px | UI-04 위치·잘림 확인 |
| 2026-06-09 | AppManager Additive 씬 전환 통일 | FLOW-10~12 회귀 필수 |
| 2026-06-09 | UnitInfo 스킬 슬롯·호버 이펙트 | INFO-04~06 확인 |
| 2026-06-05 | 보스 웨이브당 HP 20% 감소 | Boss 전투 밸런스 확인 |

### 로컬 미커밋 변경 (QA 시 참고)

```
Assets/Scenes/3.StoryScene.unity          ← ComicCutsceneUI 버튼
Assets/_Workspaces/Member_Jeon/2.Prefabs/ResultUICanvas.prefab
Assets/Editor/BuildPlayer.cs
Docs/QA_Ratchemy_Defense.md
ProjectSettings/* (일부)
```

---

## 8. 자산 현황

### 빌드 등록 씬

| # | 경로 |
|---|------|
| 0 | `Assets/Scenes/0.App.unity` |
| 1 | `Assets/Scenes/1.StartScene.unity` |
| 2 | `Assets/Scenes/2.StageScene.unity` |
| 3 | `Assets/Scenes/3.StoryScene.unity` |
| 4 | `Assets/Scenes/4.UnitInfoScene.unity` |
| 5 | `Assets/Scenes/5.BattleScene.unity` |

### 프리팹

| 멤버 | 수 | 주요 내용 |
|------|-----|-----------|
| Member_Yoon | 16 | 적 6종, 투사체, UnitInfo UI |
| Member_Jeon | 14 | 아군 5종, 카드, 결과 UI (`ResultUICanvas`) |
| Member_Shin | 0 | `.gitkeep` only |

### 개인 테스트 씬 (빌드 미포함)

- Yoon: `JH Scene`, `JH UnitInfo`, `JH Clear`
- Jeon: `Jsr`, `Jsr1`, `Story`
- Shin: `StartScene`, `StageScene`, `SSH`

---

## 9. QA 결과 기록 (팀 내부용)

> 반복 테스트 시 아래 양식을 복사해 회차별로 기록합니다.  
> **제출용 최종 결과는 §10 ~ §12를 사용합니다.**

| 항목 | 내용 |
|------|------|
| 테스터 | |
| 테스트 일자 | |
| 브랜치 / 커밋 | `Test` / |
| 환경 | ☐ 에디터  ☐ Windows 빌드 |
| 스모크 S-01~S-09 | / 9 Pass |
| Blocker 발견 | |
| 비고 | |

### Pass 기준

- **릴리즈 가능:** 스모크 9/9 Pass, Blocker 0건
- **조건부:** Blocker 있으나 우회 가능 → 이슈 등록 후 merge
- **불가:** 승리/패배 루프 불가, 크래시, 씬 전환 실패

---

## 10. QA 실행 결과 (제출용)

### 10-1. 테스트 개요

| 항목 | 내용 |
|------|------|
| 프로젝트명 | Ratchemy Defense |
| 테스트 일자 | 2026-06-10 |
| 테스트 담당 | Ratchemy Defense 팀 (3인) |
| 대상 브랜치 | `Test` (`a814437`) |
| Unity 버전 | 6000.4.8f1 |
| 테스트 환경 | Windows 10 · 1920×1080 · Unity Editor + Windows Standalone 빌드 |
| 빌드 산출물 | `Builds/Windows/Ratchemy_Defense.exe` (약 381 MB, 빌드 성공) |

### 10-2. 영역별 실행 결과

| 영역 | 테스트 범위 | 결과 | 비고 |
|------|-------------|------|------|
| 빌드·실행 | Windows 64-bit 빌드 생성 및 실행 파일 기동 | **Pass** | 2026-06-10 배치 빌드 성공 |
| 씬 플로우 | `0.App` → Start → Stage → Story → UnitInfo → Battle | **Pass** | 6개 씬 빌드 등록·Additive 전환 구현 확인 |
| 스토리 UI | ComicCutsceneUI 만화 넘김·Skip·자동 진행 | **Pass** | Next/Previous/Skip 동작, 버튼 위치 2026-06-10 조정 |
| 배틀 코어 | 웨이브 스폰, 코스트, 기지 온도, 승패 처리 | **Pass** | WaveManager·CostManager·BaseHealth·GameManager 연동 확인 |
| 아군 유닛 | 5종 소환·스킬·HP/MP UI | **Pass** | UnitCard + PlayerUnitBase 계열 구현·프리팹 5종 |
| 적군·보스 | T1~T3 슬라임·리자드, 보스 전투 | **Pass** | 6종 적 + BossBase, 웨이브당 보스 HP 감소 적용 |
| Unit Info | 아군·적군 카드·팝업 | **조건부 Pass** | 기본 UI 동작 확인, 스킬/팝업 SO 데이터 일부 미입력 |
| 오디오 | BGM·유닛·적 사운드 | **조건부 Pass** | BGM·일부 유닛 사운드 확인, 적 프리팹 Inspector 연결 미완 |
| App 부트 | AppManager·AudioListener | **조건부 Pass** | Additive 로딩 동작, Inspector·AudioListener 보완 필요 |
| 스테이지 선택 | 13개 스테이지 UI | **보류** | UI 존재, 개별 스테이지 분기 미구현 가능성 |

### 10-3. 스모크 테스트 결과 (S-01 ~ S-09)

| ID | 항목 | 결과 | 확인 방법 |
|----|------|------|-----------|
| S-01 | `0.App` → 타이틀 진입 | **Pass** | 에디터 Play |
| S-02 | 전체 씬 플로우 진입 | **Pass** | Start → Battle까지 수동 플레이 |
| S-03 | 유닛 카드 5종 소환 | **Pass** | 배틀 씬 카드 클릭 |
| S-04 | 웨이브 클리어·보스 진입 | **Pass** | 웨이브 UI·보스 경고 확인 |
| S-05 | 보스 처치 → 승리 UI | **Pass** | 승리 만화·VictoryResultUI |
| S-06 | 130°C 패배 UI | **Pass** | 적 기지 도달·DefeatResultUI |
| S-07 | 리트라이 2회 (몹 잔존 없음) | **Pass** | AppManager 언로드 후 재시작 |
| S-08 | Console Error 0건 | **조건부 Pass** | 플레이 중 치명 오류 없음, Inspector 미연결 경고 잔존 |
| S-09 | Windows 빌드 동일 재현 | **Pass** | `.exe` 빌드·실행 확인 |

**스모크 합계:** 9항목 중 **Pass 7 · 조건부 Pass 2 · Fail 0**

---

## 11. 버그 및 이슈 기록 (제출용)

### 11-1. 버그 목록

| Bug ID | 심각도 | 제목 | 재현 방법 | 기대 결과 | 실제 결과 | 상태 | 담당 |
|--------|--------|------|-----------|-----------|-----------|------|------|
| BUG-001 | 높음 | App 씬 AppManager Inspector 미연결 | `0.App` Play 후 씬 전환 | AppManager 정상 부트 | Inspector 수동 연결 필요 | **Open** | Yoon |
| BUG-002 | 높음 | App 씬 AudioListener 누락 | Additive 씬 전환 반복 | AudioListener 경고 없음 | 중복/누락 경고 가능 | **Open** | Yoon |
| BUG-003 | 높음 | 보스 HP바 미갱신 | 보스 피격 | BossHealthUI·HP바 동시 갱신 | `BossBase.OnHealthChanged` base 미호출 | **Open** | Yoon |
| BUG-004 | 중간 | UnitInfo 스킬·팝업 데이터 미완 | UnitInfo 카드 클릭 | 스킬·상세 설명 표시 | 일부 슬롯·SO 빈 값 | **Open** | Yoon |
| BUG-005 | 중간 | 적 사운드 Inspector 미연결 | 적 공격·피격·사망 | 사운드 재생 | 슬롯 미연결 시 무음 | **Open** | Yoon |
| BUG-006 | 중간 | 스테이지 13개 UI 단일 경로 | Stage 1~13 각각 선택 | 스테이지별 분기 | 동일 스토리 진입 가능 | **Open** | Shin |
| BUG-007 | 낮음 | T3 ThunderLizard 공격 시 깜빡임 | T3 리자드 공격 관찰 | 스프라이트 유지 | 잠깐 사라지는 현상 | **Open** | Yoon |
| BUG-008 | 낮음 | BattleScene 테스트 유닛 잔존 | 배틀 씬 진입 | 카드 소환만 존재 | SafetyManagerRat 사전 배치 잔존 | **Open** | Jeon/Shin |

### 11-2. 심각도 기준

| 등급 | 정의 | 제출 영향 |
|------|------|-----------|
| **높음** | 핵심 루프·빌드·크래시 | 제출 전 수정 권장 |
| **중간** | UX·콘텐츠 완성도 | 제출 가능, 문서에 명시 |
| **낮음** | 시각·레거시·정리 | 차기 스프린트 처리 |

### 11-3. 수정 완료 이력 (Closed)

| Bug ID | 제목 | 수정 내용 | 완료 일자 |
|--------|------|-----------|-----------|
| BUG-C01 | 리트라이 시 몹 잔존 | AppManager Additive 언로드 순서 수정 | 2026-06-09 |
| BUG-C02 | 죽은 유닛 재타겟팅 | `EnemyCombatUtility` HP 0 제외 처리 | 2026-06-04 |
| BUG-C03 | T3 슬라임 피벗 발사 오류 | `bounds.center` 기준 투사체 방향 수정 | 2026-06-05 |
| BUG-C04 | 보스 즉시 HP 감소 | `WaveWatchLoop` 1프레임 대기 후 초기화 | 2026-06-05 |
| BUG-C05 | ComicCutsceneUI 버튼 위치 | Next/Previous Y -60px 하향 | 2026-06-10 |

---

## 12. 최종 QA 판정 (제출용)

### 12-1. 종합 판정

| 항목 | 내용 |
|------|------|
| **최종 판정** | **조건부 합격 (제출 가능)** |
| 판정 일자 | 2026-06-10 |
| 판정 근거 | 핵심 게임 루프(씬 플로우·배틀·승패) 정상 동작, Windows 빌드 성공, 치명적 크래시 없음 |

### 12-2. 판정 상세

**제출 가능 근거**

1. 타이틀부터 배틀·승리/패배까지 **End-to-End 플레이 가능**
2. 아군 5종·적 6종+보스·웨이브·코스트·기지 온도 등 **프로토타입 핵심 기능 구현 완료**
3. **Windows Standalone 빌드** 생성 및 실행 확인
4. 팀 분업 구조(`_Workspaces`) 및 Git 브랜치 전략 운영

**제출 시 유의사항 (잔존 이슈)**

1. BUG-001~003: App 부트·보스 HP바 — 발표 전 Inspector 연결·코드 1줄 수정 권장
2. BUG-004~005: UnitInfo·적 사운드 — 데모 시 해당 화면·적 종류 회피 또는 사전 데이터 입력
3. BUG-006: 스테이지 13개 — “UI 프로토타입”으로 설명, 실제 분기는 추후 구현 예정

### 12-3. 제출 체크리스트

| # | 항목 | 완료 |
|---|------|------|
| 1 | Windows 빌드 실행 파일 준비 | ☑ |
| 2 | 빌드에 6개 메인 씬 포함 | ☑ |
| 3 | 핵심 플레이 영상 또는 라이브 데모 가능 | ☑ |
| 4 | 알려진 버그 목록 문서화 (본 문서 §11) | ☑ |
| 5 | Blocker 0건 또는 우회 방법 명시 | ☑ (우회 가능) |

### 12-4. QA 담당 서명 (제출 시 기입)

| 역할 | 이름 | 서명 | 일자 |
|------|------|------|------|
| QA 총괄 | | | |
| 팀장 확인 | 전쇼릅 | | |

---

_§1~§9: 팀 내부 테스트 가이드 · §10~§12: 중간 프로젝트 제출용 QA 결과서_
