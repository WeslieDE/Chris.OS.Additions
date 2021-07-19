using log4net;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Mono.Addins;
using Nini.Config;
using OpenMetaverse;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace Chris.OS.Additions.Region.Modules.MailKitMailModule
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "MailKitMailModule")]
    class MailKitMailModule : INonSharedRegionModule, IEmailModule
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private IConfigSource m_config = null;
        private bool m_enabled = false;

        private Scene m_scene = null;

        private Timer m_timer_recive = null;
        private Timer m_timer_send = null;

        private List<InternalMail> m_messages = new List<InternalMail>();
        private List<MimeMessage> m_sendMessages = new List<MimeMessage>();

        private bool m_debug = false;
        private int m_sending = 0;

        private String SMTP_SERVER_HOSTNAME = null;
        private String SMTP_SERVER_LOGIN = null;
        private String SMTP_SERVER_PASSWORD = null;
        private String SMTP_SERVER_SENDER = null;
        private int SMTP_SERVER_PORT = 25;
        private bool SMTP_SERVER_SSL = false;
        private bool SMTP_SERVER_TLS = false;

        private String IMAP_SERVER_HOSTNAME = null;
        private String IMAP_SERVER_LOGIN = null;
        private String IMAP_SERVER_PASSWORD = null;
        private int IMAP_SERVER_PORT = 25;
        private bool IMAP_SERVER_SSL = false;
        private bool IMAP_SERVER_TLS = false;

        private bool m_requester = false;

        #region ISharedRegionModule
        public string Name
        {
            get { return "MailKitMailModule"; }
        }

        public Type ReplaceableInterface
        {
            get { return null; }
        }

        public void AddRegion(Scene scene)
        {
            if (!m_enabled)
                return;

            scene.RegisterModuleInterface<IEmailModule>(this);
            m_scene = scene;

            m_timer_send = new Timer();
            m_timer_send.Interval = 1000 * 2;
            m_timer_send.Elapsed += sendAllMails;
            m_timer_send.Enabled = true;
            m_timer_send.Start();

            if (IMAP_SERVER_HOSTNAME != String.Empty)
            {
                m_timer_recive = new Timer();
                m_timer_recive.Interval = 1000 * 15;
                m_timer_recive.Elapsed += checkForMails;
                m_timer_recive.Enabled = true;
                m_timer_recive.Start();
            }
        }


        public void Close()
        {
            
        }

        public void Initialise(IConfigSource source)
        {
            m_config = source;

            if(source.Configs["Startup"] != null)
            {
                m_enabled = (m_config.Configs["Startup"].GetString("emailmodule", "DefaultEmailModule") == "MailKitMailModule");

                if (!m_enabled)
                    return;
            }

            if (source.Configs["Mail"] == null)
                return;

            m_debug = m_config.Configs["Mail"].GetBoolean("DEBUG", m_debug);

            SMTP_SERVER_HOSTNAME = m_config.Configs["Mail"].GetString("SMTP_SERVER_HOSTNAME", String.Empty);
            SMTP_SERVER_PORT = m_config.Configs["Mail"].GetInt("SMTP_SERVER_PORT", 25);
            SMTP_SERVER_SSL = m_config.Configs["Mail"].GetBoolean("SMTP_SERVER_SSL", false);
            SMTP_SERVER_TLS = m_config.Configs["Mail"].GetBoolean("SMTP_SERVER_TLS", false);
            SMTP_SERVER_LOGIN = m_config.Configs["Mail"].GetString("SMTP_SERVER_LOGIN", String.Empty);
            SMTP_SERVER_PASSWORD = m_config.Configs["Mail"].GetString("SMTP_SERVER_PASSWORD", String.Empty);
            SMTP_SERVER_SENDER = m_config.Configs["Mail"].GetString("SMTP_SERVER_SENDER", "");

            IMAP_SERVER_HOSTNAME = m_config.Configs["Mail"].GetString("IMAP_SERVER_HOSTNAME", String.Empty);
            IMAP_SERVER_PORT = m_config.Configs["Mail"].GetInt("IMAP_SERVER_PORT", 143);
            IMAP_SERVER_SSL = m_config.Configs["Mail"].GetBoolean("IMAP_SERVER_SSL", false);
            IMAP_SERVER_TLS = m_config.Configs["Mail"].GetBoolean("IMAP_SERVER_TLS", false);
            IMAP_SERVER_LOGIN = m_config.Configs["Mail"].GetString("IMAP_SERVER_LOGIN", String.Empty);
            IMAP_SERVER_PASSWORD = m_config.Configs["Mail"].GetString("IMAP_SERVER_PASSWORD", String.Empty);
        }

        public void PostInitialise()
        {
        }

        public void RegionLoaded(Scene scene)
        {
        }

        public void RemoveRegion(Scene scene)
        {
        }
        #endregion

        private void checkForMails(object sender, ElapsedEventArgs f)
        {
            if (!m_requester)
                return;

            try
            {
                if(m_debug)
                    m_log.Info("[" + Name + "] checkForMails");

                using (var client = new ImapClient())
                {
                    client.CheckCertificateRevocation = false;
                    client.Timeout = 10000;
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    if (IMAP_SERVER_SSL == true)
                    {
                        if (m_debug)
                            m_log.Info("[" + Name + "] Connect SSL");

                        client.Connect(IMAP_SERVER_HOSTNAME, IMAP_SERVER_PORT, SecureSocketOptions.Auto);
                    }
                    else if (IMAP_SERVER_TLS == true)
                    {
                        if (m_debug)
                            m_log.Info("[" + Name + "] Connect TLS");

                        client.Connect(IMAP_SERVER_HOSTNAME, IMAP_SERVER_PORT, SecureSocketOptions.StartTlsWhenAvailable);
                    }
                    else
                    {
                        if (m_debug)
                            m_log.Info("[" + Name + "] Connect None");

                        client.Connect(IMAP_SERVER_HOSTNAME, IMAP_SERVER_PORT, SecureSocketOptions.None);
                    }

                    if (IMAP_SERVER_LOGIN != String.Empty && IMAP_SERVER_PASSWORD != String.Empty)
                    {
                        if (m_debug)
                            m_log.Info("[" + Name + "] Login with " + IMAP_SERVER_LOGIN + ";" + IMAP_SERVER_PASSWORD);

                        client.Authenticate(IMAP_SERVER_LOGIN, IMAP_SERVER_PASSWORD);
                    
                    }

                    IMailFolder IMAPInbox = client.Inbox;
                    IMAPInbox.Open(FolderAccess.ReadWrite);

                    if (m_debug)
                        m_log.Info("[" + Name + "] Found " + IMAPInbox.Count + " messages.");

                    for (int i = 0; i < IMAPInbox.Count; i++)
                    {
                        MimeMessage message = IMAPInbox.GetMessage(i);
                        foreach (MailboxAddress adress in message.To.Mailboxes)
                        {
                            try
                            {
                                if (m_debug)
                                {
                                    m_log.Info("[" + Name + "] Message To: " + adress.Address);
                                    m_log.Info("[" + Name + "] Objekt ID: " + adress.Address.Split('@')[0]);
                                }

                                String UUIDString = adress.Address.Split('@')[0].Trim();

                                if(isUUID(UUIDString))
                                {
                                    UUID objID = UUID.Parse(UUIDString);
                                    SceneObjectPart sceneObject = m_scene.GetSceneObjectPart(objID);

                                    if (sceneObject != null)
                                    {
                                        m_messages.Add(new InternalMail(message, objID));
                                        IMAPInbox.SetFlags(i, MessageFlags.Deleted, true);

                                        if (m_debug)
                                            m_log.Info("[" + Name + "] Get Message for objekt " + sceneObject.Name + " (" + sceneObject.UUID + ")");
                                    }
                                }
                                else
                                {
                                    IMAPInbox.SetFlags(i, MessageFlags.Deleted, true);
                                }
                            }catch(Exception _innerEroor)
                            {
                                m_log.Error("[" + Name + "] " + _innerEroor.Message);
                                IMAPInbox.SetFlags(i, MessageFlags.Deleted, true);
                            }
                        }
                    }

                    IMAPInbox.Expunge();
                    client.Disconnect(true);
                }
            }catch(Exception _error)
            {
                m_log.Error("[" + Name + "] " + _error.Message);
            }
        }

        private void sendAllMails(object sender, ElapsedEventArgs e)
        {
            int _currentUnixTime = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds; 

            if (m_sendMessages.Count == 0)
                return;

            if ((m_sending + 2) > _currentUnixTime)
                return;

            List<MimeMessage> _messageToSend = new List<MimeMessage>();

            lock (m_sendMessages)
            {
                _messageToSend.AddRange(m_sendMessages);
                m_sendMessages = new List<MimeMessage>();
            }

            m_log.Info("[" + Name + "] Sending " + _messageToSend.Count + " Mails.");

            try
            {
                using (var client = new SmtpClient())
                {
                    client.CheckCertificateRevocation = false;
                    client.Timeout = 10000;

                    if (SMTP_SERVER_SSL == true)
                    {
                        client.Connect(SMTP_SERVER_HOSTNAME, SMTP_SERVER_PORT, SecureSocketOptions.Auto);
                    }
                    else if (SMTP_SERVER_TLS == true)
                    {
                        client.Connect(SMTP_SERVER_HOSTNAME, SMTP_SERVER_PORT, SecureSocketOptions.StartTlsWhenAvailable);
                    }
                    else
                    {
                        client.Connect(SMTP_SERVER_HOSTNAME, SMTP_SERVER_PORT, SecureSocketOptions.None);
                    }

                    if (SMTP_SERVER_LOGIN != String.Empty && SMTP_SERVER_PASSWORD != String.Empty)
                        client.Authenticate(SMTP_SERVER_LOGIN, SMTP_SERVER_PASSWORD);

                    foreach (MimeMessage message in _messageToSend)
                    {
                        try
                        {
                            client.Send(message);
                        }
                        catch (Exception _innerError)
                        {
                            m_log.Error("[" + Name + "] " + _innerError.Message);
                        }
                    }

                    client.Disconnect(true);
                }
            }
            catch(Exception _error)
            {
                m_log.Error("[" + Name + "] " + _error.Message);
            }
        }

        public void SendEmail(UUID objectID, string address, string subject, string body)
        {
            SceneObjectPart sceneObject = m_scene.GetSceneObjectPart(objectID);
            m_sending = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

            try
            {
                lock(m_sendMessages)
                {
                    MimeMessage message = new MimeMessage();
                    message.From.Add(new MailboxAddress(sceneObject.Name, sceneObject.UUID + "@" + SMTP_SERVER_SENDER));
                    message.To.Add(new MailboxAddress("", address));
                    message.Subject = subject;
                    message.Body = new TextPart("plain") { Text = body };

                    message.Headers.Add(new Header(Encoding.UTF8, "ObjectID", sceneObject.UUID.ToString()));
                    message.Headers.Add(new Header(Encoding.UTF8, "AvatarID", sceneObject.OwnerID.ToString()));
                    message.Headers.Add(new Header(Encoding.UTF8, "Location", m_scene.Name + "@" + sceneObject.GetWorldPosition().ToString()));

                    m_sendMessages.Add(message);
                }

            }catch(Exception _error)
            {
                m_log.Error("[" + Name + "] " + _error.Message);
            }
        }

        public Email GetNextEmail(UUID objectID, string sender, string subject)
        {
            m_requester = true;

            SceneObjectPart sceneObject = m_scene.GetSceneObjectPart(objectID);

            if(sceneObject != null)
            {
                List<InternalMail> messages = m_messages.FindAll(X => X.ID == objectID);
                if (messages.Count == 0)
                    return null;

                foreach (InternalMail mail in messages)
                {
                    if (mail.Mail != null)
                    {
                        Email lslMessage = new Email();

                        lslMessage.time = DateTime.Now.ToShortTimeString();

                        if (mail.Mail.TextBody != null)
                            lslMessage.message = mail.Mail.TextBody.ToString();

                        if (mail.Mail.From[0] != null)
                            lslMessage.sender = mail.Mail.From[0].ToString().Split(' ').Last().Replace("<", "").Replace(">", "");

                        if (mail.Mail.Subject != null)
                            lslMessage.subject = mail.Mail.Subject;

                        lslMessage.numLeft = messages.Count - 1;

                        if ((lslMessage.sender == sender || sender == "") && (lslMessage.subject == subject || subject == ""))
                        {
                            m_messages.Remove(mail);
                            return lslMessage;
                        }
                    }
                }

                return null;
            }
            else
            {
                if (m_debug)
                    m_log.Error("[" + Name + "] Cant find caller objekt!");

                return null;
            }
        }

        public bool isUUID(string thing)
        {
            UUID test;
            return UUID.TryParse(thing, out test) ? true : false;
        }
    }
}
