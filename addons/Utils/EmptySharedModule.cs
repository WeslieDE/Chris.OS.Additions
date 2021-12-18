using log4net;
using Nini.Config;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Chris.OS.Additions.Utils
{
    public class EmptySharedModule : ISharedRegionModule
    {
        protected readonly ILog Logger = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected List<Scene> Worlds = new List<Scene>();

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
            this.Worlds.Add(scene);
        }

        public virtual void Close()
        {
        }

        public virtual void Initialise(IConfigSource source)
        {
            if (Config == null)
                Config = source;
        }

        public virtual void PostInitialise()
        {
        }

        public virtual void RegionLoaded(Scene scene)
        {
        }

        public virtual void RemoveRegion(Scene scene)
        {
            this.Worlds.Remove(scene);
        }
    }
}
