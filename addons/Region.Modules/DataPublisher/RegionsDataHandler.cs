using Chris.OS.Additions.Region.Modules.DataPublisher.Data;
using log4net;
using Newtonsoft.Json;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Chris.OS.Additions.Region.Modules.DataPublisher
{
    class RegionsDataHandler : BaseStreamHandler
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private IConfigSource m_config;
        private List<Scene> m_scenes = new List<Scene>();
        private IUserManagement m_userManager = null;

        public RegionsDataHandler(IConfigSource config, ref List<Scene> scenes) : base("GET", "/RegionData")
        {
            m_log.Info("[RegionsDataModule] RegionsDataHandler loaded.");

            if (config != null)
                m_config = config;

            m_scenes = scenes;

            if(scenes.Count >= 1)
                m_userManager = scenes[0].RequestModuleInterface<IUserManagement>();
        }

        protected override byte[] ProcessRequest(string path, Stream requestData, IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
        {
            Dictionary<string, object> request = new Dictionary<string, object>();
            foreach (string name in httpRequest.QueryString)
                request[name] = httpRequest.QueryString[name];

            httpResponse.ContentType = "application/json";

            mainDataSet _dataSet = new mainDataSet();

            foreach(Scene _scene in m_scenes)
            {
                try
                {
                    if (_scene != null)
                    {
                        regionDataSet _regionData = new regionDataSet();

                        _regionData.RegionName = ToUTF8(_scene.Name);
                        _regionData.RegionEstate = ToUTF8(_scene.RegionInfo.EstateSettings.EstateName);
                        _regionData.RegionImageUUID = _scene.RegionInfo.lastMapUUID.ToString();
                        _regionData.RegionPosition = _scene.RegionInfo.WorldLocX + "/" + _scene.RegionInfo.WorldLocY;
                        _regionData.RegionPublicAccess = _scene.RegionInfo.EstateSettings.PublicAccess;
                        _regionData.RegionSize = _scene.RegionInfo.RegionSizeX.ToString();
                        _regionData.RegionUUID = _scene.RegionInfo.RegionID.ToString();
                        _regionData.RegionIsVisibleInSearch = true;

                        if (m_config.Configs["Hypergrid"] != null)
                            _regionData.RegionHomeURI = m_config.Configs["Hypergrid"].GetString("HomeURI", string.Empty);

                        _dataSet.RegionData.Add(_regionData);

                        List<ILandObject> _landData = _scene.LandChannel.AllParcels();
                        foreach (ILandObject _parcel in _landData)
                        {
                            try
                            {
                                parcelDataSet _parcelSet = new parcelDataSet();

                                if (_parcel.LandData != null)
                                {
                                    _parcelSet.ParcelName = ToUTF8(_parcel.LandData.Name);
                                    _parcelSet.ParcelDescription = ToUTF8(_parcel.LandData.Description);
                                    _parcelSet.ImageUUID = _parcel.LandData.SnapshotID.ToString();
                                    _parcelSet.ParcelDwell = (int)_parcel.LandData.Dwell;
                                    _parcelSet.ParcelGroup = _parcel.LandData.GroupID.ToString();
                                    _parcelSet.ParcelOwner.OwnerUUID = _parcel.LandData.OwnerID.ToString();

                                    if (_parcelSet.ParcelOwner.OwnerUUID != _parcelSet.ParcelGroup)
                                    {
                                        if (m_userManager != null)
                                        {
                                            _parcelSet.ParcelOwner.OwnerName = m_userManager.GetUserName(_parcel.LandData.OwnerID);
                                            _parcelSet.ParcelOwner.OwnerHomeURI = m_userManager.GetUserHomeURL(_parcel.LandData.OwnerID);

                                            if (_parcelSet.ParcelOwner.OwnerHomeURI == String.Empty)
                                                _parcelSet.ParcelOwner.OwnerHomeURI = _regionData.RegionHomeURI;
                                        }
                                    }
                                    else
                                    {
                                        if (_parcelSet.ParcelOwner.OwnerUUID == _parcelSet.ParcelGroup)
                                        {
                                            IGroupsModule groups = _scene.RequestModuleInterface<IGroupsModule>();

                                            if (groups != null)
                                            {
                                                GroupRecord _group = groups.GetGroupRecord(_parcel.LandData.GroupID);

                                                if (_group != null)
                                                {
                                                    _parcelSet.ParcelOwner.OwnerName = _group.GroupName;
                                                    _parcelSet.ParcelOwner.OwnerHomeURI = _regionData.RegionHomeURI;

                                                    if (_dataSet.GroupData.Find(x => x.GroupUUID == _group.GroupID.ToString()) == null)
                                                    {
                                                        GroupDataSet _groupData = new GroupDataSet();

                                                        _groupData.GroupName = _group.GroupName;
                                                        _groupData.GroupHomeURI = _regionData.RegionHomeURI;
                                                        _groupData.GroupUUID = _group.GroupID.ToString();
                                                        _groupData.GroupImage = _group.GroupPicture.ToString();
                                                        _groupData.GroupShowInList = _group.ShowInList;
                                                        _groupData.GroupAllowPublish = _group.AllowPublish;
                                                        _groupData.GroupFounder = _group.FounderID.ToString();

                                                        _dataSet.GroupData.Add(_groupData);
                                                    }
                                                }
                                            }
                                        }
                                    }

                                    _parcelSet.ParcelPosition = _parcel.CenterPoint.X + "/" + _parcel.CenterPoint.Y;

                                    if (_parcel.LandData.LandingType == (byte)LandingType.LandingPoint)
                                        if (_parcel.LandData.UserLocation.X != 0 && _parcel.LandData.UserLocation.X != 0 && _parcel.LandData.UserLocation.X != 0)
                                            _parcelSet.ParcelPosition = _parcel.LandData.UserLocation.X + "/" + _parcel.LandData.UserLocation.Y + "/" + _parcel.LandData.UserLocation.Z;

                                    _parcelSet.ParcelPrims = _parcel.GetParcelMaxPrimCount();

                                    _parcelSet.ParcelSize = _parcel.LandData.Area;
                                    _parcelSet.ParcelBitmap = Convert.ToBase64String(_parcel.LandData.Bitmap);
                                    _parcelSet.ParcelPrice = _parcel.LandData.SalePrice;
                                    _parcelSet.ParcelIsVisibleInSearch = getStatusForSearch(_parcel);
                                    _parcelSet.ParcelIsForSale = getStatusForSale(_parcel);
                                    _parcelSet.ParentUUID = _scene.RegionInfo.RegionID.ToString();
                                    _parcelSet.ParcelUUID = _parcel.LandData.GlobalID.ToString();

                                    _dataSet.ParcelData.Add(_parcelSet);
                                }
                            }
                            catch
                            {
                                m_log.Error("Error while fetching land data.");
                            }
                        }

                        foreach (SceneObjectGroup _cog in _scene.GetSceneObjectGroups())
                        {
                            try
                            {
                                if (_cog != null)
                                {
                                    if (_cog.IsTemporary == false && _cog.IsAttachment == false)
                                    {
                                        objectDataSet _objectData = new objectDataSet();

                                        _objectData.ObjectName = ToUTF8(_cog.Name);
                                        _objectData.ObjectDescription = ToUTF8(_cog.Description);
                                        _objectData.ObjectUUID = _cog.RootPart.UUID.ToString();
                                        _objectData.ParentUUID = _scene.LandChannel.GetLandObject(_cog.RootPart.AbsolutePosition.X, _cog.RootPart.AbsolutePosition.Y).LandData.GlobalID.ToString();
                                        _objectData.ObjectIsForSale = false;
                                        _objectData.ObjectSalePrice = _cog.RootPart.SalePrice;

                                        if (_cog.RootPart.ObjectSaleType != (byte)0)
                                            _objectData.ObjectIsForSale = true;

                                        _objectData.ObjectIsForCopy = getStatusForCopy(_cog);
                                        _objectData.ObjectGroupUUID = _cog.GroupID.ToString();
                                        _objectData.ObjectItemUUID = _cog.FromItemID.ToString();
                                        _objectData.ObjectOwner.OwnerUUID = _cog.OwnerID.ToString();

                                        _objectData.Textures = getAllTextures(_cog);

                                        foreach (SceneObjectPart part in _cog.Parts)
                                        {
                                            foreach (TaskInventoryItem item in part.Inventory.GetInventoryItems())
                                            {
                                                inventoryDataSet data = new inventoryDataSet();
                                                data.AssetID = item.AssetID.ToString();
                                                data.ItemID = item.ItemID.ToString();
                                                data.Name = item.Name;
                                                data.Description = item.Description;
                                                data.InventoryType = item.InvType;
                                                _objectData.Inventory.Add(data);
                                            }
                                        }

                                        if (_objectData.ObjectOwner.OwnerUUID != _objectData.ObjectGroupUUID)
                                        {
                                            if (m_userManager != null)
                                            {
                                                _objectData.ObjectOwner.OwnerName = m_userManager.GetUserName(_cog.OwnerID);
                                                _objectData.ObjectOwner.OwnerHomeURI = m_userManager.GetUserHomeURL(_cog.OwnerID);

                                                if (_objectData.ObjectOwner.OwnerHomeURI == String.Empty)
                                                    _objectData.ObjectOwner.OwnerHomeURI = _regionData.RegionHomeURI;
                                            }
                                        }
                                        else
                                        {
                                            if (_objectData.ObjectOwner.OwnerUUID == _objectData.ObjectGroupUUID)
                                            {
                                                IGroupsModule groups = _scene.RequestModuleInterface<IGroupsModule>();

                                                if (groups != null)
                                                {
                                                    GroupRecord _group = groups.GetGroupRecord(_cog.RootPart.GroupID);

                                                    if (_group != null)
                                                    {
                                                        _objectData.ObjectOwner.OwnerName = _group.GroupName;
                                                        _objectData.ObjectOwner.OwnerHomeURI = _regionData.RegionHomeURI;

                                                        if (_dataSet.GroupData.Find(x => x.GroupUUID == _group.GroupID.ToString()) == null)
                                                        {
                                                            GroupDataSet _groupData = new GroupDataSet();

                                                            _groupData.GroupName = _group.GroupName;
                                                            _groupData.GroupHomeURI = _regionData.RegionHomeURI;
                                                            _groupData.GroupUUID = _group.GroupID.ToString();
                                                            _groupData.GroupImage = _group.GroupPicture.ToString();
                                                            _groupData.GroupShowInList = _group.ShowInList;
                                                            _groupData.GroupAllowPublish = _group.AllowPublish;
                                                            _groupData.GroupFounder = _group.FounderID.ToString();

                                                            _dataSet.GroupData.Add(_groupData);
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        _objectData.ObjectPosition = _cog.RootPart.AbsolutePosition.X + "/" + _cog.RootPart.AbsolutePosition.Y + "/" + _cog.RootPart.AbsolutePosition.Z;
                                        _objectData.ObjectImageUUID = GuessImage(_cog);
                                        _objectData.ObjectIsVisibleInSearch = getStatusForSearch(_cog);

                                        _dataSet.ObjectData.Add(_objectData);
                                    }
                                }
                            }
                            catch
                            {
                                m_log.Error("Error while fetching scene object groups.");
                            }
                        }

                        foreach (ScenePresence _presence in _scene.GetScenePresences())
                        {
                            try
                            {
                                if (_presence.PresenceType == PresenceType.User)
                                {
                                    agentDataSet _agentData = new agentDataSet();
                                    _agentData.AgentName = _presence.Name;

                                    if (_presence.AbsolutePosition != null)
                                        _agentData.AgentPosition = _presence.AbsolutePosition.X + "/" + _presence.AbsolutePosition.Y + "/" + _presence.AbsolutePosition.Z;

                                    _agentData.AgentUUID = _presence.UUID.ToString();
                                    _agentData.AgentHomeURI = _scene.GetAgentHomeURI(_presence.UUID);

                                    if (_scene.LandChannel != null && _presence.AbsolutePosition != null)
                                        _agentData.ParentUUID = _scene.LandChannel.GetLandObject(_presence.AbsolutePosition.X, _presence.AbsolutePosition.Y).LandData.GlobalID.ToString();

                                    _dataSet.AvatarData.Add(_agentData);
                                }
                            }
                            catch
                            {
                                m_log.Error("Error while fetching presence data.");
                            }

                        }
                    }
                }catch
                {
                    m_log.Error("Error while fetching scene data.");
                }
            }
            
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_dataSet, Formatting.Indented));
        }

        private bool getStatusForCopy(SceneObjectGroup prim)
        {
            if (prim == null)
                return false;

            if (prim.RootPart == null)
                return false;

            if (((uint)prim.RootPart.EveryoneMask & (uint)OpenMetaverse.PermissionMask.Copy) != 0)
                return true;

            return false;
        }

        private bool getStatusForSearch(SceneObjectGroup prim)
        {
            if (prim == null)
                return false;

            if (prim.RootPart == null)
                return false;

            if ((prim.RootPart.Flags & PrimFlags.JointWheel) == PrimFlags.JointWheel)
                return true;

            return false;
        }

        private bool getStatusForSale(ILandObject parcel)
        {
            if (parcel == null)
                return false;

            if (parcel.LandData == null)
                return false;

            if ((parcel.LandData.Flags & (uint)ParcelFlags.ForSale) == (uint)ParcelFlags.ForSale)
                return true;

            return false;
        }

        private bool getStatusForSearch(ILandObject parcel)
        {
            if (parcel == null)
                return false;

            if (parcel.LandData == null)
                return false;

            if ((parcel.LandData.Flags & (uint)ParcelFlags.ShowDirectory) == (uint)ParcelFlags.ShowDirectory)
                return true;

            return false;
        }

        private List<String> getAllTextures(SceneObjectGroup sog)
        {
            if (sog == null)
                return new List<String>();

            List<String> texturList = new List<String>();

            PrimitiveBaseShape shape = sog.RootPart.Shape;
            if (shape != null && shape.ProfileShape == ProfileShape.Square)
            {
                Primitive.TextureEntry textures = shape.Textures;
                if (textures != null)
                {
                    if (textures.FaceTextures != null)
                    {
                        foreach (Primitive.TextureEntryFace tentry in textures.FaceTextures)
                        {
                            if (tentry != null)
                            {
                                if (tentry.TextureID != UUID.Zero && tentry.TextureID != UUID.Zero && tentry.TextureID != UUID.Zero)
                                {
                                    if (!texturList.Contains(tentry.TextureID.ToString()))
                                        texturList.Add(tentry.TextureID.ToString());
                                }
                            }
                        }
                    }
                }
            }

            return texturList;
        }

        private string GuessImage(SceneObjectGroup sog)
        {
            if (sog == null)
                return UUID.Zero.ToString();

            string bestguess = string.Empty;
            Dictionary<UUID, int> counts = new Dictionary<UUID, int>();

            PrimitiveBaseShape shape = sog.RootPart.Shape;
            if (shape != null && shape.ProfileShape == ProfileShape.Square)
            {
                Primitive.TextureEntry textures = shape.Textures;
                if (textures != null)
                {
                    if (textures.DefaultTexture != null &&
                        textures.DefaultTexture.TextureID != UUID.Zero &&
                        textures.DefaultTexture.RGBA.A < 50f)
                    {
                        counts[textures.DefaultTexture.TextureID] = 8;
                    }

                    if (textures.FaceTextures != null)
                    {
                        foreach (Primitive.TextureEntryFace tentry in textures.FaceTextures)
                        {
                            if (tentry != null)
                            {
                                if (tentry.TextureID != UUID.Zero && tentry.TextureID != UUID.Zero && tentry.TextureID != UUID.Zero && tentry.RGBA.A < 50)
                                {
                                    int c = 0;
                                    counts.TryGetValue(tentry.TextureID, out c);
                                    counts[tentry.TextureID] = c + 1;
                                    // decrease the default texture count
                                    if (counts.ContainsKey(textures.DefaultTexture.TextureID))
                                        counts[textures.DefaultTexture.TextureID] = counts[textures.DefaultTexture.TextureID] - 1;
                                }
                            }
                        }
                    }

                    // Let's pick the most unique texture
                    int min = 9999;
                    foreach (KeyValuePair<UUID, int> kv in counts)
                    {
                        if (kv.Value < min && kv.Value >= 1)
                        {
                            bestguess = kv.Key.ToString();
                            min = kv.Value;
                        }
                    }
                }
            }

            if (bestguess.Trim() == String.Empty)
                bestguess = UUID.Zero.ToString();

            return bestguess;
        }

        private String ToUTF8(String _text)
        {
            try
            {
                byte[] UTF8Bytes = Encoding.UTF8.GetBytes(_text);
                return Encoding.UTF8.GetString(UTF8Bytes, 0, UTF8Bytes.Length);
            }
            catch
            {
                return _text;
            }
        }
    }
}
