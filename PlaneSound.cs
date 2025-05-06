using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlaneSoundType
{
    Stone,
    Dirt,
    Grass,
    Metal,
    Concrete,
    Wood,
    Fabric,
    Water,
    Ice,
    Snowy
}
public class PlaneSound : MonoBehaviour
{
    [SerializeField]
    private PlaneSoundType _planeSoundType;

    public PlaneSoundType PlaneSoundType => _planeSoundType;
}
