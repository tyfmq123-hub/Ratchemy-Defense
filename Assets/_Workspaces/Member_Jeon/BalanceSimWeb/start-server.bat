@echo off
cd /d "%~dp0"
echo Ratchemy Balance Simulator Web Tool
echo.
echo 브라우저에서 아래 주소를 여세요:
echo   http://localhost:8080
echo.
echo 종료: Ctrl+C
echo.

python -m http.server 8080
if errorlevel 1 (
  echo.
  echo Python이 없습니다. index.html 을 더블클릭해도 내장 데이터로 실행됩니다.
  pause
)
