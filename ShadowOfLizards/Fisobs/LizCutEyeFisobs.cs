using Fisobs.Core;
using Fisobs.Items;
using Fisobs.Properties;
using Fisobs.Sandbox;
using UnityEngine;

namespace ShadowOfLizards;

sealed class LizCutEyeFisobs : Fisob
{
    public static readonly AbstractPhysicalObject.AbstractObjectType AbstrLizCutEye = new("LizCutEye", true);

    static readonly LizCutEyeProperties properties = new();

    public LizCutEyeFisobs() : base(AbstrLizCutEye)
    {
        Icon = new LizCutEyeIcon();
    }

    public override AbstractPhysicalObject Parse(World world, EntitySaveData saveData, SandboxUnlock unlock)
    {
        string[] array = saveData.CustomData.Split(';');

        if (array.Length < 10)
        {
            array = new string[10];
        }

        return new LizCutEyeAbstract(world, saveData.Pos, saveData.ID)
        {
            bodyColourR = float.TryParse(array[0], out float efr) ? efr : 1f,
            bodyColourG = float.TryParse(array[1], out float efg) ? efg : 1f,
            bodyColourB = float.TryParse(array[2], out float efb) ? efb : 0f,

            bloodColourR = float.TryParse(array[3], out float br) ? br : -1f,
            bloodColourG = float.TryParse(array[4], out float bg) ? bg : -1f,
            bloodColourB = float.TryParse(array[5], out float bb) ? bb : -1f,

            eyeColourR = float.TryParse(array[6], out float er) ? er : 0f,
            eyeColourG = float.TryParse(array[7], out float eg) ? eg : 0f,
            eyeColourB = float.TryParse(array[8], out float eb) ? eb : 1f,

            breed = string.IsNullOrEmpty(array[9]) ? "GreenLizard" : array[9]
        };
    }

    public override ItemProperties Properties(PhysicalObject forObject)
    {
        return properties;
    }
}

sealed class LizCutEyeIcon : Icon
{
    public override int Data(AbstractPhysicalObject apo)
    {
        return apo is LizCutEyeAbstract ? 1 : 0;
    }

    public override Color SpriteColor(int data)
    {
        return RWCustom.Custom.HSL2RGB(data / 1000f, 0.65f, 0.4f);
    }

    public override string SpriteName(int data)
    {
        return "Symbol_EggBugEgg";
    }
}