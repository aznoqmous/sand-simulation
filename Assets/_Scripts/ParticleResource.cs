

using UnityEngine;

[CreateAssetMenu(fileName = "ParticleResource", menuName = "ParticleResource", order = 0)]
public class ParticleResource : ScriptableObject {
    public Color color;
    public MovementType movementType;
    public float dispersion;
    public bool isSolid;

    [Header("Effects")]
    public bool isFlammable;
    public bool isAbrasive;
    public bool burns;
    public bool corrosive;

    [Header("LifeTime")]
    public float lifeTime;
    public ParticleTypeEnum onDeathEmit;
    public float onDeathSpawnChance;
}

public enum MovementType {
    Idle,
    Sand,
    Water,
    Gas,
    Fire
}
public enum ParticleTypeEnum {
    Empty,
    Stone,
    Sand,
    Water,
    Wood,
    Fire,
    Gas,
    Acid,
}