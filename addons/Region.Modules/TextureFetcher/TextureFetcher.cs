using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using static OpenMetaverse.Primitive;

namespace Chris.Os.Additions.TextureFetcher
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "TextureFetcher")]
    class TextureFetcher : INonSharedRegionModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        Scene m_scene = null;

        private IConfigSource m_config;

        List<UUID> m_textureBlackList = new List<UUID>();
        bool m_checkTexture = false;

        List<String> m_defaultTexturs = new List<String>(new string[] { "89556747-24cb-43ed-920b-47caed15465f", "5748decc-f629-461c-9a36-a35a221fe21f", "8dcd4a48-2d37-4909-9f78-f7a9eb4ef903", "8b5fec65-8d8d-9dc5-cda8-8fdf2716e361", "38b86f85-2575-52a9-a531-23108d8da837", "e97cf410-8e61-7005-ec06-629eba4cd1fb", "5a9f4a74-30f2-821c-b88d-70499d3e7183	", "ae2de45c-d252-50b8-5c6e-19f39ce79317	", "24daea5f-0539-cfcf-047f-fbc40b2786ba", "52cc6bb6-2ee5-e632-d3ad-50197b1dcb8a", "43529ce8-7faa-ad92-165a-bc4078371687", "09aac1fb-6bce-0bee-7d44-caac6dbb6c63", "ff62763f-d60a-9855-890b-0c96f8f8cd98", "8e915e25-31d1-cc95-ae08-d58a47488251", "9742065b-19b5-297c-858a-29711d539043", "03642e83-2bd1-4eb9-34b4-4c47ed586d2d", "edd51b77-fc10-ce7a-4b3d-011dfc349e4f"});
        private bool m_enable = false;

        #region INonSharedRegionModule
        public string Name
        {
            get { return "TextureFetcher"; }
        }

        public void Initialise(IConfigSource source)
        {
            m_config = source;

            if (m_config.Configs["TextureFetcher"] != null)
            {
                m_enable = m_config.Configs["TextureFetcher"].GetBoolean("Enable", m_enable);
                m_checkTexture = m_config.Configs["TextureFetcher"].GetBoolean("TextureFetcherCheckAssets", m_checkTexture);
            }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void RegionLoaded(Scene scene)
        {
            m_scene = scene;

            m_log.Info("[" + Name + "] Region '" + scene.Name + "' loaded.");

            if (m_enable == false)
            {
                scene.EventManager.OnObjectAddedToScene += ClearObjekt;
                scene.EventManager.OnSceneObjectLoaded += ClearObjekt;
                scene.EventManager.OnIncomingSceneObject += ClearObjekt;
                scene.EventManager.OnSceneObjectPartUpdated += ClearObjekt;
                scene.EventManager.OnSceneObjectPartCopy += ClearObjekt;

                return;
            }

            scene.EventManager.OnObjectAddedToScene += AddObject;
            scene.EventManager.OnSceneObjectLoaded += AddObject;
            scene.EventManager.OnIncomingSceneObject += AddObject;
            scene.EventManager.OnSceneObjectPartUpdated += UpdateObject;
            scene.EventManager.OnSceneObjectPartCopy += CopyObject;
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
        #endregion

        #region Events
        private void ClearObjekt(SceneObjectPart copy, SceneObjectPart original, bool userExposed)
        {
            removeTexturesToInventory(copy);
        }

        private void ClearObjekt(SceneObjectPart sop, bool full)
        {
            removeTexturesToInventory(sop);
        }

        private void ClearObjekt(SceneObjectGroup so)
        {
            foreach (SceneObjectPart _part in so.Parts)
                removeTexturesToInventory(_part);
        }

        private void CopyObject(SceneObjectPart copy, SceneObjectPart original, bool userExposed)
        {
            copyTexturesToInventory(copy);
        }

        private void UpdateObject(SceneObjectPart sop, bool full)
        {
            copyTexturesToInventory(sop);
        }

        private void AddObject(SceneObjectGroup obj)
        {
            foreach (SceneObjectPart _part in obj.Parts)
                copyTexturesToInventory(_part);
        }
        #endregion

        #region Functions
        private bool inventoryContainsScripts(SceneObjectPart part)
        {
            foreach (TaskInventoryItem item in part.ParentGroup.RootPart.Inventory.GetInventoryItems())
                if (item.InvType == 10)
                {
                    AssetBase assetData = m_scene.AssetService.Get(item.AssetID.ToString());

                    if (assetData != null)
                    {
                        String script = new ASCIIEncoding().GetString(assetData.Data).ToUpper();

                        if (script.Contains("INVENTORY_TEXTURE"))
                            return true;

                        if (script.Contains("INVENTORY_ALL"))
                            return true;
                    }
                    else
                    {
                        return true;
                    }
                }

            return false;
        }

        private bool isAssetInInventory(SceneObjectPart part, UUID assetID)
        {
            List<TaskInventoryItem> inventarContend = part.ParentGroup.RootPart.Inventory.GetInventoryItems();

            foreach (TaskInventoryItem item in inventarContend)
                if (item.AssetID == assetID)
                    return true;

            return false;
        }

        private void removeTexturesToInventory(SceneObjectPart part)
        {
            if (m_enable == true)
                return;

            if (m_scene.LoginsEnabled == true)
                return;

            if (inventoryContainsScripts(part.ParentGroup.RootPart))
                return;

            try
            {

                foreach (TaskInventoryItem item in part.ParentGroup.RootPart.Inventory.GetInventoryItems())
                {
                    if (item.Type == (int)InventoryType.Texture)
                    {
                        if (item.Description == "This item was automatically generated by the texture fetcher module.")
                            part.ParentGroup.RootPart.Inventory.RemoveInventoryItem(item.ItemID);
                    }
                }
            }
            catch (Exception _error)
            {
                m_log.Error("[" + Name + "] ERROR: " + _error.Message);
            }
        }

        private void copyTexturesToInventory(SceneObjectPart part)
        {
            if (m_enable == false)
                return;

            if (m_scene.LoginsEnabled == false)
                return;

            if (part.ParentGroup.RootPart.ClickAction == 2)
                return;

            if ((part.ParentGroup.RootPart.GetEffectiveObjectFlags() & (uint)PrimFlags.ObjectCopy) != 0)
                return;

            //if ((setPermissionMask & (uint)PermissionMask.Copy) != 0)
            //return;

            if (inventoryContainsScripts(part.ParentGroup.RootPart))
                return;

            try
            {
                List<UUID> allTextures = new List<UUID>();
                List<TaskInventoryItem> inventoryItems = new List<TaskInventoryItem>();

                //Get all Textures from the scene object
                Primitive.TextureEntry textures = part.Shape.Textures;
                int allSides = part.GetNumberOfSides();

                for (uint i = 0; i < allSides; i++)
                {
                    TextureEntryFace face = textures.GetFace(i);

                    if (!m_textureBlackList.Contains(face.TextureID) && !m_defaultTexturs.Contains(face.TextureID.ToString()))
                        allTextures.Add(face.TextureID);

                    if (m_textureBlackList.Contains(face.TextureID))
                        face.TextureID = UUID.Parse("89556747-24cb-43ed-920b-47caed15465f");
                }

                //Remove not existing textures
                if (m_checkTexture == true)
                {
                    String[] _assetIDs = new string[allTextures.Count];
                    for (int i = 0; i < allTextures.Count; i++)
                        _assetIDs[i] = allTextures[i].ToString();

                    bool[] existing = m_scene.AssetService.AssetsExist(_assetIDs);
                    for (int i = 0; i < existing.Length; i++)
                        if (existing[i] == false)
                        {
                            m_textureBlackList.Add(allTextures[i]);
                            allTextures[i] = UUID.Zero;
                        }
                }

                //Convert texture uuid list to inventar items.
                foreach (UUID texture in allTextures)
                {
                    if (texture != UUID.Zero)
                    {
                        String partname = part.Name.Trim();

                        if (partname == "")
                            partname = "tf";

                        TaskInventoryItem item = new TaskInventoryItem();
                        item.AssetID = texture;
                        item.ItemID = UUID.Random();
                        item.OwnerID = part.OwnerID;
                        item.CurrentPermissions = 581639;
                        item.Name = partname + " - Texture";
                        item.Description = "This item was automatically generated by the texture fetcher module.";
                        item.OwnerID = part.OwnerID;
                        item.CreatorID = part.OwnerID;
                        item.LastOwnerID = part.OwnerID;
                        inventoryItems.Add(item);
                    }
                }

                //Check if the item is allready in the inventory and then add it.
                bool updateNeeded = false;
                foreach (TaskInventoryItem item in inventoryItems)
                {
                    if (!isAssetInInventory(part, item.AssetID))
                    {
                        updateNeeded = true;

                        part.ParentGroup.RootPart.Inventory.AddInventoryItem(item, false);
                    }
                }

                if (updateNeeded == true)
                {
                    //part.SendFullUpdateToAllClients();
                }
            }
            catch (Exception _error)
            {
                m_log.Error("[" + Name + "] ERROR: " + _error.Message);
            }
        }
        #endregion
    }
}
