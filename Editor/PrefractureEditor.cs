using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Prefracture))]
[CanEditMultipleObjects]
public class PrefractureEditor : Editor
{

    // Empty editor required for custom property drawers to work properly

    [MenuItem("CONTEXT/Prefracture/Do The Thing")]
    private static void PrefractureContextCallback(MenuCommand menuCommand)
    {
        if (menuCommand.context is Prefracture p)
        {
            ComputeFracture(p);
        }
        else if (menuCommand.context is GameObject go && go.GetComponent<Prefracture>())
        {
            ComputeFracture(go.GetComponent<Prefracture>());
        }
    }

    /// <summary>
    /// Compute the fracture and create the fragments
    /// </summary>
    /// <returns></returns>
    public static void ComputeFracture(Prefracture targ)
    {
        // This method should only be called from the editor during design time
        if (!targ || !Application.isEditor || Application.isPlaying) return;

        var mesh = targ.GetComponent<MeshFilter>().sharedMesh;

        if (mesh != null)
        {
            // Create a game object to contain the fragments
            var fragmentRoot = new GameObject($"{targ.name}Fragments");
            fragmentRoot.transform.SetParent(targ.transform.parent);

            // Each fragment will handle its own scale
            fragmentRoot.transform.position = targ.transform.position;
            fragmentRoot.transform.rotation = targ.transform.rotation;
            fragmentRoot.transform.localScale = Vector3.one;

            var fragmentTemplate = CreateFragmentTemplate(targ).gameObject;

            Fragmenter.Fracture(targ.gameObject,
                                targ.fractureOptions,
                                fragmentTemplate,
                                fragmentRoot.transform,
                                targ.prefractureOptions.saveFragmentsToDisk,
                                targ.prefractureOptions.saveLocation);

            // Done with template, destroy it. Since we're in editor, use DestroyImmediate
            GameObject.DestroyImmediate(fragmentTemplate);

            // Deactivate the original object
            targ.gameObject.SetActive(false);

            // Fire the completion callback
            if (targ.callbackOptions.onCompleted != null)
            {
                targ.callbackOptions.onCompleted.Invoke();
            }
        }
    }

    private static System.Func<Prefracture, UnfreezeFragment> _createFragmentTemplate;
    public static System.Func<Prefracture, UnfreezeFragment> CreateFragmentTemplate
    {
        get => _createFragmentTemplate ?? (_createFragmentTemplate = DefaultCreateFragmentTemplate<UnfreezeFragment>);
        set => _createFragmentTemplate = value;
    }

    /// <summary>
    /// Creates a template object which each fragment will derive from
    /// </summary>
    /// <returns></returns>
    public static UnfreezeFragment DefaultCreateFragmentTemplate<T>(Prefracture targ) where T : UnfreezeFragment
    {
        // If pre-fracturing, make the fragments children of this object so they can easily be unfrozen later.
        // Otherwise, parent to this object's parent
        GameObject obj = new GameObject();
        obj.name = "Fragment";
        obj.tag = targ.tag;

        // Update mesh to the new sliced mesh
        obj.AddComponent<MeshFilter>();

        // Add renderer. Default material goes in slot 1, cut material in slot 2
        var meshRenderer = obj.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterials = new Material[2] {
            targ.GetComponent<MeshRenderer>().sharedMaterial,
            targ.fractureOptions.insideMaterial
        };

        // Copy collider properties to fragment
        var thisCollider = targ.GetComponent<Collider>();
        var fragmentCollider = obj.AddComponent<MeshCollider>();
        fragmentCollider.convex = true;
        fragmentCollider.sharedMaterial = thisCollider.sharedMaterial;
        fragmentCollider.isTrigger = thisCollider.isTrigger;

        // Copy rigid body properties to fragment
        var rigidBody = obj.AddComponent<Rigidbody>();
        // When pre-fracturing, freeze the rigid body so the fragments don't all crash to the ground when the scene starts.
        rigidBody.constraints = RigidbodyConstraints.FreezeAll;
        rigidBody.drag = targ.GetComponent<Rigidbody>().drag;
        rigidBody.angularDrag = targ.GetComponent<Rigidbody>().angularDrag;
        rigidBody.useGravity = targ.GetComponent<Rigidbody>().useGravity;

        var unfreeze = obj.AddComponent<T>();
        unfreeze.unfreezeAll = targ.prefractureOptions.unfreezeAll;
        unfreeze.triggerOptions = targ.triggerOptions;
        unfreeze.onFractureCompleted = targ.callbackOptions.onCompleted;

        return unfreeze;
    }

}
