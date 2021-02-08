using Chris.OS.Additions.Utils;
using Mono.Addins;
using OpenMetaverse;
using OpenSim.Framework;
using OpenSim.Region.Framework.Interfaces;
using OpenSim.Region.Framework.Scenes;
using OpenSim.Region.ScriptEngine.Shared;
using System;
using System.Collections.Generic;
using System.Text;
using System.Timers;

namespace Chris.OS.Additions.Script.Functions.EasyDialog
{
    [Extension(Path = "/OpenSim/RegionModules", NodeName = "RegionModule", Id = "EasyMenu")]

    public class EasyDialog : EmptyModule
    {
        private IDialogModule m_dialogModule = null;
        private IScriptModule m_scriptEngine;
        private Timer m_timer = null;

        private List<DialogData> m_dialogs = new List<DialogData>();

        #region EmptyModule
        public override string Name
        {
            get { return "osEasyDialog"; }
        }

        public override void RegionLoaded(Scene scene)
        {
            base.World = scene;

            try
            {
                IScriptModuleComms m_scriptModule = base.World.RequestModuleInterface<IScriptModuleComms>();
                m_scriptModule.RegisterScriptInvocation(this, "osEasyDialog");
            }
            catch (Exception e)
            {
                base.Logger.WarnFormat("[" + Name + "]: script method registration failed; {0}", e.Message);
            }

            m_dialogModule = base.World.RequestModuleInterface<IDialogModule>();
            m_scriptEngine = base.World.RequestModuleInterface<IScriptModule>();

            base.World.EventManager.OnChatFromClient += onChat;
            base.World.EventManager.OnScriptReset += onScriptReset;
            base.World.EventManager.OnRemoveScript += onScriptRemove;

            m_timer = new Timer();
            m_timer.Interval = 60000;
            m_timer.AutoReset = true;
            m_timer.Elapsed += cleanup;
            m_timer.Start();
        }

        public override void Close()
        {
            m_timer.Stop();
            m_timer.Dispose();

            base.World.EventManager.OnChatFromClient -= onChat;
            base.World.EventManager.OnScriptReset -= onScriptReset;
            base.World.EventManager.OnRemoveScript -= onScriptRemove;
        }

        #endregion

        #region Events
        private void onChat(object sender, OSChatMessage chat)
        {
            lock (m_dialogs)
            {
                DialogData data = m_dialogs.Find(x => x.ListenerID == chat.Channel && x.UserID == chat.Sender.AgentId);

                if (data != null)
                {
                    if (chat.Message != "<--" && chat.Message != "-->" && chat.Message != "<- EXIT ->" && chat.Message != " " && chat.Message != String.Empty)
                    {
                        m_scriptEngine.PostScriptEvent(data.ScriptID, "listen", new Object[] { data.ListenerID, chat.From.ToString(), data.UserID.ToString(), chat.Message });
                        return;
                    }

                    if (chat.Message == " " || chat.Message == String.Empty)
                        return;

                    if (chat.Message == "<- EXIT ->")
                    {
                        m_dialogs.Remove(data);
                        return;
                    }

                    if (chat.Message == "-->")
                        data.CurrentPage++;

                    if (chat.Message == "<--")
                        data.CurrentPage--;

                    m_dialogModule.SendDialogToUser(data.UserID, data.ObjectName, data.HostID, data.OwnerID, data.getMessage(), new UUID("00000000-0000-2222-3333-100000001000"), data.ListenerID, data.getPageButtons(data.CurrentPage));
                }
            }
        }

        private void onScriptRemove(uint localID, UUID itemID)
        {
            removeItemDialogs(itemID);
        }

        private void onScriptReset(uint localID, UUID itemID)
        {
            removeItemDialogs(itemID);
        }
        #endregion

        #region Script functions
        [ScriptInvocation]
        public int osEasyDialog(UUID hostID, UUID scriptID, String user, String message, Object[] labels)
        {
            List<String> buttons = new List<String>();

            if (UUID.TryParse(user, out UUID target))
            {
                if (labels.Length == 0)
                    throw new ScriptException("Labels must dont be empty!");

                if (Encoding.UTF8.GetByteCount(message) > 512)
                    throw new ScriptException("Message longer than 512 bytes.");

                lock (m_dialogs)
                {
                    SceneObjectPart part = base.World.GetSceneObjectPart(hostID);

                    foreach (Object o in labels)
                    {
                        String text = o.ToString().Trim();

                        if (text == String.Empty)
                            text = " ";

                        if (text.Length >= 24)
                            text = text.Substring(0, 24);

                        buttons.Add(text);
                    }

                    DialogData dialog = new DialogData(hostID, scriptID, target, part.OwnerID, part.Name, message, buttons.ToArray());
                    m_dialogModule.SendDialogToUser(dialog.UserID, dialog.ObjectName, dialog.HostID, dialog.OwnerID, dialog.getMessage(), new UUID("00000000-0000-2222-3333-100000001000"), dialog.ListenerID, dialog.getPageButtons(dialog.CurrentPage));
                    m_dialogs.Add(dialog);
                    return dialog.ListenerID;
                }
            }
            else
            {
                throw new ScriptException("User must be a valid key.");
            }
        }

        #endregion

        #region Functions
        private void cleanup(object sender, ElapsedEventArgs e)
        {
            lock (m_dialogs)
            {
                List<DialogData> dialogs = m_dialogs.FindAll(x => x.LastTimeUsed < Tools.getUnixTime() - 900);
                foreach (DialogData dialog in dialogs)
                {
                    EasyDialogEvents.onDialogTimeout(dialog.ListenerID, dialog.UserID);
                    m_dialogs.Remove(dialog);
                }
            }
        }

        private void removeItemDialogs(UUID itemID)
        {
            lock (m_dialogs)
            {
                List<DialogData> dialogs = m_dialogs.FindAll(x => x.ScriptID == itemID);

                foreach (DialogData dialog in m_dialogs)
                    m_dialogs.Remove(dialog);
            }
        }
        #endregion
    }
}
