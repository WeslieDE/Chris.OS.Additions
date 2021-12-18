using MimeKit;
using OpenMetaverse;

namespace Chris.OS.Additions.Region.Modules.MailKitMailModule
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
