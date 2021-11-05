﻿using Chris.OS.Additions.Utils;
using Mono.Addins;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;

namespace Chris.OS.Additions.Script.Functions.LSLBitMap
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "LSLBitMap")]

    class LSLBitMap : EmptyModule
    {
        private Dictionary<UUID, Bitmap> m_bitmaps = new Dictionary<UUID, Bitmap>();

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
                m_scriptModule.RegisterScriptInvocation(this, "osLoadBitmapFromURL");

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
        public UUID osCreateBitmap(UUID hostID, UUID scriptID, int sizeX, int sizeY)
        {
            lock(m_bitmaps)
            {
                UUID newImageID = UUID.Random();

                if (sizeX == 0)
                    sizeX = 1;

                if(sizeY == 0)
                    sizeY = 1;

                Bitmap bitmap = new Bitmap(sizeX, sizeY);
                m_bitmaps.Add(newImageID, bitmap);

                return newImageID;
            }
        }

        [ScriptInvocation]
        public UUID osLoadBitmap(UUID hostID, UUID scriptID, String inventoryName)
        {
            SceneObjectPart part = base.World.GetSceneObjectPart(hostID);

            TaskInventoryItem item = part.Inventory.GetInventoryItems().Find(x => x.Name.Equals(inventoryName));

            if (item == null)
                return UUID.Zero;

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
                                UUID newImageID = UUID.Random();

                                Bitmap bitmap = jpegImageData.ExportBitmap();
                                m_bitmaps.Add(newImageID, bitmap);

                                return newImageID;
                            }
                        }
                    }
                }
            }

            return UUID.Zero;
        }

        [ScriptInvocation]
        public UUID osLoadBitmapFromURL(UUID hostID, UUID scriptID, String url)
        {
            try
            {
                using (WebClient client = new WebClient())
                {
                    byte[] image = client.DownloadData(url);
                    Image tempImage = Image.FromStream(new MemoryStream(image));

                    lock (m_bitmaps)
                    {
                        UUID newImageID = UUID.Random();

                        Bitmap bitmap = new Bitmap(tempImage.Width, tempImage.Height);
                        m_bitmaps.Add(newImageID, bitmap);

                        return newImageID;
                    }
                }
            }
            catch
            {
                return UUID.Zero;
            }
        }

        [ScriptInvocation]
        public int osUnloadBitmap(UUID hostID, UUID scriptID, String bitmapID)
        {
            if(UUID.TryParse(bitmapID, out UUID imageID))
            {
                if (m_bitmaps.ContainsKey(imageID))
                {
                    m_bitmaps.Remove(imageID);
                    return 1;
                }
            }

            return 0;
        }

        [ScriptInvocation]
        public int osSaveBitmap(UUID hostID, UUID scriptID, String bitmapID, String name)
        {
            SceneObjectPart part = base.World.GetSceneObjectPart(hostID);

            if (UUID.TryParse(bitmapID, out UUID imageID))
            {
                if (m_bitmaps.TryGetValue(imageID, out Bitmap image))
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
                    String assetID = base.World.AssetService.Store(asset);

                    if (assetID == String.Empty)
                        return 0;

                    TaskInventoryItem item = new TaskInventoryItem();
                    item.Name = name;
                    item.AssetID = UUID.Parse(assetID);
                    item.OwnerID = part.OwnerID;
                    item.CreatorID = part.OwnerID;
                    item.LastOwnerID = part.OwnerID;

                    item.Type = 0;
                    item.PermsMask = 581639;
                    item.NextPermissions = 581639;
                    item.CurrentPermissions = 581639;
                    item.EveryonePermissions = 581639;

                    part.Inventory.AddInventoryItemExclusive(item, false);
                    part.SendFullUpdateToAllClients();

                    return 1;
                }
            }

            return 0;
        }

        [ScriptInvocation]
        public int osResizeBitmap(UUID hostID, UUID scriptID, String bitmapID, int sizeX, int sizeY)
        {
            if (sizeX > 8192)
                sizeX = 8192;

            if (sizeY > 8192)
                sizeY = 8192;

            lock(m_bitmaps)
            {
                if (UUID.TryParse(bitmapID, out UUID imageID))
                {
                    if (m_bitmaps.TryGetValue(imageID, out Bitmap bitmap))
                    {
                        m_bitmaps.Remove(imageID);
                        m_bitmaps.Add(imageID, new Bitmap(bitmap, sizeX, sizeY));
                        return 1;
                    }
                }
            }

            return 0;
        }

        [ScriptInvocation]
        public Vector3 osGetBitmapPixel(UUID hostID, UUID scriptID, String bitmapID, int posX, int posY)
        {
            if (UUID.TryParse(bitmapID, out UUID imageID))
            {
                if (m_bitmaps.TryGetValue(imageID, out Bitmap bitmap))
                {
                    Color pixelColor = bitmap.GetPixel(posX, posY);

                    return new Vector3(pixelColor.R, pixelColor.G, pixelColor.B);
                }
            }

            return new Vector3();
        }

        [ScriptInvocation]
        public int osSetBitmapPixel(UUID hostID, UUID scriptID, String bitmapID, int posX, int posY, Vector3 color)
        {
            lock(m_bitmaps)
            {
                try
                {
                    if (UUID.TryParse(bitmapID, out UUID imageID))
                    {
                        if (m_bitmaps.TryGetValue(imageID, out Bitmap bitmap))
                        {
                            Color pixelColor = Color.FromArgb((int)color.X, (int)color.Y, (int)color.Z);
                            bitmap.SetPixel(posX, posY, pixelColor);

                            m_bitmaps.Remove(imageID);
                            m_bitmaps.Add(imageID, bitmap);

                            return 1;
                        }
                    }
                }
                catch
                {
                    //Nothing
                }
            }

            return 0;
        }
        #endregion
    }
}