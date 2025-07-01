using System;
using System.Collections.Generic;
using UnityEngine;
using RWCustom;
using MoreSlugcats;
using static ShadowOfLizards.ShadowOfLizards;
using System.Linq;

namespace ShadowOfLizards;

internal class LizardHooks
{
    public static void Apply()
    {
        On.AbstractCreature.ctor += NewAbstractLizard;

        On.Lizard.ctor += NewLizard;
        On.Lizard.Violence += LizardViolence;
        On.Lizard.Bite += LizardBite;
        On.Lizard.Update += LizardUpdate;
        On.Lizard.HitHeadShield += LizardHitHeadShield;

        On.Creature.Die += CreatureDie;

        On.SaveState.AbstractCreatureToStringStoryWorld_AbstractCreature_WorldCoordinate += SaveStateSaveAbstractCreature;
    }

    static void NewAbstractLizard(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
    {
        orig(self, world, creatureTemplate, realizedCreature, pos, ID);

        try
        {
            #region Code
            if (self == null || self.state == null || self.state.unrecognizedSaveStrings == null || creatureTemplate.TopAncestor().type != CreatureTemplate.Type.LizardTemplate)
            {
                return;
            }

            if (!lizardstorage.TryGetValue(self, out LizardData data))
            {
                lizardstorage.Add(self, new LizardData());
                lizardstorage.TryGetValue(self, out LizardData dat);
                data = dat;
            }

            data.liz = new();

            bool isStorySession = self.world.game.IsStorySession;
            int cycleNumber = isStorySession ? self.world.game.GetStorySession.saveState.cycleNumber : -1;

            Dictionary<string, string> savedData = self.state.unrecognizedSaveStrings;

            //First Time Creating Check
            if (!savedData.TryGetValue("ShadowOfBeheaded", out string beheaded))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + "First time creating Abstract " + self);

                data.Beheaded = false;

                if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                {
                    data.cheatDeathChance = UnityEngine.Random.Range(0, 101) + ShadowOfOptions.cheat_death_chance.Value;

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " got " + data.cheatDeathChance + " Chance to Cheat Death due to Dynamic Death Chance being On.");
                }
                else
                {
                    data.cheatDeathChance = ShadowOfOptions.cheat_death_chance.Value;

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " got a flat " + data.cheatDeathChance + " Chance to Cheat Death due to Dynamic Death Chance being Off.");
                }

                for (int i = 0; i < (ModManager.DLCShared && creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.SpitLizard ? 6 : 4); i++)
                {
                    data.ArmState.Add("Normal");
                }
            }
            else //Loads info from the Lizard
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + "Not the first time creating Abstract " + self + " Loading values for Abstract");

                data.Beheaded = beheaded == "True";

                string lizKeyTemp = "";
                string lizTemp = "";
                for (int i = 0; i < savedData["ShadowOfLiz"].Length; i++)
                {
                    char letter = savedData["ShadowOfLiz"][i];

                    if (letter.ToString() == "=")
                    {
                        lizKeyTemp = lizTemp;
                        lizTemp = "";
                    }
                    else if (letter.ToString() == ";")
                    {
                        data.liz.Add(lizKeyTemp, lizTemp);
                        lizKeyTemp = "";
                        lizTemp = "";
                    }
                    else
                    {
                        lizTemp += letter;
                    }
                }

                string ArmStateTemp = "";
                for (int i = 0; i < savedData["ShadowOfArmState"].Length; i++)
                {
                    char letter = savedData["ShadowOfArmState"][i];

                    if (letter.ToString() == ";")
                    {
                        data.ArmState.Add(ArmStateTemp);
                        ArmStateTemp = "";
                    }
                    else
                    {
                        ArmStateTemp += letter;
                    }
                }

                data.transformation = savedData["ShadowOfTransformation"];
                data.transformationTimer = int.Parse(savedData["ShadowOfTransformationTimer"]);

                data.cheatDeathChance = int.Parse(savedData["ShadowOfCheatDeathChance"]);

                data.lizardUpdatedCycle = int.Parse(savedData["ShadowOfLizardUpdatedCycle"]);

                string chunkTemp = "";
                for (int i = 0; i < savedData["ShadowOfAvailableBodychunks"].Length; i++)
                {
                    char letter = savedData["ShadowOfAvailableBodychunks"][i];

                    if (letter.ToString() == ";")
                    {
                        data.ArmState.Add(chunkTemp);
                        chunkTemp = "";
                    }
                    else
                    {
                        chunkTemp += letter;
                    }
                }

                if (ShadowOfOptions.debug_logs.Value)
                {
                    Debug.Log(all + self + " beheaded = " + data.Beheaded);
                    Debug.Log(all + self + " lizDictionary = " + data.liz);

                    Debug.Log(all + self + " bodyChunks = " + data.availableBodychunks);

                    Debug.Log(all + self + " transformation = " + data.transformation);
                    Debug.Log(all + self + " transformationTimer = " + data.transformationTimer);

                    Debug.Log(all + self + " armState = " + data.ArmState);

                    Debug.Log(all + self + " updatedCycle = " + data.lizardUpdatedCycle);

                    Debug.Log(all + self + " cheatDeathChance = " + data.cheatDeathChance);
                }

                //Add Head Back if next cycle
                if (ShadowOfOptions.decapitation.Value)
                {
                    if (!data.liz.TryGetValue("BeheadedCycle", out _))
                    {
                        data.liz["BeheadedCycle"] = "-1";
                    }
                    else if (data.Beheaded == true && isStorySession && data.liz["BeheadedCycle"] != self.world.game.GetStorySession.saveState.cycleNumber.ToString())
                    {
                        data.Beheaded = false;

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " gained back it's head");
                    }
                }
                else if (data.liz.TryGetValue("BeheadedCycle", out _))
                {
                    data.liz.Remove("BeheadedCycle");
                }
            }

            bool firstTime = data.lizardUpdatedCycle != (isStorySession ? cycleNumber : -1);


            LizardBreedParams breedParameters = self.creatureTemplate.breedParameters as LizardBreedParams;

            #region Local Hooks
            string abstractAll = all + "Abstract " + self.ToString();

            Abilities();

            Immunities();

            Physical();

            Transformations();
            #endregion

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "Finished creating Abstract " + self);

            data.lizardUpdatedCycle = isStorySession ? cycleNumber : -1;
            #endregion

            void Abilities()
            {
                //Tongue: Set inside Lizard
                if (ShadowOfOptions.tongue_stuff.Value && firstTime && (data.liz.TryGetValue("Tongue", out _) || ModManager.MSC && self.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.TrainLizard && !data.liz.TryGetValue("Tongue", out _)))
                {
                    bool tongue = data.liz.TryGetValue("Tongue", out string Tongue) && Tongue != "Null";

                    if (!data.liz.TryGetValue("Tongue", out _) && ModManager.MSC && self.creatureTemplate.type == MoreSlugcatsEnums.CreatureTemplateType.TrainLizard)
                    {
                        data.liz["Tongue"] = "Null";
                    }
                    else if (tongue && data.liz["Tongue"] == "get")
                    {
                        int num = UnityEngine.Random.Range(0, 7);
                        data.liz["Tongue"] = (validTongues[num].Contains(self.creatureTemplate.type.ToString()) && UnityEngine.Random.value < 0.5) ? self.creatureTemplate.type.ToString() : validTongues[num];

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(abstractAll + " got new " + data.liz["Tongue"] + " Tongue");
                    }

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(abstractAll + (tongue ? " has a " + data.liz["Tongue"] + " Tongue" : " does not have a Tongue"));

                    if (data.liz["Tongue"] == self.creatureTemplate.type.ToString())
                    {
                        data.liz.Remove("Tongue");
                    }
                }
                else if (!ShadowOfOptions.tongue_stuff.Value && data.liz.TryGetValue("Tongue", out _))
                {
                    data.liz.Remove("Tongue");
                }

                //Jump: Set inside Lizard
                if (ShadowOfOptions.jump_stuff.Value && firstTime && data.liz.TryGetValue("CanJump", out string CanJump))
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        AbilityLog(CanJump == "True", "Jump");
                }
                else if (!ShadowOfOptions.jump_stuff.Value && data.liz.TryGetValue("CanJump", out _))
                {
                    data.liz.Remove("CanJump");
                }

                //Can Swim: Set inside Lizard
                if (ShadowOfOptions.jump_stuff.Value && firstTime && data.liz.TryGetValue("CanSwim", out string CanSwim))
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        AbilityLog(CanSwim == "True", "Swim");
                }
                else if (false && data.liz.TryGetValue("CanSwim", out _))
                {
                    data.liz.Remove("CanSwim");
                }

                //CanClimbWall: Set inside Lizard
                if (true && firstTime && data.liz.TryGetValue("CanClimbWall", out string CanClimbWall))
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        AbilityLog(CanClimbWall == "True", "Climb Walls");
                }
                else if (false && data.liz.TryGetValue("CanClimbWall", out _))
                {
                    data.liz.Remove("CanClimbWall");
                }

                //CanClimbCeiling: Set inside Lizard
                if (true && data.liz.TryGetValue("CanClimbCeiling", out string CanClimbCeiling))
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        AbilityLog(CanClimbCeiling == "True", "Climb Walls");
                }
                else if (false && data.liz.TryGetValue("CanClimbCeiling", out _))
                {
                    data.liz.Remove("CanClimbCeiling");
                }

                //CanCamo
                if (true && data.liz.TryGetValue("CanCamo", out string CanCamo))
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        AbilityLog(CanCamo == "True", "Climb Walls");
                }
                else if (false && data.liz.TryGetValue("CanCamo", out _))
                {
                    data.liz.Remove("CanCamo");
                }

                void AbilityLog(bool hasAbility, string Ability)
                {
                    Debug.Log(abstractAll + (hasAbility ? " has " : " does not have ") + "the Ability to " + Ability);
                }
            }

            void Immunities()
            {
                //WormGrassImmune: Set inside Lizard
                if (ShadowOfOptions.grass_immune.Value && firstTime && data.liz.TryGetValue("WormGrassImmune", out string WormGrassImmune))
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        ImmuneLog(WormGrassImmune == "True", "Worm Grass");
                }
                else if (!ShadowOfOptions.grass_immune.Value && data.liz.TryGetValue("WormGrassImmune", out _))
                {
                    data.liz.Remove("WormGrassImmune");
                }

                //HypothermiaImmune: Set Here
                if (ModManager.HypothermiaModule && ShadowOfOptions.hypothermia_immune.Value && firstTime && data.liz.TryGetValue("HypothermiaImmune", out string HypothermiaImmune))
                {
                    bool hypothermia = HypothermiaImmune == "True";

                    if (self.HypothermiaImmune != hypothermia)
                    {
                        self.HypothermiaImmune = hypothermia;
                    }
                    else if (hypothermia || !int.TryParse(HypothermiaImmune, out _) || HypothermiaImmune != (isStorySession ? cycleNumber.ToString() : "-1"))
                    {
                        data.liz.Remove("HypothermiaImmune");
                    }

                    if (ShadowOfOptions.debug_logs.Value)
                        ImmuneLog(hypothermia, "Hypothermia");
                }
                else if (!ShadowOfOptions.hypothermia_immune.Value && data.liz.TryGetValue("HypothermiaImmune", out _))
                {
                    data.liz.Remove("HypothermiaImmune");
                }

                //TentacleImmune: Set Here
                if (ShadowOfOptions.tentacle_immune.Value && firstTime && data.liz.TryGetValue("TentacleImmune", out string TentacleImmune))
                {
                    bool tentacle = TentacleImmune == "True";

                    if (self.tentacleImmune != tentacle)
                    {
                        self.tentacleImmune = tentacle;
                    }
                    else
                    {
                        data.liz.Remove("TentacleImmune");
                    }

                    if (ShadowOfOptions.debug_logs.Value)
                        ImmuneLog(tentacle, "Rot Tentacles");
                }
                else if(!ShadowOfOptions.tentacle_immune.Value && data.liz.TryGetValue("TentacleImmune", out _))
                {
                    data.liz.Remove("TentacleImmune");
                }

                //LavaImmune: Set Here
                if (ShadowOfOptions.lava_immune.Value && firstTime && data.liz.TryGetValue("LavaImmune", out string LavaImmune))
                {
                    bool lava = LavaImmune == "True";

                    if (self.lavaImmune != lava)
                    {
                        self.lavaImmune = lava;
                    }
                    else if (lava || !int.TryParse(LavaImmune, out _) || LavaImmune != (isStorySession ? cycleNumber.ToString() : "-1"))
                    {
                        data.liz.Remove("LavaImmune");
                    }

                    if (ShadowOfOptions.debug_logs.Value)
                        ImmuneLog(lava, "Lava/Acid");
                }
                else if (!ShadowOfOptions.lava_immune.Value && data.liz.TryGetValue("LavaImmune", out _))
                {
                    data.liz.Remove("LavaImmune");
                }

                //WaterBreather: Set Here
                if (true && firstTime && data.liz.TryGetValue("WaterBreather", out string WaterBreather))
                {
                    bool waterBreather = WaterBreather == "True";

                    if (self.lavaImmune != waterBreather)
                    {
                        self.lavaImmune = waterBreather;
                    }
                    else
                    {
                        data.liz.Remove("WaterBreather");
                    }

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(abstractAll + (waterBreather ? " can " : " cannot ") + "Breathe Underwater");
                }
                else if (false && data.liz.TryGetValue("WaterBreather", out _))
                {
                    data.liz.Remove("WaterBreather");
                }

                void ImmuneLog(bool isImmune, string whatTo)
                {
                    Debug.Log(abstractAll + (isImmune ? " is " : " is not ") + "Immune to " + whatTo);
                }
            }

            void Physical()
            {
                //Blind: Set inside Lizard
                if (!ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeRight", out _))
                {
                    data.liz.Remove("EyeRight");
                    data.liz.Remove("EyeLeft");
                }

                //Deaf: Set inside Lizard
                if (!ShadowOfOptions.deafen.Value && data.liz.TryGetValue("EarRight", out _))
                {
                    data.liz.Remove("EarRight");
                    data.liz.Remove("EarLeft");
                }

                //Teeth: Set inside Lizard
                if (!ShadowOfOptions.teeth.Value && data.liz.TryGetValue("UpperTeeth", out _))
                {
                    data.liz.Remove("UpperTeeth");
                    data.liz.Remove("LowerTeeth");
                }
            }

            void Transformations()
            {
                if (data.transformation == "Start")
                {
                    if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spawn_spider_transformation_chance.Value)
                    {
                        data.transformation = "SpiderTransformation";

                        data.liz["SpiderNumber"] = UnityEngine.Random.Range(30, 55).ToString();

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " gained the Spider Transformation due to Chance");

                        return;
                    }
                    else if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spawn_electric_transformation_chance.Value)
                    {
                        data.transformation = "ElectricTransformation";

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " gained the Electric Transformation due to Chance");

                        return;
                    }
                    else if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.spawn_melted_transformation_chance.Value)
                    {
                        data.transformation = "MeltedTransformation";

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " gained the Melted Transformation due to Chance");

                        return;
                    }
                    else
                    {
                        data.transformation = "Null";
                    }
                }

                if (!firstTime)
                {
                    return;
                }

                //Rot Transformation: Set inside Lizard
                if (data.transformation.Contains("Rot"))
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(abstractAll + " has the Rot Transformation");
                }

                //Spider Transformation
                if (ShadowOfOptions.spider_transformation.Value && (data.transformation == "Spider" || data.transformation == "SpiderTransformation"))
                {
                    if (isStorySession && data.transformation == "Spider")
                    {
                        if (data.transformationTimer <= cycleNumber - 3 || data.transformationTimer >= cycleNumber + 3 || ShadowOfOptions.spider_transformation_skip.Value)
                        {
                            data.transformation = "SpiderTransformation";

                            data.liz["SpiderNumber"] = UnityEngine.Random.Range(30, 55).ToString();

                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + " has gained the Spider Transformation");
                        }
                        else if (ShadowOfOptions.debug_logs.Value)
                        {
                            int num3 = data.transformationTimer - cycleNumber;
                            string text = (num3 < 0) ? (num3 * -1).ToString() : num3.ToString();
                            Debug.Log(abstractAll + " is Spider Mother for " + text + " cycle out of the required 3 cycles to gain the Spider Transformation");
                        }
                    }
                    else if (data.transformation == "SpiderTransformation")
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            if (data.ArmState[i] == "Cut1" || data.ArmState[i] == "Cut2")
                            {
                                data.ArmState[i] = "Spider";
                            }
                        }

                        data.liz["SpiderNumber"] = UnityEngine.Random.Range(30, 55).ToString();

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(abstractAll + " has the Spider Transformation");
                    }
                }
                else if ((!ShadowOfOptions.spider_transformation.Value || data.transformation != "Spider" && data.transformation != "SpiderTransformation") && data.liz.TryGetValue("SpiderNumber", out string _))
                {
                    data.liz.Remove("SpiderNumber");
                }

                //Electric Transformation
                if (ShadowOfOptions.electric_transformation.Value && (data.transformation == "Electric" || data.transformation == "ElectricTransformation"))
                {
                    if (data.transformationTimer <= 0)
                    {
                        data.transformation = "Null";

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " is no longer Electric due to running out of Charge.");
                    }
                    else if (data.transformationTimer >= 3 || ShadowOfOptions.electric_transformation_skip.Value)
                    {
                        data.transformation = "ElectricTransformation";

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " has gained the Electric Transformation");
                    }
                    else if (data.transformation == "ElectricTransformation")
                    {
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(abstractAll + " has the Electric Transformation");
                    }
                }

                //Melted Transformation
                if (ShadowOfOptions.melted_transformation.Value && (data.transformation == "Melted" || data.transformation == "MeltedTransformation"))
                {
                    if (data.transformation == "Melted" && isStorySession && (data.transformationTimer <= cycleNumber - 3 || data.transformationTimer >= cycleNumber + 3 || ShadowOfOptions.melted_transformation_skip.Value))
                    {
                        data.transformation = "MeltedTransformation";

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " has gained the Melted Transformation");
                    }
                    else if (data.transformation == "MeltedTransformation")
                    {
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(abstractAll + " has the Melted Transformation");
                    }
                    if (data.liz.TryGetValue("PreMeltedCycle", out string _))
                    {
                        data.liz.Remove("PreMeltedCycle");
                    }
                }
                else if ((!ShadowOfOptions.melted_transformation.Value || data.transformation != "SpiderTransformation" && data.transformation != "ElectricTransformation") && data.liz.TryGetValue("PreMeltedCycle", out string _))
                {
                    data.liz.Remove("PreMeltedCycle");
                }
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static void NewLizard(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
    {
        orig.Invoke(self, abstractCreature, world);

        try
        {
            #region code
            if (lizardGoreStorage.TryGetValue(abstractCreature, out LizardGoreData _))
            {
                self.Die();

                return;
            }

            if (self.abstractCreature == null || self.abstractCreature.state == null || self.abstractCreature.state.unrecognizedSaveStrings == null || abstractCreature.creatureTemplate.TopAncestor().type != CreatureTemplate.Type.LizardTemplate
                || !lizardstorage.TryGetValue(self.abstractCreature, out LizardData data))
            {
                return;
            }

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "Started creating " + self);

            self.abstractCreature.creatureTemplate = new CreatureTemplate(self.abstractCreature.creatureTemplate);
            self.lizardParams = LizBread(self);

            data.visualRadius = self.Template.visualRadius;
            data.waterVision = self.Template.waterVision;
            data.throughSurfaceVision = self.Template.throughSurfaceVision;

            #region Local Hooks
            string lizardAll = all + self.ToString();

            Abilities();

            Immunities();

            Physical();

            Transformations();
            #endregion

            List<TileTypeResistance> list = new();

            self.lizardParams.terrainSpeeds = new LizardBreedParams.SpeedMultiplier[Enum.GetNames(typeof(AItile.Accessibility)).Length];
            for (int i = 0; i < self.lizardParams.terrainSpeeds.Length; i++)
            {
                self.lizardParams.terrainSpeeds[i] = new LizardBreedParams.SpeedMultiplier(0.1f, 1f, 1f, 1f);
            }

            self.lizardParams.terrainSpeeds[4] = new LizardBreedParams.SpeedMultiplier(1f, 1f, 1f, 1f);
            list.Add(new TileTypeResistance(AItile.Accessibility.Climb, 0.8f, PathCost.Legality.Allowed));


            for (int l = 0; l < list.Count; l++)
            {
                self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                if (list[l].cost.legality == PathCost.Legality.Allowed && self.abstractCreature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                {
                    self.abstractCreature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
                }
            }

            LizardCustomRelationsSet.Apply(self.Template.type, self);

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "Finished creating " + self);
            #endregion
            void Abilities()
            {
                //Tongue: Set Here
                if (ShadowOfOptions.tongue_stuff.Value && data.liz.TryGetValue("Tongue", out string Tongue))
                {
                    self.lizardParams.tongue = Tongue != "Null";

                    if (self.lizardParams.tongue && data.liz["Tongue"] != "get")
                    {
                        self.tongue = null;

                        BreedTongue(self);
                        self.tongue = new LizardTongue(self);
                    }
                    else
                    {
                        self.tongue = null;

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(lizardAll + " does not have a Tongue");
                    }
                }

                //CanJump: Set Here
                if (ShadowOfOptions.jump_stuff.Value && data.liz.TryGetValue("CanJump", out string CanJump))
                {
                    bool canJump = CanJump == "True";

                    if (self.jumpModule != null != canJump)
                    {
                        self.jumpModule = canJump ? new LizardJumpModule(self) : null;
                    }
                    else
                    {
                        data.liz.Remove("CanJump");
                    }
                }

                //CanSwim: Set Here ToDo: Chance to gain this when dying to: drowning, leeches, kelp, leviathan possibly water den
                if (true && data.liz.TryGetValue("CanSwim", out string CanSwim))
                {
                    bool canSwim = CanSwim == "True";

                    if (self.Template.canSwim.ToString() != data.liz["CanSwim"])
                    {
                        canSwim = data.liz["CanSwim"] == "True";

                        self.Template.canSwim = canSwim;

                        if (canSwim && abstractCreature.creatureTemplate.waterRelationship != CreatureTemplate.WaterRelationship.Amphibious && abstractCreature.creatureTemplate.waterRelationship != CreatureTemplate.WaterRelationship.WaterOnly)
                        {
                            abstractCreature.creatureTemplate.waterRelationship = CreatureTemplate.WaterRelationship.Amphibious;
                        }

                        if (abstractCreature.creatureTemplate.waterPathingResistance > 1f)
                            abstractCreature.creatureTemplate.waterPathingResistance = ((abstractCreature.creatureTemplate.waterPathingResistance - 1f) / 2) + 1f > 1f ? ((abstractCreature.creatureTemplate.waterPathingResistance - 1f) / 2) + 1f : 1f;

                        PathCost dropToWater = abstractCreature.creatureTemplate.pathingPreferencesConnections[(int)MovementConnection.MovementType.DropToWater];
                        if (dropToWater.legality == PathCost.Legality.Unallowed || dropToWater.legality == PathCost.Legality.Unwanted)
                        {
                            List<TileConnectionResistance> list2 = new()
                            {
                            new TileConnectionResistance(MovementConnection.MovementType.DropToWater, 20f, PathCost.Legality.Allowed)
                            };

                            for (int n = 0; n < list2.Count; n++)
                            {
                                abstractCreature.creatureTemplate.pathingPreferencesConnections[(int)list2[n].movementType] = list2[n].cost;
                            }
                        }
                    }
                    else
                    {
                        data.liz.Remove("CanSwim");
                    }
                }

                //CanClimbWall: Set Here
                if (true && data.liz.TryGetValue("CanClimbWall", out string CanClimbWall))
                {
                    bool canClimb = CanClimbWall == "True";

                    if ((self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)AItile.Accessibility.Wall].legality == PathCost.Legality.Allowed).ToString() == CanClimbWall)
                    {
                        data.liz.Remove("CanClimbWall");
                    }
                    else if (canClimb)
                    {
                        List<TileTypeResistance> list = new();
                        self.lizardParams.terrainSpeeds[5] = new LizardBreedParams.SpeedMultiplier(self.lizardParams.terrainSpeeds[1].speed * 0.6f, 1f, 1f, 1f);
                        list.Add(new TileTypeResistance(AItile.Accessibility.Wall, 1f, PathCost.Legality.Allowed));

                        for (int l = 0; l < list.Count; l++)
                        {
                            self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                            if (self.abstractCreature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                            {
                                self.abstractCreature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
                            }
                        }
                    }
                    else
                    {
                        List<TileTypeResistance> list = new();
                        self.lizardParams.terrainSpeeds[5] = new LizardBreedParams.SpeedMultiplier(0, 1f, 1f, 1f);
                        list.Add(new TileTypeResistance(AItile.Accessibility.Wall, 0f, PathCost.Legality.Unallowed));

                        for (int l = 0; l < list.Count; l++)
                        {
                            self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                            if (self.abstractCreature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                            {
                                self.abstractCreature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
                            }
                        }
                    }
                }

                //CanClimbCeilings
                if (false && data.liz.TryGetValue("CanClimbCeiling", out string CanClimbCeiling))
                {
                    bool canClimb = CanClimbCeiling == "True";

                    if ((self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)AItile.Accessibility.Ceiling].legality == PathCost.Legality.Allowed).ToString() == CanClimbWall)
                    {
                        data.liz.Remove("CanClimbCeiling");
                    }
                    else if (canClimb)
                    {
                        List<TileTypeResistance> list = new();

                        self.lizardParams.terrainSpeeds[6] = new LizardBreedParams.SpeedMultiplier(self.lizardParams.terrainSpeeds[5].speed != 0f ? self.lizardParams.terrainSpeeds[5].speed * 0.9f : self.lizardParams.terrainSpeeds[1].speed * 0.6f, 1f, 1f, 1f);
                        list.Add(new TileTypeResistance(AItile.Accessibility.Ceiling, 1.2f, PathCost.Legality.Allowed));

                        for (int l = 0; l < list.Count; l++)
                        {
                            self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                            if (self.abstractCreature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                            {
                                self.abstractCreature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
                            }
                        }
                    }
                    else
                    {
                        List<TileTypeResistance> list = new();

                        self.lizardParams.terrainSpeeds[6] = new LizardBreedParams.SpeedMultiplier(0, 1f, 1f, 1f);
                        list.Add(new TileTypeResistance(AItile.Accessibility.Ceiling, 0f, PathCost.Legality.Unallowed));

                        for (int l = 0; l < list.Count; l++)
                        {
                            self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                            if (self.abstractCreature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                            {
                                self.abstractCreature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
                            }
                        }
                    }
                }

                //CanCamo
            }

            void Immunities()
            {
                //WormGrassImmune: Set here
                if (ShadowOfOptions.grass_immune.Value && data.liz.TryGetValue("WormGrassImmune", out string WormGrassImmune))
                {
                    bool wormGrass = WormGrassImmune == "True";
                    if (self.Template.wormGrassImmune != wormGrass)
                    {
                        self.Template.wormGrassImmune = wormGrass;
                    }
                    else
                    {
                        data.liz.Remove("WormGrassImmune");
                    }
                }

                //HypothermiaImmune: Set inside AbstractLizard
                if (ModManager.HypothermiaModule && ShadowOfOptions.hypothermia_immune.Value && data.liz.TryGetValue("HypothermiaImmune", out string HypothermiaImmune))
                {
                    bool hypothermia = HypothermiaImmune == "True";

                    self.Template.BlizzardWanderer = hypothermia;
                }

                //TentacleImmune: Set inside AbstractLizard

                //LavaImmune: Set inside AbstractLizard

                //WaterBreather: Set inside AbstractLizard
            }

            void Physical()
            {
                //Blind: Set Here
                if (ShadowOfOptions.blind.Value && self.Template.visualRadius > 0f)
                {
                    if (!data.liz.TryGetValue("EyeRight", out _))
                    {
                        data.liz["EyeLeft"] = "Normal";
                        data.liz["EyeRight"] = "Normal";

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(lizardAll + " is not Blind");
                    }
                    else if (data.liz["EyeRight"] == "Incompatible")
                    {
                        Debug.Log(all + "Eye sprites of " + self + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                        ShadowOfLizards.Logger.LogError(all + "Eye sprites of " + self + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                    }
                    else
                    {
                        bool flag = data.liz["EyeRight"] == "Blind" || data.liz["EyeRight"] == "BlindScar" || data.liz["EyeRight"] == "BlindScar2" || data.liz["EyeRight"] == "Cut";
                        bool flag2 = data.liz["EyeLeft"] == "Blind" || data.liz["EyeLeft"] == "BlindScar" || data.liz["EyeLeft"] == "BlindScar2" || data.liz["EyeLeft"] == "Cut";

                        if (flag && flag2)
                        {
                            self.Template.visualRadius = 0f;
                            self.Template.waterVision = 0f;
                            self.Template.throughSurfaceVision = 0f;

                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(lizardAll + " is Blind");
                        }
                        else
                        {
                            if (ShadowOfOptions.debug_logs.Value && (flag ^ flag2))
                                Debug.Log(lizardAll + "'s Vision isn't as good as it once was");

                            float visualRadius = data.visualRadius;
                            float waterVision = data.waterVision;
                            float throughSurfaceVision = data.throughSurfaceVision;
                            if (data.liz["EyeRight"] != "Normal")
                            {
                                if (data.liz["EyeRight"] == "Scar" || data.liz["EyeRight"] == "Scar2")
                                {
                                    self.Template.visualRadius -= visualRadius * 0.25f;
                                    self.Template.waterVision -= waterVision * 0.25f;
                                    self.Template.throughSurfaceVision -= throughSurfaceVision * 0.25f;
                                }
                                else if (flag)
                                {
                                    self.Template.visualRadius -= visualRadius * 0.5f;
                                    self.Template.waterVision -= waterVision * 0.5f;
                                    self.Template.throughSurfaceVision -= throughSurfaceVision * 0.5f;
                                }
                            }

                            if (data.liz["EyeLeft"] != "Normal")
                            {
                                if (data.liz["EyeLeft"] == "Scar" || data.liz["EyeLeft"] == "Scar2")
                                {
                                    self.Template.visualRadius -= visualRadius * 0.25f;
                                    self.Template.waterVision -= waterVision * 0.25f;
                                    self.Template.throughSurfaceVision -= throughSurfaceVision * 0.25f;
                                }
                                else if (flag2)
                                {
                                    self.Template.visualRadius -= visualRadius * 0.5f;
                                    self.Template.waterVision -= waterVision * 0.5f;
                                    self.Template.throughSurfaceVision -= throughSurfaceVision * 0.5f;
                                }
                            }
                        }
                    }
                }

                //Deaf: Set Here
                if (ShadowOfOptions.deafen.Value)
                {
                    if (!data.liz.TryGetValue("EarRight", out _))
                    {
                        data.liz["EarLeft"] = "Normal";
                        data.liz["EarRight"] = "Normal";

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(lizardAll + " is not Deaf");
                    }
                    else
                    {
                        bool flag = data.liz["EarRight"] == "Deaf";
                        bool flag2 = data.liz["EarLeft"] == "Deaf";

                        if (flag && flag2 && self.deaf < 120)
                        {
                            self.deaf = 120;
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(lizardAll + " is Deaf");
                        }
                        else if ((flag ^ flag2) && self.deaf < 4)
                        {
                            self.deaf = 4;
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(lizardAll + " is Half-Deaf");
                        }
                    }
                }

                //Teeth: Set Here
                if (ShadowOfOptions.teeth.Value)
                {
                    if (!data.liz.TryGetValue("UpperTeeth", out string _))
                    {
                        data.liz["UpperTeeth"] = "Normal";
                        data.liz["LowerTeeth"] = "Normal";
                    }
                    else if (data.liz["UpperTeeth"] == "Incompatible")
                    {
                        Debug.Log(all + "Teeth sprites of " + self + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                        ShadowOfLizards.Logger.LogError(all + "Teeth sprites of " + self + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                    }
                    else
                    {
                        bool flag = data.liz["UpperTeeth"] != "Normal" && data.liz["LowerTeeth"] != "Normal";

                        if (flag)
                        {
                            self.lizardParams.biteDamageChance *= 0.4f;
                            self.lizardParams.biteDominance *= 0.4f;
                            self.lizardParams.biteDamage *= 0.4f;
                            self.lizardParams.getFreeBiteChance *= 0.4f;
                        }
                        else if (data.liz["UpperTeeth"] != "Normal" || data.liz["LowerTeeth"] != "Normal")
                        {
                            self.lizardParams.biteDamageChance *= 0.7f;
                            self.lizardParams.biteDominance *= 0.7f;
                            self.lizardParams.biteDamage *= 0.7f;
                            self.lizardParams.getFreeBiteChance *= 0.7f;
                        }
                    }
                }
            }

            void Transformations()
            {
                //Rot Transformation: Set Here
                if (self.LizardState.rotType != LizardState.RotType.None)
                {
                    if (ShadowOfOptions.tentacle_immune.Value && (!data.liz.TryGetValue("TentacleImmune", out string TentacleImmune) || TentacleImmune != "True"))
                    {
                        data.liz["TentacleImmune"] = "True";
                        self.abstractCreature.tentacleImmune = true;

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(lizardAll + "'s Rot Transformation has overridden TentacleImmune to True");
                    }

                    if (data.transformation != "Rot" + self.LizardState.rotType.ToString())
                    {
                        data.transformation = "Rot" + self.LizardState.rotType.ToString();

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(lizardAll + "'s Rot Transformation has overridden the Transformation to " + data.transformation);
                    }
                    return;
                }

                //Spider Transformation: Set inside AbstractCreature

                //Electric Transformation: Set inside AbstractCreature

                //Melted Transformation: Set inside AbstractCreature
                if (ShadowOfOptions.melted_transformation.Value && (data.transformation == "Melted" || data.transformation == "MeltedTransformation"))
                {
                    TransformationMelted.NewMeltedLizard(self, world, data);
                }
            }

            #region misc
            LizardBreedParams LizBread(Lizard liz)
            {
                LizardBreedParams lizardParams = liz.lizardParams;
                return new LizardBreedParams(liz.abstractCreature.creatureTemplate.type)
                {
                    template = lizardParams.template,
                    toughness = lizardParams.toughness,
                    stunToughness = lizardParams.stunToughness,
                    biteDamage = lizardParams.biteDamage,
                    biteDamageChance = lizardParams.biteDamageChance,
                    aggressionCurveExponent = lizardParams.aggressionCurveExponent,
                    danger = lizardParams.danger,
                    biteDelay = lizardParams.biteDelay,
                    baseSpeed = lizardParams.baseSpeed,
                    biteInFront = lizardParams.biteInFront,
                    biteRadBonus = lizardParams.biteRadBonus,
                    biteHomingSpeed = lizardParams.biteHomingSpeed,
                    biteChance = lizardParams.biteChance,
                    attemptBiteRadius = lizardParams.attemptBiteRadius,
                    getFreeBiteChance = lizardParams.getFreeBiteChance,
                    baseSpeedMultiplier = lizardParams.baseSpeedMultiplier,
                    standardColor = lizardParams.standardColor,
                    regainFootingCounter = lizardParams.regainFootingCounter,
                    bodyMass = lizardParams.bodyMass,
                    bodySizeFac = lizardParams.bodySizeFac,
                    bodyLengthFac = lizardParams.bodyLengthFac,
                    bodyRadFac = lizardParams.bodyRadFac,
                    pullDownFac = lizardParams.pullDownFac,
                    floorLeverage = lizardParams.floorLeverage,
                    terrainSpeeds = lizardParams.terrainSpeeds,
                    wiggleSpeed = lizardParams.wiggleSpeed,
                    wiggleDelay = lizardParams.wiggleDelay,
                    bodyStiffnes = lizardParams.bodyStiffnes,
                    swimSpeed = lizardParams.swimSpeed,
                    idleCounterSubtractWhenCloseToIdlePos = lizardParams.idleCounterSubtractWhenCloseToIdlePos,
                    headShieldAngle = lizardParams.headShieldAngle,
                    canExitLounge = lizardParams.canExitLounge,
                    canExitLoungeWarmUp = lizardParams.canExitLoungeWarmUp,
                    findLoungeDirection = lizardParams.findLoungeDirection,
                    loungeDistance = lizardParams.loungeDistance,
                    preLoungeCrouch = lizardParams.preLoungeCrouch,
                    preLoungeCrouchMovement = lizardParams.preLoungeCrouchMovement,
                    loungeSpeed = lizardParams.loungeSpeed,
                    loungePropulsionFrames = lizardParams.loungePropulsionFrames,
                    loungeMaximumFrames = lizardParams.loungeMaximumFrames,
                    loungeJumpyness = lizardParams.loungeJumpyness,
                    loungeDelay = lizardParams.loungeDelay,
                    riskOfDoubleLoungeDelay = lizardParams.riskOfDoubleLoungeDelay,
                    postLoungeStun = lizardParams.postLoungeStun,
                    loungeTendensy = lizardParams.loungeTendensy,
                    perfectVisionAngle = lizardParams.perfectVisionAngle,
                    periferalVisionAngle = lizardParams.periferalVisionAngle,
                    shakePrey = lizardParams.shakePrey,
                    biteDominance = lizardParams.biteDominance,
                    limbSize = lizardParams.limbSize,
                    limbThickness = lizardParams.limbThickness,
                    stepLength = lizardParams.stepLength,
                    liftFeet = lizardParams.liftFeet,
                    feetDown = lizardParams.feetDown,
                    noGripSpeed = lizardParams.noGripSpeed,
                    limbSpeed = lizardParams.limbSpeed,
                    limbQuickness = lizardParams.limbQuickness,
                    limbGripDelay = lizardParams.limbGripDelay,
                    smoothenLegMovement = lizardParams.smoothenLegMovement,
                    legPairDisplacement = lizardParams.legPairDisplacement,
                    walkBob = lizardParams.walkBob,
                    tailSegments = lizardParams.tailSegments,
                    tailStiffness = lizardParams.tailStiffness,
                    tailStiffnessDecline = lizardParams.tailStiffnessDecline,
                    tailLengthFactor = lizardParams.tailLengthFactor,
                    tailColorationStart = lizardParams.tailColorationStart,
                    tailColorationExponent = lizardParams.tailColorationExponent,
                    headSize = lizardParams.headSize,
                    neckStiffness = lizardParams.neckStiffness,
                    jawOpenAngle = lizardParams.jawOpenAngle,
                    jawOpenLowerJawFac = lizardParams.jawOpenLowerJawFac,
                    jawOpenMoveJawsApart = lizardParams.jawOpenMoveJawsApart,
                    headGraphics = lizardParams.headGraphics,
                    framesBetweenLookFocusChange = lizardParams.framesBetweenLookFocusChange,
                    tongue = lizardParams.tongue,
                    tongueAttackRange = lizardParams.tongueAttackRange,
                    tongueWarmUp = lizardParams.tongueWarmUp,
                    tongueSegments = lizardParams.tongueSegments,
                    tongueChance = lizardParams.tongueChance,
                    tamingDifficulty = lizardParams.tamingDifficulty,
                    maxMusclePower = lizardParams.maxMusclePower,
                };
            }

            void BreedTongue(Lizard self)
            {
                if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) || !data.liz.TryGetValue("Tongue", out string Tongue))
                {
                    return;
                }

                switch (Tongue)
                {
                    case "WhiteLizard":
                        self.lizardParams.tongue = true;
                        self.lizardParams.tongueAttackRange = 440f;
                        self.lizardParams.tongueWarmUp = 80;
                        self.lizardParams.tongueSegments = 10;
                        self.lizardParams.tongueChance = 0.1f;
                        break;
                    case "Salamander":
                        self.lizardParams.tongue = true;
                        self.lizardParams.tongueAttackRange = 150f;
                        self.lizardParams.tongueWarmUp = 8;
                        self.lizardParams.tongueSegments = 7;
                        self.lizardParams.tongueChance = 1f / 3f;
                        break;
                    case "BlueLizard":
                        self.lizardParams.tongue = true;
                        self.lizardParams.tongueAttackRange = 140f;
                        self.lizardParams.tongueWarmUp = 10;
                        self.lizardParams.tongueSegments = 5;
                        self.lizardParams.tongueChance = 0.25f;
                        break;
                    case "CyanLizard":
                        self.lizardParams.tongue = true;
                        self.lizardParams.tongueAttackRange = 160f;
                        self.lizardParams.tongueWarmUp = 8;
                        self.lizardParams.tongueSegments = 7;
                        self.lizardParams.tongueChance = 1f / 3f;
                        break;
                    case "RedLizard":
                        self.lizardParams.tongue = true;
                        self.lizardParams.tongueAttackRange = 350f;
                        self.lizardParams.tongueWarmUp = 8;
                        self.lizardParams.tongueSegments = 10;
                        self.lizardParams.tongueChance = 0.1f;
                        break;
                    case "ZoopLizard":
                        self.lizardParams.tongue = true;
                        self.lizardParams.tongueAttackRange = 440f;
                        self.lizardParams.tongueWarmUp = 140;
                        self.lizardParams.tongueSegments = 10;
                        self.lizardParams.tongueChance = 0.3f;
                        break;
                    case "Tube":
                        self.lizardParams.tongue = true;
                        self.lizardParams.tongueAttackRange = 200f;
                        self.lizardParams.tongueWarmUp = 0;
                        self.lizardParams.tongueSegments = 20;
                        self.lizardParams.tongueChance = 0.3f;
                        break;
                    case "IndigoLizard":
                        self.lizardParams.tongue = true;
                        self.lizardParams.tongueAttackRange = 190f;
                        self.lizardParams.tongueWarmUp = 8;
                        self.lizardParams.tongueSegments = 7;
                        self.lizardParams.tongueChance = 0.33333334f;
                        break;
                    default:
                        Debug.Log(all + "Failed Getting the " + Tongue + " Tongue for " + self);
                        ShadowOfLizards.Logger.LogError(all + "Failed Getting the " + Tongue + " Tongue for " + self);
                        self.lizardParams.tongue = true;
                        self.lizardParams.tongueAttackRange = 140f;
                        self.lizardParams.tongueWarmUp = 10;
                        self.lizardParams.tongueSegments = 5;
                        self.lizardParams.tongueChance = 0.25f;
                        break;
                }
            }
            #endregion
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    /*
    List<TileTypeResistance> list = new();
    List<TileConnectionResistance> list2 = new(); 

    self.lizardParams.terrainSpeeds = new LizardBreedParams.SpeedMultiplier[Enum.GetNames(typeof(AItile.Accessibility)).Length];
    for (int i = 0; i < self.lizardParams.terrainSpeeds.Length; i++)
    {
        self.lizardParams.terrainSpeeds[i] = new LizardBreedParams.SpeedMultiplier(0.1f, 1f, 1f, 1f);
    }

    self.lizardParams.terrainSpeeds[4] = new LizardBreedParams.SpeedMultiplier(1f, 1f, 1f, 1f);
    list.Add(new TileTypeResistance(AItile.Accessibility.Climb, 0.8f, PathCost.Legality.Allowed));
    self.lizardParams.terrainSpeeds[5] = new LizardBreedParams.SpeedMultiplier(0.8f, 1f, 1f, 1f);
    list.Add(new TileTypeResistance(AItile.Accessibility.Wall, 1f, PathCost.Legality.Allowed));
    self.lizardParams.terrainSpeeds[6] = new LizardBreedParams.SpeedMultiplier(0.6f, 1f, 1f, 1f);
    list.Add(new TileTypeResistance(AItile.Accessibility.Ceiling, 1.2f, PathCost.Legality.Allowed));

    list2.Add(new TileConnectionResistance(MovementConnection.MovementType.DropToFloor, 20f, PathCost.Legality.Allowed));
    list2.Add(new TileConnectionResistance(MovementConnection.MovementType.DropToClimb, 2f, PathCost.Legality.Allowed));
    list2.Add(new TileConnectionResistance(MovementConnection.MovementType.ShortCut, 15f, PathCost.Legality.Allowed));
    list2.Add(new TileConnectionResistance(MovementConnection.MovementType.ReachOverGap, 1.1f, PathCost.Legality.Allowed));
    list2.Add(new TileConnectionResistance(MovementConnection.MovementType.ReachUp, 1.1f, PathCost.Legality.Allowed));
    list2.Add(new TileConnectionResistance(MovementConnection.MovementType.ReachDown, 1.1f, PathCost.Legality.Allowed));
    list2.Add(new TileConnectionResistance(MovementConnection.MovementType.CeilingSlope, 2f, PathCost.Legality.Allowed));

    list2.Add(new TileConnectionResistance(MovementConnection.MovementType.DropToWater, 20f, PathCost.Legality.Allowed));

    for (int l = 0; l < list.Count; l++)
    {
        self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
        if (list[l].cost.legality == PathCost.Legality.Allowed && self.abstractCreature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
        {
            self.abstractCreature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
        }
    }

    for (int n = 0; n < list2.Count; n++)
    {
        self.abstractCreature.creatureTemplate.pathingPreferencesConnections[(int)list2[n].movementType] = list2[n].cost;
    }

    if (self.lizardParams.terrainSpeeds[2].speed == 0.1f)
    {
        self.lizardParams.terrainSpeeds[2] = self.lizardParams.terrainSpeeds[1];
    }
    */

    static void LizardViolence(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
    {
        if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) || data.lastDamageType == null)
        {
            orig.Invoke(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
            return;
        }

        try
        {
            bool sourceFlag = source != null && source.owner != null;

            bool sourcetypeFlag = (source == null && type.ToString() == "Explosion") || (source != null && (source.owner == null || (source.owner != null && source.owner is not JellyFish)));

            #region Electric
            if (type == Creature.DamageType.Electric && (data.transformation == "Electric" || data.transformation == "ElectricTransformation"))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + source.owner.ToString() + "'s damage was halved due to Resistance on " + self);

                if (data.transformation == "Electric" && UnityEngine.Random.value < 0.2)
                    data.transformationTimer++;

                damage /= 2f;
            }
            #endregion

            #region Melted
            if (data.lastDamageType != "Melted" && type.ToString() != "Explosion")
                data.lastDamageType = type.ToString();
            #endregion

            PreViolenceCheck(self);
            orig.Invoke(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
            if (sourceFlag && source.owner is Creature crit)
            {
                PostViolenceCheck(self, type.ToString(), crit);
            }
            else
            {
                PostViolenceCheck(self, type.ToString(), null);
            }

            if (hitChunk == null || damage < 0.5f || !sourcetypeFlag || !directionAndMomentum.HasValue)
            {
                return;
            }

            //Cut in Half
            if (false && hitChunk.index != 0 && hitChunk.index != 1 && data.availableBodychunks.Contains(hitChunk.index))
            {
                CutInHalf(self, hitChunk);
            }

            if (hitChunk.index == 0)
            {
                if (data.Beheaded != true && hitChunk.index == 0)
                {
                    if (LizHitHeadShield(directionAndMomentum.Value, self))
                    {
                        if (ShadowOfOptions.blind.Value && data.liz.TryGetValue("EyeRight", out _) && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.blind_cut_chance.Value)
                        {
                            string eye = (UnityEngine.Random.Range(0, 2) == 0) ? "EyeRight" : "EyeLeft";

                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + "'s " + eye + " was hit");

                            if (type == Creature.DamageType.Stab || type == Creature.DamageType.Bite)
                            {
                                if ((data.liz[eye] == "Normal" || data.liz[eye] == "Blind") && UnityEngine.Random.value < 0.70)
                                {
                                    HitEye(eye, data.liz[eye], "Scar");
                                }
                                else if (data.liz[eye] == "Cut")
                                {
                                    self.Blind(5);
                                    if (ShadowOfOptions.debug_logs.Value)
                                        Debug.Log(all + self.ToString() + "'s " + eye + " is already Cut");
                                }
                                else
                                {
                                    HitEye(eye, data.liz[eye], "Cut");
                                }
                            }
                            else
                            {
                                if (data.liz[eye] == "Normal" || data.liz[eye] == "Blind")
                                {
                                    HitEye(eye, data.liz[eye], "Scar");
                                }
                                else
                                {
                                    self.Blind(5);
                                }
                            }

                        }
                        else if (ShadowOfOptions.teeth.Value && data.liz.TryGetValue("UpperTeeth", out _) && type != Creature.DamageType.Bite && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.teeth_chance.Value)
                        {
                            string teeth = UnityEngine.Random.Range(0, 2) == 0 ? "UpperTeeth" : "LowerTeeth";
                            int teethNum = UnityEngine.Random.Range(1, 5);

                            if (data.liz[teeth] == "Normal")
                            {
                                data.liz[teeth] = "Broken" + teethNum.ToString();
                                self.lizardParams.biteDamageChance *= 0.5f;
                                self.lizardParams.biteDominance *= 0.5f;
                                self.lizardParams.biteDamage *= 1.1f;
                                self.lizardParams.getFreeBiteChance *= 1.1f;

                                if (ShadowOfOptions.debug_logs.Value)
                                    Debug.Log(all + self.ToString() + " " + (teeth == "UpperTeeth" ? "Upper" : "Lower") + " Teeth were broken");

                                if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                                    data.cheatDeathChance -= 5;

                                self.room.PlaySound(SoundID.SS_AI_Marble_Hit_Floor, self.firstChunk, false, Custom.LerpMap(source.vel.magnitude, 0f, 8f, 0.2f, 1f) + 10, 1f);
                            }
                        }

                        if (ShadowOfOptions.decapitation.Value && data.lastDamageType != "Melted" && type.ToString() == "Explosion")
                        {
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + " had it's Head cut by an explosion");

                            if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.decapitation_chance.Value * 0.5)
                            {
                                data.Beheaded = true;
                                Decapitation(self);
                                self.Die();
                            }
                        }
                    }
                    else if (LizHitInMouth(directionAndMomentum.Value, self))
                    {
                        if (ShadowOfOptions.tongue_stuff.Value && self.tongue != null && data.lastDamageType != "Melted" && type != Creature.DamageType.Blunt)
                        {
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self.ToString() + " was hit in it's Mouth");

                            if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.tongue_stuff_chance.Value)
                            {
                                self.tongue.Retract();

                                data.liz["Tongue"] = "Null";
                                self.lizardParams.tongue = false;

                                if (ShadowOfOptions.debug_logs.Value)
                                    Debug.Log(all + self.ToString() + " lost it's Tongue due to being hit in Mouth");

                                if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                                    data.cheatDeathChance -= 10;
                            }
                        }
                    }
                    else if (ShadowOfOptions.decapitation.Value && data.Beheaded == false && data.lastDamageType != "Melted" && type != Creature.DamageType.Blunt)
                    {
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " was hit it's Neck");

                        if (HealthBasedChance(self, ShadowOfOptions.decapitation_chance.Value))
                        {
                            data.Beheaded = true;
                            Decapitation(self);
                            self.Die();
                        }
                    }
                }
            }
            else if (ShadowOfOptions.dismemberment.Value && (hitChunk.index == 1 || hitChunk.index == 2) && HealthBasedChance(self, ShadowOfOptions.dismemberment_chance.Value))
            {
                float num5 = Custom.Angle(new Vector2(directionAndMomentum.Value.x, directionAndMomentum.Value.y), -hitChunk.Rotation) * (hitChunk.index == 2 ? -1f : 1f);
                int num8;

                if (ModManager.DLCShared && self.Template.type == DLCSharedEnums.CreatureTemplateType.EelLizard)
                {
                    if (hitChunk.index == 1)
                    {
                        num8 = (num5 < 0f) ? 0 : 1;
                        int num9 = (num5 < 0f) ? 2 : 3;

                        if (data.ArmState[num8] == "Normal")
                        {
                            EllLegCut(num8, num9, num8);
                        }
                    }
                }
                else if ((ModManager.DLCShared && self.Template.type == DLCSharedEnums.CreatureTemplateType.SpitLizard) || (self.graphicsModule as LizardGraphics).limbs.Length == 6)
                {
                    if (hitChunk.index == 1)
                    {
                        num8 = (num5 < 0f) ? 2 : 3;
                        int num9 = (num5 < 0f) ? 4 : 5;

                        if (UnityEngine.Random.value < 0.75 && data.ArmState[num8] == "Normal")
                        {
                            LegCut(num8, num8);
                        }
                        else if (data.ArmState[num9] == "Normal")
                        {
                            LegCut(num9, num9);
                        }
                    }
                    else
                    {
                        num8 = (num5 < 0f) ? 0 : 1;
                        int num10 = (num5 < 0f) ? 5 : 4;

                        if (UnityEngine.Random.value < 0.75 && data.ArmState[num8] == "Normal")
                        {
                            LegCut(num8, num8);
                        }
                        else if (data.ArmState[num10] == "Normal")
                        {
                            LegCut(num10, num10);
                        }
                    }
                }
                else
                {
                    if (hitChunk.index == 2)
                    {
                        num8 = (num5 < 0f) ? 2 : 3;

                        if (data.ArmState[num8] == "Normal")
                        {
                            LegCut(num8, num8);
                        }
                    }
                    else
                    {
                        num8 = (num5 < 0f) ? 0 : 1;

                        if (data.ArmState[num8] == "Normal")
                        {
                            LegCut(num8, num8);
                        }
                    }
                }
            }

            if (ShadowOfOptions.electric_transformation.Value && self.graphicsModule != null && graphicstorage.TryGetValue(self.graphicsModule as LizardGraphics, out GraphicsData data2) && sourceFlag && data.transformation == "ElectricTransformation")
            {
                TransformationElectric.ElectricLizardViolence(self, source, hitChunk, data2);
                return;
            }

            //Voids (and bools)
            void EllLegCut(int FirstLeg, int SecondLeg, int int1)
            {
                bool a = UnityEngine.Random.Range(0, 2) == 0;

                data.ArmState[FirstLeg] = a ? "Cut1" : "Cut2";
                data.ArmState[SecondLeg] = a ? "Cut1" : "Cut2";

                LimbCut(self, hitChunk, int1, data.ArmState[FirstLeg]);

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " limb cut " + int1);
            }

            void LegCut(int Leg, int int1)
            {
                data.ArmState[Leg] = UnityEngine.Random.Range(0, 2) == 0 ? "Cut1" : "Cut2";

                LimbCut(self, hitChunk, int1, data.ArmState[Leg]);

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " limb cut " + int1);
            }

            static bool LizHitHeadShield(Vector2 direction, Lizard self)
            {
                float num19 = Vector2.Angle(direction, -self.bodyChunks[0].Rotation);
                if (LizHitInMouth(direction, self))
                {
                    return false;
                }
                if (num19 < self.lizardParams.headShieldAngle + 20f * self.JawOpen)
                {
                    Room room = self.room;
                    room?.PlaySound(SoundID.Lizard_Head_Shield_Deflect, self.mainBodyChunk);
                    return true;
                }
                return false;
            }
            static bool LizHitInMouth(Vector2 direction, Lizard self)
            {
                if (direction.y > 0f)
                {
                    return false;
                }
                direction = Vector3.Slerp(direction, new Vector2(0f, 1f), 0.1f);
                return Mathf.Abs(Vector2.Angle(direction, -self.bodyChunks[0].Rotation)) < Mathf.Lerp(-15f, 11f, self.JawOpen);
            }

            void HitEye(string eye, string oldEye, string newEye)
            {
                bool cut = newEye == "Cut";

                data.liz[eye] = (oldEye.StartsWith("Blind") ? "Blind" : "") + newEye + (newEye == "Scar" ? (UnityEngine.Random.Range(0, 2) == 0 ? "" : "2") : "");
                self.Blind(cut ? 40 : 5);

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + "'s " + eye + " was " + oldEye + " now it's " + data.liz[eye]);

                if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                    data.cheatDeathChance -= cut ? 10 : 5;

                if (cut)
                    EyeCut(self, eye);
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static void LizardBite(On.Lizard.orig_Bite orig, Lizard self, BodyChunk chunk)
    {
        if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data))
        {
            orig.Invoke(self, chunk);
            return;
        }

        try
        {
            if (ShadowOfOptions.teeth.Value && data.liz.TryGetValue("UpperTeeth", out string upperTeeth))
            {

                string lowerTeeth = data.liz["LowerTeeth"];

                bool flag = upperTeeth != "Normal" && lowerTeeth != "Normal";

                if (flag)
                {
                    if (UnityEngine.Random.value < 0.4f)
                    {
                        self.biteControlReset = false;
                        self.JawOpen = 0f;
                        self.lastJawOpen = 0f;

                        self.room.PlaySound(SoundID.Lizard_Jaws_Shut_Miss_Creature, self.mainBodyChunk);
                        return;
                    }
                }
                else if (upperTeeth != "Normal" || lowerTeeth != "Normal")
                {
                    if (UnityEngine.Random.value < 0.7f)
                    {
                        if (ModManager.MSC && self.Template.type == MoreSlugcatsEnums.CreatureTemplateType.TrainLizard && self.room != null)
                        {
                            for (int i = 0; i < 16; i++)
                            {
                                Vector2 a = Custom.RNV();
                                self.room.AddObject(new Spark(self.firstChunk.pos + a * 40f, a * Mathf.Lerp(4f, 30f, UnityEngine.Random.value), Color.white, null, 8, 24));
                            }
                        }
                        self.biteControlReset = false;
                        self.JawOpen = 0f;
                        self.lastJawOpen = 0f;

                        self.room.PlaySound(SoundID.Lizard_Jaws_Shut_Miss_Creature, self.mainBodyChunk);
                        return;
                    }
                }
            }

            bool elec = false;
            bool melt = false;

            if (ShadowOfOptions.electric_transformation.Value && self.graphicsModule != null && graphicstorage.TryGetValue(self.graphicsModule as LizardGraphics, out GraphicsData data2) && data.transformation == "ElectricTransformation")
            {
                TransformationElectric.PreElectricLizardBite(self, chunk, data2, data);
                elec = true;
            }

            if (ShadowOfOptions.melted_transformation.Value && (data.transformation == "Melted" || data.transformation == "MeltedTransformation"))
            {
                TransformationMelted.PreMeltedLizardBite(self, chunk);
                melt = true;
            }

            orig.Invoke(self, chunk);

            if (elec && self.graphicsModule != null && graphicstorage.TryGetValue(self.graphicsModule as LizardGraphics, out GraphicsData data3))
            {
                TransformationElectric.PostElectricLizardBite(self, data3);
            }

            if (melt)
            {
                TransformationMelted.PostMeltedLizardBite(self, chunk);
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }

    }

    static void LizardUpdate(On.Lizard.orig_Update orig, Lizard self, bool eu)
    {
        orig.Invoke(self, eu);

        try
        {
            if (self.abstractCreature == null || !lizardstorage.TryGetValue(self.abstractCreature, out LizardData data))
            {
                return;
            }

            //Deaf
            if (ShadowOfOptions.deafen.Value && data.liz.TryGetValue("EarRight", out _))
            {
                bool flag = data.liz["EarRight"] == "Deaf";
                bool flag2 = data.liz["EarLeft"] == "Deaf";

                if (flag && flag2 && self.deaf < 120)
                {
                    self.deaf = 120;
                }
                else if ((flag ^ flag2) && self.deaf < 4)
                {
                    self.deaf = 4;
                }
            }

            //Limb Health
            if (ShadowOfOptions.dismemberment.Value && self.LizardState != null && self.LizardState.limbHealth != null)
            {
                for (int i = 0; i < data.ArmState.Count; i++)
                {
                    if (data.ArmState[i] != "Normal" && data.ArmState[i] != "Spider")
                    {
                        self.LizardState.limbHealth[i] = 0f;
                    }
                }
            }

            //Worm Grass Immunity Loss
            /*
            if (ShadowOfOptions.grass_immune.Value && self.AI != null && self.AI.behavior != null && self.AI.behavior == LizardAI.Behavior.Flee && self.room != null && self.room.updateList != null && 
                self.room.updateList.Any((UpdatableAndDeletable x) => x is WormGrass))
            {
                self.Template.wormGrassImmune = false;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self.ToString() + " lost immunity to Worm Grass due to being Scared near Worm Grass");
            }
            */

            bool isStorySession = self.abstractCreature.world.game.IsStorySession;
            int cycleNumber = isStorySession ? self.abstractCreature.world.game.GetStorySession.saveState.cycleNumber : -1;

            //HypothermiaImmune
            if (ModManager.HypothermiaModule && ShadowOfOptions.hypothermia_immune.Value && self.dead && self.Hypothermia <= 1f
                && (!data.liz.TryGetValue("HypothermiaImmune", out string HypothermiaImmune) || int.TryParse(HypothermiaImmune, out _) && HypothermiaImmune != cycleNumber.ToString()))
            {
                data.liz["HypothermiaImmune"] = cycleNumber.ToString();
                if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.hypothermia_immune_chance.Value)
                {
                    data.liz["HypothermiaImmune"] = "True";
                }
            }

            //LavaImmune
            if (true && self.dead && self.Submersion > 0.1f && self.room.waterObject != null && self.room.waterObject.WaterIsLethal 
                && (!data.liz.TryGetValue("LavaImmune", out string LavaImmune) || int.TryParse(LavaImmune, out _) && LavaImmune != cycleNumber.ToString()))
            {
                data.liz["LavaImmune"] = cycleNumber.ToString();
                if (UnityEngine.Random.Range(0, 100) < ShadowOfOptions.melted_transformation_chance.Value)
                {
                    data.liz["LavaImmune"] = "True";
                }
            }

            //MeltedTransformation
            if (ShadowOfOptions.melted_transformation.Value && self.dead && self.Submersion > 0.1f && self.room.waterObject != null && self.room.waterObject.WaterIsLethal 
                && (!data.liz.TryGetValue("PreMeltedCycle", out string PreMeltedCycle) || int.TryParse(PreMeltedCycle, out _) && PreMeltedCycle != cycleNumber.ToString()))
            {
                TransformationMelted.MeltedLizardUpdate(self, data);
                return;
            }

            //ElectricTransformation
            if (ShadowOfOptions.electric_transformation.Value && data.transformation == "ElectricTransformation" && self.graphicsModule != null && graphicstorage.TryGetValue(self.graphicsModule as LizardGraphics, out GraphicsData data2))
            {
                TransformationElectric.ElectricLizardUpdate(self, data2);
                return;
            }

            //Regrowth
            if (ShadowOfOptions.eat_regrowth.Value && self.enteringShortCut.HasValue && self.room != null && self.room.shortcutData(self.enteringShortCut.Value).shortCutType != null &&
                self.room.shortcutData(self.enteringShortCut.Value).shortCutType == ShortcutData.Type.CreatureHole && self.grasps[0] != null)
            {
                if (data.denCheck == false)
                {
                    data.denCheck = true;
                    EatRegrowth(self, data);
                }
            }
            else
            {
                data.denCheck = false;
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }

        static void EatRegrowth(Lizard self, LizardData data)
        {
            Lizard liz = self.grasps[0].grabbed is Lizard lizard ? lizard : null;
            LizardData data2 = (liz != null && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData dat)) ? dat : null;

            #region Tongue
            if (ShadowOfOptions.tongue_stuff.Value && ShadowOfOptions.tongue_regrowth.Value && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.tongue_regrowth_chance.Value)
            {
                if (self.grasps[0].grabbed is TubeWorm)
                {
                    if (!self.lizardParams.tongue)
                    {
                        data.liz["Tongue"] = "Tube";

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " grew a new Tongue due to eating a " + self.grasps[0].grabbed);

                        if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                            data.cheatDeathChance += 5;
                    }
                    else if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " did not grow a new Tongue due to eating a " + self.grasps[0].grabbed + " because it already has one");
                }
                else if (ShadowOfOptions.eat_lizard.Value && liz != null && liz.lizardParams.tongue)
                {
                    if (!self.lizardParams.tongue)
                    {
                        data.liz["Tongue"] = data2.liz.TryGetValue("Tongue", out string tongue) && tongue != "Null" && tongue != "get" ? tongue : validTongues.Contains(liz.Template.type.ToString()) ? liz.Template.type.ToString() : "get";

                        if (data.liz["Tongue"] == self.Template.type.ToString())
                        {
                            data.liz.Remove("Tongue");
                        }

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " grew a new Tongue due to eating " + self.grasps[0].grabbed + " who had a Tongue");

                        if (data2 != null)
                        {
                            data2.liz["Tongue"] = "Null";

                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + liz.ToString() + " lost it's Tongue due to being eaten by " + self.grasps[0].grabbed + " that took it's Tongue");
                        }

                        if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                            data.cheatDeathChance += 5;
                    }
                    else if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " did not grow a new Tongue due to eating " + self.grasps[0].grabbed + " because it already has one");
                }
            }
            #endregion

            #region Jump
            if (ShadowOfOptions.jump_stuff.Value && ShadowOfOptions.jump_regrowth.Value && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.jump_regrowth_chance.Value)
            {
                if (self.grasps[0].grabbed is Yeek || self.grasps[0].grabbed is Cicada || self.grasps[0].grabbed is JetFish || (self.grasps[0].grabbed is Centipede centi && centi.abstractCreature.creatureTemplate.type == CreatureTemplate.Type.Centiwing))
                {
                    if (self.jumpModule == null)
                    {
                        data.liz["CanJump"] = "True";

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " gained the ability to Jump due to eating " + self.grasps[0].grabbed + " that had the ability to Jump");

                        if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                            data.cheatDeathChance += 5;
                    }
                    else if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " did not grow a new ability to Jump due to eating " + self.grasps[0].grabbed + " because it already has one");
                }
                else if (ShadowOfOptions.eat_lizard.Value && liz != null && liz.jumpModule != null)
                {
                    if (self.jumpModule == null)
                    {
                        data.liz["CanJump"] = "True";

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " gained the ability to Jump due to eating " + self.grasps[0].grabbed + " who had the ability to Jump");

                        if (data2 != null)
                        {
                            data2.liz["CanJump"] = "False";

                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + liz.ToString() + " lost it's ability to Jump due to being eaten by " + self + " that took it's ability to Jump");
                        }

                        if (ShadowOfOptions.dynamic_cheat_death_chance.Value)
                            data.cheatDeathChance += 5;
                    }
                    else if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " did not grow a new ability to Jump due to eating " + self.grasps[0].grabbed + " because it already has one");
                }
            }
            #endregion

            #region Transformations
            if (ShadowOfOptions.melted_transformation.Value && ShadowOfOptions.melted_regrowth.Value && ShadowOfOptions.eat_lizard.Value && liz != null && data.transformation != "SpiderTransformation" && data.transformation != "ElectricTransformation" &&
                ((data2.transformation == "MeltedTransformation" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.melted_regrowth_chance.Value) || (data2.transformation == "Melted" && UnityEngine.Random.Range(0, 100) < ShadowOfOptions.melted_regrowth_chance.Value * 0.5)))
            {
                TransformationMelted.MeltedEatRegrowth(self, liz, data, data2);
                return;
            }

            if (ShadowOfOptions.electric_transformation.Value && ShadowOfOptions.electric_regrowth.Value && (data.transformation == "Null" || data.transformation == "Electric" || data.transformation == "Spider"))
            {
                TransformationElectric.ElectricEatRegrowth(self, liz, data, data2);
                return;
            }

            if (ShadowOfOptions.spider_transformation.Value && ShadowOfOptions.spider_regrowth.Value && data.transformation == "Null")
            {
                TransformationSpider.SpiderEatRegrowth(self, liz, data, data2);
                return;
            }
            #endregion
        }
    }

    static bool LizardHitHeadShield(On.Lizard.orig_HitHeadShield orig, Lizard self, Vector2 direction)
    {
        if (lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) && data.Beheaded == true)
        {
            return false;
        }
        return orig.Invoke(self, direction);
    }

    static void CreatureDie(On.Creature.orig_Die orig, Creature self)
    {
        try
        {
            Lizard liz = (self is Lizard lizard) ? lizard : null;

            if (liz != null && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) && !liz.dead)
            {
                if (ShadowOfOptions.spider_transformation.Value && (data.transformation == "Spider" || data.transformation == "SpiderTransformation"))
                {
                    TransformationSpider.BabyPuff(liz);
                }

                if (self.abstractCreature.world.game.IsStorySession && UnityEngine.Random.Range(0, 100) < data.cheatDeathChance)
                {
                    self.dead = true;
                    self.LoseAllGrasps();

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " Cheated Death");

                    if (ShadowOfOptions.dynamic_cheat_death_chance.Value && data.Beheaded == true)
                    {
                        data.cheatDeathChance += 25;

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " Cheated Death while Decapitated!!!");
                    }

                    if (self.killTag != null && self.killTag.realizedCreature != null)
                    {
                        Room room = self.room;
                        room ??= self.abstractCreature.Room.realizedRoom;

                        if (room != null && room.socialEventRecognizer != null)
                        {
                            room.socialEventRecognizer.Killing(self.killTag.realizedCreature, self);
                        }
                    }
                }
                else
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self.ToString() + " failed to cheat death");

                    orig.Invoke(self);
                }
            }
            else
            {
                orig.Invoke(self);
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }

    static string SaveStateSaveAbstractCreature(On.SaveState.orig_AbstractCreatureToStringStoryWorld_AbstractCreature_WorldCoordinate orig, AbstractCreature self, WorldCoordinate cc)
    {
        if (self == null || self.state == null || self.state.unrecognizedSaveStrings == null || self.creatureTemplate.TopAncestor().type != CreatureTemplate.Type.LizardTemplate || !lizardstorage.TryGetValue(self, out LizardData data))
        {
            return orig(self, cc);
        }

        try
        {

            if (ShadowOfOptions.debug_logs.Value)
            {
                Debug.Log(all + "Saving values for Abstract " + self);
            }

            Dictionary<string, string> savedData = self.state.unrecognizedSaveStrings;

            savedData["ShadowOfBeheaded"] = data.Beheaded ? "True" : "False";

            string liz = "";
            for (int i = 0; i < data.liz.Count; i++)
            {
                liz += data.liz.ElementAt(i).Key + "=";
                liz += data.liz.ElementAt(i).Value + ";";
            }
            savedData["ShadowOfLiz"] = liz;

            string chunk = "";
            for (int i = 0; i < data.availableBodychunks.Count; i++)
            {
                chunk += data.availableBodychunks[i] + ";";
            }
            savedData["ShadowOfAvailableBodychunks"] = chunk;

            savedData["ShadowOfTransformation"] = data.transformation;
            savedData["ShadowOfTransformationTimer"] = data.transformationTimer.ToString();

            string ArmState = "";
            for (int i = 0; i < data.ArmState.Count; i++)
            {
                ArmState += data.ArmState[i] + ";";
            }
            savedData["ShadowOfArmState"] = ArmState;

            savedData["ShadowOfLizardUpdatedCycle"] = data.lizardUpdatedCycle.ToString();

            savedData["ShadowOfCheatDeathChance"] = data.cheatDeathChance.ToString();


            if (ShadowOfOptions.debug_logs.Value)
            {
                Debug.Log(all + " " + self + " destroyOnAbstraction " + self.destroyOnAbstraction);

                Debug.Log(all + self + " beheaded = " + savedData["ShadowOfBeheaded"]);
                Debug.Log(all + self + " lizDictionary = " + savedData["ShadowOfLiz"]);

                Debug.Log(all + self + " bodyChunks = " + savedData["ShadowOfAvailableBodychunks"]);

                Debug.Log(all + self + " transformation = " + savedData["ShadowOfTransformation"]);
                Debug.Log(all + self + " transformationTimer = " + savedData["ShadowOfTransformationTimer"]);

                Debug.Log(all + self + " armState = " + savedData["ShadowOfArmState"]);

                Debug.Log(all + self + " updatedCycle = " + savedData["ShadowOfLizardUpdatedCycle"]);

                Debug.Log(all + self + " cheatDeathChance = " + savedData["ShadowOfCheatDeathChance"]);
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }

        return orig(self, cc);
    }

    // Token: 0x04001E5C RID: 7772
    public PathCost[] pathingPreferencesTiles;

    // Token: 0x04001E5D RID: 7773
    public PathCost[] pathingPreferencesConnections;

}

