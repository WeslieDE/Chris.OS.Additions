using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Script.Functions.DataValue
{
    class DataStorageEvents
    {
        #region Events
        public static Action<string, string> onSetDataValue;
        public static Action<string> onDeleteDataValue;
        #endregion
    }
}
