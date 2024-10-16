using MCGalaxy.Events.PlayerEvents;
namespace MCGalaxy
{
    public class ChangeClientName : Plugin
    {
        public override string creator { get { return "HarmonyNetwork"; } }
        public override string name { get { return "ClientNameChanger"; } }


        public override void Load(bool startup)
        {
			Command.Register(new CmdChangeClient());
			OnPlayerFinishConnectingEvent.Register(ClientNameChange, Priority.High);
        }
        public override void Unload(bool shutdown)
        {
			Command.Unregister(Command.Find("ChangeClient"));
			OnPlayerFinishConnectingEvent.Unregister(ClientNameChange);
        }
        public void ClientNameChange(Player p)
        {
            string app = p.Session.ClientName();
            string name = p.truename;
			/*if (app == null)
    		{ 
    			Command.Find("opchat").Use(Player.Console, name + " connected using Classic 0.28-0.30.");
    		}
         	else 
            {            
         		Command.Find("opchat").Use(Player.Console, name + " connected using " + app + ".");
			}*/
 			if (!p.Supports(CpeExt.ExtEntityTeleport))
            {
              p.Message("&dHey! &fEven though most of the CPE features exist in " + app + " &f, you'd have a better time using the updated ClassiCube client.", true);
            }
        }
    }
    	public sealed class CmdChangeClient : Command {
		public override string name { get { return "ChangeClient"; } }
		public override string type { get { return CommandTypes.Other; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Admin; } }
		public override void Use(Player p, string message) 
        {
            //Have to override both, otherwise this doesn't work.
			string app =  p.Session.appName;
       		string app2 = p.Session.ClientName();
        	p.Session.appName = message;
        	app = message;
        	app2 = app;
			p.Message("Changed your client name to " + app + "&f.");
		}
		
		public override void Help(Player p) 
        {
			p.Message("&T/ChangeClient [Client] &H- Changes your client name.");
		}
	}
}