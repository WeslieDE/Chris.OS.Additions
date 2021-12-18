using log4net;
using Nini.Config;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Reflection;

namespace Chris.OS.Additions.Utils
{
    public class EmptyNonSharedModule : INonSharedRegionModule
    {
        protected readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected Scene World = null;

        protected Boolean Enabled = false;

        protected IConfigSource Config = null;

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
            this.World = scene;
        }

        public virtual void Close()
        {
        }

        public virtual void Initialise(IConfigSource source)
        {
            if (Config == null)
                Config = source;
        }

        public virtual void RegionLoaded(Scene scene)
        {
        }

        public virtual void RemoveRegion(Scene scene)
        {
        }
    }
}