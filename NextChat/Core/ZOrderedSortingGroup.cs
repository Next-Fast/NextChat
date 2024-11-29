using UnityEngine;
using UnityEngine.Rendering;

namespace NextChat.Core;

public class ZOrderedSortingGroup : MonoBehaviour
{
    private SortingGroup? group;
    private Renderer? renderer;
    public int ConsiderParents;
    public void SetConsiderParentsTo(Transform parent)
    {
        var num = 0;
        var t = transform;
        while(!(t == parent || t == null))
        {
            num++;
            t = t.parent;
        }
        ConsiderParents = num;
    }
    public void Start()
    {
        if(!gameObject.TryGetComponent<Renderer>(out renderer)) 
            group = gameObject.AddComponent<SortingGroup>();
    }

    private const float rate = 20000f;
    private const int baseValue = 5;

    public void Update()
    {
        var z = transform.localPosition.z;
        var t = transform;
        for (var i = 0; i < ConsiderParents; i++)
        {
            t = t.parent;
            z += t.localPosition.z;
        }
        var layer = baseValue - (int)(rate * z);
        if (group is not null)group.sortingOrder = layer;
        if(renderer is not null) renderer.sortingOrder = layer;
    }
}