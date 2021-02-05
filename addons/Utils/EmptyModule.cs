using log4net;
using Nini.Config;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Reflection;

namespace Chris.OS.Additions.Utils
{
    public class EmptyModule : INonSharedRegionModule
    {
        protected readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected Scene World = null;

        protected Boolean Enabled = false;

        public virtual string Name
        {
            get { return null; }
        }

        public virtual Type ReplaceableInterface
        {
            get { return null; }
        }

        public virtual void AddRegion(Scene scene)
        {
        }

        public virtual void Close()
        {
        }

        public virtual void Initialise(IConfigSource source)
        {
        }

        public virtual void RegionLoaded(Scene scene)
        {
        }

        public virtual void RemoveRegion(Scene scene)
        {
        }
    }
}