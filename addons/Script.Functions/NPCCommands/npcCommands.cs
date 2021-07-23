using Chris.OS.Additions.Utils;
using Mono.Addins;
using OpenMetaverse;
using OpenSim.Framework;
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

            base.Logger.Info("[NPC-COMMANDS] Waiting for NPCs");
        }

        private void NewPresence(ScenePresence presence)
        {
            if (!presence.IsNPC)
                return;

            base.Logger.Info("[NPC-COMMANDS] Create Inventory for " + presence.UUID + "(" + presence.Name + ")");

            m_inventoryService.CreateUserInventory(presence.UUID);

            UUID rootFolder = m_inventoryService.GetRootFolder(presence.UUID).ID;

            InventoryFolderBase objFolder = new InventoryFolderBase();
            objFolder.Type = 6;
            objFolder.Name = "Objects";
            objFolder.Owner = presence.UUID;
            objFolder.ParentID = rootFolder;
            m_inventoryService.AddFolder(objFolder);
        }
        #endregion
    }
}
