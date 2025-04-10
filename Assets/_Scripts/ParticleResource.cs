

using UnityEngine;

[CreateAssetMenu(fileName = "ParticleResource", menuName = "ParticleResource", order = 0)]
public class ParticleResource : ScriptableObject {
    public Color color;
    public MovementType movementType;
}
public enum MovementType {
    Idle,
    Sand,
    Water
}