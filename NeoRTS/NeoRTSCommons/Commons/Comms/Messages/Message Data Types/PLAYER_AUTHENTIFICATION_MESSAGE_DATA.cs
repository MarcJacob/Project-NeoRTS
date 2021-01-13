namespace NeoRTS
{
    namespace Communication
    {

        namespace Messages
        {
            public unsafe struct PLAYER_AUTHENTIFICATION_MESSAGE_DATA : IMessageData
            {
                public const int PLAYER_NAME_MAX_CHAR_LENGTH = 32;

                // TODO : Add whatever data's needed whenever we build an authentification system
                public fixed char playerName[PLAYER_NAME_MAX_CHAR_LENGTH];
            }

            public unsafe struct PLAYER_AUTHENTIFCATION_RESPONSE_MESSAGE_DATA : IMessageData
            {
                public const int REFUSAL_REASON_MESSAGE_MAX_LENGTH = 128;

                public bool accepted;
                public fixed char refusalReason[REFUSAL_REASON_MESSAGE_MAX_LENGTH];
            }
        }
    }
}

