using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IgnoreRotation : MonoBehaviour
{
    private void LateUpdate()
    {
        transform.rotation = Quaternion.identity;
    }

    private void OnDrawGizmos()
    {
#if UNITY_EDITOR
        runInEditMode = true;
#endif
    }
}
