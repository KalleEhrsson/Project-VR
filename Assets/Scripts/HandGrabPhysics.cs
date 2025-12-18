using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class HandGrabPhysics : MonoBehaviour
{
    public InputActionProperty grabAction;
    public float breakForce = 2000f;
    public float breakTorque = 2000f;

    Rigidbody handRb;
    Rigidbody targetRb;
    ConfigurableJoint joint;

    readonly List<GrabPoint> grabCandidates = new();
    GrabPoint bestGrabPoint;

    void Awake()
    {
        handRb = GetComponent<Rigidbody>();
        handRb.isKinematic = true;

        Debug.Log("[Grab] Hand initialized");
    }

    void Update()
    {
        bool grabbing = grabAction.action != null && grabAction.action.ReadValue<float>() > 0.5f;

        if (grabbing && joint == null)
            TryGrab();

        if (!grabbing && joint != null)
            Release();

        RefreshBestGrabPoint(showBubble: joint == null);
    }

    void OnTriggerEnter(Collider other)
    {
        GrabPoint gp = other.GetComponent<GrabPoint>();
        if (gp != null)
        {
            TryAddCandidate(gp);
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
        if (gp != null)
        {
            RemoveCandidate(gp);
            return;
        }

        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && rb == targetRb && joint == null)
            targetRb = null;
    }

    void TryGrab()
    {
        if (bestGrabPoint != null)
        {
            Rigidbody rb = bestGrabPoint.AttachedRigidbody;
            if (rb == null)
            {
                RemoveCandidate(bestGrabPoint);
                return;
            }

            if (rb.position.y < TapFloorCalibrator.RealFloorY + 0.02f || !bestGrabPoint.IsAboveFloor())
                return;

            AlignRigidBodyToHand(rb, bestGrabPoint);
            CreateJoint(rb, bestGrabPoint);
            bestGrabPoint.Hide();
            ClearCandidates();
            return;
        }

        if (targetRb != null)
        {
            if (targetRb.position.y < TapFloorCalibrator.RealFloorY + 0.02f)
                return;

            SnapToHand(targetRb);
            CreateJoint(targetRb);
            targetRb = null;
        }
    }

    void CreateJoint(Rigidbody rb, GrabPoint grabPoint = null)
    {
        joint = rb.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = handRb;

        joint.autoConfigureConnectedAnchor = false;
        joint.anchor = grabPoint != null
            ? grabPoint.GetLocalAttachPoint(rb.transform)
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
        RefreshBestGrabPoint(showBubble: true);
    }

    void TryAddCandidate(GrabPoint gp)
    {
        if (!gp.IsAboveFloor())
            return;

        if (gp.AttachedRigidbody == null)
            return;

        if (!grabCandidates.Contains(gp))
            grabCandidates.Add(gp);

        RefreshBestGrabPoint(showBubble: joint == null);
    }

    void RemoveCandidate(GrabPoint gp)
    {
        if (grabCandidates.Remove(gp))
        {
            gp.Hide();
            RefreshBestGrabPoint(showBubble: joint == null);
        }
    }

    void RefreshBestGrabPoint(bool showBubble)
    {
        for (int i = grabCandidates.Count - 1; i >= 0; i--)
        {
            GrabPoint candidate = grabCandidates[i];
            if (candidate == null || candidate.AttachedRigidbody == null || !candidate.IsAboveFloor())
                grabCandidates.RemoveAt(i);
        }

        GrabPoint newBest = null;
        float bestDistance = 0f;

        foreach (GrabPoint candidate in grabCandidates)
        {
            float distance = (candidate.GetAttachPose().position - transform.position).sqrMagnitude;

            if (newBest == null ||
                candidate.priority > newBest.priority ||
                (candidate.priority == newBest.priority && distance < bestDistance))
            {
                newBest = candidate;
                bestDistance = distance;
            }
        }

        if (bestGrabPoint != null && bestGrabPoint != newBest)
            bestGrabPoint.Hide();

        bestGrabPoint = newBest;

        if (showBubble && bestGrabPoint != null)
            bestGrabPoint.Show();
        else if (!showBubble && bestGrabPoint != null)
            bestGrabPoint.Hide();
    }

    void AlignRigidBodyToHand(Rigidbody rb, GrabPoint gp)
    {
        Pose attachPose = gp.GetAttachPose();
        Pose handPose = new Pose(transform.position, transform.rotation);

        Vector3 localAnchor = gp.GetLocalAttachPoint(rb.transform);
        Quaternion localRotation = gp.GetLocalAttachRotation(rb.transform);

        Quaternion desiredRotation = handPose.rotation * Quaternion.Inverse(localRotation);
        Vector3 desiredPosition = handPose.position - desiredRotation * localAnchor;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.MovePosition(desiredPosition);
        rb.MoveRotation(desiredRotation);
    }

    void SnapToHand(Rigidbody rb)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.MovePosition(transform.position);
        rb.MoveRotation(transform.rotation);
    }

    void ClearCandidates()
    {
        foreach (GrabPoint gp in grabCandidates)
            gp.Hide();

        grabCandidates.Clear();
        bestGrabPoint = null;
    }
}
