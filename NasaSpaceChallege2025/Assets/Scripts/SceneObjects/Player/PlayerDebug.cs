using UnityEngine;

public class PlayerDebug : MonoBehaviour
{
    CharacterController cc;
    Animator anim;
    PlayerMovementScript mover;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<Animator>();
        mover = GetComponent<PlayerMovementScript>();
    }

    void OnGUI()
    {
        return;
        if (!mover || !cc || !anim) return;
        var v = cc.velocity;
        GUI.Label(new Rect(10,10,400,120),
            $"Velocity: {v.magnitude:F2}\n" +
            $"Grounded: {cc.isGrounded}\n" +
            $"Current state: {anim.GetCurrentAnimatorStateInfo(0).shortNameHash}\n" +
            $"CurrentStateName: {(mover != null ? mover.GetType().Name : "none")}");
    }
}