using System;
using System.IO;

using Zepheus.Util;

namespace Zepheus.Zone
{
    [ServerModule(Util.InitializationStage.DataStore)]
    public sealed class ChatLog
    {
        private StreamWriter writer;
        public static ChatLog Instance { get; private set; }

        public ChatLog(string filename)
        {
            writer = new StreamWriter(File.Open(filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite));
            writer.AutoFlush = true;
        }

        [InitializerMethod]
        public static bool Load()
        {
            try
            {
                Instance = new ChatLog("ChatLog.txt");
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine(LogLevel.Exception, "Error initializing chatlog: {0}", ex.ToString());
                return false;
            }
        }

        public void LogChat(string username, string line, bool shout)
        {
            if (shout)
            {
                writer.WriteLine("[{0}][SHOUT] {1} : {2}", Program.CurrentTime.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss"), username, line);
            }
            else
            {
                writer.WriteLine("[{0}] {1} : {2}", Program.CurrentTime.ToString("yyyy'-'MM'-'dd' 'HH':'mm':'ss"), username, line);
            }
        }
    }
}
