using UnityEngine;

namespace UnderwaterOptimization
{
    [RequireComponent(typeof(UnityEngine.CharacterController))]
    public class DebugPlayerMotor : MonoBehaviour
    {
        [Header("引用")]
        public Transform cameraTransform;

        [Header("移动")]
        public float walkSpeed = 3.2f;
        public float sprintSpeed = 5.4f;
        public float gravity = -22f;
        public float jumpHeight = 1.0f;

        [Header("视角")]
        public float mouseSensitivity = 2.2f;
        public bool lockCursor = true;

        private UnityEngine.CharacterController controller;
        private float verticalVelocity;
        private float pitch;

        private void Awake()
        {
            controller = GetComponent<UnityEngine.CharacterController>();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (lockCursor)
                Cursor.lockState = CursorLockMode.Locked;
        }

        private void Update()
        {
            Look();
            Move();
        }

        private void Look()
        {
            if (cursorLocked())
            {
                float mx = Input.GetAxis("Mouse X") * mouseSensitivity;
                float my = Input.GetAxis("Mouse Y") * mouseSensitivity;

                transform.Rotate(0f, mx, 0f, Space.Self);

                pitch = Mathf.Clamp(pitch - my, -85f, 85f);
                if (cameraTransform != null)
                    cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            }

            // Esc 松开鼠标 / 点回 Game 视图重新锁定
            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked && lockCursor)
                Cursor.lockState = CursorLockMode.Locked;
        }

        private void Move()
        {
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            Vector3 direction = transform.right * h + transform.forward * v;
            direction.y = 0f;
            if (direction.sqrMagnitude > 1f)
                direction.Normalize();

            bool sprint = Input.GetKey(KeyCode.LeftShift);
            float speed = sprint ? sprintSpeed : walkSpeed;

            if (controller.isGrounded)
            {
                verticalVelocity = -2f;
                if (Input.GetKeyDown(KeyCode.Space))
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 motion = direction * speed;
            motion.y = verticalVelocity;
            controller.Move(motion * Time.deltaTime);
        }

        private bool cursorLocked()
        {
            return !lockCursor || Cursor.lockState == CursorLockMode.Locked;
        }
    }
}