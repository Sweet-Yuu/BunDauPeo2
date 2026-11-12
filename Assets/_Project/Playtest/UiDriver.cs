using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace BunDauPeo.Playtest
{
    public enum ClickStatus { Clicked, NotFound, Ambiguous, NotInteractable, Blocked }

    public readonly struct ClickResult
    {
        public ClickStatus Status { get; }
        public string Message { get; }

        public ClickResult(ClickStatus status, string message)
        {
            Status = status;
            Message = message;
        }

        public override string ToString() => $"{Status}: {Message}";
    }

    // Bấm nút như người chơi: qua raycast tại tâm nút, nên nút bị popup che sẽ báo Blocked thay vì bị bấm xuyên.
    public static class UiDriver
    {
        public static IReadOnlyList<Button> FindButtons(string name) => Match(name).Where(Usable).ToList();

        public static ClickResult Click(string name)
        {
            var matches = Match(name);
            if (matches.Count == 0) return new ClickResult(ClickStatus.NotFound, $"không có nút '{name}'. Nút đang hiện: {DescribeVisibleButtons()}");

            var usable = matches.Where(Usable).ToList();
            if (usable.Count == 0) return new ClickResult(ClickStatus.NotInteractable, $"nút '{name}' đang bị tắt (interactable = false): {PathOf(matches[0].transform)}");
            if (usable.Count > 1) return new ClickResult(ClickStatus.Ambiguous, $"{usable.Count} nút khớp '{name}': {string.Join(" | ", usable.Select(b => PathOf(b.transform)))}");

            return Click(usable[0], name);
        }

        // Bấm đúng nút này (khi nhiều nút trùng tên, ví dụ 5 dòng của hộp Thông báo): cùng đường raycast thật của Click(name).
        public static ClickResult Click(Button button, string name = null)
        {
            name ??= button.name;
            var es = EventSystem.current;
            if (es == null) return new ClickResult(ClickStatus.Blocked, "không có EventSystem đang chạy");

            var canvas = button.GetComponentInParent<Canvas>()?.rootCanvas;
            var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            var rect = (RectTransform)button.transform;
            var pointer = new PointerEventData(es)
            {
                position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center))
            };

            var hits = new List<RaycastResult>();
            es.RaycastAll(pointer, hits);
            if (hits.Count == 0) return new ClickResult(ClickStatus.Blocked, $"raycast tại tâm nút {pointer.position} không trúng gì (toạ độ sai?): {PathOf(button.transform)}");
            if (!hits[0].gameObject.transform.IsChildOf(button.transform))
                return new ClickResult(ClickStatus.Blocked, $"nút '{name}' bị che bởi {PathOf(hits[0].gameObject.transform)}");

            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            return new ClickResult(ClickStatus.Clicked, PathOf(button.transform));
        }

        // Giờ thật, không phụ thuộc timeScale; hết giờ ném lỗi kèm danh sách nút đang hiện để biết đang kẹt ở màn nào.
        public static IEnumerator WaitForButton(string name, float timeoutSeconds)
        {
            var deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (FindButtons(name).Count == 0)
            {
                if (Time.realtimeSinceStartup > deadline)
                    throw new TimeoutException($"Hết {timeoutSeconds}s chờ nút '{name}'. Nút đang hiện: {DescribeVisibleButtons()}");
                yield return null;
            }
        }

        public static string DescribeVisibleButtons()
        {
            var parts = Visible().Select(b =>
            {
                var label = LabelOf(b);
                return label.Length > 0 ? $"{b.name} ({label})" : b.name;
            });
            return string.Join(", ", parts);
        }

        // Khớp tên GameObject trước; không có thì khớp chữ Text con (nút DevOverlay chỉ có chữ).
        private static List<Button> Match(string name)
        {
            var visible = Visible();
            var byName = visible.Where(b => b.name == name).ToList();
            return byName.Count > 0 ? byName : visible.Where(b => LabelOf(b) == name).ToList();
        }

        private static List<Button> Visible() =>
            Object.FindObjectsByType<Button>(FindObjectsInactive.Exclude, FindObjectsSortMode.None)
                .Where(b => b.gameObject.activeInHierarchy)
                .OrderBy(b => PathOf(b.transform), StringComparer.Ordinal)
                .ToList();

        // IsInteractable tính cả CanvasGroup; IsActive loại Button bị tắt. Cờ interactable riêng của nút không đủ.
        private static bool Usable(Button b) => b.IsActive() && b.IsInteractable();

        private static string LabelOf(Button b) => b.GetComponentInChildren<Text>()?.text ?? "";

        private static string PathOf(Transform t)
        {
            var path = t.name;
            for (var p = t.parent; p != null; p = p.parent) path = p.name + "/" + path;
            return path;
        }
    }
}
