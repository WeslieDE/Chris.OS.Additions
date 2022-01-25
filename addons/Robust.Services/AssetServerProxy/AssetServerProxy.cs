using Chris.OS.Additions.Utils;
using log4net;
using Nini.Config;
using OpenSim.Framework;
using OpenSim.Services.Base;
using OpenSim.Services.Connectors;
using OpenSim.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Chris.OS.Additions.Robust.Services.AssetServerProxy
{
    class AssetServerProxy : ServiceBase, IAssetService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private String m_assetServiceURL = null;
        private String m_backupServiceURL = null;

        private int m_errorCount = 0;
        private int m_lastError = 0;
        public Boolean EnableErrorCounter = false;

        public String AssetServiceName
        {
            get
            {
                return m_assetServiceURL;
            }
        }
        private IAssetService m_assetService = null;
        private List<AssetServerProxy> m_extraAssetServers = new List<AssetServerProxy>();
        private AssetServerProxy m_backupService = null;

        private bool checkForError()
        {
            if (EnableErrorCounter == false)
                return false;

            if ((m_lastError + 900) <= Tools.getUnixTime())
                m_errorCount = 0;

            if (m_errorCount >= 5)
                return true;

            return false;
        }

        public AssetServerProxy(IConfigSource config, string configName) : base(config)
        {
            IConfig assetConfig = config.Configs["AssetService"];
            if (assetConfig == null)
                throw new Exception("No AssetService configuration");

            m_assetServiceURL = assetConfig.GetString("AssetServerURI", String.Empty);
            m_backupServiceURL = assetConfig.GetString("BackupAssetServerURI", String.Empty);


            String[] extraServers = assetConfig.GetString("ExtraAssetServer", String.Empty).Split(new string[] { "|" }, StringSplitOptions.None);

            m_log.Info("[AssetServerProxy]: Start with AssetServerURI: " + m_assetServiceURL);

            if(m_backupServiceURL != String.Empty)
            {
                m_log.Info("[AssetServerProxy]: Add backup asset server: " + m_backupServiceURL.Trim());

                IniConfigSource fakeConfig = new IniConfigSource();
                fakeConfig.AddConfig("AssetService");
                fakeConfig.Configs["AssetService"].Set("AssetServerURI", m_backupServiceURL.Trim());

                AssetServerProxy newService = new AssetServerProxy(fakeConfig);
                m_extraAssetServers.Add(newService);
                m_backupService = newService;
            }

            if (extraServers.Length != 0)
            {
                foreach (String thisURI in extraServers)
                {
                    if (thisURI.Trim() != String.Empty)
                    {
                        m_log.Info("[AssetServerProxy]: Add extra asset server: " + thisURI.Trim());

                        IniConfigSource fakeConfig = new IniConfigSource();
                        fakeConfig.AddConfig("AssetService");
                        fakeConfig.Configs["AssetService"].Set("AssetServerURI", thisURI.Trim());

                        AssetServerProxy newService = new AssetServerProxy(fakeConfig);
                        newService.EnableErrorCounter = true;
                        m_extraAssetServers.Add(newService);
                    }
                }
            }

            m_assetService = new AssetServicesConnector(m_assetServiceURL);
        }

        public AssetServerProxy(IConfigSource config) : base(config)
        {
            IConfig assetConfig = config.Configs["AssetService"];
            if (assetConfig == null)
                throw new Exception("No AssetService configuration");

            m_assetServiceURL = assetConfig.GetString("AssetServerURI", String.Empty);
            m_backupServiceURL = assetConfig.GetString("BackupAssetServerURI", String.Empty);

            String[] extraServers = assetConfig.GetString("ExtraAssetServer", String.Empty).Split(new string[] { "|" }, StringSplitOptions.None);

            m_log.Info("[AssetServerProxy]: Start with AssetServerURI: " + m_assetServiceURL);

            if (m_backupServiceURL != String.Empty)
            {
                m_log.Info("[AssetServerProxy]: Add backup asset server: " + m_backupServiceURL.Trim());

                IniConfigSource fakeConfig = new IniConfigSource();
                fakeConfig.AddConfig("AssetService");
                fakeConfig.Configs["AssetService"].Set("AssetServerURI", m_backupServiceURL.Trim());

                AssetServerProxy newService = new AssetServerProxy(fakeConfig);
                m_extraAssetServers.Add(newService);
                m_backupService = newService;
            }

            if (extraServers.Length != 0)
            {
                foreach(String thisURI in extraServers)
                {
                    if(thisURI.Trim() != String.Empty)
                    {
                        m_log.Info("[AssetServerProxy]: Add extra asset server: " + thisURI.Trim());

                        IniConfigSource fakeConfig = new IniConfigSource();
                        fakeConfig.AddConfig("AssetService");
                        fakeConfig.Configs["AssetService"].Set("AssetServerURI", thisURI.Trim());

                        AssetServerProxy newService = new AssetServerProxy(fakeConfig);
                        newService.EnableErrorCounter = true;
                        m_extraAssetServers.Add(newService);
                    }
                }
            }

            m_assetService = new AssetServicesConnector(m_assetServiceURL);
        }

        public bool[] AssetsExist(string[] ids)
        {
            if (checkForError())
                return new bool[0];

            try
            {
                Boolean foundAllAssets = true;
                Boolean[] result = m_assetService.AssetsExist(ids);

                foreach (bool thisBool in result)
                    if (thisBool == false)
                        foundAllAssets = false;

                if (foundAllAssets == true)
                    return result;

                List<Boolean[]> results = new List<Boolean[]>();
                foreach (AssetServerProxy service in m_extraAssetServers)
                    if (service != null)
                        results.Add(service.AssetsExist(ids));

                for (int i = 0; i < ids.Length; i++)
                {
                    if (result[i] != true)
                    {
                        foreach (Boolean[] resultset in results)
                        {
                            if (resultset[i] == true)
                                result[i] = true;
                        }
                    }
                }

                return result;
            }
            catch
            {
                m_errorCount++;
                m_lastError = Tools.getUnixTime();
                m_log.Error("[AssetServerProxy]: Add remote asset server to temporary blacklist: " + m_assetServiceURL);
            }

            return null;
        }

        public bool Delete(string id)
        {
            return true;
        }

        public AssetBase Get(string id)
        {
            if (checkForError())
                return null;

            try
            {
                AssetBase result = m_assetService.Get(id);

                if (result == null || result.Data.Length == 0)
                    foreach (AssetServerProxy service in m_extraAssetServers)
                        if (result == null)
                        {
                            result = service.Get(id);

                            if (result != null && result.Data.Length != 0)
                            {
                                m_log.Info("[AssetServerProxy]: Get asset data for '" + id + "' from external asset storage at " + service.AssetServiceName);
                                Store(result);

                                if (m_backupService != null)
                                    m_backupService.Store(result);
                            }
                        }

                return result;
            }
            catch
            {
                m_errorCount++;
                m_lastError = Tools.getUnixTime();
                m_log.Error("[AssetServerProxy]: Add remote asset server to temporary blacklist: " + m_assetServiceURL);
            }

            return null;
        }

        public bool Get(string id, object sender, AssetRetrieved handler)
        {
            if (checkForError())
                return false;

            try
            {
                Boolean result = m_assetService.Get(id, sender, handler);

                if (result == false)
                    foreach (AssetServerProxy service in m_extraAssetServers)
                        if (result == false)
                        {
                            result = service.Get(id, sender, handler);

                            if (result == true)
                                m_log.Info("[AssetServerProxy]: Add asset '" + id + "' to cache from external asset storage at " + service.AssetServiceName);
                        }

                return result;
            }
            catch
            {
                m_errorCount++;
                m_lastError = Tools.getUnixTime();
                m_log.Error("[AssetServerProxy]: Add remote asset server to temporary blacklist: " + m_assetServiceURL);
            }

            return false;
        }

        public AssetBase GetCached(string id)
        {
            if (checkForError())
                return null;

            try
            {
                AssetBase result = m_assetService.GetCached(id);

                if (result == null || result.Data.Length != 0)
                    foreach (AssetServerProxy service in m_extraAssetServers)
                        if (result == null)
                        {
                            result = service.GetCached(id);

                            if (result != null && result.Data.Length != 0)
                            {
                                m_log.Info("[AssetServerProxy]: Get asset base for '" + id + "' from external asset storage at " + service.AssetServiceName);
                                Store(result);

                                if (m_backupService != null)
                                    m_backupService.Store(result);
                            }
                        }

                return result;
            }
            catch
            {
                m_errorCount++;
                m_lastError = Tools.getUnixTime();
                m_log.Error("[AssetServerProxy]: Add remote asset server to temporary blacklist: " + m_assetServiceURL);
            }

            return null;
        }

        public byte[] GetData(string id)
        {
            if (checkForError())
                return null;

            try
            {
                byte[] result = m_assetService.GetData(id);

                if (result.Length == 0)
                    foreach (AssetServerProxy service in m_extraAssetServers)
                        if (result.Length == 0)
                        {
                            result = service.GetData(id);

                            if (result.Length != 0)
                                m_log.Info("[AssetServerProxy]: Get asset data for '" + id + "' from external asset storage at " + service.AssetServiceName);
                        }

                return result;
            }
            catch
            {
                m_errorCount++;
                m_lastError = Tools.getUnixTime();
                m_log.Error("[AssetServerProxy]: Add remote asset server to temporary blacklist: " + m_assetServiceURL);
            }

            return null;
        }

        public AssetMetadata GetMetadata(string id)
        {
            if (checkForError())
                return null;

            try
            {
                AssetMetadata result = m_assetService.GetMetadata(id);

                if (result == null)
                    foreach (AssetServerProxy service in m_extraAssetServers)
                        if (result == null)
                        {
                            result = service.GetMetadata(id);

                            if (result != null)
                                m_log.Info("[AssetServerProxy]: Get asset metadata for '" + id + "' from external asset storage at " + service.AssetServiceName);
                        }


                return result;
            }
            catch
            {
                m_errorCount++;
                m_lastError = Tools.getUnixTime();
                m_log.Error("[AssetServerProxy]: Add remote asset server to temporary blacklist: " + m_assetServiceURL);
            }

            return null;
        }

        public string Store(AssetBase asset)
        {
            asset.ID = m_assetService.Store(asset);

            if (m_backupService != null)
                m_backupService.Store(asset);

            return asset.ID;
        }

        public bool UpdateContent(string id, byte[] data)
        {
            return true;
        }

        public AssetBase Get(string id, string ForeignAssetService, bool StoreOnLocalGrid)
        {
            if (checkForError())
                return null;

            try
            {
                AssetBase result = m_assetService.Get(id, ForeignAssetService, StoreOnLocalGrid);

                if (result == null)
                    foreach (AssetServerProxy service in m_extraAssetServers)
                        if (result == null)
                        {
                            result = service.Get(id, ForeignAssetService, StoreOnLocalGrid);

                            if (result != null)
                            {
                                m_log.Info("[AssetServerProxy]: Get asset base for '" + id + "' from external asset storage at " + service.AssetServiceName);
                                Store(result);

                                if (m_backupService != null)
                                    m_backupService.Store(result);
                            }
                        }

                return result;
            }
            catch
            {
                m_errorCount++;
                m_lastError = Tools.getUnixTime();
                m_log.Error("[AssetServerProxy]: Add remote asset server to temporary blacklist: " + m_assetServiceURL);
            }

            return null;
        }

        public void Get(string id, string ForeignAssetService, bool StoreOnLocalGrid, SimpleAssetRetrieved callBack)
        {
            AssetBase a = Get(id, ForeignAssetService, StoreOnLocalGrid);

            if(a != null)
            {
                Util.FireAndForget(o => callBack(a), null, "LocalAssetServiceConnector.GotFromServiceCallback");
            }
        }
    }
}
