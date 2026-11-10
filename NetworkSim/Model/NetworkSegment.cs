using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace NetworkSim.Model
{
    /// <summary>
    /// Базовый класс «окружающей среды» модели — сегмент сети. Владеет графом
    /// дорожек, решает, кому адресовать пакет (ResolveDestination), строит
    /// маршрут и перемещает пакеты; пакеты, адресованные за пределы сегмента,
    /// передаются соседнему сегменту через шлюз. Внешний вид сегмента зависит
    /// от его состояния (State).
    /// </summary>
    public class NetworkSegment : IDrawable
    {
        /// <summary>Часы моделирования, на шаги которых подписан сегмент.</summary>
        protected readonly SimulationClock Clock;

        /// <summary>Граф дорожек сегмента.</summary>
        protected readonly TrackGraph Graph = new TrackGraph();

        private const string GatewayWaypointId = "gw";      // шлюз в графе дорожек

        private readonly List<IPacketEndpoint> _endpoints = new List<IPacketEndpoint>();
        private readonly List<Packet> _inFlight = new List<Packet>();   // пакеты в пути
        private NetworkSegment _neighbor;                   // соседний сегмент
        private PointF _neighborGateway;                    // координата шлюза соседа
        private SegmentState _state = SegmentState.Normal;
        private double _holdRemaining;                      // сколько держится временное состояние, с

        private static readonly Brush TitleBrush = new SolidBrush(Color.FromArgb(60, 64, 72));
        private static readonly Font TitleFont =
            new Font("Segoe UI", 15f, FontStyle.Bold, GraphicsUnit.Pixel);

        public string Name { get; }

        public RectangleF Bounds { get; }

        /// <summary>Идентификатор вершины-шлюза («выхода» из сегмента).</summary>
        public string GatewayId
        {
            get { return GatewayWaypointId; }
        }

        /// <summary>Состояние сегмента, определяющее его внешний вид.</summary>
        public SegmentState State
        {
            get { return _state; }
        }

        /// <summary>Событие: пакет дошёл до шлюза и адресован за пределы сегмента.</summary>
        public event EventHandler<PacketEventArgs> PacketLeaving;

        /// <summary>Событие: пакет доставлен локальному адресату сегмента.</summary>
        public event EventHandler<PacketEventArgs> PacketDelivered;

        /// <summary>Событие: пакет отброшен (адресат или маршрут не найдены).</summary>
        public event EventHandler<PacketEventArgs> PacketDropped;

        /// <summary>Событие: изменилось состояние сегмента.</summary>
        public event EventHandler<ValueChangedEventArgs<SegmentState>> StateChanged;

        /// <summary>Создаёт сегмент: регистрирует шлюз и подписывается на время.</summary>
        public NetworkSegment(string name, RectangleF bounds, PointF gatewayLocation,
                              SimulationClock clock)
        {
            Name = name;
            Bounds = bounds;
            Clock = clock;
            Graph.AddWaypoint(GatewayWaypointId, gatewayLocation);
            clock.Tick += OnTick;
        }

        /// <summary>Регистрирует локального адресата пакетов в сегменте.</summary>
        public void AddEndpoint(IPacketEndpoint endpoint)
        {
            if (endpoint == null || _endpoints.Contains(endpoint))
            {
                return;
            }
            _endpoints.Add(endpoint);
            Graph.AddWaypoint(endpoint.WaypointId, endpoint.Location);
        }

        /// <summary>Добавляет вершину графа дорожек сегмента.</summary>
        public void AddWaypoint(string id, PointF point)
        {
            Graph.AddWaypoint(id, point);
        }

        /// <summary>Добавляет дорожку между двумя вершинами сегмента.</summary>
        public void AddLane(string idA, string idB)
        {
            Graph.AddLane(idA, idB);
        }

        /// <summary>
        /// Связывает два сегмента линией связи через шлюзы: пакеты, покидающие
        /// один сегмент, передаются на приём другому.
        /// </summary>
        public void Connect(NetworkSegment other)
        {
            if (other == null || _neighbor == other)
            {
                return;
            }
            _neighbor = other;
            _neighborGateway = other.Graph.GetPoint(other.GatewayId);
            other._neighbor = this;
            other._neighborGateway = Graph.GetPoint(GatewayId);
            PacketLeaving += (s, e) => other.Accept(e.Packet);
            other.PacketLeaving += (s, e) => Accept(e.Packet);
        }

        /// <summary>
        /// Принимает новый пакет, рождённый в этом сегменте (от актора или узла):
        /// определяет адресата, строит маршрут и запускает пакет.
        /// </summary>
        public void Submit(Packet packet)
        {
            if (packet == null)
            {
                return;
            }
            IPacketEndpoint dest = packet.Destination ?? ResolveDestination(packet);
            packet.Destination = dest;

            List<PointF> route;
            if (dest != null && IsLocal(dest))
            {
                route = Graph.FindRoute(packet.SourceWaypointId, dest.WaypointId);
            }
            else
            {
                // Адресат вне сегмента: едем к шлюзу, далее по линии связи.
                route = Graph.FindRoute(packet.SourceWaypointId, GatewayId);
                if (route != null && _neighbor != null)
                {
                    route.Add(_neighborGateway);
                }
            }
            if (route == null)
            {
                OnPacketDropped(new PacketEventArgs(packet));
                return;
            }
            Launch(packet, route);
        }

        /// <summary>Принимает пакет, пришедший через шлюз от соседнего сегмента.</summary>
        public void Accept(Packet packet)
        {
            IPacketEndpoint dest = packet.Destination ?? ResolveDestination(packet);
            packet.Readdress(dest, GatewayId);      // путь продолжается от шлюза
            if (dest == null || !IsLocal(dest))
            {
                OnPacketDropped(new PacketEventArgs(packet));
                return;
            }
            List<PointF> route = Graph.FindRoute(packet.SourceWaypointId, dest.WaypointId);
            if (route == null)
            {
                OnPacketDropped(new PacketEventArgs(packet));
                return;
            }
            Launch(packet, route);
        }

        /// <summary>
        /// Определяет локального адресата пакета по его содержимому;
        /// null означает «адресат вне сегмента». Переопределяется в наследниках.
        /// </summary>
        protected virtual IPacketEndpoint ResolveDestination(Packet packet)
        {
            return packet != null ? packet.Destination : null;
        }

        /// <summary>
        /// Устанавливает состояние сегмента. При holdSeconds &gt; 0 состояние
        /// временное: по истечении этого времени сегмент вернётся к обычному.
        /// </summary>
        protected void SetState(SegmentState state, double holdSeconds)
        {
            if (holdSeconds > 0)
            {
                _holdRemaining = holdSeconds;
            }
            if (_state != state)
            {
                SegmentState old = _state;
                _state = state;
                OnStateChanged(new ValueChangedEventArgs<SegmentState>(old, state));
            }
        }

        /// <summary>Пересчитывает «постоянное» состояние сегмента.</summary>
        protected void RefreshState()
        {
            SegmentState wanted = ComputeDerivedState();
            if (wanted != SegmentState.Normal)
            {
                _holdRemaining = 0;     // авария и обработка важнее временного «успеха»
            }
            if (_holdRemaining > 0)
            {
                return;
            }
            SetState(wanted, 0);
        }

        /// <summary>Вычисляет «постоянное» состояние по внутренним объектам сегмента.</summary>
        protected virtual SegmentState ComputeDerivedState()
        {
            return SegmentState.Normal;
        }

        /// <summary>Вызывает событие PacketLeaving.</summary>
        protected virtual void OnPacketLeaving(PacketEventArgs e)
        {
            PacketLeaving?.Invoke(this, e);
        }

        /// <summary>Вызывает событие PacketDelivered.</summary>
        protected virtual void OnPacketDelivered(PacketEventArgs e)
        {
            PacketDelivered?.Invoke(this, e);
        }

        /// <summary>Вызывает событие PacketDropped.</summary>
        protected virtual void OnPacketDropped(PacketEventArgs e)
        {
            PacketDropped?.Invoke(this, e);
        }

        /// <summary>Вызывает событие StateChanged.</summary>
        protected virtual void OnStateChanged(ValueChangedEventArgs<SegmentState> e)
        {
            StateChanged?.Invoke(this, e);
        }

        // Является ли объект локальным адресатом этого сегмента.
        private bool IsLocal(IPacketEndpoint endpoint)
        {
            return _endpoints.Contains(endpoint);
        }

        // Запускает пакет по маршруту: подписывается на его прибытие.
        private void Launch(Packet packet, List<PointF> route)
        {
            packet.Arrived += OnPacketArrived;
            _inFlight.Add(packet);
            packet.StartRoute(route);
        }

        // Пакет дошёл до конца маршрута: доставить адресату или передать соседу.
        private void OnPacketArrived(object sender, PacketEventArgs e)
        {
            Packet packet = e.Packet;
            packet.Arrived -= OnPacketArrived;
            _inFlight.Remove(packet);

            IPacketEndpoint dest = packet.Destination;
            if (dest != null && IsLocal(dest))
            {
                // Сначала фиксируем доставку, потом адресат её обрабатывает.
                OnPacketDelivered(new PacketEventArgs(packet));
                dest.Receive(packet);
            }
            else if (_neighbor != null)
            {
                OnPacketLeaving(new PacketEventArgs(packet));
            }
            else
            {
                OnPacketDropped(new PacketEventArgs(packet));
            }
        }

        // Шаг модельного времени: движение пакетов и отсчёт временного состояния.
        private void OnTick(object sender, TickEventArgs e)
        {
            if (_holdRemaining > 0)
            {
                _holdRemaining -= e.Dt;
                if (_holdRemaining <= 0)
                {
                    _holdRemaining = 0;
                    RefreshState();
                }
            }
            if (_inFlight.Count > 0)
            {
                // Обход по копии списка: обработчик Arrived изменяет список.
                foreach (Packet packet in new List<Packet>(_inFlight))
                {
                    packet.Advance(e.Dt);
                }
            }
        }

        /// <summary>Рисует сегмент: фон по состоянию, рамку, название и дорожки.</summary>
        public virtual void Draw(Graphics g)
        {
            Color fill;
            Color border;
            switch (_state)
            {
                case SegmentState.Busy:
                    fill = Color.FromArgb(255, 247, 214);
                    border = Color.FromArgb(214, 184, 96);
                    break;
                case SegmentState.Alert:
                    fill = Color.FromArgb(253, 227, 227);
                    border = Color.FromArgb(200, 70, 70);
                    break;
                case SegmentState.Success:
                    fill = Color.FromArgb(227, 247, 227);
                    border = Color.FromArgb(96, 175, 96);
                    break;
                default:
                    fill = Color.FromArgb(246, 247, 249);
                    border = Color.FromArgb(150, 158, 168);
                    break;
            }
            using (SolidBrush b = new SolidBrush(fill))
            {
                g.FillRectangle(b, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
            }
            using (Pen p = new Pen(border, 2f))
            {
                g.DrawRectangle(p, Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height);
            }
            g.DrawString(Name, TitleFont, TitleBrush, Bounds.X + 10f, Bounds.Y + 6f);
            Graph.Draw(g);
        }

        /// <summary>Рисует пакеты, движущиеся по дорожкам сегмента.</summary>
        public void DrawPackets(Graphics g)
        {
            foreach (Packet packet in _inFlight)
            {
                packet.Draw(g);
            }
        }
    }
}
