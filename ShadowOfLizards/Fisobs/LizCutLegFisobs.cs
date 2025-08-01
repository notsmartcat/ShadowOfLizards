using Fisobs.Core;
using Fisobs.Items;
using Fisobs.Properties;
using Fisobs.Sandbox;
using static AbstractPhysicalObject;

namespace ShadowOfLizards;

sealed class LizCutLegFisobs : Fisob
{
    public static readonly AbstractObjectType AbstrLizardCutLeg = new("LizCutLeg", true);

    static readonly LizCutLegProperties properties = new();

    public LizCutLegFisobs() : base(AbstrLizardCutLeg)
    {
    }

    public override AbstractPhysicalObject Parse(World world, EntitySaveData saveData, SandboxUnlock unlock)
    {
        string[] array = saveData.CustomData.Split(';');

        if (array.Length < 18)
        {
            array = new string[18];
        }

        return new LizCutLegAbstract(world, saveData.Pos, saveData.ID)
        {
            hue = float.TryParse(array[0], out float hue) ? hue : 0f,
            saturation = float.TryParse(array[1], out float sat) ? sat : 1f,

            scaleX = float.TryParse(array[2], out float sX) ? sX : 1f,
            scaleY = float.TryParse(array[3], out float sY) ? sY : 1f,

            LizType = (string.IsNullOrEmpty(array[4]) ? "GreenLizard" : array[4]),

            LizBodyColourR = float.TryParse(array[5], out float lbr) ? lbr : 0f,
            LizBodyColourB = float.TryParse(array[6], out float lbb) ? lbb : 0f,
            LizBodyColourG = float.TryParse(array[7], out float lbg) ? lbg : 1f,

            LizEffectColourR = float.TryParse(array[8], out float lr) ? lr : 0f,
            LizEffectColourG = float.TryParse(array[9], out float lg) ? lg : 1f,
            LizEffectColourB = float.TryParse(array[10], out float lb) ? lb : 0f,

            LizBloodColourR = float.TryParse(array[11], out float br) ? br : -1f,
            LizBloodColourG = float.TryParse(array[12], out float bg) ? bg : -1f,
            LizBloodColourB = float.TryParse(array[13], out float bb) ? bb : -1f,

            LizSpriteName = string.IsNullOrEmpty(array[14]) ? "LizardArm_14" : array[14],
            LizColourSpriteName = string.IsNullOrEmpty(array[15]) ? "LizardArmColor_14" : array[15],

            LizBreed = string.IsNullOrEmpty(array[16]) ? "GreenLizard" : array[16],

            blackSalamander = bool.TryParse(array[17], out bool bs) && bs,

            canCamo = bool.TryParse(array[18], out bool cc) && cc
        };
    }

    public override ItemProperties Properties(PhysicalObject forObject)
    {
        return properties;
    }
}
