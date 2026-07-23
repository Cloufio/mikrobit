using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D))]
public class CrashableObject : MonoBehaviour
{
    [Tooltip("Objects with this component remove time when the boat crashes into them.")]
    [SerializeField] private bool damagesBoat = true;

    public bool DamagesBoat => damagesBoat;

    private void Reset()
    {
        Collider2D collisionShape = GetComponent<Collider2D>();
        if (collisionShape != null)
        {
            collisionShape.isTrigger = false;
        }
    }
}
