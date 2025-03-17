using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 10f; // 이동 속도
    public float rotationSpeed = 3f; // 회전 속도
    public float zoomSpeed = 5f; // 줌 속도

    private Vector3 lastMousePosition;

    void Update()
    {
        // 🔹 마우스 오른쪽 버튼 드래그 → 카메라 회전
        if (Input.GetMouseButton(1)) // 오른쪽 클릭 유지
        {
            Vector3 delta = Input.mousePosition - lastMousePosition;
            float rotationX = delta.x * rotationSpeed * Time.deltaTime;
            float rotationY = -delta.y * rotationSpeed * Time.deltaTime;
            transform.eulerAngles += new Vector3(rotationY, rotationX, 0);
        }
        lastMousePosition = Input.mousePosition;

        // 🔹 WASD 또는 방향키 → 카메라 이동
        Vector3 move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        transform.position += transform.forward * move.z * moveSpeed * Time.deltaTime;
        transform.position += transform.right * move.x * moveSpeed * Time.deltaTime;

        // 🔹 마우스 휠 → 카메라 줌 인/아웃
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        transform.position += transform.forward * scroll * zoomSpeed;
    }
}
