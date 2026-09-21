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

Sample **Demo & Self Test** có scene demo (`FastPoolDemo.unity`) và bộ self-test
(`FastPoolSelfTest.unity`). Import qua Package Manager — xem mục cuối.

## Mở scene sample

Sample **không** được import sẵn. Vào **Window ▸ Package Manager ▸ VTC Fast Pool ▸ Samples ▸ Import**.

Unity copy sample vào `Assets/Samples/VTC Fast Pool/<version>/Demo & Self Test/`, từ đó mở scene bình thường.

> **Vì sao phải import?** Package cài qua git URL nằm trong `Library/PackageCache` và ở
> chế độ read-only. Unity **cấm mở scene nằm trong package read-only** — báo
> *"It is not allowed to open a scene in a read-only package"*. Cơ chế Samples của UPM
> sinh ra chính để giải quyết việc này: nó copy sample sang `Assets/` nơi file ghi được.
