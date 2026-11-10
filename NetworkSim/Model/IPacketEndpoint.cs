using System.Drawing;

namespace NetworkSim.Model
{
    /// <summary>
    /// Объект сети, способный принимать пакеты (клиент, узел обработки,
    /// администратор, получатель результата).
    /// </summary>
    public interface IPacketEndpoint
    {
        /// <summary>Имя объекта для отображения и журнала.</summary>
        string Name { get; }

        /// <summary>Идентификатор вершины графа дорожек своего сегмента.</summary>
        string WaypointId { get; }

        /// <summary>Координаты объекта на схеме.</summary>
        PointF Location { get; }

        /// <summary>Принимает пакет, дошедший до адресата.</summary>
        void Receive(Packet packet);
    }
}
