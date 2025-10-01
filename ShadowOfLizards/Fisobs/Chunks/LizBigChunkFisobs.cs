using Fisobs.Core;
using Fisobs.Items;
using Fisobs.Properties;
using Fisobs.Sandbox;
using UnityEngine;

namespace ShadowOfLizards;

sealed class LizBigChunkFisobs : Fisob
{
    public static readonly AbstractPhysicalObject.AbstractObjectType AbstrLizBigChunk = new("LizBigChunk", true);

    static readonly LizBigChunkProperties properties = new();

    public LizBigChunkFisobs() : base(AbstrLizBigChunk)
    {
        Icon = new LizBigChunkIcon();
    }

    public override AbstractPhysicalObject Parse(World world, EntitySaveData saveData, SandboxUnlock unlock)
    {
        string[] array = saveData.CustomData.Split(';');

        if (array.Length < 20)
        {
            array = new string[20];
        }

        return new LizBigChunkAbstract(world, saveData.Pos, saveData.ID)
        {
            hue = float.TryParse(array[0], out float hue) ? hue : 0f,
            saturation = float.TryParse(array[1], out float sat) ? sat : 1f,

            rad = float.TryParse(array[2], out float rad) ? rad : 1f,
            mass = float.TryParse(array[3], out float mass) ? mass : 1f,

            breed = (string.IsNullOrEmpty(array[4]) ? "GreenLizard" : array[4]),

            bodyColourR = float.TryParse(array[5], out float lbr) ? lbr : 0f,
            bodyColourB = float.TryParse(array[6], out float lbb) ? lbb : 0f,
            bodyColourG = float.TryParse(array[7], out float lbg) ? lbg : 1f,

            effectColourR = float.TryParse(array[8], out float lr) ? lr : 0f,
            effectColourG = float.TryParse(array[9], out float lg) ? lg : 1f,
            effectColourB = float.TryParse(array[10], out float lb) ? lb : 0f,

            bloodColourR = float.TryParse(array[11], out float br) ? br : -1f,
            bloodColourG = float.TryParse(array[12], out float bg) ? bg : -1f,
            bloodColourB = float.TryParse(array[13], out float bb) ? bb : -1f,

            blackSalamander = bool.TryParse(array[14], out bool bs) && bs,

            canCamo = bool.TryParse(array[15], out bool cc) && cc,

            insideVariant = int.TryParse(array[16], out int iv) ? iv : 0,
            outsideVariant = int.TryParse(array[17], out int ov) ? ov : 0,

            insideRotation = int.TryParse(array[18], out int ir) ? ir : 0,
            outsideRotation = int.TryParse(array[19], out int or) ? or : 0
        };
    }

    public override ItemProperties Properties(PhysicalObject forObject)
    {
        return properties;
    }
}

sealed class LizBigChunkIcon : Icon
{
    public override int Data(AbstractPhysicalObject apo)
    {
        return apo is LizBigChunkAbstract ? 1 : 0;
    }

    public override Color SpriteColor(int data)
    {
        return RWCustom.Custom.HSL2RGB(data / 1000f, 0.65f, 0.4f);
    }

    public override string SpriteName(int data)
    {
        return "Kill_Leech";
    }
}