# Changelog

Tất cả thay đổi đáng chú ý của VTC Fast Pool được ghi ở đây.
Định dạng theo [Keep a Changelog](https://keepachangelog.com/vi/1.1.0/),
đánh số theo [Semantic Versioning](https://semver.org/lang/vi/).

## [2.0.0] - 2026-10-27

### Changed
- Đổi tên class `FastPool` thành `VtcFastPool`. Mọi lời gọi đổi theo, ví dụ `FastPool.GetPool` thành
  `VtcFastPool.GetPool`. Tiền tố log đổi thành `[VtcFastPool]`.

## [1.2.1] - 2026-10-26

### Fixed
- Bộ tự cập nhật nâng bản cũ bằng git URL không gắn tag. Trước đó URL bị ghim `#v1.1.0` nên nút
  Update của Package Manager chỉ tải lại đúng bản 1.1.0.
- Bộ tự cập nhật không còn nhận nhầm `package.json` cũ trong cache của GitHub ngay sau khi phát hành.

## [1.2.0] - 2026-10-26

### Added
- `FastPoolDespawner`: tự trả object về pool khi hết `lifetime`, khi particle chạy xong hoặc khi audio dừng.
  Có tùy chọn chạy lại particle mỗi lần spawn.
- `FastPoolPreloader`: khai báo pool trong Inspector (prefab, prewarm, capacity), tùy chọn xóa khi scene unload.
- `CreatePool(..., keepParentOnReturn)`: trả về pool không đổi parent, hợp với UI. Mặc định `false`, giữ hành vi cũ.
- `ClearPoolSlowly` và `ClearAllSlowly`: hủy object trong pool dần qua nhiều frame.
- `FastPool.GetPoolInfos` và cửa sổ **Window ▸ VTC ▸ Fast Pool Monitor** để xem pool lúc đang Play.

### Fixed
- README ghi `FastPool.Spawn` / `FastPool.Despawn`, tên đúng là `GetPool` / `ReturnPool`.

## [1.1.0] - 2026-10-26

### Added
- Tự kiểm tra cập nhật mỗi lần mở Editor cho mọi package `com.vtcong.*` cài qua git URL.
  Bản thấp hơn 1.1.0 được tự nâng lên 1.1.0; bản mới hơn thì hỏi người dùng trước khi cập nhật.

## [1.0.2] - 2026-10-26

### Fixed
- Tạo GUID mới cho toàn bộ file `.meta` của package.
  GUID cũ trùng với bản code gốc, nên ở project đã có sẵn bản đó Unity bỏ qua asset của package
  ("conflicts with ... The asset will be ignored") và các class trong package bị mất.

## [1.0.1] - 2026-09-21

### Fixed
- Chuyển thư mục sample sang `Samples~` và khai báo trong `package.json`.
  Trước đó scene sample nằm thẳng trong package nên Unity từ chối mở với thông báo
  "It is not allowed to open a scene in a read-only package" khi cài qua git URL.
  Giờ import sample qua Package Manager, Unity copy sang `Assets/Samples/` nên mở được.

## [1.0.0] - 2026-09-21

### Added
- Phát hành đầu tiên dưới dạng package UPM độc lập.
