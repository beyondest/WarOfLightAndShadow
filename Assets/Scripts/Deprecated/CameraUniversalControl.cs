using UnityEngine;

namespace SparFlame.GamePlaySystem.CameraControl
{
    public class CameraControl : MonoBehaviour
    {
        public float lookSpeedH = 2f;
        public float lookSpeedV = 2f;
        public float zoomSpeed = 2f;
        public float dragSpeed = 5f;

        private float _yaw;
        private float _pitch;

        private void Start()
        {
            // x - right    pitch
            // y - up       yaw
            // z - forward  roll
            _yaw = transform.eulerAngles.y;
            _pitch = transform.eulerAngles.x;
        }

        void Update()
        {
            if (!enabled) return;

            if (Input.touchCount > 0)
            {
                float touchToMouseScale = 0.25f;
                // look around with first touch
                Touch t0 = Input.GetTouch(0);
                _yaw += lookSpeedH * touchToMouseScale * t0.deltaPosition.x;
                _pitch -= lookSpeedV * touchToMouseScale * t0.deltaPosition.y;
                transform.eulerAngles = new Vector3(_pitch, _yaw, 0f);

                // and if have extra touch, also fly forward
                if (Input.touchCount > 1)
                {
                    Touch t1 = Input.GetTouch(1);
                    Vector3 offset = new Vector3(t1.deltaPosition.x, 0, t1.deltaPosition.y);
                    transform.Translate(offset * Time.deltaTime * touchToMouseScale, Space.Self);
                }
            }
            else
            {
                //Look around with Right Mouse
                if (Input.GetMouseButton(1))
                {
                    _yaw += lookSpeedH * Input.GetAxis("Mouse X");
                    _pitch -= lookSpeedV * Input.GetAxis("Mouse Y");

                    transform.eulerAngles = new Vector3(_pitch, _yaw, 0f);

                    Vector3 offset = Vector3.zero;
                    float offsetDelta = Time.deltaTime * dragSpeed;
                    if (Input.GetKey(KeyCode.LeftShift)) offsetDelta *= 5.0f;
                    if (Input.GetKey(KeyCode.S)) offset.z -= offsetDelta;
                    if (Input.GetKey(KeyCode.W)) offset.z += offsetDelta;
                    if (Input.GetKey(KeyCode.A)) offset.x -= offsetDelta;
                    if (Input.GetKey(KeyCode.D)) offset.x += offsetDelta;
                    if (Input.GetKey(KeyCode.Q)) offset.y -= offsetDelta;
                    if (Input.GetKey(KeyCode.E)) offset.y += offsetDelta;

                    transform.Translate(offset, Space.Self);
                }

                transform.Translate(0, 0, Input.GetAxis("Mouse ScrollWheel") * zoomSpeed, Space.Self);
            }
        }
    }
}

// //drag camera around with Middle Mouse
// if (Input.GetMouseButton(2))
// {
//     transform.Translate(-Input.GetAxisRaw("Mouse X") * Time.deltaTime * dragSpeed,
//         -Input.GetAxisRaw("Mouse Y") * Time.deltaTime * dragSpeed, 0);
// }

//Zoom in and out with Mouse Wheel