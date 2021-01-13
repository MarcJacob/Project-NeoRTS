namespace NeoRTS
{
    namespace Communication
    {

        namespace Messages
        {
            public struct MESSAGE_TYPE_RANGE
            {
                static public readonly MESSAGE_TYPE_RANGE GENERAL_MESSAGES = new MESSAGE_TYPE_RANGE(0, 9);
                static public readonly MESSAGE_TYPE_RANGE MATCH_MESSAGES = new MESSAGE_TYPE_RANGE(10, 999);
                static public readonly MESSAGE_TYPE_RANGE CLIENT_SERVER_COMM_MESSAGES = new MESSAGE_TYPE_RANGE(1000, 1499);

                public int Min { get; private set; }
                public int Max { get; private set; }

                public MESSAGE_TYPE_RANGE(int min, int max)
                {
                    Min = min;
                    Max = max;
                }
            }
            public enum MESSAGE_TYPE
            {
                

                // GENERAL MESSAGES
                PING = 0,
                PONG = 1,

                // MATCH MESSAGE TYPES : 10 - 999
                MATCH_STARTED = 10,  // Notifies players that a local or online match has started. Contains the local player's ID within the match data
                                    // aswell as (optionally) other meta data such as info about the other players involved. Messages of this type
                                    // contain a MATCH_STARTED_DATA data structure.
                MATCH_ENDED = 11,

                OBJECT_DATA_CHANGE_EVENT = 12,   // Notifies the receiver that the sender has modified the current order of some units.
                                    // Contains an UnitDataChangedMessageData data structure of template type OBJECT_DATA_AI. 
                SYNCH_CHECK = 13,
                UNITS_SPAWNED = 14,
                UNITS_DIED = 15,

                // CLIENT <-> SERVER COMMS MESSAGE TYPES : 1000 - 1999
                PLAYER_AUTHENTIFICATION = 1000,
                PLAYER_AUTHENTIFICATION_RESPONSE = 1001,
                PLAYER_START_MATCHMAKING = 1002,
                PLAYER_MATCH_FOUND = 1003,

                PLAYER_REQUEST_JOIN_CHANNEL = 1010,
                PLAYER_REQUEST_LEAVE_CHANNEL = 1011,
                PLAYER_SENT_MESSAGE = 1012,
            }
        }
    }
}

