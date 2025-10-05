using UnityEngine;

public class PlayerLookScript : MonoBehaviour
{
    public enum RotationAxis { MouseX = 1, MouseY = 2 }
    public RotationAxis axes = RotationAxis.MouseX;

    public float minimumVert = -85f;
    public float maximumVert = 85f;
    public float sensHorizontal = 10f;
    public float sensVertical = 10f;

    private float _rotationX = 0;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update() => CalculateLookRotation();

    private void CalculateLookRotation()
    {
        if (axes == RotationAxis.MouseX)
        {
            transform.Rotate(0, Input.GetAxis("Mouse X") * sensHorizontal, 0);
        }
        else if (axes == RotationAxis.MouseY)
        {
            _rotationX -= Input.GetAxis("Mouse Y") * sensVertical;
            _rotationX = Mathf.Clamp(_rotationX, minimumVert, maximumVert);
            float rotationY = transform.localEulerAngles.y;
            transform.localEulerAngles = new Vector3(_rotationX, rotationY, 0);
        }
    }
}