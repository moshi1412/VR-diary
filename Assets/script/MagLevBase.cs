using System.Collections.Generic;
using UnityEngine;

public class MagLevBase : MonoBehaviour
{
    // Static registry so hoverers can auto-discover bases at runtime.
    public static readonly HashSet<MagLevBase> Instances = new HashSet<MagLevBase>();

    private void OnEnable() { Instances.Add(this); }
    private void OnDisable() { Instances.Remove(this); }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Optional: draw a short normal line to visualize base up.
        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
        Vector3 p = transform.position;
        Vector3 n = transform.up.normalized;
        Gizmos.DrawLine(p, p + n * 0.5f);
    }
#endif
}
