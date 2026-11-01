using System;
using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Core;
using BunDauPeo.Restaurant;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Chỉ vẽ: mỗi khung đối chiếu Simulation.Customers/Workers theo Id rồi dời người tới điểm mốc. Không giữ luật nào.
    // Đi lại chỉ là hoạt hình, không ảnh hưởng thu nhập.
    public class RestaurantView : MonoBehaviour
    {
        private const float WalkSeconds = 1.5f;

        [SerializeField] private RectTransform _actors;
        [SerializeField] private RectTransform _door;
        [SerializeField] private RectTransform[] _tables;
        [SerializeField] private RectTransform[] _cookSpots;
        [SerializeField] private RectTransform[] _serverSpots;
        [SerializeField] private RectTransform[] _cashierSpots;
        [SerializeField] private Image _customerTemplate;
        [SerializeField] private Image _workerTemplate;

        private sealed class Actor
        {
            public RectTransform Rect;
            public Image Image;
            public Text Label;
            // Vị trí thế giới: điểm mốc nằm cùng Canvas nên không phải đổi hệ toạ độ.
            public Vector3 Origin;
            public Vector3 Target;
            public float Elapsed;
            public bool Seen;
        }

        private static readonly Comparison<Actor> FrontLast = (a, b) => b.Rect.position.y.CompareTo(a.Rect.position.y);

        private readonly Dictionary<int, Actor> _customers = new();
        private readonly Dictionary<string, Actor> _workers = new();
        private readonly List<Actor> _all = new();
        private readonly List<int> _staleCustomers = new();
        private readonly List<string> _staleWorkers = new();
        private readonly int[] _perRole = new int[3];

        private RestaurantService _service;
        private Roster _roster;
        // Khung đầu sau khi mở màn: đặt thẳng mọi người vào điểm mốc, không cho đi từ cửa.
        private bool _settle;

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _service = ServiceLocator.Get<RestaurantService>();
                _roster = ServiceLocator.Get<Roster>();
            }
            catch (InvalidOperationException e)
            {
                Debug.LogError($"{nameof(RestaurantView)}: {e.Message}", this);
                _service = null;
                return;
            }

            _service.SetPresent(true);
            _settle = true;
        }

        private void OnDisable()
        {
            if (_service == null) return;
            _service.SetPresent(false);
            _service = null;
            foreach (var actor in _all) Destroy(actor.Rect.gameObject);
            _all.Clear();
            _customers.Clear();
            _workers.Clear();
        }

        private void Update()
        {
            if (_service == null) return;

            var simulation = _service.Simulation;
            foreach (var actor in _all) actor.Seen = false;
            SyncCustomers(simulation);
            SyncWorkers(simulation);
            RemoveUnseen();
            Move(Mathf.Min(Time.unscaledDeltaTime, RestaurantRunner.MaxDelta));
            SortByDepth();
            _settle = false;
        }

        private bool HasAllReferences()
        {
            var ok = _actors != null && _door != null && _customerTemplate != null && _workerTemplate != null
                     && HasSpots(_tables) && HasSpots(_cookSpots) && HasSpots(_serverSpots) && HasSpots(_cashierSpots);
            if (!ok) Debug.LogError($"{nameof(RestaurantView)}: a serialized reference is not assigned.", this);
            return ok;
        }

        private static bool HasSpots(RectTransform[] spots)
        {
            if (spots == null || spots.Length == 0) return false;
            foreach (var spot in spots)
                if (spot == null) return false;
            return true;
        }

        private void SyncCustomers(RestaurantSimulation simulation)
        {
            var queued = 0;
            foreach (var customer in simulation.Customers)
            {
                var target = CustomerTarget(customer, ref queued);
                if (!_customers.TryGetValue(customer.Id, out var actor))
                {
                    actor = Create(_customerTemplate);
                    _customers.Add(customer.Id, actor);
                    Place(actor, _settle ? target : _door.position, target);
                }

                actor.Seen = true;
                Retarget(actor, target);
                actor.Image.color = StateColor(customer.State);
            }
        }

        private void SyncWorkers(RestaurantSimulation simulation)
        {
            Array.Clear(_perRole, 0, _perRole.Length);
            foreach (var worker in simulation.Workers)
            {
                var index = _perRole[(int)worker.Role]++;
                var target = WorkerTarget(worker, index, simulation);
                if (!_workers.TryGetValue(worker.CharacterId, out var actor))
                {
                    actor = Create(_workerTemplate);
                    actor.Image.color = RoleColor(worker.Role);
                    actor.Label.text = NameOf(worker.CharacterId);
                    _workers.Add(worker.CharacterId, actor);
                    Place(actor, target, target);
                }

                actor.Seen = true;
                Retarget(actor, target);
            }
        }

        private Vector3 CustomerTarget(CustomerSnapshot customer, ref int queued)
        {
            switch (customer.State)
            {
                case CustomerState.WaitingForTable:
                    var place = queued++;
                    return _door.position + Offset(-26f * place, 14f * place);
                case CustomerState.WaitingForCashier:
                case CustomerState.Paying:
                    // Khách trả tiền đứng phía trước quầy; điểm mốc của thu ngân nằm sau quầy.
                    return Spot(_cashierSpots, 0) + Offset(0f, -150f);
                default:
                    return Spot(_tables, Mathf.Max(0, customer.TableIndex)) + Offset(0f, 18f);
            }
        }

        private Vector3 WorkerTarget(WorkerSnapshot worker, int index, RestaurantSimulation simulation)
        {
            switch (worker.Role)
            {
                case StaffRole.Cook:
                    return Spot(_cookSpots, index);
                case StaffRole.Cashier:
                    return Spot(_cashierSpots, index);
                default:
                    // Đang bưng thì đứng cạnh bàn của khách.
                    if (worker.CustomerId >= 0)
                        foreach (var customer in simulation.Customers)
                            if (customer.Id == worker.CustomerId)
                                return Spot(_tables, Mathf.Max(0, customer.TableIndex)) + Offset(34f, -6f);
                    return Spot(_serverSpots, index);
            }
        }

        // Số bàn hoặc ô vượt số điểm mốc thì dùng chung điểm mốc cuối, chỉ ảnh hưởng hình.
        private static Vector3 Spot(RectTransform[] spots, int index) => spots[Mathf.Min(index, spots.Length - 1)].position;

        // Offset tính bằng pixel của Canvas gốc, nhân theo tỉ lệ thật của phòng.
        private Vector3 Offset(float x, float y) => new Vector3(x, y, 0f) * _actors.lossyScale.x;

        private Actor Create(Image template)
        {
            var go = Instantiate(template.gameObject, _actors, false);
            go.SetActive(true);
            var actor = new Actor { Rect = (RectTransform)go.transform, Image = go.GetComponent<Image>(), Label = go.GetComponentInChildren<Text>() };
            _all.Add(actor);
            return actor;
        }

        private void Place(Actor actor, Vector3 start, Vector3 target)
        {
            actor.Origin = start;
            actor.Target = target;
            actor.Elapsed = _settle ? WalkSeconds : 0f;
            actor.Rect.position = _settle ? target : start;
        }

        // Đổi đích khi chưa tới nơi thì nhảy thẳng tới đích cũ rồi mới đi tiếp.
        private void Retarget(Actor actor, Vector3 target)
        {
            if ((actor.Target - target).sqrMagnitude < 1e-4f) return;
            actor.Origin = actor.Target;
            actor.Target = target;
            actor.Elapsed = 0f;
        }

        private void RemoveUnseen()
        {
            _staleCustomers.Clear();
            foreach (var pair in _customers)
                if (!pair.Value.Seen) _staleCustomers.Add(pair.Key);
            foreach (var id in _staleCustomers) Remove(_customers[id], () => _customers.Remove(id));

            _staleWorkers.Clear();
            foreach (var pair in _workers)
                if (!pair.Value.Seen) _staleWorkers.Add(pair.Key);
            foreach (var id in _staleWorkers) Remove(_workers[id], () => _workers.Remove(id));
        }

        private void Remove(Actor actor, Action unregister)
        {
            unregister();
            _all.Remove(actor);
            Destroy(actor.Rect.gameObject);
        }

        private void Move(float dt)
        {
            foreach (var actor in _all)
            {
                actor.Elapsed += dt;
                actor.Rect.position = Vector3.Lerp(actor.Origin, actor.Target, Mathf.Clamp01(actor.Elapsed / WalkSeconds));
            }
        }

        // Thấp hơn trên màn hình = gần người xem hơn = vẽ sau.
        private void SortByDepth()
        {
            _all.Sort(FrontLast);
            for (var i = 0; i < _all.Count; i++)
                if (_all[i].Rect.GetSiblingIndex() != i) _all[i].Rect.SetSiblingIndex(i);
        }

        private string NameOf(string characterId)
        {
            foreach (var character in _roster.GetOwned())
                if (character.Id == characterId) return character.DisplayName;
            return characterId;
        }

        private static Color StateColor(CustomerState state) => state switch
        {
            CustomerState.Cooking => new Color(0.95f, 0.60f, 0.30f),
            CustomerState.Serving => new Color(0.60f, 0.80f, 0.95f),
            CustomerState.Eating => new Color(0.50f, 0.85f, 0.50f),
            CustomerState.Paying => new Color(0.75f, 0.60f, 0.95f),
            _ => new Color(0.95f, 0.85f, 0.45f),
        };

        private static Color RoleColor(StaffRole role) => role switch
        {
            StaffRole.Cook => new Color(0.90f, 0.45f, 0.40f),
            StaffRole.Server => new Color(0.40f, 0.70f, 0.95f),
            _ => new Color(0.95f, 0.80f, 0.30f),
        };
    }
}
