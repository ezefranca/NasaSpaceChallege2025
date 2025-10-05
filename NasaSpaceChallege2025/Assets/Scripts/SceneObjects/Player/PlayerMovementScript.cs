using UnityEngine;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementScript : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2.0f;
    public float runSpeed  = 4.5f;
    public float gravity   = 3.71f;
    public float jumpHeight = 1.6f;

    [Header("Grounding")]
    public LayerMask groundMask = ~0;   // Everything by default
    public float groundProbeExtra = 0.08f;

    [Header("Animator")]
    public Animator animator;           // Astronaut’s Animator (child)
    public float speedDamp = 0.08f;     // animator damping

    CharacterController cc;
    Vector3 vel;
    bool   grounded;

    // animator param availability (so we don't explode if missing)
    bool hasSpeed, hasGrounded, hasJump;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (animator) animator.applyRootMotion = false;

        if (animator && animator.runtimeAnimatorController)
        {
            var p = animator.parameters;
            hasSpeed    = p.Any(x => x.name == "Speed"      && x.type == AnimatorControllerParameterType.Float);
            hasGrounded = p.Any(x => x.name == "IsGrounded" && x.type == AnimatorControllerParameterType.Bool);
            hasJump     = p.Any(x => x.name == "Jump"       && x.type == AnimatorControllerParameterType.Trigger);
        }
    }

    void Update()
    {
        // --- Grounded (robust) ---
        grounded = cc.isGrounded || CapsuleGrounded();
        if (grounded && vel.y < 0f) vel.y = -2f;

        // --- Input ---
        float ix = Input.GetAxisRaw("Horizontal");
        float iz = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(ix, 0f, iz);
        float inputMag = Mathf.Clamp01(input.magnitude);      // 0..1
        bool wantsRun  = Input.GetKey(KeyCode.LeftShift) && inputMag > 0f;

        // --- Move ---
        float speed = inputMag == 0f ? 0f : (wantsRun ? runSpeed : walkSpeed);
        Vector3 move = transform.TransformDirection(input.normalized) * speed;
        cc.Move(move * Time.deltaTime);

        // --- Jump (only when grounded) ---
        if (grounded && Input.GetButtonDown("Jump"))
        {
            vel.y = Mathf.Sqrt(2f * gravity * Mathf.Max(0.01f, jumpHeight));
            if (hasJump) { animator.ResetTrigger("Jump"); animator.SetTrigger("Jump"); }
        }

        // gravity
        vel.y -= gravity * Time.deltaTime;
        cc.Move(vel * Time.deltaTime);

        // --- Drive Animator (BlendTree path) ---
        if (animator)
        {
            // We drive Speed in [0..1]: 0 idle, 0.5 walk, 1 run
            float targetAnimSpeed = 0f;
            if (inputMag > 0f) targetAnimSpeed = wantsRun ? 1f : 0.5f;

            if (hasSpeed)    animator.SetFloat("Speed", targetAnimSpeed, speedDamp, Time.deltaTime);
            if (hasGrounded) animator.SetBool("IsGrounded", grounded);
        }
    }

    bool CapsuleGrounded()
    {
        // Overlap the CharacterController capsule against groundMask
        float r = cc.radius * 0.95f;
        float h = Mathf.Max(cc.height, r * 2f);
        Vector3 center = transform.TransformPoint(cc.center);
        Vector3 top    = center + Vector3.up   * (h * 0.5f - r);
        Vector3 bottom = center + Vector3.down * (h * 0.5f - r + groundProbeExtra);
        return Physics.CheckCapsule(top, bottom, r, (groundMask.value == 0 ? ~0 : groundMask), QueryTriggerInteraction.Ignore);
    }
}