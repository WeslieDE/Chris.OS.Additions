using Chris.OS.Additions.Utils;
using Mono.Addins;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Drawing;
using LSL_Vector = OpenSim.Region.ScriptEngine.Shared.LSL_Types.Vector3;

namespace Chris.OS.Additions.Script.Functions.LSLBitMap
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "LSLBitMap")]

    class LSLBitMap : EmptyModule
    {
        private Dictionary<int, Bitmap> m_bitmaps = new Dictionary<int, Bitmap>();

        #region EmptyModule
        public override string Name
        {
            get { return "LSL_Bitmap"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();
                m_scriptModule.RegisterScriptInvocation(this, "osCreateBitmap");
                m_scriptModule.RegisterScriptInvocation(this, "osLoadBitmap");
                m_scriptModule.RegisterScriptInvocation(this, "osUnloadBitmap");
                m_scriptModule.RegisterScriptInvocation(this, "osSaveBitmap");
                m_scriptModule.RegisterScriptInvocation(this, "osResizeBitmap");
                m_scriptModule.RegisterScriptInvocation(this, "osGetBitmapPixel");
                m_scriptModule.RegisterScriptInvocation(this, "osSetBitmapPixel");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }
        }
        #endregion

        #region Script functions
        [ScriptInvocation]
        public int osCreateBitmap(UUID hostID, UUID scriptID, int sizeX, int sizeY)
        {
            lock(m_bitmaps)
            {
                int newImageID = m_bitmaps.Count + 1;

                if(sizeX == 0)
                    sizeX = 1;

                if(sizeY == 0)
                    sizeY = 1;

                if (sizeX > 8192)
                    sizeX = 8192;

                if (sizeY > 8192)
                    sizeY = 8192;

                Bitmap bitmap = new Bitmap(sizeX, sizeY);
                m_bitmaps.Add(newImageID, bitmap);

                return newImageID;
            }
        }

        [ScriptInvocation]
        public int osLoadBitmap(UUID hostID, UUID scriptID, String inventoryName)
        {
            SceneObjectPart part = base.World.GetSceneObjectPart(hostID);

            TaskInventoryItem item = part.Inventory.GetInventoryItems().Find(x => x.Name.Equals(inventoryName));

            if (item == null)
                return 0;

            if (base.World.AssetService.AssetsExist(new String[] { item.AssetID.ToString() })[0])
            {
                AssetBase asset = base.World.AssetService.Get(item.AssetID.ToString());

                if (asset != null)
                {
                    if (asset.Type == (sbyte)AssetType.Texture)
                    {
                        if (OpenMetaverse.Imaging.OpenJPEG.DecodeToImage(asset.Data, out OpenMetaverse.Imaging.ManagedImage jpegImageData))
                        {
                            lock (m_bitmaps)
                            {
                                int newImageID = m_bitmaps.Count + 1;

                                Bitmap bitmap = jpegImageData.ExportBitmap();
                                m_bitmaps.Add(newImageID, bitmap);

                                return newImageID;
                            }
                        }
                    }
                }
            }

            return 0;
        }

        [ScriptInvocation]
        public int osUnloadBitmap(UUID hostID, UUID scriptID, int bitmapID)
        {
            if (m_bitmaps.ContainsKey(bitmapID))
            {
                m_bitmaps.Remove(bitmapID);
                return 1;
            }

            return 0;
        }

        [ScriptInvocation]
        public int osSaveBitmap(UUID hostID, UUID scriptID, int bitmapID, String name)
        {
            SceneObjectPart part = base.World.GetSceneObjectPart(hostID);

            if (m_bitmaps.TryGetValue(bitmapID, out Bitmap image))
            {
                byte[] imageArray = OpenMetaverse.Imaging.OpenJPEG.EncodeFromImage(image, true);

                AssetBase asset = new AssetBase();
                asset.CreatorID = part.OwnerID.ToString();
                asset.FullID = UUID.Random();

                asset.Name = "Image from LSLBitmap Module.";
                asset.Description = "https://github.com/Sahrea/Chris.OS.Additions";
                asset.Temporary = false;
                asset.Type = (int)AssetType.Texture;
                asset.Data = imageArray;
                base.World.AssetService.Store(asset);

                TaskInventoryItem item = new TaskInventoryItem();
                item.Name = name;
                item.AssetID = asset.FullID;
                item.Type = 0;
                item.PermsMask = 581639;
                item.NextPermissions = 581639;
                item.CurrentPermissions = 581639;

                part.Inventory.AddInventoryItemExclusive(item, false);
                return 1;
            }

            return 0;
        }

        [ScriptInvocation]
        public int osResizeBitmap(UUID hostID, UUID scriptID, int bitmapID, int sizeX, int sizeY)
        {
            if (sizeX > 8192)
                sizeX = 8192;

            if (sizeY > 8192)
                sizeY = 8192;

            lock(m_bitmaps)
            {
                if (m_bitmaps.TryGetValue(bitmapID, out Bitmap bitmap))
                {
                    m_bitmaps.Remove(bitmapID);
                    m_bitmaps.Add(bitmapID, new Bitmap(bitmap, sizeX, sizeY));
                    return 1;
                }
            }

            return 0;
        }

        [ScriptInvocation]
        public Vector3 osGetBitmapPixel(UUID hostID, UUID scriptID, int bitmapID, int posX, int posY)
        {
            if (m_bitmaps.TryGetValue(bitmapID, out Bitmap bitmap))
            {
                if (bitmap.Width > posX && posX > 0)
                    return new Vector3();

                if (bitmap.Height > posY && posY > 0)
                    return new Vector3();

                Color pixelColor = bitmap.GetPixel(posX, posY);

                return new Vector3(pixelColor.R, pixelColor.G, pixelColor.B);
            }

            return new Vector3();
        }

        [ScriptInvocation]
        public int osSetBitmapPixel(UUID hostID, UUID scriptID, int bitmapID, int posX, int posY, Vector3 color)
        {
            lock(m_bitmaps)
            {
                if (m_bitmaps.TryGetValue(bitmapID, out Bitmap bitmap))
                {
                    if (bitmap.Width > posX && posX > 0)
                        return 0;

                    if (bitmap.Height > posY && posY > 0)
                        return 0;

                    m_bitmaps.Remove(bitmapID);

                    Color pixelColor = Color.FromArgb((int)color.X, (int)color.Y, (int)color.Z);
                    bitmap.SetPixel(posX, posY, pixelColor);
                    m_bitmaps.Add(bitmapID, bitmap);

                    return 1;
                }
            }

            return 0;
        }
        #endregion
    }
}
