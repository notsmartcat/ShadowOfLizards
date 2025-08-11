using Fisobs.Core;

namespace ShadowOfLizards;

sealed class LizCutHeadAbstract : AbstractPhysicalObject
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

    public float eyeRightColourR;
    public float eyeRightColourG;
    public float eyeRightColourB;

    public float eyeLeftColourR;
    public float eyeLeftColourG;
    public float eyeLeftColourB;

    public string headSprite0;
    public string headSprite1;
    public string headSprite2;
    public string headSprite3;
    public string headSprite4;
    public string headSprite5;
    public string headSprite6;

    public bool blackSalamander;

    public float rad;
    public float mass;

    public bool canCamo;

    public float jawOpenAngle;
    public float jawOpenMoveJawsApart;

    public LizCutHeadAbstract(World world, WorldCoordinate pos, EntityID ID) : base(world, LizCutHeadFisobs.AbstrLizCutHead, null, pos, ID)
    {
    }

    public override void Realize()
    {
        base.Realize();
        realizedObject ??= new LizCutHead(this);
    }

    public override string ToString()
    {
        return this.SaveToString($"{hue};{saturation};{scaleX};{scaleY};{breed};{bodyColourR};{bodyColourG};{bodyColourB};{effectColourR};{effectColourG};{effectColourB};{eyeRightColourR};{eyeRightColourG};{eyeRightColourB};{eyeLeftColourR};{eyeLeftColourG};{eyeLeftColourB};{headSprite0};{headSprite1};{headSprite2};{headSprite3};{headSprite4};{headSprite5};{headSprite6};{blackSalamander};{rad};{mass};{bloodColourR};{bloodColourG};{bloodColourB};{canCamo};{jawOpenAngle};{jawOpenMoveJawsApart}");
    }
}
