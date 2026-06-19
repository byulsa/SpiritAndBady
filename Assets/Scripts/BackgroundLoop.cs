using UnityEngine;

public class BackgroundLoop : MonoBehaviour
{
    [Header("��� ����")]
    public float maxSpeed = 100f;

    [Header("�׽�Ʈ ����")]
    public bool isTestMode = true;
    public KeyCode speedUpKey = KeyCode.RightArrow;
    public KeyCode speedDownKey = KeyCode.LeftArrow;

    public float currentSpeed = 0f;
    private float testSpeed = 0f;
    private Transform[] backgrounds;
    private float bgWidth;

    void Start()
    {
        backgrounds = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
            backgrounds[i] = transform.GetChild(i);

        bgWidth = backgrounds[0].GetComponent<Renderer>().bounds.size.x;
    }

    void Update()
    {
        if (isTestMode) HandleTestInput();

        transform.Translate(Vector3.left * currentSpeed * Time.deltaTime);

        foreach (Transform bg in backgrounds)
        {
            if (bg.position.x < -bgWidth)
            {
                float rightmostX = GetRightmostX();
                bg.position = new Vector3(rightmostX + bgWidth, bg.position.y, bg.position.z);
            }
        }
    }

    void HandleTestInput()
    {
        if (Input.GetKey(speedUpKey))
            testSpeed = Mathf.Clamp(testSpeed + Time.deltaTime * 10f, 0f, maxSpeed);
        if (Input.GetKey(speedDownKey))
            testSpeed = Mathf.Clamp(testSpeed - Time.deltaTime * 10f, 0f, maxSpeed);

        SetSpeed(testSpeed);
    }

    public void SetSpeed(float speed)
    {
        currentSpeed = Mathf.Lerp(currentSpeed, speed, Time.deltaTime * 3f);
    }

    float GetRightmostX()
    {
        float max = float.MinValue;
        foreach (Transform bg in backgrounds)
            if (bg.position.x > max) max = bg.position.x;
        return max;
    }
}