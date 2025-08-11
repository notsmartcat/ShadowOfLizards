using Fisobs.Core;

namespace ShadowOfLizards;

sealed class LizCutEyeAbstract : AbstractPhysicalObject
{
    public float bodyColourR;
    public float bodyColourG;
    public float bodyColourB;

    public float bloodColourR;
    public float bloodColourG;
    public float bloodColourB;

    public float eyeColourR;
    public float eyeColourG;
    public float eyeColourB;

    public string breed;

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
        return this.SaveToString($"{bodyColourR};{bodyColourG};{bodyColourB};{bloodColourR};{bloodColourG};{bloodColourB};{bloodColourR};{bloodColourG};{bloodColourB};{breed}");
    }
}
