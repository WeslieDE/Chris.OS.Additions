using Chris.OS.Additions.Shared.Data.DisplayName;
using Chris.OS.Additions.Utils;
using Mono.Addins;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Scenes;
using System;

namespace Chris.OS.Additions.Region.Modules.DisplayNames
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "DisplayNameManager")]

    public class DisplayNamesModule : EmptyModule, IDisplayNameManagement
    {
        private IUserManagement UserManagement = null;
        private DisplayNameServiceConnector ServiceConnector = null;

        #region EmptyModule
        public override Type ReplaceableInterface
        {
            get { return typeof(IDisplayNameManagement); }
        }

        public override void AddRegion(Scene lScene)
        {
            base.World = lScene;
            UserManagement = base.World.RequestModuleInterface<IUserManagement>();

            if (UserManagement == null)
                base.Logger.Error("["+ Name + "] Cant get User Management.");

            ServiceConnector = new DisplayNameServiceConnector();
        }
        #endregion

        #region IDisplayNameManagement
        public String getDisplayName(UUID lUser)
        {
            if (UserManagement == null)
                return null;

            Uri lDisplayNameServerURI = getUserDisplayNameServerURI(lUser);

            if(lDisplayNameServerURI != null)
                return ServiceConnector.getDisplayName(lDisplayNameServerURI, lUser);

            return null;
        }

        public Boolean setDisplayName(UUID lUser, string lNewName)
        {
            if (UserManagement == null)
                return false;

            Uri lDisplayNameServerURI = getUserDisplayNameServerURI(lUser);

            if (lDisplayNameServerURI != null)
                return ServiceConnector.setDisplayName(lDisplayNameServerURI, lNewName);

            return false;
        }
        #endregion

        #region Functions
        private Uri getUserDisplayNameServerURI(UUID lUser)
        {
            Object lHomeURL = String.Empty;

            if (UserManagement.GetUserData(lUser).ServerURLs.TryGetValue("DisplayNameServerURI", out lHomeURL))
            {
                String lMoneyServer = (String)lHomeURL;

                if (lMoneyServer != null)
                {
                    try
                    {
                        return new Uri(lMoneyServer);
                    }
                    catch
                    {
                        base.Logger.Error("[" + Name + "] Malformed URL for DisplayNameService for User '" + lUser.ToString() + "'");
                    }
                }
            }
            else
            {
                base.Logger.Warn("[" + Name + "] Cant get DisplayName Server for User '" + lUser.ToString() + "'");
                return null;
            }

            return null;
        }
        #endregion

    }
}
