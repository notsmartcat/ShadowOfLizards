using Fisobs.Core;

namespace ShadowOfLizards;

sealed class LizCutHeadAbstract : AbstractPhysicalObject
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

    public float EyeRightColourR;
    public float EyeRightColourG;
    public float EyeRightColourB;

    public float EyeLeftColourR;
    public float EyeLeftColourG;
    public float EyeLeftColourB;

    public string HeadSprite0;
    public string HeadSprite1;
    public string HeadSprite2;
    public string HeadSprite3;
    public string HeadSprite4;
    public string HeadSprite5;
    public string HeadSprite6;

    public bool blackSalamander;

    public float rad;

    public string LizBreed;

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
        return this.SaveToString($"{hue};{saturation};{scaleX};{scaleY};{LizType};{LizBaseColourR};{LizBaseColourG};{LizBaseColourB};{LizColourR};{LizColourG};{LizColourB};{EyeRightColourR};{EyeRightColourG};{EyeRightColourB};{EyeLeftColourR};{EyeLeftColourG};{EyeLeftColourB};{HeadSprite0};{HeadSprite1};{HeadSprite2};{HeadSprite3};{HeadSprite4};{HeadSprite5};{HeadSprite6};{blackSalamander};{rad};{LizBloodColourR};{LizBloodColourG};{LizBloodColourB};{LizBreed}");
    }
}
