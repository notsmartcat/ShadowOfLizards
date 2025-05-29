using RWCustom;
using System;
using UnityEngine;
namespace ShadowOfLizards;

internal static class ElectricTransformation
{
    public static void Apply()
    {
        On.MoreSlugcats.ElectricSpear.Electrocute += SpearRecharge;
        On.Player.EatMeatUpdate += PlayerEatElectricLizard;
        On.Lizard.Violence += ViolenceAttackerStun;
        On.Lizard.Update += LizardUpdate;
        On.LizardSpit.Update += ElectricSpitSpit;
        On.Lizard.Bite += BiteElectricCheck;
    }

    static void SpearRecharge(On.MoreSlugcats.ElectricSpear.orig_Electrocute orig, MoreSlugcats.ElectricSpear self, PhysicalObject otherObject)
    {
        orig.Invoke(self, otherObject);

        if (!ShadowOfOptions.electric_transformation.Value || otherObject == null || otherObject is not Lizard liz || !ShadowOfLizards.lizardstorage.TryGetValue(liz, out ShadowOfLizards.LizardData data) || data.liz["Electric"] != "True")
        {
            return;
        }

        if (self.abstractSpear.electricCharge == 0)
        {
            self.Recharge();

            if (data.liz["ElectricTransformation"] == "False")
            {
                data.liz["ElectricCharge"] = (float.Parse(data.liz["ElectricCharge"]) - 1f).ToString();
                if (float.Parse(data.liz["ElectricTransformation"]) < 0f)
                {
                    data.liz["Electric"] = "False";
                    data.liz["ElectricCharge"] = "0";
                }
            }
        }
        else
        {
            if (data.liz["ElectricTransformation"] == "False")
            {
                data.liz["ElectricCharge"] = (float.Parse(data.liz["ElectricCharge"]) + 1f).ToString();
            }
        }
    }

    static void PlayerEatElectricLizard(On.Player.orig_EatMeatUpdate orig, Player self, int graspIndex)
    {
        orig.Invoke(self, graspIndex);

        if (!ShadowOfOptions.electric_transformation.Value || self.grasps[graspIndex] == null || self.grasps[graspIndex].grabbed is not Lizard liz || !ShadowOfLizards.lizardstorage.TryGetValue(liz, out ShadowOfLizards.LizardData data))
        {
            return;
        }

        if (data.liz["ElectricTransformation"] == "True" && self.eatMeat > 40 && self.eatMeat % 15 == 3)
        {
            SoundID centipede_Shock = SoundID.Centipede_Shock;
            liz.room.PlaySound(centipede_Shock, liz.mainBodyChunk.pos);
            liz.room.AddObject(new Spark(liz.mainBodyChunk.pos, Custom.RNV() * Mathf.Lerp(4f, 14f, UnityEngine.Random.value), new Color(0.7f, 0.7f, 1f), null, 8, 14));
            liz.mainBodyChunk.vel += Custom.RNV() * 6f * UnityEngine.Random.value;
            liz.mainBodyChunk.pos += Custom.RNV() * 6f * UnityEngine.Random.value;
            for (int i = 0; i < self.bodyChunks.Length; i++)
            {
                BodyChunk obj = self.bodyChunks[i];
                obj.vel += Custom.RNV() * 6f * UnityEngine.Random.value;
                obj.pos += Custom.RNV() * 6f * UnityEngine.Random.value;
            }

            if (self != null)
            {
                self.Stun((int)Custom.LerpMap(self.TotalMass, 0f, liz.TotalMass * 2f, 300f, 30f));
                self.room.AddObject(new CreatureSpasmer(self, false, self.stun));
                self.LoseAllGrasps();
            }

            if (self.Submersion > 0f)
            {
                Room room = self.room;
                room.AddObject(new UnderwaterShock(room, self, liz.mainBodyChunk.pos, 14, Mathf.Lerp(ModManager.MMF ? 0f : 200f, 1200f, 1400f), 2.1f, self, new Color(0.7f, 0.7f, 1f)));
            }
        }
    }

    static void ViolenceAttackerStun(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
    {
        orig.Invoke(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);

        if (!ShadowOfOptions.electric_transformation.Value || !ShadowOfLizards.lizardstorage.TryGetValue(self, out ShadowOfLizards.LizardData data) || source == null || source.owner == null || data.liz["ElectricTransformation"] != "True")
        {
            return;
        }

        PhysicalObject owner = source.owner;
        self.room.PlaySound(SoundID.Centipede_Shock, hitChunk.pos);
        self.room.AddObject(new Spark(hitChunk.pos, Custom.RNV() * Mathf.Lerp(4f, 14f, UnityEngine.Random.value), new Color(0.7f, 0.7f, 1f), null, 8, 14));
        hitChunk.vel += Custom.RNV() * 6f * UnityEngine.Random.value;
        hitChunk.pos += Custom.RNV() * 6f * UnityEngine.Random.value;

        for (int i = 0; i < owner.bodyChunks.Length; i++)
        {
            BodyChunk obj = owner.bodyChunks[i];
            obj.vel += Custom.RNV() * 6f * UnityEngine.Random.value;
            obj.pos += Custom.RNV() * 6f * UnityEngine.Random.value;
        }

        if (owner is Creature creature)
        {
            creature.Stun((int)(Custom.LerpMap(owner.TotalMass, 0f, self.TotalMass * 2f, 300f, 30f) * (owner is Centipede ? 0.4f : 1f)));
            self.room.AddObject(new CreatureSpasmer(creature, false, creature.stun));
            creature.LoseAllGrasps();
        }

        if (owner.Submersion > 0f)
        {
            self.room.AddObject(new UnderwaterShock(self.room, self, hitChunk.pos, 14, Mathf.Lerp(ModManager.MMF ? 0f : 200f, 1200f, 1400f), 2.1f, self, new Color(0.7f, 0.7f, 1f)));
        }
    }

    static void LizardUpdate(On.Lizard.orig_Update orig, Lizard self, bool eu)
    {
        orig.Invoke(self, eu);

        if (ShadowOfOptions.electric_transformation.Value && self != null && ShadowOfLizards.lizardstorage.TryGetValue(self, out ShadowOfLizards.LizardData data) && data.liz["ElectricTransformation"] == "True" && UnityEngine.Random.value < 0.025f)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 val = Custom.RNV();
                Vector2 pos = self.bodyChunks[UnityEngine.Random.Range(0, 3)].pos;
                self.room.AddObject(new Spark(pos + val * UnityEngine.Random.value * 20f, val * Mathf.Lerp(4f, 10f, UnityEngine.Random.value), Color.white, null, 4, 18));
            }
        }
    }

    static void ElectricSpitSpit(On.LizardSpit.orig_Update orig, LizardSpit self, bool eu)
    {
        orig.Invoke(self, eu);

        if (!ShadowOfOptions.electric_transformation.Value || !ShadowOfOptions.electric_spit.Value || self == null || self.lizard == null || !ShadowOfLizards.lizardstorage.TryGetValue(self.lizard, out ShadowOfLizards.LizardData data) 
            || !(data.liz["ElectricTransformation"] == "True") || !(UnityEngine.Random.value < 0.1f))
        {
            return;
        }

        if (!ShadowOfLizards.ShockSpit.TryGetValue(self, out ShadowOfLizards.ElectricSpit data2))
        {
            ShadowOfLizards.ShockSpit.Add(self, new ShadowOfLizards.ElectricSpit());
        }
        else if (self.stickChunk != null && self.stickChunk.owner.room == self.room && Custom.DistLess(self.stickChunk.pos, self.pos, self.stickChunk.rad + 40f) && self.fallOff > 0 && !data2.Shocked)
        {
            data2.Shocked = true;
            PhysicalObject owner = self.stickChunk.owner;
            self.room.PlaySound(SoundID.Centipede_Shock, self.pos);
            self.room.AddObject(new Spark(self.pos, Custom.RNV() * Mathf.Lerp(4f, 14f, UnityEngine.Random.value), new Color(0.7f, 0.7f, 1f), null, 8, 14));
            self.vel += Custom.RNV() * 6f * UnityEngine.Random.value;
            self.pos += Custom.RNV() * 6f * UnityEngine.Random.value;

            for (int j = 0; j < owner.bodyChunks.Length; j++)
            {
                BodyChunk obj = owner.bodyChunks[j];
                obj.vel += Custom.RNV() * 6f * UnityEngine.Random.value;
                obj.pos += Custom.RNV() * 6f * UnityEngine.Random.value;
            }

            if (owner is Creature creature)
            {
                creature.Stun((int)(Custom.LerpMap(owner.TotalMass, 0f, self.massLeft * 2f, 300f, 30f) * (owner is Centipede ? 0.4f : 1f)));
                self.room.AddObject(new CreatureSpasmer(creature, false, creature.stun));
                creature.LoseAllGrasps();
            }

            if (owner.Submersion > 0f)
            {
                self.room.AddObject(new UnderwaterShock(self.room, self.lizard, self.pos, 14, Mathf.Lerp(ModManager.MMF ? 0f : 200f, 1200f, 1400f), 2.1f, self.lizard, new Color(0.7f, 0.7f, 1f)));
            }
        }

        if (!data2.Shocked)
        {
            for (int i = 0; i < 10; i++)
            {
                Vector2 val = Custom.RNV();
                self.room.AddObject(new Spark(self.pos + val * UnityEngine.Random.value * 20f, val * Mathf.Lerp(4f, 10f, UnityEngine.Random.value), Color.white, null, 4, 18));
            }
        }
    }

    static void BiteElectricCheck(On.Lizard.orig_Bite orig, Lizard self, BodyChunk chunk)
    {
        if (!ShadowOfOptions.electric_transformation.Value || self == null || !ShadowOfLizards.lizardstorage.TryGetValue(self, out ShadowOfLizards.LizardData data) || data.liz["ElectricTransformation"] != "True")
        {
            orig.Invoke(self, chunk);
            return;
        }

        self.room.PlaySound(SoundID.Centipede_Shock, self.mainBodyChunk.pos);

        if (self.graphicsModule != null)
        {
            int num = (int)Math.Min(Math.Max(self.lizardParams.biteDamage * 30f / 2f, (int)(self.lizardParams.biteDamage * 30f)), 25f);
            LizardGraphics graphicsModule = self.graphicsModule as LizardGraphics;
            graphicsModule.WhiteFlicker(num);
            self.room.AddObject(new Spark(self.mainBodyChunk.pos, Custom.RNV() * Mathf.Lerp(4f, 14f, UnityEngine.Random.value), new Color(0.7f, 0.7f, 1f), null, 8, 14));
        }

        if (chunk != null && chunk.owner != null && chunk.owner is Creature owner)
        {
            if (owner.TotalMass < self.TotalMass / 2)
            {
                if (ModManager.MSC && owner is Player player && (player.SlugCatClass == MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName.Artificer || player.SlugCatClass != null && player.SlugCatClass.value == "sproutcat"))
                {
                    player.PyroDeath();
                }
                else
                {
                    owner.Die();
                    Room val = self.room;
                    val.AddObject(new CreatureSpasmer(owner, true, (int)Mathf.Lerp(70f, 120f, self.mainBodyChunk.rad)));
                }
            }
            else
            {
                owner.Stun((int)(Custom.LerpMap(owner.TotalMass, 0f, self.TotalMass * 2f, 300f, 30f) * (owner is Centipede ? 0.4f : 1f)));

                Debug.Log(owner.stun);

                Room val = self.room;
                val.AddObject(new CreatureSpasmer(owner, false, owner.stun));
                owner.LoseAllGrasps();
                self.Stun(6);
            }
        }

        if (chunk != null && chunk.owner != null && chunk.owner.Submersion > 0f)
        {
            self.room.AddObject(new UnderwaterShock(self.room, self, self.mainBodyChunk.pos, 14, Mathf.Lerp(ModManager.MMF ? 0f : 200f, 1200f, self.mainBodyChunk.rad), 0.2f + 1.9f * self.mainBodyChunk.rad, self, new Color(0.7f, 0.7f, 1f)));
        }
        orig.Invoke(self, chunk);
    }
}
