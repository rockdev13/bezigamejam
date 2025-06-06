using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextAppear : MonoBehaviour
{
    private Animator textE;
    private TextMeshProUGUI textETM;
    public GameObject player;

    void Start()
    {
        textE = GetComponent<Animator>();
    }
    // void Update()
    // {
    //     if(player.GetComponent<ComeIn>().near)
    //     {
    //         textETM.text = "Press E to enter";
    //     }
    //     else if(player.GetComponent<PlayerMovement>().canOpen)
    //     {
    //         textETM.text = "Press E to unlock";
    //     }
    // }

    public void Appear()
    {
        textE.SetTrigger("Appear");
    }
    public void Disappear()
    {
        textE.SetTrigger("Disappear");
    }
}
