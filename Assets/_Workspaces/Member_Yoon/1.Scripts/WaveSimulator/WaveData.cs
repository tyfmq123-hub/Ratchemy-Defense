using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 웨이브 내 단일 적 등장 항목
/// </summary>
[Serializable]
public class WaveEnemyEntry
{
    [Header("등장 시간")]
    [Tooltip("게임 시작 후 최초 소환 시각 (초)")]
    public float spawnTime = 0f;
    [Tooltip("연속 소환 간격 (초) — count > 1 일 때 사용")]
    public float spawnInterval = 0.5f;

    [Header("적 정보 (단일)")]
    [Tooltip("enemyPool이 비어있을 때 사용")]
    public EnemyUnitData enemyData;

    [Header("적 풀 (랜덤 선택)")]
    [Tooltip("설정 시 소환마다 풀에서 랜덤 선택 — 실제 게임 WaveSetting과 동일한 방식")]
    public List<EnemyUnitData> enemyPool = new List<EnemyUnitData>();

    [Tooltip("이 항목에서 소환할 유닛 수")]
    public int count = 1;
    [Tooltip("보스 여부 (시뮬레이터 BossKiller 태그 판정에 사용)")]
    public bool isBoss = false;

    [HideInInspector]
    [Tooltip("ImportFromSceneWaveManager가 설정하는 원본 WaveSetting 배열 인덱스")]
    public int waveSettingIndex = -1;
}

/// <summary>
/// 웨이브 전체 구성을 담는 ScriptableObject
/// AssetDatabase 기반 시뮬레이터가 직접 읽는다.
/// </summary>
[CreateAssetMenu(fileName = "WaveData", menuName = "Game/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("웨이브 정보")]
    public string waveName = "Wave";
    [TextArea(1, 3)]
    public string description;

    [Header("적 등장 목록")]
    public List<WaveEnemyEntry> entries = new List<WaveEnemyEntry>();
}
