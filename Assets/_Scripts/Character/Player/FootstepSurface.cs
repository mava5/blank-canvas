
using UnityEngine;


public enum SurfaceType
{
    Stone,
    Grass,
    Wood,
}

public class FootstepSurface : MonoBehaviour
{
    public SurfaceType SurfaceType = SurfaceType.Stone;
}
