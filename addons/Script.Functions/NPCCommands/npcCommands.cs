using Chris.OS.Additions.Utils;
using Mono.Addins;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Script.Functions.NPCCommands
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "NPCCommands")]

    class npcCommands : EmptyModule
    {
        private IInventoryService m_inventoryService = null;

        #region EmptyModule
        public override string Name
        {
            get { return "NPCCommands"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            base.World.EventManager.OnNewPresence += NewPresence;

            m_inventoryService = World.RequestModuleInterface<IInventoryService>();
        }

        private void NewPresence(ScenePresence presence)
        {
            if (!presence.IsNPC)
                return;

            if (presence.IsChildAgent)
                return;

            m_inventoryService.CreateUserInventory(presence.UUID);
            base.Logger.Info("[NPC-COMMANDS] Create Inventory for " + presence.UUID + "(" + presence.Name + ")");
        }
        #endregion
    }
}
