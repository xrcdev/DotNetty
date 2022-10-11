using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SanTint.DosingExpertCore.NettyCommon
{
    public delegate void CommandReceiveEvent(object sender, MessageEventArgs e);
    public delegate void MessageSendEvent(object sender, MessageEventArgs e);
    public delegate void MessageGroupSendEvent(object sender, MessageEventArgs e);
    public delegate void MessageOfflineEvent(object sender, string e);
    public class MessageEventArgs : EventArgs
    {
        public MessageEventArgs(IMessage cmd) => CMD = cmd;

        public IMessage CMD { set; get; }
    }
}
