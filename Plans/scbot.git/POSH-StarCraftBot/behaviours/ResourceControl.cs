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

        public ResourceControl(AgentBase agent)
            : base(agent, new string[] {}, new string[] {})
        {

        }
        //
        // INTERNAL
        //

        //
        // ACTIONS
        //
        [ExecutableAction("HydraSpeedUpgrade")]
        public bool HydraSpeedUpgrade()
        {
            return Interface().GetHydraDens().Where(den => !den.isUpgrading() && den.getHitPoints() > 0).First().upgrade(bwapi.UpgradeTypes_Muscular_Augments);
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
            return Interface().GetForge().Where(forge => forge.getHitPoints() > 0).First().upgrade(bwapi.UpgradeTypes_Protoss_Ground_Weapons);
        }


        //Action to tell AI to research the Protoss Dragoon Range upgrade
        [ExecutableAction("DragoonRangeUpgrade")]
        public bool DragoonRangeUpgrade()
        {
            return Interface().GetCyberneticsCore().Where(core => core.getHitPoints() > 0).First().upgrade(bwapi.UpgradeTypes_Singularity_Charge);
        }

        //Action to tell AI to research the Protoss Shield upgrade
        [ExecutableAction("ShieldUpgrade")]
        public bool ShieldUpgrade()
        {
            return Interface().GetCyberneticsCore().Where(core => core.getHitPoints() > 0).First().upgrade(bwapi.UpgradeTypes_Protoss_Plasma_Shields);
        }
        ////////////////////////////////////////////////////////////////////////End of James' Code////////////////////////////////////////////////////////////////////////////


        //
        // SENSES
        //
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
            return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Protoss_Ground_Weapons) > 0);
        }


        //Sense to tell AI if they have the protoss Dragoon range upgreade
        [ExecutableSense("HaveDragoonRange")]
        public bool HaveDragoonRange()
        {
            return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Singularity_Charge) > 0);
        }

        //Sense to tell AI if they have the protoss Shield upgreade
        [ExecutableSense("HaveShield")]
        public bool HaveShield()
        {
            return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Protoss_Plasma_Shields) > 0);
        }       
        ////////////////////////////////////////////////////////////////////////End of James' Code////////////////////////////////////////////////////////////////////////////        
    }
}
