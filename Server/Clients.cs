using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Server;
using System.Net;
using DrawMatrixDLL;
using SendBytesToClient;
using System.Runtime.Serialization;

namespace serverChat
{
    public class Client
    {
        private string MapName;
        private Socket _handler;
        private Thread _userThread;
        public EFContext context;
        byte[] ByteMap { get; set; }
        public Client(Socket socket)
        {
            _handler = socket;
            _userThread = new Thread(Listner);
            _userThread.IsBackground = true;
            _userThread.Start();
        }

        private void Listner()
        {
            while (true)
            {
                //try
                //{


                    byte[] buffer = new byte[100000];

                    _handler.Receive(buffer);
                    //Console.WriteLine(buffer[0] + buffer[101]);
                    if (DeserealizationMethod(buffer) == 0)
                        SendMatrixToDb(ByteMap, MapName);
                    else if (DeserealizationMethod(buffer) == 1)
                        SendMapToClient();
                    End();


                //}

                //catch { Server.EndClient(this); return; }
            }
        }

        //private void handleCommand(string data)
        //{
        //    if (data.Contains("#setname"))
        //    {
        //        _userName = data.Split('&')[1];
        //        UpdateChat();
        //        return;
        //    }
        //    if (data.Contains("#newmsg"))
        //    {
        //        string message = data.Split('&')[1];
        //        ChatController.AddMessage(_userName, message);
        //        return;
        //    }
        //}
        public void UpdateChat()
        {
            Send(ChatController.GetChat());
        }
        public void Send(string command)
        {
            try
            {
                int bytesSent = _handler.Send(Encoding.UTF8.GetBytes(command));
                if (bytesSent > 0) Console.WriteLine("Success");
            }
            catch (Exception exp) { Console.WriteLine("Error with send command: {0}.", exp.Message); Server.EndClient(this); }
        }
        public void End()
        {
            try
            {
                _handler.Close();
                try
                {
                    _userThread.Abort();
                }
                catch { } // г
            }
            catch (Exception exp) { Console.WriteLine("Error with end: {0}.", exp.Message); }
        }

        public void SendMatrixToDb(byte[] _bmap, string mapName)
        {
            Map map = new Map();
            context = new EFContext();

            map.MapToByte = _bmap;
            map.MapName = mapName;

            context.Maps.Add(map);

            context.SaveChanges();
        }

        public void ReadMatrixFromDb() //дізнаюсь карту і назву карти для відправки користувачу
        {
            context = new EFContext();
           
            foreach (var map in context.Maps.OrderBy(r => Guid.NewGuid()).Take(context.Maps.Count()))
            {
                ByteMap = map.MapToByte;
                MapName = map.MapName;
            }
        }
        public void SendMapToClient() //метод, який відправляє дані з бд якщо повідомлення == 0
        {
            SendFromHost sendFromHost = new SendFromHost(_handler);
            sendFromHost.SendBytesToClient(ByteMap);
        }

        public int DeserealizationMethod(byte[] buffer)
        {
            MessageClass messageObj;
            using (var memStream = new MemoryStream())
            {
                var binForm = new BinaryFormatter();
                memStream.Write(buffer, 0, buffer.Length);
                memStream.Seek(0, SeekOrigin.Begin);
                var obj = binForm.Deserialize(memStream);
                messageObj = (MessageClass)obj;

                for (int i = 0; i < buffer.Length; i++)
                {
                    ByteMap = buffer;
                }
                MapName = messageObj.MapName;
                if (messageObj.messageStatus == 0)
                    return 0; // повідомлення про залиття карти в бд
                else
                    return 1; //гра
            }
            
        }
    }
}
