using UnityEngine;

namespace UnderwaterOptimization
{
    public static class UnderwaterInput
    {
        private static Transform xrRay;
        private static bool xrTriggerHeld;
        private static bool xrTriggerPrev;
        private static bool xrTriggerDown;
        private static bool xrTriggerUp;

        public static bool IsVr
        {
            get { return xrRay != null; }
        }

        public static void SetXrRay(Transform ray)
        {
            xrRay = ray;
        }

        public static void UpdateXrTrigger(bool held)
        {
            xrTriggerDown = held && !xrTriggerPrev;
            xrTriggerUp = !held && xrTriggerPrev;
            xrTriggerPrev = held;
            xrTriggerHeld = held;
        }

        private static Camera cachedCamera;

        private static Camera PlayerCamera
        {
            get
            {
                if (cachedCamera != null && cachedCamera.isActiveAndEnabled)
                    return cachedCamera;

                DebugPlayerMotor motor = Object.FindObjectOfType<DebugPlayerMotor>();
                if (motor != null && motor.cameraTransform != null)
                {
                    Camera motorCamera = motor.cameraTransform.GetComponent<Camera>();
                    if (motorCamera != null)
                    {
                        cachedCamera = motorCamera;
                        return cachedCamera;
                    }
                }

                Camera main = Camera.main;
                if (main != null)
                {
                    cachedCamera = main;
                    return cachedCamera;
                }

                Camera[] cameras = Camera.allCameras;
                if (cameras.Length > 0)
                {
                    cachedCamera = cameras[0];
                    return cachedCamera;
                }

                return null;
            }
        }

        public static Vector3 RayOrigin
        {
            get
            {
                if (xrRay != null)
                    return xrRay.position;

                Camera cam = PlayerCamera;
                return cam != null ? cam.transform.position : Vector3.zero;
            }
        }

        public static Vector3 RayDirection
        {
            get
            {
                if (xrRay != null)
                    return xrRay.forward;

                Camera cam = PlayerCamera;
                return cam != null ? cam.transform.forward : Vector3.forward;
            }
        }

        public static string InteractName
        {
            get { return IsVr ? "扳机" : "E"; }
        }

        public static string FireName
        {
            get { return IsVr ? "扳机" : "左键"; }
        }

        public static bool InteractDown
        {
            get { return IsVr ? xrTriggerDown : Input.GetKeyDown(KeyCode.E); }
        }

        public static bool InteractHeld
        {
            get { return IsVr ? xrTriggerHeld : Input.GetKey(KeyCode.E); }
        }

        public static bool FireDown
        {
            get { return IsVr ? xrTriggerDown : Input.GetMouseButtonDown(0); }
        }

        public static bool FireHeld
        {
            get { return IsVr ? xrTriggerHeld : Input.GetMouseButton(0); }
        }

        public static bool FireUp
        {
            get { return IsVr ? xrTriggerUp : Input.GetMouseButtonUp(0); }
        }

        public static bool RestartDown
        {
            get { return Input.GetKeyDown(KeyCode.R); }
        }
    }
}