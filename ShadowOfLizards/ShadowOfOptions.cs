using System.Collections.Generic;
using Menu.Remix.MixedUI;
using UnityEngine;

namespace ShadowOfLizards;

public class ShadowOfOptions : OptionInterface
{
    public static ShadowOfOptions instance = new();

    public static Configurable<bool> debug_keys = instance.config.Bind("debug_keys", false, new ConfigurableInfo("If turned On N cuts the head of all lizards in the room and M cut's all tongues of all lizards in the room. (Default = false)", null, "", new object[1] { "Debug Keys" }));



    public static Configurable<bool> dynamic_cheat_death_chance = instance.config.Bind("dynamic_cheat_death_chance", true, 
        new ConfigurableInfo("If turned On all Lizards will be given a Random Chance to Cheat Death when created which then will be further modified whwnever they, for example: lose limbs or gain stuff from Regrowth. If Off all Lizards will have the same Chance to Cheat Death. (Default = true)", null, "", new object[1] { "Dynamic Cheat Death" }));

    public static Configurable<int> cheat_death_chance = instance.config.Bind("cheat_death_chance", 0, 
        new ConfigurableInfo("If Dynamic Cheat Death is Off this is the Chance all Lizards have to Cheat Death. (0% = always dies, 100% = always cheats death) Otherwise this amount is added to the Dynamic Chance when a Lizard is Created. (Default = 0%)", null, "", new object[1] { "Base Chance to Cheat Death" }));

    public static Configurable<bool> debug_logs = instance.config.Bind("debug_logs", false, 
        new ConfigurableInfo("If turned On Messages that include a lot of info about Lizards will show up when you turn on Debug Logs, these will also appear in the 'consoleLog.txt' all logs from this mod start with 'ShadowOf:' for easy locating. (Default = false)", null, "", new object[1] { "Debug Logs" }));

    public static Configurable<bool> valid_lizards = instance.config.Bind("valid_lizards", true, new ConfigurableInfo("If turned On only the Base-Game and Downpour Lizards will be affected by the mod to avoid any possible issues. (Default = true)", null, "", new object[1] { "Valid Lizards Only" }));

    #region Transformations
    #region Spider Transformation
    public static Configurable<bool> spider_transformation = instance.config.Bind("spider_transformation", true, new ConfigurableInfo("Enables the Spider Transformation for Lizards. (Default = true)", null, "", 
        new object[1] { "Spider Transformation" }));

    public static Configurable<int> spider_transformation_chance = instance.config.Bind("spider_transformation_chance", 50, 
        new ConfigurableInfo("This is the Chance for Lizards who die by any type of Spider or Spider-Lizard to become Spider Mothers. Depending on what killed the Lizard this value will be multiplied in ranges from 25% to 150% (Default = 50%)", null, "", new object[1] { "Chance for Lizards to become Spider Mothers" }));

    public static Configurable<bool> spider_transformation_skip = instance.config.Bind("spider_transformation_skip", false, new ConfigurableInfo("If this if On Lizards who become Spider Mothers will gain the Spider Transformation instantly next time it is created. (Default = False)", null, "", new object[1] { "Skip Spider Transformation" }));

    public static Configurable<int> spawn_spider_transformation_chance = instance.config.Bind("spawn_spider_transformation_chance", 1, new ConfigurableInfo("If the slider is at 0% new lizards will not spawn with the Transformation. " +
        "This will not stop them from getting the Transformattion by normal means as long as the Transformation is not turned off. \nLizards can only have one Transformation. When the Lizards are created the Transformations are picked from the top, " +
        "meaning if Spider Transformation is set to 100% ALL Lizards will spawn with it no matter the % of the other Transformations. (Default = 1%)", null, "", new object[1] { "Chance for new Lizards to spawn with Spider Transformation" }));

    public static Configurable<bool> spider_spit = instance.config.Bind("spider_spit", true, new ConfigurableInfo("If On Lizards will spit Dart Maggots (and occasionally Spiders) instead of the regular spit. (Default = true)", null, "", new object[1] { "Spider Spit" }));
    #endregion

    #region Electric Transformation
    public static Configurable<bool> electric_transformation = instance.config.Bind("electric_transformation", true, new ConfigurableInfo("Enables the Electric Transformation for Lizards. (Default = true)", null, "", 
        new object[1] { "Electric Transformation" }));

    public static Configurable<int> electric_transformation_chance = instance.config.Bind("electric_transformation_chance", 50, new ConfigurableInfo("This is the Chance for Lizards who die by Electricity to become Electric. (Default = 50%)", null, "", 
        new object[1] { "Chance for Lizards to become Electric" }));

    public static Configurable<bool> electric_transformation_skip = instance.config.Bind("electric_transformation_skip", false, new ConfigurableInfo("If this if On Lizards who become Electric will instantly gain the Electric Transformation instantly next time it is created. (Default = false)", null, "", new object[1] { "Skip Electric Transformation" }));

    public static Configurable<int> spawn_electric_transformation_chance = instance.config.Bind("spawn_electric_transformation_chance", 1, new ConfigurableInfo("If the slider is at 0% new lizards will not spawn with the Transformation. " +
        "This will not stop them from getting the Transformattion by normal means as long as the Transformation is not turned off. \nLizards can only have one Transformation. When the Lizards are created the Transformations are picked from the top, " +
        "meaning if Spider Transformation is set to 100% ALL Lizards will spawn with it no matter the % of the other Transformations. (Default = 1%)", null, "", new object[1] { "Chance for new Lizards to spawn with Electric Transformation" }));

    public static Configurable<bool> electric_spit = instance.config.Bind("electric_spit", true, new ConfigurableInfo("If On Lizards will spit Electric spit instead of the regular spit. Electric Spit stuns whatever it touches. (Default = true)", null, "", new object[1] { "Electric Spit" }));
    #endregion

    #region Melted Transformation
    public static Configurable<bool> melted_transformation = instance.config.Bind("melted_transformation", true, new ConfigurableInfo("Enables the Melted Transformation for Lizards. (Default = true)", null, "", new object[1] { "Melted Transformation" }));

    public static Configurable<int> melted_transformation_chance = instance.config.Bind("melted_transformation_chance", 50, new ConfigurableInfo("This is the Chance for Lizards who die by Acid/Lava to become Melted. (Default = 50%)", null, "", 
        new object[1] { "Chance for Lizards to become Melted" }));

    public static Configurable<bool> melted_transformation_skip = instance.config.Bind("melted_transformation_skip", false, new ConfigurableInfo("If this if On Lizards who become Melted will instantly gain the Melted Transformation instantly next time it is created. (Default = false)", null, "", new object[1] { "Skip Melted Transformation" }));

    public static Configurable<int> spawn_melted_transformation_chance = instance.config.Bind("spawn_melted_transformation_chance", 1, new ConfigurableInfo("If the slider is at 0% new lizards will not spawn with the Transformation. " +
        "This will not stop them from getting the Transformattion by normal means as long as the Transformation is not turned off. \nLizards can only have one Transformation. When the Lizards are created the Transformations are picked from the top, " +
        "meaning if Spider Transformation is set to 100% ALL Lizards will spawn with it no matter the % of the other Transformations. (Default = 1%)", null, "", new object[1] { "Chance for new Lizards to spawn with Melted Transformation" }));

    public static Configurable<bool> melted_spit = instance.config.Bind("melted_spit", true, new ConfigurableInfo("If On Lizards will spit Melted spit (It works the same as Lethal Water and will kill very easily) instead of the regular spit. (Default = true)", null, "", new object[1] { "Melted Spit" }));
    #endregion
    #endregion

    #region Eat Regrowth
    public static Configurable<bool> eat_regrowth = instance.config.Bind("eat_regrowth", true, new ConfigurableInfo("If On Lizards can gain parts after bringing certain creatures to it's den. (To eat) (Default = true)", null, "", 
        new object[1] { "Regrowth by Eating" }));

    public static Configurable<bool> eat_lizard = instance.config.Bind("eat_lizard", true, new ConfigurableInfo("If On Lizards can gain parts after eating other Lizards, this will remove the part from the eaten Lizard. (If it still lives) (Default = true)", null, "", new object[1] { "Regrowth by Cannibalism" }));


    public static Configurable<bool> tongue_regrowth = instance.config.Bind("tongue_regrowth", true, new ConfigurableInfo("If On Lizards can gain tongues by either eating other Lizards who have tongues or Grapling Worms. Only works when the Lizard doesn't have a tongue. (Default = true)", null, "", new object[1] { "Tongue Regrowth" }));

    public static Configurable<int> tongue_regrowth_chance = instance.config.Bind("tongue_regrowth_chance", 20, new ConfigurableInfo("Chance for the Tongue Regrowth to trigger. (Default = 20%)", null, "", new object[1] { "Tongue Regrowth Chance" }));


    public static Configurable<bool> jump_regrowth = instance.config.Bind("jump_regrowth", true, new ConfigurableInfo("If On Lizards can gain a Jump ability by either eating other Lizards who have a Jump ability ur Yeeks, Jetfish and Centiwings. Only works when the Lizard doesn't already have a Jump ability. (Default = true)", null, "", new object[1] { "Jump Regrowth" }));

    public static Configurable<int> jump_regrowth_chance = instance.config.Bind("jump_regrowth_chance", 20, new ConfigurableInfo("Chance for the Jump Regrowth to trigger. (Default = 20%)", null, "", new object[1] { "Jump Regrowth Chance" }));


    public static Configurable<bool> spider_regrowth = instance.config.Bind("spider_regrowth", false, new ConfigurableInfo("If On Lizards can become a Spider Mother by either eating other Lizards who are Spider Mothers/have the Spider Transformation or eating spiders. (Default = false)", null, "", new object[1] { "Spider Transformation Regrowth" }));

    public static Configurable<int> spider_regrowth_chance = instance.config.Bind("spider_regrowth_chance", 20, new ConfigurableInfo("Chance for the Spider Transformation Regrowth to trigger. Depending on what creature the Lizard ate this value is multiplied in ranges from 50% to 200% (Default = 20%)", null, "", new object[1] { "Spider Transformation Regrowth Chance" }));


    public static Configurable<bool> electric_regrowth = instance.config.Bind("electric_regrowth", false, new ConfigurableInfo("If On Lizards can become Electric by either eating other Lizards who are Electric or eating Centipedes. (Default = false)", null, "", new object[1] { "Electric Transformation Regrowth" }));

    public static Configurable<int> electric_regrowth_chance = instance.config.Bind("electric_regrowth_chance", 20, new ConfigurableInfo("Chance for the Electric Transformation Regrowth to work. Depending on what creature the Lizard ate this value is multiplied in ranges from 25% to 200% (Default = 20%)", null, "", new object[1] { "Electric Transformation Regrowth Chance" }));


    public static Configurable<bool> melted_regrowth = instance.config.Bind("melted_regrowth", false, new ConfigurableInfo("If On Lizards can become Melted by eating other Lizards who are Melted. (Default = false)", null, "", 
        new object[1] { "Melted Transformation Regrowth" }));

    public static Configurable<int> melted_regrowth_chance = instance.config.Bind("melted_regrowth_chance", 30, new ConfigurableInfo("Chance for the Melted Transformation Regrowth to work. Depending on what creature the Lizard ate this value is multiplied in ranges from 50% to 100% (Default = 30%)", null, "", new object[1] { "Melted Transformation Regrowth Chance" }));
    #endregion

    #region Gore
    public static Configurable<bool> decapitation = instance.config.Bind("decapitation", true, new ConfigurableInfo("If On Lizards can be Decapitated (Insta-killed with a -50% chance to Cheat Death if Dynamic Cheat Death is turned on) when hit in the neck. (Default = true)", null, "", new object[1] { "Decapitation" }));

    public static Configurable<int> decapitation_chance = instance.config.Bind("decapitation_chance", 5, new ConfigurableInfo("Chance for Lizard to be Decapitated when hit in the Neck. (Default = 5%)", null, "", new object[1] { "Chance to Decapitate Lizards" }));

    public static Configurable<bool> dismemberment = instance.config.Bind("dismemberment", true, new ConfigurableInfo("If On Lizards can be Dismembered (All limbs can be cut off, slowing down the Lizard) when hit in the body. (Default = true)", null, "", new object[1] { "Dismemberment" }));

    public static Configurable<int> dismemberment_chance = instance.config.Bind("dismemberment_chance", 15, new ConfigurableInfo("Chance for Lizard to be Dismembered. (Default = 15%)", null, "", new object[1] { "Chance to Dismember Lizards" }));

    public static Configurable<bool> cut_in_half = instance.config.Bind("cut_in_half", true, new ConfigurableInfo("If On Lizards can be Cut in Half this will kill all but the stringest Lizards when hit in the body. (Default = true)", null, "", new object[1] { "Cut in Half" }));

    public static Configurable<int> cut_in_half_chance = instance.config.Bind("cut_in_half_chance", 5, new ConfigurableInfo("Chance for Lizard to be Cut in Half. (Default = 5%)", null, "", new object[1] { "Chance to Cut Lizards in Half" }));
    #endregion

    #region Abilities
    public static Configurable<bool> tongue_ability = instance.config.Bind("tongue_ability", true, new ConfigurableInfo("If On Lizards can gain or lose their Tongue. (Default = true)", null, "", new object[1] { "Tongue" }));

    public static Configurable<int> tongue_ability_chance = instance.config.Bind("tongue_ability_chance", 40, new ConfigurableInfo("Chance for Lizard to lose their tongue when hit in the Mouth. (Default = 40%)", null, "", 
        new object[1] { "Chance to Cut Tongue" }));


    public static Configurable<bool> jump_ability = instance.config.Bind("jump_ability", true, new ConfigurableInfo("If On Lizards can gain or lose their Jump ability (The Cyan Lizards Jump). (Default = true)", null, "", new object[1] { "Jump" }));

    public static Configurable<int> jump_ability_chance = instance.config.Bind("jump_ability_chance", 50, new ConfigurableInfo("Chance for Lizard to lose their Jump ability when they get a Gas Leak. (Default = 50%)", null, "", 
        new object[1] { "Chance to Disable Jump" }));


    public static Configurable<bool> swim_ability = instance.config.Bind("swim_ability", true, new ConfigurableInfo("If On Lizards can gain or lose their Jump ability (The Cyan Lizards Jump). (Default = true)", null, "", new object[1] { "Jump" }));

    public static Configurable<int> swim_ability_chance = instance.config.Bind("swim_ability_chance", 50, new ConfigurableInfo("Chance for Lizard to lose their Jump ability when they get a Gas Leak. (Default = 50%)", null, "",
        new object[1] { "Chance to Disable Jump" }));


    public static Configurable<bool> climb_ability = instance.config.Bind("climb_ability", true, new ConfigurableInfo("If On Lizards can gain or lose their Jump ability (The Cyan Lizards Jump). (Default = true)", null, "", new object[1] { "Jump" }));

    public static Configurable<int> climb_ability_chance = instance.config.Bind("climb_ability_chance", 50, new ConfigurableInfo("Chance for Lizard to lose their Jump ability when they get a Gas Leak. (Default = 50%)", null, "",
        new object[1] { "Chance to Disable Jump" }));


    public static Configurable<bool> camo_ability = instance.config.Bind("camo_ability", true, new ConfigurableInfo("If On Lizards can gain or lose their Jump ability (The Cyan Lizards Jump). (Default = true)", null, "", new object[1] { "Jump" }));

    public static Configurable<int> camo_ability_chance = instance.config.Bind("camo_ability_chance", 50, new ConfigurableInfo("Chance for Lizard to lose their Jump ability when they get a Gas Leak. (Default = 50%)", null, "",
        new object[1] { "Chance to Disable Jump" }));
    #endregion

    #region Immunities
    public static Configurable<bool> grass_immune = instance.config.Bind("grass_immune", true, new ConfigurableInfo("If On Lizards can gain or lose Immunity to Worm Grass. (Default = true)", null, "", new object[1] { "Worm Grass Immunity" }));

    public static Configurable<int> grass_immune_chance = instance.config.Bind("grass_immune_chance", 75, new ConfigurableInfo("Chance for Lizard to gain Immunity to Worm Grass by being eaten by Worm Grass while either dead or alive. (Default = 75%)", null, "", 
        new object[1] { "Chance to gain Worm Grass Immunity" }));


    public static Configurable<bool> hypothermia_immune = instance.config.Bind("hypothermia_immune", true, new ConfigurableInfo("If On Lizards can gain or lose Immunity to Hypothermia. (Default = true)", null, "", new object[1] { "Hypothermia Immunity" }));

    public static Configurable<int> hypothermia_immune_chance = instance.config.Bind("hypothermia_immune_chance", 75, new ConfigurableInfo("Chance for Lizard to gain Immunity to Hypothermia by getting their hypothermia too high while either dead or alive. (Default = 75%)", null, "",
        new object[1] { "Chance to gain Hypothermia Immunity" }));


    public static Configurable<bool> tentacle_immune = instance.config.Bind("tentacle_immune", true, new ConfigurableInfo("If On Lizards can gain or lose Immunity to Rot Tentacles. (Default = true)", null, "", new object[1] { "Rot Tentacle Immunity" }));

    public static Configurable<int> tentacle_immune_chance = instance.config.Bind("tentacle_immune_chance", 75, new ConfigurableInfo("Chance for Lizard to gain Immunity to Rot Tentacles by getting getting killed by a rot creature or by being eaten by Rot while either dead or alive. (Default = 75%)", null, "",
        new object[1] { "Chance to gain Rot Tentacle Immunity" }));


    public static Configurable<bool> lava_immune = instance.config.Bind("lava_immune", true, new ConfigurableInfo("If On Lizards can gain or lose Immunity to Lava/Acid. This only works when the Melted ransformation is turned Off. (Default = true)", null, "", new object[1] { "Lava/Acid Immunity" }));

    public static Configurable<int> lava_immune_chance = instance.config.Bind("lava_immune_chance", 75, new ConfigurableInfo("Chance for Lizard to gain Immunity to Lava/Acid by swimming in Lava/Acid while either dead or alive. (Default = 75%)", null, "",
        new object[1] { "Chance to gain Lava/Acid Immunity" }));


    public static Configurable<bool> water_breather = instance.config.Bind("water_breather", true, new ConfigurableInfo("If On Lizards can gain or lose Immunity to Drowning. (Default = true)", null, "", new object[1] { "Drowning Immunity" }));

    public static Configurable<int> water_breather_chance = instance.config.Bind("water_breather_chance", 75, new ConfigurableInfo("Chance for Lizard to gain Immunity to Drowning by getting their lungs empty while in water, either dead or alive. (Default = 75%)", null, "",
        new object[1] { "Chance to gain Drowning Immunity" }));
    #endregion

    #region Physical
    public static Configurable<bool> blind = instance.config.Bind("blind", true, new ConfigurableInfo("If On Lizards can become permanently Blind, have their eyes scarred or Cut out. This changes Lizard eye sprites and might not work with modded lizards. (Default = true)", null, "", new object[1] { "Blinding" }));

    public static Configurable<int> blind_chance = instance.config.Bind("blind_chance", 20, new ConfigurableInfo("Chance for Lizard's eyes to be permanently Blinded when a FlareBomb goes off too close to it. (Default = 20%)", 
        null, "", new object[1] { "Chance to Blind" }));

    public static Configurable<int> blind_cut_chance = instance.config.Bind("blind_cut_chance", 10, new ConfigurableInfo("Chance for Lizard's eyes to be hit with weapons, leading to them being Cut out or scarred. (Default = 10%)", 
        null, "", new object[1] { "Chance to hit Eyes" }));



    public static Configurable<bool> deafen = instance.config.Bind("deafen", true, new ConfigurableInfo("If On Lizards can become permanently Deaf when a Explosion goes off too close to it. (Default = true)", null, "", new object[1] { "Defening" }));

    public static Configurable<int> deafen_chance = instance.config.Bind("deafen_chance", 20, new ConfigurableInfo("Chance for Lizard's ears to be permanently Deafeaned. (Default = 20%)",
        null, "", new object[1] { "Chance to Deafen" }));



    public static Configurable<bool> teeth = instance.config.Bind("teeth", true, new ConfigurableInfo("If On Lizards can have some of their Teeth knocked out making their bites worse. (Default = true)", 
        null, "", new object[1] { "Teeth" }));

    public static Configurable<int> teeth_chance = instance.config.Bind("teeth_chance", 5, new ConfigurableInfo("Chance for Lizard's Teeth to be hit when the Head-Shield is hit. (Default = 5%)",
        null, "", new object[1] { "Chance to hit Teeth" }));
    #endregion

    #region Health Based Chance
    public static Configurable<bool> health_based_chance = instance.config.Bind("health_based_chance", true, new ConfigurableInfo("If On dismembernment-adjacent chances will be afftected by the Lizards current health. When full health the chances will be multiplied by the minimum and the lower the Lizards health the higher the multiplier. (Default = true)",
        null, "", new object[1] { "Health Based Chance" }));

    public static Configurable<bool> health_based_chance_dead = instance.config.Bind("health_based_chance_dead", true, new ConfigurableInfo("If On Dead Lizards will use the Default value for dismembernment-adjacent chances. (Default = true)",
        null, "", new object[1] { "Dead Lizard Default Chance" }));

    public static Configurable<int> health_based_chance_min = instance.config.Bind("health_based_chance_min", 50, new ConfigurableInfo("Minimum Chance Multiplier, setting this to 0% will mean that nothing dismembernment-adjacent will trigger if a Lizard is full health. " +
        "Setting this to 100% will use the Standard Chances when the Lizard is full health. (Default = 50%)", null, "", new object[1] { "Minimum Value" }));

    public static Configurable<int> health_based_chance_max = instance.config.Bind("health_based_chance_max", 150, new ConfigurableInfo("Maximum Chance Multiplier, setting this to 100% will use the Standard Chances when the Lizard has no health. " +
        "Setting this to 200% will mean that dismembernment-adjacent chances will trigger twice as often. (Default = 150%)", null, "", new object[1] { "Maximum Value" }));
    #endregion

    #region Blood
    public static Configurable<bool> blood = instance.config.Bind("blood", true, new ConfigurableInfo("If On and the Blood mod is also turned On Lizards will use the blood colours from the Blood mod for some things. (Default = true)", null, "", 
        new object[1] { "Blood" }));

    public static Configurable<bool> blood_emitter = instance.config.Bind("blood_emitter", false, new ConfigurableInfo("If On special Blood Emitters will be created whenever a lizard is beheaded or has it's limbs cut or when limbs are being eaten. (Default = false)", null, "",
        new object[1] { "Extra Blood Emitters" }));

    public static Configurable<bool> blood_emitter_impact = instance.config.Bind("blood_emitter_impact", false, new ConfigurableInfo("If On special blood particles will be created whenever the cut Head or cut Leg impacts ground. (Default = false)", null, "",
        new object[1] { "Impact Blood Partacles" }));
    #endregion


    readonly float font_height = 20f;
    readonly float spacing = 20f;
    readonly int number_of_check_boxes = 3;
    readonly float check_box_size = 24f;

    Vector2 margin_x = default;
    Vector2 position = default;
    readonly List<OpLabel> text_labels = new();
    readonly List<float> box_end_positions = new();

    readonly List<Configurable<bool>> check_box_configurables = new();
    readonly List<OpLabel> check_boxes_text_labels = new();

    readonly List<Configurable<int>> slider_configurables = new();
    readonly List<string> slider_main_text_labels = new();
    readonly List<OpLabel> slider_text_labels_left = new();
    readonly List<OpLabel> slider_text_labels_right = new();

    float Check_Box_With_Spacing => check_box_size + 0.25f * spacing;

    public override void Initialize()
    {
        base.Initialize();
        Tabs = new OpTab[6];

        #region Main Options
        Tabs[0] = new OpTab(instance, "Main Options");
        InitializeMarginAndPos();

        AddNewLine();
        AddTextLabel("Shadow of Lizards\nMod Options", 0, bigText: true);
        DrawTextLabels(ref Tabs[0]);

        AddNewLine();
        AddBox();
        AddCheckBox(dynamic_cheat_death_chance, (string)dynamic_cheat_death_chance.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[0]);
        AddSlider(cheat_death_chance, (string)cheat_death_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[0]);

        AddNewLine();
        AddCheckBox(decapitation, (string)decapitation.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[0]);
        AddSlider(decapitation_chance, (string)decapitation_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[0]);
        AddCheckBox(dismemberment, (string)dismemberment.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[0]);
        AddSlider(dismemberment_chance, (string)dismemberment_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[0]);

        AddNewLine();
        AddCheckBox(blood, (string)blood.info.Tags[0]);
        AddCheckBox(blood_emitter, (string)blood_emitter.info.Tags[0]);
        AddCheckBox(blood_emitter_impact, (string)blood_emitter_impact.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[0]);

        AddNewLine();
        AddCheckBox(debug_logs, (string)debug_logs.info.Tags[0]);
        AddCheckBox(debug_keys, (string)debug_keys.info.Tags[0]);
        AddCheckBox(valid_lizards, (string)valid_lizards.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[0]);
        DrawBox(ref Tabs[0]);
        #endregion

        #region Spider
        Tabs[1] = new OpTab(instance, "Spider");
        InitializeMarginAndPos();

        AddNewLine();
        AddTextLabel("Spider Transformation", 0, bigText: true);
        DrawTextLabels(ref Tabs[1]);

        AddNewLine();
        AddBox();
        AddCheckBox(spider_transformation, (string)spider_transformation.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[1]);

        AddNewLine();
        AddCheckBox(spider_transformation_skip, (string)spider_transformation_skip.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[1]);

        AddNewLine();
        AddSlider(spawn_spider_transformation_chance, (string)spawn_spider_transformation_chance.info.Tags[0], "0%", "100%");
        AddSlider(spider_transformation_chance, (string)spider_transformation_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[1]);

        AddNewLine();
        AddCheckBox(spider_spit, (string)spider_spit.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[1]);
        DrawBox(ref Tabs[1]);
        #endregion

        #region Electric
        Tabs[2] = new OpTab((OptionInterface)(object)instance, "Electric");
        InitializeMarginAndPos();

        AddNewLine();
        AddTextLabel("Electric Transformation", 0, bigText: true);
        DrawTextLabels(ref Tabs[2]);

        AddNewLine();
        AddBox();
        AddCheckBox(electric_transformation, (string)electric_transformation.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[2]);

        AddNewLine();
        AddCheckBox(electric_transformation_skip, (string)electric_transformation_skip.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[2]);

        AddNewLine();
        AddSlider(spawn_electric_transformation_chance, (string)spawn_electric_transformation_chance.info.Tags[0], "0%", "100%");
        AddSlider(electric_transformation_chance, (string)electric_transformation_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[2]);

        AddNewLine();
        AddCheckBox(electric_spit, (string)electric_spit.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[2]);
        DrawBox(ref Tabs[2]);
        #endregion

        #region Melted
        Tabs[3] = new OpTab((OptionInterface)(object)instance, "Melted");
        InitializeMarginAndPos();

        AddNewLine();
        AddTextLabel("Melted Transformation", 0, bigText: true);
        DrawTextLabels(ref Tabs[3]);

        AddNewLine();
        AddBox();
        AddCheckBox(melted_transformation, (string)melted_transformation.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[3]);

        AddNewLine();
        AddCheckBox(melted_transformation_skip, (string)melted_transformation_skip.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[3]);

        AddNewLine();
        AddSlider(spawn_melted_transformation_chance, (string)spawn_melted_transformation_chance.info.Tags[0], "0%", "100%");
        AddSlider(melted_transformation_chance, (string)melted_transformation_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[3]);

        AddNewLine();
        AddCheckBox(melted_spit, (string)melted_spit.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[3]);
        DrawBox(ref Tabs[3]);
        #endregion

        #region Regrowth
        Tabs[4] = new OpTab((OptionInterface)(object)instance, "Regrowth");
        InitializeMarginAndPos();
        AddBox();
        AddCheckBox(eat_regrowth, (string)eat_regrowth.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[4]);
        AddCheckBox(eat_lizard, (string)eat_lizard.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[4]);

        AddNewLine();
        AddCheckBox(tongue_regrowth, (string)tongue_regrowth.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[4]);
        AddSlider(tongue_regrowth_chance, (string)tongue_regrowth_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[4]);

        AddNewLine();
        AddCheckBox(jump_regrowth, (string)jump_regrowth.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[4]);
        AddSlider(jump_regrowth_chance, (string)jump_regrowth_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[4]);

        AddNewLine();
        AddCheckBox(spider_regrowth, (string)spider_regrowth.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[4]);
        AddSlider(spider_regrowth_chance, (string)spider_regrowth_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[4]);

        AddNewLine();
        AddCheckBox(electric_regrowth, (string)electric_regrowth.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[4]);
        AddSlider(electric_regrowth_chance, (string)electric_regrowth_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[4]);

        AddNewLine();
        AddCheckBox(melted_regrowth, (string)melted_regrowth.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[4]);
        AddSlider(melted_regrowth_chance, (string)melted_regrowth_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[4]);
        DrawBox(ref Tabs[4]);
        #endregion

        #region Misc
        Tabs[5] = new OpTab(instance, "Misc");
        InitializeMarginAndPos();
        AddBox();
        AddCheckBox(tongue_ability, (string)tongue_ability.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[5]);
        AddSlider(tongue_ability_chance, (string)tongue_ability_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[5]);

        AddNewLine();
        AddCheckBox(jump_ability, (string)jump_ability.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[5]);
        AddSlider(jump_ability_chance, (string)jump_ability_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[5]);

        AddNewLine();
        AddCheckBox(grass_immune, (string)grass_immune.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[5]);
        AddSlider(grass_immune_chance, (string)grass_immune_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[5]);

        AddNewLine();
        AddCheckBox(blind, (string)blind.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[5]);
        AddSlider(blind_chance, (string)blind_chance.info.Tags[0], "0%", "100%");
        AddSlider(blind_cut_chance, (string)blind_cut_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[5]);

        AddNewLine();
        AddCheckBox(teeth, (string)teeth.info.Tags[0]);
        DrawCheckBoxes(ref Tabs[5]);
        AddSlider(teeth_chance, (string)teeth_chance.info.Tags[0], "0%", "100%");
        DrawSliders(ref Tabs[5]);
        #endregion
    }

    void InitializeMarginAndPos()
    {
        margin_x = new Vector2(50f, 550f);
        position = new Vector2(50f, 600f);
    }

    void AddNewLine(float spacingModifier = 1f)
    {
        position.x = margin_x.x;
        position.y -= spacingModifier * spacing;
    }

    void AddBox()
    {

        margin_x += new Vector2(spacing, 0f - spacing);
        box_end_positions.Add(position.y);
        AddNewLine();
    }

    void DrawBox(ref OpTab tab)
    {
        margin_x += new Vector2(0f - spacing, spacing);
        AddNewLine();
        float num = margin_x.y - margin_x.x;
        int index = box_end_positions.Count - 1;
        tab.AddItems((UIelement[])(object)new UIelement[1] { new OpRect(position, new Vector2(num, box_end_positions[index] - position.y), 0.3f) });
        box_end_positions.RemoveAt(index);
    }

    void AddCheckBox(Configurable<bool> configurable, string text)
    {
        check_box_configurables.Add(configurable);
        check_boxes_text_labels.Add(new OpLabel(default, default, text, (FLabelAlignment)1, false, null));
    }

    void DrawCheckBoxes(ref OpTab tab)
    {
        if (check_box_configurables.Count != check_boxes_text_labels.Count)
        {
            return;
        }
        float num = margin_x.y - margin_x.x;
        float num2 = (num - (number_of_check_boxes - 1) * 0.5f * spacing) / number_of_check_boxes;
        position.y -= check_box_size;
        float num3 = position.x;
        for (int i = 0; i < check_box_configurables.Count; i++)
        {
            Configurable<bool> val = check_box_configurables[i];
            OpCheckBox val2 = new(val, new Vector2(num3, position.y))
            {
                description = (val.info?.description ?? "")
            };
            tab.AddItems((UIelement[])(object)new UIelement[1] { val2 });
            num3 += Check_Box_With_Spacing;
            OpLabel val3 = check_boxes_text_labels[i];
            ((UIelement)val3).pos = new Vector2(num3, position.y + 2f);
            val3.size = new Vector2(num2 - Check_Box_With_Spacing, font_height);
            tab.AddItems((UIelement[])(object)new UIelement[1] { val3 });
            if (i < check_box_configurables.Count - 1)
            {
                if ((i + 1) % number_of_check_boxes == 0)
                {
                    AddNewLine();
                    position.y -= check_box_size;
                    num3 = position.x;
                }
                else
                {
                    num3 += num2 - Check_Box_With_Spacing + 0.5f * spacing;
                }
            }
        }
        check_box_configurables.Clear();
        check_boxes_text_labels.Clear();
    }

    void AddSlider(Configurable<int> configurable, string text, string sliderTextLeft = "", string sliderTextRight = "")
    {
        slider_configurables.Add(configurable);
        slider_main_text_labels.Add(text);
        slider_text_labels_left.Add(new OpLabel(default, default, sliderTextLeft, (FLabelAlignment)2, false, null));
        slider_text_labels_right.Add(new OpLabel(default, default, sliderTextRight, (FLabelAlignment)1, false, null));
    }

    void DrawSliders(ref OpTab tab)
    {
        if (slider_configurables.Count != slider_main_text_labels.Count || slider_configurables.Count != slider_text_labels_left.Count || slider_configurables.Count != slider_text_labels_right.Count)
        {
            return;
        }
        float num = margin_x.y - margin_x.x;
        float num2 = margin_x.x + 0.5f * num;
        float num3 = 0.2f * num;
        float num4 = num - 2f * num3 - spacing;
        for (int i = 0; i < slider_configurables.Count; i++)
        {
            AddNewLine(2f);
            OpLabel val = slider_text_labels_left[i];
            ((UIelement)val).pos = new Vector2(margin_x.x, position.y + 5f);
            val.size = new Vector2(num3, font_height);
            tab.AddItems((UIelement[])(object)new UIelement[1] { val });
            Configurable<int> val2 = slider_configurables[i];
            OpSlider val3 = new(val2, new Vector2(num2 - 0.5f * num4, position.y), (int)num4, false)
            {
                size = new Vector2(num4, font_height),
                description = (val2.info?.description ?? "")
            };
            tab.AddItems((UIelement[])(object)new UIelement[1] { val3 });
            val = slider_text_labels_right[i];
            ((UIelement)val).pos = new Vector2(num2 + 0.5f * num4 + 0.5f * spacing, position.y + 5f);
            val.size = new Vector2(num3, font_height);
            tab.AddItems((UIelement[])(object)new UIelement[1] { val });
            AddTextLabel(slider_main_text_labels[i], 0);
            DrawTextLabels(ref tab);
            if (i < slider_configurables.Count - 1)
            {
                AddNewLine();
            }
        }
        slider_configurables.Clear();
        slider_main_text_labels.Clear();
        slider_text_labels_left.Clear();
        slider_text_labels_right.Clear();
    }

    void AddTextLabel(string text, FLabelAlignment alignment = 0, bool bigText = false)
    {
        float num = (bigText ? 2f : 1f) * font_height;
        if (text_labels.Count == 0)
        {
            position.y -= num;
        }
        OpLabel item = new(default, new Vector2(20f, num), text, alignment, bigText, null)
        {
            autoWrap = true
        };
        text_labels.Add(item);
    }

    void DrawTextLabels(ref OpTab tab)
    {
        if (text_labels.Count == 0)
        {
            return;
        }
        float num = (margin_x.y - margin_x.x) / text_labels.Count;
        foreach (OpLabel text_label in text_labels)
        {
            text_label.pos = position;
            text_label.size += new Vector2(num - 20f, 0f);
            tab.AddItems((UIelement[])(object)new UIelement[1] { text_label });
            position.x += num;
        }
        position.x = margin_x.x;
        text_labels.Clear();
    }
}
