using log4net;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Framework.Servers;
using OpenSim.Framework.Servers.HttpServer;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace Chris.OS.Additions.Region.Modules.JPEGImageService
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "JPEGImageService")]
    class JPEGImageService : BaseStreamHandler, ISharedRegionModule 
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static List<Scene> m_scenes = new List<Scene>();

        public JPEGImageService() : base("GET", "/JPEGImageService")
        {
            m_log.Error("[JPEGImageService] INFO: JPEGImageService()");
        }

        protected override byte[] ProcessRequest(string path, Stream requestData, IOSHttpRequest httpRequest, IOSHttpResponse httpResponse)
        {
            m_log.Info("[JPEGImageService] DEBUG: Get request");

            if (m_scenes.Count == 0)
            {
                m_log.Error("[JPEGImageService] ERROR: This service are only aviable at a running region!");
                return null;
            }

            if(m_scenes[0].AssetService != null)
            {
                Dictionary<string, object> request = new Dictionary<string, object>();
                foreach (string name in httpRequest.QueryString)
                    request[name] = httpRequest.QueryString[name];

                string assetUUID = Convert.ToString(request["assetID"]);

                if(assetUUID != null)
                {
                    if (m_scenes[0].AssetService.AssetsExist(new String[] { assetUUID })[0])
                    {
                        AssetBase asset = m_scenes[0].AssetService.Get(assetUUID);

                        if(asset != null)
                        {
                            if (asset.Type == (sbyte)AssetType.Texture)
                            {
                                OpenMetaverse.Imaging.ManagedImage jpegImageData = new OpenMetaverse.Imaging.ManagedImage(256, 256, OpenMetaverse.Imaging.ManagedImage.ImageChannels.Color);

                                if (OpenMetaverse.Imaging.OpenJPEG.DecodeToImage(asset.Data, out jpegImageData))
                                {
                                    Stream imageStream = new MemoryStream(ImageToByte(jpegImageData.ExportBitmap()));
                                    Stream saveStream = new MemoryStream();

                                    System.Drawing.Image image = System.Drawing.Image.FromStream(imageStream);
                                    image.Save(saveStream, System.Drawing.Imaging.ImageFormat.Jpeg);

                                    httpResponse.ContentType = "image/jpeg";
                                    return ReadToEnd(saveStream);
                                }
                                else
                                {
                                    m_log.Info("[JPEGImageService] ERROR: Cant decode asset data!");
                                    return null;
                                }
                            }
                            else
                            {
                                m_log.Error("[JPEGImageService] ERROR: Asset is not an image!");
                                return null;
                            }
                        }
                        else
                        {
                            m_log.Error("[JPEGImageService] ERROR: Unknown error while fetch Asset '" + assetUUID + "'!");
                            return null;
                        }
                    }
                    else
                    {
                        m_log.Error("[JPEGImageService] ERROR: Asset with UUID '"+ assetUUID + "' dont exist!");
                        return null;
                    }
                }
                else
                {
                    m_log.Error("[JPEGImageService] ERROR: Request dont have an valid assetID.");
                    return null;
                }
            }
            else
            {
                m_log.Error("[JPEGImageService] ERROR: AssetService is not aviable!");
                return null;
            }
        }

        private static byte[] ImageToByte(Image img)
        {
            ImageConverter converter = new ImageConverter();
            return (byte[])converter.ConvertTo(img, typeof(byte[]));
        }

        public static byte[] ReadToEnd(System.IO.Stream stream)
        {
            long originalPosition = 0;

            if (stream.CanSeek)
            {
                originalPosition = stream.Position;
                stream.Position = 0;
            }

            try
            {
                byte[] readBuffer = new byte[4096];

                int totalBytesRead = 0;
                int bytesRead;

                while ((bytesRead = stream.Read(readBuffer, totalBytesRead, readBuffer.Length - totalBytesRead)) > 0)
                {
                    totalBytesRead += bytesRead;

                    if (totalBytesRead == readBuffer.Length)
                    {
                        int nextByte = stream.ReadByte();
                        if (nextByte != -1)
                        {
                            byte[] temp = new byte[readBuffer.Length * 2];
                            Buffer.BlockCopy(readBuffer, 0, temp, 0, readBuffer.Length);
                            Buffer.SetByte(temp, totalBytesRead, (byte)nextByte);
                            readBuffer = temp;
                            totalBytesRead++;
                        }
                    }
                }

                byte[] buffer = readBuffer;
                if (readBuffer.Length != totalBytesRead)
                {
                    buffer = new byte[totalBytesRead];
                    Buffer.BlockCopy(readBuffer, 0, buffer, 0, totalBytesRead);
                }
                return buffer;
            }
            finally
            {
                if (stream.CanSeek)
                {
                    stream.Position = originalPosition;
                }
            }
        }

        #region ISharedRegionModule
        public void RegionLoaded(Scene scene)
        {
            m_scenes.Add(scene);
        }

        public void PostInitialise()
        {
            IHttpServer server = MainServer.GetHttpServer(0);
            server.AddStreamHandler(new JPEGImageService());
        }

        public Type ReplaceableInterface
        {
            get
            {
                return null;
            }
        }

        public void Initialise(IConfigSource source)
        {
            
        }

        public void Close()
        {
            
        }

        public void AddRegion(Scene scene)
        {
            
        }

        public void RemoveRegion(Scene scene)
        {
            
        }

        new public string Name
        {
            get { return "JPEGImageService"; }
        }
        #endregion

    }
}
