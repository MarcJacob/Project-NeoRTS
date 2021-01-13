using static NeoRTS.GameData.Matches.Match;
using static NeoRTS.GameData.ObjectMemoryManager;

namespace NeoRTS
{
    namespace Communication
    {

        namespace Messages
        {

            public struct MATCH_STARTED_MESSAGE_DATA : IMessageData
            {
                public int localPlayerID;
                public string[] playerNames;

                public bool localMatch; // Whether the match is local to this machine or ran on a server.
                public MATCH_STARTED_DATA matchStartData;
            }

            public struct PLAYER_START_MATCHMAKING_MESSAGE_DATA : IMessageData
            {
                // TODO : Add whatever data's needed whenever we require info about matchmaking from the client
            }
        }
    }
}

