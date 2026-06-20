using UnityEngine;
using TMPro;
public class ExpectedSpeedGainUI : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private TrainSpeedController Controller;
    void Awake()
    {
        if (Controller == null)
        {
            Controller = FindAnyObjectByType<TrainSpeedController>();
        }
        if (text == null)
        {
            TryGetComponent(out text);
        }
    }
    void OnEnable()
    {
        if (Controller)
        {
            Controller.OnExpectedSpeedGainChanged += HandlOnExpectedSpeedGainChangede;
        }
    }
    void OnDisable()
    {
        if (Controller)
        {
            Controller.OnExpectedSpeedGainChanged -= HandlOnExpectedSpeedGainChangede;
        }
    }
    private void HandlOnExpectedSpeedGainChangede(float Value)
    {
        text.text = "+" + Value.ToString("F2") + " Km/h";
    }
}
