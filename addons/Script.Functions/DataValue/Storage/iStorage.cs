using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Script.Functions.DataValue.Storage
{
    interface iStorage
    {
        void save(String storageID, String key, String data);
        String get(String storageID, String key);
        void remove(String storageID, String key);
        bool check(String storageID, String key);
        List<String> allIDs(String storageID);
    }
}
