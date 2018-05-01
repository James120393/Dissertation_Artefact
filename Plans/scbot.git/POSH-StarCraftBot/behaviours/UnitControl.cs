using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using POSH.sys;
using POSH.sys.annotations;
using SWIG.BWAPI;
using SWIG.BWTA;

namespace POSH_StarCraftBot.behaviours
{
    public class UnitControl : AStarCraftBehaviour
    {
        /// <summary>
        /// The int value key is identifying the location on the map by shifting the x corrdinate three digits to the left and adding the y value. 
        /// An example would be the position P(122,15) results in the key k=122015
        /// </summary>
        private Dictionary<int, List<Unit>> minedPatches;

        private bool forceReady = false;
		private bool stopZealotBuild = true;

        /// <summary>
        /// The int value key is identifying the location on the map by shifting the x corrdinate three digits to the left and adding the y value. 
        /// An example would be the position P(122,15) results in the key k=122015
        /// </summary>
        private Dictionary<int, List<Unit>> minedGas;
        
        /// <summary>
        /// The dict key is UnitType.getID() which is a numerical representation of the type The UnitType itself 
        /// would not work as a key due to a wrong/missing implementation of the hash
        /// </summary>
        private Dictionary<int, List<Unit>> morphingUnits;

        private Dictionary<int, List<Unit>> trainingUnits;


        public UnitControl(AgentBase agent)
            : base(agent, 
            new string[] {},
            new string[] {})
        {
            minedPatches = new Dictionary<int, List<Unit>>(); 
            minedGas = new Dictionary<int, List<Unit>>();
            morphingUnits = new Dictionary<int, List<Unit>>();
            trainingUnits = new Dictionary<int, List<Unit>>();
        }

        //
        // INTERNAL
        //

        protected int CheckForMorphingUnits(UnitType type)
        {
            if (!morphingUnits.ContainsKey(type.getID()))
                return 0;
            morphingUnits[type.getID()].RemoveAll(unit=> !unit.isMorphing());

            return morphingUnits[type.getID()].Count;
        }

        protected internal Unit GetDrone()
        {
            if (IdleDrones())
                return Interface().GetIdleDrones().ElementAt(0);
            //TODO:  here we could possibly take of the fact that we remove a busy drone from its current task which is not a good thing sometimes
            // this is especially the case if it is the last drone mining

            return (Interface().GetDrones(1).Count() > 0) ? Interface().GetDrones(1).ElementAt(0) : null;
        }

        private int ConvertTilePosition(TilePosition pos)
        {
            return (pos.xConst() * 1000) + pos.yConst();
        }

        public bool DronesToResource(IEnumerable<Unit> resources, Dictionary<int, List<Unit>> mined, int threshold, bool onlyIdle, int maxUnits)
        {
            IEnumerable<Unit> drones;
            int[] mineralTypes = { bwapi.UnitTypes_Resource_Mineral_Field.getID(), bwapi.UnitTypes_Resource_Mineral_Field_Type_2.getID(), bwapi.UnitTypes_Resource_Mineral_Field_Type_3.getID() };
            bool executed = false;
            if (onlyIdle)
                drones = Interface().GetIdleDrones();
            else
                drones = Interface().GetDrones().Where(drone => !Interface().IsBuilder(drone));

            if (drones.Count() < 1 || resources.Count() < 1)
                return executed;

            // update all minded Patches by removing non harvesting drones or dead ones
            foreach (KeyValuePair<int, List<Unit>> patch in minedPatches)
            {
                patch.Value.RemoveAll(drone => (drone.getHitPoints() <= 0 || drone.getOrderTarget() == null || ConvertTilePosition(drone.getOrderTarget().getTilePosition()) != patch.Key));
            }

            foreach (Unit drone in drones)
            {
                if (maxUnits < 1)
                    break;

                if (resources.Contains(drone.getOrderTarget()) && drone.getTarget().getResources() > 0 &&
                    mined.ContainsKey(ConvertTilePosition(drone.getOrderTarget().getTilePosition())))
                {
                    Console.Out.WriteLine("test");
                    continue;
                }

                IEnumerable<Unit> patchPositions = resources.
                    Where(patch => patch.hasPath(drone)).
                    OrderBy(patch => drone.getDistance(patch));
                Unit finalPatch = patchPositions.First();
                int positionValue;

                foreach (Unit position in patchPositions)
                {
                    positionValue = ConvertTilePosition(position.getTilePosition());
                    // a better distribution over resources would be beneficial 
                    if (!mined.ContainsKey(positionValue) || mined[positionValue].Count <= threshold)
                    {
                        finalPatch = position;
                        break;
                    }

                }
                int secCounter = patchPositions.Count() + 1;
                while (!(drone.getTarget() is Unit && drone.getTarget().getID() == finalPatch.getID()) && !drone.isMoving() && secCounter-- > 0)
                {
                    executed = drone.gather(finalPatch, false);
                    maxUnits--;
                    System.Threading.Thread.Sleep(50);
                    // if (_debug_)
                    Console.Out.WriteLine("Drone is gathering: " + executed);
                }



                positionValue = ConvertTilePosition(finalPatch.getTilePosition());
                if (!mined.ContainsKey(positionValue))
                {
                    mined.Add(positionValue, new List<Unit>());
                }

                mined[positionValue].Add(drone);
            }

            return true;
        }

        protected bool MorphUnit(UnitType type)
        {

            if (CanMorphUnit(type))
            {
                int targetLocation = (int)BuildSite.StartingLocation;
                if (Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                    targetLocation = (int)Interface().currentBuildSite;
                IEnumerable<Unit> larvae = Interface().GetLarvae();
                if (larvae.Count() <= 0)
                    return false;
                Unit larva = larvae.OrderBy(unit => unit.getDistance(new Position(Interface().baseLocations[targetLocation]))).First();
                bool morphWorked = larva.morph(type);

                // create new list to monitor specific type of unit
                if (!morphingUnits.ContainsKey(type.getID()))
                    morphingUnits[type.getID()] = new List<Unit>();

                // adding the moved unit to the appropriate unit list
                if (morphingUnits[type.getID()].Where(unit => unit.getID() == larva.getID()).Count() == 0)
                    morphingUnits[type.getID()].Add(larva);

                if (morphWorked)
                    if (Interface().forcePoints.ContainsKey(Interface().currentForcePoint))
                        larva.move(new Position(Interface().forcePoints[Interface().currentForcePoint]));
                    else
                        larva.move(new Position(Interface().baseLocations[targetLocation]));
                return morphWorked;

            }
            return false;
        }

        // Check to see if any units are currently being trained
        protected int CheckForTrainingUnits(UnitType type)
        {
            if (!trainingUnits.ContainsKey(type.getID()))
                return 0;
            trainingUnits[type.getID()].RemoveAll(unit => !unit.isTraining());

            return trainingUnits[type.getID()].Count;
        }

		protected int CheckForDeadUnits(UnitType type)
		{
			int removeUnit = 0;
			foreach (Unit unit in Interface().GetAllUnits(true).Where(unit => unit.getType() == type && unit.getHitPoints() <= 0))
			{
				removeUnit--;
			}
			return removeUnit;
		}

        // Get any idle probes
        protected internal Unit GetProbe()
        {
            if (IdleProbes())
                return Interface().GetIdleProbes().ElementAt(0);
            //TODO:  here we could possibly take of the fact that we remove a busy probe from its current task which is not a good thing sometimes
            // this is especially the case if it is the last drone mining

            return (Interface().GetProbes(1).Count() > 0) ? Interface().GetProbes(1).ElementAt(0) : null;
        }

        // Get any idle probes
        protected internal Unit GetMineralProbe()
        {
            if (IdleProbes())
                return Interface().GetIdleProbes().ElementAt(0);
            //TODO:  here we could possibly take of the fact that we remove a busy probe from its current task which is not a good thing sometimes
            // this is especially the case if it is the last drone mining
            IEnumerable<Unit>  probes = Interface().GetProbes(5);
            probes.First().isGatheringMinerals();
            return (Interface().GetProbes(1).Count() > 0) ? Interface().GetProbes(1).ElementAt(0) : null;
        }

        // Function to send probes to gather minerals
        public bool ProbesToResource(IEnumerable<Unit> resources, Dictionary<int, List<Unit>> mined, int threshold, bool onlyIdle, int maxUnits)
        {
            IEnumerable<Unit> probes;
            int[] mineralTypes = { bwapi.UnitTypes_Resource_Mineral_Field.getID(), bwapi.UnitTypes_Resource_Mineral_Field_Type_2.getID(), bwapi.UnitTypes_Resource_Mineral_Field_Type_3.getID() };
            bool executed = false;
            if (onlyIdle)
                probes = Interface().GetIdleProbes();
            else
                probes = Interface().GetProbes().Where(probe => !Interface().IsBuilder(probe));

            if (probes.Count() < 1 || resources.Count() < 1 || probes == null)
                return executed;

            // Update all minded Patches by removing non harvesting probes or dead ones
            foreach (KeyValuePair<int, List<Unit>> patch in minedPatches)
            {
                patch.Value.RemoveAll(probe => (probe.getHitPoints() <= 0 || probe.getOrderTarget() == null || ConvertTilePosition(probe.getOrderTarget().getTilePosition()) != patch.Key));
            }

            foreach (Unit probe in probes.OrderBy(probe => probe.getResources()))
            {
                if (maxUnits < 1)
                    break;
				if (probe == null)
				{
					return false;
				}
                if (probe.getOrderTarget() is Unit && probe.getTarget() is Unit && resources.Contains(probe.getOrderTarget()) && probe.getTarget().getResources() > 0 &&
                    mined.ContainsKey(ConvertTilePosition(probe.getOrderTarget().getTilePosition())))
                {
                    continue;
                }

                IEnumerable<Unit> patchPositions = resources.
                    Where(patch => patch.hasPath(probe)).
                    OrderBy(patch => probe.getDistance(patch));
                Unit finalPatch = patchPositions.First();
                int positionValue;

                foreach (Unit position in patchPositions)
                {
                    positionValue = ConvertTilePosition(position.getTilePosition());
                    // A better distribution over resources would be beneficial 
                    if (!mined.ContainsKey(positionValue) || mined[positionValue].Count <= threshold)
                    {
                        finalPatch = position;
						break;
                    }

                }
                int secCounter = 10;
                while (!(probe.getTarget() is Unit && probe.getTarget().getID() == finalPatch.getID()) && !probe.isMoving() && secCounter-- > 0)
                {
                    executed = probe.gather(finalPatch, false);
                    maxUnits--;
                    System.Threading.Thread.Sleep(50);
                    // if (_debug_)
                    //Console.Out.WriteLine("Probe is gathering: " + executed);
                }

                positionValue = ConvertTilePosition(finalPatch.getTilePosition());
                if (!mined.ContainsKey(positionValue))
                {
                    mined.Add(positionValue, new List<Unit>());
                }

                mined[positionValue].Add(probe);
            }

            return true;
        }

		// Function to Train units
		protected bool TrainProbe(UnitType type, bool naturalBuild)
		{
			if (CanTrainUnit(type))
			{
				int targetLocation = (int)BuildSite.StartingLocation;

				if (Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
					targetLocation = (int)Interface().currentBuildSite;

				IEnumerable<Unit> prodBuildings = Interface().GetNexus();
				if (prodBuildings.Count() <= 0)
					return false;

				if (naturalBuild)
				{
					if (prodBuildings.Last().getTrainingQueue().Count() < 5)
					{
						bool trainSuccess = prodBuildings.Last().train(type);
						if (trainSuccess)
						{
							Console.Out.WriteLine("Training Unit: " + type.getName() + " At Natural");
							return true;
						}
					}					
				}
				else
				{
					foreach (Unit build in prodBuildings)
					{
						if (build.getTrainingQueue().Count() < 5)
						{
							bool trainSuccess = build.train(type);
							if (trainSuccess)
							{
								Console.Out.WriteLine("Training Unit: " + type.getName());
							}
						}
					}
				}
				return true;
			}
			return false;
		}

        // Function to Train units
		protected bool TrainUnit(UnitType type, UnitType building, int timeout = 50)
		{
			if (CanTrainUnit(type))
			{
				int targetLocation = (int)BuildSite.StartingLocation;

				if (Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
					targetLocation = (int)Interface().currentBuildSite;

				IEnumerable<Unit> prodBuildings = Interface().GetBuilding(building);
				if (prodBuildings.Count() <= 0)
					return false;

				foreach (Unit build in prodBuildings)
				{
					if (build.getTrainingQueue().Count() < 1 && timeout > 0)
					{
						bool trainSuccess = build.train(type);
						if (trainSuccess)
						{
							Console.Out.WriteLine("Training Unit: " + type.getName());
						}
						//timeout--;
					}
					//if (timeout <= 0)
						//continue;
				}
			}
			return false;
		}

        //
        // SENSES
        //
        // Action to tell the AI that its forces are finished being trained
		[ExecutableAction("FinishedForce")]
        public bool FinishedForce()
        {
            forceReady = true;
            return true;
        }

		[ExecutableAction("NotFinishedForce")]
		public bool NotFinishedForce()
		{
			forceReady = false;
			return false;
		}

        // Sense to tell the AI that their forces are ready
        [ExecutableSense("ForceReady")]
        public bool ForceReady()
        {
            return forceReady;
        }
        
        [ExecutableSense("CanAttack")]
        public bool CanAttack()
        {

            return Interface().GetAllUnits(false).Where(unit => !unit.isUnderAttack() && !unit.isAttacking()).Count() > 10 || forceReady;
        }

        [ExecutableSense("IdleDrones")]
        public bool IdleDrones()
        {
            return (Interface().GetIdleDrones().Count() > 0) ? true : false;
        }

        [ExecutableSense("DroneCount")]
        public int DroneCount()
        {
            return Interface().DroneCount() + CheckForMorphingUnits(bwapi.UnitTypes_Zerg_Drone);
        }

        [ExecutableSense("OverlordCount")]
        public int OverlordCount()
        {
            return Interface().OverlordCount() + CheckForMorphingUnits(bwapi.UnitTypes_Zerg_Overlord);
        }

        [ExecutableSense("ZerglingCount")]
        public int ZerglingCount()
        {
            return Interface().ZerglingCount() + CheckForMorphingUnits(bwapi.UnitTypes_Zerg_Zergling);
        }

        [ExecutableSense("HydraliskCount")]
        public int HydraliskCount()
        {
            return Interface().HydraliskCount() + CheckForMorphingUnits(bwapi.UnitTypes_Zerg_Hydralisk);
        }
        [ExecutableSense("MutaliskCount")]
        public int MutaliskCount()
        {
            return Interface().MutaliskCount() + CheckForMorphingUnits(bwapi.UnitTypes_Zerg_Mutalisk);
        }

        [ExecutableSense("LurkerCount")]
        public int LurkerCount()
        {
            return Interface().LurkerCount() + CheckForMorphingUnits(bwapi.UnitTypes_Zerg_Lurker);
        }

        ////////////////////////////////////////////////////////////////////////Begining of James' Code////////////////////////////////////////////////////////////////////////

        // Sense to tell the AI how many Idle Probes it has
        [ExecutableSense("IdleProbes")]
        public bool IdleProbes()
        {
            return (Interface().GetIdleProbes().Count() > 0) ? true : false;
        }


        //Sense to tell the AI how many Probes it has
        [ExecutableSense("ProbeCount")]
        public int ProbeCount()
        {
            return Interface().ProbeCount() + CheckForTrainingUnits(bwapi.UnitTypes_Protoss_Probe);
        }


        //Sense to tell the AI how many Dragoons it has
        [ExecutableSense("DragoonCount")]
        public int DragoonCount()
        {
            return Interface().DragoonCount() + CheckForTrainingUnits(bwapi.UnitTypes_Protoss_Dragoon);
        }


        //Sense to tell the AI how many Zealots it has
        [ExecutableSense("ZealotCount")]
        public int ZealotCount()
        {
			int count = Interface().ZealotCount() + CheckForTrainingUnits(bwapi.UnitTypes_Protoss_Zealot) - CheckForDeadUnits(bwapi.UnitTypes_Protoss_Zealot);
			if (count <= 0)
				return 0;
 			else
				return count;
        }


        //Sense to tell the AI how many Dark Templars it has
        [ExecutableSense("DarkTemplarCount")]
        public int DarkTemplarCount()
        {
            return Interface().DarkTemplarCount() + CheckForTrainingUnits(bwapi.UnitTypes_Protoss_Dark_Templar);
        }


        //Sense to tell the AI how many Corsairs it has
        [ExecutableSense("CorsairCount")]
        public int CorsairCount()
        {
            return Interface().CorsairCount() + CheckForTrainingUnits(bwapi.UnitTypes_Protoss_Corsair);
        }


		//Sense to tell the AI how many Carriers it has
		[ExecutableSense("CarrierCount")]
		public int CarrierCount()
		{
			return Interface().CarrierCount() + CheckForTrainingUnits(bwapi.UnitTypes_Protoss_Carrier);
		}

		//Sense to tell the AI how many Observers it has
		[ExecutableSense("ObserverCount")]
		public int ObserverCount()
		{
			return Interface().ObserverCount() + CheckForTrainingUnits(bwapi.UnitTypes_Protoss_Observer);
		}

		//Sense to tell the AI how many Carriers it has
		[ExecutableSense("CarrierCountNotTraining")]
		public int CarrierCountNotTraining()
		{
			return Interface().CorsairCount() - CheckForTrainingUnits(bwapi.UnitTypes_Protoss_Carrier);
		}

        ////////////////////////////////////////////////////////////////////////End of James' Code////////////////////////////////////////////////////////////////////////
        //
        // ACTIONS
        //

        [ExecutableAction("MorphDrone")]
        public bool MorphDrone()
        {

            return MorphUnit(bwapi.UnitTypes_Zerg_Drone);
        }

        [ExecutableAction("MorphZergling")]
        public bool MorphZergling()
        {
            return MorphUnit(bwapi.UnitTypes_Zerg_Zergling);
        }

        [ExecutableAction("MorphOverlord")]
        public bool MorphOverlord()
        {
            return CheckForMorphingUnits(bwapi.UnitTypes_Zerg_Overlord) >= 1 ? false: MorphUnit(bwapi.UnitTypes_Zerg_Overlord);
        }

        [ExecutableAction("MorphHydralisk")]
        public bool MorphHydralisk()
        {
            return MorphUnit(bwapi.UnitTypes_Zerg_Hydralisk);
        }

        [ExecutableAction("MorphMutalisk")]
        public bool MorphMutalisk()
        {
            return MorphUnit(bwapi.UnitTypes_Zerg_Mutalisk);
        }

        [ExecutableAction("MorphLurker")]
        public bool MorphLurker()
        {
            return MorphUnit(bwapi.UnitTypes_Zerg_Lurker);
        }

        ////////////////////////////////////////////////////////////////////////Begining of James' Code////////////////////////////////////////////////////////////////////////

        //Action to tell the AI to Build a Protoss Probe
        [ExecutableAction("BuildProbe")]
        public bool BuildProbe()
        {
			return TrainProbe(bwapi.UnitTypes_Protoss_Probe, false);
        }

		//Action to tell the AI to Build a Protoss Probe
		[ExecutableAction("BuildNaturalProbe")]
		public bool BuildNaturalProbe()
		{
			return TrainProbe(bwapi.UnitTypes_Protoss_Probe, true);
		}

        //Action to tell the AI to Build a Protoss Zealot
        [ExecutableAction("TrainZealot")]
        public bool TrainZealot()
        {
			return TrainUnit(bwapi.UnitTypes_Protoss_Zealot, bwapi.UnitTypes_Protoss_Gateway);
        }

		//Action to tell the AI to Never build Zealots
		[ExecutableAction("StopZealot")]
		public bool StopZealot()
		{
			stopZealotBuild = false;
			return stopZealotBuild;
		}

        //Action to tell the AI to Build a Protoss Dragoon
        [ExecutableAction("TrainDragoon")]
        public bool TrainDragoon()
        {
			return TrainUnit(bwapi.UnitTypes_Protoss_Dragoon, bwapi.UnitTypes_Protoss_Gateway);
        }


        //Action to tell the AI to Build a Protoss Corsair
        [ExecutableAction("TrainCorsair")]
        public bool TrainCorsair()
        {
			return TrainUnit(bwapi.UnitTypes_Protoss_Corsair, bwapi.UnitTypes_Protoss_Stargate);
        }

		//Action to tell the AI to Build a Protoss Corsair
		[ExecutableAction("TrainCarrier")]
		public bool TrainCarrier()
		{
			return TrainUnit(bwapi.UnitTypes_Protoss_Carrier, bwapi.UnitTypes_Protoss_Stargate);
		}

        //Action to tell the AI to Build a Protoss Dark Templar
        [ExecutableAction("TrainDarkTemplar")]
        public bool TrainDarkTemplar()
        {
			return TrainUnit(bwapi.UnitTypes_Protoss_Dark_Templar, bwapi.UnitTypes_Protoss_Gateway);
        }

		//Action to tell the AI to Build a Protoss Observer
		[ExecutableAction("TrainObserver")]
		public bool TrainObserver()
		{
			return TrainUnit(bwapi.UnitTypes_Protoss_Observer, bwapi.UnitTypes_Protoss_Robotics_Facility);
		}

        ////////////////////////////////////////////////////////////////////////End of James' Code////////////////////////////////////////////////////////////////////////

		//Action to tell the AI to Never build Zealots
		[ExecutableSense("CanTrainZealot")]
		public bool CanTrainZealot()
		{
			return stopZealotBuild;
		}

        //Action to tell the AI to Assign Probes to gather minerals
        [ExecutableAction("AssignProbes")]
        public bool AssignProbes()
        {
            IEnumerable<Unit> mineralPatches = Interface().GetMineralPatches();
            return ProbesToResource(mineralPatches, minedPatches, 6, true, 1);
        }


        //Action to tell the AI to Assign Probes to gather Vespin Gas
        [ExecutableAction("AssignToGas")]
        public bool AssignToGas()
        {
            IEnumerable<Unit> assimilators = Interface().GetBuilding(bwapi.UnitTypes_Protoss_Assimilator);

            return ProbesToResource(assimilators, minedGas, 2, true, 1);
        }

        
    }
}
