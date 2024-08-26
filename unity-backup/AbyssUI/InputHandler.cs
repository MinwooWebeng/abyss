using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    [SerializeField] private InputActionAsset actions;

    [SerializeField] private UIHandler uiHandler;

    //main
    private InputAction viewAction;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction crouchAction;
    private InputAction enterUIAction;

    //ui
    private InputAction mainReturnAction;

    void Awake()
    {
        //main
        viewAction = actions.FindActionMap("main").FindAction("view", throwIfNotFound: true);
        moveAction = actions.FindActionMap("main").FindAction("move", throwIfNotFound: true);
        jumpAction = actions.FindActionMap("main").FindAction("jump", throwIfNotFound: true);
        crouchAction = actions.FindActionMap("main").FindAction("crouch", throwIfNotFound: true);
        enterUIAction = actions.FindActionMap("main").FindAction("enter_ui", throwIfNotFound: true);

        viewAction.Enable();
        moveAction.Enable();
        jumpAction.Enable();
        crouchAction.Enable();
        enterUIAction.Enable();

        jumpAction.performed += OnJump;
        crouchAction.performed += OnCrouch;
        enterUIAction.performed += OnUIEnter;

        //ui
        mainReturnAction = actions.FindActionMap("ui").FindAction("return", throwIfNotFound: true);

        mainReturnAction.Enable();

        mainReturnAction.performed += OnMainReturn;
    }
    void OnEnable()
    {
        actions.FindActionMap("main").Enable();
    }
    void Update()
    {
        Vector2 viewVector = viewAction.ReadValue<Vector2>();
        Vector3 moveVector = moveAction.ReadValue<Vector3>();
        if (viewVector != Vector2.zero)
        {
            //Debug.Log(viewVector);
        }
        if (moveVector != Vector3.zero)
        {
            //Debug.Log(moveVector);
        }
    }
    private void OnJump(InputAction.CallbackContext context)
    {
        //Debug.Log("jump!");
    }
    private void OnCrouch(InputAction.CallbackContext context)
    {
        //Debug.Log("crouch!");
    }
    private void OnUIEnter(InputAction.CallbackContext context)
    {
        actions.FindActionMap("main").Disable();
        actions.FindActionMap("ui").Enable();
        uiHandler.Activate();
    }

    //ui
    private void OnMainReturn(InputAction.CallbackContext context)
    {
        uiHandler.Deactivate();
        actions.FindActionMap("ui").Disable();
        actions.FindActionMap("main").Enable();
    }
}
