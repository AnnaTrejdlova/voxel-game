using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]

public class FPSController : MonoBehaviour
{
    public float walkingSpeed = 7.5f;
    public float runningSpeed = 11.5f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;
    public Camera playerCamera;
    public float lookSpeed = 2.0f;
    public float lookXLimit = 90.0f;

    public float reachDistance = 5f;

    CharacterController characterController;
    Vector3 moveDirection = Vector3.zero;
    float rotationX = 0;

    [HideInInspector]
    public bool canMove = true;

    public GameObject blockWireframe;

    static GenerateTerrain generateTerrain;
    static UIControl UIControl;

    float lastLeftClick = 0;
    float lastRightClick = 0;
    float lastSpaceDown = 0;
    float lastSpaceUp = 0;

    void Start()
    {
        characterController = GetComponent<CharacterController>();

        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        generateTerrain = FindObjectOfType<GenerateTerrain>();
        UIControl = FindObjectOfType<UIControl>();
    }

    //[UnityEditor.Callbacks.DidReloadScripts]
    //private static void OnScriptsReloaded()
    //{
    //    Debug.Log("Scripts reloaded");
    //    generateTerrain = FindObjectOfType<GenerateTerrain>();
    //}

    void FixedUpdate()
    {
        RaycastHit hit;
        Transform cameraTransform = transform.Find("Main Camera");
        Vector3 lookDirection = cameraTransform.rotation * Vector3.forward;

        //Debug.Log("--------");
        //Debug.Log(lookDirection);
        lookDirection.Normalize();

        int layerMask = 1 << 6;
        layerMask = ~layerMask;

        if (Physics.Raycast(cameraTransform.position, lookDirection, out hit, reachDistance, layerMask))
        {
            Debug.DrawRay(cameraTransform.position, lookDirection * hit.distance, Color.yellow);

            Vector3 pos = cameraTransform.position + lookDirection * hit.distance;
            //Debug.Log(pos.x.ToString("f10"));

            pos += new Vector3(0.0001f, 0.0001f, 0.0001f);
            pos.x = Mathf.Round(pos.x) + ((lookDirection.x < 0f) ? -hit.normal.x : 0f);
            pos.y = Mathf.Floor(pos.y) + 0.5f + ((lookDirection.y < 0f) ? -hit.normal.y : 0f);
            pos.z = Mathf.Round(pos.z) + ((lookDirection.z < 0f) ? -hit.normal.z : 0f);

            blockWireframe.transform.position = pos;
            blockWireframe.SetActive(true);

            //Debug.Log(pos.x);

            if ((Time.fixedTime - lastLeftClick) >= 0.300f && Input.GetMouseButton(0))
            {
                lastLeftClick = Time.fixedTime;
                generateTerrain.DestroyBlock(pos);
            } else if (!Input.GetMouseButton(0)) {
                lastLeftClick = 0 ;
            }

            if ((Time.fixedTime - lastRightClick) >= 0.300f && Input.GetMouseButton(1))
            {
                lastRightClick = Time.fixedTime;

                Vector3 newPos = pos + hit.normal;

                GameObject playerObject = GameObject.Find("FPSPlayer");
                Vector3 playerPos = playerObject.transform.position;
                CharacterController characterController = playerObject.GetComponent<CharacterController>();
                float radius = characterController.radius;
                float height = characterController.height;
                Vector3 center = characterController.center;

                //if (isBlock(UIControl.itemBarIds[UIControl.selectedPos-1]))
                //{
                    if (newPos.y + 0.5f > playerPos.y + center.y - height / 2 && newPos.y - 0.5f < playerPos.y + center.y + height / 2)
                    {
                        if (Mathf.Abs(newPos.x - playerPos.x) > (0.5f + radius) || Mathf.Abs(newPos.z - playerPos.z) > (0.5f + radius))
                        {
                            generateTerrain.AddBlock(pos, hit.normal, UIControl.itemBarIds[UIControl.selectedPos-1]);
                        }
                    } else
                    {
                        generateTerrain.AddBlock(pos, hit.normal, UIControl.itemBarIds[UIControl.selectedPos-1]);
                    }
                //}
                //if (isItem(UIControl.itemBarIds[UIControl.selectedPos-1]))
                //{
                //    use();
                //}
            }
            else if (!Input.GetMouseButton(1))
            {
                lastRightClick = 0;
            }
        }
        else
        {
            blockWireframe.SetActive(false);
            Debug.DrawRay(cameraTransform.position, lookDirection * reachDistance, Color.white);
        }
    }

    void Update()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        // We are grounded, so recalculate move direction based on axes
        Vector3 forward = transform.TransformDirection(Vector3.forward);
        Vector3 right = transform.TransformDirection(Vector3.right);
        // Press Left Shift to run
        bool isRunning = Input.GetKey(KeyCode.LeftControl);
        float curSpeedX = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Vertical") : 0;
        float curSpeedY = canMove ? (isRunning ? runningSpeed : walkingSpeed) * Input.GetAxis("Horizontal") : 0;
        float movementDirectionY = moveDirection.y;
        moveDirection = (forward * curSpeedX) + (right * curSpeedY);

        if (Input.GetButtonDown("Jump"))
        {
            if ((Time.time - lastSpaceUp) <= 0.200f)
            {
                if (gravity == 0)
                {
                    gravity = 20f;
                }
                else
                {
                    gravity = 0;
                }
            }
            lastSpaceDown = Time.time;
        }
        if ((Time.time - lastSpaceDown) <= 0.200f && Input.GetButtonUp("Jump"))
        {
            lastSpaceUp = Time.time;
        }
        if (Input.GetButtonDown("Jump"))
        {

        }

        if (Input.GetButton("Jump") && canMove && characterController.isGrounded)
        {
            moveDirection.y = jumpSpeed;
        }
        else if (Input.GetButton("Jump") && gravity == 0 && canMove)
        {
            moveDirection.y = walkingSpeed * Input.GetAxis("Jump");
        }
        else if (gravity == 0 && canMove)
        {
            moveDirection.y = 0;
        }
        else
        {
            moveDirection.y = movementDirectionY;
        }

        // Apply gravity. Gravity is multiplied by deltaTime twice (once here, and once below
        // when the moveDirection is multiplied by deltaTime). This is because gravity should be applied
        // as an acceleration (ms^-2)
        if (!characterController.isGrounded)
        {
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Move the controller
        characterController.Move(moveDirection * Time.deltaTime);

        // Player and Camera rotation
        if (canMove)
        {
            rotationX += -Input.GetAxis("Mouse Y") * lookSpeed;
            rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeed, 0);
        }
    }
}