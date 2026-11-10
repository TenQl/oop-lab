using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NetworkSim.Model
{
    /// <summary>
    /// Клиентский сегмент сети — производный от NetworkSegment. Объединяет
    /// клиента и пользователя; задачи направляет наружу (в серверный сегмент),
    /// ошибки данных и результаты возвращает клиенту. Пользователь стоит рядом,
    /// видит переданный ему результат и радуется.
    /// Переопределяет ResolveDestination и Draw.
    /// </summary>
    public class ClientSegment : NetworkSegment
    {
        private Client _client;
        private EndUser _endUser;

        private static readonly Pen LinkPen = new Pen(Color.FromArgb(39, 134, 82), 1.8f);

        /// <summary>Создаёт клиентский сегмент и строит его дорожки.</summary>
        public ClientSegment(RectangleF bounds, PointF gatewayLocation, SimulationClock clock)
            : base("Клиентский сегмент", bounds, gatewayLocation, clock)
        {
            // Магистраль сегмента: шлюз — середина — к клиенту.
            AddWaypoint("ct", Layout.ClientTop);
            AddWaypoint("cm", Layout.ClientMiddle);
            AddLane("gw", "cm");
            AddLane("cm", "ct");
        }

        /// <summary>Подключает клиента к сегменту и настраивает реакцию на его события.</summary>
        public void SetClient(Client client)
        {
            if (client == null)
            {
                return;
            }
            _client = client;
            AddWaypoint(client.WaypointId, client.Location);
            AddLane(client.WaypointId, "ct");
            AddEndpoint(client);
            client.PacketSent += OnClientPacketSent;
            client.FixStarted += OnClientFixStarted;
            client.ResultReceived += OnClientResultReceived;
        }

        /// <summary>Подключает пользователя: он получает результат от клиента.</summary>
        public void SetEndUser(EndUser user)
        {
            if (user == null)
            {
                return;
            }
            _endUser = user;
            user.ResultReceived += OnResultReceived;
        }

        /// <summary>
        /// Определяет адресата пакета: и результат, и ошибка данных возвращаются
        /// клиенту; задачи уходят в серверный сегмент.
        /// </summary>
        protected override IPacketEndpoint ResolveDestination(Packet packet)
        {
            if (packet.Kind == PacketKind.Result || packet.Kind == PacketKind.DataError)
            {
                return _client;
            }
            return null;        // адресат вне сегмента
        }

        // Клиент отправил задачу — запускаем её по дорожкам сегмента.
        private void OnClientPacketSent(object sender, PacketEventArgs e)
        {
            Submit(e.Packet);
        }

        // Клиент начал исправлять ошибку — сегмент переходит в аварийное состояние.
        private void OnClientFixStarted(object sender, EventArgs e)
        {
            SetState(SegmentState.Alert, 2.5);
        }

        // Клиент получил результат — передаёт его пользователю.
        private void OnClientResultReceived(object sender, PacketEventArgs e)
        {
            if (_endUser != null)
            {
                _endUser.ShowResult();
            }
        }

        // Пользователь получил результат — сегмент «празднует» успех.
        private void OnResultReceived(object sender, EventArgs e)
        {
            SetState(SegmentState.Success, 2.0);
        }

        /// <summary>Рисует сегмент и пунктирную связь «клиент — пользователь».</summary>
        public override void Draw(Graphics g)
        {
            base.Draw(g);
            if (_client == null || _endUser == null || _endUser.Mood != UserMood.Happy)
            {
                return;
            }
            LinkPen.DashStyle = DashStyle.Dot;
            LinkPen.EndCap = LineCap.ArrowAnchor;
            g.DrawLine(LinkPen,
                _client.Location.X, _client.Location.Y + 40f,
                _endUser.Location.X, _endUser.Location.Y - 34f);
        }
    }
}
