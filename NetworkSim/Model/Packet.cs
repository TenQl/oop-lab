using System;
using System.Collections.Generic;
using System.Drawing;

namespace NetworkSim.Model
{
    /// <summary>
    /// Пакет — перемещающийся по дорожкам сети объект. Вид пакета определяет
    /// его назначение: задача, ошибка данных, уведомление об отказе узла,
    /// команда ремонта или результат обработки.
    /// </summary>
    public class Packet : IDrawable
    {
        private static int _nextId = 1;     // общий счётчик номеров пакетов

        private const float DefaultSpeed = 160f;    // скорость, пикселей схемы в секунду

        private List<PointF> _route;        // текущий маршрут (null, если пакет не движется)
        private int _routeIndex;            // индекс следующей точки маршрута
        private PointF _position;           // текущая позиция на схеме

        private static readonly Brush TaskBrush = new SolidBrush(Color.FromArgb(46, 116, 181));
        private static readonly Brush ErrorBrush = new SolidBrush(Color.FromArgb(198, 40, 40));
        private static readonly Brush FailureBrush = new SolidBrush(Color.FromArgb(230, 126, 34));
        private static readonly Brush FixBrush = new SolidBrush(Color.FromArgb(39, 134, 82));
        private static readonly Brush ResultBrush = new SolidBrush(Color.FromArgb(212, 160, 23));
        private static readonly Pen OutlinePen = new Pen(Color.White, 1.4f);
        private static readonly Brush WhiteBrush = new SolidBrush(Color.White);
        private static readonly Font MarkerFont = new Font("Arial", 9f, FontStyle.Bold, GraphicsUnit.Pixel);

        /// <summary>Размер значка пакета на схеме, пикселей.</summary>
        public const float VisualSize = 13f;

        public int Id { get; }

        public PacketKind Kind { get; }

        public IPacketEndpoint Owner { get; }

        public int Attempt { get; }

        public ProcessingNode Subject { get; }

        /// <summary>Адресат пакета; null означает «за пределами сегмента».</summary>
        public IPacketEndpoint Destination { get; set; }

        /// <summary>Идентификатор вершины графа, из которой пакет начинает движение.</summary>
        public string SourceWaypointId { get; private set; }

        /// <summary>Текущая позиция пакета на схеме.</summary>
        public PointF Position
        {
            get { return _position; }
        }

        /// <summary>Скорость движения, пикселей схемы в секунду.</summary>
        public double Speed { get; set; }

        /// <summary>Событие: пакет дошёл до конца маршрута.</summary>
        public event EventHandler<PacketEventArgs> Arrived;

        /// <summary>Создаёт пакет заданного типа.</summary>
        public Packet(PacketKind kind, string sourceWaypointId, IPacketEndpoint owner,
                      int attempt, ProcessingNode subject = null)
        {
            Id = _nextId++;
            Kind = kind;
            SourceWaypointId = sourceWaypointId;
            Owner = owner;
            Attempt = attempt;
            Subject = subject;
            Speed = DefaultSpeed;
        }

        /// <summary>
        /// Перенаправляет пакет: задаёт нового адресата и вершину,
        /// из которой пакет продолжит движение.
        /// </summary>
        public void Readdress(IPacketEndpoint destination, string fromWaypointId)
        {
            Destination = destination;
            SourceWaypointId = fromWaypointId;
            _route = null;
            _routeIndex = 0;
        }

        /// <summary>Запускает пакет по маршруту (последовательности точек схемы).</summary>
        public void StartRoute(IList<PointF> route)
        {
            if (route == null || route.Count == 0)
            {
                return;
            }
            _route = new List<PointF>(route);
            _position = _route[0];
            _routeIndex = 1;
        }

        /// <summary>
        /// Продвигает пакет по маршруту за шаг модельного времени dt.
        /// Когда маршрут пройден полностью, вызывает событие Arrived.
        /// </summary>
        public void Advance(double dt)
        {
            if (_route == null || _routeIndex >= _route.Count)
            {
                return;     // пакет не движется
            }
            float remaining = (float)(Speed * dt);
            while (remaining > 0f && _routeIndex < _route.Count)
            {
                PointF target = _route[_routeIndex];
                float dx = target.X - _position.X;
                float dy = target.Y - _position.Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                if (dist <= remaining)
                {
                    // Доходим до очередной точки маршрута и переходим к следующей.
                    _position = target;
                    remaining -= dist;
                    _routeIndex++;
                }
                else
                {
                    // Сдвигаемся к следующей точке на оставшуюся часть шага.
                    _position = new PointF(
                        _position.X + dx / dist * remaining,
                        _position.Y + dy / dist * remaining);
                    remaining = 0f;
                }
            }
            if (_routeIndex >= _route.Count)
            {
                _route = null;      // маршрут пройден полностью
                OnArrived(new PacketEventArgs(this));
            }
        }

        /// <summary>Рисует пакет; форма и цвет зависят от типа пакета.</summary>
        public void Draw(Graphics g)
        {
            float s = VisualSize;
            float x = _position.X - s / 2f;
            float y = _position.Y - s / 2f;
            switch (Kind)
            {
                case PacketKind.Task:
                    g.FillEllipse(TaskBrush, x, y, s, s);
                    g.DrawEllipse(OutlinePen, x, y, s, s);
                    DrawAttemptNumber(g);       // номер попытки внутри пакета
                    break;

                case PacketKind.DataError:
                    // Красный треугольник — ошибка данных.
                    PointF[] triangle =
                    {
                        new PointF(x + s / 2f, y),
                        new PointF(x + s, y + s),
                        new PointF(x, y + s)
                    };
                    g.FillPolygon(ErrorBrush, triangle);
                    g.DrawPolygon(OutlinePen, triangle);
                    break;

                case PacketKind.NodeFailure:
                    // Оранжевый ромб с восклицательным знаком — отказ узла.
                    PointF[] diamond =
                    {
                        new PointF(x + s / 2f, y),
                        new PointF(x + s, y + s / 2f),
                        new PointF(x + s / 2f, y + s),
                        new PointF(x, y + s / 2f)
                    };
                    g.FillPolygon(FailureBrush, diamond);
                    g.DrawPolygon(OutlinePen, diamond);
                    SizeF excl = g.MeasureString("!", MarkerFont);
                    g.DrawString("!", MarkerFont, WhiteBrush,
                        _position.X - excl.Width / 2f, _position.Y - excl.Height / 2f);
                    break;

                case PacketKind.Fix:
                    // Зелёный квадрат — команда ремонта.
                    g.FillRectangle(FixBrush, x, y, s, s);
                    g.DrawRectangle(OutlinePen, x, y, s, s);
                    break;

                case PacketKind.Result:
                    // Золотой круг с галочкой — результат.
                    g.FillEllipse(ResultBrush, x, y, s, s);
                    g.DrawEllipse(OutlinePen, x, y, s, s);
                    PointF[] check =
                    {
                        new PointF(x + 3f, y + 7f),
                        new PointF(x + 5.5f, y + 9.5f),
                        new PointF(x + 10f, y + 3.5f)
                    };
                    g.DrawLines(OutlinePen, check);
                    break;
            }
        }

        /// <summary>Вызывает событие Arrived.</summary>
        protected virtual void OnArrived(PacketEventArgs e)
        {
            Arrived?.Invoke(this, e);
        }

        // Рисует номер попытки выполнения задачи внутри кружка задачи.
        private void DrawAttemptNumber(Graphics g)
        {
            string text = Attempt.ToString();
            SizeF size = g.MeasureString(text, MarkerFont);
            g.DrawString(text, MarkerFont, WhiteBrush,
                _position.X - size.Width / 2f, _position.Y - size.Height / 2f);
        }
    }
}

