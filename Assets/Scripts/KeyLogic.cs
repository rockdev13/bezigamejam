using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class KeyLogic : MonoBehaviour
{
    [SerializeField] Image keyForImage;
    [SerializeField] Sprite key;
    public bool canOpen;
    [SerializeField] GameObject textE;
    [SerializeField] GameObject door;

    void Start()
    {
        canOpen = false;
        keyForImage.sprite = null;
    }

    void Update()
    {
        if(canOpen)
        {
            textE.GetComponent<TextMeshProUGUI>().text = "Press E to unlock";
        }
        else if(!canOpen)
        {
            textE.GetComponent<TextMeshProUGUI>().text = "Key is requested";
        }

        if (canOpen && Input.GetKeyDown(KeyCode.E))
        {
            door.SetActive(false);
            textE.GetComponent<TextAppear>().Disappear();
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if(col.CompareTag("Key"))
        {
            Destroy(col.gameObject);
            keyForImage.sprite = key;
            keyForImage.SetNativeSize();
            canOpen = true;
        }
        if(col.CompareTag("Instrument"))
        {
            textE.GetComponent<TextAppear>().Appear();
        }
    }
    void OnTriggerExit2D(Collider2D col)
    {
        if(col.CompareTag("Instrument"))
        {
            textE.GetComponent<TextAppear>().Disappear();
        }
    }
}
