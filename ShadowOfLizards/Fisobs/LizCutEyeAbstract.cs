using Fisobs.Core;
using System;
using System.Runtime.CompilerServices;

namespace ShadowOfLizards;

sealed class LizCutEyeAbstract : AbstractPhysicalObject
{
    public float EyeColourR;
    public float EyeColourG;
    public float EyeColourB;

    public float LizBloodColourR;
    public float LizBloodColourG;
    public float LizBloodColourB;

    public float LizColourR;
    public float LizColourG;
    public float LizColourB;

    public string LizBreed;

    public LizCutEyeAbstract(World world, WorldCoordinate pos, EntityID ID) : base(world, LizCutEyeFisobs.AbstrLizCutEye, null, pos, ID)
    {
    }

    public override void Realize()
    {
        base.Realize();
        realizedObject ??= new LizCutEye(this);
    }

    public override string ToString()
    {
        return this.SaveToString($"{EyeColourR};{EyeColourG};{EyeColourB};{LizColourR};{LizColourG};{LizColourB};{LizBloodColourR};{LizBloodColourG};{LizBloodColourB};{LizBreed}");
    }
}
