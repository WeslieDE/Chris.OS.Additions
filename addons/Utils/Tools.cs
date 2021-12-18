using OpenMetaverse;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Services.Interfaces;
using System;
using System.Collections.Generic;

namespace Chris.OS.Additions.Utils
{
    public class Tools
    {
        public static int getUnixTime()
        {
            return (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
        }

        public static SceneObjectPart getGetSceneObjectPart(List<Scene> worlds, UUID objectUUID)
        {
            foreach(Scene world in worlds)
            {
                SceneObjectPart sceneObjectPart = world.GetSceneObjectPart(objectUUID);

                if (sceneObjectPart != null)
                    return sceneObjectPart;
            }

            return null;
        }

        public static IAssetService getAssetService(List<Scene> worlds)
        {
            if (worlds.Count >= 1)
                return worlds[0].AssetService;

            return null;
        }
    }
}
