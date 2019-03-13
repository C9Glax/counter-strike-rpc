using DiscordRPC;
using System.IO;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;

namespace CSGO_Presence
{
    public class Service : System.ServiceProcess.ServiceBase
    {
        private string uri;
        private readonly Thread httpListener;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        private void UpdateServiceStatus(ServiceState state)
        {
            ServiceStatus serviceStatus = new ServiceStatus
            {
                dwCurrentState = state,
                dwWaitHint = 100000
            };
            SetServiceStatus(this.ServiceHandle, ref serviceStatus);
        }

        public Service()
        {
            this.UpdateServiceStatus(ServiceState.SERVICE_START_PENDING);
            this.GetFreeUri();
            this.httpListener = new Thread(this.ListenerThread);
            this.httpListener.Start();
            this.CsgoInstallation();
            this.UpdateServiceStatus(ServiceState.SERVICE_RUNNING);
        }

        private string GetFreeUri()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            this.uri = @"http://127.0.0.1:" + ((IPEndPoint)l.LocalEndpoint).Port + "/";
            l.Stop();
            return this.uri;
        }

        private void ListenerThread()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(this.uri);
            try
            {
                listener.Start();
                System.Console.Out.WriteLine("jap");
                while (listener.IsListening)
                {
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerResponse response = context.Response;
                    dynamic JSON = JObject.Parse(this.GetRequestData(context.Request));
                    this.UpdateDiscordPresence(JSON);
                    string responseString = "";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
            }
            catch (ThreadAbortException)
            {
                listener.Stop();
                this.UpdateServiceStatus(ServiceState.SERVICE_STOPPED);
            }
        }

        private string GetRequestData(HttpListenerRequest request)
        {
            StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding);
            string s = reader.ReadToEnd();
            request.InputStream.Close();
            reader.Close();
            return s;
        }

        private void CsgoInstallation()
        {
            string file = Properties.Resources.gamestate_integration_discordpresence_cfg.Replace("{uri}", this.uri);
            if (File.Exists(@"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\csgo.exe"))
                File.WriteAllText(@"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\csgo\cfg\gamestate_integration_discordpresence.cfg", file);
            else
                File.WriteAllText(@"C:\gamestate_integration_discordpresence.cfg", file);
        }

        private void UpdateDiscordPresence(dynamic jsondata)
        {
            File.WriteAllText(@"C:\Program Files (x86)\Steam\steamapps\common\Counter-Strike Global Offensive\csgo\cfg\test.json", jsondata);
            RichPresence presence = new RichPresence()
            {
                
            };

            /*
             * if (json.player.activity == "menu")
                Mode = "In menus";

            if (Steam_ID == null)
                Steam_ID = json.player.steamid;


            if (json.map != null)
            {
                if (Mode == "In menus" && json.map.phase.ToString() != "live")
                {
                    Now = null;
                }
                else
                {
                    if (Now == null)
                        Now = DateTime.UtcNow;
                }
                switch (json.map.mode.ToString())
                {
                    case "gungameprogressive":
                        Mode = "Arms Race";
                        break;
                    case "gungametrbomb":
                        Mode = "Demolition";
                        break;
                    case "scrimcomp2v2":
                        Mode = "Wingman";
                        break;
                    default:
                        Mode = char.ToUpper(json.map.mode.ToString().ToCharArray()[0]) + json.map.mode.ToString().Substring(1);
                        break;
                }
                switch (json.map.name.ToString())
                {
                    case "de_cbble":
                        Map = "Cobblestone";
                        break;
                    case "de_stmarc":
                        Map = "St. Marc";
                        break;
                    case "de_dust2":
                        Map = "Dust II";
                        break;
                    case "de_shortnuke":
                        Map = "Nuke";
                        break;
                    default:
                        if (json.map.name.ToString().StartsWith("workshop"))
                        {
                            WorkShop = true;
                            Map = json.map.name.ToString().Substring(json.map.name.ToString().Split('/')[1].Length + json.map.name.ToString().Split('/')[2].Length + 1);
                        }
                        else
                        {
                            WorkShop = false;
                            Map = char.ToUpper(json.map.name.ToString().Substring(3).ToCharArray()[0]) + json.map.name.ToString().Substring(4);
                        }
                        break;
                }
            }

            if (json.player.team != null)
            {
                if (json.player.team.ToString() == "CT")
                    TeamName = "Counter-Terrorists";
                else
                    TeamName = "Terrorists";
                if (json.player.match_stats != null)
                {
                    if (json.player.steamid == Steam_ID)
                    {
                        string s = json.player.team.ToString() == "T"
                            ? $"Score: {json.map.team_t.score}:{json.map.team_ct.score}"
                            : $"Score: {json.map.team_ct.score}:{json.map.team_t.score}";
                        presence.State = $"K: {json.player.match_stats.kills} / A: {json.player.match_stats.assists} / D: {json.player.match_stats.deaths}. {s}";
                    }
                    else
                    {
                        presence.State = $"Spectating. Score: T: {json.map.team_t.score} / CT: {json.map.team_ct.score}";
                    }
                }
                presence.Details = $"Playing {Mode}";
                if (Now != null)
                {
                    presence.Timestamps = new Timestamps()
                    {
                        Start = Now
                    };

                }
                if (!WorkShop)
                {
                    presence.Assets = new Assets()
                    {
                        LargeImageKey = Map.ToLower().Replace(' ', '_'),
                        LargeImageText = Map,
                        SmallImageKey = json.player.team.ToString().ToLower(),
                        SmallImageText = TeamName
                    };
                }
                else
                {
                    presence.Assets = new Assets()
                    {
                        LargeImageKey = "workshop",
                        LargeImageText = Map,
                        SmallImageKey = json.player.team.ToString().ToLower(),
                        SmallImageText = TeamName
                    };
                }
                client.SetPresence(presence);
            }
            else if (Mode == "In menus")
            {
                presence.Details = Mode;
                presence.Assets = new Assets()
                {
                    LargeImageKey = "idle",
                    LargeImageText = "In menus"
                };
                presence.Timestamps = new Timestamps()
                {
                    Start = Start
                };
                client.SetPresence(presence);
            }
            */
        }

        public static void Main(string[] args)
        {
            new Service();
        }
    }

    public enum ServiceState
    {
        SERVICE_STOPPED = 0x00000001,
        SERVICE_START_PENDING = 0x00000002,
        SERVICE_STOP_PENDING = 0x00000003,
        SERVICE_RUNNING = 0x00000004,
        SERVICE_CONTINUE_PENDING = 0x00000005,
        SERVICE_PAUSE_PENDING = 0x00000006,
        SERVICE_PAUSED = 0x00000007,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatus
    {
        public int dwServiceType;
        public ServiceState dwCurrentState;
        public int dwControlsAccepted;
        public int dwWin32ExitCode;
        public int dwServiceSpecificExitCode;
        public int dwCheckPoint;
        public int dwWaitHint;
    };
}
