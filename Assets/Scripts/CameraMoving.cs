using System.Collections;
using UnityEngine;

public class CameraMoving : MonoBehaviour
{
    [Header("Camera")]
    public Camera RoomCamera;
    public Camera TrainCamera;

    private Vector3 roomOriginPos;
    private Vector3 trainOriginPos;
    private float roomOriginFov;

    private Coroutine shakeCoroutine;
    private Coroutine zoomCoroutine;

    private void Start()
    {
        roomOriginPos = RoomCamera.transform.localPosition;
        trainOriginPos = TrainCamera.transform.localPosition;
        roomOriginFov = RoomCamera.fieldOfView;
    }

    public void Shake(float duration, float magnitude)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
        }

        shakeCoroutine = StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        float timer = 0f;

        while (timer < duration)
        {
            Vector2 offset = Random.insideUnitCircle * magnitude;

            RoomCamera.transform.localPosition =
                roomOriginPos + new Vector3(offset.x, offset.y, 0);

            TrainCamera.transform.localPosition =
                trainOriginPos + new Vector3(offset.x, offset.y, 0);

            timer += Time.deltaTime;

            yield return null;
        }

        RoomCamera.transform.localPosition = roomOriginPos;
        TrainCamera.transform.localPosition = trainOriginPos;

        shakeCoroutine = null;
    }


    public void Zoom(float targetFov, float duration)
    {
        if (zoomCoroutine != null)
        {
            StopCoroutine(zoomCoroutine);
        }

        zoomCoroutine = StartCoroutine(ZoomCoroutine(targetFov, duration));
    }

    private IEnumerator ZoomCoroutine(float targetFov, float duration)
    {
        float timer = 0f;

        while (timer < duration)
        {
            float t = timer / duration;

            // targetFov까지 갔다가 다시 원래 FOV로 돌아옴
            float fov = Mathf.Lerp(
                targetFov,
                roomOriginFov,
                t);

            RoomCamera.fieldOfView = fov;

            timer += Time.deltaTime;

            yield return null;
        }

        RoomCamera.fieldOfView = roomOriginFov;

        zoomCoroutine = null;
    }
}