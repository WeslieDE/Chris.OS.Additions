using System;
using System.Collections.Generic;

namespace Chris.OS.Additions.Script.Functions.ObjectFinder
{
    class CacheData
    {
        public String searchString = "";
        public List<object> results = new List<object>();

        public CacheData(String search)
        {
            searchString = search;
        }

        public CacheData(String search, List<object> datas)
        {
            searchString = search;
            results = datas;
        }

        public CacheData(String search, List<object> datas, float dis)
        {
            searchString = search;
            results = datas;
        }
    }
}
