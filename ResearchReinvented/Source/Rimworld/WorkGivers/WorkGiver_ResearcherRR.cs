﻿using PeteTimesSix.ResearchReinvented.DefOfs;
using PeteTimesSix.ResearchReinvented.Managers;
using PeteTimesSix.ResearchReinvented.Opportunities;
using PeteTimesSix.ResearchReinvented.Rimworld.JobDrivers;
using PeteTimesSix.ResearchReinvented.Rimworld.Jobs;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using Verse.AI;

namespace PeteTimesSix.ResearchReinvented.Rimworld.WorkGivers
{
	public class WorkGiver_ResearcherRR : WorkGiver_Scanner
	{
		public static Type DriverClass = typeof(JobDriver_ResearchRR);

		public override ThingRequest PotentialWorkThingRequest
		{
			get
			{
				if (Find.ResearchManager.currentProj == null)
				{
					return ThingRequest.ForGroup(ThingRequestGroup.Nothing);
				}
				return ThingRequest.ForGroup(ThingRequestGroup.ResearchBench);
			}
		}

		public override bool Prioritized
		{
			get
			{
				return true;
			}
		}

		public override bool ShouldSkip(Pawn pawn, bool forced = false)
		{
			return Find.ResearchManager.currentProj == null;
		}

		public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
		{
			ResearchProjectDef currentProj = Find.ResearchManager.currentProj;
			if (currentProj == null)
			{
				return false;
			}
			var opportunity = ResearchOpportunityManager.instance.CurrentOpportunities.Where(o => o.def.jobDef.driverClass == DriverClass).Where(o => !o.IsFinished).FirstOrDefault();
			if (opportunity == null)
			{
				Log.Warning("found no research opportunities when looking for a research job on a research bench => the basic research should always be available!");
				return false;
			}
			Building_ResearchBench building_ResearchBench = t as Building_ResearchBench;
			return 
				opportunity != null &&
				building_ResearchBench != null && 
				currentProj.CanBeResearchedAt(building_ResearchBench, false) && 
				pawn.CanReserve(t, 1, -1, null, forced) && 
				(!t.def.hasInteractionCell || pawn.CanReserveSittableOrSpot(t.InteractionCell, forced)) && 
				new HistoryEvent(HistoryEventDefOf.Researching, pawn.Named(HistoryEventArgsNames.Doer)).Notify_PawnAboutToDo_Job();
		}

		public override Job JobOnThing(Pawn pawn, Thing thing, bool forced = false)
		{
			var opportunity = ResearchOpportunityManager.instance.CurrentOpportunities.Where(o => o.def.jobDef.driverClass == DriverClass).Where(o => !o.IsFinished).FirstOrDefault();
			if (opportunity == null)
			{
				Log.Warning("found no research opportunities when looking for a research job on a research bench => the basic research should always be available!");
				return null;
			}

			Job job = JobMaker.MakeJob(opportunity.def.jobDef, thing, expiryInterval: 1500, checkOverrideOnExpiry: true);
			ResearchOpportunityManager.instance.AssociateJobWithOpportunity(pawn, job, opportunity);
			return job;
		}

		public override float GetPriority(Pawn pawn, TargetInfo t)
		{
			return t.Thing.GetStatValue(StatDefOf.ResearchSpeedFactor, true);
		}
	}
}
