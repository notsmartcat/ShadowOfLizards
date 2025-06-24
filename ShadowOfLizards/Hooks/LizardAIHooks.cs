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

        if (!lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data) || !data.liz.TryGetValue("CanSpit", out string _))
        {
            return;
        }

        try
        {
            if (data.liz["CanSpit"] == "True" && self.redSpitAI == null)
            {
                self.redSpitAI = new LizardAI.LizardSpitTracker(self);
                self.AddModule(self.redSpitAI);

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " added Spit ability");
            }
            else if (data.liz["CanSpit"] == "False" && self.redSpitAI != null)
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

