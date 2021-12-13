using Chris.OS.Additions.Utils;
using Mono.Addins;
using NLua;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Script.Functions.osLua
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "osLua")]

    class osLua : EmptyModule
    {
        private Lua m_state = null;

        private Thread m_runtime = null;
        private List<LuaStartParameters> m_startParameters = new List<LuaStartParameters>();

        #region EmptyModule
        public override string Name
        {
            get { return "osLua"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            m_state = new Lua();
            m_state.LoadCLRPackage();

            try
            {
                if (!Directory.Exists("Scripts"))
                    Directory.CreateDirectory("Scripts");

                foreach (String file in Directory.GetFiles("Scripts"))
                {
                    if(file.EndsWith(".lua"))
                    {
                        base.Logger.Info("[" + Name + "]: Run Script '" + file + "'");

                        String script = File.ReadAllText(file);
                        startLuaScript(base.World, null, script);
                    }
                }

                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();
                m_scriptModule.RegisterScriptInvocation(this, "osRunLua");
            }
            catch (Exception e)
            {
                base.Logger.Error("[" + Name + "]: Initialisierungs error:" + e.Message);
            }

            try
            {
                m_runtime = new Thread(() => runLua());
                m_runtime.Start();
            }
            catch (Exception e)
            {
                base.Logger.Error("[" + Name + "]: Initialisierungs error:" + e.Message);
            }
        }
        #endregion

        #region functions
        private void startLuaScript(Scene world, SceneObjectPart part, String lua)
        {
            lock(m_startParameters)
            {
                LuaStartParameters parameters = new LuaStartParameters() { Part = part, LuaScript = lua};
                m_startParameters.Add(parameters);
            }
        }

        private void runLua()
        {
            base.Logger.Error("[" + Name + "]: Start Lua Runtime Thread.");

            m_state["World"] = base.World;
            m_state["Logger"] = base.Logger;
            m_state["Config"] = base.Config;
            m_state["Events"] = base.World.EventManager;

            while (true)
            {
                lock (m_startParameters)
                {
                    if (m_startParameters.Count == 0)
                        Thread.Sleep(100);

                    LuaStartParameters first = m_startParameters.First();
                    if(first != null)
                    {
                        m_startParameters.Remove(first);
                        m_state["Object"] = first.Part;

                        try
                        {
                            m_state.DoString(first.LuaScript);
                        }
                        catch (Exception error)
                        {
                            base.Logger.Error(error.Message);
                            throw new Exception("lua runtime error");
                        }
                    }

                }

                Thread.Sleep(10);
            }
        }
        #endregion

        #region Script functions
        [ScriptInvocation]
        public void osRunLua(UUID hostID, UUID scriptID, String lua)
        {
            SceneObjectPart part = base.World.GetSceneObjectPart(hostID);
            if (World.RegionInfo.EstateSettings.IsEstateManagerOrOwner(part.OwnerID) || World.RegionInfo.EstateSettings.EstateOwner == part.OwnerID)
            {
                try
                {
                    startLuaScript(base.World, part, lua);

                    if (m_startParameters.Count >= 1000)
                    {
                        base.Logger.Info("[" + Name + "]: WARNING: Lua queue is over 1000!");
                        Thread.Sleep(250);
                    }
                }
                catch (Exception error)
                {
                    base.Logger.Error(error.Message);
                    throw new Exception("lua runtime error");
                }
            }
            else
            {
                throw new Exception("lua runtime error: no permission");
            }
        }
        #endregion
    }
}
