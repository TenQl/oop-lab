using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NetworkSim.Model
{
    /// <summary>
    /// Брандмауэр — узел, производный от ProcessingNode. Проверяет данные
    /// задачи; в строгом режиме вероятность блокировки удваивается.
    /// Переопределяет Evaluate и Draw базового класса.
    /// </summary>
    public class FirewallNode : ProcessingNode
    {
        private int _blockedCount;      // число заблокированных пакетов

        private static readonly Brush ShieldBrush = new SolidBrush(Color.FromArgb(91, 129, 181));

        /// <summary>Строгий режим проверки (вероятность блокировки удваивается).</summary>
        public bool StrictMode { get; set; }

        /// <summary>Число пакетов, заблокированных брандмауэром (только чтение).</summary>
        public int BlockedCount
        {
            get { return _blockedCount; }
        }

        /// <summary>Создаёт брандмауэр с заданными параметрами.</summary>
        public FirewallNode(string name, string waypointId, PointF location,
                            SimulationClock clock, double processingTime = 1.5,
                            double failureProbability = 0.1)
            : base(name, waypointId, location, clock, processingTime, failureProbability)
        {
        }

        /// <summary>
        /// Определяет исход проверки: как в базовом классе, но в строгом
        /// режиме вероятность ошибки данных удваивается.
        /// </summary>
        protected override FailureKind Evaluate(Packet packet)
        {
            double p = EffectiveProbability(packet);
            if (StrictMode)
            {
                p = Math.Min(1.0, p * 2.0);
            }
            if (Random.Shared.NextDouble() < p)
            {
                _blockedCount++;
                return FailureKind.InvalidData;
            }
            return FailureKind.None;
        }

        /// <summary>Рисует узел с пиктограммой щита.</summary>
        public override void Draw(Graphics g)
        {
            DrawBody(g);
            if (Status != NodeStatus.Failed)
            {
                DrawShieldIcon(g);
            }
        }

        // Пиктограмма щита с галочкой.
        private void DrawShieldIcon(Graphics g)
        {
            float cx = Location.X;
            float cy = Location.Y + 4f;
            PointF[] shield =
            {
                new PointF(cx - 13f, cy - 12f),
                new PointF(cx + 13f, cy - 12f),
                new PointF(cx + 13f, cy + 2f),
                new PointF(cx, cy + 13f),
                new PointF(cx - 13f, cy + 2f)
            };
            g.FillPolygon(ShieldBrush, shield);
            using (Pen p = new Pen(Color.White, 1.6f))
            {
                p.StartCap = LineCap.Round;
                p.EndCap = LineCap.Round;
                g.DrawLine(p, cx - 6f, cy - 1f, cx - 1f, cy + 5f);
                g.DrawLine(p, cx - 1f, cy + 5f, cx + 7f, cy - 6f);
            }
        }
    }
}
