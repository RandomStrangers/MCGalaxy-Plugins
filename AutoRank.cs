//Put any vaild ip to auto rank in line 22
using MCGalaxy.Events.PlayerEvents;
namespace MCGalaxy
{
    public class AutoRank : Plugin
    {
        public override string creator { get { return "HarmonyNetwork"; } }
        public override string name { get { return "AutoRank"; } }

        public override void Load(bool startup)
        {
            OnPlayerConnectEvent.Register(Rank, Priority.Critical);
        }
        public override void Unload(bool shutdown)
        {
            OnPlayerConnectEvent.Unregister(Rank);
        }
        public void Rank(Player p)
        {
            string name = p.truename;
            string ip = p.ip;
            string RankIP = "127.0.0.1";
            if (ip == RankIP)
            {
                Command.Find("setrank").Use(Player.Console, name + " " + LevelPermission.Admin 
                    + " Auto-rank by Console&S.");
            }
        }
    }
}