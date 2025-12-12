using System.Collections.Generic;

using static ShadowOfLizards.ShadowOfLizards;

namespace ShadowOfLizards;

internal class LizardAIHooks
{
    public static void Apply()
    {
        On.LizardAI.ctor += NewLizardAI;
    }

    static void NewLizardAI(On.LizardAI.orig_ctor orig, LizardAI self, AbstractCreature creature, World world)
    {
        orig(self, creature, world);

        if (!ShadowOfOptions.deafen.Value || !lizardstorage.TryGetValue(creature, out LizardData data) || !data.liz.ContainsKey("EarRight"))
        {
            return;
        }

        bool flag5 = data.liz["EarRight"] == "Deaf";
        bool flag6 = data.liz["EarLeft"] == "Deaf";

        if (ShadowOfOptions.blind.Value && data.liz.ContainsKey("EyeRight"))
        {
            bool flag = data.liz["EyeRight"] == "Blind" || data.liz["EyeRight"] == "BlindScar" || data.liz["EyeRight"] == "BlindScar2" || data.liz["EyeRight"] == "Cut";
            bool flag2 = data.liz["EyeLeft"] == "Blind" || data.liz["EyeLeft"] == "BlindScar" || data.liz["EyeLeft"] == "BlindScar2" || data.liz["EyeLeft"] == "Cut";

            if (flag && flag2)
            {
                List<AIModule> modules = self.modules;

                bool superHearing = false;

                for (int j = 0; j < modules.Count; j++)
                {
                    if (modules[j] is SuperHearing)
                    {
                        superHearing = true;

                        break;
                    }
                }

                if (!superHearing && (!flag5 || !flag6))
                {
                    self.modules.Add(new SuperHearing(self, self.tracker, 350f));
                }
            }
        }

        if (flag5 && flag6)
        {
            List<AIModule> modules = self.modules;

            for (int i = 0; i < modules.Count; i++)
            {
                if (modules[i] is SuperHearing)
                {
                    (modules[i] as SuperHearing).superHearingSkill = 0f;

                    break;
                }
            }

        }
        else if (flag5 ^ flag6)
        {
            for (int i = 0; i < self.modules.Count; i++)
            {
                if (self.modules[i] is SuperHearing)
                {
                    (self.modules[i] as SuperHearing).superHearingSkill = (self.modules[i] as SuperHearing).superHearingSkill / 2;

                    break;
                }
            }
        }
    }
}
