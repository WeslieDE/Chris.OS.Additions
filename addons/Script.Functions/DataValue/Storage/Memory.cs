using System;
using System.Collections.Generic;

namespace Chris.OS.Additions.Script.Functions.DataValue.Storage
{
    class Memory : iStorage
    {
        List<MemoryStorageObjekt> m_storage = new List<MemoryStorageObjekt>();

        public bool check(string storageID, string key)
        {
            MemoryStorageObjekt _storageObjekt = m_storage.Find(X => X.StorageID == storageID && X.StorageKey == key);

            if (_storageObjekt != null)
                return true;

            return false;
        }

        public string get(string storageID, string key)
        {
            MemoryStorageObjekt _storageObjekt = m_storage.Find(X => X.StorageID == storageID && X.StorageKey == key);

            if (_storageObjekt != null)
                return _storageObjekt.StorageData;

            return null;
        }

        public void remove(string storageID, string key)
        {
            MemoryStorageObjekt _storageObjekt = m_storage.Find(X => X.StorageID == storageID && X.StorageKey == key);

            if (_storageObjekt != null)
                m_storage.Remove(_storageObjekt);
        }

        public void save(string storageID, string key, string data)
        {
            MemoryStorageObjekt _storageObjekt = m_storage.Find(X => X.StorageID == storageID && X.StorageKey == key);

            if (_storageObjekt != null)
                _storageObjekt.StorageData = data;

            m_storage.Add(new MemoryStorageObjekt(storageID, key, data));
        }

        public List<String> allIDs(string storageID)
        {
            List<String> allStorageID = new List<String>();

            foreach(MemoryStorageObjekt storage in m_storage)
            {
                if(storage.StorageKey.Equals(storageID))
                {
                    string newListKey = storage.StorageKey;
                    
                    if(newListKey != null)
                        allStorageID.Add(newListKey);
                }
            }

            return allStorageID;
        }
    }

    class MemoryStorageObjekt
    {
        public String StorageID = null;
        public String StorageKey = null;
        public String StorageData = null;

        public MemoryStorageObjekt(String id, String key, String data)
        {
            StorageID = id;
            StorageKey = key;
            StorageData = data;
        }
    }
}
