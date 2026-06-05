Ratchemy Balance Simulator Web Tool
===================================

브라우저에서 밸런스 시뮬레이션을 실행하는 팀 공용 툴입니다.
Unity Play 없이 유닛 선택, 스탯 조절, 승률 계산이 가능합니다.

폴더 위치
---------
Assets/_Workspaces/Member_Jeon/BalanceSimWeb/

1. 데이터 최신화 (Unity)
-----------------------
Unity 메뉴:
Member_Jeon → Balance → Export Web Tool Data (JSON)

→ data/balance-data.json 이 갱신됩니다.
프리팹/SO 스탯을 바꾼 뒤에는 이 export를 다시 실행하세요.

2. 로컬에서 실행
----------------
PowerShell / 터미널에서 BalanceSimWeb 폴더로 이동 후:

  npx serve .

브라우저에서 표시된 주소(예: http://localhost:3000) 접속

또는:

  python -m http.server 8080

브라우저: http://localhost:8080

※ index.html 을 더블클릭만 하면 JSON 자동 로드가 안 될 수 있습니다.
  위처럼 간단한 웹 서버로 여는 것을 권장합니다.

3. 팀원 공유 (GitHub Pages)
---------------------------
Ratchemy_Defense repo Settings → Pages
Source: Deploy from branch
Branch: develop (또는 main)
Folder: /docs 또는 BalanceSimWeb 경로에 맞게 설정

또는 BalanceSimWeb 폴더만 Netlify/Vercel에 드래그 배포.

4. 사용법
---------
- JSON 파일 불러오기: export 한 balance-data.json 선택
- 아군/적 +/- 버튼: 소환 마리 수 선택
- 스탯 조절: 각 카드의 「스탯 조절」 펼치기 (임시 테스트용)
- 아군 팀: 「코스트 내 랜덤」 또는 「직접 선택」
- 적 팀: 「풀에서 랜덤」 또는 「직접 선택」
- 시뮬레이션 실행
- 리포트 다운로드: BalanceReport.txt 저장

5. 모드 설명
------------
[1판 테스트]
  아군 직접 선택 + 적 직접 선택 + 반복 1판

[고정 팀 배치]
  아군/적 중 하나 이상 직접 선택 + 반복 1000~2000판
  → 같은 팀으로 N판 반복 (적만 랜덤 등 조합 가능)

[완전 랜덤 배치]
  양쪽 모두 랜덤 + 반복 N판
  → Unity Balance Simulator Tool 과 유사

6. 주의
-------
- 실제 Unity 전투와 100% 일치하지 않습니다 (평균 DPS 추정).
- 웹에서 바꾼 스탯은 게임에 자동 반영되지 않습니다.
- 게임 반영은 Unity 프리팹/SO 수정 후 export 다시 실행.
