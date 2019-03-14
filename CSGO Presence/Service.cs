﻿using DiscordRPC;
using System.IO;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Newtonsoft.Json.Linq;
using System.Runtime.InteropServices;

namespace CSGO_Presence
{
    public class CSGORichPresence : System.ServiceProcess.ServiceBase
    {
        private string uri;
        private static DiscordRpcClient discordClient;
        private readonly Thread listenerThread;

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool SetServiceStatus(System.IntPtr handle, ref ServiceStatus serviceStatus);

        public void UpdateServiceStatus(ServiceState state)
        {
            ServiceStatus serviceStatus = new ServiceStatus
            {
                dwCurrentState = state,
                dwWaitHint = 10000
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
            this.listenerThread = new Thread(this.Listen);
            this.listenerThread.Start();
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
                while (true)
                {
                    this.UpdateServiceStatus(ServiceState.SERVICE_RUNNING);
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
                presence.Details = "In Menu";
                presence.State = "Lobby";
            }
            else
            {
                presence.Details = jsondata.player.activity;
                presence.State = "Unknown Activity";
            }
            presence.Assets.LargeImageKey = "csgologo";

            discordClient.SetPresence(presence);
        }
    }
}
