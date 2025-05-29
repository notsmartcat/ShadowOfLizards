using Fisobs.Core;
using Fisobs.Items;
using Fisobs.Properties;
using Fisobs.Sandbox;
using static AbstractPhysicalObject;

namespace ShadowOfLizards;

sealed class LizCutEyeFisobs : Fisob
{
    public static readonly AbstractObjectType AbstrLizCutEye = new("LizCutEye", true);

    static readonly LizCutEyeProperties properties = new();

    public LizCutEyeFisobs() : base(AbstrLizCutEye)
    {
    }

    public override AbstractPhysicalObject Parse(World world, EntitySaveData saveData, SandboxUnlock unlock)
    {
        string[] array = saveData.CustomData.Split(';');

        if (array.Length < 9)
        {
            array = new string[9];
        }

        return new LizCutEyeAbstract(world, saveData.Pos, saveData.ID)
        {
            EyeColourR = float.TryParse(array[0], out float er) ? er : 1f,
            EyeColourB = float.TryParse(array[1], out float eb) ? eb : 0f,
            EyeColourG = float.TryParse(array[2], out float eg) ? eg : 1f,

            LizBloodColourR = float.TryParse(array[3], out float lbr) ? lbr : -1f,
            LizBloodColourB = float.TryParse(array[4], out float lbb) ? lbb : -1f,
            LizBloodColourG = float.TryParse(array[5], out float lbg) ? lbg : -1f,

            LizColourR = float.TryParse(array[6], out float lr) ? lr : 0f,
            LizColourB = float.TryParse(array[7], out float lb) ? lb : 0f,
            LizColourG = float.TryParse(array[8], out float lg) ? lg : 1f,

            LizBreed = string.IsNullOrEmpty(array[9]) ? "GreenLizard" : array[9]
        };
    }

    public override ItemProperties Properties(PhysicalObject forObject)
    {
        return properties;
    }
}
