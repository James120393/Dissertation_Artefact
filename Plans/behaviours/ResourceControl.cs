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
        // ACTIONS
        //

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

        ////////////////////////////////////////////////////////////////////////End of James' Code////////////////////////////////////////////////////////////////////////////


        //
        // SENSES
        //

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

        ////////////////////////////////////////////////////////////////////////Begining of James' Code////////////////////////////////////////////////////////////////////////

        //Sense to tell AI if they have the protoss attack upgreade 1
        [ExecutableSense("HaveAttackUpgrade")]
        public bool HaveAttackUpgrade()
        {
            return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Protoss_Ground_Weapons) > 0);
        }


        //Sense to tell AI if they have the protoss Dragoon range upgreade
        [ExecutableSense("HaveDragoonRange")]
        public bool HaveDragoonRange()
        {
            return (Interface().Self().getUpgradeLevel(bwapi.UpgradeTypes_Singularity_Charge) > 0);
        }        
        ////////////////////////////////////////////////////////////////////////End of James' Code////////////////////////////////////////////////////////////////////////////        
    }
}
