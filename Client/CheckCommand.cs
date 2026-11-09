using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    public class CheckCommand
    {
        /// <summary> Функция проверки команд </summary>
        public static bool Check(string message)
        {
            bool BCommand = false;
            string[] DataMessage = message.Split(new string[] { " " }, StringSplitOptions.None);

            if (DataMessage.Length > 0)
            {
                string Command = DataMessage[0];

                if (Command == "connect")
                {
                    if (DataMessage.Length != 3)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: connect [login] [password]\nПример: connect User1 P@ssw0rd");
                        BCommand = false;
                        Console.ResetColor();
                    }
                    else
                    {
                        BCommand = true;
                    }
                }
                else if (Command == "cd")
                {
                    BCommand = true;
                }
                else if (Command == "get")
                {
                    if (DataMessage.Length == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: get [NameFile]\nПример: get Test.txt");
                        BCommand = false;
                        Console.ResetColor();
                    }
                    else
                    {
                        BCommand = true;
                    }
                }
                else if (Command == "set")
                {
                    if (DataMessage.Length == 1)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Использование: set [NameFile]\nПример: set Test.txt");
                        BCommand = false;
                        Console.ResetColor();
                    }
                    else
                    {
                        BCommand = true;
                    }
                }
            }

            return BCommand;
        }
    }
}