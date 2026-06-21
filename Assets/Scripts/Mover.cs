using Unity.VisualScripting;
using UnityEngine;

public class Mover : MonoBehaviour
{
    public event System.Action<Transform> OnEnd;
    void OnTriggerEnter(Collider other)
    {
        OnEnd?.Invoke(transform);
    }
}
