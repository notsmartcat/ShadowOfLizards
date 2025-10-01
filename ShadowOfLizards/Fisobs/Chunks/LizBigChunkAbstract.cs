using Fisobs.Core;

namespace ShadowOfLizards;

internal sealed class LizBigChunkAbstract : AbstractPhysicalObject
{
    public float hue;
    public float saturation;

    public float rad;
    public float mass;

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

    public bool blackSalamander;

    public bool canCamo;

    public int insideVariant;
    public int outsideVariant;

    public int insideRotation;
    public int outsideRotation;

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
        return this.SaveToString($"{hue};{saturation};{rad};{mass};{breed};{bodyColourR};{bodyColourG};{bodyColourB};{effectColourR};{effectColourG};{effectColourB};{bloodColourR};{bloodColourG};{bloodColourB};{blackSalamander};{canCamo};{insideVariant};{outsideVariant};{insideRotation};{outsideRotation}");
    }
}
