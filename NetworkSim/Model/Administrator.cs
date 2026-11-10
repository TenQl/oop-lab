using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NetworkSim.Model
{
    /// <summary>
    /// Администратор — специалист, обслуживающий серверный сегмент. Получает
    /// уведомления об отказах узлов, «подключается по SSH» к отказавшему узлу
    /// и по истечении времени ремонта отправляет ему команду ремонта (Fix).
    /// </summary>
    public class Administrator : IPacketEndpoint, IDrawable
    {
        protected readonly SimulationClock Clock;

        private readonly Queue<Packet> _requests = new Queue<Packet>(); // уведомления об отказах
        private double _repairRemaining;    // оставшееся время ремонта, с
        private AdminState _state = AdminState.Idle;
        private double _repairTime;

        private static readonly Brush PersonBrush = new SolidBrush(Color.FromArgb(52, 73, 110));
        private static readonly Brush LabelBrush = new SolidBrush(Color.FromArgb(40, 44, 52));
        private static readonly Brush HintBrush = new SolidBrush(Color.FromArgb(46, 116, 181));
        private static readonly Font LabelFont = new Font("Segoe UI", 12f, FontStyle.Bold, GraphicsUnit.Pixel);
        private static readonly Font HintFont = new Font("Segoe UI", 11f, FontStyle.Regular, GraphicsUnit.Pixel);

        public string Name { get; }

        public string WaypointId { get; }

        public PointF Location { get; }

        /// <summary>Состояние администратора (снаружи только чтение).</summary>
        public AdminState State
        {
            get { return _state; }
        }

        /// <summary>Время ремонта одного узла, с (должно быть больше нуля).</summary>
        public double RepairTime
        {
            get { return _repairTime; }
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value),
                        "Время ремонта должно быть положительным числом.");
                }
                _repairTime = value;
            }
        }

        /// <summary>Число выполненных ремонтов (только чтение).</summary>
        public int RepairsDone { get; private set; }

        /// <summary>Узел, к которому администратор подключён в данный момент (или null).</summary>
        public ProcessingNode CurrentTarget { get; private set; }

        /// <summary>Событие: администратор отправил команду ремонта узлу.</summary>
        public event EventHandler<PacketEventArgs> PacketSent;

        /// <summary>Событие: администратор начал ремонт (подключился по SSH).</summary>
        public event EventHandler RepairStarted;

        /// <summary>Создаёт администратора и подписывает его на шаги времени.</summary>
        public Administrator(string name, string waypointId, PointF location, SimulationClock clock)
        {
            Name = name;
            WaypointId = waypointId;
            Location = location;
            Clock = clock;
            RepairTime = 3.0;
            clock.Tick += OnTick;
        }

        /// <summary>Принимает пакет: ожидается уведомление об отказе узла.</summary>
        public void Receive(Packet packet)
        {
            if (packet == null || packet.Kind != PacketKind.NodeFailure)
            {
                return;
            }
            _requests.Enqueue(packet);
            if (_state == AdminState.Idle)
            {
                StartRepair();
            }
        }

        /// <summary>Вызывает событие PacketSent.</summary>
        protected virtual void OnPacketSent(PacketEventArgs e)
        {
            PacketSent?.Invoke(this, e);
        }

        /// <summary>Вызывает событие RepairStarted.</summary>
        protected virtual void OnRepairStarted(EventArgs e)
        {
            RepairStarted?.Invoke(this, e);
        }

        // Начинает ремонт очередного отказавшего узла.
        private void StartRepair()
        {
            Packet request = _requests.Dequeue();
            _state = AdminState.Repairing;
            CurrentTarget = request.Subject;
            _repairRemaining = RepairTime;
            OnRepairStarted(EventArgs.Empty);
        }

        // Ремонт завершён: отправить узлу команду ремонта (пакет Fix).
        private void FinishRepair()
        {
            ProcessingNode target = CurrentTarget;
            _state = AdminState.Idle;
            CurrentTarget = null;
            RepairsDone++;
            if (target != null)
            {
                Packet fix = new Packet(PacketKind.Fix, WaypointId, this, 1, target);
                fix.Destination = target;
                OnPacketSent(new PacketEventArgs(fix));
            }
            if (_requests.Count > 0)
            {
                StartRepair();      // в очереди есть ещё отказы
            }
        }

        // Шаг модельного времени: отсчёт времени ремонта.
        private void OnTick(object sender, TickEventArgs e)
        {
            if (_state != AdminState.Repairing)
            {
                return;
            }
            _repairRemaining -= e.Dt;
            if (_repairRemaining <= 0)
            {
                FinishRepair();
            }
        }

        /// <summary>
        /// Рисует администратора; во время ремонта — пунктирную анимированную
        /// линию «SSH» к ремонтируемому узлу.
        /// </summary>
        public void Draw(Graphics g)
        {
            if (_state == AdminState.Repairing && CurrentTarget != null)
            {
                PointF target = CurrentTarget.Location;
                using (Pen p = new Pen(HintBrush, 1.8f))
                {
                    p.DashStyle = DashStyle.Dash;
                    p.DashOffset = -(float)(Clock.Elapsed * 12.0);   // «бегущий» пунктир
                    g.DrawLine(p, Location.X, Location.Y - 14f,
                        target.X, target.Y + ProcessingNode.NodeHeight / 2f);
                }
                g.DrawString("SSH", HintFont, HintBrush,
                    (Location.X + target.X) / 2f + 6f,
                    (Location.Y - 14f + target.Y + ProcessingNode.NodeHeight / 2f) / 2f);
            }

            // Фигура человека: голова и плечи.
            g.FillEllipse(PersonBrush, Location.X - 8f, Location.Y - 24f, 16f, 16f);
            g.FillEllipse(PersonBrush, Location.X - 18f, Location.Y - 4f, 36f, 26f);
            g.DrawString(Name, LabelFont, LabelBrush, Location.X - 50f, Location.Y + 27f);
            if (_state == AdminState.Repairing)
            {
                g.DrawString("ремонтирует…", HintFont, HintBrush,
                    Location.X - 32f, Location.Y + 45f);
            }
        }
    }
}
