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
        private int ConvertTilePosition(TilePosition pos)
        {
            return (pos.xConst() * 1000) + pos.yConst();
        }


        // Check to see if any units are currently being trained
        protected int CheckForTrainingUnits(UnitType type)
        {
            if (!trainingUnits.ContainsKey(type.getID()))
                return 0;
            trainingUnits[type.getID()].RemoveAll(unit => !unit.isTraining());

            return trainingUnits[type.getID()].Count;
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

            if (probes.Count() < 1 || resources.Count() < 1)
                return executed;

            // Update all minded Patches by removing non harvesting probes or dead ones
            foreach (KeyValuePair<int, List<Unit>> patch in minedPatches)
            {
                patch.Value.RemoveAll(probe => (probe.getHitPoints() <= 0 || probe.getOrderTarget() == null || ConvertTilePosition(probe.getOrderTarget().getTilePosition()) != patch.Key));
            }

            foreach (Unit probe in probes)
            {
                if (maxUnits < 1)
                    break;

                if (resources.Contains(probe.getOrderTarget()) && probe.getTarget().getResources() > 0 &&
                    mined.ContainsKey(ConvertTilePosition(probe.getOrderTarget().getTilePosition())))
                {
                    Console.Out.WriteLine("test");
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
                int secCounter = patchPositions.Count() + 1;
                while (!(probe.getTarget() is Unit && probe.getTarget().getID() == finalPatch.getID()) && !probe.isMoving() && secCounter-- > 0)
                {
                    executed = probe.gather(finalPatch, false);
                    maxUnits--;
                    System.Threading.Thread.Sleep(50);
                    // if (_debug_)
                    Console.Out.WriteLine("Probe is gathering: " + executed);
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
        protected bool TrainUnit(UnitType type)
        {
            if (CanTrainUnit(type))
            {
                int targetLocation = (int)BuildSite.StartingLocation;
                if (Interface().baseLocations.ContainsKey((int)Interface().currentBuildSite))
                    targetLocation = (int)Interface().currentBuildSite;
                IEnumerable<Unit> nexuss = Interface().GetNexus();
                if (nexuss.Count() <= 0)
                    return false;
                Unit nexus = nexuss.OrderBy(unit => unit.getDistance(new Position(Interface().baseLocations[targetLocation]))).First();
                bool trainWorked = nexus.train(type);

                // create new list to monitor specific type of unit
                if (!trainingUnits.ContainsKey(type.getID()))
                    trainingUnits[type.getID()] = new List<Unit>();

                // adding the moved unit to the appropriate unit list
                if (trainWorked)
                    if (Interface().forcePoints.ContainsKey(Interface().currentForcePoint))
                        nexus.move(new Position(Interface().forcePoints[Interface().currentForcePoint]));
                    else
                        nexus.move(new Position(Interface().baseLocations[targetLocation]));
                return trainWorked;
            }
            return false;
        }


        //
        // SENSES
        //

        // Action to tell the AI that its forces are finished being trained
        [ExecutableSense("FinishedForce")]
        public bool FinishedForce()
        {
            forceReady = true;
            return forceReady;
        }


        // Sense to tell the AI that their forces are ready
        [ExecutableSense("ForceReady")]
        public bool ForceReady()
        {
            return forceReady;
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
            return Interface().ZealotCount() + CheckForTrainingUnits(bwapi.UnitTypes_Protoss_Zealot);
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

        //
        // ACTIONS
        //

        //Action to tell the AI to Build a Protoss Probe
        [ExecutableAction("BuildProbe")]
        public bool BuildProbe()
        {
            return TrainUnit(bwapi.UnitTypes_Protoss_Probe);
        }


        //Action to tell the AI to Build a Protoss Zealot
        [ExecutableAction("TrainZealot")]
        public bool TrainZealot()
        {
            return TrainUnit(bwapi.UnitTypes_Protoss_Zealot);
        }


        //Action to tell the AI to Build a Protoss Dragoon
        [ExecutableAction("TrainDragoon")]
        public bool TrainDragoon()
        {
            return TrainUnit(bwapi.UnitTypes_Protoss_Dragoon);
        }


        //Action to tell the AI to Build a Protoss Corsair
        [ExecutableAction("TrainCorsair")]
        public bool TrainCorsair()
        {
            return TrainUnit(bwapi.UnitTypes_Protoss_Corsair);
        }


        //Action to tell the AI to Build a Protoss Dark Templar
        [ExecutableAction("TrainDarkTemplar")]
        public bool TrainDarkTemplar()
        {
            return TrainUnit(bwapi.UnitTypes_Protoss_Dark_Templar);
        }

        ////////////////////////////////////////////////////////////////////////End of James' Code////////////////////////////////////////////////////////////////////////


        //Action to tell the AI to Assign Probes to gather minerals
        [ExecutableAction("AssignProbes")]
        public bool ProbesToMineral()
        {
            IEnumerable<Unit> mineralPatches = Interface().GetMineralPatches();
            return ProbesToResource(mineralPatches, minedPatches, 2, true, 1);
        }


        //Action to tell the AI to Assign Probes to gather Vespin Gas
        [ExecutableAction("AssignToGas")]
        public bool ProbesToGas()
        {
            IEnumerable<Unit> extractors = Interface().GetAssimilator();

            return ProbesToResource(extractors, minedGas, 6, false, 1);
        }

        
    }
}
