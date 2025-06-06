using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float Speed;
    [SerializeField] private float SpeedRun;
    float currentSpeed;
    private Rigidbody2D _rb;
    private Vector2 _moveInput;
    private Animator _animator;

    void Start()
    {
        currentSpeed = Speed;
        _rb = GetComponent<Rigidbody2D>();
        _animator = GetComponent<Animator>();
    }

   void Update()
    {
        if(Input.GetKeyDown(KeyCode.LeftShift))
        {
            currentSpeed = SpeedRun;
        }
        if(Input.GetKeyUp(KeyCode.LeftShift))
        {
            currentSpeed = Speed;
        }
        _rb.linearVelocity = _moveInput * currentSpeed;

        bool isWalking = _moveInput.magnitude > 0.1f;
        _animator.SetBool("isWalking", isWalking);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        _moveInput = context.ReadValue<Vector2>().normalized;

        float inputX = Mathf.Round(_moveInput.x);
        float inputY = Mathf.Round(_moveInput.y);

        _animator.SetFloat("InputX", inputX);
        _animator.SetFloat("InputY", inputY);

        if (_moveInput.magnitude > 0.1f)
        {
            _animator.SetFloat("LastInputX", inputX);
            _animator.SetFloat("LastInputY", inputY);
        }
    }
}