namespace PSSRoot
{
    public class Log
    {
        static Mutex mLock = new Mutex();
        private static void printAllLines(string message, bool error)
        {
            foreach (string line in message.Replace("\r", "").Split('\n'))
            {
                if (error) Console.Error.WriteLine("[*] " + message);
                else Console.WriteLine("[*] " + message);
            }
        }

        private static void printMessageColored(ConsoleColor color, string message, bool error=false)
        {
            mLock.WaitOne();
            ConsoleColor prevColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            printAllLines(message, error);
            Console.ForegroundColor = prevColor;
            mLock.ReleaseMutex();
        }

        public static void Debug(string message)
        {
        #if DEBUG
            printMessageColored(ConsoleColor.DarkGray, message);
        #endif
        }
        public static void Command(string message)
        {
            printMessageColored(ConsoleColor.Yellow, message);
        }
        public static void Task(string message)
        {
            printMessageColored(ConsoleColor.Green, message);
        }
        public static void Info(string message)
        {
            printMessageColored(ConsoleColor.Gray, message);
        }
        public static void Warn(string message)
        {
            printMessageColored(ConsoleColor.Cyan, message, true);
        }
        public static void Error(string message)
        {
            printMessageColored(ConsoleColor.Red, message, true);
        }
    }
}
