using System;
using System.Collections.Generic;
using System.IO;

namespace Cine_Critic_AI.Services
{
    // Kласa AppLoggerSingleton представлява единствен, споделен за цялото приложение
    // логър, който записва съобщенията с дата и час както в паметта, така и във външен файл (AppLogs.txt).
    // Singleton шаблонът гарантира, че ще има само един активен екземпляр от логъра,
    // до който се достъпва чрез AppLoggerSingleton.Instance,
    // а методът Log() добавя нов запис в списъка и го записва във файла.
    public sealed class AppLoggerSingleton
    {
        // Lazy инициализация на Singleton-а. Обектът ще се създаде само при първото извикване на Instance
        private static readonly Lazy<AppLoggerSingleton> lazy =
            new Lazy<AppLoggerSingleton>(() => new AppLoggerSingleton());

        public static AppLoggerSingleton Instance => lazy.Value;

        private readonly List<string> _logs = new List<string>();

        private readonly string _logFilePath;

        // Private конструктор – предотвратява създаването на други екземпляри
        private AppLoggerSingleton()
        {
            _logFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppLogs.txt");
        }

        // Метод за записване на лог съобщение
        public void Log(string message)
        {
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";

            // В текущата реализация _logs не е thread-safe при многопоточен достъп
            _logs.Add(logEntry);

            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
        }

        // Метод за достъп до логовете като read-only списък
        public IReadOnlyList<string> GetLogs() => _logs.AsReadOnly();
    }
}
