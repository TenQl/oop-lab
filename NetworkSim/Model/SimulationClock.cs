using System;
using System.Diagnostics;
using System.Windows.Forms;

namespace NetworkSim.Model
{
    /// <summary>
    /// Единственный источник времени в модели: обёртка над системным таймером,
    /// порождающая событие Tick с шагом модельного времени. Все объекты модели
    /// подписываются на это событие и ведут свои таймеры накоплением шага Dt.
    /// Шаг пропорционален реально прошедшему времени, поэтому коэффициент
    /// ускорения точно задаёт скорость модели.
    /// </summary>
    public class SimulationClock
    {
        /// <summary>Минимальный коэффициент ускорения.</summary>
        public const double MinSpeedFactor = 0.25;

        /// <summary>Максимальный коэффициент ускорения.</summary>
        public const double MaxSpeedFactor = 5.0;

        private readonly Timer _timer;              // системный таймер (UI-поток)
        private readonly Stopwatch _stopwatch = new Stopwatch();
        private double _elapsed;                    // накопленное модельное время, с
        private double _speedFactor = 1.0;

        /// <summary>Интервал системного таймера, мс.</summary>
        public int Interval
        {
            get { return _timer.Interval; }
            set { _timer.Interval = value; }
        }

        /// <summary>Коэффициент ускорения модельного времени (от 0,25 до 5).</summary>
        public double SpeedFactor
        {
            get { return _speedFactor; }
            set
            {
                if (value < MinSpeedFactor || value > MaxSpeedFactor)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Коэффициент скорости должен быть в диапазоне от 0,25 до 5.");
                }
                _speedFactor = value;
            }
        }

        /// <summary>Идёт ли моделирование в данный момент (только чтение).</summary>
        public bool IsRunning
        {
            get { return _timer.Enabled; }
        }

        /// <summary>Модельное время с момента создания часов, с (только чтение).</summary>
        public double Elapsed
        {
            get { return _elapsed; }
        }

        /// <summary>Событие: очередной шаг модельного времени.</summary>
        public event EventHandler<TickEventArgs> Tick;

        /// <summary>
        /// Создаёт часы. Интервал 25 мс система округляет до ~31 мс,
        /// что даёт около 30 шагов в секунду.
        /// </summary>
        public SimulationClock(int intervalMs = 25)
        {
            _timer = new Timer();
            _timer.Interval = intervalMs;
            _timer.Tick += OnTimerTick;
        }

        /// <summary>Запускает моделирование.</summary>
        public void Start()
        {
            _stopwatch.Restart();
            _timer.Start();
        }

        /// <summary>Останавливает моделирование.</summary>
        public void Stop()
        {
            _timer.Stop();
            _stopwatch.Reset();
        }

        /// <summary>Вызывает событие Tick.</summary>
        protected virtual void OnTick(TickEventArgs e)
        {
            Tick?.Invoke(this, e);
        }

        // Обработчик таймера: реально прошедшее время переводится в модельное.
        private void OnTimerTick(object sender, EventArgs e)
        {
            double realDt = _stopwatch.Elapsed.TotalSeconds;
            _stopwatch.Restart();
            double dt = realDt * _speedFactor;
            _elapsed += dt;
            OnTick(new TickEventArgs(dt));
        }
    }
}
