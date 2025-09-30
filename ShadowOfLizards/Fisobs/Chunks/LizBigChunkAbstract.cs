using Fisobs.Core;

namespace ShadowOfLizards;

internal sealed class LizBigChunkAbstract : AbstractPhysicalObject
{
    public float hue;
    public float saturation;

    public float rad;

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

    public int spriteVariant;

    public bool blackSalamander;

    public bool canCamo;

    public LizBigChunkAbstract(World world, WorldCoordinate pos, EntityID ID) : base(world, LizBigChunkFisobs.AbstrLizBigChunk, null, pos, ID)
    {
    }

    public override void Realize()
    {
        base.Realize();
        realizedObject ??= new LizBigChunk(this);
    }

    public override string ToString()
    {
        return this.SaveToString($"{hue};{saturation};{rad};{breed};{bodyColourR};{bodyColourG};{bodyColourB};{effectColourR};{effectColourG};{effectColourB};{bloodColourR};{bloodColourG};{bloodColourB};{spriteVariant};{blackSalamander};{canCamo}");
    }
}
