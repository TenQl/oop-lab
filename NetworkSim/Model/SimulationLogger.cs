using System;

namespace NetworkSim.Model
{
    /// <summary>Аргументы события записи в журнал моделирования.</summary>
    public class LogEventArgs : EventArgs
    {
        public double Time { get; }

        public string Source { get; }

        public string Message { get; }

        public LogEventArgs(double time, string source, string message)
        {
            Time = time;
            Source = source;
            Message = message;
        }

        /// <summary>Готовая строка для отображения в журнале.</summary>
        public string Line
        {
            get { return string.Format("[{0:0.0} с] {1}: {2}", Time, Source, Message); }
        }
    }

    /// <summary>
    /// Журнал моделирования. Объекты модели не зависят от интерфейса: они
    /// сообщают о своих действиях событиями, а журнал (наблюдатель) слушает
    /// эти события и уведомляет главную форму о новых записях.
    /// </summary>
    public class SimulationLogger
    {
        private readonly SimulationClock _clock;    // для отметки модельного времени

        /// <summary>Событие: добавлена новая запись журнала.</summary>
        public event EventHandler<LogEventArgs> MessageLogged;

        /// <summary>Создаёт журнал, привязанный к часам моделирования.</summary>
        public SimulationLogger(SimulationClock clock)
        {
            _clock = clock;
        }

        /// <summary>Добавляет запись в журнал с текущим модельным временем.</summary>
        public void Log(string source, string message)
        {
            OnMessageLogged(new LogEventArgs(_clock.Elapsed, source, message));
        }

        /// <summary>Вызывает событие MessageLogged.</summary>
        protected virtual void OnMessageLogged(LogEventArgs e)
        {
            MessageLogged?.Invoke(this, e);
        }
    }
}
