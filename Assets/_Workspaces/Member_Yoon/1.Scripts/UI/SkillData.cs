using UnityEngine;

[System.Serializable]
public class SkillData
{
    public string skillName;
    public Sprite skillIcon;
    [TextArea(2, 4)] public string skillDescription;
}
