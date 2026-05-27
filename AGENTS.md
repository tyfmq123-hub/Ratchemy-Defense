# Ratchemy Defense AGENTS.md

## 1. 프로젝트 개요

이 문서는 3인 Unity 팀 프로젝트 **Ratchemy Defense**의 GitHub 브랜치 전략, 폴더 사용 규칙, Cursor 작업 규칙, Push/Pull 주의사항을 정리한 팀 작업 규칙 문서이다.

- 프로젝트명: Ratchemy Defense
- 프로젝트 경로: `C:\Users\user\Documents\Workspace\Team\Ratchemy_Defense`
- Assets 경로: `C:\Users\user\Documents\Workspace\Team\Ratchemy_Defense\2.Assets`
- 팀장: 전쇼릅
- Merge 담당: 전쇼릅
- 작업 도구: Cursor, Unity, GitHub

---

## 2. 핵심 폴더 규칙

### 2.1 `_Project` 폴더

`2.Assets/_Project`는 최종 메인 프로젝트 폴더이다.

이 폴더는 실제 게임에 반영될 최종 리소스, 스크립트, 프리팹, 씬, 데이터가 들어가는 공용 메인 폴더이다.

일반 팀원은 `_Project` 폴더를 직접 수정하지 않는다.

`_Project` 수정 및 최종 반영은 되도록 팀장 전쇼릅만 담당한다.

```plaintext
2.Assets/_Project
```

### 2.2 `_Workspaces` 폴더

각 팀원은 자기 이름이 있는 개인 폴더에서만 작업한다.

```plaintext
2.Assets/_Workspaces/Member_Yoon
2.Assets/_Workspaces/Member_Jeon
2.Assets/_Workspaces/Member_Shin
```

개인 작업이 완료되면 Pull Request를 통해 develop 브랜치로 올리고, 팀장 전쇼릅이 확인 후 `_Project`에 반영한다.

---

## 3. 최종 폴더 트리

```plaintext
Ratchemy_Defense/
├─ 2.Assets/
│  ├─ _Project/
│  │  ├─ Scenes/
│  │  ├─ Scripts/
│  │  ├─ Prefabs/
│  │  ├─ Art/
│  │  ├─ Animations/
│  │  ├─ Audio/
│  │  ├─ Data/
│  │  ├─ Materials/
│  │  ├─ Fonts/
│  │  ├─ Settings/
│  │  └─ Resources/
│  │
│  ├─ _Workspaces/
│  │  ├─ Member_Yoon/
│  │  │  ├─ Scenes/
│  │  │  ├─ Scripts/
│  │  │  ├─ Prefabs/
│  │  │  ├─ Art/
│  │  │  └─ Notes/
│  │  │
│  │  ├─ Member_Jeon/
│  │  │  ├─ Scenes/
│  │  │  ├─ Scripts/
│  │  │  ├─ Prefabs/
│  │  │  ├─ Art/
│  │  │  └─ Notes/
│  │  │
│  │  └─ Member_Shin/
│  │     ├─ Scenes/
│  │     ├─ Scripts/
│  │     ├─ Prefabs/
│  │     ├─ Art/
│  │     └─ Notes/
│  │
│  └─ Plugins/
│
├─ Packages/
├─ ProjectSettings/
├─ Docs/
├─ README.md
├─ AGENTS.md
├─ .gitignore
└─ .gitattributes
```

---

## 4. 브랜치 전략

### 4.1 브랜치 종류

```plaintext
main
```

최종 제출용 안정 버전 브랜치이다. 발표나 제출 직전에만 사용한다.

```plaintext
develop
```

개발 통합 브랜치이다. 각 팀원의 작업 브랜치를 Pull Request로 합치는 곳이다.

```plaintext
feature/작업명
```

개인 기능 작업 브랜치이다.

예시:

```plaintext
feature/unit-move
feature/card-ui
feature/enemy-spawn
feature/boss-resource
feature/attack-effect
```

```plaintext
fix/버그명
```

버그 수정 브랜치이다.

예시:

```plaintext
fix/unit-null-error
fix/enemy-spawn-position
fix/card-click-bug
```

```plaintext
docs/문서명
```

문서 작업 브랜치이다.

예시:

```plaintext
docs/git-rule
docs/story
docs/planning
```

---

## 5. 브랜치 사용 규칙

1. `main`에는 직접 push하지 않는다.
2. `develop`에도 직접 push하지 않는 것을 권장한다.
3. 각자 `feature/작업명` 브랜치에서 작업한다.
4. 작업 완료 후 `feature/작업명`에서 `develop`으로 Pull Request를 생성한다.
5. `develop`에서 `main`으로 병합은 발표 또는 제출 직전에만 진행한다.
6. Merge는 되도록 팀장 전쇼릅만 진행한다.
7. `_Project` 반영도 되도록 팀장 전쇼릅만 담당한다.

---

## 6. 팀원별 작업 폴더 규칙

### 6.1 Member_Yoon

Member_Yoon은 아래 폴더 내부에서만 작업한다.

```plaintext
2.Assets/_Workspaces/Member_Yoon
```

### 6.2 Member_Jeon

Member_Jeon은 아래 폴더 내부에서만 작업한다.

```plaintext
2.Assets/_Workspaces/Member_Jeon
```

### 6.3 Member_Shin

Member_Shin은 아래 폴더 내부에서만 작업한다.

```plaintext
2.Assets/_Workspaces/Member_Shin
```

---

## 7. 절대 바로 Push하면 안 되는 경우

아래 상황이 하나라도 있으면 바로 push하지 않는다.

1. `2.Assets/_Project` 안의 파일이 변경된 경우
2. 자기 개인 폴더가 아닌 다른 팀원 폴더가 변경된 경우
3. `ProjectSettings`가 변경된 경우
4. `Packages`가 변경된 경우
5. Scene 파일이 여러 개 변경된 경우
6. Prefab 파일이 예상보다 많이 변경된 경우
7. `.meta` 파일만 단독으로 삭제 또는 수정된 경우
8. `.gitignore`, `.gitattributes`, `README.md`, `AGENTS.md`가 의도치 않게 변경된 경우

---

## 8. 경고 메시지 규칙

Cursor 또는 작업자가 아래 상황을 발견하면 작업을 중단하고 경고한다.

### 8.1 개인 폴더 외 파일 변경 경고

```plaintext
[WARNING]
내 개인 작업 폴더가 아닌 파일이 변경되었습니다.
바로 push하지 말고 팀장 전쇼릅에게 확인하세요.
```

### 8.2 `_Project` 변경 경고

```plaintext
[WARNING]
_Project 폴더가 수정되었습니다.
_Project는 최종 메인 폴더이므로 일반 팀원이 직접 수정하면 안 됩니다.
push를 중단하고 팀장 전쇼릅에게 확인하세요.
```

### 8.3 다른 팀원 폴더 변경 경고

```plaintext
[WARNING]
다른 팀원의 개인 폴더가 수정되었습니다.
의도하지 않은 변경일 수 있으므로 push하지 말고 변경 내용을 확인하세요.
```

### 8.4 Unity 설정 파일 변경 경고

```plaintext
[WARNING]
ProjectSettings 또는 Packages가 수정되었습니다.
Unity 설정이나 패키지 변경은 팀 전체에 영향을 줄 수 있습니다.
팀장 전쇼릅에게 확인 후 push하세요.
```

---

## 9. Cursor 작업 전 체크 규칙

Cursor에서 작업을 요청하기 전에 반드시 아래 내용을 확인한다.

```bash
git branch
```

현재 브랜치가 `main` 또는 `develop`이면 직접 작업하지 않는다.

작업 브랜치를 새로 만든다.

```bash
git checkout develop
git pull origin develop
git checkout -b feature/작업명
```

작업 전 변경 파일을 확인한다.

```bash
git status
git diff --name-only
```

Cursor에게 작업을 요청할 때는 아래 문장을 프롬프트 앞에 붙이는 것을 권장한다.

```plaintext
작업 전 git diff --name-only 기준으로 내 개인 폴더 외의 파일이 수정되는지 확인해줘. _Project 또는 다른 팀원 폴더가 수정될 경우 작업을 중단하고 경고해줘.
```

---

## 10. Push 전 체크 규칙

Push 전에는 반드시 아래 명령어를 실행한다.

```bash
git status
git diff --name-only
```

변경 파일 목록에 아래 경로가 포함되어 있으면 바로 push하지 않는다.

```plaintext
2.Assets/_Project/
2.Assets/_Workspaces/다른팀원폴더/
ProjectSettings/
Packages/
```

Push 전 확인 순서:

1. 내 브랜치가 `feature/작업명`인지 확인한다.
2. 변경 파일이 내 개인 폴더 안에만 있는지 확인한다.
3. `_Project`가 수정되지 않았는지 확인한다.
4. 다른 팀원 폴더가 수정되지 않았는지 확인한다.
5. Scene, Prefab, ProjectSettings 변경이 있는지 확인한다.
6. 이상이 없을 때만 commit, push한다.

---

## 11. 권장 Git 명령어

### 11.1 작업 시작

```bash
git checkout develop
git pull origin develop
git checkout -b feature/작업명
```

### 11.2 작업 확인

```bash
git status
git diff --name-only
```

### 11.3 커밋

```bash
git add .
git commit -m "작업 내용 요약"
```

### 11.4 푸시

```bash
git push origin feature/작업명
```

### 11.5 Pull Request

```plaintext
feature/작업명 → develop
```

### 11.6 최종 병합

```plaintext
develop → main
```

최종 병합은 팀장 전쇼릅만 진행한다.

---

## 12. Unity Git 주의사항

### 12.1 Git에 올릴 폴더

```plaintext
2.Assets/
Packages/
ProjectSettings/
Docs/
README.md
AGENTS.md
.gitignore
.gitattributes
```

### 12.2 Git에 올리면 안 되는 폴더

```plaintext
Library/
Temp/
Obj/
Logs/
UserSettings/
Build/
Builds/
.vs/
.idea/
```

### 12.3 Unity Editor 필수 설정

Unity에서 아래 설정을 확인한다.

```plaintext
Edit → Project Settings → Editor
```

설정값:

```plaintext
Version Control Mode: Visible Meta Files
Asset Serialization Mode: Force Text
```

### 12.4 `.meta` 파일 규칙

Unity는 리소스마다 `.meta` 파일을 생성한다.

예시:

```plaintext
Unit.png
Unit.png.meta
```

원본 파일과 `.meta` 파일은 반드시 같이 커밋한다.

파일 이동이나 이름 변경은 가능하면 Unity Editor의 Project 창 안에서 진행한다.

---

## 13. 충돌이 자주 나는 파일

아래 파일은 충돌이 자주 발생하므로 작업 전 팀원에게 공유한다.

```plaintext
*.unity
*.prefab
*.meta
ProjectSettings/
Packages/
```

특히 공용 Scene은 동시에 수정하지 않는다.

개인 테스트 Scene은 반드시 자기 개인 폴더 안에서 만든다.

예시:

```plaintext
2.Assets/_Workspaces/Member_Yoon/Scenes/Yoon_TestScene.unity
2.Assets/_Workspaces/Member_Jeon/Scenes/Jeon_TestScene.unity
2.Assets/_Workspaces/Member_Shin/Scenes/Shin_TestScene.unity
```

---

## 14. Commit 메시지 규칙

커밋 메시지는 작업 내용을 명확하게 작성한다.

좋은 예시:

```plaintext
유닛 이동 테스트 스크립트 추가
보스 리소스 정리
공격 이펙트 후보 이미지 추가
카드 UI 테스트 프리팹 생성
```

나쁜 예시:

```plaintext
수정
최종
완성
ㅇㅇ
마지막
```

---

## 15. 역할별 추천 작업 구역

### Member_Yoon

```plaintext
2.Assets/_Workspaces/Member_Yoon/Scenes
2.Assets/_Workspaces/Member_Yoon/Scripts
2.Assets/_Workspaces/Member_Yoon/Prefabs
2.Assets/_Workspaces/Member_Yoon/Art
```

추천 작업:

```plaintext
아군 스프라이트
유닛 테스트
유닛 관련 프리팹 후보
```

### Member_Jeon

```plaintext
2.Assets/_Workspaces/Member_Jeon/Scenes
2.Assets/_Workspaces/Member_Jeon/Scripts
2.Assets/_Workspaces/Member_Jeon/Prefabs
2.Assets/_Workspaces/Member_Jeon/Art
```

추천 작업:

```plaintext
보스 리소스
머지 관리
_Project 최종 반영
develop 병합 확인
```

### Member_Shin

```plaintext
2.Assets/_Workspaces/Member_Shin/Scenes
2.Assets/_Workspaces/Member_Shin/Scripts
2.Assets/_Workspaces/Member_Shin/Prefabs
2.Assets/_Workspaces/Member_Shin/Art
```

추천 작업:

```plaintext
유닛 공격 이펙트
이펙트 후보 리소스
이펙트 테스트 Scene
```

---

## 16. 팀장 전쇼릅 Merge 규칙

팀장 전쇼릅은 Pull Request를 확인할 때 아래 내용을 확인한다.

1. 변경 파일이 해당 팀원의 개인 폴더 중심인지 확인한다.
2. `_Project` 변경이 필요한 경우 의도된 변경인지 확인한다.
3. Unity Scene, Prefab 충돌 가능성을 확인한다.
4. `.meta` 파일 누락 여부를 확인한다.
5. develop에서 Unity 프로젝트가 정상 실행되는지 확인한다.
6. 문제가 없으면 merge한다.
7. 최종 제출 전 develop을 main으로 병합한다.

---

## 17. 핵심 요약

```plaintext
_Project는 최종 메인 폴더이며 직접 수정 금지
개인 작업은 반드시 _Workspaces/Member_이름 폴더에서만 진행
merge는 되도록 팀장 전쇼릅만 진행
다른 사람 폴더가 수정되면 push 금지
_Project가 수정되면 push 금지
push 전 git status와 git diff --name-only 확인 필수
Unity .meta 파일은 원본 파일과 같이 커밋
Scene과 Prefab은 충돌이 쉬우므로 동시에 수정 금지
```
