using System;
using UnityEngine;
using static ShadowOfLizards.ShadowOfLizards;

namespace ShadowOfLizards;

internal class LizardAIHooks
{
    public static void Apply()
    {
        On.LizardAI.ctor += NewLizardAI;
        On.LizardAI.Update += LizardAIUpdate;
    }

    static void NewLizardAI(On.LizardAI.orig_ctor orig, LizardAI self, AbstractCreature creature, World world)
    {
        orig.Invoke(self, creature, world);

        if (!lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
        {
            return;
        }

        try
        {
            if ((ShadowOfOptions.swim_ability.Value || ShadowOfOptions.water_breather.Value) && self.lurkTracker != null && (creature.creatureTemplate.type == CreatureTemplate.Type.Salamander || (ModManager.DLCShared && creature.creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.EelLizard)))
            {
                if (data.liz.TryGetValue("CanSwim", out string CanSwim) && CanSwim != "True" || data.liz.TryGetValue("WaterBreather", out string WaterBreather) && WaterBreather != "True")
                {
                    self.modules.Remove(self.lurkTracker);
                    self.lurkTracker = null;

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " removed Lurk ability from aquatic Lizard because it cannot last underwater");
                }
            }

            if (!data.liz.TryGetValue("CanSpit", out string canSpit))
            {
                return;
            }

            if (canSpit == "True" && self.redSpitAI == null)
            {
                self.redSpitAI = new LizardAI.LizardSpitTracker(self);
                self.AddModule(self.redSpitAI);

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " added Spit ability");
            }
            else if (canSpit == "False" && self.redSpitAI != null)
            {
                self.modules.Remove(self.redSpitAI);
                self.redSpitAI = null;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " removed Spit ability");
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static void LizardAIUpdate(On.LizardAI.orig_Update orig, LizardAI self)
    {
        orig.Invoke(self);

        if (self != null && self.redSpitAI != null && self.redSpitAI.spitting && self.lizard != null)
        {
            self.lizard.EnterAnimation(Lizard.Animation.Spit, false);
        }
    }
}