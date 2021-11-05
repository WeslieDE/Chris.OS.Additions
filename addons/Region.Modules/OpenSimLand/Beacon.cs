using Chris.OS.Additions.Utils;
using Mono.Addins;
using Newtonsoft.Json;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using System;
using System.Net;
using System.Timers;

namespace Chris.OS.Additions.Region.Modules.OpenSimLand
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "OpenSimLand")]

    class Beacon : EmptyModule
    {
        private Timer m_timer = null;
        private string m_hostname = null;
        private int m_port = 0;

        private bool m_enable = true;

        private string m_serviceURL = "https://api.opensim.land/?api=register";

        #region EmptyModule
        public override string Name
        {
            get { return "OpenSimLand"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            m_timer = new Timer();
            m_timer.Interval = (5 * 60 * 1000); //1 Minute
            m_timer.Elapsed += send_ping;
            m_timer.AutoReset = true;
            m_timer.Start();

            base.World.EventManager.OnRegionLoginsStatusChange += onLoginChange;

            base.Logger.Error("[OpenSimLand] Beacon start.");

            m_hostname = base.Config.Configs["Network"].GetString("ExternalHostNameForLSL", null);
            m_port = base.Config.Configs["Network"].GetInt("http_listener_port", 0);
        }

        #endregion
        private void onLoginChange(IScene scene)
        {
            send_ping(null, null);
        }

        private void send_ping(object sender, ElapsedEventArgs e)
        {
            base.Logger.Error("[OpenSimLand] Sending Ping to webservice.");

            try
            {
                if(!World.RegionInfo.EstateSettings.PublicAccess)
                    return;

                UserAccount account = World.UserAccountService.GetUserAccount(World.RegionInfo.ScopeID, World.RegionInfo.EstateSettings.EstateOwner);

                PingData ping = new PingData();
                ping.RegionName = base.World.RegionInfo.RegionName;
                ping.UUID = base.World.RegionInfo.RegionID.ToString();

                ping.RegionOwnerID = World.RegionInfo.EstateSettings.EstateOwner.ToString();
                ping.RegionOwnerName = account.FirstName + " " + account.LastName;
                ping.RegionOwnerMail = account.Email;
                ping.RegionOwnerURL = account.ServiceURLs;

                ping.Hostname = m_hostname;
                ping.Port = m_port;

                ping.GridName = World.SceneGridInfo.GridName;
                ping.HomeURL = World.SceneGridInfo.HomeURLNoEndSlash;

                WebClient client = new WebClient();

                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                ServicePointManager.CertificatePolicy = new NoCheckCertificatePolicy();

                String jsonData = JsonConvert.SerializeObject(ping);
                String result = client.UploadString(m_serviceURL, jsonData);
            }
            catch(Exception error)
            {
                base.Logger.Error("[OpenSimLand] Error while sending ping!" + error.Message);
            }
        }
    }
}
