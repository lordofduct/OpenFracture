using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(Rigidbody))]
public class Prefracture : MonoBehaviour
{
    public TriggerOptions triggerOptions;
    public FractureOptions fractureOptions;
    public CallbackOptions callbackOptions;
    public PrefractureOptions prefractureOptions;

    /// <summary>
    /// Collector object that stores the produced fragments
    /// </summary>
    private GameObject fragmentRoot;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (this.transform.parent != null)
        {
            // When an object is fractured, the fragments are created as children of that object's parent.
            // Because of this, they inherit the parent transform. If the parent transform is not scaled
            // the same in all axes, the fragments will not be rendered correctly.
            var scale = this.transform.parent.localScale;
            if ((scale.x != scale.y) || (scale.x != scale.z) || (scale.y != scale.z))
            {
                Debug.LogWarning($"Warning: Parent transform of fractured object must be uniformly scaled in all axes or fragments will not render correctly.", this.transform);
            }
        }
    }
#endif

}