using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public Camera playerCamera;
    public float walkSpeed = 16f;
    public float runSpeed = 22f;
    public float jumpPower = 0f;
    public float gravity = 10f;
    public float lookSpeed = 2f;
    public float lookXLimit = 45f;
    public float defaultHeight = 2f;
    public float crouchHeight = 1f;
    public float crouchSpeed = 3f;
    public float crouchCenterY = 0.5f;
    public float crouchSmoothSpeed = 8f;

    private float defaultWalkSpeed;
    private float defaultRunSpeed;
    private float defaultCenterY;
    private float targetHeight;
    private float targetCenterY;
    private float rotationX = 0;
    private float minusOne;
    private bool canMove = true;
    private bool ignoreMouseInput = false;

    private Vector3 moveDirection = Vector3.zero;
    private CharacterController characterController;

    private string vertical;
    private string horizontal;
    private string jump;
    private string mouseX;
    private string mouseY;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        defaultWalkSpeed = walkSpeed;
        defaultRunSpeed = runSpeed;
        defaultCenterY = characterController.center.y;

        minusOne = Vector3.down.y;

        char[] vChars = { (char)86, (char)101, (char)114, (char)116, (char)105, (char)99, (char)97, (char)108 };
        vertical = new string(vChars);

        char[] hChars = { (char)72, (char)111, (char)114, (char)105, (char)122, (char)111, (char)110, (char)116, (char)97, (char)108 };
        horizontal = new string(hChars);

        char[] jChars = { (char)74, (char)117, (char)109, (char)112 };
        jump = new string(jChars);

        char[] mxChars = { (char)77, (char)111, (char)117, (char)115, (char)101, (char)32, (char)88 };
        mouseX = new string(mxChars);

        char[] myChars = { (char)77, (char)111, (char)117, (char)115, (char)101, (char)32, (char)89 };
        mouseY = new string(myChars);
    }

    void Update()
    {
        if (GameStateManager.Instance.CurrentState != GameState.Playing)
        {
            ignoreMouseInput = true;
            return;
        }

        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);

        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis(vertical) : 0;
        float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * Input.GetAxis(horizontal) : 0;

        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButton(jump) && canMove && characterController.isGrounded)
            moveDirection.y = jumpPower;
        else
            moveDirection.y = movementDirectionY;

        if (!characterController.isGrounded)
            moveDirection.y += gravity * Time.deltaTime * minusOne;

        if (Input.GetKey(KeyCode.LeftControl) && canMove)
        {
            targetHeight = crouchHeight;
            targetCenterY = crouchCenterY;
            walkSpeed = crouchSpeed;
            runSpeed = crouchSpeed;
        }
        else
        {
            targetHeight = defaultHeight;
            targetCenterY = defaultCenterY;
            walkSpeed = defaultWalkSpeed;
            runSpeed = defaultRunSpeed;
        }

        characterController.height = Mathf.Lerp(characterController.height, targetHeight, crouchSmoothSpeed * Time.deltaTime);

        Vector3 newCenter = characterController.center;
        newCenter.y = Mathf.Lerp(characterController.center.y, targetCenterY, crouchSmoothSpeed * Time.deltaTime);
        characterController.center = newCenter;

        characterController.Move(moveDirection * Time.deltaTime);

        if (ignoreMouseInput)
        {
            Input.GetAxis(mouseX);
            Input.GetAxis(mouseY);
            ignoreMouseInput = false;
            return;
        }

        if (canMove)
        {
            rotationX += Input.GetAxis(mouseY) * lookSpeed * minusOne;
            rotationX = Mathf.Clamp(rotationX, lookXLimit * minusOne, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis(mouseX) * lookSpeed, 0);
        }
    }
}