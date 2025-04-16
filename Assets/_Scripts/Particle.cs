using UnityEngine;

public struct Particle {
    public Vector2 position;
    public Vector2 realPosition;
    public Vector2 direction;
    public float speed;
    public int particleType;
    public float idleTime;
}

public struct ParticleType 
{
    public Color color;
    public int movementType;
    public float dispersion;
    public int isSolid;
}
