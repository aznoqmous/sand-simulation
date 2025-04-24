

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
    public float shareWetness;

    public bool isWet;
    public bool burns;
    public bool corrosive;

    [Header("LifeTime")]
    public float lifeTime;
    public ParticleTypeEnum onDeathEmit;
    public float onDeathSpawnChance;
}

public struct ParticleType 
{
    public Color color;
    public int movementType;
    public float dispersion;
    public int isSolid;
    
    public int isFlammable;
    public int isAbrasive;
    public float shareWetness;

    public int isWet;
    public int burns;
    public int corrosive;

    public float lifeTime;
    public int onDeathEmit;
    public float onDeathSpawnChance;
}

public enum MovementType {
    Idle,
    Sand,
    Water,
    Gas,
    Fire,
    Wood,
    Root,
    Stem
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
    Earth,
    Root,
    Stem
}