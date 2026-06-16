# Cursor Git 작업용 프롬프트 (Member_Shin)

Git 커밋·푸시·PR 요청할 때 **아래 블록 전체를 복사해서 맨 위에 붙여넣기**.

---

```
[Git 안전 규칙 — 반드시 준수]

1. 로컬 디스크 `Assets/_Workspaces/Member_Shin/` 이 최신 원본이다.
2. develop·예전 커밋·다른 브랜치를 기준으로 로컬을 수정하지 마라.
3. 브랜치 전환, stash, git reset, 예전 커밋에서 파일 복구 — 전부 금지 (내가 명시하지 않으면).
4. `git add`는 `Assets/_Workspaces/Member_Shin/` 만. 다른 폴더 절대 포함 금지.
5. 커밋 전 `git diff --cached --name-only` 보여주고 Member_Shin만인지 확인해라.
6. develop과 비교해서 "빠진 것"을 AI가 판단해 채우지 마라. 로컬에 있는 그대로 올려라.
7. Member_Jeon/Member_Yoon/_Project/ProjectSettings/Packages 건드리지 마라.
8. 실행할 git 명령 목록을 먼저 보여주고, 내가 시킨 것만 실행해라.

작업:
(여기에 할 일 작성 — 예: Member_Shin 폴더 커밋 푸시 PR)
```

---

## 사용 예시

### 커밋 + 푸시 + PR

```
[Git 안전 규칙 — 반드시 준수]
(... 위 블록 ...)

작업: 로컬 Member_Shin 폴더 전체를 develop에 반영하도록 커밋, 푸시, PR 생성해줘.
```

### 커밋만

```
[Git 안전 규칙 — 반드시 준수]
(... 위 블록 ...)

작업: Member_Shin 변경분만 커밋해줘. 푸시는 하지 마.
```

### 상태 확인만

```
[Git 안전 규칙 — 반드시 준수]
(... 위 블록 ...)

작업: 로컬 Member_Shin과 develop 차이만 목록으로 보여줘. 파일 수정·커밋 하지 마.
```

---

## 이 규칙이 막는 실수 (과거 사례)

| AI가 멋대로 한 것 | 결과 |
|------------------|------|
| develop에서 브랜치 새로 만들고 전환 | Member_Yoon AppManager 등 생김 → Unity 중복 에러 |
| 예전 커밋에서 Result UI 스크립트 4개 복구 | Member_Jeon과 클래스 중복 → Unity 컴파일 에러 |
| "커밋 안 된 것만" 올림 | 로컬 최신 전체가 develop에 반영 안 됨 |

---

## 관련 파일

- 자동 적용 규칙: `.cursor/rules/member-shin-git-safety.mdc`
- 팀 공용 규칙: `AGENTS.md`
