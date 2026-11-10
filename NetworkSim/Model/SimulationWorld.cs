using System;
using System.Collections.Generic;
using System.Drawing;

namespace NetworkSim.Model
{
    /// <summary>
    /// «Сборщик» модели: создаёт часы, сегменты сети, узлы, акторов и журнал,
    /// связывает объекты подписками на события и ведёт общие счётчики.
    /// Главная форма работает с моделью только через этот класс.
    /// </summary>
    public class SimulationWorld : IDrawable
    {
        private readonly List<ProcessingNode> _nodes = new List<ProcessingNode>();

        private int _errorsReturned;        // ошибок данных возвращено клиенту
        private int _tasksActive;           // задач «в полёте»

        /// <summary>Максимальное число задач, одновременно находящихся в работе.</summary>
        public const int MaxActiveTasks = 5;

        public SimulationClock Clock { get; }

        public ClientSegment ClientSide { get; }

        public ServerSegment ServerSide { get; }

        public Client Client { get; }

        public EndUser EndUser { get; }

        public Administrator Administrator { get; }

        public SimulationLogger Logger { get; }

        /// <summary>Список узлов обработки серверного сегмента (только чтение).</summary>
        public IReadOnlyList<ProcessingNode> Nodes
        {
            get { return _nodes; }
        }

        /// <summary>Всего отправлено задач (с учётом повторов).</summary>
        public int SentCount
        {
            get { return Client.SentCount; }
        }

        /// <summary>Доставлено результатов пользователю.</summary>
        public int DeliveredCount
        {
            get { return EndUser.ResultsReceived; }
        }

        /// <summary>Ошибок данных возвращено клиенту.</summary>
        public int ErrorsReturned
        {
            get { return _errorsReturned; }
        }

        /// <summary>Выполнено ремонтов узлов.</summary>
        public int RepairsDone
        {
            get { return Administrator.RepairsDone; }
        }

        /// <summary>Задач сейчас «в полёте» (принято к работе, но не завершено).</summary>
        public int TasksActive
        {
            get { return _tasksActive; }
        }


        /// <summary>
        /// Создаёт и связывает все объекты модели: часы, журнал, два сегмента,
        /// четыре узла (включая экземпляр базового класса — коммутатор) и акторов.
        /// </summary>
        public SimulationWorld()
        {
            Clock = new SimulationClock();
            Logger = new SimulationLogger(Clock);

            ClientSide = new ClientSegment(Layout.ClientSegmentBounds,
                Layout.ClientGateway, Clock);
            ServerSide = new ServerSegment(Layout.ServerSegmentBounds,
                Layout.ServerGateway, Clock);

            Client = new Client("Клиент", Layout.ClientWaypointId,
                Layout.ClientLocation, Clock);
            // Подписка раньше сегмента: в журнале сначала «клиент получил»,
            // затем реакция пользователя.
            Client.ResultReceived += (s, e) => Logger.Log(Client.Name,
                "результат получен и передан пользователю");
            EndUser = new EndUser("Пользователь", Layout.EndUserLocation, Clock);
            EndUser.ResultReceived += (s, e) => Logger.Log(EndUser.Name,
                "результат получен, пользователь доволен");
            Administrator = new Administrator("Администратор", Layout.AdministratorWaypointId,
                Layout.AdministratorLocation, Clock);

            // Цепочка обработки: экземпляр базового класса (коммутатор) и три наследника.
            ProcessingNode switchNode = new ProcessingNode("Коммутатор", "n0",
                Layout.NodeLocation(0), Clock, 1.0, 0.0);
            FirewallNode firewall = new FirewallNode("Брандмауэр", "n1",
                Layout.NodeLocation(1), Clock, 1.5, 0.1);
            AppServerNode appServer = new AppServerNode("Сервер приложений", "n2",
                Layout.NodeLocation(2), Clock, 2.0, 0.1);
            DatabaseNode database = new DatabaseNode("База данных", "n3",
                Layout.NodeLocation(3), Clock, 2.0, 0.05);
            _nodes.Add(switchNode);
            _nodes.Add(firewall);
            _nodes.Add(appServer);
            _nodes.Add(database);

            ClientSide.SetClient(Client);
            ClientSide.SetEndUser(EndUser);
            ServerSide.AddNode(switchNode);
            ServerSide.AddNode(firewall);
            ServerSide.AddNode(appServer);
            ServerSide.AddNode(database);
            ServerSide.SetAdministrator(Administrator);
            ClientSide.Connect(ServerSide);

            // Условие отправки: не больше MaxActiveTasks задач одновременно.
            Client.SendGate = () => _tasksActive < MaxActiveTasks;

            // Общие счётчики ведутся по событиям объектов.
            Client.PacketSent += (s, e) =>
            {
                if (e.Packet.Attempt == 1)
                {
                    _tasksActive++;
                }
            };
            Client.FixStarted += (s, e) => _errorsReturned++;
            Client.TaskAbandoned += (s, e) => _tasksActive--;
            EndUser.ResultReceived += (s, e) => _tasksActive--;
            ClientSide.PacketDropped += OnPacketDropped;
            ServerSide.PacketDropped += OnPacketDropped;

            WireLogger();
        }

        /// <summary>
        /// Применяет настройки модели: вероятность ошибки данных (брандмауэр
        /// и сервер приложений), вероятность отказа (база данных) и время
        /// обработки на всех узлах.
        /// </summary>
        public void ApplySettings(double dataErrorProbability, double outageProbability,
                                  double processingTime)
        {
            foreach (ProcessingNode node in _nodes)
            {
                node.ProcessingTime = processingTime;
                FirewallNode firewall = node as FirewallNode;
                if (firewall != null)
                {
                    firewall.FailureProbability = dataErrorProbability;
                    continue;
                }
                AppServerNode appServer = node as AppServerNode;
                if (appServer != null)
                {
                    appServer.FailureProbability = dataErrorProbability;
                    continue;
                }
                DatabaseNode database = node as DatabaseNode;
                if (database != null)
                {
                    database.FailureProbability = outageProbability;
                }
            }
        }

        /// <summary>
        /// Отправляет новую задачу по команде пользователя. Возвращает false
        /// и пояснение в reason, если моделирование остановлено или достигнут
        /// предел числа задач «в полёте».
        /// </summary>
        public bool TrySendTask(out string reason)
        {
            reason = null;
            if (!Clock.IsRunning)
            {
                reason = "Моделирование остановлено — нажмите «Старт»";
                return false;
            }
            if (_tasksActive >= MaxActiveTasks)
            {
                reason = "Достигнут предел числа задач в полёте (" + MaxActiveTasks + ")";
                return false;
            }
            Client.SendTask();
            return true;
        }


        /// <summary>Рисует всю сцену: линию связи, сегменты, узлы, акторов и пакеты.</summary>
        public void Draw(Graphics g)
        {
            // Линия связи между шлюзами сегментов.
            using (Pen casing = new Pen(TrackGraph.CasingColor, 9f))
            using (Pen road = new Pen(TrackGraph.RoadColor, 6f))
            {
                casing.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                casing.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                road.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                road.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                g.DrawLine(casing, Layout.ClientGateway, Layout.ServerGateway);
                g.DrawLine(road, Layout.ClientGateway, Layout.ServerGateway);
            }

            ClientSide.Draw(g);
            ServerSide.Draw(g);
            // Пакеты рисуются до узлов: заходя в узел, пакет скрывается за его рамкой.
            ClientSide.DrawPackets(g);
            ServerSide.DrawPackets(g);
            foreach (ProcessingNode node in _nodes)
            {
                node.Draw(g);
            }
            Client.Draw(g);
            EndUser.Draw(g);
            Administrator.Draw(g);
        }

        // Пакет потерян: задача завершилась, счётчик «в полёте» уменьшается.
        private void OnPacketDropped(object sender, PacketEventArgs e)
        {
            if (e.Packet.Kind == PacketKind.Task && _tasksActive > 0)
            {
                _tasksActive--;
            }
        }

        // Подписывает журнал на события всех объектов: объекты не знают
        // друг о друге, наблюдатель просто слушает их события.
        private void WireLogger()
        {
            Client.PacketSent += (s, e) => Logger.Log(Client.Name,
                "задача отправлена (пакет #" + e.Packet.Id + ", попытка " + e.Packet.Attempt + ")");
            Client.FixStarted += (s, e) => Logger.Log(Client.Name, "исправление ошибки данных…");
            Client.TaskAbandoned += (s, e) => Logger.Log(Client.Name, "попытки исчерпаны, задача отменена");

            Administrator.RepairStarted += (s, e) => Logger.Log(Administrator.Name,
                "подключение по SSH к узлу «" + Administrator.CurrentTarget.Name + "»");
            Administrator.PacketSent += (s, e) => Logger.Log(Administrator.Name,
                "отправлена команда ремонта узлу «" + e.Packet.Subject.Name + "»");


            ClientSide.StateChanged += (s, e) => Logger.Log(ClientSide.Name,
                "состояние сегмента: " + SegmentStateText(e.NewValue));
            ServerSide.StateChanged += (s, e) => Logger.Log(ServerSide.Name,
                "состояние сегмента: " + SegmentStateText(e.NewValue));
            ClientSide.PacketLeaving += (s, e) => Logger.Log(ClientSide.Name,
                "пакет #" + e.Packet.Id + " отправлен в серверный сегмент");
            ServerSide.PacketLeaving += (s, e) => Logger.Log(ServerSide.Name,
                "пакет #" + e.Packet.Id + " отправлен в клиентский сегмент");
            ClientSide.PacketDelivered += (s, e) => Logger.Log(ClientSide.Name,
                "пакет #" + e.Packet.Id + " доставлен адресату «" + e.Packet.Destination.Name + "»");
            ServerSide.PacketDelivered += (s, e) => Logger.Log(ServerSide.Name,
                "пакет #" + e.Packet.Id + " доставлен адресату «" + e.Packet.Destination.Name + "»");
            ClientSide.PacketDropped += (s, e) => Logger.Log(ClientSide.Name,
                "пакет #" + e.Packet.Id + " потерян (маршрут не найден)");
            ServerSide.PacketDropped += (s, e) => Logger.Log(ServerSide.Name,
                "пакет #" + e.Packet.Id + " потерян (маршрут не найден)");

            foreach (ProcessingNode node in _nodes)
            {
                node.ProcessingCompleted += (s, e) =>
                {
                    string text = "пакет #" + e.Packet.Id + " (попытка " +
                                  e.Packet.Attempt + ") обработан";
                    AppServerNode appServer = e.Node as AppServerNode;
                    if (appServer != null)
                    {
                        text += " (всего обработано: " + appServer.HandledCount + ")";
                    }
                    Logger.Log(e.Node.Name, text);
                };
                node.ProcessingFailed += (s, e) =>
                {
                    if (e.Kind == FailureKind.InvalidData)
                    {
                        string text = "ОШИБКА ДАННЫХ в пакете #" + e.Packet.Id +
                                      " (попытка " + e.Packet.Attempt + "), пакет возвращён клиенту";
                        FirewallNode firewall = e.Node as FirewallNode;
                        if (firewall != null)
                        {
                            text += " (всего блокировок: " + firewall.BlockedCount + ")";
                        }
                        Logger.Log(e.Node.Name, text);
                    }
                    else
                    {
                        string text = "ОТКАЗ УЗЛА: пакет #" + e.Packet.Id +
                                      " удерживается до ремонта";
                        DatabaseNode database = e.Node as DatabaseNode;
                        if (database != null)
                        {
                            text += " (всего отказов: " + database.OutageCount + ")";
                        }
                        Logger.Log(e.Node.Name, text);
                    }
                };
                node.StatusChanged += (s, e) => Logger.Log(node.Name,
                    "состояние: " + NodeStatusText(e.NewValue));
            }
        }

        // Текстовое описание состояния узла для журнала.
        private static string NodeStatusText(NodeStatus status)
        {
            switch (status)
            {
                case NodeStatus.Processing: return "обработка";
                case NodeStatus.Failed: return "ОТКАЗ";
                default: return "простой";
            }
        }

        // Текстовое описание состояния сегмента для журнала.
        private static string SegmentStateText(SegmentState state)
        {
            switch (state)
            {
                case SegmentState.Busy: return "обработка";
                case SegmentState.Alert: return "авария";
                case SegmentState.Success: return "успешно";
                default: return "штатный режим";
            }
        }
    }
}



