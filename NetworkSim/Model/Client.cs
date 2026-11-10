using System;
using System.Collections.Generic;
using System.Drawing;

namespace NetworkSim.Model
{
    /// <summary>
    /// Клиент — источник задач и получатель результата. Отправляет задачи
    /// в сеть, при получении ошибки данных исправляет её за время FixTime
    /// и отправляет задачу повторно (с начала цепочки узлов); получив
    /// результат, передаёт его пользователю. После MaxAttempts неудачных
    /// попыток задача отменяется.
    /// </summary>
    public class Client : IPacketEndpoint, IDrawable
    {
        protected readonly SimulationClock Clock;

        private const float IconWidth = 52f;
        private const float IconHeight = 36f;
        private const double DoneTime = 2.0;        // сколько показывается «результат получен», с

        private readonly Queue<Packet> _fixQueue = new Queue<Packet>(); // полученные ошибки
        private double _fixRemaining;       // оставшееся время исправления, с
        private double _doneRemaining;      // оставшееся время показа результата, с
        private double _autoSendRemaining;  // до автоматической отправки, с
        private ClientState _state = ClientState.Idle;
        private double _fixTime;
        private int _maxAttempts;
        private double _autoSendInterval;

        private static readonly Brush MonitorBrush = new SolidBrush(Color.FromArgb(70, 90, 120));
        private static readonly Brush ScreenBrush = new SolidBrush(Color.FromArgb(235, 241, 248));
        private static readonly Brush ScreenAlertBrush = new SolidBrush(Color.FromArgb(250, 205, 205));
        private static readonly Brush ScreenDoneBrush = new SolidBrush(Color.FromArgb(205, 240, 210));
        private static readonly Brush ProgressBrush = new SolidBrush(Color.FromArgb(192, 57, 43));
        private static readonly Brush DoneTextBrush = new SolidBrush(Color.FromArgb(39, 134, 82));
        private static readonly Brush LabelBrush = new SolidBrush(Color.FromArgb(40, 44, 52));
        private static readonly Pen MonitorPen = new Pen(Color.FromArgb(40, 44, 52), 1.4f);
        private static readonly Font LabelFont = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel);
        private static readonly Font HintFont = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel);

        public string Name { get; }

        public string WaypointId { get; }

        public PointF Location { get; }

        /// <summary>Состояние клиента (снаружи только чтение).</summary>
        public ClientState State
        {
            get { return _state; }
        }

        /// <summary>Время исправления ошибки данных, с (должно быть больше нуля).</summary>
        public double FixTime
        {
            get { return _fixTime; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Время исправления ошибки должно быть положительным числом.");
                }
                _fixTime = value;
            }
        }

        /// <summary>Максимальное число попыток выполнения задачи (не меньше 1).</summary>
        public int MaxAttempts
        {
            get { return _maxAttempts; }
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Число попыток должно быть не меньше 1.");
                }
                _maxAttempts = value;
            }
        }

        /// <summary>Автоматическая отправка задач через равные интервалы.</summary>
        public bool AutoSend { get; set; }

        /// <summary>Период автоматической отправки задач, с (больше нуля).</summary>
        public double AutoSendInterval
        {
            get { return _autoSendInterval; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Период автоотправки должен быть положительным числом.");
                }
                _autoSendInterval = value;
            }
        }

        /// <summary>Число отправленных задач (с учётом повторов, только чтение).</summary>
        public int SentCount { get; private set; }

        /// <summary>
        /// Внешнее условие возможности отправки задачи; задаётся главной
        /// программой (ограничение числа задач «в полёте»).
        /// </summary>
        public Func<bool> SendGate { get; set; }

        /// <summary>Событие: клиент отправил задачу.</summary>
        public event EventHandler<PacketEventArgs> PacketSent;

        /// <summary>Событие: клиент начал исправлять ошибку данных.</summary>
        public event EventHandler FixStarted;

        /// <summary>Событие: попытки исчерпаны, задача отменена.</summary>
        public event EventHandler TaskAbandoned;

        /// <summary>Событие: клиент получил результат задачи.</summary>
        public event EventHandler<PacketEventArgs> ResultReceived;

        /// <summary>Создаёт клиента и подписывает его на шаги модельного времени.</summary>
        public Client(string name, string waypointId, PointF location, SimulationClock clock)
        {
            Name = name;
            WaypointId = waypointId;
            Location = location;
            Clock = clock;
            FixTime = 2.0;
            MaxAttempts = 3;
            AutoSendInterval = 6.0;
            clock.Tick += OnTick;
        }

        /// <summary>Создаёт и отправляет новую задачу (попытка 1).</summary>
        public void SendTask()
        {
            if (SendGate != null && !SendGate())
            {
                return;     // главная программа запретила отправку
            }
            Packet packet = new Packet(PacketKind.Task, WaypointId, this, 1);
            SentCount++;
            OnPacketSent(new PacketEventArgs(packet));
        }

        /// <summary>
        /// Принимает пакет: результат задачи или сообщение об ошибке данных.
        /// Ошибки встают в очередь на исправление.
        /// </summary>
        public void Receive(Packet packet)
        {
            if (packet == null)
            {
                return;
            }
            if (packet.Kind == PacketKind.DataError)
            {
                _fixQueue.Enqueue(packet);
                if (_state != ClientState.Fixing)
                {
                    StartFixing();
                }
            }
            else if (packet.Kind == PacketKind.Result)
            {
                _state = ClientState.Done;
                _doneRemaining = DoneTime;
                OnResultReceived(new PacketEventArgs(packet));
            }
        }

        /// <summary>Вызывает событие PacketSent.</summary>
        protected virtual void OnPacketSent(PacketEventArgs e)
        {
            PacketSent?.Invoke(this, e);
        }

        /// <summary>Вызывает событие FixStarted.</summary>
        protected virtual void OnFixStarted(EventArgs e)
        {
            FixStarted?.Invoke(this, e);
        }

        /// <summary>Вызывает событие TaskAbandoned.</summary>
        protected virtual void OnTaskAbandoned(EventArgs e)
        {
            TaskAbandoned?.Invoke(this, e);
        }

        /// <summary>Вызывает событие ResultReceived.</summary>
        protected virtual void OnResultReceived(PacketEventArgs e)
        {
            ResultReceived?.Invoke(this, e);
        }

        // Начинает исправление очередной ошибки данных.
        private void StartFixing()
        {
            _state = ClientState.Fixing;
            _fixRemaining = FixTime;
            OnFixStarted(EventArgs.Empty);
        }

        // Исправление завершено: повторная отправка задачи или отмена.
        private void ResendAfterFix()
        {
            Packet failed = _fixQueue.Dequeue();
            int nextAttempt = failed.Attempt + 1;
            if (nextAttempt > MaxAttempts)
            {
                OnTaskAbandoned(EventArgs.Empty);
                return;
            }
            Packet retry = new Packet(PacketKind.Task, WaypointId, this, nextAttempt);
            SentCount++;
            OnPacketSent(new PacketEventArgs(retry));
        }

        // Шаг модельного времени: исправление, показ результата, автоотправка.
        private void OnTick(object sender, TickEventArgs e)
        {
            if (_state == ClientState.Fixing)
            {
                _fixRemaining -= e.Dt;
                if (_fixRemaining <= 0)
                {
                    _state = ClientState.Idle;
                    ResendAfterFix();
                    if (_state == ClientState.Idle && _fixQueue.Count > 0)
                    {
                        StartFixing();      // в очереди есть ещё ошибки
                    }
                }
            }
            else if (_state == ClientState.Done)
            {
                _doneRemaining -= e.Dt;
                if (_doneRemaining <= 0)
                {
                    _state = ClientState.Idle;
                }
            }

            if (AutoSend)
            {
                _autoSendRemaining -= e.Dt;
                if (_autoSendRemaining <= 0)
                {
                    _autoSendRemaining = AutoSendInterval;
                    SendTask();
                }
            }
        }

        /// <summary>Рисует клиента: монитор, подпись и состояние обмена с сетью.</summary>
        public void Draw(Graphics g)
        {
            RectangleF screen = new RectangleF(Location.X - IconWidth / 2f,
                Location.Y - IconHeight / 2f - 4f, IconWidth, IconHeight);

            Brush screenBrush = ScreenBrush;
            if (_state == ClientState.Fixing) screenBrush = ScreenAlertBrush;
            else if (_state == ClientState.Done) screenBrush = ScreenDoneBrush;

            g.FillRectangle(screenBrush, screen);
            g.DrawRectangle(MonitorPen, screen.X, screen.Y, screen.Width, screen.Height);
            g.FillRectangle(MonitorBrush, Location.X - 5f, Location.Y + IconHeight / 2f - 4f, 10f, 6f);
            g.FillRectangle(MonitorBrush, Location.X - 16f, Location.Y + IconHeight / 2f + 2f, 32f, 4f);

            if (_state == ClientState.Fixing)
            {
                // Полоса прогресса исправления ошибки.
                double fraction = 1.0 - Math.Max(0, _fixRemaining) / FixTime;
                if (fraction < 0) fraction = 0;
                if (fraction > 1) fraction = 1;
                RectangleF bar = new RectangleF(screen.X + 4f, screen.Bottom - 8f, screen.Width - 8f, 5f);
                g.FillRectangle(ScreenBrush, bar);
                g.FillRectangle(ProgressBrush, bar.X, bar.Y, (float)(bar.Width * fraction), bar.Height);
            }

            g.DrawString(Name, LabelFont, LabelBrush, Location.X - 22f, Location.Y + 28f);
            if (_state == ClientState.Fixing)
            {
                g.DrawString("исправляет ошибку…", HintFont, ProgressBrush,
                    Location.X - 50f, Location.Y + 46f);
            }
            else if (_state == ClientState.Done)
            {
                g.DrawString("результат получен", HintFont, DoneTextBrush,
                    Location.X - 50f, Location.Y + 46f);
            }
        }
    }
}
