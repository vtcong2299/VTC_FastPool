# VTC FastPool — Samples

Hai scene da setup san, mo len la chay duoc ngay:

| File | Muc dich |
|---|---|
| `FastPoolDemo.unity` | Demo truc quan, co GUI dieu khien |
| `FastPoolSelfTest.unity` | Chay bo test tu dong, doc ket qua o Console |
| `DemoBullet.prefab` | Prefab vien dan dung cho scene demo |

> Chay tung scene mot, dung ghep ca hai vao cung 1 scene: `FastPoolSelfTest` co goi
> `FastPool.ClearAll()` nen se don luon pool cua demo.

## 1. FastPoolDemo — demo truc quan

Mo `Samples/FastPoolDemo.unity` va bam **Play**.

Scene gom `Main Camera`, `Directional Light` va GameObject **FastPool Demo** (script `FastPoolDemo`,
truong `bulletPrefab` da tro san vao `DemoBullet.prefab`).

> Muon dung khong can scene: gan `FastPoolDemo` vao 1 GameObject rong va de trong `bulletPrefab`
> — demo se tu dung prefab luc runtime.

Man hinh se ban lien tuc cac vien dan xoay vong quanh goc, moi vien tu tra ve pool sau 2 giay.
Bang GUI goc trai hien so object dang nam trong pool / dang duoc dung / so lan `OnSpawn` / `OnReturn`,
kem cac nut `Spawn 10`, `Return All`, `Prewarm +20`, `Clear Pool`.

Quan sat trong Hierarchy: object `[FastPoolManager]` (o scene DontDestroyOnLoad) chua node con `DemoBullet`
la noi cac object nghi duoc cat giu. So luong object trong do khong tang len vo han — do la pool dang hoat dong.

## 2. FastPoolSelfTest — kiem chung hanh vi

Mo `Samples/FastPoolSelfTest.unity`, bam **Play** va doc Console.
Scene chi co GameObject **FastPool SelfTest** gan script cung ten — test tu dung prefab
ngay trong runtime nen khong phu thuoc asset nao.

Cac muc duoc kiem tra:

| Test | Kiem chung |
|---|---|
| Scale & transform | Scale goc cua prefab khong bi ep ve `Vector3.one` |
| Reuse & callbacks | Object duoc tai su dung dung instance, `OnSpawn`/`OnReturn` dung so lan |
| Child poolable | `IPoolable` nam o object con van nhan callback |
| Capacity | Pool khong vuot `capacity`, phan thua bi huy |
| ReturnAll | Tra toan bo object dang dung ve pool |
| Double return | Tra 2 lan khong nhan doi object trong pool |
| Custom parent | `ClearPool` khong huy parent do nguoi dung truyen vao |
| Pool id | Sau `ClearAll`, object cu khong the lot vao pool cua prefab khac |

> Test co in ra vai dong Warning `[FastPool] ...` — day la ket qua **mong doi**,
> moi dong deu duoc bao truoc bang `[SelfTest] 1 warning ngay duoi day la mong doi:`.
> Chi cac dong `FAIL` mau do moi la loi.

Ket qua cuoi cung: `[FastPool SelfTest] PASS n/n`.

## Ghi chu khi dung that

- `CreatePool(prefab, prewarmCount, poolParent, capacity)` — goi luc load man choi de tranh giat.
- `capacity` gioi han so object **giu san** trong pool, khong gioi han so object spawn dong thoi.
- Neu tu truyen `poolParent`, `ClearPool` se khong huy parent do (chi huy parent do FastPool tu tao).
- Nen luon `ReturnPool` thay vi `Destroy`. Neu lo `Destroy` tu ben ngoai, FastPool van tu don dep
  nho `OnDestroy` cua component noi bo.
