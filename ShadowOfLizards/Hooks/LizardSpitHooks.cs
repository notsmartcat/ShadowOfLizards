using UnityEngine;
using RWCustom;
using static RoomCamera;
using static ShadowOfLizards.ShadowOfLizards;

namespace ShadowOfLizards;

internal class LizardSpitHooks
{
    public static void Apply()
    {
        On.LizardSpit.DrawSprites += SpitDraw;
        On.LizardSpit.Update += SpitUpdate;
    }

    static void SpitDraw(On.LizardSpit.orig_DrawSprites orig, LizardSpit self, SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
    {
        orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);

        if (self.lizard == null || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
        {
            return;
        }
        else if (ShadowOfOptions.spider_transformation.Value && ShadowOfOptions.spider_spit.Value && data.transformation == "SpiderTransformation")
        {
            TransformationSpider.SpiderSpitDraw(sLeaser);
            return;
        }
        else if (ShadowOfOptions.melted_transformation.Value && ShadowOfOptions.melted_spit.Value && data.liz.TryGetValue("MeltedR", out _) && (data.transformation == "Melted" || data.transformation == "MeltedTransformation"))
        {
            TransformationMelted.MeltedSpitDraw(self, sLeaser, data);
            return;
        }
        else if (ShadowOfOptions.electric_transformation.Value && ShadowOfOptions.electric_spit.Value && shockSpit.TryGetValue(self, out ElectricSpit electricData) && data.transformation == "ElectricTransformation")
        {
            TransformationElectric.ElectricSpitDraw(self, sLeaser, electricData);
            return;
        }
    }

    static void SpitUpdate(On.LizardSpit.orig_Update orig, LizardSpit self, bool eu)
    {
        if (self.lizard == null || !lizardstorage.TryGetValue(self.lizard.abstractCreature, out LizardData data))
        {
            orig.Invoke(self, eu);
            return;
        }
        else if (ShadowOfOptions.spider_transformation.Value && ShadowOfOptions.spider_spit.Value && data.transformation == "SpiderTransformation" && data.liz.TryGetValue("SpiderNumber", out _))
        {
            TransformationSpider.SpiderSpitUpdate(self, data);
            return;
        }

        orig.Invoke(self, eu);

        if (ShadowOfOptions.melted_transformation.Value && ShadowOfOptions.melted_spit.Value && (data.transformation == "Melted" || data.transformation == "MeltedTransformation") && self.stickChunk != null && self.stickChunk.owner != null && self.stickChunk.owner.room == self.room && Custom.DistLess(self.stickChunk.pos, self.pos, self.stickChunk.rad + 40f) && self.fallOff > 0)
        {
            TransformationMelted.MeltedSpitUpdate(self);
            return;
        }
        else if (ShadowOfOptions.electric_transformation.Value && ShadowOfOptions.electric_spit.Value && shockSpit.TryGetValue(self, out ElectricSpit electricData) && data.transformation == "ElectricTransformation")
        {
            TransformationElectric.ElectricSpitUpdate(self, electricData);
            return;
        }
    }
}