namespace NetworkSim.Model
{
    /// <summary>Тип пакета, перемещающегося по сети.</summary>
    public enum PacketKind
    {
        Task,           // задача на обработку
        DataError,      // сообщение об ошибке данных (возвращается клиенту)
        NodeFailure,    // уведомление об отказе узла (направляется администратору)
        Fix,            // команда ремонта узла (от администратора)
        Result          // результат обработки задачи
    }

    /// <summary>Состояние узла обработки.</summary>
    public enum NodeStatus
    {
        Idle,           // узел свободен
        Processing,     // узел обрабатывает пакет
        Failed          // отказ: узел неисправен и ожидает ремонта
    }

    /// <summary>Вид сбоя при обработке пакета.</summary>
    public enum FailureKind
    {
        None,           // сбоя не было
        InvalidData,    // ошибка данных: пакет некорректен, узел исправен
        NodeOutage      // отказ узла: требуется ремонт
    }

    /// <summary>Состояние сегмента сети (определяет его внешний вид).</summary>
    public enum SegmentState
    {
        Normal,         // штатный режим
        Busy,           // сегмент занят обработкой
        Alert,          // аварийная ситуация
        Success         // успешное завершение задачи
    }

    /// <summary>Состояние клиента.</summary>
    public enum ClientState
    {
        Idle,           // свободен
        Fixing,         // исправляет ошибку данных
        Done            // получил результат задачи
    }

    /// <summary>Состояние администратора.</summary>
    public enum AdminState
    {
        Idle,           // ремонтов не требуется
        Repairing       // ремонтирует узел (подключён по SSH)
    }

    /// <summary>Настроение получателя результата.</summary>
    public enum UserMood
    {
        Waiting,        // ждёт результата
        Happy           // результат получен
    }
}
