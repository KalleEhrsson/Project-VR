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

        Debug.Log($"[Grab] Hand initialized. HandRB = {handRb}");
    }

    void Update()
    {
        bool grabbing = grabAction.action.ReadValue<float>() > 0.5f;

        if (grabbing && joint == null)
        {
            Debug.Log("[Grab] Grip pressed. Attempting grab...");
            TryGrab();
        }

        if (!grabbing && joint != null)
        {
            Debug.Log("[Grab] Grip released. Releasing object.");
            Release();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        GrabPoint gp = other.GetComponent<GrabPoint>();
        if (gp != null)
        {
            Debug.Log($"[Grab] Entered GrabPoint {gp.name} (priority {gp.priority})");

            if (currentPoint == null || gp.priority > currentPoint.priority)
            {
                if (currentPoint != null)
                {
                    Debug.Log($"[Grab] Replacing previous GrabPoint {currentPoint.name}");
                    currentPoint.Hide();
                }

                currentPoint = gp;
                currentPoint.Show();
            }
            return;
        }

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null)
        {
            Debug.Log($"[Grab] Entered RB candidate {rb.name}");
            targetRb = rb;
        }
    }

    void OnTriggerExit(Collider other)
    {
        GrabPoint gp = other.GetComponent<GrabPoint>();
        if (gp != null && gp == currentPoint)
        {
            Debug.Log($"[Grab] Exited GrabPoint {gp.name}");
            currentPoint.Hide();
            currentPoint = null;
            return;
        }

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && rb == targetRb && joint == null)
        {
            Debug.Log($"[Grab] Exited RB candidate {rb.name}");
            targetRb = null;
        }
    }

    void TryGrab()
    {
        // Priority grab: GrabPoint → rigidbody
        if (currentPoint != null)
        {
            Rigidbody rb = currentPoint.GetComponentInParent<Rigidbody>();

            Debug.Log($"[Grab] Trying GrabPoint {currentPoint.name}, RB = {rb}");

            if (rb == null)
            {
                Debug.LogError("[Grab] ERROR: GrabPoint has NO Rigidbody parent!");
                return;
            }

            CreateJoint(rb);
            Debug.Log($"[Grab] SUCCESS: Grabbed via GrabPoint {currentPoint.name}");

            currentPoint.Hide();
            currentPoint = null;
            return;
        }

        // Fallback: target rigidbody
        if (targetRb != null)
        {
            Debug.Log($"[Grab] Trying fallback RB grab: {targetRb.name}");
            CreateJoint(targetRb);
            return;
        }

        Debug.LogWarning("[Grab] No GrabPoint or RB available to grab.");
    }

    void CreateJoint(Rigidbody rb)
    {
        Debug.Log($"[Grab] Creating joint between HAND and {rb.name}");

        joint = rb.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = handRb;

        if (rb.isKinematic)
            Debug.LogWarning($"[Grab] WARNING: Target RB {rb.name} is KINEMATIC. Joint will NOT move it.");

        // Lock position + rotation
        joint.xMotion = ConfigurableJointMotion.Locked;
        joint.yMotion = ConfigurableJointMotion.Locked;
        joint.zMotion = ConfigurableJointMotion.Locked;

        joint.angularXMotion = ConfigurableJointMotion.Locked;
        joint.angularYMotion = ConfigurableJointMotion.Locked;
        joint.angularZMotion = ConfigurableJointMotion.Locked;

        joint.breakForce = breakForce;
        joint.breakTorque = breakTorque;

        JointDrive drive = new JointDrive();
        drive.positionSpring = 4000f;
        drive.positionDamper = 50f;
        drive.maximumForce = Mathf.Infinity;

        joint.xDrive = drive;
        joint.yDrive = drive;
        joint.zDrive = drive;

        Debug.Log($"[Grab] Joint successfully created on {rb.name}");
    }

    void Release()
    {
        if (joint == null)
        {
            Debug.LogWarning("[Grab] Release called but no joint exists.");
            return;
        }

        Debug.Log($"[Grab] Destroying joint on {joint.gameObject.name}");
        Destroy(joint);
        joint = null;
        targetRb = null;
    }
}