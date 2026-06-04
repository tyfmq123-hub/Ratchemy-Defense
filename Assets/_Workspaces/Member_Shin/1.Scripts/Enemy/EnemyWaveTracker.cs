using System;      
using UnityEngine; 
public class EnemyWaveTracker : MonoBehaviour
{
    private Action onRemoved;
    private bool hasReportedRemoval;

    public void Initialize(Action removedCallback)
    {
        onRemoved = removedCallback;
        hasReportedRemoval = false;
    }

    private void OnDestroy()
    {
        NotifyRemoved();
    }

    public void NotifyRemoved()
    {
        if (hasReportedRemoval)
        {
            return;
        }
        hasReportedRemoval = true;
        onRemoved?.Invoke();
    }
}