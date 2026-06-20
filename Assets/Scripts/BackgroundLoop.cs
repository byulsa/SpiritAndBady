using UnityEngine;

public class BackgroundLoop : MonoBehaviour
{
    public float currentSpeed = 0f;
    private float targetSpeed = 0f;
    private float offset = 0f;
    private Material mat;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
    }

    void Update()
    {
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 3f);

        offset += currentSpeed * Time.deltaTime * 0.01f; // 속도에 맞게 조정
        mat.mainTextureOffset = new Vector2(offset, 0f);
    }

    public void SetSpeed(float speed)
    {
        targetSpeed = speed;
    }
}