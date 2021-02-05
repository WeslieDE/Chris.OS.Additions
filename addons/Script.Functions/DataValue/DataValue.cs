using Chris.OS.Additions.Script.Functions.DataValue.Storage;
using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Timers;

namespace Chris.OS.Additions.Script.Functions.DataValue
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "ScriptDataStorage")]

    public class ScriptDataStorage : INonSharedRegionModule
    {
        #region Region Module
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene = null;
        private IConfig m_config = null;
        private IScriptModuleComms m_scriptModule;

        private String m_storageTyp = null;
        private iStorage m_storage = null;

        private List<StorageElement> m_cache = new List<StorageElement>();

        private Timer m_timer = null;
        private int m_rateLimit = 0;

        public string Name
        {
            get { return "ScriptDataStorage"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void AddRegion(Scene scene)
        {

        }

        public void Close()
        {

        }

        public void Initialise(IConfigSource source)
        {
            try
            {
                if(source.Configs["ScriptDataStorage"] != null)
                {
                    m_config = source.Configs["ScriptDataStorage"];
                    m_storageTyp = m_config.GetString("DataStorageTyp", "Memory").ToUpper().Trim();
                }
            }
            catch (Exception e)
            {
                m_log.ErrorFormat("[" + Name + "]: initialization error: {0}", e.Message);
                return;
            }

            m_timer = new Timer();
            m_timer.Interval = 1000;
            m_timer.Elapsed += cleanUp;
            m_timer.Start();
        }

        public void RegionLoaded(Scene scene)
        {
            m_log.Info("[" + Name + "]: Load region " + scene.Name);

            m_scene = scene;

            if (m_storageTyp == "REGIONEXTRAS")
                m_storage = new RegionExtras(m_scene, m_config);

            if (m_storageTyp == "FILESYSTEM")
                m_storage = new FileSystem(m_scene, m_config);

            if (m_storageTyp == "MYSQL")
                m_storage = new MySQL(m_scene, m_config);

            if (m_storageTyp == "MEMORY")
                m_storage = new Memory();

            if (m_storage == null)
                m_storage = new RegionExtras(m_scene, m_config);

            m_log.Info("[" + Name + "] Using '" + m_storageTyp + "' as Storage.");

            m_scriptModule = m_scene.RequestModuleInterface<IScriptModuleComms>();
            if (m_scriptModule != null)
            {
                m_scriptModule.RegisterScriptInvocation(this, "osGetDataValue");
                m_scriptModule.RegisterScriptInvocation(this, "osSetDataValue");
                m_scriptModule.RegisterScriptInvocation(this, "osDeleteDataValue");
                m_scriptModule.RegisterScriptInvocation(this, "osCheckDataValue");
            }
        }

        public void RemoveRegion(Scene scene)
        {
            
        }

        #endregion

        #region Script Funktions

        [ScriptInvocation]
        public string osGetDataValue(UUID hostID, UUID scriptID, string key)
        {
            if(m_storage != null)
            {
                SceneObjectPart _host = m_scene.GetSceneObjectPart(hostID);
                StorageElement _element = m_cache.Find(X => X.Group == _host.GroupID.ToString() && X.Index == key);

                if (_element != null)
                    return _element.get();

                checkRateLimit();

                try
                {
                    String _data = m_storage.get(_host.GroupID.ToString(), key);

                    if (_data == null)
                        return "";

                    m_cache.Add(new StorageElement(_host.GroupID.ToString(), key, _data, m_storage));

                    return _data;
                }catch(Exception _error)
                {
                    m_log.Error("[" + Name + "] osGetDataValue: " + _error.Message);
                    return "";
                }
            }

            throw new Exception("No data Storage aviable.");
        }

        [ScriptInvocation]
        public void osSetDataValue(UUID hostID, UUID scriptID, string key, string value)
        {
            if (m_storage != null)
            {
                SceneObjectPart _host = m_scene.GetSceneObjectPart(hostID);
                StorageElement _element = m_cache.Find(X => X.Group == _host.GroupID.ToString() && X.Index == key);

                if (_element != null)
                {
                    _element.save(value);
                    return;
                }

                checkRateLimit();

                try
                {
                    m_cache.Add(new StorageElement(_host.GroupID.ToString(), key, value, m_storage));
                    m_storage.save(_host.GroupID.ToString(), key, value);
                    return;
                }
                catch (Exception _error)
                {
                    m_log.Error("[" + Name + "] osSetDataValue: " + _error.Message);
                }
            }

            throw new Exception("No data Storage aviable.");
        }
        [ScriptInvocation]
        public void osDeleteDataValue(UUID hostID, UUID scriptID, string key, string value)
        {
            SceneObjectPart _host = m_scene.GetSceneObjectPart(hostID);
            StorageElement _element = m_cache.Find(X => X.Group == _host.GroupID.ToString() && X.Index == key);

            checkRateLimit();

            if (m_storage != null)
            {
                try
                {
                    if (_element != null)
                        m_cache.Remove(_element);

                    m_storage.remove(_host.GroupID.ToString(), key);
                }
                catch (Exception _error)
                {
                    m_log.Error("[" + Name + "] osDeleteDataValue: " + _error.Message);
                }

            }

            throw new Exception("No data Storage aviable.");
        }

        [ScriptInvocation]
        public int osCheckDataValue(UUID hostID, UUID scriptID, string key)
        {
            if (m_storage != null)
            {
                SceneObjectPart _host = m_scene.GetSceneObjectPart(hostID);
                StorageElement _element = m_cache.Find(X => X.Group == _host.GroupID.ToString() && X.Index == key);

                if (_element != null)
                    return 1;

                checkRateLimit();

                try
                {
                    if (m_storage.check(_host.GroupID.ToString(), key))
                        return 1;

                    return 0;
                }
                catch (Exception _error)
                {
                    m_log.Error("[" + Name + "] osCheckDataValue: " + _error.Message);
                    return 0;
                }
            }

            throw new Exception("No data Storage aviable.");
        }

        private void cleanUp(object sender, ElapsedEventArgs e)
        {
            if(m_rateLimit >= 0)
                m_rateLimit = m_rateLimit - 100;

            List<StorageElement> _allStorageElements = m_cache.FindAll(X => X.LastUse + 10 <= (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);

            foreach(StorageElement _element in _allStorageElements)
                m_cache.Remove(_element);
        }

        private void checkRateLimit()
        {
            m_rateLimit++;

            if (m_rateLimit >= 1000)
                throw new Exception("Data storage rate limit reached!");
        }

        #endregion

    }
}
