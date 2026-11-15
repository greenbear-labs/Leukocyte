using UnityEngine;

namespace Leukocyte
{
    /// <summary>First-person controller for the Leukocyte trooper.
    /// Slightly floaty movement, plus a gentle blood current pushing toward low Z.</summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public float walkSpeed = 6.5f;
        public float sprintMultiplier = 1.6f;
        public float jumpSpeed = 6.5f;
        public float gravity = 15f;
        public float mouseSensitivity = 2f;
        public Vector3 bloodCurrent = new Vector3(0f, 0f, -0.7f);

        public Health Hp { get; private set; }
        public Camera Cam { get; private set; }
        public EnzymeRifle Rifle { get; private set; }
        public Vector3 Velocity { get; private set; }
        public bool Sprinting { get; private set; }
        public bool InputEnabled => Cursor.lockState == CursorLockMode.Locked;

        static readonly Vector3 EyeOffset = new Vector3(0f, 1.6f, 0f);

        CharacterController cc;
        Vector3 momentum;
        float verticalSpeed;
        float pitch;

        public void Init(Camera cam)
        {
            Cam = cam;
            cc = GetComponent<CharacterController>();
            Hp = GetComponent<Health>();
            Rifle = gameObject.AddComponent<EnzymeRifle>();
            Rifle.Init(this, cam);
            LockCursor();
        }

        public static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void Update()
        {
            var gm = GameManager.I;
            if (!gm || gm.State != GameState.Playing) return;

            if (Input.GetKeyDown(KeyCode.Escape)) UnlockCursor();
            else if (!InputEnabled && Input.GetMouseButtonDown(0)) LockCursor();

            float dt = Time.deltaTime;

            if (Hp.IsDead)
            {
                // Slump to the floor while waiting to respawn.
                Cam.transform.localPosition = Vector3.Lerp(Cam.transform.localPosition, new Vector3(0f, 0.35f, 0f), 4f * dt);
                Cam.transform.localRotation = Quaternion.Slerp(Cam.transform.localRotation, Quaternion.Euler(pitch, 0f, 35f), 4f * dt);
                return;
            }

            if (InputEnabled)
            {
                transform.Rotate(0f, Input.GetAxis("Mouse X") * mouseSensitivity, 0f);
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * mouseSensitivity, -85f, 85f);
            }
            Cam.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);

            Vector3 input = Vector3.zero;
            if (InputEnabled)
                input = Vector3.ClampMagnitude(new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")), 1f);

            Sprinting = Input.GetKey(KeyCode.LeftShift) && input.z > 0.1f;
            Vector3 wish = transform.TransformDirection(input) * walkSpeed * (Sprinting ? sprintMultiplier : 1f);

            bool grounded = cc.isGrounded;
            float accel = (grounded ? 12f : 4f) * walkSpeed;
            momentum = Vector3.MoveTowards(momentum, wish, accel * dt);

            if (grounded)
            {
                if (verticalSpeed < 0f) verticalSpeed = -2f;
                if (InputEnabled && Input.GetButtonDown("Jump")) verticalSpeed = jumpSpeed;
            }
            verticalSpeed -= gravity * dt;

            var flags = cc.Move((momentum + bloodCurrent + Vector3.up * verticalSpeed) * dt);
            if ((flags & CollisionFlags.Above) != 0 && verticalSpeed > 0f) verticalSpeed = 0f;
            Velocity = cc.velocity;
        }

        public void AddRecoil(float degrees)
        {
            pitch = Mathf.Clamp(pitch - degrees, -85f, 85f);
        }

        public void Respawn(Vector3 position) => Respawn(position, Quaternion.identity);

        public void Respawn(Vector3 position, Quaternion facing)
        {
            cc.enabled = false;
            transform.SetPositionAndRotation(position, facing);
            cc.enabled = true;
            pitch = 0f;
            verticalSpeed = 0f;
            momentum = Vector3.zero;
            Cam.transform.localPosition = EyeOffset;
            Cam.transform.localRotation = Quaternion.identity;
            Hp.ResetHealth();
            Rifle.Refill();
        }
    }
}
