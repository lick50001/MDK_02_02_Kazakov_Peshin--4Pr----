using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Ftp;

namespace Client
{
    public class Program
    {
        public static IPAddress IpAdress;
        public static int Port;
        public static int Id = -1;

        static void Main()
        {
            Console.Write("Введите IP адрес сервера: ");
            string sIpAdress = Console.ReadLine();

            Console.Write("Введите порт: ");
            string sPort = Console.ReadLine();

            if (int.TryParse(sPort, out Port) && IPAddress.TryParse(sIpAdress, out IpAdress))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Данные успешно введены. Подключаюсь к серверу.");
                Console.ResetColor();

                while (true)
                    ConnectServer();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Неверный IP или порт.");
                Console.ResetColor();
            }
        }

        static string ReadAll(Socket s)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] buf = new byte[65536];
                int n;
                while ((n = s.Receive(buf)) > 0)
                    ms.Write(buf, 0, n);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        static string JoinArgs(string[] parts)
        {
            return string.Join(" ", parts, 1, parts.Length - 1);
        }

        public static void ConnectServer()
        {
            try
            {
                // 1. Сначала читаем команду, потом подключаемся
                Console.ForegroundColor = ConsoleColor.White;
                string message = Console.ReadLine();
                Console.ResetColor();

                if (string.IsNullOrWhiteSpace(message) || !CheckCommand(message))
                    return;

                string[] parts = message.Split(' ');
                ViewModelSend viewModelSend = new ViewModelSend(message, Id);

                if (parts[0] == "set")
                {
                    string nameFile = JoinArgs(parts);
                    if (!File.Exists(nameFile))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Указанный файл не существует");
                        Console.ResetColor();
                        return; // не отправляем мусор на сервер
                    }

                    FileInfoFTP fileInfoFtp = new FileInfoFTP(File.ReadAllBytes(nameFile), Path.GetFileName(nameFile));
                    viewModelSend = new ViewModelSend("set " + JsonConvert.SerializeObject(fileInfoFtp), Id);
                }

                IPEndPoint endPoint = new IPEndPoint(IpAdress, Port);
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(endPoint);

                    byte[] messageByte = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(viewModelSend));
                    socket.Send(messageByte);
                    socket.Shutdown(SocketShutdown.Send); // сообщаем серверу, что запрос закончен

                    string messageServer = ReadAll(socket);
                    ViewModelMessage viewModelMessage = JsonConvert.DeserializeObject<ViewModelMessage>(messageServer);

                    if (viewModelMessage == null)
                    {
                        Console.WriteLine("Пустой ответ сервера.");
                    }
                    else if (viewModelMessage.Command == "authorization")
                    {
                        Id = int.Parse(viewModelMessage.Data);
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("Авторизация успешна.");
                        Console.ResetColor();
                    }
                    else if (viewModelMessage.Command == "message")
                    {
                        Console.WriteLine(viewModelMessage.Data);
                    }
                    else if (viewModelMessage.Command == "cd")
                    {
                        List<string> foldersFiles = JsonConvert.DeserializeObject<List<string>>(viewModelMessage.Data);
                        foreach (string name in foldersFiles)
                            Console.WriteLine(name);
                    }
                    else if (viewModelMessage.Command == "file")
                    {
                        string getFile = Path.GetFileName(JoinArgs(parts));
                        byte[] byteFile = JsonConvert.DeserializeObject<byte[]>(viewModelMessage.Data);
                        File.WriteAllBytes(getFile, byteFile);
                        Console.WriteLine("Файл сохранён: " + getFile);
                    }
                }
            }
            catch (Exception exp)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Что-то случилось: " + exp.Message);
                Console.ResetColor();
            }
        }

        public static bool CheckCommand(string message)
        {
            string[] parts = message.Split(' ');
            string command = parts[0];

            if (command == "connect")
            {
                if (parts.Length != 3)
                {
                    Error("Использование: connect [login] [password]\nПример: connect User1 P@ssw0rd");
                    return false;
                }
                return true;
            }
            if (command == "cd") return true;
            if (command == "get" || command == "set")
            {
                if (parts.Length == 1)
                {
                    Error("Использование: " + command + " [NameFile]\nПример: " + command + " Test.txt");
                    return false;
                }
                return true;
            }

            Error("Неизвестная команда. Доступно: connect, cd, get, set");
            return false;
        }

        static void Error(string text)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(text);
            Console.ResetColor();
        }
    }
}