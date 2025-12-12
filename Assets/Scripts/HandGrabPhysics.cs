using UnityEngine;
using UnityEngine.InputSystem;

public class HandGrabPhysics : MonoBehaviour
{
    public InputActionProperty grabAction;
    public float breakForce = 2000f;
    public float breakTorque = 2000f;

    Rigidbody handRb;
    Rigidbody targetRb;
    ConfigurableJoint joint;

    GrabPoint currentPoint;

    void Awake()
    {
        handRb = GetComponent<Rigidbody>();
        handRb.isKinematic = true;

        Debug.Log("[Grab] Hand initialized");
    }

    void Update()
    {
        bool grabbing = grabAction.action.ReadValue<float>() > 0.5f;

        if (grabbing && joint == null)
            TryGrab();

        if (!grabbing && joint != null)
            Release();
    }

    void OnTriggerEnter(Collider other)
    {
        GrabPoint gp = other.GetComponent<GrabPoint>();
        if (gp != null && gp.IsAboveFloor())
        {
            if (currentPoint == null || gp.priority > currentPoint.priority)
            {
                if (currentPoint != null)
                    currentPoint.Hide();

                currentPoint = gp;
                currentPoint.Show();
            }
            return;
        }

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && rb.position.y >= TapFloorCalibrator.RealFloorY + 0.02f)
        {
            targetRb = rb;
        }
    }

    void OnTriggerExit(Collider other)
    {
        GrabPoint gp = other.GetComponent<GrabPoint>();
        if (gp != null && gp == currentPoint)
        {
            gp.Hide();
            currentPoint = null;
            return;
        }

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && rb == targetRb && joint == null)
            targetRb = null;
    }

    void TryGrab()
    {
        if (currentPoint != null)
        {
            Rigidbody rb = currentPoint.GetComponentInParent<Rigidbody>();
            if (rb == null)
                return;

            if (rb.position.y < TapFloorCalibrator.RealFloorY + 0.02f)
                return;

            CreateJoint(rb, currentPoint);
            currentPoint.Hide();
            currentPoint = null;
            return;
        }

        if (targetRb != null)
        {
            if (targetRb.position.y < TapFloorCalibrator.RealFloorY + 0.02f)
                return;

            CreateJoint(targetRb);
        }
    }

    void CreateJoint(Rigidbody rb, GrabPoint grabPoint = null)
    {
        joint = rb.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = handRb;
        
        // Snap to the grab point so the intended hand placement aligns with the controller
        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = grabPoint != null
            ? rb.transform.InverseTransformPoint(grabPoint.transform.position)
            : Vector3.zero;
        joint.connectedAnchor = Vector3.zero;

        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        joint.breakForce = breakForce;
        joint.breakTorque = breakTorque;
    }

    void Release()
    {
        if (joint != null)
            Destroy(joint);

        joint = null;
        targetRb = null;
    }
}