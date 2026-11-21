
using UnityEngine;


public class MaterialBrush : ColoredBrush
{
    public Material Material = null;

    protected internal override Material FinalMaterial => Material;

    public MaterialBrush(Material material)
    { Material = material; }
}
