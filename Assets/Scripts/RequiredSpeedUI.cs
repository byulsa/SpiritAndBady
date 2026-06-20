using UnityEngine;
using TMPro;

public class RequiredSpeedUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI requiredSpeedText;
    [SerializeField] private ObstacleSpawner obstacleSpawner;

    void Update()
    {
        if (obstacleSpawner != null)
            requiredSpeedText.text = $"요구 속도 : {obstacleSpawner.currentRequiredSpeed:F0}";
    }
}