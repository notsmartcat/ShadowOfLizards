using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using RWCustom;

using static ShadowOfLizards.ShadowOfLizards;

namespace ShadowOfLizards;

internal class LizardHooks
{
    public static void Apply()
    {
        On.AbstractCreature.ctor += NewAbstractLizard;
        On.LizardState.LoadFromString += CreatureStateLoadFromString;
        On.SaveState.AbstractCreatureToStringStoryWorld_AbstractCreature_WorldCoordinate += SaveStateSaveAbstractCreature;

        On.Lizard.ctor += NewLizard;
        On.Lizard.Bite += LizardBite;
        On.Lizard.HitHeadShield += LizardHitHeadShield;
        On.Lizard.Update += LizardUpdate;
        On.Lizard.Violence += LizardViolence;

        On.Creature.Die += CreatureDie;
    }

    #region LizardCreation
    static void NewAbstractLizard(On.AbstractCreature.orig_ctor orig, AbstractCreature self, World world, CreatureTemplate creatureTemplate, Creature realizedCreature, WorldCoordinate pos, EntityID ID)
    {
        orig(self, world, creatureTemplate, realizedCreature, pos, ID);

        if (self == null || self.state == null || self.state.unrecognizedSaveStrings == null || creatureTemplate.TopAncestor().type != CreatureTemplate.Type.LizardTemplate || !IsLizardValid(self.creatureTemplate.type.ToString()))
        {
            return;
        }

        if (!lizardstorage.TryGetValue(self, out LizardData data))
        {
            lizardstorage.Add(self, new());
            lizardstorage.TryGetValue(self, out data);
        }

        try
        {
            self.creatureTemplate = new(self.creatureTemplate);

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "First time creating Abstract " + self);

            data.beheaded = false;

            if (ShadowOfOptions.dynamic_cheat_death.Value)
            {
                data.cheatDeathChance = UnityEngine.Random.Range(0, 101) + ShadowOfOptions.cheat_death_chance.Value;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self + " got " + data.cheatDeathChance + " Chance to Cheat Death due to Dynamic Death Chance being On.");
            }
            else
            {
                data.cheatDeathChance = ShadowOfOptions.cheat_death_chance.Value;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self + " got a flat " + data.cheatDeathChance + " Chance to Cheat Death due to Dynamic Death Chance being Off.");
            }

            for (int i = 0; i < (ModManager.DLCShared && creatureTemplate.type == DLCSharedEnums.CreatureTemplateType.SpitLizard ? 6 : 4); i++)
            {
                data.armState.Add("Normal");
            }

            Abilities();

            Transformations();

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "Finished creating Abstract " + self);
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }

        #region Local
        void Abilities()
        {           
            if (ShadowOfOptions.tongue_ability.Value && ModManager.MSC && self.creatureTemplate.type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.TrainLizard)
            {
                data.liz["Tongue"] = "Null";
            } //Tongue: Set inside Lizard
        }
        void Transformations()
        {
            if (data.transformation == "Start")
            {
                int chance = UnityEngine.Random.Range(0, 100);

                if (ShadowOfOptions.spider_transformation.Value && chance < ShadowOfOptions.spawn_spider_transformation_chance.Value)
                {
                    data.transformation = "SpiderTransformation";
                    data.spiderLikness = 2;

                    data.liz["SpiderNumber"] = UnityEngine.Random.Range(30, 55).ToString();

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self + " gained the Spider Transformation due to Chance");

                    return;
                }
                else if (ShadowOfOptions.electric_transformation.Value && !electricPorhibited.Contains(self.creatureTemplate.type.ToString()) && chance < ShadowOfOptions.spawn_electric_transformation_chance.Value)
                {
                    data.transformation = "ElectricTransformation";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self + " gained the Electric Transformation due to Chance");

                    return;
                }
                else if (ShadowOfOptions.melted_transformation.Value && !meltedPorhibited.Contains(self.creatureTemplate.type.ToString()) && chance < ShadowOfOptions.spawn_melted_transformation_chance.Value)
                {
                    data.transformation = "MeltedTransformation";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self + " gained the Melted Transformation due to Chance");

                    return;
                }
                else
                {
                    data.transformation = "Null";
                }
            }
        }
        #endregion
    }
    static void CreatureStateLoadFromString(On.LizardState.orig_LoadFromString orig, LizardState self, string[] s)
    {
        orig(self, s);

        if (self.creature == null || self.creature.creatureTemplate == null || self.unrecognizedSaveStrings == null || self.creature.creatureTemplate.TopAncestor().type != CreatureTemplate.Type.LizardTemplate || !IsLizardValid(self.creature.creatureTemplate.type.ToString()) && self.unrecognizedSaveStrings.ContainsKey("ShadowOfbeheaded"))
        {
            return;
        }

        Dictionary<string, string> savedData = self.unrecognizedSaveStrings;

        if (!IsLizardValid(self.creature.creatureTemplate.type.ToString()))
        {
            if (savedData.ContainsKey("ShadowOfbeheaded"))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + "Removing saved info from " + self.creature + " due to it no longer being valid");

                savedData.Remove("ShadowOfbeheaded");

                if (savedData.ContainsKey("ShadowOfLiz"))
                {
                    savedData.Remove("ShadowOfLiz");
                }

                if (savedData.ContainsKey("ShadowOfarmState"))
                {
                    savedData.Remove("ShadowOfarmState");
                }

                if (savedData.ContainsKey("ShadowOfTransformation"))
                {
                    savedData.Remove("ShadowOfTransformation");
                }
                if (savedData.ContainsKey("ShadowOfTransformationTimer"))
                {
                    savedData.Remove("ShadowOfTransformationTimer");
                }

                if (savedData.ContainsKey("ShadowOfCheatDeathChance"))
                {
                    savedData.Remove("ShadowOfCheatDeathChance");
                }

                if (savedData.ContainsKey("ShadowOfLizardUpdatedCycle"))
                {
                    savedData.Remove("ShadowOfLizardUpdatedCycle");
                }

                if (savedData.ContainsKey("ShadowOfAvailableBodychunks"))
                {
                    savedData.Remove("ShadowOfAvailableBodychunks");
                }
                if (savedData.ContainsKey("ShadowOfCosmeticBodychunks"))
                {
                    savedData.Remove("ShadowOfCosmeticBodychunks");
                }

                if (savedData.ContainsKey("ShadowOfCutAppendage"))
                {
                    savedData.Remove("ShadowOfCutAppendage");
                }
                if (savedData.ContainsKey("ShadowOfCutAppendageCycle"))
                {
                    savedData.Remove("ShadowOfCutAppendageCycle");
                }
            }

            return;
        }

        if (!lizardstorage.TryGetValue(self.creature, out LizardData data))
        {
            lizardstorage.Add(self.creature, new());
            lizardstorage.TryGetValue(self.creature, out data);
        }

        bool firstTime;

        int cycleNumber = CycleNum(self.creature);

        string abstractAll = all + "Abstract " + self.creature;

        CreatureTemplate creatureTemplate = self.creature.creatureTemplate = new(self.creature.creatureTemplate);

        try
        {
            if (savedData.TryGetValue("ShadowOfbeheaded", out string beheaded))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + "Not the first time creating Abstract " + self.creature + " Loading values for Abstract");

                string Temp;

                if (beheaded == "Gore")
                {
                    savedData.Remove("ShadowOfbeheaded");

                    if (savedData.ContainsKey("ShadowOfLiz"))
                    {
                        string KeyTemp = "";
                        Temp = "";
                        for (int i = 0; i < savedData["ShadowOfLiz"].Length; i++)
                        {
                            char letter = savedData["ShadowOfLiz"][i];

                            if (letter.ToString() == "=")
                            {
                                KeyTemp = Temp;
                                Temp = "";
                            }
                            else if (letter.ToString() == ";")
                            {
                                if (!data.liz.ContainsKey(KeyTemp))
                                    data.liz[KeyTemp] = Temp;

                                KeyTemp = "";
                                Temp = "";
                            }
                            else
                            {
                                Temp += letter;
                            }
                        }
                        savedData.Remove("ShadowOfLiz");
                    }

                    if (savedData.ContainsKey("ShadowOfarmState"))
                    {
                        data.armState.Clear();

                        Temp = "";
                        for (int i = 0; i < savedData["ShadowOfarmState"].Length; i++)
                        {
                            char letter = savedData["ShadowOfarmState"][i];

                            if (letter.ToString() == ";")
                            {
                                data.armState.Add(Temp);

                                Temp = "";
                            }
                            else
                            {
                                Temp += letter;
                            }
                        }
                        savedData.Remove("ShadowOfarmState");
                    }

                    if (savedData.ContainsKey("ShadowOfTransformation"))
                    {
                        data.transformation = savedData["ShadowOfTransformation"];
                        savedData.Remove("ShadowOfTransformation");
                    }

                    if (savedData.ContainsKey("ShadowOfAvailableBodychunks"))
                    {
                        data.availableBodychunks.Clear();

                        Temp = "";
                        for (int i = 0; i < savedData["ShadowOfAvailableBodychunks"].Length; i++)
                        {
                            char letter = savedData["ShadowOfAvailableBodychunks"][i];

                            if (letter.ToString() == ";")
                            {
                                data.availableBodychunks.Add(int.Parse(Temp));
                                Temp = "";
                            }
                            else
                            {
                                Temp += letter;
                            }
                        }
                        savedData.Remove("ShadowOfAvailableBodychunks");
                    }
                    if (savedData.ContainsKey("ShadowOfCosmeticBodychunks"))
                    {
                        if (ShadowOfOptions.cosmetic_body_chunks.Value)
                        {
                            data.cosmeticBodychunks.Clear();

                            Temp = "";
                            for (int i = 0; i < savedData["ShadowOfCosmeticBodychunks"].Length; i++)
                            {
                                char letter = savedData["ShadowOfCosmeticBodychunks"][i];

                                if (letter.ToString() == ";")
                                {
                                    data.cosmeticBodychunks.Add(int.Parse(Temp));
                                    Temp = "";
                                }
                                else
                                {
                                    Temp += letter;
                                }
                            }
                        }

                        savedData.Remove("ShadowOfCosmeticBodychunks");
                    }

                    data.actuallyDead = true;

                    if (ShadowOfOptions.debug_logs.Value)
                    {
                        Debug.Log(all + self.creature + " beheaded = " + data.beheaded);

                        for (int i = 0; i < data.availableBodychunks.Count; i++)
                        {
                            Debug.Log(all + self.creature + " bodyChunks = " + data.availableBodychunks[i]);
                        }

                        for (int i = 0; i < data.cosmeticBodychunks.Count; i++)
                        {
                            Debug.Log(all + self.creature + " cosmeticBodyChunks = " + data.cosmeticBodychunks[i]);
                        }

                        Debug.Log(all + self.creature + " transformation = " + data.transformation);

                        for (int i = 0; i < data.armState.Count; i++)
                        {
                            Debug.Log(all + self.creature + " armState = " + data.armState[i]);
                        }
                    }

                    return;
                }

                data.beheaded = beheaded == "True";
                savedData.Remove("ShadowOfbeheaded");

                data.actuallyDead = beheaded == "True";

                if (savedData.ContainsKey("ShadowOfactuallyDead"))
                {
                    data.actuallyDead = savedData["ShadowOfactuallyDead"] == "True";
                    savedData.Remove("ShadowOfactuallyDead");
                }

                if (savedData.ContainsKey("ShadowOfLiz"))
                {
                    string KeyTemp = "";
                    Temp = "";
                    for (int i = 0; i < savedData["ShadowOfLiz"].Length; i++)
                    {
                        char letter = savedData["ShadowOfLiz"][i];

                        if (letter.ToString() == "=")
                        {
                            KeyTemp = Temp;
                            Temp = "";
                        }
                        else if (letter.ToString() == ";")
                        {
                            if (!data.liz.ContainsKey(KeyTemp))
                                data.liz[KeyTemp] = Temp;

                            KeyTemp = "";
                            Temp = "";
                        }
                        else
                        {
                            Temp += letter;
                        }
                    }
                    savedData.Remove("ShadowOfLiz");
                }

                if (savedData.ContainsKey("ShadowOfarmState"))
                {
                    data.armState.Clear();

                    Temp = "";
                    for (int i = 0; i < savedData["ShadowOfarmState"].Length; i++)
                    {
                        char letter = savedData["ShadowOfarmState"][i];

                        if (letter.ToString() == ";")
                        {
                            data.armState.Add(Temp);

                            Temp = "";
                        }
                        else
                        {
                            Temp += letter;
                        }
                    }
                    savedData.Remove("ShadowOfarmState");
                }

                if (savedData.ContainsKey("ShadowOfTransformation"))
                {
                    data.transformation = savedData["ShadowOfTransformation"];
                    savedData.Remove("ShadowOfTransformation");
                }
                if (savedData.ContainsKey("ShadowOfTransformationTimer"))
                {
                    data.transformationTimer = int.Parse(savedData["ShadowOfTransformationTimer"]);
                    savedData.Remove("ShadowOfTransformationTimer");
                }

                if (savedData.ContainsKey("ShadowOfCheatDeathChance"))
                {
                    data.cheatDeathChance = int.Parse(savedData["ShadowOfCheatDeathChance"]);
                    savedData.Remove("ShadowOfCheatDeathChance");
                }
                else
                {
                    if (ShadowOfOptions.dynamic_cheat_death.Value)
                    {
                        data.cheatDeathChance = UnityEngine.Random.Range(0, 101) + ShadowOfOptions.cheat_death_chance.Value;

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.creature + " got " + data.cheatDeathChance + " Chance to Cheat Death due to Dynamic Death Chance being On.");
                    }
                    else
                    {
                        data.cheatDeathChance = ShadowOfOptions.cheat_death_chance.Value;

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.creature + " got a flat " + data.cheatDeathChance + " Chance to Cheat Death due to Dynamic Death Chance being Off.");
                    }
                }

                if (savedData.ContainsKey("ShadowOfLizardUpdatedCycle"))
                {
                    data.lizardUpdatedCycle = int.Parse(savedData["ShadowOfLizardUpdatedCycle"]);
                    savedData.Remove("ShadowOfLizardUpdatedCycle");
                }

                List<int> TempAvailableBodychunks = new();

                if (savedData.ContainsKey("ShadowOfAvailableBodychunks"))
                {
                    data.availableBodychunks.Clear();

                    Temp = "";
                    for (int i = 0; i < savedData["ShadowOfAvailableBodychunks"].Length; i++)
                    {
                        char letter = savedData["ShadowOfAvailableBodychunks"][i];

                        if (letter.ToString() == ";")
                        {
                            data.availableBodychunks.Add(int.Parse(Temp));

                            TempAvailableBodychunks.Add(int.Parse(Temp));
                            Temp = "";
                        }
                        else
                        {
                            Temp += letter;
                        }
                    }
                    savedData.Remove("ShadowOfAvailableBodychunks");
                }
                if (savedData.ContainsKey("ShadowOfCosmeticBodychunks"))
                {
                    if (ShadowOfOptions.cosmetic_body_chunks.Value)
                    {
                        data.cosmeticBodychunks.Clear();

                        Temp = "";
                        for (int i = 0; i < savedData["ShadowOfCosmeticBodychunks"].Length; i++)
                        {
                            char letter = savedData["ShadowOfCosmeticBodychunks"][i];

                            if (letter.ToString() == ";")
                            {
                                data.cosmeticBodychunks.Add(int.Parse(Temp));
                                Temp = "";
                            }
                            else
                            {
                                Temp += letter;
                            }
                        }
                    }

                    savedData.Remove("ShadowOfCosmeticBodychunks");
                }

                if (ModManager.Watcher)
                {
                    if (savedData.ContainsKey("ShadowOfCutAppendage"))
                    {
                        string KeyTemp = "";
                        Temp = "";
                        for (int i = 0; i < savedData["ShadowOfCutAppendage"].Length; i++)
                        {
                            char letter = savedData["ShadowOfCutAppendage"][i];

                            if (letter.ToString() == "=")
                            {
                                KeyTemp = Temp;
                                Temp = "";
                            }
                            else if (letter.ToString() == ";")
                            {
                                if (!data.liz.ContainsKey(KeyTemp))
                                    data.liz[KeyTemp] = Temp;

                                KeyTemp = "";
                                Temp = "";
                            }
                            else
                            {
                                Temp += letter;
                            }
                        }
                        savedData.Remove("ShadowOfCutAppendage");
                    }
                    if (savedData.ContainsKey("ShadowOfCutAppendageCycle"))
                    {
                        string KeyTemp = "";
                        Temp = "";
                        for (int i = 0; i < savedData["ShadowOfCutAppendageCycle"].Length; i++)
                        {
                            char letter = savedData["ShadowOfCutAppendageCycle"][i];

                            if (letter.ToString() == "=")
                            {
                                KeyTemp = Temp;
                                Temp = "";
                            }
                            else if (letter.ToString() == ";")
                            {
                                if (!data.liz.ContainsKey(KeyTemp))
                                    data.liz[KeyTemp] = Temp;

                                KeyTemp = "";
                                Temp = "";
                            }
                            else
                            {
                                Temp += letter;
                            }
                        }
                        savedData.Remove("ShadowOfCutAppendageCycle");
                    }
                }
                else
                {
                    if (savedData.ContainsKey("ShadowOfCutAppendage"))
                    {
                        savedData.Remove("ShadowOfCutAppendage");
                    }
                    if (savedData.ContainsKey("ShadowOfCutAppendageCycle"))
                    {
                        savedData.Remove("ShadowOfCutAppendageCycle");
                    }
                }

                if (ShadowOfOptions.debug_logs.Value)
                {
                    Debug.Log(all + self.creature + " beheaded = " + data.beheaded);

                    for (int i = 0; i < data.availableBodychunks.Count; i++)
                    {
                        Debug.Log(all + self.creature + " bodyChunks = " + data.availableBodychunks[i]);
                    }

                    Debug.Log(all + self.creature + " transformation = " + data.transformation);
                    Debug.Log(all + self.creature + " transformationTimer = " + data.transformationTimer);

                    for (int i = 0; i < data.armState.Count; i++)
                    {
                        Debug.Log(all + self.creature + " armState = " + data.armState[i]);
                    }

                    Debug.Log(all + self.creature + " updatedCycle = " + data.lizardUpdatedCycle);
                    Debug.Log(all + self.creature + " cheatDeathChance = " + data.cheatDeathChance);

                    if (ModManager.Watcher)
                    {
                        if(data.cutAppendage.Count > 0)
                            Debug.Log(all + self.creature + " cutAppendage = " + data.cutAppendage);
                        if (data.cutAppendageCycle.Count > 0)
                            Debug.Log(all + self.creature + " cutAppendageCycle = " + data.cutAppendageCycle);
                    }

                    Debug.Log(all + self.creature + " actuallyDead = " + data.actuallyDead);
                }

                if (!TempAvailableBodychunks.Contains(0) && !data.actuallyDead)
                {                   
                    if (ShadowOfOptions.blind.Value && data.liz.ContainsKey("EyeRight") && data.liz["EyeRight"] != "Incompatible")
                    {
                        data.liz["EyeLeft"] = "Normal";
                        data.liz["EyeRight"] = "Normal";
                    } //Blind: Set inside Lizard
                    if (ShadowOfOptions.deafen.Value && data.liz.ContainsKey("EarRight"))
                    {
                        data.liz["EarLeft"] = "Normal";
                        data.liz["EarRight"] = "Normal";
                    } //Deaf: Set inside Lizard
                    if (ShadowOfOptions.teeth.Value && data.liz.ContainsKey("UpperTeeth") && data.liz["UpperTeeth"] != "Incompatible")
                    {
                        data.liz["UpperTeeth"] = "Normal";
                        data.liz["LowerTeeth"] = "Normal";
                    } //Teeth: Set inside Lizard
                }

                if (ShadowOfOptions.cut_in_half.Value && ShadowOfOptions.cut_in_half_regrowth.Value && ShadowOfOptions.dismemberment.Value && !TempAvailableBodychunks.Contains(2) && (!ModManager.DLCShared || creatureTemplate.type != DLCSharedEnums.CreatureTemplateType.EelLizard) && !data.actuallyDead)
                {
                    data.armState[2] = "Normal";
                    data.armState[3] = "Normal";

                    if (!data.availableBodychunks.Contains(2))
                    {
                        data.availableBodychunks.Add(2);
                    }
                }
                
                if (ShadowOfOptions.decapitation.Value && data.beheaded && !data.actuallyDead)
                {
                    if (!data.liz.TryGetValue("beheadedCycle", out string beheadedCycle))
                    {
                        data.liz["beheadedCycle"] = cycleNumber.ToString();
                    }
                    else if (beheadedCycle != cycleNumber.ToString())
                    {
                        data.beheaded = false;

                        data.liz.Remove("beheadedCycle");

                        if (data.liz.ContainsKey("BeheadedSprite"))
                            data.liz.Remove("BeheadedSprite");

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.creature + " gained back it's head");
                    }
                } //Add Head Back if next cycle
                else if (data.liz.ContainsKey("beheadedCycle"))
                {
                    data.liz.Remove("beheadedCycle");
                }
            } //Loads info from the Lizard

            firstTime = data.lizardUpdatedCycle != (self.creature.world.game.IsStorySession ? cycleNumber : 0);

            Abilities();

            Immunities();

            Physical();

            Transformations();

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "Finished creating Abstract " + self.creature);

            data.lizardUpdatedCycle = cycleNumber;
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }

        #region Local
        void Abilities()
        {           
            if (ShadowOfOptions.tongue_ability.Value && firstTime && data.liz.ContainsKey("Tongue"))
            {
                bool tongue = data.liz.TryGetValue("Tongue", out string Tongue) && Tongue != "Null";

                if (tongue && data.liz["Tongue"] == "get")
                {
                    int num = UnityEngine.Random.Range(0, 7);
                    data.liz["Tongue"] = (validTongues[num].Contains(creatureTemplate.type.ToString()) && UnityEngine.Random.value < 0.5) ? creatureTemplate.type.ToString() : validTongues[num];

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(abstractAll + " got new " + data.liz["Tongue"] + " Tongue");
                }

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(abstractAll + (tongue ? " has a " + data.liz["Tongue"] + " Tongue" : " does not have a Tongue"));

                if (data.liz["Tongue"] == creatureTemplate.type.ToString())
                    data.liz.Remove("Tongue");
            } //Tongue: Set inside Lizard
            else if (!ShadowOfOptions.tongue_ability.Value && data.liz.ContainsKey("Tongue"))
                data.liz.Remove("Tongue");
            
            if (ShadowOfOptions.jump_ability.Value && firstTime && data.liz.TryGetValue("CanJump", out string CanJump) && ShadowOfOptions.debug_logs.Value)
            {
                AbilityLog(CanJump == "True", "Jump");
            } //Jump: Set inside Lizard
            else if (!ShadowOfOptions.jump_ability.Value && data.liz.ContainsKey("CanJump"))
                data.liz.Remove("CanJump");
           
            if (ShadowOfOptions.swim_ability.Value && firstTime && data.liz.TryGetValue("CanSwim", out string CanSwim))
            {
                if (CanSwim == "True" && defaultWaterBreather.Contains(creatureTemplate.type.ToString()) || CanSwim != "True" && !defaultWaterBreather.Contains(creatureTemplate.type.ToString()))
                    data.liz.Remove("CanSwim");

                if (ShadowOfOptions.debug_logs.Value)
                    AbilityLog(CanSwim == "True", "Swim");
            } //Can Swim: Set inside Lizard
            else if (!ShadowOfOptions.swim_ability.Value && data.liz.ContainsKey("CanSwim"))
                data.liz.Remove("CanSwim");
            
            if (ShadowOfOptions.climb_ability.Value && firstTime)
            {
                bool canClimb;
                if (data.liz.TryGetValue("CanClimbPole", out string CanClimbPole))
                {
                    canClimb = CanClimbPole == "True";

                    if (self.creature.creatureTemplate.pathingPreferencesTiles[(int)AItile.Accessibility.Climb].legality == PathCost.Legality.Allowed == canClimb)
                    {
                        data.liz.Remove("CanClimbPole");
                    } //Remove value if it equals the Lizards actual climb ability
                    else if (canClimb)
                    {
                        List<TileTypeResistance> list = new()
                            {
                                new(AItile.Accessibility.Climb, 1f, PathCost.Legality.Allowed)
                            };

                        for (int l = 0; l < list.Count; l++)
                        {
                            self.creature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                            if (self.creature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                            {
                                self.creature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
                            }
                        }
                    }
                    else
                    {
                        List<TileTypeResistance> list = new()
                            {
                                new(AItile.Accessibility.Climb, 0f, PathCost.Legality.Unallowed)
                            };

                        for (int l = 0; l < list.Count; l++)
                        {
                            self.creature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                            if (self.creature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                            {
                                self.creature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
                            }
                        }
                    }

                    if (ShadowOfOptions.debug_logs.Value)
                        AbilityLog(canClimb, "Climb Poles");
                }
                if (data.liz.TryGetValue("CanClimbWall", out string CanClimbWall))
                {
                    canClimb = CanClimbWall == "True";

                    if (self.creature.creatureTemplate.pathingPreferencesTiles[(int)AItile.Accessibility.Wall].legality == PathCost.Legality.Allowed == canClimb)
                    {
                        data.liz.Remove("CanClimbWall");
                    } //Remove value if it equals the Lizards actual climb ability
                    else if (canClimb)
                    {
                        List<TileTypeResistance> list = new()
                            {
                                new(AItile.Accessibility.Wall, 1f, PathCost.Legality.Allowed)
                            };

                        for (int l = 0; l < list.Count; l++)
                        {
                            self.creature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                            if (self.creature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                            {
                                self.creature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
                            }
                        }
                    }
                    else
                    {
                        List<TileTypeResistance> list = new()
                            {
                                new(AItile.Accessibility.Wall, 0f, PathCost.Legality.Unallowed)
                            };

                        for (int l = 0; l < list.Count; l++)
                        {
                            self.creature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                            if (self.creature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                            {
                                self.creature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
                            }
                        }
                    }

                    if (ShadowOfOptions.debug_logs.Value)
                        AbilityLog(canClimb, "Climb Walls");
                }
                if (data.liz.TryGetValue("CanClimbCeiling", out string CanClimbCeiling))
                {
                    canClimb = CanClimbCeiling == "True";

                    if (self.creature.creatureTemplate.pathingPreferencesTiles[(int)AItile.Accessibility.Ceiling].legality == PathCost.Legality.Allowed == canClimb)
                    {
                        data.liz.Remove("CanClimbCeiling");
                    } //Remove value if it equals the Lizards actual climb ability
                    else if (canClimb)
                    {
                        List<TileTypeResistance> list = new()
                            {
                                new(AItile.Accessibility.Ceiling, 1.2f, PathCost.Legality.Allowed)
                            };

                        for (int l = 0; l < list.Count; l++)
                        {
                            self.creature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                            if (self.creature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                            {
                                self.creature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
                            }
                        }
                    }
                    else
                    {
                        List<TileTypeResistance> list = new()
                            {
                                new(AItile.Accessibility.Ceiling, 0f, PathCost.Legality.Unallowed)
                            };

                        for (int l = 0; l < list.Count; l++)
                        {
                            self.creature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                            if (self.creature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                            {
                                self.creature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
                            }
                        }
                    }

                    if (ShadowOfOptions.debug_logs.Value)
                        AbilityLog(canClimb, "Climb Walls");
                }
            } //CanClimb: Set in Both
            else if (!ShadowOfOptions.climb_ability.Value)
            {
                if (data.liz.ContainsKey("CanClimbWall"))
                    data.liz.Remove("CanClimbPole");
                if (data.liz.ContainsKey("CanClimbWall"))
                    data.liz.Remove("CanClimbWall");
                if (data.liz.ContainsKey("CanClimbCeiling"))
                    data.liz.Remove("CanClimbCeiling");
            }
           
            if (ShadowOfOptions.camo_ability.Value && firstTime && data.liz.TryGetValue("CanCamo", out string CanCamo))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    AbilityLog(CanCamo == "True", "Camo");
            } //CanCamo
            else if (!ShadowOfOptions.camo_ability.Value && data.liz.ContainsKey("CanCamo"))
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
            if (ShadowOfOptions.debug_logs.Value && ShadowOfOptions.grass_immune.Value && firstTime && data.liz.TryGetValue("WormGrassImmune", out string WormGrassImmune))
            {
                ImmuneLog(WormGrassImmune == "True", "Worm Grass");
            } //WormGrassImmune: Set inside Lizard
            else if (!ShadowOfOptions.grass_immune.Value && data.liz.ContainsKey("WormGrassImmune"))
                data.liz.Remove("WormGrassImmune");
          
            if (ModManager.HypothermiaModule && ShadowOfOptions.hypothermia_immune.Value && firstTime && data.liz.TryGetValue("HypothermiaImmune", out string HypothermiaImmune))
            {
                bool hypothermia = HypothermiaImmune == "True";

                if (self.creature.HypothermiaImmune != hypothermia)
                {
                    self.creature.HypothermiaImmune = hypothermia;
                }
                else if (hypothermia || !int.TryParse(HypothermiaImmune, out int HypothermiaImmuneInt) || HypothermiaImmuneInt != CycleNum(self.creature))
                    data.liz.Remove("HypothermiaImmune");

                if (ShadowOfOptions.debug_logs.Value)
                    ImmuneLog(hypothermia, "Hypothermia");
            } //HypothermiaImmune: Set Here
            else if (!ShadowOfOptions.hypothermia_immune.Value && data.liz.ContainsKey("HypothermiaImmune"))
                data.liz.Remove("HypothermiaImmune");
           
            if (ShadowOfOptions.tentacle_immune.Value && firstTime && data.liz.TryGetValue("TentacleImmune", out string TentacleImmune))
            {
                self.creature.tentacleImmune = TentacleImmune == "True";

                if (ShadowOfOptions.debug_logs.Value)
                    ImmuneLog(self.creature.tentacleImmune, "Rot Tentacles");
            } //TentacleImmune: Set Here
            else if (!ShadowOfOptions.tentacle_immune.Value && data.liz.ContainsKey("TentacleImmune"))
                data.liz.Remove("TentacleImmune");
            
            if (ShadowOfOptions.lava_immune.Value && firstTime && data.liz.TryGetValue("LavaImmune", out string LavaImmune))
            {
                bool lava = LavaImmune == "True";

                if (self.creature.lavaImmune != lava)
                {
                    self.creature.lavaImmune = lava;
                }
                else if (lava || !int.TryParse(LavaImmune, out int LavaImmuneInt) || LavaImmuneInt != CycleNum(self.creature))
                    data.liz.Remove("LavaImmune");

                if (ShadowOfOptions.debug_logs.Value)
                    ImmuneLog(lava, "Lava/Acid");
            } //LavaImmune: Set Here
            else if (!ShadowOfOptions.lava_immune.Value && data.liz.ContainsKey("LavaImmune"))
                data.liz.Remove("LavaImmune");
            
            if (ShadowOfOptions.water_breather.Value && firstTime && data.liz.TryGetValue("WaterBreather", out string WaterBreather))
            {
                bool waterBreather = WaterBreather == "True";

                if (waterBreather == defaultWaterBreather.Contains(creatureTemplate.type.ToString()))
                    data.liz.Remove("WaterBreather");

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(abstractAll + (waterBreather ? " can " : " cannot ") + "Breathe Underwater");
            } //WaterBreather: Set Here
            else if (!ShadowOfOptions.water_breather.Value && data.liz.ContainsKey("WaterBreather"))
                data.liz.Remove("WaterBreather");

            void ImmuneLog(bool isImmune, string whatTo)
            {
                Debug.Log(abstractAll + (isImmune ? " is " : " is not ") + "Immune to " + whatTo);
            }
        }
        void Physical()
        {           
            if (!ShadowOfOptions.blind.Value && data.liz.ContainsKey("EyeRight"))
            {
                data.liz.Remove("EyeRight");
                data.liz.Remove("EyeLeft");
            } //Blind: Set inside Lizard
            if (!ShadowOfOptions.deafen.Value && data.liz.ContainsKey("EarRight"))
            {
                data.liz.Remove("EarRight");
                data.liz.Remove("EarLeft");
            } //Deaf: Set inside Lizard
            if (!ShadowOfOptions.teeth.Value && data.liz.ContainsKey("UpperTeeth"))
            {
                data.liz.Remove("UpperTeeth");
                data.liz.Remove("LowerTeeth");
            } //Teeth: Set inside Lizard
        }
        void Transformations()
        {
            if (!firstTime)
            {
                return;
            }
           
            if (ShadowOfOptions.debug_logs.Value && data.transformation.Contains("Rot")) 
            {
                Debug.Log(abstractAll + " has the Rot Transformation");
            } //Rot Transformation: Set inside Lizard

            if (ShadowOfOptions.spider_transformation.Value && (data.transformation == "Spider" || data.transformation == "SpiderTransformation"))
            {
                if (data.transformation == "Spider")
                {
                    data.spiderLikness = 1;
                    if (data.transformationTimer <= cycleNumber - 3 || data.transformationTimer >= cycleNumber + 3 || ShadowOfOptions.spider_transformation_skip.Value)
                    {
                        data.transformation = "SpiderTransformation";
                        data.spiderLikness = 2;

                        data.liz["SpiderNumber"] = UnityEngine.Random.Range(30, 55).ToString();

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self + " has gained the Spider Transformation");
                    }
                    else if (ShadowOfOptions.debug_logs.Value)
                    {
                        int num3 = data.transformationTimer - cycleNumber;
                        string cycles = (num3 < 0) ? (num3 * -1).ToString() : num3.ToString();
                        Debug.Log(abstractAll + " is Spider Mother for " + cycles + " cycle out of the required 3 cycles to gain the Spider Transformation");
                    }
                }
                else if (data.transformation == "SpiderTransformation")
                {
                    data.spiderLikness = 2;
                    for (int i = 0; i < data.armState.Count; i++)
                    {
                        if (data.armState[i] == "Cut1" || data.armState[i] == "Cut2")
                        {
                            data.armState[i] = "Spider";
                        }
                    }

                    data.liz["SpiderNumber"] = UnityEngine.Random.Range(30, 55).ToString();

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(abstractAll + " has the Spider Transformation");
                }
            } //Spider Transformation
            else if ((!ShadowOfOptions.spider_transformation.Value || data.transformation != "Spider" && data.transformation != "SpiderTransformation") && data.liz.ContainsKey("SpiderNumber"))
                data.liz.Remove("SpiderNumber");
            
            if (ShadowOfOptions.electric_transformation.Value && (data.transformation == "Electric" || data.transformation == "ElectricTransformation"))
            {
                if (electricPorhibited.Contains(self.creature.creatureTemplate.type.ToString()))
                {
                    data.transformation = "Null";
                    return;
                }

                if (data.transformationTimer <= 0)
                {
                    data.transformation = "Null";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self + " is no longer Electric due to running out of Charge");
                }
                else if (data.transformationTimer >= 3 || ShadowOfOptions.electric_transformation_skip.Value)
                {
                    data.transformation = "ElectricTransformation";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self + " has gained the Electric Transformation");
                }
                else if (ShadowOfOptions.debug_logs.Value && data.transformation == "ElectricTransformation")
                {
                    Debug.Log(abstractAll + " has the Electric Transformation");
                }
            } //Electric Transformation

            if (ShadowOfOptions.melted_transformation.Value && (data.transformation == "Melted" || data.transformation == "MeltedTransformation"))
            {
                if (meltedPorhibited.Contains(self.creature.creatureTemplate.type.ToString()))
                {
                    data.transformation = "Null";
                    return;
                }

                if (data.transformation == "Melted" && (data.transformationTimer <= cycleNumber - 3 || data.transformationTimer >= cycleNumber + 3 || ShadowOfOptions.melted_transformation_skip.Value))
                {
                    data.transformation = "MeltedTransformation";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self + " has gained the Melted Transformation");
                }
                else if (ShadowOfOptions.debug_logs.Value && data.transformation == "MeltedTransformation")
                {
                    Debug.Log(abstractAll + " has the Melted Transformation");
                }

                if (data.liz.ContainsKey("PreMeltedCycle"))
                    data.liz.Remove("PreMeltedCycle");
            } //Melted Transformation
            else if ((!ShadowOfOptions.melted_transformation.Value || meltedPorhibited.Contains(self.creature.creatureTemplate.type.ToString()) || data.transformation == "SpiderTransformation" || data.transformation == "ElectricTransformation") && data.liz.ContainsKey("PreMeltedCycle"))
                data.liz.Remove("PreMeltedCycle");
        }
        #endregion
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
                Debug.Log(all + "Saving values for " + (data.isGoreHalf ? "Gore Abstract " : "Abstract ") + self);

            Dictionary<string, string> savedData = self.state.unrecognizedSaveStrings;

            string liz;

            if (data.isGoreHalf)
            {
                savedData["ShadowOfbeheaded"] = "Gore";

                liz = "";
                for (int i = 0; i < data.liz.Count; i++)
                {
                    liz += data.liz.ElementAt(i).Key + "=";
                    liz += data.liz.ElementAt(i).Value + ";";
                }
                savedData["ShadowOfLiz"] = liz;

                liz = "";
                for (int i = 0; i < data.availableBodychunks.Count; i++)
                {
                    liz += data.availableBodychunks[i] + ";";
                }
                savedData["ShadowOfAvailableBodychunks"] = liz;

                if (ShadowOfOptions.cosmetic_body_chunks.Value)
                {
                    liz = "";
                    for (int i = 0; i < data.cosmeticBodychunks.Count; i++)
                    {
                        liz += data.cosmeticBodychunks[i] + ";";
                    }
                    savedData["ShadowOfCosmeticBodychunks"] = liz;
                }

                savedData["ShadowOfTransformation"] = data.transformation;

                liz = "";
                for (int i = 0; i < data.armState.Count; i++)
                {
                    liz += data.armState[i] + ";";
                }
                savedData["ShadowOfarmState"] = liz;

                if (ShadowOfOptions.debug_logs.Value)
                {
                    Debug.Log(all + self + " beheaded = " + savedData["ShadowOfbeheaded"]);

                    Debug.Log(all + self + " lizDictionary = " + savedData["ShadowOfLiz"]);

                    Debug.Log(all + self + " bodyChunks = " + savedData["ShadowOfAvailableBodychunks"]);
                    Debug.Log(all + self + " cosmeticBodyChunks = " + savedData["ShadowOfCosmeticBodychunks"]);

                    Debug.Log(all + self + " transformation = " + savedData["ShadowOfTransformation"]);

                    Debug.Log(all + self + " armState = " + savedData["ShadowOfarmState"]);
                }

                return orig(self, cc);
            }

            savedData["ShadowOfbeheaded"] = data.beheaded ? "True" : "False";

            liz = "";
            for (int i = 0; i < data.liz.Count; i++)
            {
                liz += data.liz.ElementAt(i).Key + "=";
                liz += data.liz.ElementAt(i).Value + ";";
            }
            savedData["ShadowOfLiz"] = liz;

            liz = "";
            for (int i = 0; i < data.availableBodychunks.Count; i++)
            {
                liz += data.availableBodychunks[i] + ";";
            }
            savedData["ShadowOfAvailableBodychunks"] = liz;

            if (ShadowOfOptions.cosmetic_body_chunks.Value)
            {
                List<int> list = new(data.cosmeticBodychunks);
                for (int i = 0; i < list.Count; i++)
                {
                    if (!data.availableBodychunks.Contains(list[i]))
                    {
                        data.cosmeticBodychunks.Remove(list[i]);
                    }
                }

                liz = "";
                for (int i = 0; i < data.cosmeticBodychunks.Count; i++)
                {
                    liz += data.cosmeticBodychunks[i] + ";";
                }
                savedData["ShadowOfCosmeticBodychunks"] = liz;
            }

            savedData["ShadowOfTransformation"] = data.transformation;
            savedData["ShadowOfTransformationTimer"] = data.transformationTimer.ToString();

            string armState = "";
            for (int i = 0; i < data.armState.Count; i++)
            {
                armState += data.armState[i] + ";";
            }
            savedData["ShadowOfarmState"] = armState;

            savedData["ShadowOfLizardUpdatedCycle"] = data.lizardUpdatedCycle.ToString();

            savedData["ShadowOfCheatDeathChance"] = data.cheatDeathChance.ToString();

            savedData["ShadowOfactuallyDead"] = data.actuallyDead ? "True" : "False";

            if (ModManager.Watcher)
            {
                if (data.cutAppendage.Count > 0)
                {
                    liz = "";
                    for (int i = 0; i < data.liz.Count; i++)
                    {
                        liz += data.cutAppendage.ElementAt(i).Key + "=";
                        liz += data.cutAppendage.ElementAt(i).Value + ";";
                    }
                    savedData["ShadowOfCutAppendage"] = liz;
                }
                if (data.cutAppendageCycle.Count > 0)
                {
                    liz = "";
                    for (int i = 0; i < data.liz.Count; i++)
                    {
                        liz += data.cutAppendageCycle.ElementAt(i).Key + "=";
                        liz += data.cutAppendageCycle.ElementAt(i).Value + ";";
                    }
                    savedData["ShadowOfCutAppendageCycle"] = liz;
                }
            }

            if (ShadowOfOptions.debug_logs.Value)
            {
                Debug.Log(all + self + " beheaded = " + savedData["ShadowOfbeheaded"]);
                Debug.Log(all + self + " lizDictionary = " + savedData["ShadowOfLiz"]);

                if (ShadowOfOptions.cosmetic_body_chunks.Value)
                    Debug.Log(all + self + " cosmeticBodyChunks = " + savedData["ShadowOfCosmeticBodychunks"]);

                Debug.Log(all + self + " bodyChunks = " + savedData["ShadowOfAvailableBodychunks"]);

                Debug.Log(all + self + " transformation = " + savedData["ShadowOfTransformation"]);
                Debug.Log(all + self + " transformationTimer = " + savedData["ShadowOfTransformationTimer"]);

                Debug.Log(all + self + " armState = " + savedData["ShadowOfarmState"]);

                Debug.Log(all + self + " updatedCycle = " + savedData["ShadowOfLizardUpdatedCycle"]);

                Debug.Log(all + self + " cheatDeathChance = " + savedData["ShadowOfCheatDeathChance"]);

                if (ModManager.Watcher)
                {
                    if (savedData.ContainsKey("ShadowOfCutAppendage"))
                        Debug.Log(all + self + " cutAppendage = " + savedData["ShadowOfCutAppendage"]);

                    if (savedData.ContainsKey("ShadowOfCutAppendageCycle"))
                        Debug.Log(all + self + " cutAppendageCycle = " + savedData["ShadowOfCutAppendageCycle"]);
                }
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }

        return orig(self, cc);
    }
    #endregion

    #region Lizard
    static void NewLizard(On.Lizard.orig_ctor orig, Lizard self, AbstractCreature abstractCreature, World world)
    {
        orig(self, abstractCreature, world);

        if (abstractCreature.state == null || abstractCreature.state.unrecognizedSaveStrings == null || !lizardstorage.TryGetValue(abstractCreature, out LizardData data))
        {
            return;
        }

        string lizardAll = all + self;

        if (ModManager.Watcher && self.LizardState.rotType == LizardState.RotType.None && (data.transformation.Contains("Rot") || ShadowOfOptions.tentacle_immune.Value && data.liz.TryGetValue("TentacleImmune", out string TentacleImmune) && TentacleImmune == "True"))
        {
            self.LizardState.SetRotType(new("Slight", false));

            self.rotModule ??= new(self);
        }

        if (data.isGoreHalf)
        {
            self.Die();

            if (ShadowOfOptions.tongue_ability.Value && data.liz.ContainsKey("Tongue"))
            {
                self.lizardParams.tongue = self.tongue != null;
            } //Tongue: Set Here

            Transformations();

            return;
        }

        try
        {
            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "Started creating " + self);

            self.lizardParams = LizBread(self);

            data.visualRadius = self.Template.visualRadius;
            data.waterVision = self.Template.waterVision;
            data.throughSurfaceVision = self.Template.throughSurfaceVision;

            Abilities();

            Immunities();

            Physical();

            Transformations();

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + "Finished creating " + self);
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }

        #region Local
        void Abilities()
        {            
            if (ShadowOfOptions.tongue_ability.Value && data.liz.TryGetValue("Tongue", out string Tongue))
            {
                self.lizardParams.tongue = Tongue != "Null";

                if (self.lizardParams.tongue && data.liz["Tongue"] != "get")
                {
                    self.tongue = null;

                    BreedTongue(self);
                    self.tongue = new(self);
                }
                else
                {
                    self.tongue = null;

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(lizardAll + " does not have a Tongue");
                }
            } //Tongue: Set Here

            if (ShadowOfOptions.jump_ability.Value && data.liz.TryGetValue("CanJump", out string CanJump))
            {
                bool canJump = CanJump == "True";

                if (self.jumpModule != null != canJump)
                {
                    self.jumpModule = canJump ? new(self) : null;
                }
                else
                    data.liz.Remove("CanJump");
            } //CanJump: Set Here

            if (ShadowOfOptions.swim_ability.Value && data.liz.TryGetValue("CanSwim", out string CanSwim))
            {
                bool canSwim = CanSwim == "True";

                bool waterRelationship = abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.Amphibious || abstractCreature.creatureTemplate.waterRelationship == CreatureTemplate.WaterRelationship.WaterOnly;

                if (self.Template.canSwim != canSwim || waterRelationship != canSwim)
                {
                    if (canSwim)
                    {
                        self.Template.canSwim = true;

                        if (!waterRelationship)
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
                                new(MovementConnection.MovementType.DropToWater, 20f, PathCost.Legality.Allowed)
                            };
                            for (int n = 0; n < list2.Count; n++)
                            {
                                abstractCreature.creatureTemplate.pathingPreferencesConnections[(int)list2[n].movementType] = list2[n].cost;
                            }
                        }
                    }
                    else
                    {
                        if (waterRelationship)
                        {
                            abstractCreature.creatureTemplate.waterRelationship = CreatureTemplate.WaterRelationship.AirAndSurface;
                        }
                        abstractCreature.creatureTemplate.waterPathingResistance *= 2;

                        PathCost dropToWater = abstractCreature.creatureTemplate.pathingPreferencesConnections[(int)MovementConnection.MovementType.DropToWater];
                        if (dropToWater.legality == PathCost.Legality.Allowed)
                        {
                            abstractCreature.creatureTemplate.pathingPreferencesConnections[(int)MovementConnection.MovementType.DropToWater].resistance *= 2;
                        }
                    }
                }
                else
                    data.liz.Remove("CanSwim");
            } //CanSwim: Set Here

            if (ShadowOfOptions.climb_ability.Value)
            {
                if (data.liz.TryGetValue("CanClimbPole", out string CanClimbPole))
                {
                    self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Climb] = CanClimbPole == "True" ? new(self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].speed * 0.8f, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].horizontal, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].up, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].down) : new(0, 1f, 1f, 1f);
                }
                if (data.liz.TryGetValue("CanClimbWall", out string CanClimbWall))
                {
                    self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Wall] = CanClimbWall == "True" ? new(self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].speed * 0.6f, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].horizontal, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].up, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].down) : new(0, 1f, 1f, 1f);
                }
                if (data.liz.TryGetValue("CanClimbCeiling", out string CanClimbCeiling))
                {
                    self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Ceiling] = CanClimbCeiling == "True" ? new(self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Wall].speed != 0f ? self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Wall].speed * 0.9f : self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].speed * 0.6f, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].horizontal, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].up, self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Floor].down) : new(0, 1f, 1f, 1f);
                }
            } //CanClimb: Set in Both
        }
        void Immunities()
        {            
            if (ShadowOfOptions.grass_immune.Value && data.liz.TryGetValue("WormGrassImmune", out string WormGrassImmune))
            {
                self.Template.wormGrassImmune = WormGrassImmune == "True";
            } //WormGrassImmune: Set here
            if (ModManager.HypothermiaModule && ShadowOfOptions.hypothermia_immune.Value && data.liz.TryGetValue("HypothermiaImmune", out string HypothermiaImmune))
            {
                self.Template.BlizzardWanderer = HypothermiaImmune == "True";
            } //HypothermiaImmune: Set inside AbstractLizard
            //TentacleImmune: Set inside AbstractLizard
            //LavaImmune: Set inside AbstractLizard
            //WaterBreather: Set inside AbstractLizard
        }
        void Physical()
        {          
            if (ShadowOfOptions.blind.Value && self.Template.visualRadius > 0f)
            {
                if (!data.liz.ContainsKey("EyeRight"))
                {
                    data.liz["EyeLeft"] = "Normal";
                    data.liz["EyeRight"] = "Normal";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(lizardAll + " can see normally");
                }
                else if (data.liz["EyeRight"] == "Incompatible")
                {
                    Debug.Log(all + "Eye sprites of " + self + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                    ShadowOfLizards.Logger.LogError(all + "Eye sprites of " + self + " are Incompatible, if able please report to the mod author of Shadow Of Lizards");
                }
                else
                {
                    bool eyeRightBlind = IsEyeBlind(data.liz["EyeRight"]);
                    bool eyeLeftBlind = IsEyeBlind(data.liz["EyeLeft"]);

                    if (eyeRightBlind && eyeLeftBlind)
                    {
                        self.Template.visualRadius = 0f;
                        self.Template.waterVision = 0f;
                        self.Template.throughSurfaceVision = 0f;

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(lizardAll + " is Blind");
                    }
                    else
                    {
                        float sightMultiplier = GetSightMultiplier(data.liz["EyeRight"]) + GetSightMultiplier(data.liz["EyeLeft"]);

                        self.Template.visualRadius = data.visualRadius * sightMultiplier;
                        self.Template.waterVision = data.waterVision * sightMultiplier;
                        self.Template.throughSurfaceVision = data.throughSurfaceVision * sightMultiplier;

                        if (ShadowOfOptions.debug_logs.Value && (eyeRightBlind ^ eyeLeftBlind))
                            Debug.Log(lizardAll + " is half Blind");
                    }
                }
            } //Blind: Set Here

            if (ShadowOfOptions.deafen.Value)
            {
                if (!data.liz.ContainsKey("EarRight"))
                {
                    data.liz["EarLeft"] = "Normal";
                    data.liz["EarRight"] = "Normal";
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(lizardAll + " is not Deaf");
                }
                else
                {
                    bool earRightDeaf = data.liz["EarRight"] == "Deaf";
                    bool earLeftDeaf = data.liz["EarLeft"] == "Deaf";
                    if (earRightDeaf && earLeftDeaf)
                    {
                        self.deaf = 120;
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(lizardAll + " is Deaf");
                    }
                    else if (earRightDeaf ^ earLeftDeaf)
                    {
                        self.deaf = 4;
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(lizardAll + " is Half-Deaf");
                    }
                }
            } //Deaf: Set Here

            if (ShadowOfOptions.teeth.Value)
            {
                if (!data.liz.ContainsKey("UpperTeeth"))
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
                    if (data.liz["UpperTeeth"] != "Normal" && data.liz["LowerTeeth"] != "Normal")
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
            } //Teeth: Set Here
        }
        void Transformations()
        {           
            if (ModManager.Watcher && self.LizardState.rotType != LizardState.RotType.None)
            {
                if (ShadowOfOptions.tentacle_immune.Value && (!data.liz.TryGetValue("TentacleImmune", out string TentacleImmune) || TentacleImmune != "True"))
                {
                    data.liz["TentacleImmune"] = "True";
                    self.abstractCreature.tentacleImmune = true;

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(lizardAll + "'s Rot Transformation has overridden TentacleImmune to True");
                }

                if (data.transformation != "Rot" + self.LizardState.rotType.ToString() && self.LizardState.rotType != LizardState.RotType.Slight)
                {
                    data.transformation = "Rot" + self.LizardState.rotType.ToString();

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(lizardAll + "'s Rot Transformation has overridden the Transformation to " + data.transformation);
                }
                return;
            } //Rot Transformation: Set Here
            //Spider Transformation: Set inside AbstractCreature
            //Electric Transformation: Set inside AbstractCreature
            
            if (ShadowOfOptions.melted_transformation.Value && (data.transformation == "Melted" || data.transformation == "MeltedTransformation"))
            {
                TransformationMelted.NewMeltedLizard(self, world, data);
            } //Melted Transformation: Set inside AbstractCreature
        }
        LizardBreedParams LizBread(Lizard liz)
        {
            LizardBreedParams lizardParams = liz.lizardParams;
            return new(liz.abstractCreature.creatureTemplate.type)
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
            if (!data.liz.TryGetValue("Tongue", out string Tongue))
            {
                return;
            }

            self.lizardParams.tongue = true;

            switch (Tongue)
            {
                case "WhiteLizard":
                    self.lizardParams.tongueAttackRange = 440f;
                    self.lizardParams.tongueWarmUp = 80;
                    self.lizardParams.tongueSegments = 10;
                    self.lizardParams.tongueChance = 0.1f;
                    break;
                case "Salamander":
                    self.lizardParams.tongueAttackRange = 150f;
                    self.lizardParams.tongueWarmUp = 8;
                    self.lizardParams.tongueSegments = 7;
                    self.lizardParams.tongueChance = 1f / 3f;
                    break;
                case "BlueLizard":
                    self.lizardParams.tongueAttackRange = 140f;
                    self.lizardParams.tongueWarmUp = 10;
                    self.lizardParams.tongueSegments = 5;
                    self.lizardParams.tongueChance = 0.25f;
                    break;
                case "CyanLizard":
                    self.lizardParams.tongueAttackRange = 160f;
                    self.lizardParams.tongueWarmUp = 8;
                    self.lizardParams.tongueSegments = 7;
                    self.lizardParams.tongueChance = 1f / 3f;
                    break;
                case "RedLizard":
                    self.lizardParams.tongueAttackRange = 350f;
                    self.lizardParams.tongueWarmUp = 8;
                    self.lizardParams.tongueSegments = 10;
                    self.lizardParams.tongueChance = 0.1f;
                    break;
                case "ZoopLizard":
                    self.lizardParams.tongueAttackRange = 440f;
                    self.lizardParams.tongueWarmUp = 140;
                    self.lizardParams.tongueSegments = 10;
                    self.lizardParams.tongueChance = 0.3f;
                    break;
                case "Tube":
                    self.lizardParams.tongueAttackRange = 200f;
                    self.lizardParams.tongueWarmUp = 0;
                    self.lizardParams.tongueSegments = 20;
                    self.lizardParams.tongueChance = 0.3f;
                    break;
                case "IndigoLizard":
                    self.lizardParams.tongueAttackRange = 190f;
                    self.lizardParams.tongueWarmUp = 8;
                    self.lizardParams.tongueSegments = 7;
                    self.lizardParams.tongueChance = 0.33333334f;
                    break;
                case "PeachLizard":
                    self.lizardParams.tongueAttackRange = 160f;
                    self.lizardParams.tongueWarmUp = 8;
                    self.lizardParams.tongueSegments = 7;
                    self.lizardParams.tongueChance = 0.33333334f;
                    break;
                case "NoodleEater":
                    self.lizardParams.tongueAttackRange = 350f;
                    self.lizardParams.tongueWarmUp = 2;
                    self.lizardParams.tongueSegments = 7;
                    self.lizardParams.tongueChance = 0.6f;
                    break;
                case "Polliwog":
                    self.lizardParams.tongueAttackRange = 150f;
                    self.lizardParams.tongueWarmUp = 8;
                    self.lizardParams.tongueSegments = 7;
                    self.lizardParams.tongueChance = 0.3333333f;
                    break;
                case "HunterSeeker":
                    self.lizardParams.tongueAttackRange = 440f;
                    self.lizardParams.tongueWarmUp = 80;
                    self.lizardParams.tongueSegments = 10;
                    self.lizardParams.tongueChance = 0.1f;
                    break;
                case "MoleSalamander":
                    self.lizardParams.tongueAttackRange = 150f;
                    self.lizardParams.tongueWarmUp = 8;
                    self.lizardParams.tongueSegments = 7;
                    self.lizardParams.tongueChance = 0.3333333f;
                    break;
                default:
                    Debug.Log(all + "Failed Getting the " + Tongue + " Tongue for " + self);
                    ShadowOfLizards.Logger.LogError(all + "Failed Getting the " + Tongue + " Tongue for " + self);
                    self.lizardParams.tongueAttackRange = 140f;
                    self.lizardParams.tongueWarmUp = 10;
                    self.lizardParams.tongueSegments = 5;
                    self.lizardParams.tongueChance = 0.25f;
                    break;
            }
        }
        #endregion
    }
    static void LizardBite(On.Lizard.orig_Bite orig, Lizard self, BodyChunk chunk)
    {
        if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data))
        {
            orig(self, chunk);
            return;
        }

        try
        {
            if (ShadowOfOptions.teeth.Value && data.liz.TryGetValue("UpperTeeth", out string upperTeeth))
            {
                string lowerTeeth = data.liz["LowerTeeth"];

                if (upperTeeth != "Normal" && lowerTeeth != "Normal")
                {
                    if (UnityEngine.Random.value < 0.4f)
                    {
                        if (ModManager.MSC && self.Template.type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.TrainLizard && self.room != null)
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
                else if (upperTeeth != "Normal" || lowerTeeth != "Normal")
                {
                    if (UnityEngine.Random.value < 0.7f)
                    {
                        if (ModManager.MSC && self.Template.type == MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType.TrainLizard && self.room != null)
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

            if (ShadowOfOptions.electric_transformation.Value && (data.transformation == "Electric" || data.transformation == "ElectricTransformation") && self.graphicsModule != null && graphicstorage.TryGetValue(self.graphicsModule as LizardGraphics, out GraphicsData graphicData))
            {
                TransformationElectric.PreElectricLizardBite(self, chunk, graphicData, data);
                elec = true;
            }
            else if (ShadowOfOptions.melted_transformation.Value && (data.transformation == "Melted" || data.transformation == "MeltedTransformation"))
            {
                TransformationMelted.PreMeltedLizardBite(self, data, chunk);
                melt = true;
            }

            orig(self, chunk);

            if (elec && self.graphicsModule != null && graphicstorage.TryGetValue(self.graphicsModule as LizardGraphics, out GraphicsData graphicData2))
            {
                TransformationElectric.PostElectricLizardBite(self, graphicData2, chunk);
            }
            else if (melt)
            {
                TransformationMelted.PostMeltedLizardBite(self, data, chunk);
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    static bool LizardHitHeadShield(On.Lizard.orig_HitHeadShield orig, Lizard self, Vector2 direction)
    {
        return (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) || !data.isGoreHalf && !data.beheaded) && orig(self, direction);
    }
    static void LizardUpdate(On.Lizard.orig_Update orig, Lizard self, bool eu)
    {
        orig(self, eu);

        if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data))
        {
            return;
        }

        try
        {
            if (data.spearList.Count > 0)
            {
                List<Spear> tempList = new(data.spearList);
                for (int i = 0; i < tempList.Count; i++)
                {
                    if (tempList[i] == null)
                    {
                        data.spearList.Remove(tempList[i]);
                        continue;
                    }

                    tempList[i].ChangeMode(Weapon.Mode.Free);
                    data.spearList.Remove(tempList[i]);
                }
            }

            if (data.isGoreHalf || self.dead)
            {
                return;
            }

            if (ShadowOfOptions.deafen.Value && data.liz.ContainsKey("EarRight"))
            {
                bool earRightDeaf = data.liz["EarRight"] == "Deaf";
                bool earLeftDeaf = data.liz["EarLeft"] == "Deaf";

                if (earRightDeaf && earLeftDeaf)
                {
                    self.deaf = Mathf.Max(self.deaf, 120);
                }
                else if (earRightDeaf ^ earLeftDeaf)
                {
                    self.deaf = Mathf.Max(self.deaf, 4);
                }
            } //Deaf

            if (ShadowOfOptions.dismemberment.Value && self.LizardState != null && self.LizardState.limbHealth != null)
            {
                for (int i = 0; i < data.armState.Count; i++)
                {
                    if (data.armState[i] != "Normal" && data.armState[i] != "Spider")
                    {
                        self.LizardState.limbHealth[i] = 0f;
                    }
                }
            } //Limb Health

            if (self.dead)
            {
                if (ModManager.HypothermiaModule && ShadowOfOptions.hypothermia_immune.Value && self.Hypothermia >= 1f && (!data.liz.TryGetValue("HypothermiaImmune", out string hypothermia) || hypothermia != "True"))
                {
                    data.liz["HypothermiaImmune"] = "True";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self + " gained Immunity to Hypothermia due to Freezing after Death");

                    if (ShadowOfOptions.dynamic_cheat_death.Value)
                        data.cheatDeathChance += 5;
                } //HypothermiaImmune

                if (ShadowOfOptions.water_breather.Value && self.dead && self.Submersion > 0.1f && self.lungs <= 0f && (!data.liz.TryGetValue("WaterBreather", out string waterBreather) || waterBreather != "True"))
                {
                    data.liz["WaterBreather"] = "True";

                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self + " gained Immunity to Drowning due to Drowning");

                    if (ShadowOfOptions.dynamic_cheat_death.Value)
                        data.cheatDeathChance += 5;
                } //WaterBreather

                if (self.Submersion > 0.1f && self.room.waterObject != null && self.room.waterObject.WaterIsLethal)
                {
                    if (ShadowOfOptions.lava_immune.Value && !self.abstractCreature.lavaImmune && (!data.liz.TryGetValue("LavaImmune", out string lavaImmune) || lavaImmune == "False"))
                    {
                        self.abstractCreature.lavaImmune = true;
                        data.liz["LavaImmune"] = "True";

                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self.ToString() + " gained Immunity to Lava/Acid due to Swimming in Lethal Water");

                        if (ShadowOfOptions.dynamic_cheat_death.Value)
                            data.cheatDeathChance += 5;
                    }

                    if (ShadowOfOptions.melted_transformation.Value && !meltedPorhibited.Contains(self.Template.type.ToString()) && data.transformation != "SpiderTransformation" && data.transformation != "ElectricTransformation" && data.transformation != "Melted" && data.transformation != "MeltedTransformation" && CWTCycleCheck(data, "PreMeltedCycle", CycleNum(self.abstractCreature)))
                    {
                        data.liz["PreMeltedCycle"] = CycleNum(self.abstractCreature).ToString();
                        TransformationMelted.MeltedLizardUpdate(self, data);
                        return;
                    }
                } //LavaImmune && MeltedTransformation
            }

            if (ShadowOfOptions.electric_transformation.Value && data.transformation == "ElectricTransformation" && self.graphicsModule != null && graphicstorage.TryGetValue(self.graphicsModule as LizardGraphics, out GraphicsData GraphicsData))
            {
                TransformationElectric.ElectricLizardUpdate(self, data, GraphicsData);
                return;
            } //ElectricTransformation
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }
    }
    static void LizardViolence(On.Lizard.orig_Violence orig, Lizard self, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
    {
        if (!lizardstorage.TryGetValue(self.abstractCreature, out LizardData data) || type == null)
        {
            orig(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
            return;
        }

        try
        {
            bool sourceOwnerFlag = source != null && source.owner != null;

            bool sourceValidTypeFlag = source != null && (source.owner == null || (source.owner != null && source.owner is not JellyFish && source.owner is not Leech && source.owner is not DartMaggot && source.owner is not SporePlant.AttachedBee));

            #region Electric Damage Reduction
            if (type == Creature.DamageType.Bite && sourceOwnerFlag && source.owner is Lizard sourceLiz && lizardstorage.TryGetValue(sourceLiz.abstractCreature, out LizardData sourceData) && (sourceData.transformation == "Electric" || sourceData.transformation == "ElectricTransformation"))
            {
                self.Violence(source ?? null, directionAndMomentum ?? null, hitChunk ?? null, onAppendagePos ?? null, Creature.DamageType.Electric, damage / 2, stunBonus / 2);

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + source.owner + "'s Bite dealt additional Electric damage to " + self);
            }

            if (type == Creature.DamageType.Electric && (data.transformation == "Electric" || data.transformation == "ElectricTransformation"))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + source.owner + "'s damage was halved due to Resistance on " + self);

                if (data.transformation == "Electric" && UnityEngine.Random.value < 0.2)
                    data.transformationTimer++;

                damage /= 2f;
            }
            #endregion

            #region Spider Likness
            if (data.transformation == "SpiderTransformation" && sourceOwnerFlag)
            {
                if (source.owner is Lizard liz && lizardstorage.TryGetValue(liz.abstractCreature, out LizardData lizdata) && (lizdata.transformation == "SpiderTransformation" || lizdata.transformation == "Spider"))
                {
                    lizdata.spiderLikness--;

                    if (lizdata.transformation == "SpiderTransformation")
                    {
                        damage /= 2f;
                    }
                }
                else if (source.owner is Player && self.AI != null && self.AI.friendTracker != null && self.AI.friendTracker.friend != source.owner && self.room != null && self.room.abstractRoom != null)
                {
                    for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
                    {
                        if (self.room.abstractRoom.creatures[i].realizedCreature != null && self.room.abstractRoom.creatures[i].realizedCreature is Lizard liz2 && lizardstorage.TryGetValue(liz2.abstractCreature, out LizardData lizdata2) && lizdata2.spiderLikness > 0 && liz2.AI != null && liz2.AI.friendTracker.friend == source.owner && (self.room.abstractRoom.creatures[i].rippleLayer == self.abstractPhysicalObject.rippleLayer || self.room.abstractRoom.creatures[i].rippleBothSides || self.abstractPhysicalObject.rippleBothSides))
                        {
                            lizdata2.spiderLikness--;
                        }
                    }
                }
            }
            #endregion

            #region lastDamageType
            if (data.lastDamageType != "Melted" && type.ToString() != "Explosion")
                data.lastDamageType = type.ToString();
            #endregion

            PreViolenceCheck(self, data);

            orig(self, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);

            PostViolenceCheck(self, data, type.ToString(), sourceOwnerFlag && source.owner is Creature crit ? crit : null);

            if (damage < 0.01f)
            {
                return;
            }

            float multiplier = ShadowOfOptions.damage_based_chance.Value ? damage : 1;

            if (ModManager.Watcher && ShadowOfOptions.dismemberment.Value && onAppendagePos != null && onAppendagePos.appendage != null && sourceValidTypeFlag && (type == Creature.DamageType.Stab || type == Creature.DamageType.Bite) && HealthBasedChance(self, ShadowOfOptions.dismemberment_chance.Value * multiplier, "Dismembernment") && !TransformationRot.InnactiveTentacleCheck(data, onAppendagePos.appendage.appIndex, CycleNum(self.abstractCreature)))
            {
                int prevPos = data.cutAppendage.TryGetValue(onAppendagePos.appendage.appIndex, out int pos) ? pos : onAppendagePos.appendage.segments.Length;

                data.cutAppendage[onAppendagePos.appendage.appIndex] = Mathf.Min(onAppendagePos.prevSegment, prevPos);
                data.cutAppendageCycle[onAppendagePos.appendage.appIndex] = CycleNum(self.abstractCreature);

                onAppendagePos.appendage.canBeHit = false;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + onAppendagePos.appendage.appIndex + " appendage was cut down to size " + data.cutAppendage[onAppendagePos.appendage.appIndex]);

                if (sourceOwnerFlag && source.owner is Spear spear)
                {
                    data.spearList.Add(spear);
                }

                return;
            }

            if (hitChunk == null || !sourceValidTypeFlag || !directionAndMomentum.HasValue)
            {
                return;
            }

            if (hitChunk.index == 0)
            {
                if (!data.beheaded)
                {
                    if (LizHitHeadShield(directionAndMomentum.Value))
                    {
                        if (ShadowOfOptions.blind.Value && data.liz.ContainsKey("EyeRight") && Chance(self.abstractCreature, ShadowOfOptions.blind_cut_chance.Value * multiplier, "Eye being Hit"))
                        {
                            string eye = (UnityEngine.Random.Range(0, 2) == 0) ? "EyeRight" : "EyeLeft";

                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self + "'s " + eye + " was hit");

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
                                        Debug.Log(all + self + "'s " + eye + " is already Cut");
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
                        else if (ShadowOfOptions.teeth.Value && data.liz.ContainsKey("UpperTeeth") && type != Creature.DamageType.Bite && Chance(self.abstractCreature, ShadowOfOptions.teeth_chance.Value * multiplier, "Teeth being Hit"))
                        {
                            string teeth = UnityEngine.Random.Range(0, 2) == 0 ? "UpperTeeth" : "LowerTeeth";
                            int teethNum = UnityEngine.Random.Range(1, 5);

                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self + "'s " + teeth + " were hit");

                            if (data.liz[teeth] == "Normal")
                            {
                                bool alreadyBroken = data.liz["UpperTeeth"] != "Normal" || data.liz["LowerTeeth"] != "Normal";

                                data.liz[teeth] = "Broken" + teethNum.ToString();

                                self.lizardParams.biteDamageChance *= alreadyBroken ? 0.5714285714285714f : 0.7f;
                                self.lizardParams.biteDominance *= alreadyBroken ? 0.5714285714285714f : 0.7f;
                                self.lizardParams.biteDamage *= alreadyBroken ? 0.5714285714285714f : 0.7f;
                                self.lizardParams.getFreeBiteChance *= alreadyBroken ? 0.5714285714285714f : 0.7f;

                                if (ShadowOfOptions.debug_logs.Value)
                                    Debug.Log(all + self + " " + (teeth == "UpperTeeth" ? "Upper" : "Lower") + " Teeth were broken");

                                if (ShadowOfOptions.dynamic_cheat_death.Value)
                                    data.cheatDeathChance -= 5;

                                self.room.PlaySound(SoundID.SS_AI_Marble_Hit_Floor, self.firstChunk, false, Custom.LerpMap(source.vel.magnitude, 0f, 8f, 0.2f, 1f) + 10, 1f);

                                FSprite sprite = data.sLeaser.sprites[((LizardGraphics)self.graphicsModule).SpriteHeadStart + teeth == "UpperTeeth" ? 2 : 1];

                                Color colour = self.Template.type == CreatureTemplate.Type.CyanLizard ? self.effectColor : sprite.color;

                                string spriteName = "Pulled" + teeth + self.lizardParams.headGraphics[2] + ".";

                                List<int> spriteNameEnder;

                                if (teeth == "UpperTeeth")
                                {
                                    spriteNameEnder = teethNum switch
                                    {
                                        1 => self.lizardParams.headGraphics[2] switch
                                        {
                                            0 => new() { 1, 3, 4, 5 },
                                            1 => new() { 1, 2, 2, 3 },
                                            2 => new() { 1, 2, 2, 1 },
                                            8 => new() { 1, 1, 1 },
                                            9 => new() { 1, 3, 5 },
                                            10 => new() { 2, 3 },
                                            11 => new() { 1, 1 },
                                            _ => new() { 1 },
                                        },
                                        2 => self.lizardParams.headGraphics[2] switch
                                        {
                                            0 => new() { 1, 5 },
                                            1 => new() { 2, 3 },
                                            2 => new() { 1, 1 },
                                            8 => new() { 1, 1 },
                                            9 => new() { 1, 5 },
                                            10 => new() { 1, 4 },
                                            11 => new() { 1, 1 },
                                            _ => new() { 1 },
                                        },
                                        3 => self.lizardParams.headGraphics[2] switch
                                        {
                                            0 => new() { 4, 3, 5 },
                                            1 => new() { 2, 2, 3 },
                                            2 => new() { 2, 2, 1 },
                                            8 => new() { 1, 1, 1 },
                                            9 => new() { 4, 5 },
                                            10 => new() { 3, 4 },
                                            11 => new() { 1, 1 },
                                            _ => new() { 1 },
                                        },
                                        4 => self.lizardParams.headGraphics[2] switch
                                        {
                                            0 => new() { 2, 3, 4, 3, 5 },
                                            1 => new() { 2, 2, 2, 2, 3 },
                                            2 => new() { 1, 2, 2, 2, 1 },
                                            8 => new() { 1, 1, 1, 1 },
                                            9 => new() { 2, 3, 4, 5 },
                                            10 => new() { 2, 3, 4 },
                                            11 => new() { 1, 1, 1 },
                                            _ => new() { 1 },
                                        },
                                        _ => new() { 1 },
                                    };
                                }
                                else
                                {
                                    spriteNameEnder = teethNum switch
                                    {
                                        1 => self.lizardParams.headGraphics[2] switch
                                        {
                                            0 => new() { 2, 2 },
                                            1 => new() { 2, 2 },
                                            2 => new() { 2, 1 },
                                            8 => new() { 1, 1 },
                                            9 => new() { 2, 4 },
                                            10 => new() { 1, 6 },
                                            11 => new() { 1, 1 },
                                            _ => new() { 1 },
                                        },
                                        2 => self.lizardParams.headGraphics[2] switch
                                        {
                                            0 => new() { 2, 2, 2, 2 },
                                            1 => new() { 2, 2, 2, 2 },
                                            2 => new() { 2, 1, 1, 1 },
                                            8 => new() { 1, 1, 1, 1 },
                                            9 => new() { 2, 3 },
                                            10 => new() { 2, 5 },
                                            11 => new() { 1, 2 },
                                            _ => new() { 1 },
                                        },
                                        3 => self.lizardParams.headGraphics[2] switch
                                        {
                                            0 => new() { 1, 2, 2 },
                                            1 => new() { 1, 2, 2 },
                                            2 => new() { 1, 2, 1 },
                                            8 => new() { 1, 1, 1 },
                                            9 => new() { 1, 2 },
                                            10 => new() { 1, 2, 3 },
                                            11 => new() { 1, 1, 2 },
                                            _ => new() { 1 },
                                        },
                                        4 => self.lizardParams.headGraphics[2] switch
                                        {
                                            0 => new() { 2, 2, 2, 2, 3 },
                                            1 => new() { 2, 2, 2, 2, 3 },
                                            2 => new() { 2, 1, 1, 1, 1 },
                                            8 => new() { 1, 1, 1, 1, 1 },
                                            9 => new() { 2, 3, 4 },
                                            10 => new() { 2, 3, 4, 5, 6 },
                                            11 => new() { 1, 1, 1, 1, 2 },
                                            _ => new() { 1 },
                                        },
                                        _ => new() { 1 },
                                    };
                                }

                                for (int i = 0; i < spriteNameEnder.Count; i++)
                                {
                                    for (int j = 0; j < 2; j++)
                                    {
                                        BrokenTooth brokenTooth = new(self.bodyChunks[0].pos + new Vector2(self.bodyChunks[0].rad * UnityEngine.Random.Range(-1f, 1f), self.bodyChunks[0].rad * UnityEngine.Random.Range(-1f, 1f)), directionAndMomentum.Value * UnityEngine.Random.Range(0.8f, 1.2f), spriteName + spriteNameEnder[i], colour, BloodColoursCheck(self.Template.type.ToString()) ? bloodcolours[self.Template.type.ToString()] : self.effectColor, sprite.scaleX, sprite.scaleY);

                                        self.room.AddObject(brokenTooth);

                                        if (self.Template.type == CreatureTemplate.Type.CyanLizard && graphicstorage.TryGetValue(self.graphicsModule as LizardGraphics, out GraphicsData data3))
                                        {
                                            brokenTooth.electricColorTimer = data3.electricColorTimer;
                                        }
                                    }
                                }

                                if (bloodModCheck && ShadowOfOptions.blood_emitter.Value)
                                    BloodParticle();
                            }
                        }

                        if (ShadowOfOptions.decapitation.Value && data.lastDamageType != "Melted" && type.ToString() == "Explosion" && HealthBasedChance(self, (ShadowOfOptions.decapitation_chance.Value * 0.5f) * multiplier, "Head Cut by Explosion"))
                        {
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self + " had it's Head cut by an explosion");

                            data.beheaded = true;
                            Decapitation(self);
                            self.Die();
                        }
                    }
                    else if (LizHitInMouth(directionAndMomentum.Value))
                    {
                        if (ShadowOfOptions.tongue_ability.Value && self.tongue != null && data.lastDamageType != "Melted" && type != Creature.DamageType.Blunt)
                        {
                            if (ShadowOfOptions.debug_logs.Value)
                                Debug.Log(all + self + " was hit in it's Mouth");

                            if (Chance(self.abstractCreature, ShadowOfOptions.tongue_ability_chance.Value * multiplier, "Tongue being Cut"))
                            {
                                self.tongue.Retract();

                                data.liz["Tongue"] = "Null";
                                self.lizardParams.tongue = false;

                                if (ShadowOfOptions.debug_logs.Value)
                                    Debug.Log(all + self + " lost it's Tongue due to being hit in Mouth");

                                if (ShadowOfOptions.dynamic_cheat_death.Value)
                                    data.cheatDeathChance -= 5;

                                if (sourceOwnerFlag && source.owner is Spear spear)
                                {
                                    data.spearList.Add(spear);
                                }
                            }
                        }
                    }
                    else if (ShadowOfOptions.decapitation.Value && data.beheaded == false && data.lastDamageType != "Melted" && type != Creature.DamageType.Blunt)
                    {
                        if (ShadowOfOptions.debug_logs.Value)
                            Debug.Log(all + self + " was hit it's Neck");

                        if (HealthBasedChance(self, ShadowOfOptions.decapitation_chance.Value * multiplier, "Decapitation"))
                        {
                            data.beheaded = true;
                            Decapitation(self);

                            PreViolenceCheck(self, data);
                            self.Die();
                            PostViolenceCheck(self, data, type.ToString(), sourceOwnerFlag && source.owner is Creature crit2 ? crit2 : null);

                            if (sourceOwnerFlag && source.owner is Spear spear)
                            {
                                data.spearList.Add(spear);
                            }
                        }
                    }
                }
            } //Hitting Head
            else if (type != Creature.DamageType.Blunt)
            {
                if (hitChunk.index == 1 && ShadowOfOptions.decapitation.Value && data.beheaded == false && sourceOwnerFlag && source.owner is Spear && Vector2.Dot(source.pos - self.bodyChunks[1].pos, self.bodyChunks[0].pos - self.bodyChunks[1].pos) > 0f && HealthBasedChance(self, ShadowOfOptions.decapitation_chance.Value * multiplier, "Decapitation"))
                {
                    if (ShadowOfOptions.debug_logs.Value)
                        Debug.Log(all + self + " was hit it's Neck through bodychunk 1");

                    data.beheaded = true;
                    Decapitation(self);

                    PreViolenceCheck(self, data);
                    self.Die();
                    PostViolenceCheck(self, data, type.ToString(), sourceOwnerFlag && source.owner is Creature crit2 ? crit2 : null);


                    data.spearList.Add(source.owner as Spear);

                } //Chance to cut Head if hit close enough to the head
                else if (ShadowOfOptions.dismemberment.Value && (hitChunk.index == 1 || hitChunk.index == 2) && HealthBasedChance(self, ShadowOfOptions.dismemberment_chance.Value * multiplier, "Dismembernment")) //Leg Dismembernment
                {
                    float num5 = Custom.Angle(new(directionAndMomentum.Value.x, directionAndMomentum.Value.y), -hitChunk.Rotation) * (hitChunk.index == 2 ? -1f : 1f);
                    int num8;

                    if (ModManager.DLCShared && self.Template.type == DLCSharedEnums.CreatureTemplateType.EelLizard)
                    {
                        if (hitChunk.index == 1)
                        {
                            num8 = (num5 < 0f) ? 0 : 1;
                            int num9 = (num5 < 0f) ? 2 : 3;

                            if (data.armState[num8] == "Normal")
                            {
                                EllLegCut(num8, num9, num8);
                            }
                        }
                    }
                    else if ((ModManager.DLCShared && self.Template.type == DLCSharedEnums.CreatureTemplateType.SpitLizard) || (self.graphicsModule as LizardGraphics).limbs.Length == 6)
                    {
                        if (hitChunk.index == 1)
                        {
                            num8 = (num5 < 0f) ? 4 : 5;
                            int num9 = (num5 < 0f) ? 2 : 3;

                            if (UnityEngine.Random.value < 0.75f && data.armState[num8] == "Normal")
                            {
                                LegCut(num8, num8);
                            }
                            else if (data.armState[num9] == "Normal")
                            {
                                LegCut(num9, num9);
                            }
                        }
                        else
                        {
                            num8 = (num5 < 0f) ? 0 : 1;
                            int num10 = (num5 < 0f) ? 2 : 3;

                            if (!data.isGoreHalf && UnityEngine.Random.value < 0.75f && data.armState[num8] == "Normal")
                            {
                                LegCut(num8, num8);
                            }
                            else if (data.armState[num10] == "Normal")
                            {
                                LegCut(num10, num10);
                            }
                        }
                    }
                    else if ((self.graphicsModule as LizardGraphics).limbs.Length == 4)
                    {
                        if (hitChunk.index == 1)
                        {
                            num8 = (num5 < 0f) ? 0 : 1;

                            if (data.armState[num8] == "Normal")
                            {
                                LegCut(num8, num8);
                            }
                        }
                        else
                        {
                            num8 = (num5 < 0f) ? 2 : 3;

                            if (data.armState[num8] == "Normal")
                            {
                                LegCut(num8, num8);
                            }
                        }
                    }
                }
                else if (ShadowOfOptions.cut_in_half.Value && RotModuleCheck(self) && data.availableBodychunks.Contains(hitChunk.index) && (hitChunk.index == 1 && data.availableBodychunks.Contains(hitChunk.index + 1) || hitChunk.index != 1 && data.availableBodychunks.Count > 1) && HealthBasedChance(self, ShadowOfOptions.cut_in_half_chance.Value * multiplier, "Cutting in Half")) //Cut in Half
                {
                    CutInHalf(self, data, hitChunk);

                    self.LizardState.health = Mathf.Min(self.LizardState.health, -0.5f);

                    if (sourceOwnerFlag && source.owner is Spear spear)
                    {
                        data.spearList.Add(spear);
                    }
                } //Cut in Half
            }

            if (ShadowOfOptions.electric_transformation.Value && self.graphicsModule != null && graphicstorage.TryGetValue(self.graphicsModule as LizardGraphics, out GraphicsData data2) && sourceOwnerFlag && data.transformation == "ElectricTransformation")
            {
                TransformationElectric.ElectricLizardViolence(self, source, hitChunk, data2);
                return;
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }

        #region Local
        void EllLegCut(int FirstLeg, int SecondLeg, int int1)
        {
            bool a = UnityEngine.Random.Range(0, 2) == 0;

            data.armState[FirstLeg] = a ? "Cut1" : "Cut2";
            data.armState[SecondLeg] = a ? "Cut1" : "Cut2";

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self + " limb cut " + int1);

            if (source != null && source.owner != null && source.owner is Spear spear)
            {
                data.spearList.Add(spear);
            }

            if (ShadowOfOptions.climb_ability.Value)
                ClimbLoss();

            LimbCut(self, data, hitChunk, int1, data.armState[FirstLeg]);
        }
        void LegCut(int Leg, int int1)
        {
            data.armState[Leg] = UnityEngine.Random.Range(0, 2) == 0 ? "Cut1" : "Cut2";

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self + " limb cut " + int1);

            if (source != null && source.owner != null && source.owner is Spear spear)
            {
                data.spearList.Add(spear);
            }

            if (ShadowOfOptions.climb_ability.Value)
                ClimbLoss();

            LimbCut(self, data, hitChunk, int1, data.armState[Leg]);
        }

        void ClimbLoss()
        { 
            if ((!data.liz.TryGetValue("CanClimbPole", out string CanClimbPole) && self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)AItile.Accessibility.Climb].legality == PathCost.Legality.Allowed || CanClimbPole == "True") && Chance(self.abstractCreature, ShadowOfOptions.climb_ability_chance.Value, "Removing Ability to Climb Poles"))
            {
                data.liz["CanClimbPole"] = "False";

                List<TileTypeResistance> list = new();
                self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Climb] = new(0, 1f, 1f, 1f);
                list.Add(new(AItile.Accessibility.Climb, 0f, PathCost.Legality.Unallowed));

                for (int l = 0; l < list.Count; l++)
                {
                    self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                    if (self.abstractCreature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                    {
                        self.abstractCreature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
                    }
                }
            }//Climb
            if ((!data.liz.TryGetValue("CanClimbWall", out string CanClimbWall) && self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)AItile.Accessibility.Wall].legality == PathCost.Legality.Allowed || CanClimbWall == "True") && Chance(self.abstractCreature, ShadowOfOptions.climb_ability_chance.Value, "Removing Ability to Climb Walls"))
            {
                data.liz["CanClimbWall"] = "False";

                List<TileTypeResistance> list = new();
                self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Wall] = new(0, 1f, 1f, 1f);
                list.Add(new(AItile.Accessibility.Wall, 0f, PathCost.Legality.Unallowed));

                for (int l = 0; l < list.Count; l++)
                {
                    self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                    if (self.abstractCreature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                    {
                        self.abstractCreature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
                    }
                }
            }//Wall
            if ((!data.liz.TryGetValue("CanClimbCeiling", out string CanClimbCeiling) && self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)AItile.Accessibility.Ceiling].legality == PathCost.Legality.Allowed || CanClimbCeiling == "True") && Chance(self.abstractCreature, ShadowOfOptions.climb_ability_chance.Value, "Removing Ability to Climb Ceilings"))
            {
                data.liz["CanClimbCeiling"] = "False";

                List<TileTypeResistance> list = new();
                self.lizardParams.terrainSpeeds[(int)AItile.Accessibility.Ceiling] = new(0, 1f, 1f, 1f);
                list.Add(new(AItile.Accessibility.Ceiling, 0f, PathCost.Legality.Unallowed));

                for (int l = 0; l < list.Count; l++)
                {
                    self.abstractCreature.creatureTemplate.pathingPreferencesTiles[(int)list[l].accessibility] = list[l].cost;
                    if (self.abstractCreature.creatureTemplate.maxAccessibleTerrain < (int)list[l].accessibility && list[l].accessibility != AItile.Accessibility.Sand)
                    {
                        self.abstractCreature.creatureTemplate.maxAccessibleTerrain = (int)list[l].accessibility;
                    }
                }
            }//Ceiling
        }

        bool LizHitHeadShield(Vector2 direction)
        {
            float num19 = Vector2.Angle(direction, -self.bodyChunks[0].Rotation);
            if (LizHitInMouth(direction))
            {
                return false;
            }
            if (num19 < self.lizardParams.headShieldAngle + 20f * self.JawOpen)
            {
                return true;
            }
            return false;
        }
        bool LizHitInMouth(Vector2 direction)
        {
            if (direction.y > 0f)
            {
                return false;
            }
            direction = Vector3.Slerp(direction, new(0f, 1f), 0.1f);
            return Mathf.Abs(Vector2.Angle(direction, -self.bodyChunks[0].Rotation)) < Mathf.Lerp(-15f, 11f, self.JawOpen);
        }

        void HitEye(string eye, string oldEye, string newEye)
        {
            bool cut = newEye == "Cut";

            data.liz[eye] = !cut ? (oldEye.Contains("Blind") ? "Blind" : "") + newEye + (newEye == "Scar" ? (UnityEngine.Random.Range(0, 2) == 0 ? "" : "2") : "") : "Cut";
            self.Blind(cut ? 40 : 5);

            if (ShadowOfOptions.debug_logs.Value)
                Debug.Log(all + self + "'s " + eye + " was " + oldEye + " now it's " + data.liz[eye]);

            float sightMultiplier = GetSightMultiplier(data.liz["EyeRight"]) + GetSightMultiplier(data.liz["EyeLeft"]);

            self.Template.visualRadius = data.visualRadius * sightMultiplier;
            self.Template.waterVision = data.waterVision * sightMultiplier;
            self.Template.throughSurfaceVision = data.throughSurfaceVision * sightMultiplier;

            if (ShadowOfOptions.dynamic_cheat_death.Value)
                data.cheatDeathChance -= cut && !oldEye.Contains("Scar") ? 10 : 5; //This is done so losing an eye will always reward a -10 cheatDeathChance in total. if the eye is first scarred it will be a -5 and then if a scarred eye is cut it does the other -5. however if the eye is cut without ever being scarred it is simply a -10

            if (cut)
                EyeCut(self, eye);

            if (bloodModCheck && ShadowOfOptions.blood_emitter.Value)
                BloodParticle();
        }

        void BloodParticle()
        {
            self.room.AddObject(new BloodParticle(self.bodyChunks[0].pos, new Vector2(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(5f, 10f)), BloodColoursCheck(self.Template.type.ToString()) ? bloodcolours[self.Template.type.ToString()] : self.effectColor, self.Template.type.value, null, 2.3f));
        }
        #endregion
    }
    #endregion

    #region CreatureDie
    static void CreatureDie(On.Creature.orig_Die orig, Creature self)
    {
        Lizard liz = (self is Lizard lizard) ? lizard : null;

        if (liz == null || !lizardstorage.TryGetValue(liz.abstractCreature, out LizardData data) || data.isGoreHalf)
        {
            orig(self);
            return;
        }

        try
        {
            if (ShadowOfOptions.spider_transformation.Value && (data.transformation == "Spider" || data.transformation == "SpiderTransformation"))
            {
                TransformationSpider.BabyPuff(liz);

                if (shadowOfIncapacitationCheck)
                {
                    IncapacitationSpewBabies();
                }
            }

            if (data.actuallyDead)
            {
                orig(self);
                return;
            }

            if (ShadowOfOptions.dynamic_cheat_death.Value && data.lastDamageType == "BigEel")
            {
                data.cheatDeathChance -= 50;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self + " gets a -50% Chance to Cheat Death due to dying to a Big Eel");
            }

            if (ShadowOfOptions.decapitation.Value && !ShadowOfOptions.decapitation_survivable.Value && data.beheaded || ShadowOfOptions.cut_in_half.Value && !ShadowOfOptions.cut_in_half_survivable.Value && data.availableBodychunks.Contains(2))
            {
                data.actuallyDead = true;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self + " was forced to fail cheating death");

                if (liz.abstractCreature.state != null)
                    liz.abstractCreature.state.alive = true;

                orig(self);
            }
            else if (Chance(liz.abstractCreature, data.cheatDeathChance, "Cheating Death"))
            {
                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self + " Cheated Death");

                if (liz.abstractCreature.state != null)
                    liz.abstractCreature.state.alive = false;

                orig(self);
            }
            else
            {
                if (liz.abstractCreature.state != null)
                    liz.abstractCreature.state.alive = true;

                data.actuallyDead = true;

                if (ShadowOfOptions.debug_logs.Value)
                    Debug.Log(all + self + " failed to cheat death");

                orig(self);
            }
        }
        catch (Exception e) { ShadowOfLizards.Logger.LogError(e); }

        void IncapacitationSpewBabies()
        {
            if (!Incapacitation.ShadowOfOptions.spid_mother.Value || !Incapacitation.Incapacitation.inconstorage.TryGetValue(self.abstractCreature, out Incapacitation.Incapacitation.InconData data2) || data2.isAlive)
            {
                return;
            }

            if (!data.spewBabies)
            {
                data2.spiderMotherWasDead = true;
                TransformationSpider.BabyPuff(liz);
            }
            else
            {
                data2.spiderMotherWasDead = false;
            }
        }
    }
    #endregion
}