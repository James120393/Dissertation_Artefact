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
			try
			{
				return building.Where(build => !build.isUpgrading() && build.getHitPoints() > 0).First().upgrade(research);
			}
			catch
			{
				return false;
			}
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

        [ExecutableAction("FinishedResearch")]
        public bool FinishedResearch()
        {
            finishedResearch = true;
            return finishedResearch;
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
		[ExecutableAction("AirWepUpgrade")]
		public bool AirWepUpgrade()
		{
			return DoResearch(bwapi.UpgradeTypes_Protoss_Air_Weapons, Interface().GetCyberneticsCore());
		}

		//Action to tell AI to research the Protoss Air Weapon upgrade
		[ExecutableAction("ObserverUpgrade")]
		public bool ObserverUpgrade()
		{
			return DoResearch(bwapi.UpgradeTypes_Sensor_Array, Interface().GetObservatory());
		}

		//Action to tell AI to research the Protoss Ground Weapon upgrade
		[ExecutableAction("GroundWepUpgrade")]
		public bool GroundWepUpgrade()
		{
			return DoResearch(bwapi.UpgradeTypes_Protoss_Ground_Weapons, Interface().GetForge());
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
		[ExecutableSense("HaveGroundWep")]
		public bool HaveGroundWep()
		{
			return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Protoss_Ground_Weapons) > 0 || Interface().Self().isUpgrading(bwapi.UpgradeTypes_Protoss_Ground_Weapons));
		}

		//Sense to tell AI if they have the protoss Air Weapon upgreade
		[ExecutableSense("HaveOberverUpgrade")]
		public bool HaveOberverUpgrade()
		{
			return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Sensor_Array) > 0 || Interface().Self().isUpgrading(bwapi.UpgradeTypes_Sensor_Array));
		}

		//Sense to tell AI if they have the protoss Ground Weapon upgreade
		[ExecutableSense("HaveAirWep")]
		public bool HaveAirWep()
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

		[ExecutableSense("IsObservatoryResearching")]
		public bool IsObservatoryResearching()
		{
			return (Interface().GetObservatory().Where(observatory => observatory.getHitPoints() > 0).First().isUpgrading());
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
    }
}
