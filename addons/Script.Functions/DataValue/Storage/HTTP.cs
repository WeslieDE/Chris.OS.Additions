using Nini.Config;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Script.Functions.DataValue.Storage
{
    class HTTP : iStorage
    {
        private Scene m_scene = null;
        private WebClient m_client = null;

        public HTTP(Scene scene, IConfig config)
        {
            m_scene = scene;
            m_client = new WebClient();
        }

        public bool check(string storageID, string key)
        {
            throw new NotImplementedException();
        }

        public string get(string storageID, string key)
        {
            throw new NotImplementedException();
        }

        public void remove(string storageID, string key)
        {
            throw new NotImplementedException();
        }

        public void save(string storageID, string key, string data)
        {
            throw new NotImplementedException();
        }
    }
}
