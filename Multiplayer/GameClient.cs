﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;
using Helper.Multiplayer;
using System.Net;
using Helper.Multiplayer.Packets;
using System.Threading;


namespace Multiplayer
{
    public class GameClient
    {
        public int iPort;
        public string sAlias;
        IPAddress a;
        TcpEventClient client;
        ServerInfo Server;
        bool ShouldBeRunning = false;
        Queue<Packet> InputQueue = new Queue<Packet>();
        Thread inputThread;

        public GameClient(string ip, int port, string alias)
        {
            
            if (!IPAddress.TryParse(ip, out a))
                throw new ArgumentException("Unparsable IP");
            
            iPort = port;
            sAlias = alias;
            Server = new ServerInfo(new IPEndPoint(a, iPort));

        }

        public void Connect()
        {
            Debug.WriteLine("Client: Connection " + Server.endPoint.Address.ToString() + " " + iPort);
            try
            {
                ShouldBeRunning = true;
                inputThread = new Thread(new ThreadStart(inputWorker));
                inputThread.Start();
                client = new TcpEventClient();
                client.Connect(Server.endPoint);
                client.PacketReceived += new Helper.Handlers.PacketReceivedEH(PacketReceived);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error..... " + ex.Message);
            }

        }

        void PacketReceived(Helper.Multiplayer.Packets.Packet p)
        {
            InputQueue.Enqueue(p);            
        }

        private void inputWorker()
        {
            while (ShouldBeRunning)
            {
                if (InputQueue.Count > 0)                    
                {

                    ProcessInputPacket(InputQueue.Dequeue());
                }
            }
        }

        private void ProcessInputPacket(Packet packet)
        {
            if (packet is ClientInfoRequestPacket)
            {
                ClientInfoResponsePacket clientInfoResponse = new ClientInfoResponsePacket(sAlias);
                client.Send(clientInfoResponse);
            }
            else if (packet is ChatPacket)
            {
                ChatPacket cp = packet as ChatPacket;
                CallChatMessageReceived(cp.message);
            }
        }

        
        public event Helper.Handlers.StringEH ChatMessageReceived;
        private void CallChatMessageReceived(string msg)
        {
            if (ChatMessageReceived == null)
                return;
            ChatMessageReceived(msg);
        }


        public void Stop()
        {
            ShouldBeRunning = false;
        }

        public void SendChatPacket(string msg)
        {
            client.Send(new ChatPacket(msg));
        }
    }
}
