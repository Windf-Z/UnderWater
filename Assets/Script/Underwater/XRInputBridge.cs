using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

namespace UnderwaterOptimization
{
    public class XRInputBridge : MonoBehaviour
    {
        public Transform rightHandRay;

        private void Start()
        {
            if (rightHandRay != null)
            {
                UnderwaterInput.SetXrRay(rightHandRay);
                return;
            }

            Transform found = FindRightHand();
            if (found != null)
            {
                rightHandRay = found;
                UnderwaterInput.SetXrRay(found);
            }
            else
            {
                Debug.LogWarning("[Underwater] 没找到右手射线，退化成相机射线。");
            }
        }

        private Transform FindRightHand()
        {
            XRRayInteractor[] rays = Object.FindObjectsOfType<XRRayInteractor>();
            foreach (XRRayInteractor ray in rays)
            {
                if (IsRightHand(ray.transform))
                    return ray.transform;
            }

            if (rays.Length > 0)
                return rays[0].transform;

            XRController[] controllers = Object.FindObjectsOfType<XRController>();
            foreach (XRController controller in controllers)
            {
                if (IsRightHand(controller.transform))
                    return controller.transform;
            }

            return null;
        }

        private static bool IsRightHand(Transform t)
        {
            while (t != null)
            {
                if (t.name.ToLower().Contains("right"))
                    return true;
                t = t.parent;
            }

            return false;
        }

        private void Update()
        {
            bool held = false;

            InputDevice device = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
            if (device.isValid)
                device.TryGetFeatureValue(CommonUsages.triggerButton, out held);

            UnderwaterInput.UpdateXrTrigger(held);
        }
    }
}