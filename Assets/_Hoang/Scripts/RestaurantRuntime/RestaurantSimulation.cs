using BunDauPeo.Restaurant;
using System;
using System.Collections.Generic;
using BunDauPeo.Core;

namespace Hoang.Remake.RestaurantRuntime
{
    // Giá trị số theo thứ tự dây chuyền; test dựa vào việc trạng thái chỉ đi tới.
    public enum CustomerState
    {
        WaitingForTable,
        WaitingForCook,
        Cooking,
        WaitingForServer,
        Serving,
        Eating,
        WaitingForCashier,
        Paying,
    }

    public readonly struct CustomerSnapshot
    {
        public CustomerSnapshot(int id, CustomerDefinition definition, DishDefinition dish, CustomerState state, int tableIndex, float patienceRemaining)
        {
            Id = id;
            Definition = definition;
            Dish = dish;
            State = state;
            TableIndex = tableIndex;
            PatienceRemaining = patienceRemaining;
        }

        public int Id { get; }
        public CustomerDefinition Definition { get; }
        public DishDefinition Dish { get; }
        public CustomerState State { get; }
        // -1 khi chưa có bàn.
        public int TableIndex { get; }
        public float PatienceRemaining { get; }
    }

    public readonly struct WorkerSnapshot
    {
        public WorkerSnapshot(string characterId, StaffRole role, int customerId)
        {
            CharacterId = characterId;
            Role = role;
            CustomerId = customerId;
        }

        public string CharacterId { get; }
        public StaffRole Role { get; }
        // -1 khi rảnh.
        public int CustomerId { get; }
    }

    // Mô phỏng thuần: không biết Wallet, không biết tọa độ. View đọc Customers và Workers mỗi khung.
    // Thứ tự gọi RNG là hợp đồng (spec 4.3): constructor 1 lần; mỗi lần khách đến còn chỗ: loại, món, khoảng đến kế; hàng đầy: chỉ khoảng đến kế.
    public sealed class RestaurantSimulation
    {
        private const double Step = 0.1;
        // Dung sai so sánh thời gian: cộng dồn 0,1 nhiều lần không đúng tuyệt đối.
        private const double Epsilon = 1e-9;

        private sealed class Customer
        {
            public int Id;
            public CustomerDefinition Definition;
            public DishDefinition Dish;
            public CustomerState State;
            public int Table = -1;
            public double EatRemaining;
            // Đồng hồ kiên nhẫn đang chạy; khi bắt đầu ăn nó đặt lại đầy và đứng yên, nên đây cũng là đồng hồ 2.
            public double Patience;
        }

        private sealed class Worker
        {
            public string CharacterId;
            public StaffRole Role;
            public int Stat;
            public Customer Current;
            public double Remaining;
        }

        private readonly RestaurantConfig _config;
        private readonly Random _random;
        private readonly List<Customer> _customers = new();
        private readonly List<Customer> _tableQueue = new();
        private readonly List<Customer> _cookQueue = new();
        private readonly List<Customer> _serveQueue = new();
        private readonly List<Customer> _cashierQueue = new();
        private readonly List<Worker> _workers = new();
        private readonly List<CustomerSnapshot> _customerSnapshots = new();
        private readonly List<WorkerSnapshot> _workerSnapshots = new();

        private RestaurantProfile _profile;
        private bool _hasProfile;
        private Customer[] _tables = Array.Empty<Customer>();
        private double _accumulated;
        private double _arrivalTimer;
        // Khoảng đến đầu tiên bốc ở constructor, khi chưa có hồ sơ nên chưa biết hệ số; tính ở SetProfile đầu tiên.
        private double? _firstArrivalFraction;
        private int _nextCustomerId;
        private readonly bool[] _blocked = new bool[3];

        public RestaurantSimulation(RestaurantConfig config, Random random)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _random = random ?? throw new ArgumentNullException(nameof(random));
            DrawFirstArrival();
        }

        public IReadOnlyList<CustomerSnapshot> Customers => _customerSnapshots;
        public IReadOnlyList<WorkerSnapshot> Workers => _workerSnapshots;
        public int Served { get; private set; }
        public int Abandoned { get; private set; }

        public event Action<int, long> CustomerPaid;

        public void SetProfile(RestaurantProfile profile)
        {
            _profile = profile;
            _hasProfile = true;
            if (_firstArrivalFraction.HasValue)
            {
                _arrivalTimer = ArrivalInterval(_firstArrivalFraction.Value);
                _firstArrivalFraction = null;
            }

            // Bàn đang có khách không biến mất khi số bàn giảm: mảng chỉ co tới bàn cuối còn người ngồi.
            var length = profile.Tables;
            for (var i = _tables.Length - 1; i >= profile.Tables; i--)
            {
                if (_tables[i] == null) continue;
                length = i + 1;
                break;
            }

            if (_tables.Length != length) Array.Resize(ref _tables, length);
            ReconcileWorkers(profile);
            RefreshSnapshots();
        }

        // Người vẫn còn ở cùng vai giữ việc; người bị rút đi hoặc vượt số ô thì việc của họ về ĐẦU hàng đợi của trạm, mất tiến độ.
        private void ReconcileWorkers(RestaurantProfile profile)
        {
            var previous = new List<Worker>(_workers);
            _workers.Clear();
            foreach (var role in new[] { StaffRole.Cook, StaffRole.Server, StaffRole.Cashier })
            {
                foreach (var info in profile.Workers(role))
                {
                    var kept = previous.Find(w => w.Role == role && w.CharacterId == info.CharacterId);
                    if (kept != null)
                    {
                        previous.Remove(kept);
                        kept.Stat = info.Stat;
                        _workers.Add(kept);
                    }
                    else
                    {
                        _workers.Add(new Worker { CharacterId = info.CharacterId, Role = role, Stat = info.Stat });
                    }
                }
            }

            var insertAt = new int[3];
            foreach (var gone in previous)
            {
                if (gone.Current == null) continue;
                QueueOf(gone.Role).Insert(insertAt[(int)gone.Role]++, gone.Current);
                gone.Current.State = gone.Role switch
                {
                    StaffRole.Cook => CustomerState.WaitingForCook,
                    StaffRole.Server => CustomerState.WaitingForServer,
                    _ => CustomerState.WaitingForCashier,
                };
            }
        }

        // Sự cố: người của vai ngừng nhận việc mới và việc đang làm đóng băng tiến độ.
        public void SetStationBlocked(StaffRole role, bool blocked) => _blocked[(int)role] = blocked;

        public void Reset()
        {
            _customers.Clear();
            _tableQueue.Clear();
            _cookQueue.Clear();
            _serveQueue.Clear();
            _cashierQueue.Clear();
            Array.Clear(_tables, 0, _tables.Length);
            foreach (var worker in _workers) worker.Current = null;
            Served = 0;
            Abandoned = 0;
            _accumulated = 0;
            DrawFirstArrival();
            RefreshSnapshots();
        }

        private void DrawFirstArrival()
        {
            var fraction = 0.5 + _random.NextDouble();
            if (_hasProfile) _arrivalTimer = ArrivalInterval(fraction);
            else _firstArrivalFraction = fraction;
        }

        // Cộng dồn dt rồi chạy bước cố định 0,1 giây, nên kết quả không phụ thuộc tốc độ khung hình.
        public void Tick(float dt)
        {
            if (!_hasProfile) return;
            _accumulated += dt;
            while (_accumulated >= Step - 1e-6)
            {
                _accumulated -= Step;
                RunStep();
            }

            RefreshSnapshots();
        }

        private void RunStep()
        {
            // Kiên nhẫn và ăn chạy trước worker: khách vừa đổi trạng thái ở bước này không bị trừ giờ ngay trong chính bước đó.
            AdvancePatience();
            AdvanceEating();
            AdvanceWorkers();
            SeatWaiting();
            AdvanceArrivals();
            SeatWaiting();
            AssignWork();
        }

        private void AdvancePatience()
        {
            List<Customer> expired = null;
            foreach (var customer in _customers)
            {
                if (customer.State == CustomerState.Eating) continue;
                customer.Patience -= Step;
                if (customer.Patience <= Epsilon) (expired ??= new List<Customer>()).Add(customer);
            }

            if (expired == null) return;
            foreach (var customer in expired) Abandon(customer);
        }

        // Rời không trả tiền: việc đang làm bị huỷ ngay (người làm rảnh), nhả bàn, rời mọi hàng đợi.
        private void Abandon(Customer customer)
        {
            _tableQueue.Remove(customer);
            _cookQueue.Remove(customer);
            _serveQueue.Remove(customer);
            _cashierQueue.Remove(customer);
            foreach (var worker in _workers)
                if (worker.Current == customer) worker.Current = null;
            Remove(customer);
            Abandoned++;
        }

        private void AdvanceWorkers()
        {
            foreach (var worker in _workers)
            {
                if (worker.Current == null || _blocked[(int)worker.Role]) continue;
                worker.Remaining -= Step;
                if (worker.Remaining > Epsilon) continue;

                var customer = worker.Current;
                worker.Current = null;
                switch (worker.Role)
                {
                    case StaffRole.Cook:
                        customer.State = CustomerState.WaitingForServer;
                        _serveQueue.Add(customer);
                        break;
                    case StaffRole.Server:
                        customer.State = CustomerState.Eating;
                        customer.EatRemaining = _config.EatSeconds;
                        customer.Patience = customer.Definition.PatienceSeconds * _profile.PatienceMultiplier;
                        break;
                    default:
                        Pay(customer);
                        break;
                }
            }
        }

        private void AdvanceEating()
        {
            foreach (var customer in _customers)
            {
                if (customer.State != CustomerState.Eating) continue;
                customer.EatRemaining -= Step;
                if (customer.EatRemaining > Epsilon) continue;
                customer.State = CustomerState.WaitingForCashier;
                _cashierQueue.Add(customer);
            }
        }

        private void Pay(Customer customer)
        {
            var price = Math.Round(customer.Dish.BasePrice * (double)customer.Definition.PayMultiplier * _profile.PriceMultiplier,
                MidpointRounding.AwayFromZero);
            var coins = price >= long.MaxValue ? long.MaxValue : Math.Max(1L, (long)price);
            Remove(customer);
            Served++;
            CustomerPaid.SafeInvoke(customer.Id, coins);
        }

        private void Remove(Customer customer)
        {
            if (customer.Table >= 0) _tables[customer.Table] = null;
            _customers.Remove(customer);
        }

        private void AdvanceArrivals()
        {
            _arrivalTimer -= Step;
            while (_arrivalTimer <= Epsilon)
            {
                if (_tableQueue.Count >= _config.MaxWaitingLine) Abandoned++;
                else Spawn();
                _arrivalTimer += ArrivalInterval(0.5 + _random.NextDouble());
            }
        }

        private void Spawn()
        {
            var customer = new Customer
            {
                Id = _nextCustomerId++,
                Definition = PickCustomer(),
                State = CustomerState.WaitingForTable,
            };
            customer.Patience = customer.Definition.PatienceSeconds * _profile.PatienceMultiplier;
            var dishes = _config.Dishes;
            customer.Dish = dishes[Math.Min((int)(_random.NextDouble() * dishes.Count), dishes.Count - 1)];
            _customers.Add(customer);
            _tableQueue.Add(customer);
        }

        private CustomerDefinition PickCustomer()
        {
            var total = 0.0;
            foreach (var customer in _config.Customers) total += customer.SpawnWeight;
            var roll = _random.NextDouble() * total;
            var cumulative = 0.0;
            foreach (var customer in _config.Customers)
            {
                cumulative += customer.SpawnWeight;
                if (roll < cumulative) return customer;
            }

            return _config.Customers[_config.Customers.Count - 1];
        }

        private double ArrivalInterval(double fraction) =>
            _config.BaseArrivalInterval * _profile.ArrivalIntervalMultiplier * fraction;

        private void SeatWaiting()
        {
            while (_tableQueue.Count > 0)
            {
                var free = Array.IndexOf(_tables, null, 0, Math.Min(_tables.Length, _profile.Tables));
                if (free < 0) return;
                var customer = _tableQueue[0];
                _tableQueue.RemoveAt(0);
                _tables[free] = customer;
                customer.Table = free;
                // Đặt món tự động ngay khi ngồi xuống.
                customer.State = CustomerState.WaitingForCook;
                _cookQueue.Add(customer);
            }
        }

        private void AssignWork()
        {
            foreach (var worker in _workers)
            {
                if (worker.Current != null || _blocked[(int)worker.Role]) continue;
                var queue = QueueOf(worker.Role);
                if (queue.Count == 0) continue;

                var customer = queue[0];
                queue.RemoveAt(0);
                worker.Current = customer;
                worker.Remaining = BaseSeconds(worker.Role, customer) * 100.0 / (100.0 + worker.Stat);
                customer.State = worker.Role switch
                {
                    StaffRole.Cook => CustomerState.Cooking,
                    StaffRole.Server => CustomerState.Serving,
                    _ => CustomerState.Paying,
                };
            }
        }

        private List<Customer> QueueOf(StaffRole role) => role switch
        {
            StaffRole.Cook => _cookQueue,
            StaffRole.Server => _serveQueue,
            _ => _cashierQueue,
        };

        private double BaseSeconds(StaffRole role, Customer customer) => role switch
        {
            StaffRole.Cook => (double)customer.Dish.CookSeconds / _profile.CookingSpeedMultiplier,
            StaffRole.Server => _config.ServeSeconds,
            _ => _config.CashierSeconds,
        };

        private void RefreshSnapshots()
        {
            _customerSnapshots.Clear();
            foreach (var customer in _customers)
                _customerSnapshots.Add(new CustomerSnapshot(customer.Id, customer.Definition, customer.Dish, customer.State, customer.Table, (float)customer.Patience));
            _workerSnapshots.Clear();
            foreach (var worker in _workers)
                _workerSnapshots.Add(new WorkerSnapshot(worker.CharacterId, worker.Role, worker.Current?.Id ?? -1));
        }
    }
}

