using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenSim.Modules.DataValue.Storage
{
    interface iStorage
    {
        void save(String storageID, String key, String data);
        String get(String storageID, String key);
        void remove(String storageID, String key);
        bool check(String storageID, String key);
    }
}
