using System;

namespace NetworkSim.Model
{
    /// <summary>Аргументы события «шаг модельного времени».</summary>
    public class TickEventArgs : EventArgs
    {
        public double Dt { get; }

        public TickEventArgs(double dt)
        {
            Dt = dt;
        }
    }

    /// <summary>Аргументы событий, связанных с пакетом.</summary>
    public class PacketEventArgs : EventArgs
    {
        public Packet Packet { get; }

        public PacketEventArgs(Packet packet)
        {
            Packet = packet;
        }
    }

    /// <summary>Аргументы событий, связанных с узлом обработки.</summary>
    public class NodeEventArgs : EventArgs
    {
        public ProcessingNode Node { get; }

        public Packet Packet { get; }

        public NodeEventArgs(ProcessingNode node, Packet packet)
        {
            Node = node;
            Packet = packet;
        }
    }

    /// <summary>Аргументы события «сбой при обработке пакета».</summary>
    public class NodeFailureEventArgs : NodeEventArgs
    {
        public FailureKind Kind { get; }

        public NodeFailureEventArgs(ProcessingNode node, Packet packet, FailureKind kind)
            : base(node, packet)
        {
            Kind = kind;
        }
    }

    /// <summary>
    /// Аргументы события узла с числовым счётчиком: сколько всего событий
    /// данного вида произошло на узле (блокировок, обработок, отказов).
    /// </summary>
    public class NodeCounterEventArgs : NodeEventArgs
    {
        public int Count { get; }

        public NodeCounterEventArgs(ProcessingNode node, Packet packet, int count)
            : base(node, packet)
        {
            Count = count;
        }
    }

    /// <summary>Аргументы события «изменилось значение свойства».</summary>
    public class ValueChangedEventArgs<T> : EventArgs
    {
        public T OldValue { get; }

        public T NewValue { get; }

        public ValueChangedEventArgs(T oldValue, T newValue)
        {
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}
