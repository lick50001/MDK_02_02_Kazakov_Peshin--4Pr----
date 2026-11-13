using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Ftp;
using Newtonsoft.Json;

namespace Server
{
    public class Program
    {
        public static List<User> Users = new List<User>();
        public static IPAddress IpAddress;
        public static int Port;

        static void Main(string[] args)
        {
            Users.Add(new User("123", "123", @"A:\Авiatexнимум"));

            Console.Write("Введите IP адрес сервера: ");
            string sIpAddress = Console.ReadLine();
            Console.Write("Введите порт: ");
            string sPort = Console.ReadLine();

            if (int.TryParse(sPort, out Port) && IPAddress.TryParse(sIpAddress, out IpAddress))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Данные успешно введены. Запускаю сервер.");
                Console.ResetColor();
                StartServer();
            }

            Console.Read();
        }

        public static bool AutorizationUser(string login, string password)
        {
            return Users.Find(x => x.login == login && x.password == password) != null;
        }

        public static List<string> GetDirectory(string src)
        {
            List<string> foldersFiles = new List<string>();
            if (Directory.Exists(src))
            {
                foreach (string dir in Directory.GetDirectories(src))
                    foldersFiles.Add(Path.GetFileName(dir) + "/");

                foreach (string file in Directory.GetFiles(src))
                    foldersFiles.Add(Path.GetFileName(file));
            }
            return foldersFiles;
        }

        // Склейка пути + защита от выхода за корневую папку пользователя
        static string SafePath(User user, string baseDir, string name)
        {
            string full = Path.GetFullPath(Path.Combine(baseDir, name));
            string root = Path.GetFullPath(user.src).TrimEnd('\\') + "\\";
            if (!(full.TrimEnd('\\') + "\\").StartsWith(root, StringComparison.OrdinalIgnoreCase))
                return null;
            return full;
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

        static void SendReply(Socket handler, ViewModelMessage msg)
        {
            byte[] data = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));
            handler.Send(data);
        }

        static string JoinArgs(string[] parts)
        {
            return string.Join(" ", parts, 1, parts.Length - 1);
        }

        public static void StartServer()
        {
            IPEndPoint endPoint = new IPEndPoint(IpAddress, Port);
            Socket sListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sListener.Bind(endPoint);
            sListener.Listen(10);
            Console.WriteLine("Сервер запущен");

            while (true)
            {
                Socket handler = null;
                try
                {
                    handler = sListener.Accept();
                    string data = ReadAll(handler);
                    if (string.IsNullOrEmpty(data)) continue;

                    string shortLog = data.Length > 200 ? data.Substring(0, 200) + "..." : data;
                    Console.WriteLine("Сообщение от пользователя: " + shortLog);

                    ViewModelSend request = JsonConvert.DeserializeObject<ViewModelSend>(data);
                    if (request == null || request.Message == null) continue;

                    ViewModelMessage reply;
                    string[] parts = request.Message.Split(new string[] { " " }, 2, StringSplitOptions.None);
                    string command = parts[0];
                    bool logged = request.Id >= 0 && request.Id < Users.Count;

                    if (command == "connect")
                    {
                        string[] args = request.Message.Split(' ');
                        if (args.Length == 3 && AutorizationUser(args[1], args[2]))
                        {
                            int idUser = Users.FindIndex(x => x.login == args[1] && x.password == args[2]);
                            reply = new ViewModelMessage("authorization", idUser.ToString());
                        }
                        else
                            reply = new ViewModelMessage("message", "Неправильный логин или пароль пользователя");
                    }
                    else if (!logged)
                    {
                        reply = new ViewModelMessage("message", "Необходимо авторизоваться");
                    }
                    else
                    {
                        User user = Users[request.Id];

                        if (command == "cd")
                        {
                            string[] all = request.Message.Split(' ');
                            if (all.Length == 1)
                            {
                                user.temp_src = user.src;
                                reply = new ViewModelMessage("cd", JsonConvert.SerializeObject(GetDirectory(user.temp_src)));
                            }
                            else
                            {
                                string folder = JoinArgs(all).Trim().TrimEnd('/', '\\');
                                string newPath = SafePath(user, user.temp_src, folder);

                                if (newPath == null || !Directory.Exists(newPath))
                                {
                                    reply = new ViewModelMessage("message", "Директория не существует или недоступна.");
                                }
                                else
                                {
                                    user.temp_src = newPath;
                                    List<string> list = GetDirectory(newPath);
                                    if (list.Count == 0)
                                        reply = new ViewModelMessage("message", "Директория пуста.");
                                    else
                                        reply = new ViewModelMessage("cd", JsonConvert.SerializeObject(list));
                                }
                            }
                        }
                        else if (command == "get")
                        {
                            string name = parts.Length > 1 ? parts[1].Trim() : "";
                            string filePath = SafePath(user, user.temp_src, name);

                            if (filePath == null || !File.Exists(filePath))
                                reply = new ViewModelMessage("message", "Файл не найден.");
                            else
                                reply = new ViewModelMessage("file", JsonConvert.SerializeObject(File.ReadAllBytes(filePath)));
                        }
                        else if (command == "set")
                        {
                            FileInfoFTP file = JsonConvert.DeserializeObject<FileInfoFTP>(parts[1]);
                            string savePath = SafePath(user, user.temp_src, Path.GetFileName(file.Name));

                            if (savePath == null)
                            {
                                reply = new ViewModelMessage("message", "Недопустимое имя файла.");
                            }
                            else
                            {
                                File.WriteAllBytes(savePath, file.Data);
                                reply = new ViewModelMessage("message", "Файл загружен");
                            }
                        }
                        else
                        {
                            reply = new ViewModelMessage("message", "Неизвестная команда.");
                        }
                    }

                    SendReply(handler, reply);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Что-то случилось: " + ex.Message);
                    Console.ResetColor();
                    try
                    {
                        if (handler != null)
                            SendReply(handler, new ViewModelMessage("message", "Ошибка сервера: " + ex.Message));
                    }
                    catch { }
                }
                finally
                {
                    if (handler != null) handler.Close();
                }
            }
        }
    }
}