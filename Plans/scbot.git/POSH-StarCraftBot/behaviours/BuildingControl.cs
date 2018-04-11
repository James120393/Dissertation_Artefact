using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using POSH.sys;
using POSH.sys.annotations;
using SWIG.BWAPI;
using SWIG.BWTA;
using POSH_StarCraftBot.logic;

namespace POSH_StarCraftBot.behaviours
{
    public class BuildingControl : AStarCraftBehaviour
    {

        TilePosition buildLocation;
        Dictionary<int, Unit> destroyedBuildings;
        Unit buildingToRepair;
        Unit repairDrone;
        Unit builder;
		private bool needNewBuilder = false;
		private int needBuilderCounter = 0;

        private bool needBuilding = true;

        /// <summary>
        /// contains the current location and build queue 
        /// </summary>
        Dictionary<int, List<TilePosition>> buildQueue;

        /// <summary>
        /// contains the buildings which are currently built and still in progress. Once a building is complete it 
        /// gets removed from both dictionaries buildQueue and currentlyBuilt.
        /// </summary>
        Dictionary<Unit, TilePosition> buildingInProgress;

        public BuildingControl(AgentBase agent)
            : base(agent,
            new string[] { },
            new string[] { })
        {
            buildQueue = new Dictionary<int, List<TilePosition>>();
            buildingInProgress = new Dictionary<Unit, TilePosition>();
            destroyedBuildings = new Dictionary<int, Unit>();
        }


        //
        // INTERNAL
        //
        private TilePosition GetBaseLocation()
        {
            TilePosition baseLoc = Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite) ? Interface().baseLocations[(int)Interface().currentBuildSite] : null;
            if (!(baseLoc is TilePosition))
                baseLoc = Interface().baseLocations[(int)BuildSite.StartingLocation];
            

            return baseLoc;
        }

		private TilePosition addToTile(TilePosition pos, int x, int y)
        {
			//int intX = (int) x;
			//int intY = (int)y;
			TilePosition output = new TilePosition(pos.xConst() + x, pos.yConst() + y);

			return output;
        }

        private TilePosition PossibleBuildLocation(TilePosition start, int xSpace, int ySpace, int iterations, Unit builder, UnitType building)
        {
			int x = 0;
			int y = 0;
			int dx = 0;
			int dy = -1;
			int t = (int)Math.Sqrt(iterations);
            for (int i = 0; i < iterations; i++)
            {
                if ((-Math.Sqrt(iterations) / 2 < x && x <= Math.Sqrt(iterations) / 2) && (-Math.Sqrt(iterations) / 2 < y && y <= Math.Sqrt(iterations) / 2))
                {
                    if (bwapi.Broodwar.canBuildHere(builder, addToTile(start, x, y), building))
                    {
                        Console.Out.WriteLine("building " + building.getName() + " at:" + addToTile(start, x, y).xConst() + ":" + addToTile(start, x, y).yConst());
                        return addToTile(start, x, y);
                    }
                }
                if (x == y || (x < 0 && x == -y) || (x > 0 && x == 1 - y))
                {
                    t = dx;
                    dx = -dy;
                    dy = t;
                }
                x += dx;
                y += dy;
            }

			return null;
        }

        protected int CountUnbuiltBuildings(UnitType type)
        {
            int count = 0;
            if (!buildQueue.ContainsKey(type.getID()) || !(buildQueue[type.getID()] is List<TilePosition>))
                return count;
            
            IEnumerable<Unit> houses = Interface().GetAllBuildings().Where(u => u.getType() == type);
            
            foreach (TilePosition pos in buildQueue[type.getID()])
            {
                foreach (Unit house in houses)
                if (pos.opEquals(house.getTilePosition())){
                    if (house.isBeingConstructed())
                    {
                        buildingInProgress[house] = house.getTilePosition();
                        count++;
                    }
                    else if (house.getHitPoints() == 0 || house.isCompleted())
                    {
                        buildQueue[type.getID()].Remove(pos);
                    }
                }
            }

            return count;
        }

        protected int CountBuildingsinProgress(UnitType type)
        {
            for (int i = buildingInProgress.Keys.Count() - 1; i > 0; i--)
            {
                Unit unit = buildingInProgress.Keys.ElementAt(i);
                if (unit.getHitPoints() == 0 || unit.isCompleted())
                {
                    buildingInProgress.Remove(unit);
                }

            }

            return buildingInProgress.Where(pair => pair.Key.getType().getID() == type.getID()).Count();
        }

        //Function to build a building
        protected bool Build(UnitType type, int timeout = 10)
        {
            bool building = false;
            if (builder is Unit)
            {
                if (!buildQueue.ContainsKey(type.getID()) || !(buildQueue[type.getID()] is List<TilePosition>))
                    buildQueue[type.getID()] = new List<TilePosition>();

                //This is a list
				IEnumerable<Unit> buildings = Interface().GetAllBuildings().Where(currentBuilding => currentBuilding.getType() 
					== type && currentBuilding.getTilePosition().opEquals(buildLocation));

                Unit specific = null;
				if (buildings.Count() > 0)
					specific = buildings.First();
                while (specific == null && timeout > 0)
                {
					building = builder.build(buildLocation, type);
                    //get the building done at that location using its tile position and type
                    System.Threading.Thread.Sleep(50);
                    if (building)
                    {
                        buildQueue[type.getID()].Add(buildLocation);
                        return building;
                    }
                    timeout--;

                }
                if (timeout <= 0)
                {
					building = builder.build(builder.getTilePosition(), type);
					if (building)
					{
						buildQueue[type.getID()].Add(buildLocation);
						return building;
					}
                }
            }
			needNewBuilder = true;
            return false;
        }

        ////////////////////////////////////////////////////////////////////////James' Code////////////////////////////////////////////////////////////////////////
		// Function to position buildings taking he unit type for the building size
        // an xSpace Value, ySpace Value and Z for in iterations
        protected bool Position(UnitType type, int X, int Y, int itterations, int timeout = 75)
        {
            //if (!Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite) && Interface().currentBuildSite != BuildSite.NaturalChoke)
               // return false;

            TilePosition buildPosition;
			if (needBuilderCounter >= 5)
				SelectNewBuilder();


			if (Interface().currentBuildSite == BuildSite.NaturalChoke)
                buildPosition = Interface().buildingChoke;
			else if (Interface().currentBuildSite == BuildSite.Natural)
				buildPosition = Interface().naturalBuild;
            else
                buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];

			if (builder == null || builder.getHitPoints() <= 0)
			{
				builder = Interface().GetBuilder(buildPosition);
			}

			double dist = itterations;
            if (buildLocation is TilePosition && buildPosition is TilePosition)
                dist = buildLocation.getDistance(buildPosition);
            if (buildLocation != null && dist < itterations && bwapi.Broodwar.canBuildHere(builder, buildLocation, type)) 
            {
				//move(new Position(buildLocation), builder);
				//return true;
            }
            else
            {
                Position pos = new Position(buildPosition);
                Console.Out.WriteLine("Base: " + Interface().currentBuildSite + " loc: " + buildPosition.xConst() + ": " + buildPosition.yConst() + " pos " + pos.xConst() + ": " + pos.yConst());
                buildPosition = PossibleBuildLocation(buildPosition, X, Y, itterations, builder, type);
				if (buildPosition == null)
				{
					if (bwapi.Broodwar.canBuildHere(builder, new TilePosition(builder.getPosition()), type))
						buildPosition = new TilePosition(builder.getPosition());
						needBuilderCounter++;
						return true;
						
				}
                buildLocation = buildPosition;
            }
			if (buildLocation is TilePosition)
			{
				Position target = new Position(buildLocation);
				builder.move(target, false);
				while (builder.getDistance(target) >= DELTADISTANCE / 2 && timeout > 0)
				{
					if (!builder.isMoving() || builder.isGatheringMinerals())
					{
						builder.move(target, false);
					}
					timeout--;
					System.Threading.Thread.Sleep(100);
				}
				if (timeout <= 0)
				{
					needBuilderCounter++;
					return false;
				}
				needBuilderCounter = 0;
				return true;	
			}
            return false;
        }

        // Function to position buildings taking he unit type for the building size
        // an xSpace Value, ySpace Value and Z for in iterations
        //protected bool ChokePosition(UnitType type, int X, int Y, int Z)
        //{
        //    if (!Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
        //        return false;
        //    // TODO: this needs to be changed to a better location around the base taking exits and resources into account
        //    builder = Interface().GetBuilder(Interface().baseLocations[(int)Interface().currentBuildSite]);
        //    TilePosition buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];
        //    // Take the input coordinates for the building and set the position to that
        //    buildPosition = PossibleBuildLocation(buildPosition, X, Y, 500, builder, type);
        //    buildLocation = buildPosition;
        //    if (buildLocation is TilePosition)
        //    {
        //        move(new Position(buildPosition), builder);
        //        return true;
        //    }
        //    return false;
        //}

        [ExecutableAction("SelectDefenceBase")]
        public bool SelectDefenceBase()
        {
            if (Interface().GetNexus().Count() < 1)
                return false;
            double distanceStart = Interface().baseLocations[(int)ForceLocations.OwnStart].getDistance(buildingToRepair.getTilePosition());
            double distanceNatural = Interface().baseLocations[(int)ForceLocations.Natural].getDistance(buildingToRepair.getTilePosition());

            ForceLocations defenceBase = (distanceStart < distanceNatural) ? ForceLocations.OwnStart : ForceLocations.Natural;
            Interface().currentBuildSite = (BuildSite)defenceBase;
            return true;
        }

        ////////////////////////////////////////////////////////////////////////James' Code////////////////////////////////////////////////////////////////////////
        //
        //Actions
        //
		[ExecutableAction("SelectNewBuilder")]
		public bool SelectNewBuilder()
		{
			if (needNewBuilder == true)
			{
				builder = Interface().GetBuilder(Interface().baseLocations[(int)Interface().currentBuildSite]);
				needBuilderCounter = 0;
				needNewBuilder = false;
				return true;
			}
			return false;
		}

        [ExecutableAction("RepairBuilding")]
        public bool RepairBuilding()
        {
            if (repairDrone == null || repairDrone.getHitPoints() <= 0 || buildingToRepair == null || buildingToRepair.getHitPoints() <= 0)
                return false;
            move(buildingToRepair.getPosition(), repairDrone);
            return repairDrone.repair(buildingToRepair, true);
        }
        
        //
        // SENSES
        //
        [ExecutableSense("NeedBuilding")]
        public bool NeedBuilding()
        {
            return needBuilding;
        }

 
        [ExecutableSense("BuildingDamaged")]
        public bool BuildingDamaged()
        {

            IEnumerable<Unit> buildings = Interface().GetAllBuildings().Where(building => building.isCompleted() && building.getHitPoints() < building.getType().maxHitPoints()).Where(building => building.getHitPoints() > 0);

            return (buildings.Count() > 0);
        }

        [ExecutableSense("FindDamagedBuilding")]
        public bool FindDamagedBuilding()
        {
            IEnumerable<Unit> buildings = Interface().GetAllBuildings().Where(building => building.isCompleted() && building.getHitPoints() < building.getType().maxHitPoints())
                .Where(building => building.getHitPoints() > 0)
                .OrderBy(building => building.getHitPoints());

            // nothing to repair so reset memory and continue
            if (buildings.Count() < 1)
            {
                repairDrone = null;
                buildingToRepair = null;
                return false;
            }
            if (repairDrone == null || repairDrone.getHitPoints() <= 0)
                repairDrone = Interface().GetDrones().Where(drone => drone.getHitPoints() > 0).OrderBy(drone => drone.getDistance(buildings.First())).First();

            if (buildingToRepair == null || buildingToRepair.getHitPoints() <= 0)
                buildingToRepair = buildings.First();

            return (repairDrone is Unit && buildingToRepair is Unit);
        }

        // Action for finding a suitable loaction for the Protoss Assimiltor to harvest Vespin Gas
        [ExecutableAction("SelectAssimilatorLocation")]
        public bool SelectAssimilatorLocation()
        {
            // Enough resources available?
            if (!CanBuildBuilding(bwapi.UnitTypes_Protoss_Assimilator) || !Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;

            TilePosition buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];
            // Are there any geysers available/visible?
            IEnumerable<Unit> geysers = Interface().GetGeysers().Where(geyser => geyser.getResources() > 0);
            if (geysers.Count() < 1)
                return false;

            // Sort by closest path for ground units from selected build base
            TilePosition closest = geysers
                .OrderBy(geyser => geyser.getDistance(new Position(buildPosition)))
                .First().getTilePosition();
            
            // If there is a close geyers we are done
            if (closest is TilePosition)
            {
                this.buildLocation = closest;
                builder = Interface().GetBuilder(Interface().baseLocations[(int)Interface().currentBuildSite]);
                return true;
            }
            return false;
        }


        // Action to use the suitable location to build the protoss Assimilator
        [ExecutableAction("BuildAssimilator")]
        public bool BuildAssimilator()
        {
            //Check to see if the AI can afford the building
            if (CanBuildBuilding(bwapi.UnitTypes_Protoss_Assimilator) && buildLocation is TilePosition)
            {
                // Call the build function feeding in the unit ID
                return Build(bwapi.UnitTypes_Protoss_Assimilator);
            }
            return false;
        }


        // Action for finding a suitable loaction for the Protoss Forge
        [ExecutableAction("PositionForge")]
        public bool PositionForge()
        {
			return Position(bwapi.UnitTypes_Protoss_Forge, 1, 1, 300);
        }


        // Action to use the suitable location to build the protoss Forge
        [ExecutableAction("BuildForge")]
        public bool BuildForge()
        {
            return Build(bwapi.UnitTypes_Protoss_Forge);
        }


        // Action for finding a suitable loaction for the Protoss Cybernetics Core
        [ExecutableAction("PositionCyberneticsCore")]
        public bool PositionCyberneticsCore()
        {
			return Position(bwapi.UnitTypes_Protoss_Cybernetics_Core, 1, 1, 300);
        }


        // Action to use the suitable location to build the protoss Cybernetics Core
        [ExecutableAction("BuildCyberneticsCore")]
        public bool BuildCyberneticsCore()
        {
            return Build(bwapi.UnitTypes_Protoss_Cybernetics_Core);
        }


        // Action for finding a suitable loaction for the Protoss Nexus
        [ExecutableAction("PositionNexus")]
        public bool PositionNexus()
        {
            return Position(bwapi.UnitTypes_Protoss_Nexus, 1, 1, 300);
        }


        // Action to use the suitable location to build the protoss Nexus
        [ExecutableAction("BuildNexus")]
        public bool BuildNexus()
        {
            return Build(bwapi.UnitTypes_Protoss_Nexus);
        }

        // Action for finding a suitable loaction for the Protoss Pylon
        [ExecutableAction("PositionPylon")]
        public bool PositionPylon()
        {
            return Position(bwapi.UnitTypes_Protoss_Pylon, 1, 1, 300);
        }


        // Action to use the suitable location to build the protoss Pylon
        [ExecutableAction("BuildPylon")]
        public bool BuildPylon()
        {
            return Build(bwapi.UnitTypes_Protoss_Pylon);            
        }


        // Action for finding a suitable loaction for the Protoss Pylon that is used for defense
        [ExecutableAction("PositionChokePylon")]
        public bool PositionChokePylon()
        {
            //TODO Create function to select the appropriate choke point for the pylon to be built
            return Position(bwapi.UnitTypes_Protoss_Pylon, 1, 1, 300);
        }


        // Action to use the suitable location to build the protoss Pylon that is used for defense
        [ExecutableAction("BuildChokePylon")]
        public bool BuildChokePylon()
        {
            return Build(bwapi.UnitTypes_Protoss_Pylon);
        }


        // Action for finding a suitable loaction for the Protoss Gateway
        [ExecutableAction("PositionGateway")]
        public bool PositionGateway()
        {
            return Position(bwapi.UnitTypes_Protoss_Gateway, 1, 1, 300);
        }


        // Action to use the suitable location to build the protoss GateWay
        [ExecutableAction("BuildGateway")]
        public bool BuildGateway()
        {
            return Build(bwapi.UnitTypes_Protoss_Gateway);
        }


        // Action for finding a suitable loaction for the Protoss Photon Cannon
        [ExecutableAction("PositionCannon")]
        public bool PositionCannon()
        {
            //TODO Create function to select the appropriate choke point for the cannon to be built
            return Position(bwapi.UnitTypes_Protoss_Photon_Cannon, 1, 1, 300);
        }

        // Action to use the suitable location to build the protoss Photon Cannon
        [ExecutableAction("BuildCannon")]
        public bool BuildCannon()
        {
            return Build(bwapi.UnitTypes_Protoss_Photon_Cannon);
        }

        // Action to tell the AI that this build order is finished
        [ExecutableAction("FinishedEighteenNexusOpening")]
        public bool FinishedEighteenNexusOpening()
        {
            needBuilding = false;

            return needBuilding;
        }


        //
        // SENSES
        //
		[ExecutableSense("HasChokePylon")]
		public bool HasChokePylon()
		{
			if (Interface().buildingChoke is TilePosition)
			{
				if (Interface().naturalHasBeenFound == true)
				{
					foreach (Unit unit in Interface().GetAllBuildings())
						if (unit.getType().getID() == bwapi.UnitTypes_Protoss_Pylon.getID() &&
							unit.getDistance(new Position(Interface().buildingChoke)) <= DELTADISTANCE * 2)
							return true;
				}
				return false;
			}
			return false;
		}

		[ExecutableSense("HasNaturalPylon")]
		public bool HasNaturalPylon()
		{
			if (Interface().naturalBuild is TilePosition)
			{
				if (Interface().naturalHasBeenFound == true)
				{
					foreach (Unit unit in Interface().GetAllBuildings())
						if (unit.getType().getID() == bwapi.UnitTypes_Protoss_Pylon.getID() &&
							unit.getDistance(new Position(Interface().naturalBuild)) <= DELTADISTANCE * 2)
							return true;
				}
				return false;
			}
			return false;
		}

		[ExecutableSense("NeedNewBuilder")]
		public bool NeedNewBuilder()
		{
			return needNewBuilder;
		}

        // Sense to return the number of protoss Nexus'
        [ExecutableSense("NexusCount")]
        public int NexusCount()
        {
            return Interface().GetNexus().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Protoss_Nexus) + CountUnbuiltBuildings(bwapi.UnitTypes_Protoss_Nexus);
        }


        // Sense to return the number of protoss Forges
        [ExecutableSense("ForgeCount")]
        public int ForgeCount()
        {
            return Interface().GetForge().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Protoss_Forge) + CountUnbuiltBuildings(bwapi.UnitTypes_Protoss_Forge);
        }


        // Sense to return the number of protoss Assimilators
        [ExecutableSense("AssimilatorCount")]
        public int AssimilatorCount()
        {
            return Interface().GetAssimilator().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Protoss_Assimilator) + CountUnbuiltBuildings(bwapi.UnitTypes_Protoss_Assimilator);
        }


        // Sense to return the number of protoss Cybernetics Cores
        [ExecutableSense("CyberneticsCoreCount")]
        public int CyberneticsCoreCount()
        {
            return Interface().GetCyberneticsCore().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Protoss_Cybernetics_Core) + CountUnbuiltBuildings(bwapi.UnitTypes_Protoss_Cybernetics_Core);
        }


        // Sense to return the number of protoss Gateways
        [ExecutableSense("GatewayCount")]
        public int GatewayCount()
        {
            return Interface().GetGateway().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Protoss_Gateway) + CountUnbuiltBuildings(bwapi.UnitTypes_Protoss_Gateway);
        }


        // Sense to return the number of protoss Pylon's
        [ExecutableSense("PylonCount")]
        public int PylonCount()
        {
            return Interface().GetPylon().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Protoss_Pylon) + CountUnbuiltBuildings(bwapi.UnitTypes_Protoss_Pylon);
        }


        // Sense to return the number of protoss Cannon's
        [ExecutableSense("CannonCount")]
        public int CannonCount()
        {
            return Interface().GetCannon().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Protoss_Photon_Cannon) + CountUnbuiltBuildings(bwapi.UnitTypes_Protoss_Photon_Cannon);
        }


        // Sense to return the number of protoss StarGates
        [ExecutableSense("StargateCount")]
        public int StargateCount()
        {
            return Interface().GetStargate().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Protoss_Stargate) + CountUnbuiltBuildings(bwapi.UnitTypes_Protoss_Stargate);
        }


        // Sense to return the number of protoss Fleet Beacon's
        [ExecutableSense("FleetbeaconCount")]
        public int FleetbeaconCount()
        {
            return Interface().GetFleetbeacon().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Protoss_Fleet_Beacon) + CountUnbuiltBuildings(bwapi.UnitTypes_Protoss_Fleet_Beacon);
        }


        // Select a unit for building a structure
        [ExecutableSense("HaveBuilder")]
        public bool HaveBuilder()
        {
            builder = UnitManager().GetProbe();

            return (builder is Unit) ? true : false;
        }

        // Sense to tell the AI whether they have a Nexus at their natural expansion
        [ExecutableSense("HaveNaturalNexus")]
        public bool HaveNaturalNExus()
        {
            TilePosition natural = Interface().baseLocations.ContainsKey((int)BuildSite.Natural) ? Interface().baseLocations[(int)BuildSite.Natural] : null;
            TilePosition start = Interface().baseLocations[(int)BuildSite.StartingLocation];

            // Natural not known
            if (natural == null)
                return false;

            // Arbitratry distance measure to determine if the Nexus is closer to the natural or the starting location
            double dist = new Position(natural).getDistance(new Position(start)) / 3;

            if (Interface().GetNexus().Where(nexus => nexus.getDistance(new Position(natural)) < dist).Count() > 0)
                return true;

            foreach (Unit unit in this.buildingInProgress.Keys)
                if (unit.getType().getID() == bwapi.UnitTypes_Protoss_Nexus.getID() &&
                    unit.getDistance(new Position(natural)) < dist)
                    return true;

            return false;
        }
    }
    ////////////////////////////////////////////////////////////////////////End of James' Code////////////////////////////////////////////////////////////////////////
}