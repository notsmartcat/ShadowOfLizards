using Fisobs.Properties;
using static Player;

namespace ShadowOfLizards;

sealed class LizCutEyeProperties : ItemProperties
{
    public override void Throwable(Player player, ref bool throwable)
    {
        throwable = true;
    }

    public override void Grabability(Player player, ref ObjectGrabability grabability)
    {
        grabability = ObjectGrabability.OneHand;
    }
}
