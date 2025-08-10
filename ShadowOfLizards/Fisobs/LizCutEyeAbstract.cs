using Fisobs.Core;

namespace ShadowOfLizards;

sealed class LizCutEyeAbstract : AbstractPhysicalObject
{
    public float BodyColourR;
    public float BodyColourG;
    public float BodyColourB;

    public float BloodColourR;
    public float BloodColourG;
    public float BloodColourB;

    public float EyeColourR;
    public float EyeColourG;
    public float EyeColourB;

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
        return this.SaveToString($"{BodyColourR};{BodyColourG};{BodyColourB};{BloodColourR};{BloodColourG};{BloodColourB};{BloodColourR};{BloodColourG};{BloodColourB};{LizBreed}");
    }
}
