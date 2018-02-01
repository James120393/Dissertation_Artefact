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
        Chokepoint chokepoint;
        Dictionary<int, Unit> destroyedBuildings;
        Unit buildingToRepair;
        Unit repairDrone;
        Unit builder;

        private bool needBuilding = true;

        /// <summary>
        /// contains the current location and build queue 
        /// </summary>
        Dictionary<int, Dictionary<Unit, TilePosition>> buildQueue;

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
            buildQueue = new Dictionary<int, Dictionary<Unit, TilePosition>>();
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
            if (!buildQueue.ContainsKey(type.getID()) || !(buildQueue[type.getID()] is Dictionary<Unit, TilePosition>))
                return count;
            
            Unit[] units = buildQueue[type.getID()].Keys.ToArray();
            foreach (Unit unit in units)
            {
                if (unit.isBeingConstructed() || (unit.isConstructing() && unit.getTargetPosition().opEquals(new Position(buildQueue[type.getID()][unit]))))
                {
                    buildingInProgress[unit] = unit.getTilePosition();
                    count++;
                }
                else if (unit.getHitPoints() == 0 || !unit.getTargetPosition().opEquals(new Position(buildQueue[type.getID()][unit])) || unit.isCompleted())
                {
                    buildQueue[type.getID()].Remove(unit);
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
        protected bool Build(UnitType type, int timeout = 5)
        {
            bool building = false;
            if (buildLocation is TilePosition && builder is Unit && !builder.isConstructing() && !builder.isBeingConstructed())
            {
                if (!buildQueue.ContainsKey(type.getID()) || !(buildQueue[type.getID()] is Dictionary<Unit, TilePosition>))
                    buildQueue[type.getID()] = new Dictionary<Unit, TilePosition>();

                if (buildQueue[type.getID()].ContainsKey(builder) && builder.isConstructing())
                    return true;
                foreach (int uType in buildQueue.Keys)
                    if (buildQueue[uType].ContainsKey(builder))
                        return false;

                while (!builder.isConstructing() && builder.getHitPoints() > 0 && timeout-- > 0)
                {
                    building = builder.build(buildLocation, type);
                    //System.Threading.Thread.Sleep(0);
                }

                if (building && timeout <= 0)
                    buildQueue[type.getID()][builder] = buildLocation;
                return building;
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

        ////////////////////////////////////////////////////////////////////////James' Code////////////////////////////////////////////////////////////////////////


        //
        // ACTIONS
        //
        [ExecutableAction("SelectExtractorLocation")]
        public bool SelectExtractorLocation()
        {
            // enough resources available?
            if (!CanMorphUnit(bwapi.UnitTypes_Zerg_Extractor) || !Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;

            TilePosition buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];
            // are there any geysers available/visible?
            IEnumerable<Unit> geysers = Interface()
                .GetGeysers().Where(geyser => geyser.getResources() > 0);
            if (geysers.Count() < 1)
                return false;


            // sort by closest path for ground units from selected build base
            TilePosition closest = geysers
                .OrderBy(geyser => geyser.getDistance(new Position(buildPosition)))
                .First().getTilePosition();
            
            // if there is a close geyers we are done
            if (closest is TilePosition)
            {
                this.buildLocation = closest;
                builder = Interface().GetBuilder(buildPosition);
                //move(new Position(closest), builder);
                // if (builder.getDistance(new Position(closest)) < DELTADISTANCE)
                //     return true;
                return true;
            }

            return false;
        }

        [ExecutableAction("BuildExtractor")]
        public bool BuildExtractor()
        {
            if (CanMorphUnit(bwapi.UnitTypes_Zerg_Extractor) && buildLocation is TilePosition)
            {

                return Build(bwapi.UnitTypes_Zerg_Extractor);
            }
            return false;
        }

        /// <summary>
        /// Select suitable location for the spawning pool
        /// </summary>
        /// <returns></returns>
        [ExecutableAction("PositionSpwnPl")]
        public bool PositionSpwnPl()
        {
            if (!Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;
            // TODO: this needs to be changed to a better location around the base taking exits and resources into account
            TilePosition buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];
            builder = Interface().GetBuilder(buildPosition); 
            
            buildPosition = PossibleBuildLocation(buildPosition, 1, 1, 100, builder, bwapi.UnitTypes_Zerg_Spawning_Pool);
            buildLocation = buildPosition;

            if (buildPosition is TilePosition)
            {
                move(new Position(buildPosition), builder);
                return true;
            }
            return false;
        }

        [ExecutableAction("BuildSpwnPl")]
        public bool BuildSpwnPl()
        {
            return Build(bwapi.UnitTypes_Zerg_Spawning_Pool);
        }

        /// <summary>
        /// Select suitable location for the spawning pool
        /// </summary>
        /// <returns></returns>
        [ExecutableAction("PositionHydraDen")]
        public bool PositionHydraDen()
        {
            if (!Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;
            // TODO: this needs to be changed to a better location around the base taking exits and resources into account
            TilePosition buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];
            builder = Interface().GetBuilder(buildPosition);
            
            buildPosition = PossibleBuildLocation(buildPosition, 1, 1, 200, builder, bwapi.UnitTypes_Zerg_Hydralisk_Den);
            buildLocation = buildPosition;
            if (buildPosition is TilePosition)
            {
                move(new Position(buildPosition), builder);
                return true;
            }

            return false;
        }

        [ExecutableAction("BuildHydraDen")]
        public bool BuildHydraDen()
        {
            return Build(bwapi.UnitTypes_Zerg_Hydralisk_Den);
        }


        [ExecutableAction("PositionHatchery")]
        public bool PositionHatchery()
        {
            if (!Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;

            TilePosition buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];
            builder = Interface().GetBuilder(buildPosition);
            buildPosition = PossibleBuildLocation(buildPosition, 1, 1, 100, builder, bwapi.UnitTypes_Zerg_Hatchery);
            
            buildLocation = buildPosition;

            move(new Position(buildPosition), builder);

            // if (builder.getDistance(new Position(buildPosition)) < DELTADISTANCE )
            //     return true;

            return true;
        }

        [ExecutableAction("BuildHatchery")]
        public bool BuildHatchery()
        {
            return Build(bwapi.UnitTypes_Zerg_Hatchery);
        }

        [ExecutableAction("UpgradeHatchery")]
        public bool UpgradeHatchery()
        {
            if (!CanMorphUnit(bwapi.UnitTypes_Zerg_Lair) || !Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;

            return Interface().GetHatcheries()
                .OrderBy(hatch => Interface().baseLocations[(int)Interface().currentBuildSite].getDistance(hatch.getTilePosition()))
                .First().morph(bwapi.UnitTypes_Zerg_Lair);
        }

        [ExecutableAction("PositionCreepColony")]
        public bool PositionCreepColony()
        {
            if (!Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;
            TilePosition buildPosition = Interface().baseLocations[(int)Interface().currentBuildSite];
            builder = Interface().GetBuilder(buildPosition);
            buildPosition = PossibleBuildLocation(buildPosition, 1, 1, 200, builder, bwapi.UnitTypes_Zerg_Creep_Colony);
            
            buildLocation = buildPosition;

            move(new Position(buildPosition), builder);
            if (builder.getDistance(new Position(buildPosition)) < DELTADISTANCE)
                return true;

            return false;
        }

        [ExecutableAction("BuildCreepColony")]
        public bool BuildCreepColony()
        {
            return Build(bwapi.UnitTypes_Zerg_Creep_Colony);
        }

        [ExecutableAction("UpgradeSunkenColony")]
        public bool UpgradeCreepColony()
        {
            if (!CanMorphUnit(bwapi.UnitTypes_Zerg_Sunken_Colony) || !Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;

            double dist = Interface().baseLocations[(int)Interface().currentBuildSite].getDistance(Interface().baseLocations[(int)BuildSite.StartingLocation]) / 3;

            return Interface().GetCreepColonies().Where(col => col.getDistance(new Position(Interface().baseLocations[(int)Interface().currentBuildSite])) < dist)
                .OrderByDescending(hatch => Interface().baseLocations[(int)Interface().currentBuildSite].getDistance(hatch.getTilePosition()))
                .ElementAt(0).morph(bwapi.UnitTypes_Zerg_Sunken_Colony);
        }

        [ExecutableAction("UpgradeSporeColony")]
        public bool UpgradeSporeColony()
        {
            if (!CanMorphUnit(bwapi.UnitTypes_Zerg_Spore_Colony) || !Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                return false;

            double dist = Interface().baseLocations[(int)Interface().currentBuildSite].getDistance(Interface().baseLocations[(int)BuildSite.StartingLocation]) / 3;

            return Interface().GetCreepColonies().Where(col => col.getDistance(new Position(Interface().baseLocations[(int)Interface().currentBuildSite])) < dist)
                .OrderByDescending(hatch => Interface().baseLocations[(int)Interface().currentBuildSite].getDistance(hatch.getTilePosition()))
                .ElementAt(0).morph(bwapi.UnitTypes_Zerg_Spore_Colony);
        }

        [ExecutableAction("RepairBuilding")]
        public bool RepairBuilding()
        {
            if (repairDrone == null || repairDrone.getHitPoints() <= 0 || buildingToRepair == null || buildingToRepair.getHitPoints() <= 0)
                return false;
            move(buildingToRepair.getPosition(), repairDrone);
            return repairDrone.repair(buildingToRepair, true);
        }


        [ExecutableAction("FinishedThreeHatchBuilding")]
        public bool FinishedThreeHatchBuilding()
        {
            needBuilding = false;

            return needBuilding;
        }

        //
        // SENSES
        //
        [ExecutableSense("NeedBuilding")]
        public bool NeedBuilding()
        {
            return needBuilding;
        }

        [ExecutableSense("HatcheryCount")]
        public int HatcheryCount()
        {
            return Interface().GetHatcheries().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Zerg_Hatchery) + CountUnbuiltBuildings(bwapi.UnitTypes_Zerg_Hatchery);
        }
        
        [ExecutableSense("SpawnPoolCount")]
        public int SpawnPoolCount()
        {
            return Interface().GetSpawningPools().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Zerg_Spawning_Pool) + CountUnbuiltBuildings(bwapi.UnitTypes_Zerg_Spawning_Pool);
        }
        [ExecutableSense("ExtractorCount")]
        public int ExtractorCount()
        {
            return Interface().GetExtractors().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Zerg_Extractor) + CountUnbuiltBuildings(bwapi.UnitTypes_Zerg_Extractor);
        }

        [ExecutableSense("HydraDenCount")]
        public int HydraDenCount()
        {
            return Interface().GetHydraDens().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Zerg_Hydralisk_Den) + CountUnbuiltBuildings(bwapi.UnitTypes_Zerg_Hydralisk_Den);
        }

        [ExecutableSense("LairCount")]
        public int LairCount()
        {
            return Interface().GetLairs().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Zerg_Lair) + CountUnbuiltBuildings(bwapi.UnitTypes_Zerg_Lair);
        }

        [ExecutableSense("SunkenColonyCount")]
        public int SunkenColonyCount()
        {
            return Interface().GetSunkenColonies().Count() + CountBuildingsinProgress(bwapi.UnitTypes_Zerg_Sunken_Colony) + CountUnbuiltBuildings(bwapi.UnitTypes_Zerg_Sunken_Colony);
        }



        /// <summary>
        /// Select a unit for building a structure
        /// </summary>
        /// <returns></returns>
        [ExecutableSense("HaveNaturalHatchery")]
        public bool NaturalHatchery()
        {
            TilePosition natural = Interface().baseLocations.ContainsKey((int)BuildSite.Natural) ? Interface().baseLocations[(int)BuildSite.Natural] : null;
            TilePosition start = Interface().baseLocations[(int)BuildSite.StartingLocation];

            // natural not known
            if (natural == null)
                return false;

            // arbitratry distance measure to determine if the hatchery is closer to the natural or the starting location
            double dist = new Position(natural).getDistance(new Position(start)) / 3;

            if (Interface().GetHatcheries().Where(hatch => hatch.getDistance(new Position(natural)) < dist).Count() > 0)
                return true;

            foreach (Unit unit in this.buildingInProgress.Keys)
                if (unit.getType().getID() == bwapi.UnitTypes_Zerg_Hatchery.getID() &&
                    unit.getDistance(new Position(natural)) < dist)
                    return true;

            return false;
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

            if (buildingToRepair == null || buildingToRepair.getHitPoints() <= 0 )
                buildingToRepair = buildings.First();

            return (repairDrone is Unit && buildingToRepair is Unit);
        }


        ////////////////////////////////////////////////////////////////////////Begining of James' Code////////////////////////////////////////////////////////////////////////


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
            return Position(bwapi.UnitTypes_Protoss_Cybernetics_Core, 1, 1, 200);
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
            return Position(bwapi.UnitTypes_Protoss_Pylon, 1, 1, 100);
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
            return Position(bwapi.UnitTypes_Protoss_Gateway, 1, 1, 150);
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