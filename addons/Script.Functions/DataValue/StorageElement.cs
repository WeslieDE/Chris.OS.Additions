using OpenSim.Modules.DataValue.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSim.Modules.DataValue
{
    class StorageElement
    {
        private String m_group = null;
        private String m_index = null;
        private String m_data = null;

        private int m_lastUseTime = 0;

        private iStorage m_storage = null;

        public StorageElement(String group, String index, String data, iStorage storage)
        {
            m_group = group;
            m_index = index;
            m_data = data;

            m_storage = storage;

            m_lastUseTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public string get()
        {
            m_lastUseTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            return m_data;
        }

        public void remove()
        {
            m_lastUseTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            m_storage.remove(m_group, m_index);
        }

        public void save(string data)
        {
            m_lastUseTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            m_data = data;
            m_storage.save(m_group, m_index, data);
        }

        public int LastUse
        {
            get
            {
                return m_lastUseTime;
            }
        }

        public String Group
        {
            get
            {
                return m_group;
            }
        }

        public String Index
        {
            get
            {
                return m_index;
            }
        }
    }
}
