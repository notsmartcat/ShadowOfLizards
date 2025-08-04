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
        if (type == DamageType.Bite && source != null && source.owner != null && source.owner is Lizard liz && ShadowOfLizards.lizardstorage.TryGetValue(liz.abstractCreature, out ShadowOfLizards.LizardData data) && (data.transformation == "Electric" || data.transformation == "ElectricTransformation"))
        {
            self.Violence(source, directionAndMomentum, hitChunk, hitAppendage, DamageType.Electric, damage / 2, stunBonus / 2);

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(ShadowOfLizards.all + source.owner.ToString() + "'s Bite dealt additional Electric damage to " + self.ToString());
        }

        orig.Invoke(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
    }
}
