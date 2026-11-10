using System;
using System.Collections.Generic;
using System.Drawing;

namespace NetworkSim.Model
{
    /// <summary>
    /// Делегат события «узел завершил обработку пакета». Объявлен явно,
    /// а не взят готовым EventHandler, как требует задание.
    /// </summary>
    public delegate void NodeCompletedHandler(object sender, NodeEventArgs e);

    /// <summary>
    /// Делегат события «узел не смог обработать пакет». Объявлен явно,
    /// а не взят готовым EventHandler, как требует задание.
    /// </summary>
    public delegate void NodeFailedHandler(object sender, NodeFailureEventArgs e);

    /// <summary>
    /// Основной класс модели: узел обработки пакетов. Принимает задачи,
    /// обрабатывает их заданное время и сообщает о результате событиями
    /// ProcessingCompleted и ProcessingFailed. Может выйти из строя и быть
    /// отремонтирован. Является базовым классом для специализированных узлов.
    /// </summary>
    public class ProcessingNode : IPacketEndpoint, IDrawable
    {
        private static int _nextIndex = 1;          // общий счётчик номеров узлов

        private readonly Queue<Packet> _queue = new Queue<Packet>();   // ожидающие задачи
        private Packet _currentPacket;              // обрабатываемый (или удерживаемый) пакет
        private double _processingRemaining;        // оставшееся время обработки, с
        private bool _successGuaranteed;            // после ремонта следующая обработка успешна
        private NodeStatus _status = NodeStatus.Idle;
        private double _processingTime;
        private double _failureProbability;

        /// <summary>Часы моделирования, на шаги которых подписан узел.</summary>
        protected readonly SimulationClock Clock;

        /// <summary>Ширина узла на схеме.</summary>
        public const float NodeWidth = 130f;

        /// <summary>Высота узла на схеме.</summary>
        public const float NodeHeight = 64f;

        private const int QueueSlot = 13;           // шаг между пакетами в очереди
        private const int QueueMaxVisible = 5;      // сколько пакетов очереди рисуем
        private const int LongNameLength = 14;      // с какой длины название пишется мельче

        // Кисти, перья и шрифты для рисования (создаются один раз).
        private static readonly Brush IdleBrush = new SolidBrush(Color.FromArgb(222, 231, 241));
        private static readonly Brush ProcessingBrush = new SolidBrush(Color.FromArgb(255, 226, 168));
        private static readonly Brush FailedBrush = new SolidBrush(Color.FromArgb(245, 183, 183));
        private static readonly Pen IdlePen = new Pen(Color.FromArgb(84, 110, 142), 1.6f);
        private static readonly Pen ProcessingPen = new Pen(Color.FromArgb(176, 116, 18), 1.6f);
        private static readonly Pen FailedPen = new Pen(Color.FromArgb(158, 42, 42), 1.8f);
        private static readonly Pen QueuePen = new Pen(Color.FromArgb(150, 158, 168), 1f);
        private static readonly Brush NameBrush = new SolidBrush(Color.FromArgb(40, 44, 52));
        private static readonly Brush QueueBackBrush = new SolidBrush(Color.FromArgb(244, 246, 249));
        private static readonly Brush QueuePacketBrush = new SolidBrush(Color.FromArgb(46, 116, 181));
        private static readonly Brush ProgressBackBrush = new SolidBrush(Color.White);
        private static readonly Brush ProgressBrush = new SolidBrush(Color.FromArgb(240, 156, 20));
        private static readonly Brush QueueBrush = new SolidBrush(Color.FromArgb(110, 116, 126));
        private static readonly Brush CrossBrush = new SolidBrush(Color.FromArgb(158, 42, 42));
        private static readonly Brush IconBrush = new SolidBrush(Color.FromArgb(70, 90, 120));

        // Шрифты заданы в пикселях: их размер не зависит от масштаба экрана.
        private static readonly Font NameFont = new Font("Segoe UI", 12f, FontStyle.Regular, GraphicsUnit.Pixel);
        private static readonly Font LongNameFont = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel);
        private static readonly Font QueueFont = new Font("Segoe UI", 10f, FontStyle.Regular, GraphicsUnit.Pixel);
        private static readonly Font CrossFont = new Font("Arial", 22f, FontStyle.Bold, GraphicsUnit.Pixel);

        // Выравнивание названия по центру рамки.
        private static readonly StringFormat NameFormat = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Near
        };

        public int Index { get; }

        public string Name { get; }

        public string WaypointId { get; }

        public PointF Location { get; }

        /// <summary>Состояние узла: простой / обработка / отказ (снаружи только чтение).</summary>
        public NodeStatus Status
        {
            get { return _status; }
            private set
            {
                if (_status == value)
                {
                    return;
                }
                NodeStatus old = _status;
                _status = value;
                OnStatusChanged(new ValueChangedEventArgs<NodeStatus>(old, value));
            }
        }

        /// <summary>Время обработки одного пакета, с (должно быть больше нуля).</summary>
        public double ProcessingTime
        {
            get { return _processingTime; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Время обработки должно быть положительным числом.");
                }
                _processingTime = value;
            }
        }

        /// <summary>Вероятность сбоя при обработке пакета, значение от 0 до 1.</summary>
        public double FailureProbability
        {
            get { return _failureProbability; }
            set
            {
                if (value < 0 || value > 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Вероятность сбоя должна быть в диапазоне от 0 до 1.");
                }
                _failureProbability = value;
            }
        }

        /// <summary>Число пакетов, ожидающих обработки в очереди (только чтение).</summary>
        public int QueueLength
        {
            get { return _queue.Count; }
        }

        // Хранилище подписчиков для событий с явными add/remove.
        private NodeCompletedHandler _processingCompleted;
        private NodeFailedHandler _processingFailed;

        /// <summary>
        /// Событие: обработка пакета успешно завершена. Объявлено полностью,
        /// с явными методами add и remove (а не «по умолчанию»).
        /// </summary>
        public event NodeCompletedHandler ProcessingCompleted
        {
            add { _processingCompleted += value; }
            remove { _processingCompleted -= value; }
        }

        /// <summary>
        /// Событие: обработка завершилась ошибкой данных или отказом узла.
        /// Объявлено полностью, с явными методами add и remove.
        /// </summary>
        public event NodeFailedHandler ProcessingFailed
        {
            add { _processingFailed += value; }
            remove { _processingFailed -= value; }
        }

        /// <summary>Событие: изменилось состояние узла.</summary>
        public event EventHandler<ValueChangedEventArgs<NodeStatus>> StatusChanged;

        /// <summary>Создаёт узел обработки с заданными параметрами.</summary>
        public ProcessingNode(string name, string waypointId, PointF location,
                              SimulationClock clock, double processingTime = 1.5,
                              double failureProbability = 0.0)
        {
            Index = _nextIndex++;
            Name = name;
            WaypointId = waypointId;
            Location = location;
            Clock = clock;
            ProcessingTime = processingTime;        // через свойство — с проверкой
            FailureProbability = failureProbability;
            clock.Tick += OnTick;
        }

        /// <summary>
        /// Принимает пакет: задача ставится в очередь, команда ремонта
        /// чинит отказавший узел.
        /// </summary>
        public void Receive(Packet packet)
        {
            if (packet == null)
            {
                return;
            }
            if (packet.Kind == PacketKind.Task)
            {
                _queue.Enqueue(packet);
                TryStartNext();
            }
            else if (packet.Kind == PacketKind.Fix)
            {
                Repair();
            }
        }

        /// <summary>
        /// Ремонтирует отказавший узел: удерживаемый пакет продолжает
        /// обрабатываться, следующая обработка гарантированно успешна.
        /// </summary>
        public void Repair()
        {
            if (Status != NodeStatus.Failed)
            {
                return;
            }
            _successGuaranteed = true;
            _processingRemaining = ProcessingTime;
            Status = NodeStatus.Processing;
        }

        /// <summary>
        /// Определяет исход обработки пакета. Базовое поведение: с вероятностью
        /// EffectiveProbability — ошибка данных, иначе успех. Переопределяется
        /// в классах-наследниках.
        /// </summary>
        protected virtual FailureKind Evaluate(Packet packet)
        {
            return Random.Shared.NextDouble() < EffectiveProbability(packet)
                ? FailureKind.InvalidData
                : FailureKind.None;
        }

        /// <summary>
        /// Действующая вероятность сбоя: базовая, уменьшенная вдвое за каждую
        /// повторную попытку доставки задачи.
        /// </summary>
        protected double EffectiveProbability(Packet packet)
        {
            double p = FailureProbability;
            int attempt = packet != null ? packet.Attempt : 1;
            while (attempt > 1)
            {
                p /= 2.0;
                attempt--;
            }
            return p;
        }

        /// <summary>Вызывает событие ProcessingCompleted.</summary>
        protected virtual void OnProcessingCompleted(NodeEventArgs e)
        {
            _processingCompleted?.Invoke(this, e);
        }

        /// <summary>Вызывает событие ProcessingFailed.</summary>
        protected virtual void OnProcessingFailed(NodeFailureEventArgs e)
        {
            _processingFailed?.Invoke(this, e);
        }

        /// <summary>Вызывает событие StatusChanged.</summary>
        protected virtual void OnStatusChanged(ValueChangedEventArgs<NodeStatus> e)
        {
            StatusChanged?.Invoke(this, e);
        }

        /// <summary>Рисует узел: корпус, очередь и пиктограмму.</summary>
        public virtual void Draw(Graphics g)
        {
            DrawBody(g);
            if (Status != NodeStatus.Failed)
            {
                DrawSwitchIcon(g);
            }
        }

        /// <summary>Рисует общую для всех узлов часть: корпус и очередь пакетов.</summary>
        protected void DrawBody(Graphics g)
        {
            DrawFrame(g);
            DrawQueue(g);
        }

        /// <summary>
        /// Рисует корпус узла: заливку и рамку по состоянию, название,
        /// полосу прогресса обработки и знак отказа.
        /// </summary>
        protected void DrawFrame(Graphics g)
        {
            RectangleF box = new RectangleF(
                Location.X - NodeWidth / 2f, Location.Y - NodeHeight / 2f,
                NodeWidth, NodeHeight);

            Brush fill;
            Pen border;
            if (Status == NodeStatus.Processing)
            {
                fill = ProcessingBrush;
                border = ProcessingPen;
            }
            else if (Status == NodeStatus.Failed)
            {
                fill = FailedBrush;
                border = FailedPen;
            }
            else
            {
                fill = IdleBrush;
                border = IdlePen;
            }

            g.FillRectangle(fill, box);
            g.DrawRectangle(border, box.X, box.Y, box.Width, box.Height);

            // Длинные названия пишутся мельче, чтобы поместиться в рамку.
            Font nameFont = Name.Length > LongNameLength ? LongNameFont : NameFont;
            g.DrawString(Name, nameFont, NameBrush,
                new RectangleF(box.X, box.Y + 2f, box.Width, 20f), NameFormat);

            if (Status == NodeStatus.Processing)
            {
                // Полоса прогресса обработки текущего пакета.
                double fraction = 1.0 - Math.Max(0, _processingRemaining) / ProcessingTime;
                if (fraction < 0) fraction = 0;
                if (fraction > 1) fraction = 1;
                RectangleF bar = new RectangleF(box.X + 8f, box.Bottom - 11f, box.Width - 16f, 5f);
                g.FillRectangle(ProgressBackBrush, bar);
                g.FillRectangle(ProgressBrush, bar.X, bar.Y, (float)(bar.Width * fraction), bar.Height);
            }

            if (Status == NodeStatus.Failed)
            {
                // Знак отказа поверх корпуса.
                SizeF crossSize = g.MeasureString("×", CrossFont);
                g.DrawString("×", CrossFont, CrossBrush,
                    Location.X - crossSize.Width / 2f,
                    Location.Y - crossSize.Height / 2f - 2f);
            }
        }

        // Рисует очередь пакетов под узлом: они «стоят друг за другом» и ждут обработки.
        private void DrawQueue(Graphics g)
        {
            int count = _queue.Count;
            if (count == 0)
            {
                return;
            }
            int shown = Math.Min(count, QueueMaxVisible);
            float width = 8f + shown * QueueSlot + (count > shown ? 18f : 0f);
            RectangleF tray = new RectangleF(Location.X - width / 2f,
                Location.Y + NodeHeight / 2f + 5f, width, 18f);

            g.FillRectangle(QueueBackBrush, tray);
            g.DrawRectangle(Status == NodeStatus.Failed ? FailedPen : QueuePen,
                tray.X, tray.Y, tray.Width, tray.Height);

            for (int i = 0; i < shown; i++)
            {
                float cx = tray.X + 8f + i * QueueSlot;
                g.FillEllipse(QueuePacketBrush, cx - 4f, tray.Y + 5f, 8f, 8f);
            }
            if (count > shown)
            {
                g.DrawString("+" + (count - shown), QueueFont, QueueBrush,
                    tray.X + 4f + shown * QueueSlot, tray.Y + 2f);
            }
        }

        // Запускает обработку следующего пакета из очереди, если узел свободен.
        private void TryStartNext()
        {
            if (Status != NodeStatus.Idle || _queue.Count == 0)
            {
                return;
            }
            _currentPacket = _queue.Dequeue();
            _processingRemaining = ProcessingTime;
            Status = NodeStatus.Processing;
        }

        // Шаг модельного времени: продвижение обработки текущего пакета.
        private void OnTick(object sender, TickEventArgs e)
        {
            if (Status != NodeStatus.Processing)
            {
                return;
            }
            _processingRemaining -= e.Dt;
            if (_processingRemaining > 0)
            {
                return;
            }
            CompleteCurrent();
        }

        // Завершает обработку пакета по автомату состояний узла.
        private void CompleteCurrent()
        {
            FailureKind verdict = _successGuaranteed ? FailureKind.None : Evaluate(_currentPacket);
            _successGuaranteed = false;

            if (verdict == FailureKind.NodeOutage)
            {
                // Узел выходит из строя; пакет удерживается до ремонта.
                Status = NodeStatus.Failed;
                OnProcessingFailed(new NodeFailureEventArgs(this, _currentPacket, verdict));
                return;
            }

            Packet completed = _currentPacket;
            _currentPacket = null;
            Status = NodeStatus.Idle;
            if (verdict == FailureKind.None)
            {
                OnProcessingCompleted(new NodeEventArgs(this, completed));
            }
            else
            {
                OnProcessingFailed(new NodeFailureEventArgs(this, completed, verdict));
            }
            TryStartNext();
        }

        // Пиктограмма базового узла (коммутатор): две встречные стрелки.
        private void DrawSwitchIcon(Graphics g)
        {
            float cx = Location.X;
            float cy = Location.Y + 4f;
            using (Pen p = new Pen(IconBrush, 2f))
            {
                g.DrawLine(p, cx - 22f, cy - 6f, cx + 14f, cy - 6f);
                g.DrawLine(p, cx - 14f, cy + 8f, cx + 22f, cy + 8f);
            }
            PointF[] headRight =
            {
                new PointF(cx + 14f, cy - 11f),
                new PointF(cx + 22f, cy - 6f),
                new PointF(cx + 14f, cy - 1f)
            };
            PointF[] headLeft =
            {
                new PointF(cx - 14f, cy + 3f),
                new PointF(cx - 22f, cy + 8f),
                new PointF(cx - 14f, cy + 13f)
            };
            g.FillPolygon(IconBrush, headRight);
            g.FillPolygon(IconBrush, headLeft);
        }
    }
}
