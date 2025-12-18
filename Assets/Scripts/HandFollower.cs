using UnityEngine;

public class HandFollower : MonoBehaviour
{
    #region Inspector Stuff (Scene References)
    [SerializeField]
    private Transform target; // Kept in the Inspector because each follower targets a specific tracked transform
    #endregion

    #region Main Logic (What Actually Happens)
    private void LateUpdate()
    {
        transform.position = target.position;
        transform.rotation = target.rotation;
    }
    #endregion
}
