using Fisobs.Core;
using Fisobs.Items;
using Fisobs.Properties;
using Fisobs.Sandbox;
using static AbstractPhysicalObject;

namespace ShadowOfLizards;

sealed class LizCutHeadFisobs : Fisob
{
    public static readonly AbstractObjectType AbstrLizCutHead = new("LizCutHead", true);

    static readonly LizCutHeadProperties properties = new();

    public LizCutHeadFisobs() : base(AbstrLizCutHead)
    {
    }

    public override AbstractPhysicalObject Parse(World world, EntitySaveData saveData, SandboxUnlock unlock)
    {
        string[] array = saveData.CustomData.Split(';');

        if (array.Length < 33)
        {
            array = new string[33];
        }

        return new LizCutHeadAbstract(world, saveData.Pos, saveData.ID)
        {
            hue = float.TryParse(array[0], out float hue) ? hue : 0f,
            saturation = float.TryParse(array[1], out float sat) ? sat : 1f,

            scaleX = float.TryParse(array[2], out float sX) ? sX : 1f,
            scaleY = float.TryParse(array[3], out float sY) ? sY : 1f,

            LizType = string.IsNullOrEmpty(array[4]) ? "GreenLizard" : array[4],

            LizBodyColourR = float.TryParse(array[5], out float lbr) ? lbr : 0f,
            LizBodyColourB = float.TryParse(array[6], out float lbb) ? lbb : 0f,
            LizBodyColourG = float.TryParse(array[7], out float blg) ? blg : 1f,

            LizEffectColourR = float.TryParse(array[8], out float lr) ? lr : 0f,
            LizEffectColourG = float.TryParse(array[9], out float lg) ? lg : 1f,
            LizEffectColourB = float.TryParse(array[10], out float lb) ? lb : 0f,

            EyeRightColourR = float.TryParse(array[11], out float err) ? err : 0f,
            EyeRightColourB = float.TryParse(array[12], out float erb) ? erb : 0f,
            EyeRightColourG = float.TryParse(array[13], out float erg) ? erg : 1f,

            EyeLeftColourR = float.TryParse(array[14], out float elr) ? elr : 0f,
            EyeLeftColourG = float.TryParse(array[15], out float elg) ? elg : 1f,
            EyeLeftColourB = float.TryParse(array[16], out float elb) ? elb : 0f,

            HeadSprite0 = string.IsNullOrEmpty(array[17]) ? "LizardHead0.0" : array[17],
            HeadSprite1 = string.IsNullOrEmpty(array[18]) ? "LizardHead0.0" : array[18],
            HeadSprite2 = string.IsNullOrEmpty(array[19]) ? "LizardHead0.0" : array[19],
            HeadSprite3 = string.IsNullOrEmpty(array[20]) ? "LizardHead0.0" : array[20],
            HeadSprite4 = string.IsNullOrEmpty(array[21]) ? "LizardHead0.0" : array[21],
            HeadSprite5 = string.IsNullOrEmpty(array[22]) ? null : array[22],
            HeadSprite6 = string.IsNullOrEmpty(array[23]) ? null : array[23],

            blackSalamander = bool.TryParse(array[24], out bool bs) && bs,

            rad = float.TryParse(array[25], out float rad) ? rad : 1f,
            mass = float.TryParse(array[26], out float mass) ? mass : 1f,

            LizBloodColourR = float.TryParse(array[27], out float br) ? br : -1f,
            LizBloodColourG = float.TryParse(array[28], out float bg) ? bg : -1f,
            LizBloodColourB = float.TryParse(array[29], out float bb) ? bb : -1f,

            LizBreed = string.IsNullOrEmpty(array[30]) ? "GreenLizard" : array[30],

            canCamo = bool.TryParse(array[31], out bool cc) && cc,

            jawOpenAngle = float.TryParse(array[32], out float joa) ? joa : 100f,
            jawOpenMoveJawsApart = float.TryParse(array[33], out float jomja) ? jomja : 20f
        };
    }

    public override ItemProperties Properties(PhysicalObject forObject)
    {
        return properties;
    }
}
