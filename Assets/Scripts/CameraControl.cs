using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public static Camera Cam;
    void Awake() => Cam = GetComponent<Camera>();
    void Update()
    {
        float speed = Input.GetKey(KeyCode.LeftShift) ? 60 : 30;
        
        if (Input.GetKey(KeyCode.W))
            transform.position += Vector3.up * (speed * Time.deltaTime);
        if (Input.GetKey(KeyCode.D))
            transform.position += Vector3.right * (speed * Time.deltaTime);
        if (Input.GetKey(KeyCode.S))
            transform.position += Vector3.down * (speed * Time.deltaTime);
        if (Input.GetKey(KeyCode.A))
            transform.position += Vector3.left * (speed * Time.deltaTime);

        switch (Input.mouseScrollDelta.y)
        {
            case > 0:
                Cam.orthographicSize /= 1.5f;
                break;
            case < 0:
                Cam.orthographicSize *= 1.5f;
                break;
        }
    }
}