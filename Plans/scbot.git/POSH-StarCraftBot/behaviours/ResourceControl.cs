using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using POSH.sys;
using POSH.sys.annotations;
using System.Threading;
using SWIG.BWAPI;

namespace POSH_StarCraftBot.behaviours
{
    public class ResourceControl : AStarCraftBehaviour
    {
        private bool finishedResearch;
		private bool needResearch = true;

        public ResourceControl(AgentBase agent)
            : base(agent, new string[] {}, new string[] {})
        {

        }
        //
        // INTERNAL
        //

		private bool HaveResearch(UpgradeType research)
		{
			return Interface().Self().getUpgradeLevel(research) > 0;
		}

		private bool DoResearch(UpgradeType research, IEnumerable<Unit> building)
		{
			return building.Where(build => !build.isUpgrading() && build.getHitPoints() > 0).First().upgrade(research);
		}

        //
        // ACTIONS
        //

		[ExecutableAction("BuildInterceptors")]
		public bool BuildInterceptors()
		{
			foreach (Unit carrier in Interface().GetCarrier())
			{
				for (int i = 0; i < 5; i++)
				{
					if (carrier.getTrainingQueue().Count() < 5)
					{
						carrier.train(bwapi.UnitTypes_Protoss_Interceptor);
					}
					else
					{
						break;
					}
				}
				continue;
			}		
			return true;
		}

		[ExecutableAction("DoNotNeedResearch")]
		public bool DoNotNeedResearch()
		{
			needResearch = false;
			return true;
		}

        [ExecutableAction("HydraSpeedUpgrade")]
        public bool HydraSpeedUpgrade()
        {
            return DoResearch(bwapi.UpgradeTypes_Muscular_Augments, Interface().GetHydraDens());
        }

        [ExecutableAction("HydraRangeUpgrade")]
        public bool HydraRangeUpgrade()
        {
            return Interface().GetHydraDens().Where(den => !den.isUpgrading() && den.getHitPoints() > 0).First().upgrade(bwapi.UpgradeTypes_Grooved_Spines);
        }


        [ExecutableAction("FinishedResearch")]
        public bool FinishedResearch()
        {
            finishedResearch = true;
            return finishedResearch;
        }

        ////////////////////////////////////////////////////////////////////////Begining of James' Code////////////////////////////////////////////////////////////////////////

        //Action to tell AI to research the Protoss attack upgrade 1
        [ExecutableAction("AttackUpgrade")]
        public bool AttackUpgrade()
        {
            return DoResearch(bwapi.UpgradeTypes_Protoss_Ground_Weapons, Interface().GetForge());
        }


        //Action to tell AI to research the Protoss Dragoon Range upgrade
        [ExecutableAction("DragoonRangeUpgrade")]
        public bool DragoonRangeUpgrade()
        {
            return DoResearch(bwapi.UpgradeTypes_Singularity_Charge, Interface().GetCyberneticsCore());
        }

        //Action to tell AI to research the Protoss Shield upgrade
        [ExecutableAction("ShieldUpgrade")]
        public bool ShieldUpgrade()
        {
			return DoResearch(bwapi.UpgradeTypes_Protoss_Plasma_Shields, Interface().GetForge());
        }

		//Action to tell AI to research the Protoss Air Weapon upgrade
		[ExecutableAction("WepUpgrade")]
		public bool WepUpgrade()
		{
			return DoResearch(bwapi.UpgradeTypes_Protoss_Air_Weapons, Interface().GetCyberneticsCore());
		}

		//Action to tell AI to research the Protoss Shield upgrade
		[ExecutableAction("CarrierUpgrade")]
		public bool CarrierUpgrade()
		{
			return DoResearch(bwapi.UpgradeTypes_Carrier_Capacity, Interface().GetFleetbeacon());
		}

		//Action to tell AI to research the Protoss Legs
		[ExecutableAction("LegsUpgrade")]
		public bool LegsUpgrade()
		{
			return DoResearch(bwapi.UpgradeTypes_Leg_Enhancements, Interface().GetCitadel());
		}
        ////////////////////////////////////////////////////////////////////////End of James' Code////////////////////////////////////////////////////////////////////////////


        //
        // SENSES
        //
		[ExecutableSense("InterceptorsNeeded")]
		public bool InterceptorsNeeded()
		{
			IEnumerable<Unit> carrier = Interface().GetCarrier();
			foreach (Unit c in carrier)
			{
				if (c.getInterceptorCount() < 8)
				{
					if (c.getTrainingQueue().Count() >= 5)
					{
						continue;
					}
					return true;
				}
				continue;
			}
			return false;
		}

        [ExecutableSense("StopHydraResearch")]
        public int StopHydraResearch()
        {
            return Interface().TotalSupply();
        }

        [ExecutableSense("DoneResearch")]
        public bool DoneResearch()
        {
            return finishedResearch;
        }

        [ExecutableSense("TotalSupply")]
        public int TotalSupply()
        {
            return Interface().TotalSupply();
        }

        [ExecutableSense("Supply")]
        public int SupplyCount()
        {
            return Interface().SupplyCount();
        }

        [ExecutableSense("AvailableSupply")]
        public int AvailableSupply()
        {
            return Interface().AvailableSupply();
        }

        [ExecutableSense("Gas")]
        public int Gas()
        {
            return Interface().GasCount();
        }

        [ExecutableSense("Minerals")]
        public int Minerals()
        {
            return Interface().MineralCount();
        }

        [ExecutableSense("HaveHydraSpeed")]
        public bool HaveHydraSpeed()
        {
            return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Muscular_Augments) > 0);
        }

        [ExecutableSense("HaveHydraRange")]
        public bool HaveHydraRange()
        {
            return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Grooved_Spines) > 0);
        }

        ////////////////////////////////////////////////////////////////////////Begining of James' Code////////////////////////////////////////////////////////////////////////

        //Sense to tell AI if they have the protoss attack upgreade 1
        [ExecutableSense("HaveAttack")]
        public bool HaveAttack()
        {
			return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Protoss_Ground_Weapons) > 0 || Interface().Self().isUpgrading(bwapi.UpgradeTypes_Protoss_Ground_Weapons));
        }

		[ExecutableSense("HaveCarrier")]
		public bool HaveCarrier()
		{
			return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Carrier_Capacity) > 0 || Interface().Self().isUpgrading(bwapi.UpgradeTypes_Carrier_Capacity));
		}
		
        //Sense to tell AI if they have the protoss Dragoon range upgreade
        [ExecutableSense("HaveDragoonRange")]
        public bool HaveDragoonRange()
        {
			return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Singularity_Charge) > 0 || Interface().Self().isUpgrading(bwapi.UpgradeTypes_Singularity_Charge));
        }

        //Sense to tell AI if they have the protoss Shield upgreade
        [ExecutableSense("HaveShield")]
        public bool HaveShield()
        {
			return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Protoss_Plasma_Shields) > 0 || Interface().Self().isUpgrading(bwapi.UpgradeTypes_Protoss_Plasma_Shields));
        }

		//Sense to tell AI if they have the protoss Air Weapon upgreade
		[ExecutableSense("HaveWep")]
		public bool HaveWep()
		{
			return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Protoss_Air_Weapons) > 0 || Interface().Self().isUpgrading(bwapi.UpgradeTypes_Protoss_Air_Weapons));
		}

		//Sense to tell AI if they have the protoss Legs upgreade
		[ExecutableSense("HaveLegs")]
		public bool HaveLegs()
		{
			return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Leg_Enhancements) > 0 || Interface().Self().isUpgrading(bwapi.UpgradeTypes_Leg_Enhancements));
		}

		[ExecutableSense("IsResearching")]
		public bool IsResearching()
		{
			return (Interface().GetForge().Where(forge => forge.getHitPoints() > 0).First().isUpgrading() || Interface().GetCyberneticsCore().Where(core => core.getHitPoints() > 0).First().isUpgrading());
		}

		[ExecutableSense("IsForgeResearching")]
		public bool IsForgeResearching()
		{
			return (Interface().GetForge().Where(forge => forge.getHitPoints() > 0).First().isUpgrading());
		}

		[ExecutableSense("IsCoreResearching")]
		public bool IsCoreResearching()
		{
			return (Interface().GetCyberneticsCore().Where(core => core.getHitPoints() > 0).First().isUpgrading());
		}

		[ExecutableSense("NeedResearch")]
		public bool NeedResearch()
		{
			return needResearch;
		}    
        ////////////////////////////////////////////////////////////////////////End of James' Code////////////////////////////////////////////////////////////////////////////        
    }
}
