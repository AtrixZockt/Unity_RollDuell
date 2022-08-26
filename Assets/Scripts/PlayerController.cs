using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private LayerMask platformLayerMask;
    private Rigidbody2D rb;
    private CircleCollider2D pBoxCollider;
    public float speed = 5f;
    public float jumpHeight = 300f;
    public PlayerControls playerControls;

    public int jumpsLeft = 1;

    Vector2 moveDirectoin = Vector2.zero;
    private InputAction move;
    private InputAction jump;
    private InputAction fire;
    private InputAction groundDash;

    private void Awake()
    {
        playerControls = new PlayerControls();
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) Destroy(this);
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        pBoxCollider = GetComponent<CircleCollider2D>();
    }

    private void OnEnable()
    {
        move = playerControls.Player.Move;
        move.Enable();

        jump = playerControls.Player.Jump;
        jump.Enable(); 
        jump.performed += Jump;


        fire = playerControls.Player.Fire;
        fire.Enable();
        fire.performed += Fire;

        groundDash = playerControls.Player.DashDown;
        groundDash.Enable();
        groundDash.performed += GroundPound;
    }

    private void OnDisable()
    {
        move.Disable();
        jump.Disable();
        fire.Disable();
        groundDash.Disable();
    }

    void Update()
    {
        moveDirectoin = move.ReadValue<Vector2>();
        if (IsGrounded())
        {
            jumpsLeft = 1;
            if (rb.gravityScale != 1f)
            {
                rb.gravityScale = 1f;
            };
        }
    }

    private void FixedUpdate()
    {
        rb.velocity = new Vector2(moveDirectoin.x * speed, rb.velocity.y);
    }

    private void Jump(InputAction.CallbackContext context)
    {
        if(IsGrounded() || jumpsLeft > 0)
        {
            jumpsLeft -= 1;
            rb.AddForce(Vector2.up * jumpHeight);
            //rb.velocity = Vector2.up * jumpHeight;
            if (jumpsLeft < 0)
                jumpsLeft = 0;
        }
    }

    private void Fire(InputAction.CallbackContext context)
    {
        Debug.Log("FIREEE");
    }

    private void GroundPound(InputAction.CallbackContext context)
    {
        if (!IsGrounded() && context.ReadValue<Vector2>().y < -0.7f)
        {
            rb.gravityScale = 20f;
        }
    }

    private bool IsGrounded()
    {
        float extraHeight = .05f;
        RaycastHit2D raycastHit = Physics2D.Raycast(pBoxCollider.bounds.center, Vector2.down, pBoxCollider.bounds.extents.y + extraHeight, platformLayerMask);
        return raycastHit.collider != null;
    }
}
