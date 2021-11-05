using Newtonsoft.Json;
using System;

namespace Chris.OS.Additions.Region.Modules.DataPublisher.Data
{
    class inventoryDataSet
    {
        [JsonProperty(PropertyName = "ItemID")]
        public String ItemID = null;

        [JsonProperty(PropertyName = "AssetUUID")]
        public String AssetID = null;

        [JsonProperty(PropertyName = "Name")]
        public String Name = null;

        [JsonProperty(PropertyName = "Description")]
        public String Description = null;

        [JsonProperty(PropertyName = "Type")]
        public int InventoryType = -1;
    }
}
