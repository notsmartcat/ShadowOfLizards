using Fisobs.Core;

namespace ShadowOfLizards;

internal sealed class LizCutLegAbstract : AbstractPhysicalObject
{
    public float hue;
    public float saturation;

    public float scaleX;
    public float scaleY;

    public string LizType;

    public float LizBaseColourR;
    public float LizBaseColourG;
    public float LizBaseColourB;

    public float LizColourR;
    public float LizColourG;
    public float LizColourB;

    public float LizBloodColourR;
    public float LizBloodColourG;
    public float LizBloodColourB;

    public string LizSpriteName;
    public string LizColourSpriteName;

    public string LizBreed;

    public LizCutLegAbstract(World world, WorldCoordinate pos, EntityID ID) : base(world, LizCutLegFisobs.AbstrLizardCutLeg, null, pos, ID)
    {
    }

    public override void Realize()
    {
        base.Realize();
        realizedObject ??= new LizCutLeg(this);
    }

    public override string ToString()
    {
        return this.SaveToString($"{hue};{saturation};{scaleX};{scaleY};{LizType};{LizBaseColourR};{LizBaseColourG};{LizBaseColourB};{LizColourR};{LizColourG};{LizColourB};{LizBloodColourR};{LizBloodColourG};{LizBloodColourB};{LizSpriteName};{LizColourSpriteName};{LizBreed}");
    }
}
