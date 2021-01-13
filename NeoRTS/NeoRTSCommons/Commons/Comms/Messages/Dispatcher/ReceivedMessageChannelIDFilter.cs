using NeoRTS.Communication.Messages;

namespace NeoRTS
{
    namespace Communication
    {
        /// <summary>
        /// Checks a MESSAGE object's metadata and sees if it has been received / is being sent (depending on context of use) to a specific channel ID.
        /// </summary>
        public class ReceivedMessageChannelIDFilter : MessageDispatcher.IFilter
        {
            public int requiredChannelID;
            public ReceivedMessageChannelIDFilter(int requiredID)
            {
                requiredChannelID = requiredID;
            }

            public bool Enabled => requiredChannelID >= 0;

            public bool FilterMessage(MESSAGE message)
            {
                return message.ChannelID == requiredChannelID;
            }
        }
    }

}

