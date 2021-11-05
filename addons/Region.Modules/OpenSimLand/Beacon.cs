﻿using Chris.OS.Additions.Utils;
using Newtonsoft.Json;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using System.Net;
using System.Timers;

namespace Chris.OS.Additions.Region.Modules.OpenSimLand
{
    class Beacon : EmptyModule
    {
        private Timer m_timer = null;
        private string m_hostname = null;
        private int m_port = 0;

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
            m_timer.Interval = (10 * 60 * 1000); //10 Minuten
            m_timer.Elapsed += send_ping;
            m_timer.AutoReset = true;
            m_timer.Start();

            send_ping(null, null);

            m_hostname = base.Config.Configs["Network"].GetString("ExternalHostNameForLSL", null);
            m_port = base.Config.Configs["Network"].GetInt("http_listener_port", 0);
        }
        #endregion

        private void send_ping(object sender, ElapsedEventArgs e)
        {
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

                string jsonData = JsonConvert.SerializeObject(ping);
                client.UploadString(m_serviceURL, jsonData);
            }
            catch
            {
                base.Logger.Error("[OpenSimLand] Error while sending ping!");
            }
        }
    }
}
