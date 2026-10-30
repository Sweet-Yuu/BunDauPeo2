# Điều hướng agent: model, effort, subagent hay native

> **Đọc khi:** bắt đầu một đợt triển khai, chuẩn bị gọi subagent, hoặc đang sửa mãi không xong.
> **Cập nhật khi:** dùng thực tế thấy một mức quá yếu hoặc quá tốn. Đây là khuyến nghị ban đầu, chưa đo chi phí thật.

## Native hay subagent

**Mặc định: Native + một reviewer độc lập ở cuối nhánh.** Mỗi việc lớn có một session code riêng, tự làm từng task theo kế hoạch, rồi một subagent mới review toàn nhánh trước khi coi là xong.

**Cách chia session (user chốt 2026-10-30):**
- **Session điều phối:** hỏi đáp tổng quan và viết prompt cho session code. Không viết code game.
- **Session code:** mỗi session nhận một prompt từ session điều phối. Chỉ chạy **một session đụng Unity tại một thời điểm**.
- Session không chia sẻ trí nhớ. Tiến độ truyền qua checkbox trong kế hoạch, `docs/roadmap.md` và git.

Lý do riêng của dự án này:
- Một project Unity chỉ mở được một Editor hoặc một lệnh batch cùng lúc. Các agent song song sẽ tranh project khi chạy test.
- Worktree riêng phải tự import lại `Library/`, tốn thời gian.
- Task nối tiếp nhau và dùng chung interface, ít việc tách song song được.

**Dùng subagent khi:**
| Tình huống | Cách dùng |
|---|---|
| Tìm kiếm rộng, chỉ cần kết luận | Agent `Explore` |
| Review độc lập cuối nhánh | Subagent mới, không biết quá trình code |
| Kế hoạch trên 10 task, có logic dễ sai (AFK, combat), và bạn muốn review sau từng task | `superpowers:subagent-driven-development`, nhưng vẫn chạy tuần tự |

**Không dùng subagent để:** chạy song song nhiều task đụng Unity, hoặc làm việc nhỏ vài lệnh.

## Bảng chọn model và effort

Mức effort: `low`, `medium`, `high`, `xhigh`, `max`.

| Loại việc | Model | Effort | Ví dụ trong dự án |
|---|---|---|---|
| Điều phối, viết prompt, hỏi đáp tổng quan | Sonnet 5.5 | medium | Session điều phối |
| Tra cứu, đọc log, tìm file | Haiku 5.5 | low | Tìm nơi dùng `Wallet`, đọc log Unity |
| Sửa docs nhỏ, cập nhật roadmap | Haiku 5.5 | low | Đánh dấu task xong |
| Tạo SO, thêm số liệu, asmdef, cây thư mục | Sonnet 5.5 | low | Thêm nhân vật mới, chỉnh `ProjectSetup` |
| Viết code theo kế hoạch đã chốt | Sonnet 5.5 | medium | `Roster`, `Wallet`, view của quán |
| Logic dễ sai, ảnh hưởng tiến độ người chơi | Sonnet 5.5 | high | `SaveService`, AFK, roll gacha, `BattleSession` |
| Thao tác Editor, scene, ProjectSettings | Sonnet 5.5 | medium | `unity command`, Player Settings |
| Debug lỗi rõ triệu chứng | Sonnet 5.5 | high | Test đỏ, lỗi compile |
| Debug lỗi khó hoặc đã sửa 2 lần không xong | Opus 5.5 | xhigh | Lỗi chỉ xuất hiện khi Play |
| Viết spec, brainstorm, viết kế hoạch | Opus 5.5 | high | Spec Combat, spec Restaurant |
| Review từng task | Sonnet 5.5 | medium | Khi dùng subagent-driven |
| Review cuối nhánh | Opus 5.5 | high | Mọi nhánh trước khi coi là xong |
| Review phần tiền, save, gacha, AFK | Opus 5.5 | xhigh | Thêm cho các nhánh đụng các phần đó |
| Cân bằng số liệu | Sonnet 5.5 | medium | Tỉ lệ gacha, hệ số AFK |

Fable 5.1 và effort `max` không dùng mặc định: chỉ dùng khi Opus `xhigh` đã thất bại, hoặc khi bạn yêu cầu. Chưa có số liệu so sánh chi phí cho dự án này.

## Model của phiên chính

- Lập spec và kế hoạch: Opus 5.5.
- Làm theo kế hoạch: đổi sang Sonnet 5.5, vì kế hoạch đã chứa quyết định thiết kế.
- Model phiên chính do bạn đổi bằng `/model`. Model và effort của subagent được đặt khi gọi.

## Quy tắc tăng mức

1. Sai hai lần cùng một việc: tăng một bậc effort, hoặc từ Sonnet lên Opus.
2. Phát hiện kế hoạch sai hoặc thiếu: dừng, quay lại Opus sửa kế hoạch, không tự thiết kế lại trong lúc code.
3. Việc đụng tới mất dữ liệu hoặc tiền trong game: không dùng Haiku, kể cả khi trông nhỏ.
4. Việc đã rõ và lặp lại: giảm mức, không giữ mức cao theo thói quen.
