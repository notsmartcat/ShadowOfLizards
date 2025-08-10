using Fisobs.Core;

namespace ShadowOfLizards;

internal sealed class LizCutLegAbstract : AbstractPhysicalObject
{
    public float hue;
    public float saturation;

    public float scaleX;
    public float scaleY;

    public string LizBreed;

    public float LizBodyColourR;
    public float LizBodyColourG;
    public float LizBodyColourB;

    public float LizEffectColourR;
    public float LizEffectColourG;
    public float LizEffectColourB;

    public float LizBloodColourR;
    public float LizBloodColourG;
    public float LizBloodColourB;

    public string LizSpriteName;
    public string LizColourSpriteName;

    public bool blackSalamander;

    public bool canCamo;

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
        return this.SaveToString($"{hue};{saturation};{scaleX};{scaleY};{LizBreed};{LizBodyColourR};{LizBodyColourG};{LizBodyColourB};{LizEffectColourR};{LizEffectColourG};{LizEffectColourB};{LizBloodColourR};{LizBloodColourG};{LizBloodColourB};{LizSpriteName};{LizColourSpriteName};{blackSalamander};{canCamo}");
    }
}
