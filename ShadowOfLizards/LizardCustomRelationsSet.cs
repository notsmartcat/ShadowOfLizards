using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static CreatureTemplate;
using static RelationshipTracker;
using static Tracker;

namespace ShadowOfLizards;
internal class LizardCustomRelationsSet
{
    [Serializable]
    [CompilerGenerated]
    sealed class LizardCustomRelations
    {
    }

    static Creature RelationNullCheck(DynamicRelationship rel)
    {
        Creature crit = null;
        if (rel == null)
        {
            crit = null;
        }
        else
        {
            CreatureRepresentation trackerRep = rel.trackerRep;
            if (trackerRep != null)
            {
                AbstractCreature representedCreature = trackerRep.representedCreature;
                crit = representedCreature?.realizedCreature;
            }
        }
        return crit;
    }

    public static Relationship.Type Afraid = Relationship.Type.Afraid;

    public static Relationship.Type Ignores = Relationship.Type.Ignores;

    public static Relationship.Type StayOutOfWay = Relationship.Type.StayOutOfWay;

    public static Relationship.Type Attacks = Relationship.Type.Attacks;

    public static Relationship.Type Pack = Relationship.Type.Pack;

    public static float intensity = 1f;

    public static bool TemplateCheck(Creature crit, CreatureTemplate.Type type)
    {
        if (crit != null && crit.Template.type == type)
        {
            return true;
        }
        return false;
    }

    public static bool SpiderTemplateCheck(Creature crit)
    {
        if (crit != null && (crit.Template.type == CreatureTemplate.Type.Spider || crit.Template.type == CreatureTemplate.Type.BigSpider || crit.Template.type == CreatureTemplate.Type.SpitterSpider 
            || ModManager.DLCShared && crit.Template.type == DLCSharedEnums.CreatureTemplateType.MotherSpider))
        {
            return true;
        }
        return false;
    }

    public static bool CentipedeTemplateCheck(Creature crit)
    {
        if (crit != null && (crit.Template.type == CreatureTemplate.Type.Centipede || crit.Template.type == CreatureTemplate.Type.SmallCentipede || crit.Template.type == CreatureTemplate.Type.Centiwing 
            || crit.Template.type == CreatureTemplate.Type.RedCentipede || ModManager.DLCShared && crit.Template.type == DLCSharedEnums.CreatureTemplateType.AquaCenti))
        {
            return true;
        }
        return false;
    }

    public static bool LizardSpiderTransformationTemplateCheck(Creature crit)
    {
        if (crit != null && crit is Lizard liz && ShadowOfLizards.lizardstorage.TryGetValue(liz.abstractCreature, out ShadowOfLizards.LizardData data) 
            && data.transformation == "SpiderTransformation")
        {
            return true;
        }
        return false;
    }

    public static bool LizardSpiderMotherTemplateCheck(Creature crit)
    {
        if (crit != null && crit is Lizard liz && ShadowOfLizards.lizardstorage.TryGetValue(liz.abstractCreature, out ShadowOfLizards.LizardData data)
            && data.transformation == "Spider")
        {
            return true;
        }
        return false;
    }

    public static bool LizardElectricTransformationTemplateCheck(Creature crit)
    {
        if (crit != null && crit is Lizard liz && ShadowOfLizards.lizardstorage.TryGetValue(liz.abstractCreature, out ShadowOfLizards.LizardData data)
            && data.transformation == "ElectricTransformation")
        {
            return true;
        }
        return false;
    }

    public static bool LizardElectricTemplateCheck(Creature crit)
    {
        if (crit != null && crit is Lizard liz && ShadowOfLizards.lizardstorage.TryGetValue(liz.abstractCreature, out ShadowOfLizards.LizardData data) 
            && (data.transformation == "Electric" || data.transformation == "ElectricTransformation"))
        {
            return true;
        }
        return false;
    }

    public static void Apply(CreatureTemplate.Type type, Lizard self)
    {
        if (self == null || !ShadowOfLizards.lizardstorage.TryGetValue(self.abstractCreature, out ShadowOfLizards.LizardData data))
        {
            return;
        }

        if (data.transformation == "Spider")
        {
            //Debug.Log("Spider Relations set for " + self);

            On.BigSpiderAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return TemplateCheck(RelationNullCheck(dRelation), type) ? new Relationship(StayOutOfWay, 0.9f) : orig.Invoke(self, dRelation);
            };

            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return LizardSpiderMotherTemplateCheck(RelationNullCheck(dRelation)) ? new Relationship(StayOutOfWay, 0.9f) : orig.Invoke(self, dRelation);
            };

            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return SpiderTemplateCheck(RelationNullCheck(dRelation)) ? new Relationship(Attacks, intensity) : orig.Invoke(self, dRelation);
            };

            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return LizardSpiderTransformationTemplateCheck(RelationNullCheck(dRelation)) ? new Relationship(Attacks, intensity) : orig.Invoke(self, dRelation);
            };
        }
        else if (data.transformation == "SpiderTransformation")
        {
            //Debug.Log("SpiderTransformation Relations set for " + self);

            On.BigSpiderAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return TemplateCheck(RelationNullCheck(dRelation), type) ? new Relationship(Ignores, 0.0f) : orig.Invoke(self, dRelation);
            };

            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return SpiderTemplateCheck(RelationNullCheck(dRelation)) ? new Relationship(Ignores, 0.0f) : orig.Invoke(self, dRelation);
            };

            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return LizardSpiderTransformationTemplateCheck(RelationNullCheck(dRelation)) ? new Relationship(Pack, 0.5f) : orig.Invoke(self, dRelation);
            };

            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return LizardSpiderMotherTemplateCheck(RelationNullCheck(dRelation)) ? new Relationship(StayOutOfWay, 0.9f) : orig.Invoke(self, dRelation);
            };
        }
        else if (data.transformation == "Electric")
        {
            //Debug.Log("Electric set for " + self);

            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (orig, self, dRelation) =>
            {
                return CentipedeTemplateCheck(RelationNullCheck(dRelation)) ? new Relationship(Relationship.Type.Eats, 0.9f) : orig.Invoke(self, dRelation);
            };
        }
    }
}
