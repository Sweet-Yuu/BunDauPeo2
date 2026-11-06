# Spec: Sửa layout theo ảnh Mota

> Ngày: 2026-11-06 · Trạng thái: **đã duyệt** (user, 2026-11-06, thiết kế lẫn bản viết; chọn sửa cả 13 mục theo thứ tự đề xuất) · Phạm vi: bố cục các màn đang có (Battle, StageSelect, Gacha, Restaurant) cho gần ảnh trong `Mota/`. Không thêm màn, không đổi luật chơi.
> Nguồn: [ui-screens.md](../../references/ui-screens.md) (ảnh theo màn, "chỉ tham khảo", "luật chơi không lấy từ ảnh"), [spec Combat mục 5](2026-11-02-combat-design.md#5-ui), [spec Gacha mục 5](2026-10-31-gacha-design.md#5-ui-tối-thiểu), [spec reveal mục 3](2026-10-31-gacha-reveal-design.md#3-luồng), [spec Restaurant mục 5.1](2026-11-01-restaurant-design.md#51-màn-quán-restaurantunity). Luật UI: [unity-workflow, UI và hình tạm](../../guides/unity-workflow.md#ui-và-hình-tạm). Kế hoạch: [plans/2026-11-06-layout-mota.md](../plans/2026-11-06-layout-mota.md).
> **Đã xong, đây là lịch sử.** Khi spec khác code hoặc [unity-workflow.md](../../guides/unity-workflow.md), code và unity-workflow thắng; không sửa spec này để theo kịp.

## 1. Mục tiêu

Kênh 📐 chụp mọi màn đang có ở 1920×1080 (`PlaytestTools.Capture1080`, ảnh ở `D:/Unity/bdp-shots/lm-*.png`, ngoài repo) và so với ảnh Mota: bố cục, vị trí khối, thứ tự thông tin, nút chính. Tìm ra 13 chỗ lệch (mục 2), trong đó hai chỗ là lỗi thật: chữ kết quả trận đè lên hình trận, và nút DEV "Cài đặt" che nút "Bỏ qua" của Gacha.

Thành công khi: (1) ảnh chụp lại cả 13 chỗ cho thấy khối nằm đúng vị trí ghi ở mục 4; (2) test EditMode đọc lại scene kiểm khối trong màn và không đè nhau; (3) smoke test PlayMode bắt được nút Cài đặt đè lên cả nút đang tắt; (4) vòng chơi cũ không đổi (mọi test hiện có xanh, chỉ test vị trí được sửa theo bố cục mới).

### Quyết định đã chốt với user (2026-11-06)

| Chủ đề | Quyết định |
|---|---|
| Mục sửa | Cả 13 mục ở mục 2 |
| Thứ tự | Phiên A (K1, K2, G4, S1) → Phiên B (T1, P1, B1, B2) → Phiên C (G1, G2, G3, M1, R1) |
| Màn trận (B1) | Đổi bố cục khỏi phác thảo của spec Combat 5.4 cho theo `battle-wave.jpg`; sửa lại phác thảo đó (mục 6) |
| Roadmap | Mục mới đặt đầu "Tiếp theo", trước "Build Android thử sớm" |

## 2. Bảng chỗ lệch

| # | Màn | Lệch (trước khi sửa) | Ảnh Mota | Công |
|---|---|---|---|---|
| K1 | Kết quả thắng | Nền phủ trong suốt 15 %: chữ thưởng đè lên tên nhân vật phía sau. Thưởng là dòng chữ, spec Combat 5.5 và ảnh đều là ô + số | `battle-result-clear-en.webp` | S |
| K2 | Kết quả thua | Chữ đè hình trận như K1. Hai nút ở giữa; ảnh có Về ở dưới-trái, Thử lại ở dưới-phải | `battle-result-failed-en.webp` | S |
| G4 | Gacha (lỗi) | Nút DEV "Cài đặt" (góc dưới phải) che một phần nút "Bỏ qua" lúc mở 10 lượt. Test L-025 không bắt vì chỉ xét nút đang bật | — | S |
| S1 | Popup màn | Một cột giữa, thưởng bằng chữ, giữa popup trống to; ảnh 2 cột, thưởng là ô, thể lực và nút ở dưới-phải | `quest-stage-info-popup.jpg` | M |
| T1 | Chọn đội | Thẻ ngang to 4 cột; đội hiện tại là ô chữ rộng; nút ở hàng riêng. Ảnh: lưới chân dung vuông, đội là 5 chân dung, nút cùng hàng bên phải | `quest-team-select-popup.jpg` | M |
| P1 | Tạm dừng | HP là năm dòng chữ, thiếu tên màn. Ảnh: tên màn, 5 chân dung có thanh | `battle-pause-popup.jpg` | S–M |
| B1 | Màn trận | Thẻ dồn trái, x1/AUTO/Menu hàng ngang trên phải, Đợt giữa trên. Ảnh: thẻ giữa dưới, AUTO/tốc độ cột phải, wave trái trên, MENU riêng phải trên | `battle-wave.jpg` | M |
| B2 | Màn trận | Quái chỉ có thanh máu; ảnh FGO có tên kèm số HP | `battle-skill-select-en.png` | S |
| G1 | Banner Gacha | Tên banner phải-trên, Gem trái-trên, nút quay giữa-dưới, giữa màn trống. Ảnh: tên trái-trên, số dư phải-trên, hình banner giữa, nút quay phải-dưới | `gacha-banner.jpg` | M |
| G2 | Mở từng thẻ | Thẻ nhỏ giữa, chữ "1 sao". Ảnh: hình to, tên + sao ở phải-dưới | `gacha-reveal-character.webp` | S |
| G3 | Trang tổng | Lưới sát mép trên, ô 330×300, không khung, không tiêu đề | `gacha-result-10.jpg` | S |
| M1 | Bản đồ | Ổ khoá là mảng đen lệch dưới-phải mỗi nút, trông như lỗi; bản đồ chỉ dùng dải y 380–700 | `quest-stage-map.jpg` | S |
| R1 | Quán | Thanh thông tin (khách, chất lượng) đè lên mép tường sau phòng; ba thẻ tiền rộng 340 | `restaurant-layout-garden-en.webp` | S |

Không so (không có ảnh Mota): panel Nhân viên, Nâng cấp, Lịch sử Gacha, popup Quay tiếp, popup Truyện, panel Cài đặt DEV. Chưa chụp: trận boss (chỉ ở 1-5); các khối boss không đổi vị trí ở spec này.

**Màn chưa có trong game** (không làm ở đây, ghi để biết): Title (`title-touch-to-start.jpg`), Hub và thanh điều hướng dưới (`hub-home*`; hiện mọi màn chỉ tới qua nút DEV Cài đặt), chọn chế độ Quest, danh sách nhân vật, nâng cấp nhân vật, hội thoại. **Không lấy** (ngoài phạm vi game): trang MVP, popup thống kê sát thương, 3 sao, độ khó HARD, nút SET, EX trang bị, bộ lọc nguyên tố, nhiều banner.

## 3. Nguyên tắc chung

- **Chỉ sửa builder và view**, rồi dựng lại scene bằng batch mode với Editor đóng (`unity run . -- -executeMethod BunDauPeo.EditorTools.ProjectSetup.Rebuild…Scene`). Không sửa tay `.unity`, không đụng `.meta` ([unity-workflow, Luật với file Unity](../../guides/unity-workflow.md#luật-với-file-unity)).
- **Gacha chưa có lệnh dựng lại**: `GachaSceneBuilder.Create` chỉ chạy khi scene chưa có. Thêm `ProjectSetup.RebuildGachaScene` và mục menu `Tools/BunDauPeo/Rebuild Gacha Scene`, cùng khuôn ba lệnh kia.
- **Hình chỉ dùng `Image`** với sprite có sẵn (`UISprite`, `Knob`, hình vuông đặc), chữ là `Text` uGUI. Không thêm file ảnh. Ô giữ chỗ hình banner (G1) là `Image` vuông tô màu.
- **Toạ độ** ghi dưới đây theo khung tham chiếu 1920×1080, gốc **trái trên**, y tính xuống (như `BattleLayout`). Builder đổi sang anchor tương ứng. Con số là đích; worker được lệch ±20 px nếu test chứng minh không đè.
- **Không đổi luật**: mọi luồng (trừ thể lực, Thử lại, Bỏ cuộc, quay tiếp, phân trang) giữ nguyên. Thay đổi code ngoài builder chỉ là: thêm `RewardRow` (mục 4.1), đổi số thẻ mỗi trang (T1), thêm ô HP vào popup tạm dừng (P1), số HP quái (B2), chữ sao (G2).
- **Test**: mỗi màn sửa có test EditMode đọc lại scene (khuôn `RestaurantSceneTests.InReferenceCanvas`): khối nằm trong 1920×1080, các nút không đè nhau, chữ nằm trong hộp của nó. Test vị trí cũ mâu thuẫn bố cục mới thì sửa theo spec này, không xoá.

## 4. Thiết kế từng mục

### 4.1 `RewardRow` (dùng chung cho K1 và S1)

Component mới trong assembly `UI`: một hàng tối đa 4 ô vuông 96×96, mỗi ô là `Image` tô màu theo loại tiền và một `Text` số ở dưới. Có một `Text` nhãn bên trái hàng ("Lần đầu", "Đánh lại", hoặc rỗng).

- `Show(IReadOnlyList<CurrencyAmount> lines, string label)`: bật đủ ô cho `lines`, tắt ô thừa; số ô cần lớn hơn 4 thì chỉ hiện 4 ô đầu (`shortcut:` ghi trong code; nới khi có màn thưởng hơn 4 loại).
- Màu và chữ nằm trong hàm tĩnh `CombatViewText.CurrencyColor(CurrencyType)` và `CombatViewText.CurrencyCell(CurrencyAmount)` (vd. "30 Material"), có test. Coin vàng, Material nâu, Gem xanh lam.
- Builder dựng bằng một hàm chung `BuildRewardRow(parent, …)` đặt trong `GachaSceneBuilder` cạnh `NewButton`/`NewText` (hai builder Combat đã `using static` nó).

### 4.2 Phiên A

**K1, K2 — Kết quả trận** (`BattleSceneBuilder.BuildResultView`, `BattleResultView`)

- Thêm `ResultBox` 1400×760 đặc (`CardColor`, alpha 1) giữa màn, trên nền phủ hiện có. Mọi chữ và nút nằm trong hộp; nền phủ giữ alpha 0.85.
- Thắng: banner "CLEAR" ở đỉnh hộp; `RewardRow` thay `_rewardText` khi `Status == Success` (nhãn "Lần đầu" nếu thưởng lần đầu); dòng thể lực đã dùng, cảnh báo lưu; nút Xác nhận ở đáy hộp, giữa. `NotPaid` (trận DEV) giữ dòng chữ "Trận thử, không có thưởng" ở vị trí hàng ô.
- Thua: banner "Thất bại" ở đỉnh hộp; gợi ý và dòng thể lực canh trái; **Về bản đồ dưới-trái, Thử lại dưới-phải** của hộp; dòng lý do Thử lại ngay trên nút Thử lại.
- Dữ liệu lấy từ `StageCompleteResult` đang có: `Rewards` cho các ô, `FirstClear` cho nhãn ("Lần đầu" hoặc rỗng). `CombatViewText.ResultRewards` hết dùng thì xoá cùng test của nó.
- Test: chữ và nút của kết quả nằm trong `ResultBox`; Xác nhận, Thử lại, Về bản đồ không đè nhau; `RewardRow` nối dây.

**G4 — Nút Bỏ qua bị che** (`GachaSceneBuilder`, `SmokeLoopTests`)

- Dời `SkipButton` lên **góc trên phải** (300×90, cách mép 40). `RevealRoot` phủ kín màn với nền đặc nên không đè chữ banner.
- Sửa gốc lỗi: `AssertSettingsButtonClear` xét cả nút **đang tắt** (`FindObjectsInactive.Include`), vì nút Bỏ qua, nút trong popup và kết quả chỉ bật khi cần. Test bỏ qua nút có `rootCanvas` là `DevOverlay` như cũ.
- Ghi một mục lesson (#ui #process): test đè nút chỉ xét nút đang bật thì bỏ sót nút chỉ hiện trong luồng (L-025 lặp lại).

**S1 — Popup màn** (`StageSelectSceneBuilder.BuildInfoPopup`, `StageInfoPopup`)

- Hộp 1600×820 giữa màn. Tên màn canh trái ở đỉnh hộp (x hộp + 60).
- Cột trái (x hộp + 60 … + 1000): nhãn "Quái" rồi tối đa 4 hàng đợt (nhãn "Đợt n" và 5 ô như hiện nay); dưới đó `RewardRow` với nhãn lấy từ `CombatViewText.RewardHeader(firstClear)` đang có (thay `_rewardHeader` + `_rewardLine`; `RewardLine` còn dùng ở chỗ khác thì giữ).
- Khối phải dưới (x hộp + 1060 … + 1540): dòng thể lực "Thể lực 100 → 90" (chữ `CombatViewText.StaminaChange` giữ nguyên), dòng cảnh báo không hoàn, dòng chờ thể lực; nút **Hủy** và **Thách đấu** cạnh nhau ở đáy hộp, góc phải.
- Không thêm hàng vật phẩm rơi, sao, "auto tiến màn" (game không có).
- Test: mọi chữ và nút trong hộp; hai nút không đè nhau và nằm ở nửa phải hộp; `RewardRow` nối dây.

### 4.3 Phiên B

> **Sửa 2026-11-06 (user, sau khi xem ảnh Phiên B):** màn trận (B1, B2) đổi lại như sau, thắng các toạ độ cũ bên dưới: mỗi phe là lưới 3×3 (cột x 320/540/760 và 1180/1400/1620; hàng y 280/520/760; Front và Mid ở hàng trên và dưới, Back ở giữa; người 130×130; boss 280×320 tại (1180, 400)), giãn dọc để sau có chỗ hiện buff/debuff dưới mỗi người; **bỏ hàng thẻ dưới màn**, số HP dưới tên cả hai phe; x1, AUTO, Menu một hàng ở góc trên phải; nút Cài đặt DEV ẩn ở màn trận và chuyển vào Menu tạm dừng (nút "Cài đặt"). Chi tiết ở spec Combat 5.4.

**T1 — Chọn đội** (`StageSelectSceneBuilder.BuildTeamPopup`, `BuildTeamCard`, `TeamCardView`, `TeamSelectPopup`)

- Hộp 1700×880 giữ nguyên. Lưới **chân dung vuông** 150×190 (ô màu class 150×150, tên dưới, dòng sao trong ô), **8 cột × 2 hàng = 16 thẻ mỗi trang**: `TeamSelectPopup.CardsPerPage` 12 → 16. Người trong đội: viền vàng và dấu tích góc trên phải (dựng như dấu tích nút bản đồ); nhân viên quán: nhãn "Quán: <vai>" thu gọn trong ô.
- Phân trang `<  1/1  >` dưới lưới như cũ.
- Hàng dưới, bên trái: "Thành viên hiện tại (n/5)" và 5 chip **vuông 150×120** (ô màu class, tên, class) kèm nhãn Hàng trước / Hàng giữa / Hàng sau trên nhóm của nó. Bên phải cùng hàng: **Hủy** và **Vào trận** xếp cạnh nhau (mỗi nút 300×100).
- Hai hộp xác nhận (đội dưới 5 người, nhân viên quán) không đổi.
- Test: 16 thẻ trong hộp, không đè nhau; chip và hai nút cùng hàng không đè nhau; test phân trang hiện có sửa theo 16.

**P1 — Tạm dừng** (`BattleSceneBuilder.BuildPausePopup`, `BattlePausePopup`)

- Hộp 1100×760. Tiêu đề "Tạm dừng"; dòng tên màn (`session.Stage.DisplayName`) canh trái dưới tiêu đề; nhãn "Tình trạng đội".
- Năm `_hpLines` thay bằng **5 thẻ `BattleCardView`** dựng bằng `BuildCard` (cùng thẻ dưới trận) xếp hàng ngang trong hộp; popup gọi `Bind(unit)` và `Refresh(unit.Hp, maxEnergy, false)`; nút của thẻ `interactable = false`. `CombatViewText.PauseHpLine` không còn dùng thì xoá cùng test của nó.
- Tiếp tục và Bỏ cuộc ở đáy hộp như cũ; hộp xác nhận Bỏ cuộc không đổi. Không thêm AUTO/x2 vào popup (thanh trận đã có).
- Test: 5 thẻ trong hộp, không đè nhau; popup vẫn chặn chạm và tắt sẵn.

**B1 — Màn trận** (`BattleSceneBuilder.BuildTopBar`, `BuildCard`, `BuildActionPanel`)

- **Góc trên trái**: chữ "Đợt n/m" (x 40, y 20) và thanh lượt ngay dưới (x 40, y 90); dòng "Lượt của X" (`_turnLine`, vẫn là con của `ActionPanel`) đặt dưới thanh lượt (y 150), canh trái.
- **Góc trên phải**: chỉ nút Menu 150×110.
- **Cột phải**: AUTO và x2 xếp dọc, mỗi nút 150×110, x 1730–1880, y 560–670 và 690–800 (trên ba nút chiêu, ngoài hộp quái: quái phải nhất kết thúc ở x 1690).
- **Thẻ 5 nhân vật**: giữ cỡ 180×150 và khoảng 192, dời vào giữa: x 480 + i × 192 (480 … 1440), y vẫn trong dải `CardAreaTop`–`CardAreaBottom`.
- **Ba nút chiêu** giữ ở góc phải dưới (x 1520–1880).
- `BattleLayout` (vị trí nhân vật và quái) **không đổi**. Sửa test: `BattleScene_TopBarWired_InsideCanvas` (Menu và thanh lượt ở dải trên, AUTO và x2 ở cột phải, không đè hộp quái nào của `BattleLayout`), `BattleScene_CardsAndButtonsInCardArea` (thẻ x 480–1440, không đè nút chiêu).

**B2 — Số HP quái** (`BattleSceneBuilder.BuildUnit`, `BattleUnitView`)

- Thêm `Text` `HpText` (22 px) ngay dưới tên của **quái**; phe ta không có (thẻ đã có số HP). `BattleUnitView` cập nhật chữ theo `ShownHp` cùng lúc với thanh máu, dùng `CombatViewText.UnitHp`.
- Field mới để trống ở đồng minh: view chỉ ghi khi field có giá trị (thiếu field ở đồng minh không phải lỗi; kiểm tra tham chiếu của view phải cho phép điều này).
- Test: quái có `HpText` nối dây, đồng minh không có; chữ khớp `UnitHp` sau `ApplyDelta`.

### 4.4 Phiên C

**G1 — Trang banner** (`GachaSceneBuilder`)

- **Trái trên**: khung `BannerPanel` 760×150 (nền `Panel`): tên banner 48 px và dòng "SSR đảm bảo sau N lượt" bên dưới (đổi chỗ hai chữ đang ở phải trên, giữ tên field).
- **Phải trên**: thẻ số Gem 360×80.
- **Giữa**: ô giữ chỗ `BannerArt` 900×560 (`Image` vuông, màu xám nhạt), tâm hơi lệch trái (x 860, y 520), không chặn chạm.
- **Phải dưới**: hai nút quay dạng thẻ 300×160, chữ hai dòng "Quay 1" / "100 Gem" và "Quay 10" / "900 Gem" (`GachaViewText.PullLabel` thêm xuống dòng; test chữ sửa theo). Nút Quay 10 ở phải nhất.
- Nút **Lịch sử** 300×80 ở phải, trên hai nút quay; dòng thông báo trên hai nút quay, canh phải.
- Không làm cột danh sách banner (chỉ có một banner), không làm nút "chi tiết nhân vật/gacha".

**G2 — Mở từng thẻ** (`GachaSceneBuilder`, `GachaViewText.StarsText`)

- `RevealPanel` phủ gần kín màn (1800×1000). `Portrait` lớn 760×760 lệch trái (tâm x 700). Tên 64 px và dòng sao ở **góc phải dưới** panel, canh phải.
- `StarsText(n)` trả n ký tự "★" thay "n sao" (font `LegacyRuntime` đã hiện được "★" ở thẻ chọn đội); test `StarsText_ShowsCount` sửa theo.
- Cutscene và flash không đổi.

**G3 — Trang tổng** (`GachaSceneBuilder`)

- Khung `TotalPanel` 1300×640 giữa màn (nền `Panel`), tiêu đề "Kết quả quay" canh trái trên khung.
- Lưới trong khung: ô 200×220, khoảng cách 30, 5 cột; chân dung 160×140, nhãn MỚI/TRÙNG dưới. Nút OK 360×90 ngay dưới khung.
- Icon vẫn tô màu theo độ hiếm và hiện lần lượt như cũ (`GachaTotalView` không đổi).

**M1 — Bản đồ** (`StageSelectSceneBuilder.BuildNode`, `StageMapView.NodeCenter`)

- Ổ khoá: dời lên **đỉnh giữa** nút (tâm cách tâm nút 60 px lên trên), thân khoá màu vàng (`Gold`), quai tối; nhãn "1-3" vẫn đọc được.
- `NodeCenter`: biên độ đường gấp khúc giãn để dùng chiều cao màn: y 720 cho chỉ số chẵn, 400 cho chỉ số lẻ (x giữ 240 + 240 × i). Chữ "NEXT" vẫn đặt trên nút.
- Test `NodeCenters_SevenNodes_InsideMapArea_NoOverlap` sửa theo; thêm: ổ khoá nằm trong biên màn và không đè nút bên cạnh.

**R1 — Quán** (`RestaurantSceneBuilder`)

- Ba thẻ tiền rộng 260 (thay 340).
- `InfoBar` dời lên **hàng trên cùng**, bên phải ba thẻ tiền (x ≈ 860 … 1700, y 20 … 140), không chạm cột nút phải (bắt đầu ở x 1730) và không chạm sàn/tường phòng.
- `MessageRoot` đặt ngay dưới `InfoBar` (y 150 … 206), cùng bề ngang.
- Test: `RestaurantScene_HudAndColumnInsideCanvas` kiểm thêm `InfoBar` và `MessageRoot` không đè hộp phòng (`Room`) và cột phải.

## 5. Test

- EditMode: theo từng mục ở mục 4; mỗi task kết thúc bằng `unity test . --mode EditMode` xanh (Editor đóng, vì phải dựng lại scene ở batch mode trước).
- PlayMode: `SmokeLoopTests` chạy cuối mỗi phiên đụng Gacha hoặc Battle (A, B, C), xanh cả 12 test hiện có; `AssertSettingsButtonClear` mới xét cả nút tắt (G4).
- Dừng playtest cuối mỗi phiên: chụp lại các màn đã sửa bằng quy trình "Công cụ thử tự động" (`BackupSave` → `OpenBoot` → Play → `Capture1080` → `RestoreSave`), ảnh ra `D:/bdp_BunDauPeo/shots/`, user xem và so với Mota.

## 6. Tài liệu cần sửa khi làm

- [spec Combat](2026-11-02-combat-design.md) mục 5.4: phác thảo bố cục màn trận theo B1 (Đợt và thanh lượt trái trên, Menu phải trên, AUTO/x2 cột phải, thẻ giữa). Mục 5.3 và 5.5 thêm một dòng trỏ về spec này.
- [ui-screens.md](../../references/ui-screens.md): không đổi (không thêm, đổi tên ảnh).
- [unity-workflow.md](../../guides/unity-workflow.md): dòng "Dựng lại scene bằng builder" thêm `RebuildGachaScene`.
- [lesson.md](../../lessons/lesson.md): mục G4 (mục 4.2).
- [roadmap.md](../../roadmap.md): mục mới đầu "Tiếp theo" (kênh 📐 thêm), "Đang làm" do worker cập nhật.

## 7. Ngoài phạm vi

Màn chưa có (mục 2), art thật, hoạt hình mới, chỉnh bố cục ở tỉ lệ khác 16:9 (chỉ giữ không vỡ như hiện nay), Song ngữ (chữ mới viết tiếng Việt như cũ; mục Song ngữ của roadmap sẽ dịch sau).

## 8. Rủi ro

- **Dựng lại Gacha** có thể làm mất tham chiếu đã chỉnh tay trong scene (nếu có). Kế hoạch chạy test `GachaSceneTests` và `GachaRevealAssetTests` ngay sau lần dựng đầu, trước khi đổi bố cục, và so `git diff --stat` của `Gacha.unity`.
- **Đổi 12 → 16 thẻ mỗi trang** chạm test phân trang và smoke test (bấm `TeamCard0…4`); tên thẻ giữ `TeamCard{i}`.
- **Batch mode và Editor mở**: dựng lại scene cần Editor đóng; worker không kill Unity, nhờ user đóng.
- Chưa kiểm cutscene mở Gacha có chạy đủ 3,5 giây không (ảnh chụp lúc rà thấy thẻ ngay sau 1 giây); không thuộc spec này, ghi để 🔍 hoặc playtest để ý.
