using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace NetworkSim.Model
{
    /// <summary>
    /// Граф дорожек сегмента сети: вершины (точки на схеме) и соединяющие их
    /// дорожки, по которым движутся пакеты. Умеет находить маршрут между
    /// вершинами поиском в ширину и рисовать дорожки.
    /// </summary>
    public class TrackGraph
    {
        private readonly Dictionary<string, PointF> _points = new Dictionary<string, PointF>();
        private readonly Dictionary<string, List<string>> _adjacency =
            new Dictionary<string, List<string>>();
        private readonly List<KeyValuePair<string, string>> _lanes =
            new List<KeyValuePair<string, string>>();

        // Цвета дорожек (общедоступны: линия связи между сегментами та же).
        public static readonly Color CasingColor = Color.FromArgb(122, 130, 140);
        public static readonly Color RoadColor = Color.FromArgb(213, 218, 225);

        /// <summary>Добавляет вершину графа (точку на схеме сегмента).</summary>
        public void AddWaypoint(string id, PointF point)
        {
            if (!_points.ContainsKey(id))
            {
                _points[id] = point;
            }
        }

        /// <summary>
        /// Добавляет двустороннюю дорожку между двумя вершинами. Вершины
        /// могут быть добавлены и позже (до первого поиска маршрута).
        /// </summary>
        public void AddLane(string idA, string idB)
        {
            if (idA == idB)
            {
                return;
            }
            AddNeighbor(idA, idB);
            AddNeighbor(idB, idA);
            _lanes.Add(new KeyValuePair<string, string>(idA, idB));
        }

        /// <summary>Возвращает координаты вершины графа.</summary>
        public PointF GetPoint(string id)
        {
            return _points[id];
        }

        /// <summary>
        /// Ищет кратчайший маршрут между вершинами поиском в ширину.
        /// Возвращает последовательность точек или null, если пути нет.
        /// </summary>
        public List<PointF> FindRoute(string fromId, string toId)
        {
            if (!_points.ContainsKey(fromId) || !_points.ContainsKey(toId))
            {
                return null;
            }
            List<PointF> route = new List<PointF>();
            if (fromId == toId)
            {
                route.Add(_points[fromId]);
                return route;
            }

            // Поиск в ширину: previous хранит вершину, из которой пришли.
            Dictionary<string, string> previous = new Dictionary<string, string>();
            Queue<string> frontier = new Queue<string>();
            frontier.Enqueue(fromId);
            previous[fromId] = null;
            bool found = false;
            while (frontier.Count > 0 && !found)
            {
                string current = frontier.Dequeue();
                List<string> neighbors;
                if (!_adjacency.TryGetValue(current, out neighbors))
                {
                    continue;
                }
                foreach (string next in neighbors)
                {
                    if (previous.ContainsKey(next))
                    {
                        continue;
                    }
                    previous[next] = current;
                    if (next == toId)
                    {
                        found = true;
                        break;
                    }
                    frontier.Enqueue(next);
                }
            }
            if (!found)
            {
                return null;
            }

            // Восстановление пути: от конечной вершины поднимаемся к начальной.
            Stack<string> path = new Stack<string>();
            string step = toId;
            while (step != null)
            {
                path.Push(step);
                step = previous[step];
            }
            foreach (string id in path)
            {
                route.Add(_points[id]);
            }
            return route;
        }

        /// <summary>Рисует дорожки сегмента толстыми серыми линиями.</summary>
        public void Draw(Graphics g)
        {
            using (Pen casing = new Pen(CasingColor, 9f))
            using (Pen road = new Pen(RoadColor, 6f))
            {
                casing.StartCap = LineCap.Round;
                casing.EndCap = LineCap.Round;
                road.StartCap = LineCap.Round;
                road.EndCap = LineCap.Round;
                foreach (KeyValuePair<string, string> lane in _lanes)
                {
                    PointF a, b;
                    if (!_points.TryGetValue(lane.Key, out a) ||
                        !_points.TryGetValue(lane.Value, out b))
                    {
                        continue;   // вершина ещё не зарегистрирована
                    }
                    g.DrawLine(casing, a, b);
                    g.DrawLine(road, a, b);
                }
            }
        }

        // Добавляет вершине соседа по дорожке (без повторов).
        private void AddNeighbor(string fromId, string toId)
        {
            List<string> neighbors;
            if (!_adjacency.TryGetValue(fromId, out neighbors))
            {
                neighbors = new List<string>();
                _adjacency[fromId] = neighbors;
            }
            if (!neighbors.Contains(toId))
            {
                neighbors.Add(toId);
            }
        }
    }
}
