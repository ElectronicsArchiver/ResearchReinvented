﻿using PeteTimesSix.ResearchReinvented.DefOfs;
using PeteTimesSix.ResearchReinvented.Rimworld.Comps.CompProperties;
using PeteTimesSix.ResearchReinvented.Rimworld.MiscData;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;

namespace PeteTimesSix.ResearchReinvented.Rimworld.Comps
{

	public class Comp_ResearchKit : ThingComp
	{
		public CompProperties_ResearchKit Props
		{
			get
			{
				return (CompProperties_ResearchKit)this.props;
			}
		}

		public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
		{
            var benchHyperlink = new Dialog_InfoCard.Hyperlink(Props.substitutedResearchBench, -1);
			yield return new StatDrawEntry(StatCategoryDefOf.Basics, "Equivalent research bench", Props.substitutedResearchBench.LabelCap, "This kit provives portable versions of the tools avaiable at this type of research bench.", -1, hyperlinks: new List<Dialog_InfoCard.Hyperlink>() { benchHyperlink });

			if (!Props.substitutedFacilities.NullOrEmpty())
			{
				List<Dialog_InfoCard.Hyperlink> hyperlinks = new List<Dialog_InfoCard.Hyperlink>();
				List<string> labels = new List<string>();
				foreach (var facility in Props.substitutedFacilities)
				{
					hyperlinks.Add(new Dialog_InfoCard.Hyperlink(facility, -1));
					labels.Add(facility.LabelCap);
				}

				yield return new StatDrawEntry(StatCategoryDefOf.Basics, "Equivalent research facilities", string.Join(", ", labels), "This kit provives portable versions of these research facilities.", -1, hyperlinks: hyperlinks);
            }

            if (Props.remotesThrough != null)
            {
                var remoteHyperlink = new Dialog_InfoCard.Hyperlink(Props.remotesThrough, -1);
                yield return new StatDrawEntry(StatCategoryDefOf.Basics, "Remote connection facility", Props.remotesThrough.LabelCap, "This kit connects to research benches (and their networked facilities) through this facility.", -1, hyperlinks: new List<Dialog_InfoCard.Hyperlink>() { remoteHyperlink });

                var remoteFactor = GetRemoteResearchSpeedFactor();
                var remoteFactorString = remoteFactor > 1f ? remoteFactor.ToStringPercent() : "None";
                yield return new StatDrawEntry(StatCategoryDefOf.EquippedStatOffsets, "Research speed factor (via uplink)", remoteFactorString, "Research speed multiplier provided by remote connection to the research facility.", -1);
            }
        }
        public bool MeetsProjectRequirements(ResearchProjectDef project)
        {
            if (MeetsProjectRequirementsLocally(project))
                return true;
            if (MeetsProjectRequirementsRemotely(project))
                return true;

            return false;
        }

        public bool MeetsProjectRequirementsLocally(ResearchProjectDef project)
        {
            if (project.requiredResearchBuilding != null && Props.substitutedResearchBench != project.requiredResearchBuilding)
            {
                return false;
            }

            if (project.requiredResearchFacilities != null)
            {
                foreach (var requiredFacility in project.requiredResearchFacilities)
                {
                    if (!Props.substitutedFacilities.Contains(requiredFacility))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool MeetsProjectRequirementsRemotely(ResearchProjectDef project)
        {
            if (Props.remotesThrough == null)
                return false;

            var remotableBenches = GetRemotableBenches(project);
            return remotableBenches.Any();

        }

        public float GetTotalResearchSpeedFactor(ResearchProjectDef project = null)
        {
            var localFactor = StatDefOf_Custom.FieldResearchSpeedMultiplier.defaultBaseValue + StatWorker.StatOffsetFromGear(this.parent, StatDefOf_Custom.FieldResearchSpeedMultiplier);
            if(Props.remotesThrough != null) 
            {
                var remoteFactor = GetRemoteResearchSpeedFactor(project);
                if (remoteFactor > 1f)
                    localFactor *= remoteFactor;
            }
            return localFactor;
        }

        public float GetRemoteResearchSpeedFactor(ResearchProjectDef project = null)
        {
            if (Props.remotesThrough == null)
                return -1f;

            float best = -1f;
            List<Building_ResearchBench> benchesToCheck = GetRemotableBenches(project);
            foreach (var researchBench in benchesToCheck)
            {
                var researchSpeedFactor = researchBench.GetStatValue(StatDefOf.ResearchSpeedFactor, true);
                if (researchSpeedFactor > best)
                    best = researchSpeedFactor;
            }

            return best;
        }

        private List<Building_ResearchBench> GetRemotableBenches(ResearchProjectDef project = null)
        {
            var map = this.parent.MapHeld;
            if (map == null)
                return new List<Building_ResearchBench>();

            var remoteBuildings = map.listerBuildings.AllBuildingsColonistOfDef(Props.remotesThrough).ToList();
            var remotableBenches = new List<Building_ResearchBench>();
            foreach (var building in remoteBuildings)
            {
                {
                    CompPowerTrader powerComp = building.TryGetComp<CompPowerTrader>();
                    if (powerComp != null && !powerComp.PowerOn)
                        continue;
                }
                if (Props.remotesThrough.thingClass == typeof(Building_ResearchBench))
                {
                    remotableBenches.AddRange(remoteBuildings.Cast<Building_ResearchBench>());
                }
                else
                {
                    var facility = building.TryGetComp<CompFacility>();
                    if (facility != null)
                    {
                        foreach (var researchBench in facility.LinkedBuildings.Where(t => t is Building_ResearchBench).Cast<Building_ResearchBench>())
                        {
                            {
                                CompPowerTrader powerComp = researchBench.TryGetComp<CompPowerTrader>();
                                if (powerComp != null && !powerComp.PowerOn)
                                    continue;
                            }
                            remotableBenches.Add(researchBench);
                        }
                    }
                }
            }
            if (project != null)
                remotableBenches = remotableBenches.Where(bench => project.CanBeResearchedAt(bench, false)).ToList();

            return remotableBenches;
        }
    }
}
