﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using Helper.Collections;
using System.Net;
using System.Diagnostics;

namespace Helper.Communication
{
    public class SocketComm
    {
        public Socket socket;
        Thread inputThread;
        IPEndPoint endPoint;
        public delegate void PacketReceivedEventHandler(byte[] data);
        public event PacketReceivedEventHandler PacketReceived;

        bool ShouldBeRunning = false;
        ThreadQueue DataToSendQueue;

        public SocketComm()
        {
            

            
            DataToSendQueue = new ThreadQueue();
        }

        public void Dicsonnect()
        {
            ShouldBeRunning = false;
        }

        public void Send(byte[] data)
        {
            DataToSendQueue.Enqueue(data);
        }

        private void inputWorker()
        {
            List<byte[]> dataToSend = new List<byte[]>();
            byte[] lenBytes = new byte[4];
            int length = -1;
            
            while (ShouldBeRunning)
            {
                if (length == -1 && socket.Available>=4)
                {
                    int count = socket.Receive(lenBytes);
                    length = BitConverter.ToInt32(lenBytes, 0);
                    Debug.WriteLine(length);
                    if (length > 5000)
                        throw new FormatException("packet length "+length+" is unreasonably long.");
                }
                else if (length>0 && socket.Available>=length)
                {
                    byte[] data = new byte[length];
                    int datacount = socket.Receive(data);
                    
                    if (data != null)
                        CallPacketReceived(data);
                    length = -1;
                }

                dataToSend.Clear();
                while (DataToSendQueue.Count > 0)
                    dataToSend.Add(DataToSendQueue.DeQ() as byte[]);
                if (!socket.Connected)
                    continue;
                try
                {
                    foreach (byte[] b in dataToSend)
                        socket.Send(b);
                }
                catch(SocketException E)
                {
                    ShouldBeRunning = false;
                    System.Diagnostics.Debug.WriteLine(E.StackTrace);
                    // no longer connected.
                }
                Thread.Sleep(10);
            }

            CallClientDisconnected();
        }

        private void CallClientDisconnected()
        {
            if (ClientDisconnected == null)
                return;
            ClientDisconnected();
        }
        public event Helper.Handlers.voidEH ClientDisconnected;

        public void CallPacketReceived(byte[] data)
        {
            if (PacketReceived == null)
                return;
            PacketReceived(data);
        }

        internal void Connect(IPEndPoint remoteEndPoint)
        {
            this.endPoint = remoteEndPoint;
            TcpClient client = new TcpClient();
            try
            {
                client.Connect(remoteEndPoint);
                socket = client.Client;
            }
            catch (Exception E)
            {

            }

            ShouldBeRunning = true;
            inputThread = new Thread(new ThreadStart(inputWorker));
            inputThread.Start();
        }
    }
}