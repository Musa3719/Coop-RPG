using UnityEngine;

public class LookAt : MonoBehaviour
{
    [SerializeField] private Transform _lookAtTransform;
    [SerializeField] private bool _isLookingToCamera;
    private void LateUpdate()
    {
        if (_isLookingToCamera)
            transform.parent.LookAt(GameManager._Instance._MainCamera.transform);
        if (_lookAtTransform != null)
            transform.parent.LookAt(_lookAtTransform);
    }
}
