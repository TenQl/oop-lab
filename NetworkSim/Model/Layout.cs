using System.Drawing;

namespace NetworkSim.Model
{
    /// <summary>
    /// Статический класс с координатами всех объектов сцены: сегментов,
    /// шлюзов, узлов, акторов и точек магистралей. Собран в одном месте,
    /// чтобы схему можно было править в одном месте.
    /// </summary>
    public static class Layout
    {
        /// <summary>Ширина логического холста схемы.</summary>
        public const int CanvasWidth = 1060;

        /// <summary>Высота логического холста схемы.</summary>
        public const int CanvasHeight = 540;

        // Границы сегментов сети на холсте.
        public static readonly RectangleF ClientSegmentBounds = new RectangleF(20f, 20f, 300f, 500f);
        public static readonly RectangleF ServerSegmentBounds = new RectangleF(380f, 20f, 660f, 500f);

        // Шлюзы сегментов (между ними проходит линия связи).
        public static readonly PointF ClientGateway = new PointF(320f, 270f);
        public static readonly PointF ServerGateway = new PointF(380f, 270f);

        // Акторы клиентского и серверного сегментов.
        public static readonly PointF ClientLocation = new PointF(110f, 130f);
        public static readonly PointF EndUserLocation = new PointF(110f, 330f);
        public static readonly PointF AdministratorLocation = new PointF(685f, 450f);

        // Идентификаторы вершин акторов в графах дорожек.
        public const string ClientWaypointId = "cl";
        public const string AdministratorWaypointId = "adm";

        // Промежуточные точки магистрали клиентского сегмента.
        public static readonly PointF ClientTop = new PointF(200f, 190f);
        public static readonly PointF ClientMiddle = new PointF(200f, 270f);

        // Развилка магистрали серверного сегмента (от неё идёт ветка к администратору).
        public static readonly PointF ServerJunction = new PointF(685f, 270f);

        // Центры узлов обработки: четыре узла на одной магистрали.
        public static PointF NodeLocation(int nodeIndex)
        {
            return new PointF(460f + nodeIndex * 150f, 270f);
        }
    }
}
