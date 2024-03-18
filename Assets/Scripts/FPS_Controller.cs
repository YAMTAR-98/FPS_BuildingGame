using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
[RequireComponent(typeof(CharacterController))]
public class FPS_Controller : MonoBehaviour
{
    CharacterController characterController;
    [Header("CaharacterCamera")]
    public Camera playerCamera;
    [Header("Movement Values")]
    public float walkSpeed = 6f;
    public float runSpeed = 12f;
    public float jumpPower = 3f;
    public float gravity = 10f;

    [Header("Look Values")]
    internal bool canRotate = true;
    public float lookSpeed = 7f;
    public float lookXLimit = 60f;

    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;

    public bool canMove = true;
    bool canJump = true;

    private void Start() {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void Update() {
        #region Handles Movement
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        //PressLeftShiftToRun
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        #endregion

        #region Handles Jumping
        if(Input.GetButton("Jump") && canJump && canMove && characterController.isGrounded){
            moveDirection.y = jumpPower;
            canJump = false;
        }else{
            moveDirection.y = movementDirectionY;
        }
        if(Input.GetButtonUp("Jump")){
            canJump = true;
        }

        if(!characterController.isGrounded){
            moveDirection.y -= gravity * Time.deltaTime;
        }
        #endregion

        #region Handles Rotation
        characterController.Move(moveDirection * Time.deltaTime);

        if(canMove && canRotate){
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX , 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed , 0);
        }
        #endregion
    }
}