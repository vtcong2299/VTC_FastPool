# VTC Fast Pool

Object pool cho GameObject: prewarm, tự trả về, interface `IPoolable`.

## Cài đặt

Unity ▸ **Window ▸ Package Manager ▸ + ▸ Add package from git URL**:

```
https://github.com/vtcong2299/VTC_FastPool.git
```

Không phụ thuộc package nào khác.

## Cập nhật

Mỗi lần mở Unity Editor, package kiểm tra tất cả các package `com.vtcong.*` đang cài qua git URL:

- Bản thấp hơn 1.1.0 được tự nâng lên bản mới nhất, không cần thao tác.
- Có bản mới hơn trên GitHub thì hiện hộp thoại: **Cập nhật**, **Để sau** (hỏi lại lần mở sau) hoặc **Bỏ qua bản này**.
- Cài bằng git URL **không gắn tag** (`...git`, không phải `...git#v1.1.0`). URL có tag thì nút
  **Update** của Package Manager chỉ tải lại đúng tag đó. Project đang ghim tag sẽ được bỏ tag khi
  bấm **Cập nhật** trong hộp thoại trên.

Không chạy trong batchmode (CI). Mất mạng thì bỏ qua, không báo lỗi.

## Dùng

```csharp
FastPool.CreatePool(bulletPrefab, prewarmCount: 50);

var bullet = FastPool.GetPool(bulletPrefab, position, rotation);
FastPool.ReturnPool(bullet);
```

Muốn nhận sự kiện lúc lấy ra / trả về thì implement `IPoolable`:

```csharp
public class Bullet : MonoBehaviour, IPoolable
{
    public void OnSpawn()  { /* reset trạng thái */ }
    public void OnReturn() { /* dọn dẹp */ }
}
```

### Tự trả về pool: `FastPoolDespawner`

Gắn vào prefab effect hoặc SFX. Object tự `ReturnPool` khi điều kiện nào đến trước:

- `lifetime` > 0: hết số giây này (chọn `useUnscaledTime` để không phụ thuộc `Time.timeScale`).
- `waitForParticles`: mọi `ParticleSystem` trong object đã chạy xong.
- `waitForAudio`: mọi `AudioSource` trong object đã dừng.

Với particle hoặc audio loop, đặt thêm `lifetime` làm chốt an toàn. `restartParticlesOnSpawn` chạy lại
particle từ đầu mỗi lần spawn. Chỉ hoạt động với object lấy ra bằng `FastPool.GetPool`.

### Khai báo pool trong Inspector: `FastPoolPreloader`

Gắn vào một GameObject trong scene, thêm prefab, `prewarmCount`, `capacity` cho từng dòng.
Pool được tạo trong `Awake` của scene. Bật `clearOnDestroy` để xóa các pool này khi scene unload,
`destroyPerFrame` > 0 thì xóa dần qua nhiều frame.

### Giữ parent khi trả về (cho UI)

```csharp
FastPool.CreatePool(itemPrefab, prewarmCount: 20, keepParentOnReturn: true);
```

`ReturnPool` chỉ tắt object, không đổi parent, nên Canvas không phải rebuild. Object nằm lại dưới parent
cũ trong lúc chờ; nếu parent đó bị hủy thì object cũng bị hủy và pool tự bỏ qua. Tùy chọn chỉ có tác
dụng khi pool được tạo lần đầu.

### Xóa pool dần qua nhiều frame

```csharp
FastPool.ClearPoolSlowly(prefab, destroyPerFrame: 20);
FastPool.ClearAllSlowly(destroyPerFrame: 20);
```

Pool bị gỡ ngay (lần `GetPool` sau tạo pool mới), object đang dùng bị hủy ngay như `ClearPool`, còn
object đang nằm trong pool được hủy dần. Ngoài Play mode thì hủy hết trong một lần.

### Xem pool lúc chạy

**Window ▸ VTC ▸ Fast Pool Monitor**: ID, prefab, số object trong pool, đang dùng, capacity của từng
pool khi đang Play. Lấy cùng dữ liệu từ code bằng `FastPool.GetPoolInfos(list)`.

### Pool cho object C# thường

Package chỉ pool GameObject. Với class C# thường (lệnh, sự kiện, node tìm đường), dùng
`UnityEngine.Pool.GenericPool<T>`, `ObjectPool<T>` hoặc `ListPool<T>` có sẵn trong Unity 2021.3.

## Samples

Sample **Demo & Self Test** có scene demo (`FastPoolDemo.unity`) và bộ self-test
(`FastPoolSelfTest.unity`). Import qua Package Manager — xem mục cuối.

## Mở scene sample

Sample **không** được import sẵn. Vào **Window ▸ Package Manager ▸ VTC Fast Pool ▸ Samples ▸ Import**.

Unity copy sample vào `Assets/Samples/VTC Fast Pool/<version>/Demo & Self Test/`, từ đó mở scene bình thường.

> **Vì sao phải import?** Package cài qua git URL nằm trong `Library/PackageCache` và ở
> chế độ read-only. Unity **cấm mở scene nằm trong package read-only** — báo
> *"It is not allowed to open a scene in a read-only package"*. Cơ chế Samples của UPM
> sinh ra chính để giải quyết việc này: nó copy sample sang `Assets/` nơi file ghi được.
