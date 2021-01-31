using MimeKit;
using OpenMetaverse;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chris.OS.Additions.Script.Functions.MailKitMail
{
    class InternalMail
    {
        public MimeMessage Mail = null;
        public UUID ID = UUID.Zero;

        public InternalMail(MimeMessage mail, UUID id)
        {
            Mail = mail;
            ID = id;
        }
    }
}
