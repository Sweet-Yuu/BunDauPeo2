using System;
using System.Collections.Generic;
using BunDauPeo.Characters;
using BunDauPeo.Combat;
using BunDauPeo.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BunDauPeo.UI
{
    // Bản đồ màn (spec Combat 5.2): nút dựng lúc chạy từ một nút mẫu cho từng màn trong catalog, nối bằng đoạn Image xoay.
    // Chỉ vẽ và mở popup; luật mở màn, thể lực, thưởng nằm ở StageProgressService và CombatService.
    public class StageMapView : MonoBehaviour
    {
        private const float HintSeconds = 2f, NodeSize = 120f, LinkThickness = 14f;
        private static readonly Color LinkIdle = new(0.35f, 0.36f, 0.42f);
        private static readonly Color LinkDone = new(0.30f, 0.70f, 0.40f);

        [SerializeField] private RectTransform _nodesRoot;
        [SerializeField] private StageNodeView _nodeTemplate;
        [SerializeField] private Image _linkTemplate;
        [SerializeField] private RectTransform _nextMarker;
        [SerializeField] private Text _hintText;
        [SerializeField] private Text _invalidText;
        [SerializeField] private StageInfoPopup _info;
        [SerializeField] private StoryPlaceholderPopup _story;
        [SerializeField] private TeamSelectPopup _team;

        private readonly List<StageNodeView> _nodes = new();
        private readonly List<Image> _links = new();

        private CombatService _service;
        private Wallet _wallet;
        private Roster _roster;
        private bool _ready;
        private bool _built;
        private bool _dirty = true;
        private float _hintLeft;
        private CanvasGroup _markerGroup;

        // Gốc trái trên, đơn vị 1920×1080: zíc-zắc giữa y 400 và 720, cách nhau 240 theo x. shortcut: đủ cho một chương 7 nút;
        // nhiều chương thì cuộn hoặc chia trang.
        public static Vector2 NodeCenter(int index) => new(240f + 240f * index, index % 2 == 0 ? 720f : 400f);

        // Màn mở đầu tiên chưa clear (nơi hiện NEXT); -1 khi không còn.
        public static int NextStageIndex(StageProgressService progress, StageCatalog catalog)
        {
            if (progress == null || catalog == null) return -1;

            for (var i = 0; i < catalog.Items.Count; i++)
            {
                var stage = catalog.Items[i];
                if (stage != null && progress.GetStatus(stage.Id) == StageStatus.Available) return i;
            }

            return -1;
        }

        private void OnEnable()
        {
            if (!HasAllReferences()) return;

            try
            {
                _service = ServiceLocator.Get<CombatService>();
                _wallet = ServiceLocator.Get<Wallet>();
                _roster = ServiceLocator.Get<Roster>();
            }
            catch (InvalidOperationException)
            {
                // Mở không qua Boot: không có dịch vụ thì chỉ hiện dòng cấu hình lỗi.
                _service = null;
                _wallet = null;
                _roster = null;
            }

            _info.Challenge += OnChallenge;
            if (_service != null) _service.Progress.Changed += MarkDirty;
            if (_wallet != null) _wallet.Changed += MarkDirty;
            _markerGroup = _nextMarker.GetComponent<CanvasGroup>();
            _dirty = true;
            _ready = true;
        }

        private void OnDisable()
        {
            _ready = false;
            if (_info != null) _info.Challenge -= OnChallenge;
            if (_service != null) _service.Progress.Changed -= MarkDirty;
            if (_wallet != null) _wallet.Changed -= MarkDirty;
        }

        private bool HasAllReferences()
        {
            var ok = _nodesRoot != null
                     && _nodeTemplate != null && _linkTemplate != null && _nextMarker != null && _hintText != null && _invalidText != null
                     && _info != null && _story != null && _team != null;
            if (!ok) Debug.LogError($"{nameof(StageMapView)}: references are not assigned.", this);
            return ok;
        }

        private void MarkDirty() => _dirty = true;

        private void Update()
        {
            if (!_ready) return;

            var running = _service != null && _service.IsRunning;
            _invalidText.gameObject.SetActive(!running);
            _invalidText.text = CombatViewText.InvalidConfigLine();
            if (!running)
            {
                _nextMarker.gameObject.SetActive(false);
                return;
            }

            if (!_built) Build();
            if (_dirty) Redraw();

            if (_hintLeft > 0f)
            {
                _hintLeft -= Time.unscaledDeltaTime;
                if (_hintLeft <= 0f) _hintText.gameObject.SetActive(false);
            }

            if (_markerGroup != null && _nextMarker.gameObject.activeSelf)
                _markerGroup.alpha = 0.35f + 0.65f * Mathf.PingPong(Time.unscaledTime * 1.5f, 1f);
        }

        // Một lần: liên kết trước, nút sau, để nút vẽ đè lên đoạn nối.
        private void Build()
        {
            _built = true;
            var items = _service.Config.Stages.Items;
            for (var i = 0; i + 1 < items.Count; i++)
            {
                var a = BattleLayout.ToAnchored(NodeCenter(i));
                var b = BattleLayout.ToAnchored(NodeCenter(i + 1));
                var link = Instantiate(_linkTemplate, _nodesRoot);
                link.name = $"Link{i}";
                var rect = link.rectTransform;
                rect.anchoredPosition = (a + b) / 2f;
                rect.sizeDelta = new Vector2((b - a).magnitude, LinkThickness);
                rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg);
                link.gameObject.SetActive(true);
                _links.Add(link);
            }

            for (var i = 0; i < items.Count; i++)
            {
                var node = Instantiate(_nodeTemplate, _nodesRoot);
                node.name = $"Node{i}";
                ((RectTransform)node.transform).anchoredPosition = BattleLayout.ToAnchored(NodeCenter(i));
                var captured = node;
                node.Button.onClick.AddListener(() => OnNodeClicked(captured));
                node.gameObject.SetActive(true);
                _nodes.Add(node);
            }

            // Marker nằm sau cùng để vẽ trên nút.
            _nextMarker.SetAsLastSibling();
        }

        private void Redraw()
        {
            _dirty = false;
            var items = _service.Config.Stages.Items;
            for (var i = 0; i < _nodes.Count; i++)
                _nodes[i].Bind(items[i], _service.Progress.GetStatus(items[i].Id));
            for (var i = 0; i < _links.Count; i++)
                _links[i].color = _service.Progress.GetStatus(items[i].Id) == StageStatus.Cleared ? LinkDone : LinkIdle;

            var next = NextStageIndex(_service.Progress, _service.Config.Stages);
            _nextMarker.gameObject.SetActive(next >= 0);
            if (next >= 0) _nextMarker.anchoredPosition = BattleLayout.ToAnchored(NodeCenter(next)) + new Vector2(0f, NodeSize / 2f + 50f);
        }

        private void OnNodeClicked(StageNodeView node)
        {
            if (node.Stage == null || _service == null) return;

            switch (_service.Progress.GetStatus(node.Stage.Id))
            {
                case StageStatus.Locked:
                    _hintText.text = CombatViewText.LockedHint();
                    _hintText.gameObject.SetActive(true);
                    _hintLeft = HintSeconds;
                    break;
                case StageStatus.Available:
                case StageStatus.Cleared:
                    if (node.Stage.Kind == StageKind.Battle) _info.Open(_service, node.Stage);
                    else _story.Open(_service, node.Stage);
                    break;
            }
        }

        // Thách đấu mở popup chọn đội; chỉ nó gọi EnterStage (kiểm, trừ thể lực, lưu), khi người chơi bấm Vào trận.
        private void OnChallenge(StageDefinition stage)
        {
            _info.Close();
            _team.Open(_service, _roster, stage);
        }
    }
}
