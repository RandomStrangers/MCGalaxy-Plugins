namespace MCGalaxy
{
    public class LogPlugin : Plugin
    {
        public override string creator { get { return "HarmonyNetwork"; } }
        public override string name { get { return "LogChoice"; } }
        public override void Load(bool startup)
        {
            Command.Register(new CmdLogChoice());
        }
        public override void Unload(bool shutdown)
        {
            Command.Unregister(Command.Find("LogChoice"));
        }
        public override void Help(Player p)
        {
            p.Message("No help is available for this plugin.");
        }
        public class CmdLogChoice : Command
        {
            public override string name { get { return "LogChoice"; } }
            public override string shortcut { get { return "Log"; } }
            public override string type { get { return CommandTypes.Information; } }
            public override bool museumUsable { get { return true; } }
            public override LevelPermission defaultRank { get { return LevelPermission.Owner; } }
            public override void Use(Player p, string message)
            {
                bool messageEmpty = string.IsNullOrEmpty(message);
                string[] args = message.SplitSpaces(2);
                int x;
                int.TryParse(args[0], out x);
                LogType type = (LogType)x;
                if (args.Length < 1) 
                { 
                    Help(p); 
                    return; 
                }

                if (args.Length == 1)
                {
                    p.Message(args[0] + " is LogType." + type + ".");
                    return;
                }
                else
                {
                    if (args.Length >= 2)
                    {
                        string reason = args[1];
                        string playername = p.truename;
                        p.Message("&cLog of type: " + type + " sent.");
                        Logger.Log(type, reason);
                    }
                }
                if (messageEmpty == true)
                {
                    Help(p);
                    return;
                }
            }
            public override void Help(Player p)
            {
                p.Message("&T/LogChoice [LogType] [Message] &H- Sends a message to Console.");
            }
        }
    }
}