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

        bool earRightDeaf = data.liz["EarRight"] == "Deaf";
        bool earLeftDeaf = data.liz["EarLeft"] == "Deaf";

        if (ShadowOfOptions.blind.Value && data.liz.ContainsKey("EyeRight"))
        {
            bool eyeRightBlind = IsEyeBlind(data.liz["EyeRight"]);
            bool eyeLeftBlind = IsEyeBlind(data.liz["EyeLeft"]);

            if (eyeRightBlind && eyeLeftBlind)
            {
                bool hasSuperHearing = false;

                for (int j = 0; j < self.modules.Count; j++)
                {
                    if (self.modules[j] is SuperHearing)
                    {
                        hasSuperHearing = true;
                        break;
                    }
                }

                if (!hasSuperHearing && (!earRightDeaf || !earLeftDeaf))
                {
                    self.modules.Add(new SuperHearing(self, self.tracker, 350f));
                }
            }
        }

        if (earRightDeaf && earLeftDeaf)
        {
            for (int i = 0; i < self.modules.Count; i++)
            {
                if (self.modules[i] is SuperHearing superHearing)
                {
                    superHearing.superHearingSkill = 0f;

                    break;
                }
            }

        }
        else if (earRightDeaf ^ earLeftDeaf)
        {
            for (int i = 0; i < self.modules.Count; i++)
            {
                if (self.modules[i] is SuperHearing superHearing)
                {
                    superHearing.superHearingSkill /= 2;

                    break;
                }
            }
        }
    }
}