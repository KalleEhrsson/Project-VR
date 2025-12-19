using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class HandGrabPhysics : MonoBehaviour
{
    #region Inspector Stuff (Input And Tunables)
    [Tooltip("Input action used for grab. Must be assigned per input setup.")]
    [SerializeField]
    private InputActionProperty grabAction; // Kept serialized because actions come from scene-specific input assets

    [SerializeField]
    private float breakForce = 2000f;

    [SerializeField]
    private float breakTorque = 2000f;
    #endregion

    #region Cached Components (Self Setup)
    private Rigidbody handRb;
    #endregion

    #region Current State (What Is Happening Right Now)
    private Rigidbody targetRb;
    private ConfigurableJoint joint;

    private readonly List<GrabPoint> grabCandidates = new();
    private GrabPoint bestGrabPoint;
    #endregion

    #region Unity Lifetime (Awake Enable Disable Destroy)
    private void Awake()
    {
        handRb = GetComponent<Rigidbody>();
        if (handRb == null)
        {
            Debug.LogError($"HandGrabPhysics on {name} requires a Rigidbody. Disabling.");
            enabled = false;
            return;
        }

        handRb.isKinematic = true;

        if (grabAction.action == null)
            Debug.LogWarning($"HandGrabPhysics on {name} is missing grab input action.");

        Debug.Log("[Grab] Hand initialized");
    }
    #endregion

    #region Unity Messages (Update And Physics)
    private void Update()
    {
        bool grabbing = grabAction.action != null && grabAction.action.ReadValue<float>() > 0.5f;

        if (grabbing && joint == null)
            TryGrab();

        if (!grabbing && joint != null)
            Release();

        RefreshBestGrabPoint(showBubble: joint == null);
    }

    private void OnTriggerEnter(Collider other)
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

    private void OnTriggerExit(Collider other)
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
    #endregion

    #region Main Logic (What Actually Happens)
    private void TryGrab()
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

    private void CreateJoint(Rigidbody rb, GrabPoint grabPoint = null)
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

    private void Release()
    {
        if (joint != null)
            Destroy(joint);

        joint = null;
        targetRb = null;
        RefreshBestGrabPoint(showBubble: true);
    }

    private void TryAddCandidate(GrabPoint gp)
    {
        if (!gp.IsAboveFloor())
            return;

        if (gp.AttachedRigidbody == null)
            return;

        if (!grabCandidates.Contains(gp))
            grabCandidates.Add(gp);

        RefreshBestGrabPoint(showBubble: joint == null);
    }

    private void RemoveCandidate(GrabPoint gp)
    {
        if (grabCandidates.Remove(gp))
        {
            gp.Hide();
            RefreshBestGrabPoint(showBubble: joint == null);
        }
    }

    private void RefreshBestGrabPoint(bool showBubble)
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
                candidate.Priority > newBest.Priority ||
                (candidate.Priority == newBest.Priority && distance < bestDistance))
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

    private void AlignRigidBodyToHand(Rigidbody rb, GrabPoint gp)
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

    private void SnapToHand(Rigidbody rb)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.MovePosition(transform.position);
        rb.MoveRotation(transform.rotation);
    }

    private void ClearCandidates()
    {
        foreach (GrabPoint gp in grabCandidates)
            gp.Hide();

        grabCandidates.Clear();
        bestGrabPoint = null;
    }
    #endregion
}
