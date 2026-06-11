using System.Collections;
using TMPro;
using UnityEngine;

public class DamageText : MonoBehaviour
{
    [SerializeField] private TextMeshPro label;

    public void Show(int damage, Color color)
    {
        label.text = damage.ToString();
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        const float duration = 0.8f;
        float elapsed = 0f;
        Vector3 origin = transform.position;
        Color baseColor = label.color;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            transform.position = origin + Vector3.up * (Mathf.Sqrt(t) * 1.2f);
            label.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }
}
