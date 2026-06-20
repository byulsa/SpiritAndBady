using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class SpeedUI : MonoBehaviour
{
    [SerializeField] private Image image;
    private float MaxFiilValue = 0.75f;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TrainSpeedController Controller;
    void Awake()
    {
        if(Controller == null)
        {
            Controller = FindAnyObjectByType<TrainSpeedController>();
        }
    }
    void OnEnable()
    {
        if(Controller)
        {
            Controller.OnSpeedChanged += OnSpeedChanged;
        }
    }
    void OnDisable()
    {
        if(Controller)
        {
            Controller.OnSpeedChanged -= OnSpeedChanged;
        }
    }
    private void OnSpeedChanged(float speed)
    {
        if(image && Controller)
        {
            image.fillAmount = speed / Controller.GetMaxSpeed() * MaxFiilValue;
        }
        if(speedText)
        {
            speedText.text = speed.ToString("F2") + " Km/h";
        }
    }
}
