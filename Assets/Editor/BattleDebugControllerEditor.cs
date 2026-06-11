using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BattleDebugController))]
public class BattleDebugControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("전투 장면 테스트", EditorStyles.boldLabel);

        BattleDebugController debugController =
            (BattleDebugController)target;

        if (GUILayout.Button("보스 경고 연출 실행"))
        {
            debugController.TestBossWarning();
        }

        if (GUILayout.Button("승리 화면 실행"))
        {
            debugController.TestVictory();
        }

        if (GUILayout.Button("패배 화면 실행"))
        {
            debugController.TestDefeat();
        }
    }
}