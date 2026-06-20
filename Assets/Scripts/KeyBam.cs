using UnityEngine;
using UnityEngine.UI;
public class KeyBam : MonoBehaviour
{
    public Image KeyImage;
    public Sprite UnKeySprite;
    public Sprite GetKeySprite;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {


        KeyImage.sprite = (Input.GetKey(KeyCode.Space)) ? GetKeySprite : UnKeySprite;

    }
}
