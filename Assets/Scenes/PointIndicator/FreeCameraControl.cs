using UnityEngine;

public class FreeCameraControl : MonoBehaviour
{
    public float rotationSpeed = 10.0f;

    void Update()
    {
        float horizontalInput = Input.GetAxis("Mouse X");
        float verticalInput = Input.GetAxis("Mouse Y");

        transform.Rotate(Vector3.up, horizontalInput * rotationSpeed * Time.deltaTime);
        transform.Rotate(Vector3.right, -verticalInput * rotationSpeed * Time.deltaTime);
    }
}
