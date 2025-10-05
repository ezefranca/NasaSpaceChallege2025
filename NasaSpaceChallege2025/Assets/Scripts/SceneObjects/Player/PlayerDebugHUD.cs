using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerMovementScript))]
public class PlayerDebugHUD : MonoBehaviour
{
    Text label;
    PlayerMovementScript mover;
    CharacterController cc;

    void Awake()
    {
        mover = GetComponent<PlayerMovementScript>();
        cc = GetComponent<CharacterController>();

        // Create world-space canvas in front of the camera
        var cam = Camera.main;
        var canvasGO = new GameObject("HUD_Canvas", typeof(Canvas));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = cam;
        canvas.planeDistance = 0.5f;
        canvas.sortingOrder = 999;

        var rect = canvas.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0.4f, 0.2f);

        var textGO = new GameObject("HUD_Text", typeof(Text));
        textGO.transform.SetParent(canvas.transform, false);
        label = textGO.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.alignment = TextAnchor.UpperLeft;
        label.fontSize = 24;
        label.color = Color.yellow;

        // Attach to camera so it's always visible
        if (cam)
        {
            canvasGO.transform.SetParent(cam.transform);
            canvasGO.transform.localPosition = new Vector3(0.2f, -0.2f, 1.0f);
            canvasGO.transform.localRotation = Quaternion.identity;
        }
    }

    void Update()
    {
        if (!mover || !mover.animator) return;

        float animSpeed = mover.animator.GetFloat("Speed");
        float inputMag = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).magnitude;
        float ccVel = new Vector3(cc.velocity.x, 0, cc.velocity.z).magnitude;

        label.text =
            $"Input: {inputMag:F2}\n" +
            $"CC vel: {ccVel:F2}\n" +
            $"Animator Speed: {animSpeed:F2}\n" +
            $"Grounded: {cc.isGrounded}\n" +
            $"State: {mover.animator.GetCurrentAnimatorStateInfo(0).shortNameHash}";
    }
}