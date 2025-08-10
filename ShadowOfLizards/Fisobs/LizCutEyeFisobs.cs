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
            BodyColourR = float.TryParse(array[0], out float efr) ? efr : 1f,
            BodyColourG = float.TryParse(array[1], out float efg) ? efg : 1f,
            BodyColourB = float.TryParse(array[2], out float efb) ? efb : 0f,

            BloodColourR = float.TryParse(array[3], out float br) ? br : -1f,
            BloodColourG = float.TryParse(array[4], out float bg) ? bg : -1f,
            BloodColourB = float.TryParse(array[5], out float bb) ? bb : -1f,

            EyeColourR = float.TryParse(array[6], out float er) ? er : 0f,
            EyeColourG = float.TryParse(array[7], out float eg) ? eg : 0f,
            EyeColourB = float.TryParse(array[8], out float eb) ? eb : 1f,

            LizBreed = string.IsNullOrEmpty(array[9]) ? "GreenLizard" : array[9]
        };
    }

    public override ItemProperties Properties(PhysicalObject forObject)
    {
        return properties;
    }
}
