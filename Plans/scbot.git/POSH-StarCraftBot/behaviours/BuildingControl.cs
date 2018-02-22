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

        private TilePosition PossibleBuildLocation(TilePosition start, int xSpace, int ySpace, int iterations, Unit builder, UnitType building)
        {
            List<Position> directions = new List<Position>()
            {
                new Position(0,0),
            /*  new TilePosition(xSpace,0), new TilePosition(xSpace,-ySpace),
                new TilePosition(0,-ySpace),new TilePosition(-xSpace,-ySpace),
                new TilePosition(-xSpace,0),new TilePosition(-xSpace,ySpace),
                new TilePosition(0,ySpace), new TilePosition(xSpace,ySpace) */
             };
            if (iterations < 0)
                return null;
            for (int x = -xSpace; x <= xSpace; x++)
            {
                directions.Add(new Position(x, ySpace));
                directions.Add(new Position(x, -ySpace));
            }
            for (int y = -ySpace; y <= ySpace; y++)
            {
                directions.Add(new Position(xSpace, y));
                directions.Add(new Position(-xSpace, y));
            }


            foreach (Position pos in directions)
            {
                if (bwapi.Broodwar.canBuildHere(builder, start.opAdd(new TilePosition(pos)), building))
                {
                    
                   // if (_debug_)
                    //    Console.Out.WriteLine(building.getID() + " building here: " + start.opAdd(pos).xConst() + " " + start.opAdd(pos).yConst());
                    return start.opAdd(new TilePosition(pos));
                }
            }
            return PossibleBuildLocation(start, ++xSpace, ++ySpace, --iterations, builder, building);
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
            Unit[] units = buildingInProgress.Keys.ToArray();
                foreach (Unit unit in units)
                {
                    if (unit.getHitPoints() == 0 || unit.isCompleted())
                    {
                        buildingInProgress.Remove(unit);
                    }

                }

            return buildingInProgress.Where(pair => pair.Key.getType().getID() == type.getID()).Count();
        }

        //Function to build a building
        protected bool Build(UnitType type, int timeout = 50)
        {
            bool building = false;
            if (builder is Unit)
            {
                if (!buildQueue.ContainsKey(type.getID()) || !(buildQueue[type.getID()] is List<TilePosition>))
                    buildQueue[type.getID()] = new List<TilePosition>();

                //This is a list
                IEnumerable<Unit> houses = Interface().GetAllBuildings().Where(u => u.getType() == type && u.getTilePosition().opEquals(buildLocation));

                Unit specific = null;
                if (houses.Count() > 0)
                    specific = houses.First();
                while (specific == null && timeout > 0)
                {
                    building = builder.build(buildLocation, type);

                    //get the building done at that location using its tile position and type
                    //System.Threading.Thread.Sleep(5);
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
            return false;
        }

        ////////////////////////////////////////////////////////////////////////James' Code////////////////////////////////////////////////////////////////////////
        // Function to position buildings taking he unit type for the building size
        // an xSpace Value, ySpace Value and Z for in iterations
        protected bool Position(UnitType type, int X, int Y, int Z)
        {
            if (!Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;
            // TODO: this needs to be changed to a better location around the base taking exits and resources into account
            TilePosition buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];
            builder = Interface().GetBuilder(buildPosition); 
            // Take the input coordinates for the building and set the position to that
            buildPosition = PossibleBuildLocation(buildPosition, X, Y, Z, builder, type);
            buildLocation = buildPosition;

            if (buildPosition is TilePosition)
            {
                move(new Position(buildPosition), builder);
                return true;
            }
            return false;
        }

        // Function to position buildings taking he unit type for the building size
        // an xSpace Value, ySpace Value and Z for in iterations
        protected bool ChokePosition(UnitType type, int X, int Y, int Z)
        {
            if (!Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;
            // TODO: this needs to be changed to a better location around the base taking exits and resources into account
            TilePosition buildPosition = Interface().buildingChoke;
            builder = Interface().GetBuilder(Interface().baseLocations[(int)Interface().currentBuildSite]);
            // Take the input coordinates for the building and set the position to that
            //buildPosition = PossibleBuildLocation(buildPosition, X, Y, Z, builder, type);
            buildLocation = buildPosition;

            if (buildPosition is TilePosition)
            {
                move(new Position(buildPosition), builder);
                return true;
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////James' Code////////////////////////////////////////////////////////////////////////
        //
        //Actions
        //
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
                builder = Interface().GetBuilder(buildPosition);
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
            return Position(bwapi.UnitTypes_Protoss_Cybernetics_Core, 5, 5, 300);
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
            return Position(bwapi.UnitTypes_Protoss_Nexus, 1, 1, 100);
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
            return ChokePosition(bwapi.UnitTypes_Protoss_Pylon, 5, 5, 250);
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
            return Position(bwapi.UnitTypes_Protoss_Pylon, 1, 1, 500);
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
            return Position(bwapi.UnitTypes_Protoss_Gateway, 5, 5, 250);
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
            return Position(bwapi.UnitTypes_Protoss_Photon_Cannon, 1, 1, 550);
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