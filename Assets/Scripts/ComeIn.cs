using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ComeIn : MonoBehaviour
{
    public GameObject textE;
    public bool near;

    public GameObject textWASD;
    public GameObject textShift;
    public GameObject display;

    void Start()
    {
        near = false;
        textWASD.SetActive(true);
        textShift.SetActive(false);
        display.SetActive(true);
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || 
        Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D))
        {
            if(textWASD.activeSelf)
            {
                textWASD.SetActive(false);
                textShift.SetActive(true);
            }
        }
        if(Input.GetKeyDown(KeyCode.LeftShift))
        {
            if(textShift.activeSelf)
            {
                textShift.SetActive(false);
                display.SetActive(false);
            }
        }
        
        if (near && Input.GetKeyDown(KeyCode.E))
        {
            SceneManager.LoadScene(1);
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Finish"))
        {
            near = true;
            textE.GetComponent<TextAppear>().Appear();
        }
    }

    void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Finish"))
        {
            near = false;
            textE.GetComponent<TextAppear>().Disappear();
        }
    }
}