using UnityEngine;

public class HandFollower : MonoBehaviour
{
    #region Inspector Stuff (Scene References)
    [Header("Target Reference")]
    [Tooltip("Must be assigned to the tracked transform this follower should mirror.")]
    [SerializeField]
    private Transform target; // Kept in the Inspector because each follower targets a specific tracked transform
    #endregion

    #region Unity Lifetime (Awake Enable Disable Destroy)
    private void Awake()
    {
        if (target == null)
        {
            Debug.LogError($"HandFollower on {name} has no target assigned. Disabling to avoid null references.");
            enabled = false;
        }
    }
    #endregion

    #region Main Logic (What Actually Happens)
    private void LateUpdate()
    {
        if (target == null)
            return;

        transform.position = target.position;
        transform.rotation = target.rotation;
    }
    #endregion
}
