using DiscordRPC;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CSGO_Presence
{
    public class CSGORichPresence : System.ServiceProcess.ServiceBase
    {
        private string uri;
        private static DiscordRpcClient discordClient;

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


        public CSGORichPresence()
        {
            this.ServiceName = "CSGORichPresence";
            this.UpdateServiceStatus(ServiceState.SERVICE_START_PENDING);
            discordClient = new DiscordRpcClient("555446389320974348", "730", false, -1, null);
            discordClient.Initialize();
            this.GetFreeUri();
            this.CsgoInstallation();
            this.UpdateServiceStatus(ServiceState.SERVICE_RUNNING);
            this.Listen();
        }

        private string GetFreeUri()
        {
            TcpListener l = new TcpListener(IPAddress.Loopback, 0);
            l.Start();
            this.uri = @"http://127.0.0.1:" + ((IPEndPoint)l.LocalEndpoint).Port;
            l.Stop();
            return this.uri;
        }

        private void Listen()
        {
            try
            {
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add(this.uri + "/");
                listener.Start();
                HttpListenerContext context = listener.GetContext();
                HttpListenerResponse response = context.Response;
                dynamic Json = JObject.Parse(this.GetRequestData(context.Request));
                response.StatusCode = 200;
                Stream output = response.OutputStream;
                output.Write(new byte[1], 0, (new byte[1]).Length);
                listener.Stop();
                this.UpdateDiscordPresence(Json);
                this.Listen();
            }
            catch (ThreadAbortException)
            {
                this.UpdateServiceStatus(ServiceState.SERVICE_STOPPED);
                discordClient.ClearPresence();
                discordClient.Dispose();
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
            RichPresence presence = new RichPresence() { Assets = new Assets() };

            if (jsondata.player.activity == "playing")
            {
                if (jsondata.player.team == "T")
                {
                    presence.State = $"{jsondata.map.team_ct.name} {jsondata.map.team_t.score} - {jsondata.map.team_ct.score} CT";
                    presence.Assets.SmallImageKey = "tcoin";
                }
                else
                {
                    presence.State = $"{jsondata.map.team_ct.name} {jsondata.map.team_ct.score} - {jsondata.map.team_t.score} T";
                    presence.Assets.SmallImageKey = "ctcoin";
                }

                string gamemode = jsondata.map.mode;
                switch (jsondata.map.mode.ToString())
                {
                    case "gungameprogressive":
                        gamemode = "Arms Race";
                        break;
                    case "gungametrbomb":
                        gamemode = "Demolition";
                        break;
                    case "scrimcomp2v2":
                        gamemode = "Wingman";
                        break;
                    default:
                        gamemode = char.ToUpper(jsondata.map.mode.ToString().ToCharArray()[0]) + jsondata.map.mode.ToString().Substring(1);
                        break;
                }

                string mapname = jsondata.map.name;
                if (mapname.StartsWith("workshop"))
                    mapname = mapname.Split('/')[1];

                presence.Details = $"{jsondata.map.mode} on {mapname}";
            }
            else if (jsondata.player.activity == "menu")
            {
                presence.Details = "CS:GO - In Menu";
                presence.State = "Lobby";
            }

            discordClient.SetPresence(presence);
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
