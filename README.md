# VTC Fast Pool

Object pool cho GameObject: prewarm, tự trả về, interface `IPoolable`.

## Cài đặt

Unity ▸ **Window ▸ Package Manager ▸ + ▸ Add package from git URL**:

```
https://github.com/vtcong2299/VTC_FastPool.git
```

Không phụ thuộc package nào khác.

## Dùng

```csharp
FastPool.CreatePool(bulletPrefab, prewarmCount: 50);

var bullet = FastPool.Spawn(bulletPrefab, position, rotation);
FastPool.Despawn(bullet);
```

Muốn nhận sự kiện lúc lấy ra / trả về thì implement `IPoolable`:

```csharp
public class Bullet : MonoBehaviour, IPoolable
{
    public void OnSpawn()  { /* reset trạng thái */ }
    public void OnReturn() { /* dọn dẹp */ }
}
```

## Samples

Thư mục `Samples/` có scene demo (`FastPoolDemo.unity`) và bộ self-test
(`FastPoolSelfTest.unity`). Xem `Samples/README.md`.
