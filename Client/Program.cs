using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;

namespace Client
{
    public class Program
    {
        public static IPAddress IpAdress;
        public static int Port;
        public static int Id = -1;

        static void Main(string[] args)
        {
            Console.Write("Введите IP адрес сервера: ");
            string sIpAdress = Console.ReadLine();

            Console.Write("Введите порт: ");
            string sPort = Console.ReadLine();
            if (int.TryParse(sPort, out Port) && IPAddress.TryParse(sIpAdress, out IpAdress))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Данные успешно введены. Подключаюсь к серверу.");

                while (true)
                {
                    ConnectServer.Connect();
                }
            }
        }
    }
}