using log4net;
using Nini.Config;
using OpenSim.Region.Framework.Scenes;
using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace Chris.OS.Additions.Script.Functions.DataValue.Storage
{
    class FileSystem : iStorage
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Scene m_scene = null;

        private string m_dataValueDirectory = "./ScriptDataValue";

        public FileSystem(Scene scene, IConfig config)
        {
            m_scene = scene;

            m_dataValueDirectory = config.GetString("DataValueStorageDirectory", m_dataValueDirectory);
        }

        public bool check(String storageID, string key)
        {
            string _filePath = getFilePath(storageID, key);

            FileInfo file = new FileInfo(_filePath);

            if (file.Exists)
                return true;

            return false;
        }

        public String get(String storageID, string key)
        {
            FileInfo file = new FileInfo(getFilePath(storageID, key));

            if (file.Exists)
                return File.ReadAllText(file.FullName);

            return null;
        }

        public void remove(string storageID, string key)
        {
            FileInfo file = new FileInfo(getFilePath(storageID, key));

            if (file.Exists)
                file.Delete();
        }

        public void save(String storageID, string key, string data)
        {
            FileInfo file = new FileInfo(getFilePath(storageID, key));
            File.WriteAllText(file.FullName, data);
        }

        private string getFilePath(String host, String index)
        {
            string _nameSpace = host.Trim().ToUpper().Replace("-", "");

            if (!Directory.Exists(m_dataValueDirectory))
                Directory.CreateDirectory(m_dataValueDirectory);

            if (!Directory.Exists(m_dataValueDirectory + "/" + _nameSpace))
                Directory.CreateDirectory(m_dataValueDirectory + "/" + _nameSpace);

            string _storageKey = BitConverter.ToString(new MD5CryptoServiceProvider().ComputeHash(ASCIIEncoding.ASCII.GetBytes(index.Trim().ToUpper()))).Replace("-", "");

            return m_dataValueDirectory + "/" + _nameSpace + "/" + _storageKey + ".txt";
        }
    }
}
