using Chris.OS.Additions.Utils;
using Mono.Addins;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Script.Functions.osSetInventoryDesc
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "osSetInventoryDesc")]

    class SetInventoryDesc : EmptyModule
    {
        #region EmptyModule
        public override string Name
        {
            get { return "osSetInventoryDesc"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();
                m_scriptModule.RegisterScriptInvocation(this, "osSetInventoryDesc");
                m_scriptModule.RegisterScriptInvocation(this, "osGetInventoryDesc");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }
        }
        #endregion

        #region Script functions
        [ScriptInvocation]
        public void osSetInventoryDesc(UUID hostID, UUID scriptID, String itemName, String desc)
        {
            SceneObjectPart part = base.World.GetSceneObjectPart(hostID);

            TaskInventoryItem item = part.Inventory.GetInventoryItems().Find(x => x.Name.Equals(itemName));

            if (item == null)
                return;

            item.Description = desc;
            part.Inventory.AddInventoryItemExclusive(item, false);
            part.SendFullUpdateToAllClients();
        }

        [ScriptInvocation]
        public String osGetInventoryDesc(UUID hostID, UUID scriptID, String itemName)
        {
            SceneObjectPart part = base.World.GetSceneObjectPart(hostID);

            TaskInventoryItem item = part.Inventory.GetInventoryItems().Find(x => x.Name.Equals(itemName));

            if (item == null)
                throw new Exception("Item not found!");

            return item.Description;
        }
        #endregion
    }
}
