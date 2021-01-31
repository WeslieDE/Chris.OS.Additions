using Mono.Addins;
using Nini.Config;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Framework;
using OpenMetaverse;

[assembly: Addin("RegionFullPerm", "0.2")]
[assembly: AddinDependency("OpenSim.Region.Framework", OpenSim.VersionInfo.VersionNumber)]
namespace OpenSim.Modules.FullPerm
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "RegionFullPerm")]
    class FullPerm : INonSharedRegionModule
    {
        List<Scene> m_scenes = new List<Scene>();

        public string Name
        {
            get { return "RegionFullPerm"; }
        }

        public void AddRegion(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }

        public void Close()
        {
        }

        public void Initialise(IConfigSource source)
        {

        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void RegionLoaded(Scene scene)
        {
            scene.EventManager.OnObjectAddedToScene += addObject;
            scene.EventManager.OnSceneObjectLoaded += addObject;
            scene.EventManager.OnIncomingSceneObject += addObject;
            scene.EventManager.OnSceneObjectPartUpdated += updateObject;
            scene.EventManager.OnNewInventoryItemUploadComplete += addInventory;
            scene.EventManager.OnSceneObjectPartCopy += copyObject;
            scene.EventManager.OnNewClient += taskInventoryUpdate;
        }

        private void taskInventoryUpdate(IClientAPI client)
        {
            client.OnUpdateTaskInventory += updateTaskInventory;
            client.OnUpdateInventoryItem += updateInventoryItem;
        }

        private void updateInventoryItem(IClientAPI remoteClient, UUID transactionID, UUID itemID, InventoryItemBase itemUpd)
        {
            if (itemUpd == null)
                return;

            itemUpd.CurrentPermissions = 581639;
            itemUpd.NextPermissions = 581639;
            itemUpd.BasePermissions = 581639;
        }

        private void updateTaskInventory(IClientAPI remoteClient, UUID transactionID, TaskInventoryItem item, uint localID)
        {
            if (item == null)
                return;

            item.CurrentPermissions = 581639;
            item.NextPermissions = 581639;
            item.BasePermissions = 581639;
        }

        private void copyObject(SceneObjectPart copy, SceneObjectPart original, bool userExposed)
        {
            if (copy == null)
                return;

            updateObject(copy, true);
        }

        private void addInventory(InventoryItemBase item, int userlevel)
        {
            if (item == null)
                return;

            item.BasePermissions = 581639;
            item.CurrentPermissions = 581639;
            item.NextPermissions = 581639;
        }

        private void updateObject(SceneObjectPart sop, bool full)
        {
            if (sop == null)
                return;

            sop.BaseMask = 581639;
            sop.OwnerMask = 581639;
            sop.NextOwnerMask = 581639;

            foreach (TaskInventoryItem _item in sop.Inventory.GetInventoryItems())
            {
                _item.BasePermissions = 581639;
                _item.NextPermissions = 581639;
                _item.CurrentPermissions = 581639;
            }
        }

        private void addObject(SceneObjectGroup obj)
        {
            if (obj == null)
                return;

            if (obj.IsAttachment)
                return;

            if (obj.RootPart.ObjectSaleType != (byte)SaleType.Not)
                obj.RootPart.SalePrice = 0;

            foreach (SceneObjectPart _part in obj.Parts)
            {
                _part.BaseMask = 581639;
                _part.OwnerMask = 581639;
                _part.NextOwnerMask = 581639;

                if(_part.ObjectSaleType != (byte)SaleType.Not)
                    _part.SalePrice = 0;               

                foreach(TaskInventoryItem _item in _part.Inventory.GetInventoryItems())
                {
                    _item.BasePermissions = 581639;
                    _item.NextPermissions = 581639;
                    _item.CurrentPermissions = 581639;
                }
            }
        }
    }
}
