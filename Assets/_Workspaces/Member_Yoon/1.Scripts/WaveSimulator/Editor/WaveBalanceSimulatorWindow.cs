#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Tools > Wave Balance Simulator
/// 좌측: 로드된 데이터 목록 / 중앙: 설정+결과 / 하단: 그래프+추천
/// </summary>
public class WaveBalanceSimulatorWindow : EditorWindow
{
    // ════════════════════════════════════════════════════════════
    //  메뉴 등록
    // ════════════════════════════════════════════════════════════
    [MenuItem("Tools/Wave Balance Simulator")]
    public static void Open()
    {
        var win = GetWindow<WaveBalanceSimulatorWindow>("Wave Balance Simulator");
        win.minSize = new Vector2(1000, 700);
        win.Show();
    }

    // ════════════════════════════════════════════════════════════
    //  에셋 경로 기본값
    // ════════════════════════════════════════════════════════════
    private string _playerDataFolder = "Assets/_Workspaces/Member_Jeon/2.Prefabs/Data";
    private string _enemyDataFolder  = "Assets/_Workspaces/Member_Yoon/6.SO";
    private string _waveDataFolder   = "Assets/_Workspaces/Member_Yoon/6.SO";

    // ════════════════════════════════════════════════════════════
    //  로드된 데이터
    // ════════════════════════════════════════════════════════════
    private List<SimUnitTemplate> _playerTemplates = new();
    private List<SimUnitTemplate> _enemyTemplates  = new();
    private List<WaveData>        _waveDatas        = new();

    // 활성화 여부 토글
    private readonly Dictionary<string, bool> _playerEnabled = new();
    private readonly Dictionary<string, bool> _enemyEnabled  = new();
    private int _selectedWaveIndex = -1;

    // ════════════════════════════════════════════════════════════
    //  시뮬레이션 설정
    // ════════════════════════════════════════════════════════════
    private float _gameDuration         = 180f;
    private float _costIncomePerSec     = 3f;
    private float _initialCost          = 10f;
    private float _startTemperature     = 20f;
    private float _temperaturePerBreach = 10f;
    private float _defeatTemperature    = 130f;
    private int   _randomSeed           = 42;
    private bool  _useRandomSeed        = false;

    // ════════════════════════════════════════════════════════════
    //  최적화 설정
    // ════════════════════════════════════════════════════════════
    private float _targetWinRateLow  = 0.60f;
    private float _targetWinRateHigh = 0.70f;
    private float _targetEndTimeLow  = 170f;
    private float _targetEndTimeHigh = 180f;
    private float _targetTempLow     = 90f;
    private float _targetTempHigh    = 120f;
    private int   _optMaxIter        = 30;
    private int   _optRunsPerIter    = 200;
    private bool  _optAdjustCount    = true;
    private bool  _optAdjustHp       = true;
    private bool  _optAdjustDamage   = false;
    private bool  _optAdjustInterval = false;

    // ════════════════════════════════════════════════════════════
    //  결과
    // ════════════════════════════════════════════════════════════
    private BatchSimResult     _lastResult;
    private OptimizationResult _lastOptResult;
    private bool               _isRunning;
    private string             _statusMessage = "데이터를 로드하고 시뮬레이션을 실행하세요.";
    private float              _optimizeProgress;
    private string             _optimizeProgressMsg = "";

    // ════════════════════════════════════════════════════════════
    //  UI 상태
    // ════════════════════════════════════════════════════════════
    private int   _rightTabIndex = 0;
    private static readonly string[] _rightTabs
        = { "개요", "유닛 분석", "웨이브 구간", "난이도 분석", "추천", "최적화" };

    private Vector2 _leftScroll;
    private Vector2 _rightScroll;
    private Vector2 _bottomScroll;

    private bool _showPlayerFold = true;
    private bool _showEnemyFold  = true;
    private bool _showWaveFold   = true;
    private bool _showConfigFold = true;

    // ════════════════════════════════════════════════════════════
    //  레이아웃 상수
    // ════════════════════════════════════════════════════════════
    private const float LEFT_WIDTH   = 240f;
    private const float BOTTOM_HEIGHT = 200f;
    private const float TOOLBAR_HEIGHT = 50f;

    // ════════════════════════════════════════════════════════════
    //  OnGUI 진입점
    // ════════════════════════════════════════════════════════════
    private void OnGUI()
    {
        DrawToolbar();

        float bodyHeight = position.height - TOOLBAR_HEIGHT - BOTTOM_HEIGHT - 4f;

        EditorGUILayout.BeginHorizontal(GUILayout.Height(bodyHeight));
        {
            DrawLeftPanel(bodyHeight);
            GUILayout.Space(2f);
            DrawRightPanel();
        }
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(2f);
        DrawBottomPanel();
    }

    // ════════════════════════════════════════════════════════════
    //  툴바
    // ════════════════════════════════════════════════════════════
    private void DrawToolbar()
    {
        EditorGUILayout.BeginVertical("toolbar", GUILayout.Height(TOOLBAR_HEIGHT));
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button("데이터 로드", GUILayout.Width(90), GUILayout.Height(22)))
                    LoadAllData();

                GUILayout.Space(10);
                using (new EditorGUI.DisabledScope(_isRunning))
                {
                    if (GUILayout.Button("1회 실행",    GUILayout.Width(80),  GUILayout.Height(22))) RunBatch(1);
                    if (GUILayout.Button("100회",       GUILayout.Width(70),  GUILayout.Height(22))) RunBatch(100);
                    if (GUILayout.Button("1000회",      GUILayout.Width(70),  GUILayout.Height(22))) RunBatch(1000);
                    if (GUILayout.Button("5000회",      GUILayout.Width(70),  GUILayout.Height(22))) RunBatch(5000);
                }

                GUILayout.Space(10);
                using (new EditorGUI.DisabledScope(_isRunning))
                {
                    if (GUILayout.Button("목표 최적화", GUILayout.Width(90), GUILayout.Height(22)))
                        RunOptimize();
                }

                GUILayout.FlexibleSpace();

                // 상태 메시지
                GUIStyle status = new GUIStyle(EditorStyles.miniLabel)
                    { normal = { textColor = _isRunning ? Color.yellow : Color.gray } };
                GUILayout.Label(_statusMessage, status, GUILayout.MaxWidth(400));
            }
            EditorGUILayout.EndHorizontal();

            // 최적화 진행 바
            if (_isRunning && _optimizeProgress > 0f)
            {
                Rect r = EditorGUILayout.GetControlRect(false, 8f);
                EditorGUI.ProgressBar(r, _optimizeProgress, _optimizeProgressMsg);
            }
        }
        EditorGUILayout.EndVertical();
    }

    // ════════════════════════════════════════════════════════════
    //  좌측 패널 — 데이터 목록
    // ════════════════════════════════════════════════════════════
    private void DrawLeftPanel(float height)
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(LEFT_WIDTH), GUILayout.Height(height));
        _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
        {
            // ── 폴더 경로 설정 ─────────────────────────
            _showConfigFold = EditorGUILayout.Foldout(_showConfigFold, "폴더 경로", true);
            if (_showConfigFold)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("플레이어 유닛:", EditorStyles.miniLabel);
                _playerDataFolder = EditorGUILayout.TextField(_playerDataFolder);
                EditorGUILayout.LabelField("적 유닛:", EditorStyles.miniLabel);
                _enemyDataFolder = EditorGUILayout.TextField(_enemyDataFolder);
                EditorGUILayout.LabelField("웨이브 데이터:", EditorStyles.miniLabel);
                _waveDataFolder = EditorGUILayout.TextField(_waveDataFolder);

                EditorGUILayout.Space(4);
                if (GUILayout.Button("씬에서 WaveData 생성", GUILayout.Height(20)))
                    ImportWaveDataFromScene();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(4);

            // ── 플레이어 유닛 목록 ─────────────────────
            _showPlayerFold = EditorGUILayout.Foldout(
                _showPlayerFold,
                $"플레이어 유닛 [{_playerTemplates.Count}]", true,
                new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });

            if (_showPlayerFold)
            {
                if (_playerTemplates.Count == 0)
                    EditorGUILayout.LabelField("  (데이터 없음)", EditorStyles.miniLabel);
                else
                    DrawUnitList(_playerTemplates, _playerEnabled, Color.cyan);
            }

            EditorGUILayout.Space(4);

            // ── 적 유닛 목록 ──────────────────────────
            _showEnemyFold = EditorGUILayout.Foldout(
                _showEnemyFold,
                $"적 유닛 [{_enemyTemplates.Count}]", true,
                new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });

            if (_showEnemyFold)
            {
                if (_enemyTemplates.Count == 0)
                    EditorGUILayout.LabelField("  (데이터 없음)", EditorStyles.miniLabel);
                else
                    DrawUnitList(_enemyTemplates, _enemyEnabled, new Color(1f, 0.5f, 0.3f));
            }

            EditorGUILayout.Space(4);

            // ── 웨이브 데이터 목록 ────────────────────
            _showWaveFold = EditorGUILayout.Foldout(
                _showWaveFold,
                $"웨이브 데이터 [{_waveDatas.Count}]", true,
                new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold });

            if (_showWaveFold)
            {
                if (_waveDatas.Count == 0)
                    EditorGUILayout.LabelField("  (데이터 없음)", EditorStyles.miniLabel);

                for (int i = 0; i < _waveDatas.Count; i++)
                {
                    bool selected = _selectedWaveIndex == i;
                    Color bg = selected ? new Color(0.3f, 0.6f, 1f, 0.3f) : Color.clear;
                    DrawColoredBox(bg, () =>
                    {
                        EditorGUILayout.BeginHorizontal();
                        bool newSel = GUILayout.Toggle(selected, "", GUILayout.Width(16));
                        if (newSel != selected) _selectedWaveIndex = newSel ? i : -1;
                        EditorGUILayout.LabelField(_waveDatas[i].waveName, EditorStyles.miniLabel);
                        EditorGUILayout.LabelField(
                            $"{_waveDatas[i].entries.Count}항목",
                            EditorStyles.miniLabel, GUILayout.Width(45));
                        EditorGUILayout.EndHorizontal();
                    });
                }
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawUnitList(List<SimUnitTemplate> templates, Dictionary<string, bool> enabled, Color dotColor)
    {
        foreach (var t in templates)
        {
            if (!enabled.ContainsKey(t.id)) enabled[t.id] = true;

            EditorGUILayout.BeginHorizontal();
            enabled[t.id] = EditorGUILayout.Toggle(enabled[t.id], GUILayout.Width(16));

            // 색 점
            Rect dotRect = GUILayoutUtility.GetRect(8, 8, GUILayout.Width(8));
            dotRect.y += 4;
            EditorGUI.DrawRect(dotRect, dotColor);

            EditorGUILayout.LabelField(t.displayName, EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                $"HP:{t.maxHp:F0} ATK:{t.damage:F0}",
                EditorStyles.miniLabel, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
        }
    }

    // ════════════════════════════════════════════════════════════
    //  우측 패널 — 설정 + 결과 탭
    // ════════════════════════════════════════════════════════════
    private void DrawRightPanel()
    {
        EditorGUILayout.BeginVertical("box");
        _rightTabIndex = GUILayout.Toolbar(_rightTabIndex, _rightTabs);

        _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);
        {
            switch (_rightTabIndex)
            {
                case 0: DrawTabOverview();      break;
                case 1: DrawTabUnitStats();     break;
                case 2: DrawTabSegments();      break;
                case 3: DrawTabDifficulty();    break;
                case 4: DrawTabRecommend();     break;
                case 5: DrawTabOptimize();      break;
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    // ── 탭 0: 개요 ────────────────────────────────────────────

    private void DrawTabOverview()
    {
        EditorGUILayout.LabelField("시뮬레이션 설정", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        _gameDuration         = EditorGUILayout.FloatField("게임 시간 (초)",     _gameDuration);
        _costIncomePerSec     = EditorGUILayout.FloatField("초당 코스트 증가",    _costIncomePerSec);
        _initialCost          = EditorGUILayout.FloatField("시작 코스트",         _initialCost);
        _startTemperature     = EditorGUILayout.FloatField("초기 온도",           _startTemperature);
        _temperaturePerBreach = EditorGUILayout.FloatField("돌파당 온도 상승",    _temperaturePerBreach);
        _defeatTemperature    = EditorGUILayout.FloatField("패배 온도",           _defeatTemperature);

        EditorGUILayout.BeginHorizontal();
        _useRandomSeed = EditorGUILayout.Toggle("랜덤 시드 사용", _useRandomSeed);
        if (!_useRandomSeed)
            _randomSeed = EditorGUILayout.IntField(_randomSeed);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        if (_lastResult == null)
        {
            EditorGUILayout.HelpBox("시뮬레이션을 실행하면 결과가 여기에 표시됩니다.", MessageType.Info);
            return;
        }

        // ── 결과 요약 ─────────────────────────────
        EditorGUILayout.LabelField("═══ 결과 요약 ═══", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        var r = _lastResult;
        DrawResultRow("총 실행 수",          $"{r.totalRuns:N0} 회");
        DrawResultRow("승률",               $"{r.WinRate:P1}",
            r.WinRate >= 0.60f && r.WinRate <= 0.70f ? Color.green : Color.red);
        DrawResultRow("패배율",             $"{r.LossRate:P1}");
        DrawResultRow("평균 종료 시간",      $"{r.avgEndTime:F1} 초",
            r.avgEndTime >= 170f && r.avgEndTime <= 180f ? Color.green : Color.yellow);
        DrawResultRow("평균 최종 온도",      $"{r.avgFinalTemperature:F1} 도",
            r.avgFinalTemperature >= 90f && r.avgFinalTemperature <= 120f ? Color.green : Color.yellow);
        DrawResultRow("평균 돌파 허용 횟수", $"{r.avgBreaches:F1} 회");

        EditorGUILayout.Space(8);

        // 목표 범위 달성 여부
        bool winOk  = r.WinRate >= 0.60f && r.WinRate <= 0.70f;
        bool timeOk = r.avgEndTime >= 170f && r.avgEndTime <= 180f;
        bool tempOk = r.avgFinalTemperature >= 90f && r.avgFinalTemperature <= 120f;
        int  pass   = (winOk ? 1 : 0) + (timeOk ? 1 : 0) + (tempOk ? 1 : 0);

        string grade = pass == 3 ? "✓ 목표 달성" : pass == 2 ? "△ 부분 달성" : "✗ 목표 미달";
        Color  gradeCol = pass == 3 ? Color.green : pass >= 2 ? Color.yellow : Color.red;

        DrawResultRow("종합 평가", grade, gradeCol);
    }

    // ── 탭 1: 유닛 통계 ───────────────────────────────────────

    private void DrawTabUnitStats()
    {
        if (_lastResult == null) { EditorGUILayout.HelpBox("결과 없음", MessageType.Info); return; }

        EditorGUILayout.LabelField("플레이어 유닛 통계", EditorStyles.boldLabel);
        DrawUnitStatTable(isPlayer: true);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("적 유닛 통계", EditorStyles.boldLabel);
        DrawUnitStatTable(isPlayer: false);
    }

    private void DrawUnitStatTable(bool isPlayer)
    {
        if (_lastResult?.unitStats == null) return;

        // 헤더
        EditorGUILayout.BeginHorizontal("box");
        DrawHeaderCell("이름",          80);
        DrawHeaderCell("평균딜",        60);
        if (isPlayer)
        {
            DrawHeaderCell("흡수",      55);
            DrawHeaderCell("평균킬",    55);
            DrawHeaderCell("소환수",    55);
            DrawHeaderCell("생존시간",  65);
            DrawHeaderCell("채용률",    55);
            DrawHeaderCell("효율",      55);
        }
        else
        {
            DrawHeaderCell("평균생존",  65);
            DrawHeaderCell("소환수",    55);
            DrawHeaderCell("사망수",    55);
        }
        EditorGUILayout.EndHorizontal();

        foreach (var kv in _lastResult.unitStats)
        {
            var bu = kv.Value;
            if (bu.isPlayer != isPlayer) continue;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(bu.name,                  GUILayout.Width(80));
            EditorGUILayout.LabelField($"{bu.avgDamageDealt:F0}", GUILayout.Width(60));
            if (isPlayer)
            {
                EditorGUILayout.LabelField($"{bu.avgDamageAbsorbed:F0}", GUILayout.Width(55));
                EditorGUILayout.LabelField($"{bu.avgKills:F1}",          GUILayout.Width(55));
                EditorGUILayout.LabelField($"{bu.avgSpawnCount:F1}",     GUILayout.Width(55));
                EditorGUILayout.LabelField($"{bu.avgSurvivalTime:F1}s",  GUILayout.Width(65));
                EditorGUILayout.LabelField($"{bu.usageRate:P0}",         GUILayout.Width(55));
                EditorGUILayout.LabelField($"{bu.costEfficiency:F2}",    GUILayout.Width(55));
            }
            else
            {
                EditorGUILayout.LabelField($"{bu.avgSurvivalTime:F1}s",  GUILayout.Width(65));
                EditorGUILayout.LabelField($"{bu.avgSpawnCount:F1}",     GUILayout.Width(55));
                EditorGUILayout.LabelField($"{bu.avgDeathCount:F1}",     GUILayout.Width(55));
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    // ── 탭 2: 웨이브 구간 분석 ────────────────────────────────

    private void DrawTabSegments()
    {
        if (_lastResult == null) { EditorGUILayout.HelpBox("결과 없음", MessageType.Info); return; }

        EditorGUILayout.LabelField("구간별 분석 (30초 단위)", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        string[] segLabels = { "0~30초", "30~60초", "60~90초", "90~120초", "120~150초", "150~180초" };

        // 헤더
        EditorGUILayout.BeginHorizontal("box");
        DrawHeaderCell("구간",    70);
        DrawHeaderCell("생존율",  60);
        DrawHeaderCell("온도상승",65);
        DrawHeaderCell("적소환",  60);
        DrawHeaderCell("돌파",    50);
        DrawHeaderCell("아군소환",65);
        DrawHeaderCell("아군사망",65);
        DrawHeaderCell("적사망",  60);
        EditorGUILayout.EndHorizontal();

        for (int i = 0; i < 6; i++)
        {
            var ss = _lastResult.segmentStats[i];

            // 문제 구간 하이라이트
            bool isTempSpike = ss.avgTemperatureGain > 25f;
            bool isEmpty     = i > 0 && ss.avgEnemySpawns < 0.5f;

            Color rowBg = isTempSpike ? new Color(1f, 0.3f, 0.3f, 0.15f)
                        : isEmpty     ? new Color(0.5f, 0.5f, 1f, 0.15f)
                        :               Color.clear;

            DrawColoredBox(rowBg, () =>
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(segLabels[i],                    GUILayout.Width(70));
                EditorGUILayout.LabelField($"{ss.survivorRate:P0}",         GUILayout.Width(60));
                EditorGUILayout.LabelField($"+{ss.avgTemperatureGain:F1}°", GUILayout.Width(65));
                EditorGUILayout.LabelField($"{ss.avgEnemySpawns:F1}",       GUILayout.Width(60));
                EditorGUILayout.LabelField($"{ss.avgBreachCount:F1}",       GUILayout.Width(50));
                EditorGUILayout.LabelField($"{ss.avgPlayerSpawns:F1}",      GUILayout.Width(65));
                EditorGUILayout.LabelField($"{ss.avgPlayerDeaths:F1}",      GUILayout.Width(65));
                EditorGUILayout.LabelField($"{ss.avgEnemyDeaths:F1}",       GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            });
        }
    }

    // ── 탭 3: 난이도 분석 ─────────────────────────────────────

    private void DrawTabDifficulty()
    {
        if (_lastResult == null) { EditorGUILayout.HelpBox("결과 없음", MessageType.Info); return; }

        EditorGUILayout.LabelField("자동 난이도 분석", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        var issues = _lastResult.difficultyIssues;
        if (issues == null || issues.Count == 0)
        {
            EditorGUILayout.HelpBox("탐지된 난이도 문제 없음", MessageType.Info);
            return;
        }

        foreach (var issue in issues)
        {
            MessageType mt = issue.severity >= 0.7f ? MessageType.Error
                           : issue.severity >= 0.4f ? MessageType.Warning
                           :                          MessageType.Info;
            EditorGUILayout.HelpBox(issue.description, mt);
        }
    }

    // ── 탭 4: 추천 ────────────────────────────────────────────

    private void DrawTabRecommend()
    {
        if (_lastResult == null) { EditorGUILayout.HelpBox("결과 없음", MessageType.Info); return; }

        EditorGUILayout.LabelField("밸런스 추천", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        var recs = _lastResult.recommendations;
        if (recs == null || recs.Count == 0)
        {
            EditorGUILayout.HelpBox("현재 설정이 목표 범위에 있습니다.", MessageType.Info);
            return;
        }

        foreach (var rec in recs)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField(rec.description, EditorStyles.wordWrappedLabel);

            if (!string.IsNullOrEmpty(rec.entryKey))
                EditorGUILayout.LabelField($"대상: {rec.entryKey}", EditorStyles.miniLabel);

            if (rec.currentValue != 0f)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"현재: {rec.currentValue:F0}", GUILayout.Width(100));
                EditorGUILayout.LabelField("→");
                GUIStyle rStyle = new GUIStyle(EditorStyles.label)
                    { normal = { textColor = Color.yellow }, fontStyle = FontStyle.Bold };
                EditorGUILayout.LabelField($"추천: {rec.recommendedValue:F0}", rStyle, GUILayout.Width(110));
                EditorGUILayout.LabelField($"({rec.ChangePercent:+0.0;-0.0}%)", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
            }

            if (rec.expectedWinRateDelta != 0f)
                EditorGUILayout.LabelField(
                    $"예상 승률 변화: {rec.expectedWinRateDelta:+P1;-P1}",
                    EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
    }

    // ── 탭 5: 최적화 ──────────────────────────────────────────

    private void DrawTabOptimize()
    {
        EditorGUILayout.LabelField("목표 기반 자동 최적화", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("목표 범위", EditorStyles.boldLabel);
        DrawRangeField("목표 승률",         ref _targetWinRateLow, ref _targetWinRateHigh, 0f, 1f, true);
        DrawRangeField("목표 종료 시간(초)", ref _targetEndTimeLow, ref _targetEndTimeHigh, 0f, 180f, false);
        DrawRangeField("목표 최종 온도",     ref _targetTempLow,    ref _targetTempHigh,    20f, 130f, false);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("최적화 파라미터", EditorStyles.boldLabel);
        _optMaxIter      = EditorGUILayout.IntSlider("최대 반복 횟수", _optMaxIter, 5, 100);
        _optRunsPerIter  = EditorGUILayout.IntField("반복당 실행 수", _optRunsPerIter);
        _optAdjustCount    = EditorGUILayout.Toggle("적 수량 조정",    _optAdjustCount);
        _optAdjustHp       = EditorGUILayout.Toggle("적 HP 조정",      _optAdjustHp);
        _optAdjustDamage   = EditorGUILayout.Toggle("적 공격력 조정",  _optAdjustDamage);
        _optAdjustInterval = EditorGUILayout.Toggle("소환 간격 조정",  _optAdjustInterval);

        EditorGUILayout.Space(8);

        if (_lastOptResult == null)
        {
            EditorGUILayout.HelpBox("'목표 최적화' 버튼을 누르면 수천 번 시뮬레이션 후 최적 수치를 추천합니다.",
                MessageType.Info);
            return;
        }

        var opt = _lastOptResult;

        EditorGUILayout.LabelField("══ 최적화 결과 ══", EditorStyles.boldLabel);
        DrawResultRow("수렴 여부",    opt.converged ? "수렴 완료 ✓" : "최대 반복 도달", opt.converged ? Color.green : Color.yellow);
        DrawResultRow("반복 횟수",    $"{opt.iterationsUsed}회");
        DrawResultRow("최종 승률",    $"{opt.finalWinRate:P1}",
            opt.finalWinRate >= _targetWinRateLow && opt.finalWinRate <= _targetWinRateHigh ? Color.green : Color.yellow);
        DrawResultRow("최종 평균 시간", $"{opt.finalAvgEndTime:F1}초");
        DrawResultRow("최종 평균 온도", $"{opt.finalAvgTemperature:F1}도");

        EditorGUILayout.Space(8);
        using (new EditorGUI.DisabledScope(_selectedWaveIndex < 0 || _selectedWaveIndex >= _waveDatas.Count))
        {
            if (GUILayout.Button("추천값 적용 (WaveData + EnemyUnitData 수정)", GUILayout.Height(28)))
            {
                if (EditorUtility.DisplayDialog(
                    "추천값 적용",
                    "최적화 추천값을 에셋에 적용합니다.\n\n" +
                    "• count → 선택된 WaveData 항목에 반영\n" +
                    "• HP / 데미지 → EnemyUnitData에 반영 (같은 적을 쓰는 모든 웨이브에 영향)\n\n" +
                    "계속하시겠습니까?",
                    "적용", "취소"))
                {
                    ApplyOptimizationResult();
                }
            }
        }

        if (opt.adjustments != null && opt.adjustments.Count > 0)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("수치 조정 추천", EditorStyles.boldLabel);

            // 헤더
            EditorGUILayout.BeginHorizontal("box");
            DrawHeaderCell("적 이름",   90);
            DrawHeaderCell("등장(초)",  55);
            DrawHeaderCell("현재수량",  60);
            DrawHeaderCell("추천수량",  60);
            DrawHeaderCell("현재HP",    60);
            DrawHeaderCell("추천HP",    60);
            DrawHeaderCell("현재공격",  60);
            DrawHeaderCell("추천공격",  60);
            DrawHeaderCell("현재간격",  60);
            DrawHeaderCell("추천간격",  60);
            EditorGUILayout.EndHorizontal();

            foreach (var adj in opt.adjustments)
            {
                bool countChanged = adj.recommendedCount != adj.originalCount;
                bool hpChanged    = Mathf.Abs(adj.recommendedHp       - adj.originalHp)       > 1f;
                bool dmgChanged   = Mathf.Abs(adj.recommendedDamage   - adj.originalDamage)   > 1f;
                bool itvChanged   = Mathf.Abs(adj.recommendedInterval - adj.originalInterval) > 0.01f;
                if (!countChanged && !hpChanged && !dmgChanged && !itvChanged) continue;

                GUIStyle changed = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.yellow }, fontStyle = FontStyle.Bold };
                GUIStyle normal  = EditorStyles.label;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(adj.enemyName,                                                          GUILayout.Width(90));
                EditorGUILayout.LabelField($"{adj.spawnTime:F0}",                                                  GUILayout.Width(55));
                EditorGUILayout.LabelField($"{adj.originalCount}",                                                 GUILayout.Width(60));
                EditorGUILayout.LabelField($"{adj.recommendedCount}",    countChanged ? changed : normal,          GUILayout.Width(60));
                EditorGUILayout.LabelField($"{adj.originalHp:F0}",                                                 GUILayout.Width(60));
                EditorGUILayout.LabelField($"{adj.recommendedHp:F0}",   hpChanged    ? changed : normal,          GUILayout.Width(60));
                EditorGUILayout.LabelField($"{adj.originalDamage:F0}",                                             GUILayout.Width(60));
                EditorGUILayout.LabelField($"{adj.recommendedDamage:F0}", dmgChanged ? changed : normal,          GUILayout.Width(60));
                EditorGUILayout.LabelField($"{adj.originalInterval:F2}s",                                          GUILayout.Width(60));
                EditorGUILayout.LabelField($"{adj.recommendedInterval:F2}s", itvChanged ? changed : normal,       GUILayout.Width(60));
                EditorGUILayout.EndHorizontal();
            }
        }
    }

    private void ApplyOptimizationResult()
    {
        if (_lastOptResult?.adjustments == null || _lastOptResult.adjustments.Count == 0) return;
        if (_selectedWaveIndex < 0 || _selectedWaveIndex >= _waveDatas.Count) return;

        var waveData     = _waveDatas[_selectedWaveIndex];
        int appliedCount = 0;

        // WaveManager 씬 오브젝트 가져오기 (spawnCount/spawnInterval 쓰기용)
        var waveManager = UnityEngine.Object.FindFirstObjectByType<WaveManager>(FindObjectsInactive.Include);
        SerializedObject waveManagerSo = waveManager != null ? new SerializedObject(waveManager) : null;
        SerializedProperty waveSettingsProp = waveManagerSo?.FindProperty("waveSettings");

        foreach (var adj in _lastOptResult.adjustments)
        {
            int idx = adj.originalEntryIndex;
            if (idx < 0 || idx >= waveData.entries.Count) continue;

            var entry = waveData.entries[idx];

            if (_optAdjustCount && adj.recommendedCount != adj.originalCount)
            {
                entry.count = adj.recommendedCount;
                EditorUtility.SetDirty(waveData);

                // WaveManager.waveSettings[waveSettingIndex].spawnCount 쓰기
                if (waveSettingsProp != null && adj.waveSettingIndex >= 0
                    && adj.waveSettingIndex < waveSettingsProp.arraySize)
                {
                    var settingEl   = waveSettingsProp.GetArrayElementAtIndex(adj.waveSettingIndex);
                    var countProp   = settingEl.FindPropertyRelative("spawnCount");
                    if (countProp != null)
                        countProp.intValue = adj.recommendedCount;
                }
            }

            if (_optAdjustInterval && Mathf.Abs(adj.recommendedInterval - adj.originalInterval) > 0.01f)
            {
                entry.spawnInterval = adj.recommendedInterval;
                EditorUtility.SetDirty(waveData);

                // WaveManager.waveSettings[waveSettingIndex].spawnInterval 쓰기
                if (waveSettingsProp != null && adj.waveSettingIndex >= 0
                    && adj.waveSettingIndex < waveSettingsProp.arraySize)
                {
                    var settingEl    = waveSettingsProp.GetArrayElementAtIndex(adj.waveSettingIndex);
                    var intervalProp = settingEl.FindPropertyRelative("spawnInterval");
                    if (intervalProp != null)
                        intervalProp.floatValue = adj.recommendedInterval;
                }
            }

            if (entry.enemyData != null)
            {
                bool dirty = false;

                if (_optAdjustHp && Mathf.Abs(adj.recommendedHp - adj.originalHp) > 0.5f)
                {
                    entry.enemyData.hp    = Mathf.RoundToInt(adj.recommendedHp);
                    entry.enemyData.maxHp = Mathf.RoundToInt(adj.recommendedHp);
                    dirty = true;
                }

                if (_optAdjustDamage && Mathf.Abs(adj.recommendedDamage - adj.originalDamage) > 0.5f)
                {
                    entry.enemyData.damage = Mathf.RoundToInt(adj.recommendedDamage);
                    dirty = true;
                }

                if (dirty)
                    EditorUtility.SetDirty(entry.enemyData);
            }

            appliedCount++;
        }

        // WaveManager 씬 변경사항 저장
        if (waveManagerSo != null)
        {
            waveManagerSo.ApplyModifiedProperties();
            EditorSceneManager.MarkSceneDirty(waveManager.gameObject.scene);
        }

        AssetDatabase.SaveAssets();
        string waveManagerNote = waveManager != null ? " (WaveManager 씬 반영됨)" : " (WaveManager 없음 — 씬을 열어두세요)";
        _statusMessage = $"적용 완료 — {appliedCount}개 항목 수정됨{waveManagerNote}";
        Repaint();
    }

    // ════════════════════════════════════════════════════════════
    //  하단 패널 — 그래프
    // ════════════════════════════════════════════════════════════
    private void DrawBottomPanel()
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Height(BOTTOM_HEIGHT));
        _bottomScroll = EditorGUILayout.BeginScrollView(_bottomScroll, GUILayout.Height(BOTTOM_HEIGHT - 6));
        {
            EditorGUILayout.BeginHorizontal();
            {
                DrawBarChart(
                    "구간별 온도 상승",
                    GetSegmentValues(ss => ss.avgTemperatureGain),
                    new[] { "0-30", "30-60", "60-90", "90-120", "120-150", "150-180" },
                    new Color(1f, 0.4f, 0.2f, 0.85f),
                    maxValue: 50f,
                    targetLine: 15f);

                GUILayout.Space(8);

                DrawBarChart(
                    "구간별 생존률",
                    GetSegmentValues(ss => ss.survivorRate),
                    new[] { "0-30", "30-60", "60-90", "90-120", "120-150", "150-180" },
                    new Color(0.2f, 0.8f, 0.4f, 0.85f),
                    maxValue: 1f,
                    targetLine: -1f,
                    formatPercent: true);

                GUILayout.Space(8);

                DrawBarChart(
                    "구간별 적 소환 수",
                    GetSegmentValues(ss => ss.avgEnemySpawns),
                    new[] { "0-30", "30-60", "60-90", "90-120", "120-150", "150-180" },
                    new Color(0.9f, 0.2f, 0.2f, 0.85f),
                    maxValue: 20f,
                    targetLine: -1f);

                GUILayout.Space(8);

                DrawBarChart(
                    "구간별 돌파 횟수",
                    GetSegmentValues(ss => ss.avgBreachCount),
                    new[] { "0-30", "30-60", "60-90", "90-120", "120-150", "150-180" },
                    new Color(1f, 0.8f, 0.0f, 0.85f),
                    maxValue: 5f,
                    targetLine: -1f);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private float[] GetSegmentValues(Func<BatchSegmentStats, float> selector)
    {
        if (_lastResult?.segmentStats == null) return new float[6];
        var vals = new float[6];
        for (int i = 0; i < 6; i++) vals[i] = selector(_lastResult.segmentStats[i]);
        return vals;
    }

    private void DrawBarChart(
        string title, float[] values, string[] labels,
        Color barColor, float maxValue, float targetLine,
        bool formatPercent = false)
    {
        const float chartW = 160f;
        const float chartH = 110f;
        const float barW   = 20f;

        EditorGUILayout.BeginVertical(GUILayout.Width(chartW));
        EditorGUILayout.LabelField(title, EditorStyles.miniLabel, GUILayout.Width(chartW));

        Rect chartRect = GUILayoutUtility.GetRect(chartW, chartH);
        EditorGUI.DrawRect(chartRect, new Color(0.15f, 0.15f, 0.15f));

        if (values == null || values.Length == 0 || maxValue <= 0f)
        {
            EditorGUILayout.EndVertical();
            return;
        }

        float step  = chartW / values.Length;
        float scale = chartH / maxValue;

        for (int i = 0; i < values.Length; i++)
        {
            float val    = Mathf.Clamp(values[i], 0f, maxValue);
            float barH   = val * scale;
            float x      = chartRect.x + i * step + (step - barW) * 0.5f;
            float y      = chartRect.yMax - barH;

            EditorGUI.DrawRect(new Rect(x, y, barW, barH), barColor);

            // 값 레이블
            string lbl = formatPercent ? $"{val:P0}" : $"{val:F0}";
            GUI.Label(new Rect(x - 2, y - 14f, barW + 8, 14f), lbl,
                new GUIStyle(EditorStyles.miniLabel) { fontSize = 8 });

            // x축 레이블
            if (labels != null && i < labels.Length)
                GUI.Label(new Rect(x - 4, chartRect.yMax, barW + 8, 14f), labels[i],
                    new GUIStyle(EditorStyles.miniLabel) { fontSize = 7 });
        }

        // 목표선
        if (targetLine > 0f && targetLine < maxValue)
        {
            float ty = chartRect.yMax - targetLine * scale;
            EditorGUI.DrawRect(new Rect(chartRect.x, ty, chartW, 1f), new Color(0f, 1f, 0f, 0.6f));
        }

        EditorGUILayout.EndVertical();
    }

    // ════════════════════════════════════════════════════════════
    //  실행 로직
    // ════════════════════════════════════════════════════════════

    private void ImportWaveDataFromScene()
    {
        var (created, error) = WaveSimDataLoader.ImportFromSceneWaveManager(_waveDataFolder);

        if (error != null)
        {
            EditorUtility.DisplayDialog("WaveData 생성 실패", error, "확인");
            _statusMessage = $"생성 실패: {error}";
        }
        else
        {
            _statusMessage = $"WaveData 생성 완료 ({created.waveName}) → {_waveDataFolder}";
            LoadAllData();
        }

        Repaint();
    }

    private void LoadAllData()
    {
        _playerTemplates = WaveSimDataLoader.LoadPlayerTemplates(_playerDataFolder);
        _enemyTemplates  = WaveSimDataLoader.LoadEnemyTemplates(_enemyDataFolder);
        _waveDatas       = WaveSimDataLoader.LoadWaveData(_waveDataFolder);

        // 활성화 상태 기본값 설정
        foreach (var t in _playerTemplates)
            if (!_playerEnabled.ContainsKey(t.id)) _playerEnabled[t.id] = true;
        foreach (var t in _enemyTemplates)
            if (!_enemyEnabled.ContainsKey(t.id)) _enemyEnabled[t.id] = true;

        if (_waveDatas.Count > 0 && _selectedWaveIndex < 0)
            _selectedWaveIndex = 0;

        _statusMessage = $"로드 완료: 플레이어 {_playerTemplates.Count}, 적 {_enemyTemplates.Count}, 웨이브 {_waveDatas.Count}";
        Repaint();
    }

    private WaveSimConfig BuildConfig()
    {
        var cfg = new WaveSimConfig
        {
            gameDuration         = _gameDuration,
            costIncomePerSecond  = _costIncomePerSec,
            initialCost          = _initialCost,
            startTemperature     = _startTemperature,
            temperaturePerBreach = _temperaturePerBreach,
            defeatTemperature    = _defeatTemperature,
            seed                 = _useRandomSeed ? UnityEngine.Random.Range(0, int.MaxValue) : _randomSeed,
        };

        // 활성화된 플레이어 유닛만 포함
        foreach (var t in _playerTemplates)
            if (_playerEnabled.TryGetValue(t.id, out bool en) && en)
                cfg.playerTemplates.Add(t);

        // 활성화된 적 유닛 + 선택된 WaveData 기반 스폰 스케줄 생성
        var enabledEnemy = new List<SimUnitTemplate>();
        foreach (var t in _enemyTemplates)
            if (_enemyEnabled.TryGetValue(t.id, out bool en) && en)
                enabledEnemy.Add(t);

        if (_selectedWaveIndex >= 0 && _selectedWaveIndex < _waveDatas.Count)
        {
            var wd = _waveDatas[_selectedWaveIndex];
            cfg.spawnSchedule = WaveSimConfig.ExpandWaveData(wd.entries, enabledEnemy, cfg.seed);
        }
        else
        {
            // WaveData 없을 때: 활성화된 적을 기본 타임라인에 배치
            cfg.spawnSchedule = GenerateDefaultSchedule(enabledEnemy);
        }

        return cfg;
    }

    private List<WaveSpawnScheduleEntry> GenerateDefaultSchedule(List<SimUnitTemplate> enemies)
    {
        var schedule = new List<WaveSpawnScheduleEntry>();
        if (enemies.Count == 0) return schedule;

        // 30초 간격으로 각 적 유닛 3마리씩 기본 배치
        float[] spawnTimes = { 10f, 30f, 60f, 90f, 120f, 150f };
        int entryIdx = 0;
        foreach (float t in spawnTimes)
        {
            var tmpl = enemies[entryIdx % enemies.Count];
            for (int k = 0; k < 3; k++)
                schedule.Add(new WaveSpawnScheduleEntry
                {
                    exactSpawnTime     = t + k * 0.5f,
                    template           = tmpl,
                    originalEntryIndex = entryIdx,
                });
            entryIdx++;
        }
        return schedule;
    }

    private void RunBatch(int count)
    {
        if (_playerTemplates.Count == 0 || (_enemyTemplates.Count == 0 && _waveDatas.Count == 0))
        {
            _statusMessage = "먼저 '데이터 로드'를 실행하세요.";
            Repaint();
            return;
        }

        _isRunning     = true;
        _statusMessage = $"{count:N0}회 시뮬레이션 실행 중...";
        Repaint();

        try
        {
            var cfg  = BuildConfig();
            var runs = new List<SimRunResult>(count);

            for (int i = 0; i < count; i++)
                runs.Add(WaveSimEngine.Run(cfg, cfg.seed + i));

            _lastResult = WaveSimAnalyzer.Aggregate(runs);
            WaveSimAnalyzer.ComputeCostEfficiency(_lastResult, cfg.playerTemplates);

            _statusMessage = $"완료 — 승률 {_lastResult.WinRate:P1} | 평균 {_lastResult.avgEndTime:F0}초 | 온도 {_lastResult.avgFinalTemperature:F0}도";
        }
        catch (Exception ex)
        {
            _statusMessage = $"오류: {ex.Message}";
            Debug.LogError($"[WaveBalanceSim] {ex}");
        }
        finally
        {
            _isRunning = false;
            Repaint();
        }
    }

    private void RunOptimize()
    {
        _isRunning          = true;
        _optimizeProgress   = 0f;
        _optimizeProgressMsg = "최적화 시작...";
        _statusMessage      = "목표 최적화 실행 중...";
        Repaint();

        try
        {
            var cfg = BuildConfig();

            List<WaveEnemyEntry> waveEntries = _selectedWaveIndex >= 0 && _selectedWaveIndex < _waveDatas.Count
                ? _waveDatas[_selectedWaveIndex].entries
                : new List<WaveEnemyEntry>();

            var targets = new WaveSimOptimizer.OptimizationTargets
            {
                winRateLow      = _targetWinRateLow,
                winRateHigh     = _targetWinRateHigh,
                endTimeLow      = _targetEndTimeLow,
                endTimeHigh     = _targetEndTimeHigh,
                tempLow         = _targetTempLow,
                tempHigh        = _targetTempHigh,
                maxIterations   = _optMaxIter,
                runsPerIteration = _optRunsPerIter,
                adjustCount     = _optAdjustCount,
                adjustHp        = _optAdjustHp,
                adjustDamage    = _optAdjustDamage,
                adjustInterval  = _optAdjustInterval,
            };

            _lastOptResult = WaveSimOptimizer.Run(cfg, waveEntries, targets,
                (prog, msg) =>
                {
                    _optimizeProgress    = prog;
                    _optimizeProgressMsg = msg;
                    _statusMessage       = msg;
                    // 에디터 즉각 갱신 (메인 스레드에서만)
                    Repaint();
                });

            _statusMessage = _lastOptResult.converged
                ? $"최적화 완료 — 최종 승률 {_lastOptResult.finalWinRate:P1}"
                : $"최대 반복 도달 — 최종 승률 {_lastOptResult.finalWinRate:P1}";

            // 유닛 분석 탭에 표시할 기준 데이터 생성
            var baseRuns = new List<SimRunResult>(100);
            for (int i = 0; i < 100; i++)
                baseRuns.Add(WaveSimEngine.Run(cfg, cfg.seed + i));
            _lastResult = WaveSimAnalyzer.Aggregate(baseRuns);
            WaveSimAnalyzer.ComputeCostEfficiency(_lastResult, cfg.playerTemplates);

            _rightTabIndex = 5; // 최적화 탭으로 이동
        }
        catch (Exception ex)
        {
            _statusMessage = $"최적화 오류: {ex.Message}";
            Debug.LogError($"[WaveBalanceSim Optimize] {ex}");
        }
        finally
        {
            _isRunning          = false;
            _optimizeProgress   = 0f;
            Repaint();
        }
    }

    // ════════════════════════════════════════════════════════════
    //  UI 유틸리티
    // ════════════════════════════════════════════════════════════

    private static void DrawResultRow(string label, string value, Color? valueColor = null)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(150));

        GUIStyle vs = new GUIStyle(EditorStyles.label);
        if (valueColor.HasValue) vs.normal.textColor = valueColor.Value;
        EditorGUILayout.LabelField(value, vs);
        EditorGUILayout.EndHorizontal();
    }

    private static void DrawHeaderCell(string text, float width)
    {
        GUIStyle s = new GUIStyle(EditorStyles.miniLabel) { fontStyle = FontStyle.Bold };
        EditorGUILayout.LabelField(text, s, GUILayout.Width(width));
    }

    private static void DrawColoredBox(Color bg, Action content)
    {
        Rect r = EditorGUILayout.BeginVertical();
        if (bg != Color.clear) EditorGUI.DrawRect(r, bg);
        content();
        EditorGUILayout.EndVertical();
    }

    private static void DrawRangeField(
        string label,
        ref float low, ref float high,
        float min, float max,
        bool asPercent)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(140));

        if (asPercent)
        {
            float lowPct  = low  * 100f;
            float highPct = high * 100f;
            EditorGUILayout.LabelField("최소%", GUILayout.Width(30));
            lowPct  = EditorGUILayout.FloatField(lowPct,  GUILayout.Width(50));
            EditorGUILayout.LabelField("최대%", GUILayout.Width(30));
            highPct = EditorGUILayout.FloatField(highPct, GUILayout.Width(50));
            low  = Mathf.Clamp(lowPct  / 100f, min, max);
            high = Mathf.Clamp(highPct / 100f, min, max);
        }
        else
        {
            EditorGUILayout.LabelField("최소", GUILayout.Width(24));
            low  = EditorGUILayout.FloatField(low,  GUILayout.Width(60));
            EditorGUILayout.LabelField("최대", GUILayout.Width(24));
            high = EditorGUILayout.FloatField(high, GUILayout.Width(60));
            low  = Mathf.Clamp(low,  min, max);
            high = Mathf.Clamp(high, min, max);
        }
        EditorGUILayout.EndHorizontal();
    }
}
#endif
