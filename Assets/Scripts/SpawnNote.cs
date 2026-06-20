using UnityEngine;

public class SpawnNote : MonoBehaviour
{
    public float SpawnTime = 1f;
    private float currentTime = 0f;
    public GameObject prefabObject;
    public Judgement judgement;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        currentTime += Time.deltaTime;
        if (currentTime > SpawnTime)
        {
            currentTime = 0;
            GameObject Note = Instantiate(prefabObject, transform.position, Quaternion.identity);
            //judgement.notes.Add(Note.transform);
        }
    }
}
