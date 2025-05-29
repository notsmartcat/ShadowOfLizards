using UnityEngine;
using static Creature;
using static PhysicalObject.Appendage;

namespace ShadowOfLizards;

public class ViolenceTypeCheck
{
    public static void Apply()
    {
        On.Creature.Violence += ViolenceDamageTypeCheck;
    }

    static void ViolenceDamageTypeCheck(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, Pos hitAppendage, DamageType type, float damage, float stunBonus)
    {
        if (source != null && source.owner != null && source.owner is Lizard liz && ShadowOfLizards.lizardstorage.TryGetValue(liz, out ShadowOfLizards.LizardData value) && value.liz["Electric"] == "True" && self is not Centipede)
        {
            type = DamageType.Electric;
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(source.owner.ToString() + "'s damage converted to Electric on " + self.ToString() + " in creature Damage");
        }

        orig.Invoke(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }
}
