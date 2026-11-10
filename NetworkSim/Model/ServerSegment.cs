using System;
using System.Collections.Generic;
using System.Drawing;

namespace NetworkSim.Model
{
    /// <summary>
    /// Серверный сегмент сети — производный от NetworkSegment. Объединяет
    /// упорядоченную цепочку узлов обработки и администратора; направляет
    /// задачи на первый узел, передаёт пакеты между узлами по цепочке,
    /// уведомления об отказах — администратору, команды ремонта — узлам.
    /// Переопределяет ResolveDestination и ComputeDerivedState.
    /// </summary>
    public class ServerSegment : NetworkSegment
    {
        private readonly List<ProcessingNode> _nodes = new List<ProcessingNode>();
        private Administrator _administrator;

        /// <summary>Создаёт серверный сегмент и строит его дорожки.</summary>
        public ServerSegment(RectangleF bounds, PointF gatewayLocation, SimulationClock clock)
            : base("Серверный сегмент", bounds, gatewayLocation, clock)
        {
            // Магистраль: шлюз → первый узел → … → развилка → последние узлы;
            // от развилки вниз идёт ветка к администратору.
            AddWaypoint("j", Layout.ServerJunction);
            AddLane("gw", "n0");
            AddLane("n0", "n1");
            AddLane("n1", "j");
            AddLane("j", "n2");
            AddLane("n2", "n3");
            AddLane("j", "adm");
        }

        /// <summary>Цепочка узлов обработки (только чтение).</summary>
        public IReadOnlyList<ProcessingNode> Nodes
        {
            get { return _nodes; }
        }

        /// <summary>
        /// Добавляет узел в цепочку и настраивает реакцию сегмента на его
        /// события: завершение, сбой и смену состояния.
        /// </summary>
        public void AddNode(ProcessingNode node)
        {
            if (node == null || _nodes.Contains(node))
            {
                return;
            }
            _nodes.Add(node);
            AddWaypoint(node.WaypointId, node.Location);
            AddEndpoint(node);
            SubscribeToNode(node);
            node.StatusChanged += OnNodeStatusChanged;
        }

        /// <summary>
        /// Подписывает сегмент на события узла. Обе подписки записаны явно,
        /// через new ...Handler, — так видно, что используются собственные
        /// делегаты NodeCompletedHandler и NodeFailedHandler.
        /// </summary>
        private void SubscribeToNode(ProcessingNode node)
        {
            node.ProcessingCompleted += new NodeCompletedHandler(OnNodeCompleted);
            node.ProcessingFailed += new NodeFailedHandler(OnNodeFailed);
        }

        /// <summary>Подключает администратора к сегменту.</summary>
        public void SetAdministrator(Administrator admin)
        {
            if (admin == null)
            {
                return;
            }
            _administrator = admin;
            AddWaypoint(admin.WaypointId, admin.Location);
            AddEndpoint(admin);
            // Отправленная администратором команда ремонта запускается в сегменте.
            admin.PacketSent += (s, e) => Submit(e.Packet);
        }

        /// <summary>
        /// Определяет адресата пакета: задача — первому узлу цепочки,
        /// уведомление об отказе — администратору, команда ремонта — узлу;
        /// результат уходит в клиентский сегмент.
        /// </summary>
        protected override IPacketEndpoint ResolveDestination(Packet packet)
        {
            switch (packet.Kind)
            {
                case PacketKind.Task:
                    return _nodes.Count > 0 ? _nodes[0] : null;
                case PacketKind.NodeFailure:
                    return _administrator;
                case PacketKind.Fix:
                    return packet.Subject;
                default:
                    return null;        // адресат вне сегмента
            }
        }

        /// <summary>Вычисляет состояние сегмента по состояниям узлов.</summary>
        protected override SegmentState ComputeDerivedState()
        {
            bool anyFailed = false;
            bool anyProcessing = false;
            foreach (ProcessingNode node in _nodes)
            {
                if (node.Status == NodeStatus.Failed)
                {
                    anyFailed = true;
                }
                else if (node.Status == NodeStatus.Processing)
                {
                    anyProcessing = true;
                }
            }
            if (anyFailed)
            {
                return SegmentState.Alert;
            }
            return anyProcessing ? SegmentState.Busy : SegmentState.Normal;
        }

        // Узел завершил обработку: пакет уходит следующему узлу, а после
        // последнего узла формируется результат для клиента.
        private void OnNodeCompleted(object sender, NodeEventArgs e)
        {
            int index = _nodes.IndexOf(e.Node);
            if (index >= 0 && index < _nodes.Count - 1)
            {
                e.Packet.Readdress(_nodes[index + 1], e.Node.WaypointId);
                Submit(e.Packet);
            }
            else
            {
                Packet result = new Packet(PacketKind.Result,
                    e.Node.WaypointId, e.Packet.Owner, e.Packet.Attempt);
                Submit(result);
                SetState(SegmentState.Success, 1.5);
            }
        }

        // Узел сообщил о сбое: ошибка данных возвращается клиенту,
        // отказ узла — уведомление администратору.
        private void OnNodeFailed(object sender, NodeFailureEventArgs e)
        {
            if (e.Kind == FailureKind.InvalidData)
            {
                Packet error = new Packet(PacketKind.DataError,
                    e.Node.WaypointId, e.Packet.Owner, e.Packet.Attempt);
                error.Destination = e.Packet.Owner;     // адресат вне сегмента
                Submit(error);
            }
            else if (e.Kind == FailureKind.NodeOutage)
            {
                Packet alert = new Packet(PacketKind.NodeFailure,
                    e.Node.WaypointId, e.Packet.Owner, e.Packet.Attempt, e.Node);
                Submit(alert);      // адресата определит ResolveDestination
            }
        }

        // Состояние узла изменилось — пересчитать состояние сегмента.
        private void OnNodeStatusChanged(object sender, ValueChangedEventArgs<NodeStatus> e)
        {
            RefreshState();
        }
    }
}
