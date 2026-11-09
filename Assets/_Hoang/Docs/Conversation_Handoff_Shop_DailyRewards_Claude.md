# BunDauPeo — Bàn giao Shop và Daily Rewards để remake bằng ChatGPT PLUS

Ngày tổng hợp: **07/11/2026**, múi giờ Việt Nam. Ngôn ngữ tài liệu: tiếng Việt. Ngôn ngữ giao diện Shop và Daily Rewards: **tiếng Anh**.

## 1. Mục đích và phạm vi

Đây là bản mô tả độc lập để người nhận hiểu và remake phần Shop, điểm danh hàng ngày đã thực hiện trong cuộc hội thoại giữa Hoàng và Codex. Người nhận có thể đưa file này cho ChatGPT PLUS cùng source dự án. Tài liệu mô tả hành vi cần giữ, cách tích hợp, những gì đã kiểm tra và những gì chưa có.

Đã rà soát **43 file Markdown của repository** trước khi tạo bản này, gồm cả file chưa được Git theo dõi. Phạm vi khảo sát không bao gồm tài liệu sinh tự động trong Library, Temp, Logs, obj, UserSettings hoặc dữ liệu nội bộ Git. Danh mục nguồn nằm cuối tài liệu. Đã đối chiếu trực tiếp code Shop/Daily, save economy, test, scene hierarchy, package và Build Settings tại HEAD `b2ecb20`.

**Phân biệt nguồn:** phần Shop/Daily và các yêu cầu tiếp theo về scene, tiếng Anh là công việc được xác nhận trong cuộc hội thoại này. Skill Tree, Facilities, Power Grid, Buff Bridge, tooltip, combat, gacha và workflow Google Sheets có tài liệu/code sẵn trong repository; không vì vậy mà quy toàn bộ chúng thành công việc đã làm trong cuộc hội thoại này. Mục 12 cung cấp ngữ cảnh để người remake tránh nhầm phạm vi.

Đây là đặc tả bàn giao, không phải bản chép nguyên văn toàn bộ lịch sử chat hoặc cam kết rằng mọi hệ thống trong repository đều đã hoàn thiện.

## 2. Các yêu cầu người dùng và kết quả đã thống nhất

| Yêu cầu trong hội thoại | Kết quả cần bảo toàn khi remake |
| --- | --- |
| Cập nhật cơ chế Shop và điểm danh hàng ngày | Có logic nhận thưởng, đổi tiền, giao diện, save/load và test |
| “Tự đề xuất và làm luôn” | Codex chọn bộ số cơ bản tại mục 4, không cần thêm bước duyệt thiết kế |
| “Mới chỉ có code thôi đúng không?” | Kết quả bàn giao phải có UI thực tế trong Unity, không dừng ở service/code |
| Đã tạo Shop Manager và Daily Reward Manager, yêu cầu di chuyển vào đó | Đã bổ sung UI có thể chỉnh trong scene; dùng các nhóm có sẵn thay vì chỉ tạo khi Play |
| “Chuyển ngôn ngữ sang tiếng anh” | Scene, prefab và nội dung khi chạy của hai tính năng hiển thị tiếng Anh |
| “Để tôi tự push” | Người dùng tự push; không tiếp tục yêu cầu đăng nhập hoặc tự push thay |

Trình tự sản phẩm: tạo logic và prefab dùng được → đưa UI vào scene theo nhóm Manager → chuyển nội dung sang tiếng Anh. Về sau repository có commit người dùng điều chỉnh scene; xem hiện trạng tại mục 6.

## 3. Môi trường và ranh giới dự án

| Thành phần | Cấu hình đã đối chiếu |
| --- | --- |
| Engine | Unity `6000.3.15f1` |
| Render pipeline | URP `17.3.0`, dự án 2D |
| Input | New Input System `1.19.0` |
| UI | uGUI `2.0.0` và TextMeshPro |
| JSON | Newtonsoft JSON package `3.2.2` |
| Kiểm thử | Unity Test Framework `1.6.0`, NUnit/EditMode |
| Namespace Shop/Daily | `Hoang.ShopDaily` |
| Nhánh làm việc | `Hoang` |

Quy tắc tích hợp:

- Đặt phần mới trong `Assets/_Hoang`.
- Chỉ chỉnh các scene bản sao tại `Assets/_Hoang/Scene`; giữ nguyên scene gốc trong `Assets/__Project/Scenes`.
- Không sửa `Assets/__Vu` ngoài phần tích hợp save/reset cần thiết đã được cho phép.
- Cơ chế lưu bền vững dùng `GameSaveCoordinator` của Vũ và tham gia nút Reset có sẵn trong Settings. Không tạo hệ save hoặc nút Reset riêng cho Shop/Daily.
- Giữ nguyên thay đổi khác của người dùng; không đưa chúng vào commit của tính năng.
- Dùng UI và package sẵn có; không cần cài thêm SDK cho bản hiện tại.
- Khi viết tương tác chuột/chạm mới, dùng New Input System hoặc event interface uGUI phù hợp.

## 4. Luật sản phẩm cần remake

### 4.1. Daily Rewards

- Mỗi **ngày lịch Việt Nam UTC+7** được bấm nhận một lần.
- Mỗi lần nhận thành công được **100 Gold và 100 Diamond**.
- Người chơi phải bấm `Claim rewards`; không tự cộng khi đăng nhập hoặc mở panel.
- Ngày mới bắt đầu lúc **00:00 UTC+7**, không phải sau 24 giờ tính từ lần bấm trước.
- Khi chưa nhận: bật dấu báo có quà và cho phép bấm nhận nếu save đã sẵn sàng.
- Sau khi nhận: nút bị vô hiệu hóa, hiển thị `Claimed today`, cập nhật số dư và thông báo thành công.
- Có đồng hồ `New day in HH:MM:SS · 00:00 UTC+7`, cập nhật khoảng mỗi giây.
- Đóng/mở panel hoặc khởi động lại game không cho nhận lại trong cùng ngày.
- Qua ngày mới có thể nhận tiếp; không có chuỗi 7 ngày, streak, nhận bù hoặc thưởng tích lũy trong bản này.

Ví dụ: nhận lúc 23:59:59 giờ Việt Nam thì có thể nhận lượt ngày mới lúc 00:00:00. Đổi timezone hiển thị của máy không thay đổi cách chia ngày UTC+7.

### 4.2. Shop đổi Diamond lấy Gold

| Thứ tự thẻ đổi | Tên tiếng Anh | Diamond bị trừ | Gold được cộng |
| --- | --- | ---: | ---: |
| 1 | Gold pouch | 50 | 1.000 |
| 2 | Gold chest | 200 | 4.500 |
| 3 | Gold vault | 500 | 12.000 |

- Shop có thẻ quảng cáo miễn phí ở vị trí đầu tiên; ba gói đổi nằm sau đó.
- Hiển thị số dư Gold/Diamond hiện tại.
- Thiếu Diamond thì không cho mua và không thay đổi số dư. Service vẫn phải kiểm tra độc lập với trạng thái nút UI.
- Mua thành công trừ Diamond và cộng Gold trong cùng giao dịch lưu.
- Không có giới hạn số lần mua mỗi ngày cho ba gói này.
- Không có giao dịch tiền thật, IAP, subscription, kho vật phẩm hoặc skin trong bản đã làm.
- Số lượng mặc định được tập trung trong service; nếu đổi bộ số, cần đồng bộ nội dung card trong scene và prefab.

### 4.3. Rewarded ad

- Mỗi lượt hoàn tất hợp lệ nhận **50 Diamond**.
- Tối đa **3 lượt nhận thành công mỗi ngày UTC+7**.
- Hủy, lỗi hoặc đóng trước khi hoàn tất: không nhận Diamond và không mất lượt nhận.
- Callback hoàn tất lặp không được cấp thưởng hai lần.
- Chỉ cho phép một quảng cáo đang chờ trên một service.
- Reset, nạp lại save hoặc dispose service phải hủy hiệu lực ticket đang chờ.
- Nếu bắt đầu trước nửa đêm và hoàn tất sau nửa đêm, giao dịch tính vào ngày **hoàn tất** theo code hiện tại.

**Trạng thái triển khai thực tế:** chưa gắn SDK quảng cáo. Chỉ trong Unity Editor có hộp `TEST AD · EDITOR` với `Complete`, `Cancel`, `Simulate error`. Hai trường hợp cuối cùng đều đi qua nhánh không hoàn tất.

Trong build, bấm quảng cáo báo `Ads are currently unavailable. Please try again later.` và không cộng thưởng giả lập. Dòng thiết kế cũ trong master plan nói SDK với ad unit test là mục tiêu rộng hơn, không phải bằng chứng SDK đã được tích hợp.

### 4.4. Trạng thái lỗi và bảo vệ dữ liệu

- Save chưa sẵn sàng: khóa thao tác nhận/mua, hiển thị trạng thái chờ phù hợp.
- UI có cờ `busy` chặn thao tác chồng trong lúc đợi giao dịch và khóa nút đóng khi đang commit.
- Ghi file thất bại: báo lỗi, không thay đổi số dư RAM hoặc đánh dấu đã nhận.
- Số dư dùng kiểu `long`; phép cộng thưởng có kiểm tra tràn số. Tràn số trả về `BalanceLimit`.
- Offer index không hợp lệ trả về `Unavailable`.
- Ngày hiện tại nhỏ hơn hoặc bằng ngày đã nhận Daily thì không cấp Daily tiếp. Ngày quảng cáo lùi trước ngày đã ghi thì không mở lượt mới.
- Đây là kiểm tra cục bộ dựa trên đồng hồ máy. Không có thời gian máy chủ, không bảo đảm chống chỉnh giờ tiến, sửa save hoặc gian lận thiết bị.

## 5. Lưu dữ liệu, giao dịch và Reset

### 5.1. Dùng chung economy hiện có

Không lưu số dư hoặc lượt nhận vào một PlayerPrefs key/file riêng. Thêm các trường tùy chọn vào `EconomySaveData` đang có:

| Tên property C# | Tên JSON | Ý nghĩa |
| --- | --- | --- |
| `DailyRewardDay` | `dailyRewardDay` | Ngày lịch UTC+7 đã nhận Daily gần nhất |
| `RewardedAdDay` | `rewardedAdDay` | Ngày của bộ đếm quảng cáo gần nhất |
| `RewardedAdClaims` | `rewardedAdClaims` | Số lượt quảng cáo đã nhận thưởng trong ngày đó |

Ngày được mã hóa bằng số ngày tính từ `1970-01-01`, sau khi chuyển thời điểm sang UTC+7. Đây không phải timestamp giây/millisecond.

Save schema-1 cũ thiếu các trường mới sẽ nhận giá trị mặc định `0`, vẫn nhận được thưởng trong ngày hiện tại. Schema Economy vẫn là `1` vì bổ sung tương thích. Mọi đường copy/capture/apply của Economy phải giữ nguyên ba trường này, kể cả khi gacha, combat hoặc tính năng khác ghi economy.

Gold, Diamond, Shrimp, revision và schema hiện có vẫn được giữ. Shop/Daily không đổi Shrimp. Giá trị khởi tạo dự án hiện là 1.000 Gold, 5.000 Diamond, 0 Shrimp; đó là **default save**, khác với phần quà Daily.

### 5.2. Giao dịch quan trọng

```text
UI bấm nhận/mua
  -> service kiểm tra điều kiện
  -> CaptureData tạo bản đề xuất Economy
  -> cập nhật số dư và marker trên bản đề xuất
  -> SaveMutation(Economy, proposed, proposed.Revision)
  -> GameSaveCoordinator.CommitCriticalAsync
  -> ghi candidate save thành công
  -> coordinator áp dụng vào RAM
  -> UI hiển thị kết quả và số dư mới
```

Revision giúp coordinator từ chối proposal đã cũ khi có giao dịch cạnh tranh. Không cộng/trừ tiền trước rồi mới cố ghi file. Retry ở tầng file không được chạy lại logic grant hoặc spend.

Save chung nằm trong `Application.persistentDataPath/BunDauPeo/`, gồm `save.json`, `save.backup.json` và file tạm khi ghi. Tái sử dụng recovery/validation sẵn có của repository, không hard-code thư mục AppData của một máy cụ thể.

### 5.3. Khắc phục lỗi khóa file trên Windows

Trong quá trình xác minh đã tái hiện lỗi reader tạm thời chặn `File.Replace`. Đã bổ sung retry trong `LocalJsonSaveRepository`:

- Thử thay file chính bằng file tạm đã chuẩn bị.
- Khi gặp `IOException` và cả file tạm/file chính còn tồn tại, chờ 10 ms rồi thử lại.
- Tối đa **3 lần thử lại sau lần đầu**, tức tối đa 4 lần gọi replacement, tổng delay tối đa 30 ms.
- Dùng lại cùng file đã serialize, không tạo thêm phần thưởng hoặc giao dịch kinh tế.
- Khóa kéo dài vẫn trả lỗi; không áp dụng số dư đề xuất vào RAM.

Đây là thay đổi tích hợp save có phạm vi hẹp, không phải hệ save mới.

### 5.4. Reset

Dùng nút Reset dữ liệu hiện có trong Settings và luồng default bundle của coordinator. Khi tạo `EconomySaveData` mặc định, ba trường mới về `0`; Daily được phép nhận lại và bộ đếm quảng cáo được xóa. Ticket cũ không còn hiệu lực sau khi nạp/reset bundle.

Không thêm nút reset riêng vào Shop/Daily. Khi kiểm thử, dùng repository tạm hoặc bản sao save; không xóa dữ liệu người chơi thật để chứng minh cơ chế hoạt động.

## 6. Scene, prefab và cấu trúc UI

### 6.1. Hai cách cung cấp giao diện

1. **Scene chỉnh được trong Edit Mode:** `Assets/_Hoang/Scene/SkillTreeSystem.unity` có `ShopCanvas` và `DailyRewardCanvas`. Bản scene đã unpack để người dùng chỉnh trực tiếp.
2. **Prefab dự phòng:** `Assets/_Hoang/Resources/HoangShopDaily.prefab`, được bootstrap runtime khi scene hỗ trợ chưa có controller.

Bootstrap chỉ áp dụng cho hai đường dẫn scene `_Hoang/Scene/SkillTreeSystem.unity` và `_Hoang/Scene/1_Background.unity`. Nó tìm controller cả trên object inactive trong scene; nếu đã có thì không tạo thêm. Không chỉ kiểm tra tên scene hoặc tự chèn vào mọi scene của game.

### 6.2. Yêu cầu nhóm Manager và hiện trạng sau chỉnh sửa

Người dùng đã có các tiêu đề hierarchy `--Shop Manager` và `--Daily Reward Manger` (tên gốc có chữ `Manger`). Trong lần sửa theo yêu cầu, UI được đặt dưới hai nhóm đó, với controller dùng chung trên ShopCanvas.

**Hiện trạng đã đọc tại HEAD `b2ecb20`:** cả hai tiêu đề và `ShopCanvas`, `DailyRewardCanvas` hiện đều nằm ở **scene root**. Vì vậy đường dẫn cha-con ghi trong `ShopDailyRewards.md` là mô tả lần bàn giao trước, không còn phản ánh chính xác hierarchy hiện tại. Không tự động “sửa lại” scene chỉ để khớp tài liệu cũ.

Khi remake, giữ UI được tổ chức rõ theo Shop/Daily, có thể chỉnh trong scene, bảo toàn lựa chọn hierarchy của chủ dự án. Nếu muốn tái lập đúng lần bàn giao trước thì dùng cấu trúc sau như một phương án tổ chức, không giả định đây là hiện trạng:

```text
--Shop Manager
  ShopCanvas
    SafeArea / LobbyEntries / Shop / EditorAdSimulation
    ShopDailyRewardsUI (controller dùng chung)
--Daily Reward Manger
  DailyRewardCanvas
    SafeArea / LobbyEntries / Daily
```

### 6.3. Wiring cần có

- Một `ShopDailyRewardsUI` điều khiển cả Shop và Daily qua reference serialize.
- ShopCanvas giữ controller và các control Shop; DailyRewardCanvas giữ entry/panel Daily của scene.
- `safeAreaRoot` và `additionalSafeAreas` cập nhật safe area cho hai Canvas.
- `entryRoot` và `additionalEntryRoots` đồng bộ việc hiện/ẩn các nút vào.
- Có reference đầy đủ cho open/close, claim, ad, 3 nút mua, 3 nút giả lập, balance, countdown, feedback và label ngôn ngữ.
- Chỉ mở một trong hai panel tại một thời điểm.
- Khi mở panel, ẩn entry; đóng panel thì entry hiện lại nếu đang ở lobby phù hợp.
- Kiểm tra `TabsManager.ActiveTabIndex` để entry không đè lên trang tab đang mở.
- Subscribe save-ready/state-change để refresh; hủy đăng ký và dispose service khi view bị disable.
- Có Canvas, CanvasScaler, GraphicRaycaster và EventSystem thích hợp; không tạo bản controller trùng gây nhận sự kiện hai lần.

### 6.4. Trình bày

Template dùng Screen Space Overlay, Canvas sorting order 30, Scale With Screen Size với reference 1920×1080 và match 0,5. Phong cách nền giấy màu kem, nút xanh lá/tím, màu Gold, icon đơn giản bằng chữ G/D; không cần asset mỹ thuật hoặc plugin mới để tái tạo chức năng.

Panel nằm trong safe area và được thu nhỏ khi cần để vừa màn hình. Các thông số trên là mặc định của generator; scene đã chỉnh tay có thể khác. Kiểm tra tỷ lệ màn hình và vùng notch thay vì chỉ nhìn một kích thước Game View.

Menu `Hoang > Shop & Daily > Rebuild UI Prefab` tạo lại resource template. **Không** tự cập nhật bản đã unpack trong SkillTreeSystem; sửa template và scene cần được đồng bộ có chủ ý.

### 6.5. Build Settings hiện tại

Đã đọc thấy bốn scene enabled đều là bản sao `_Hoang`: `0_Bootstrap`, `0_GameMenu`, `1_Background`, `2_Gameplay`. `SkillTreeSystem` không có trong danh sách build hiện tại.

Luồng build lobby có thể dùng resource fallback tại copied `1_Background`. Cần test build thực tế để xác nhận luồng khởi động và UI; việc có scene trong Build Settings chưa phải bằng chứng Android build đã qua.

## 7. Giao diện tiếng Anh

Yêu cầu tiếng Anh áp dụng cho Shop và Daily Rewards, không phải yêu cầu dịch toàn bộ game hoặc tất cả tài liệu sang tiếng Anh.

- Controller có `[SerializeField] private bool english = true`.
- Dữ liệu label vẫn giữ biến thể Việt/Anh để tái sử dụng, nhưng mặc định scene và prefab dùng tiếng Anh.
- Không để `SettingsSaveModule.LanguageCode` mặc định `vi` ghi đè lại lựa chọn này trong `Refresh`.
- Các chuỗi tĩnh phải là tiếng Anh ngay trong Edit Mode, không chỉ được thay sau khi Play.
- Generator cũng sinh tiếng Anh để lần rebuild sau không quay về tiếng Việt.
- Entry `Daily rewards` đặt NoWrap và auto sizing để không vỡ thành hai dòng ở bố cục đã kiểm tra.

| Trường hợp | Nội dung tiếng Anh tiêu biểu |
| --- | --- |
| Mở Shop | `Shop`, `Exchange Diamond. Keep your restaurant growing.` |
| Thẻ miễn phí | `Free daily`, `Watch ad`, `3/3 claims left today` |
| Gói đổi | `Gold pouch`, `Gold chest`, `Gold vault`, `Added instantly` |
| Daily | `Daily rewards`, `TODAY'S GIFT`, `Claim rewards` |
| Nhận xong | `Claimed today`, `Received 100 Gold + 100 Diamond!` |
| Đổi xong | `Gold exchange complete!` |
| Quảng cáo xong | `Received 50 Diamond!` |
| Hủy/lỗi quảng cáo | `Ad not completed. No reward claim was used.` |
| Thiếu tiền | `Not enough Diamond.` |
| Lỗi ghi | `Could not save this transaction. Please try again.` |

Bảo đảm font hiển thị đủ glyph cho các ký tự dùng trong UI, gồm dấu nhân nút đóng hoặc dấu phân cách nếu giữ thiết kế hiện tại. Tài liệu nhà hàng có quy tắc tránh icon/emoji; không diễn giải thành kết luận mọi ký tự Unicode đều không được hỗ trợ.

## 8. Danh mục file phục vụ remake

Các đường dẫn dưới đây tính từ project root để người nhận dùng trên máy khác.

| File | Trách nhiệm |
| --- | --- |
| `Assets/_Hoang/Scripts/ShopDaily/ShopDailyRewardsService.cs` | Bộ số, ngày UTC+7, eligibility, đổi Gold, ticket quảng cáo và commit |
| `Assets/_Hoang/Scripts/ShopDaily/ShopDailyRewardsUI.cs` | Wiring UI, ngôn ngữ, safe area, điều hướng, feedback, bootstrap fallback |
| `Assets/_Hoang/Scripts/ShopDaily/Editor/ShopDailyRewardsPrefabBuilder.cs` | Sinh resource prefab có reference đầy đủ |
| `Assets/_Hoang/Resources/HoangShopDaily.prefab` | Template uGUI/TMP và fallback runtime |
| `Assets/_Hoang/Scene/SkillTreeSystem.unity` | Bản UI được chỉnh trực tiếp trong scene |
| `Assets/_Hoang/Tests/Editor/ShopDailyRewardsTests.cs` | 12 test cho logic, save, prefab và file locking |
| `Assets/__Vu/_User/SaveSystem/Data/EconomySaveData.cs` | Ba trường claim mới trong document Economy |
| `Assets/__Vu/_User/SaveSystem/Modules/EconomySaveModule.cs` | Copy/apply/validate các trường mới cùng economy |
| `Assets/__Vu/_User/SaveSystem/Local/LocalJsonSaveRepository.cs` | Retry replacement nguyên tử khi reader khóa ngắn hạn |
| `Assets/__Vu/_User/SaveSystem/Core/GameSaveCoordinator.cs` | API save có sẵn được dùng lại; không phải manager mới của tính năng |
| `Assets/__Vu/_User/SaveSystem/Data/GameSaveDefaults.cs` | Default bundle hiện có, marker mới mặc định bằng 0 |
| `Assets/_Hoang/Docs/ShopDailyRewards.md` | Thiết kế, cách dùng và kết quả kiểm tra trước đây |
| `Assets/_Hoang/Docs/ShopDailyRewards-Plan.md` | Kế hoạch triển khai lịch sử |

API service hiện có:

```csharp
ShopDailyRewardsService(GameSaveCoordinator coordinator)
bool IsReady
static int VietnamDay(DateTimeOffset now)
bool CanClaimDaily(DateTimeOffset now)
int RemainingAds(DateTimeOffset now)
Task<ShopRewardResult> ClaimDailyAsync(DateTimeOffset now)
Task<ShopRewardResult> BuyGoldAsync(int offerIndex)
AdTicket BeginAd(DateTimeOffset now)
Task<ShopRewardResult> FinishAdAsync(AdTicket ticket, bool completed, DateTimeOffset now)
void Dispose()
```

Kết quả gồm `Success`, `Unavailable`, `AlreadyClaimed`, `InsufficientFunds`, `DailyLimit`, `Cancelled`, `SaveFailed`, `BalanceLimit`. Có thể tổ chức lại code khi remake, nhưng giữ nguyên hành vi và hợp đồng dữ liệu hoặc cung cấp migration rõ ràng.

## 9. Kiểm chứng đã thực hiện và giới hạn bằng chứng

**Kết quả lịch sử từ lần triển khai 01/11/2026**, không phải lượt test mới chạy khi viết tài liệu này:

- Unity compile được assembly runtime và Editor.
- **48/48 test được chọn** đã qua, thuộc ShopDailyRewardsTests và các regression liên quan: GameplayStateModuleTests, GameSaveDataDefaultsTests, GachaSaveTransactionTests, CombatVictorySaveTests, LocalJsonSaveRepositoryTests. Không đồng nghĩa toàn bộ test của repository đã chạy.
- Sau di chuyển hierarchy, **12/12 ShopDailyRewardsTests** đã qua.
- Tái hiện được lỗi reader khóa file Windows trước sửa; test pass sau retry. Có kiểm tra 100 lần ghi liên tiếp và khóa kéo dài trả lỗi.
- Play Mode trong SkillTreeSystem với simulator có notch: mở/đóng panel, dấu có quà, nhận Daily, đổi Gold, quảng cáo hủy/hoàn tất và đếm lượt.
- Sau đổi ngôn ngữ: kiểm tra Shop/Daily, countdown, nút đã nhận và thông báo thưởng bằng tiếng Anh; compile không có lỗi được ghi nhận ở lượt kiểm tra đó.
- Preview giao dịch dùng repository tạm cách ly. Hash save chính, backup và restaurant_save của người chơi không thay đổi trong các lượt kiểm tra đã ghi nhận.
- Review độc lập trước đó không phát hiện vấn đề nghiêm trọng trong thay đổi Shop/Daily và save retry.

**Chưa có bằng chứng:** Android build/device test, tích hợp SDK quảng cáo thật, xác minh callback SDK trên thiết bị, chống gian lận phía server. Reset được kiểm tra qua default-data path, không bấm nút Reset phá dữ liệu người chơi thật. Khi bàn giao/remake, cần chạy lại kiểm tra trên bản code mới.

## 10. Checklist nghiệm thu cho người remake

- [ ] Unity mở project và compile không có lỗi mới do thay đổi.
- [ ] Có UI chỉnh được trong scene và resource prefab; reference không missing; chỉ có một controller cho scene.
- [ ] Build lobby bản sao mở được Shop/Daily; không sửa scene gốc để đạt kết quả.
- [ ] Chưa nhận Daily thì có dấu báo và nút nhận; nhận đúng +100 Gold/+100 Diamond một lần.
- [ ] Bấm lặp, mở lại panel, reload save không cấp Daily lại trong ngày.
- [ ] Qua 00:00 UTC+7 được nhận tiếp; đổi timezone không làm đổi ngày của cùng một thời điểm.
- [ ] Lùi ngày không mở lại quyền nhận đã ghi; giải thích đúng giới hạn đồng hồ cục bộ.
- [ ] Ba gói đổi trừ/cộng đúng; thiếu tiền và index sai không làm thay đổi số dư.
- [ ] Quảng cáo Complete được +50 Diamond, tối đa 3 lượt; Cancel/Error không mất lượt.
- [ ] Callback lặp, ticket cũ sau reset/reload/dispose không thể cấp thưởng.
- [ ] Giao dịch gặp lỗi ghi không trừ tiền, cộng quà hoặc tiêu eligibility.
- [ ] Save cũ thiếu marker vẫn load; ghi economy từ tính năng khác không xóa marker.
- [ ] Reset Settings hiện có đưa economy và marker về default, không cần reset riêng.
- [ ] Tràn số bị từ chối mà không thay đổi state.
- [ ] Scene, prefab, generator và runtime hiển thị tiếng Anh; Daily rewards không xuống dòng sai.
- [ ] UI vừa safe area, mở/đóng đúng, không đè entry lên trang tab khác.
- [ ] Bản build không có giả lập cấp thưởng quảng cáo.
- [ ] Kiểm thử bằng dữ liệu tạm; giữ nguyên save và thay đổi khác của chủ dự án.

## 11. Các mốc Git để đối chiếu

| Commit | Nội dung |
| --- | --- |
| `1704dac` | Thêm Shop/Daily, prefab, test và tích hợp economy/save nguyên tử |
| `ca1ddf7` | Đưa Shop và Daily Rewards vào scene theo các nhóm Manager |
| `3ec8bb5` | Chuyển Shop/Daily sang tiếng Anh ở scene, prefab và runtime |
| `b2ecb20` | Commit tiếp theo `Update Shop&Daily`, có thay đổi scene; là HEAD lúc tổng hợp |

Lúc đầu push bị chặn vì Git chưa xác thực. Người dùng đã chọn tự push. Khi tổng hợp, `Hoang` không còn báo ahead/behind so với tracking ref cục bộ; không thực hiện fetch hoặc kiểm tra remote mới trong tác vụ viết tài liệu này.

## 12. Ngữ cảnh các phân hệ khác trong Markdown

File có sẵn `Assets/_Hoang/Docs/Restaurant_System_Architecture_And_Remake_Spec.md` mô tả rộng hơn: Skill Tree, Facilities Grid, điện, Buff Bridge, save nhà hàng và tooltip. File này đang chưa được Git theo dõi tại lúc bắt đầu tổng hợp và được giữ nguyên.

Nếu người nhận còn muốn remake các phân hệ đó, dùng tài liệu trên làm nguồn khảo sát bổ sung, rồi kiểm tra code và scene thực tế trước khi triển khai. Không coi trạng thái “đã hoàn thiện” trong tài liệu là một lượt nghiệm thu mới.

| Phân hệ được mô tả trong tài liệu khác | Ý chính để định vị | Quan hệ với công việc Shop/Daily |
| --- | --- | --- |
| Skill Tree | Node kỹ năng, cấp độ, điểm thường/đặc biệt, buff và hoàn điểm | Có sẵn; không phải điểm thưởng Daily |
| Facilities/Power Grid | Lưới thiết bị, hình dạng/xoay, truyền điện, công suất và relay | Có sẵn; không phải sản phẩm bán trong Shop hiện tại |
| RestaurantBuffBridge | API tổng hợp buff từ kỹ năng/thiết bị cho gameplay | Không phải economy service Shop/Daily |
| RestaurantSaveManager | Tài liệu mô tả save nhà hàng riêng bằng JSON/PlayerPrefs | Hiện trạng legacy cần kiểm tra; không sao chép cách lưu này cho Shop/Daily |
| Tooltip | Hiển thị thông tin kỹ năng/thiết bị và tránh tràn viền | Không phải phần bắt buộc để hoạt động Shop/Daily |
| Combat/gacha/character/formation | Hệ gameplay đã có, nhiều luồng đọc/ghi economy chung | Giữ tương thích; không remake toàn bộ theo mặc định |
| Sheets/workflow/task status | Công cụ quản lý công việc của nhóm | Ngoài runtime Shop/Daily |

**Mâu thuẫn tài liệu cần xử lý:** bản đặc tả nhà hàng nhắc dual-storage riêng, nhưng AGENTS yêu cầu cơ chế bền vững mới dùng coordinator/Reset hiện có. Tài liệu này không yêu cầu dựng thêm save độc lập và không tự xác nhận save nhà hàng cũ đã tích hợp Reset. Nếu mở rộng remake sang nhà hàng, phải xác định migration và yêu cầu reset với chủ dự án trước khi thay đổi save đó.

Các plan Firebase/Google Login đầu tháng 10 là lịch sử. Thiết kế local JSON ngày 12/10 và code hiện tại đã thay thế hướng đó; không cài lại Firebase/đăng nhập chỉ vì thấy các file cũ. Tương tự, master plan có backlog/ý tưởng tương lai; không biến chúng thành các tính năng đã làm trong cuộc hội thoại này.

## 13. Prompt đưa cho Claude Pro

Sao chép đoạn dưới và gửi kèm file này. Để remake có tương thích save/scene, nên gửi thêm source liên quan ở mục 8 cùng Packages, ProjectSettings và các file `.meta`; chỉ có Markdown thì Claude có thể mô tả hoặc sinh code, nhưng không kiểm chứng được project thật.

```text
Bạn giúp tôi remake phần Shop và Daily Rewards trong Unity BunDauPeo theo tài liệu bàn giao đính kèm.

Phạm vi chính: hai tính năng Shop/Daily đã mô tả; không tự remake toàn bộ combat, gacha, nhà hàng hoặc workflow Sheets.

Yêu cầu giữ nguyên:
1. Unity 6000.3.15f1, URP 2D, uGUI/TMP và New Input System.
2. Code mới ở Assets/_Hoang, scene bản sao ở Assets/_Hoang/Scene, không sửa scene gốc.
3. Daily bấm nhận 100 Gold + 100 Diamond một lần mỗi ngày lịch UTC+7, reset 00:00.
4. Shop có thẻ ad miễn phí đầu tiên; ba gói đổi 50/200/500 Diamond lấy 1000/4500/12000 Gold.
5. Ad hoàn tất nhận 50 Diamond, tối đa 3 lượt/ngày. Bản hiện tại chỉ giả lập trong Editor; build không được grant giả lập. Không tự cài SDK/IAP ngoài phạm vi.
6. Dùng GameSaveCoordinator và Economy hiện có; số dư và marker phải commit cùng nhau. Save lỗi không được grant/spend. Giữ tương thích JSON cũ và các ghi economy từ gacha/combat.
7. Dùng Reset Settings hiện có; không thêm save/reset riêng. Ticket ad cũ không có hiệu lực sau reset/reload/dispose.
8. Có UI thực tế, chỉnh được trong scene và resource prefab dự phòng; không dừng ở code thuần. Giữ lựa chọn hierarchy hiện tại của chủ dự án, tránh controller trùng.
9. Mọi nội dung Shop/Daily hiển thị tiếng Anh cả Edit Mode và runtime. Có safe area, số dư, countdown, claimed/disabled state và thông báo lỗi.
10. Bảo toàn thay đổi khác và dữ liệu người chơi; kiểm thử bằng repository tạm.

Trước khi sửa, đọc AGENTS.md, code/save/scene hiện có và đối chiếu những mâu thuẫn tài liệu được ghi ở mục 12. Sau khi làm, bàn giao file, wiring scene/prefab, cách chạy, kết quả kiểm tra thật và giới hạn còn lại. Chạy checklist mục 10; không báo Android/SDK đã pass nếu chưa kiểm tra.

Làm trên Hoang nếu đang dùng cùng repository. Tôi sẽ tự push. Nếu thiếu source hoặc không có Unity để kiểm chứng, nêu chính xác phần thiếu và phân biệt code đã sinh với kết quả đã chạy thử.
```

## 14. Danh mục 43 file Markdown đã rà soát

Đường dẫn tương đối từ project root; các tài liệu lịch sử được dùng để nhận biết bối cảnh, không tự trở thành yêu cầu triển khai mới.

### Quy tắc và hướng dẫn plugin — 3 file

1. `AGENTS.md`
2. `.agents/skills/unityskills/SKILL.md`
3. `.agents/skills/unityskills/references/unityskills-agent-instructions.md`

### Tài liệu Hoàng và Shop/Daily — 3 file

4. `Assets/_Hoang/Docs/ShopDailyRewards.md`
5. `Assets/_Hoang/Docs/ShopDailyRewards-Plan.md`
6. `Assets/_Hoang/Docs/Restaurant_System_Architecture_And_Remake_Spec.md`

### Ngữ cảnh và kế hoạch sản phẩm — 5 file

7. `docs/AI/UnityProjectContext.md`
8. `docs/game-save-system-setup.md`
9. `docs/BunDauPeo_MasterPlan_GoogleSheets.md`
10. `docs/ChapterOneCampaign.md`
11. `docs/tasks/CV-DES-001.md`

### Workflow quản lý nhóm — 2 file

12. `docs/google-sheets/workflow/README.md`
13. `docs/google-sheets/workflow/progress.md`

### Kế hoạch triển khai lịch sử — 14 file

14. `docs/superpowers/plans/2026-09-30-formation-screen.md`
15. `docs/superpowers/plans/2026-10-01-game-menu-prototype.md`
16. `docs/superpowers/plans/2026-10-07-android-google-sign-in.md`
17. `docs/superpowers/plans/2026-10-08-combined-map-formation.md`
18. `docs/superpowers/plans/2026-10-09-game-save-system.md`
19. `docs/superpowers/plans/2026-10-12-local-json-save-implementation.md`
20. `docs/superpowers/plans/2026-10-13-combat-progression-implementation.md`
21. `docs/superpowers/plans/2026-10-21-sheet-workflow.md`
22. `docs/superpowers/plans/2026-10-23-native-gacha-cutscenes.md`
23. `docs/superpowers/plans/2026-10-24-chapter-one-combat-results.md`
24. `docs/superpowers/plans/2026-10-24-character-stars-backpack.md`
25. `docs/superpowers/plans/2026-10-24-lobby-page-layers.md`
26. `docs/superpowers/plans/2026-10-24-ultimate-cutscene-and-gacha-order.md`
27. `docs/superpowers/plans/2026-10-25-readonly-task-status.md`

### Thiết kế lịch sử — 7 file

28. `docs/superpowers/specs/2026-10-01-game-menu-prototype-design.md`
29. `docs/superpowers/specs/2026-10-07-google-sign-in-android-design.md`
30. `docs/superpowers/specs/2026-10-08-combined-map-formation-design.md`
31. `docs/superpowers/specs/2026-10-09-game-save-system-design.md`
32. `docs/superpowers/specs/2026-10-12-local-json-save-design.md`
33. `docs/superpowers/specs/2026-10-13-combat-progression-design.md`
34. `docs/superpowers/specs/2026-10-21-sheet-workflow-design.md`

### Tài liệu gameplay, UI và asset khác — 9 file

35. `Assets/__Project/MD/ThongKe_P1_SRS_P6_BunDauPeo.md`
36. `Assets/__Project/MD/Gacha_RollOnly_Checkpoint.md`
37. `Assets/__Project/MD/EnemyPlaceholders.md`
38. `Assets/__Project/Arts/CombatTestRoster/README.md`
39. `Assets/__Vu/CharacterInfo/ProgressionAndBackpack.md`
40. `Assets/__Vu/Gacha/NativeCutscenes.md`
41. `Assets/__Vu/_Characters/UltimateCutscene.md`
42. `Assets/__Vu/Tabs/BottomMenu.md`
43. `Assets/__Vu/Tabs/Art/GenerationNotes.md`
