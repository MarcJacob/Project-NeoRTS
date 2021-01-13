using NeoRTS.GameData;
using System.Collections.Generic;
namespace NeoRTS
{
    namespace Tools
    {
        public static class Debug
        {
            public struct DEBUG_DRAW_REQUEST
            {
                public enum DRAW_TYPE
                {
                    LINE,
                    RAY,
                    SPHERE,
                    CUBE
                }

                public DEBUG_DRAW_REQUEST(DRAW_TYPE type, UnityEngine.Color color, float size, params Position[] positions)
                {
                    drawType = type;
                    this.size = size;
                    this.color = color;
                    this.positions = positions;
                }

                public DRAW_TYPE drawType;
                public Position[] positions;
                public UnityEngine.Color color;
                public float size;
            }


            private static Queue<DEBUG_DRAW_REQUEST> m_unpolledDrawRequests = new Queue<DEBUG_DRAW_REQUEST>();
            private static Queue<string> m_unreadMessageLog = new Queue<string>();

            public static void Log(string message)
            {
                m_unreadMessageLog.Enqueue(message);
            }

            public static void LogWarning(string message)
            {
                Log(message);
            }

            public static void LogError(string message)
            {
                Log(message);
            }

            public static void LogDrawRequest(DEBUG_DRAW_REQUEST request)
            {
                m_unpolledDrawRequests.Enqueue(request);
            }

            public static bool PollMessage(out string msg)
            {
                if (m_unreadMessageLog.Count > 0)
                {
                    msg = m_unreadMessageLog.Dequeue();
                    return true;
                }
                else
                {
                    msg = "";
                    return false;
                }
            }

            public static bool PollDrawRequest(out DEBUG_DRAW_REQUEST request)
            {
                if (m_unpolledDrawRequests.Count > 0)
                {
                    request = m_unpolledDrawRequests.Dequeue();
                    return true;
                }
                else
                {
                    request = new DEBUG_DRAW_REQUEST();
                    return false;
                }
            }

            public static void Assert(bool assertion, string failMessage)
            {
                System.Diagnostics.Debug.Assert(assertion, failMessage);
            }
        }
    }
}
