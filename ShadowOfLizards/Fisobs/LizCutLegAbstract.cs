using Fisobs.Core;

namespace ShadowOfLizards;

internal sealed class LizCutLegAbstract : AbstractPhysicalObject
{
    public float hue;
    public float saturation;

    public float scaleX;
    public float scaleY;

    public string breed;

    public float bodyColourR;
    public float bodyColourG;
    public float bodyColourB;

    public float effectColourR;
    public float effectColourG;
    public float effectColourB;

    public float bloodColourR;
    public float bloodColourG;
    public float bloodColourB;

    public string spriteName;
    public string colourSpriteName;

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
        return this.SaveToString($"{hue};{saturation};{scaleX};{scaleY};{breed};{bodyColourR};{bodyColourG};{bodyColourB};{effectColourR};{effectColourG};{effectColourB};{bloodColourR};{bloodColourG};{bloodColourB};{spriteName};{colourSpriteName};{blackSalamander};{canCamo}");
    }
}
