# FastLZMA2Net 工业级基础设施库改进报告

> 审查版本: 1.0.1 | 目标框架: .NET 8 | 审查日期: 2025

---

## 目录

1. [总体评价](#1-总体评价)
2. [P0 — 严重缺陷（必须修复）](#2-p0--严重缺陷必须修复)
3. [P1 — 重要改进（强烈建议）](#3-p1--重要改进强烈建议)
4. [P2 — 质量提升（推荐）](#4-p2--质量提升推荐)
5. [P3 — 长期演进（可选）](#5-p3--长期演进可选)
6. [测试改进](#6-测试改进)
7. [改进路线图](#7-改进路线图)

---

## 1. 总体评价

FastLZMA2Net 是一个封装 native fast-lzma2 C 库的 .NET 压缩库，提供了简单 API、上下文压缩和流式压缩/解压三种模式。代码结构清晰，P/Invoke 层设计合理，跨平台 RID 解析完整。

**已做得好的方面：**
- ✅ 使用 `LibraryImport` 源码生成器（现代 P/Invoke）
- ✅ 自定义 `DllImportResolver` 覆盖多平台 RID
- ✅ 提供 `ReadOnlySpan<byte>` 重载，减少内存拷贝
- ✅ 正确的 Dispose 模式 + 终结器
- ✅ 项目属性完善（符号包、Source Link 就绪）

**需要改进的方面：** 以下按优先级列出。

---

## 2. P0 — 严重缺陷（必须修复）

### 2.1 原生上下文分配失败未检查 → 潜在 AccessViolation

**文件:** `Compressor.cs:83`, `Decompressor.cs:25-29`, `CompressStream.cs:95`, `DecompressStream.cs:47-52`

`FL2_createCCtxMt()` / `FL2_createDCtx()` 等函数在内存不足时返回 `IntPtr.Zero`（NULL）。当前代码**从不检查返回值**，后续使用 NULL 指针调用任何 native 函数将导致 `AccessViolationException`（进程崩溃，无法被 catch 捕获）。

```csharp
// 当前代码 — 危险
_context = NativeMethods.FL2_createCCtxMt(nbThreads);
// 如果返回 0，下一行直接崩溃
CompressLevel = (int)compressLevel;

// 修复
_context = NativeMethods.FL2_createCCtxMt(nbThreads);
if (_context == IntPtr.Zero)
    throw new FL2Exception(FL2ErrorCode.MemoryAllocation);
```

**影响：** 生产环境内存压力下进程直接崩溃，无日志无恢复。

### 2.2 `ObjectDisposedException` 防护缺失

**文件:** `Compressor.cs`, `Decompressor.cs`, `CompressStream.cs`, `DecompressStream.cs`

所有公开方法在 `disposed == true` 后仍可调用 native 代码，使用已释放的 native 指针。

```csharp
// 应在每个公开方法开头添加
ObjectDisposedException.ThrowIf(disposed, this);
```

### 2.3 `DecompressStream.Dispose` 中 `GCHandle.Free()` 路径错误

**文件:** `DecompressStream.cs:276-288`

```csharp
protected override void Dispose(bool disposing)
{
    if (!disposed)
    {
        _innerStream.Dispose();   // ← 在 disposing=false 时也调用了托管对象！
        if (disposing)
        {
            inputBufferHandle.Free(); // ← Free 只在 disposing=true，但 handle 必须总是释放
        }
        NativeMethods.FL2_freeDStream(_context);
        disposed = true;
    }
}
```

**问题：**
1. `_innerStream.Dispose()` 应仅在 `disposing == true` 时调用（终结器中不应碰托管对象）。
2. `GCHandle.Free()` **必须** 在任何路径下都调用（它是非托管资源），否则 pinned 内存永不释放。
3. `CompressStream.cs:406-419` 同样的问题。

### 2.4 `FL2.Compress` / `FL2.Decompress` 的 `int` 溢出

**文件:** `FL2.cs:139`, `FL2.cs:173`, `FL2.cs:212` 等

```csharp
return compressed[..(int)code]; // code 是 nuint，超过 int.MaxValue (2GB) 直接溢出
```

对于工业级库，应至少在超过 `int.MaxValue` 时抛出明确异常，或提供返回 `Memory<byte>` 的重载。

### 2.5 `NativeMethods` 类不应为 `public`

**文件:** `NativeMethods.cs:7`

```csharp
public static unsafe partial class NativeMethods // ← 暴露了所有 native 函数指针签名
```

作为基础设施库，内部 P/Invoke 层不应对消费者可见。应改为 `internal`。

---

## 3. P1 — 重要改进（强烈建议）

### 3.1 线程安全问题

`Compressor` 和 `Decompressor` 持有 native 上下文指针，native 层**不是线程安全的**。当前无任何文档或保护措施防止多线程并发调用。

**建议：**
- 在 XML doc 中明确标注 "此类型不是线程安全的"
- 考虑在 Debug 模式下加入重入检测（`Interlocked.CompareExchange`）

### 3.2 `CompressStream` 语义混乱

当前 `CompressStream` 继承 `Stream` 但：
- `CanWrite = false`（第20行）— 但实际上提供了 `Write` 方法！
- `CanRead` 检查 `_innerStream.CanRead` — 但 CompressStream 不支持 Read！
- `Write()` 同时压缩+Flush（结束流），与 `Append()` 行为完全不同

**建议：**
- `CanWrite` 应返回 `true`，`CanRead` 应返回 `false`
- 统一语义：`Write` = 追加数据，`Flush` = 结束流，移除 `Append` 或将 `Append` 作为 `Write` 的实现

### 3.3 `FL2Buffer` 结构体不应为 `public`

**文件:** `FL2Buffer.cs`

`FL2InBuffer`、`FL2OutBuffer`、`FL2DictBuffer`、`FL2cBuffer` 是纯粹的 P/Invoke 互操作结构，不属于公共 API 契约。

### 3.4 异常类型设计

**文件:** `FL2Exception.cs`

```csharp
public class FL2Exception : Exception  // ← 直接继承 Exception
```

**问题：**
1. 缺少序列化构造函数（虽然 .NET 8 中不严格要求，但 `ISerializable` 仍是好习惯）
2. 缺少 `FL2Exception(string message, Exception innerException)` 构造函数
3. `GetErrorCode` / `IsError` / `GetErrorString` 是 `public static` — 应作为内部工具方法
4. 建议：对于参数校验错误映射到 `ArgumentOutOfRangeException`，而非全部使用 `FL2Exception`

### 3.5 参数验证不完整

多处缺少输入验证：

```csharp
// FL2.cs:130 — Level 没有范围检查
public static byte[] Compress(byte[] data, int Level) // PascalCase 参数命名也不规范

// FL2.cs:164 — 零长度 data 会导致 compressBound(0) 可能返回异常值
public static byte[] CompressMT(byte[] data, int Level, uint nbThreads)

// Compressor.cs:159 — srcPath/dstPath 未做 null/empty 检查
public unsafe nuint Compress(string srcPath, string dstPath)
```

### 3.6 `DirectFileAccessor` 使用 `MemoryMappedFileAccess.ReadWrite` 打开只读文件

**文件:** `FL2.cs:119`, `Compressor.cs:168`

读取源文件时使用 `ReadWrite` 权限，在只读文件系统或权限受限环境中会失败。应对源文件使用 `MemoryMappedFileAccess.Read`。

---

## 4. P2 — 质量提升（推荐）

### 4.1 API 命名规范

| 当前命名 | 问题 | 建议 |
|---------|------|------|
| `FL2.CompressMT` / `DecompressMT` | 缩写不直观 | `CompressMultiThreaded` 或提供 `nbThreads` 参数的重载 |
| `Compress(byte[] src, int Level)` | 参数 PascalCase | `level` (camelCase) |
| `FL2.FindCompressBound` | 动词前缀不一致 | `GetCompressBound` 或 `EstimateCompressedSize` |
| `Compressor.CompressAsync` | ✅ 正确 | — |
| `FL2Parameter.posBits` | camelCase 与其他枚举成员不一致 | `PosBits` |
| `LCLP_MAX` | SCREAMING_CASE | `LclpMax` |

### 4.2 缺少 `CancellationToken` 的真正协作取消

**文件:** `Compressor.cs:99`, `Decompressor.cs:93`

```csharp
public Task<byte[]> CompressAsync(byte[] src, int compressLevel, CancellationToken cancellationToken = default)
    => Task.Run(() => Compress(src, compressLevel), cancellationToken);
```

`Task.Run` 的 `CancellationToken` 仅在任务**启动前**有效。一旦压缩开始，取消请求被忽略。应利用 native 层的 `FL2_cancelCStream` 机制，或至少在文档中说明局限性。

### 4.3 应添加 `ConfigureAwait(false)`

库代码中的 async 方法应使用 `ConfigureAwait(false)` 避免捕获同步上下文。`CompressStream.cs` 中部分使用了，但不一致。

### 4.4 文件作用域命名空间

所有文件使用传统块级命名空间。建议统一为 file-scoped namespace 减少缩进（.NET 8 / C# 12 支持）：

```csharp
namespace FastLZMA2Net;  // file-scoped
```

### 4.5 `CompressionParameters` 应为不可变类型

**文件:** `CompressionParameters.cs`

当前是可变 `struct`（所有字段均为 public），容易被意外修改。建议：
- 字段改为 `readonly`
- 考虑使用 `readonly record struct`（C# 10+）

### 4.6 `FL2` 静态属性使用 Property 但实际是常量

```csharp
public static int DictSizeMin => 1 << 20;  // 每次调用都重新计算
public static int BlockOverlapMin => 0;
```

对于编译期常量，使用 `const` 更高效更语义化。

### 4.7 版本信息冗余

```csharp
public static readonly Version Version =
    typeof(FL2).Assembly.GetName().Version ?? new Version(1, 0, 1);
```

这个 fallback `1.0.1` 是硬编码的，与 `.csproj` 中的版本可能不同步。建议移除 fallback 或使用源码生成器注入版本。

---

## 5. P3 — 长期演进（可选）

### 5.1 多目标框架支持 (Multi-TFM)

当前仅 `net8.0`。工业级库通常支持：
- `net8.0`（当前 LTS）
- `net9.0`（当前 STS）
- `netstandard2.0` / `netstandard2.1`（最大兼容性）

```xml
<TargetFrameworks>net8.0;net9.0;netstandard2.0</TargetFrameworks>
```

### 5.2 macOS / FreeBSD 支持

`ImportResolver` 中缺少 macOS (`osx-x64`, `osx-arm64`) 支持。`fast-lzma2` C 库可以在 macOS 上编译。

### 5.3 `ArrayPool<byte>` 减少大数组分配

所有压缩/解压方法都 `new byte[...]` 分配大数组，对 GC 造成压力。

```csharp
// 当前
byte[] compressed = new byte[FL2.FindCompressBound(src)];
// ...
return compressed[..(int)code]; // 再分配一次！

// 建议
byte[] rented = ArrayPool<byte>.Shared.Rent((int)FL2.FindCompressBound(src));
try
{
    // ... 使用 rented
    return rented[..(int)code]; // 仍需拷贝，但 rented 会被归还
}
finally
{
    ArrayPool<byte>.Shared.Return(rented);
}
```

更进一步，提供接受 `IBufferWriter<byte>` 或 `Span<byte>` 目标的重载，让调用者控制内存。

### 5.4 SourceLink & Deterministic Build

```xml
<!-- 建议添加到 csproj -->
<PublishRepositoryUrl>true</PublishRepositoryUrl>
<EmbedUntrackedSources>true</EmbedUntrackedSources>
<Deterministic>true</Deterministic>
```

并添加 `Microsoft.SourceLink.GitHub` 包。

### 5.5 XML 文档完整性

部分公共方法缺少 XML 文档：
- `FL2.Decompress(ReadOnlySpan<byte>)`
- `FL2.DecompressMT(ReadOnlySpan<byte>, uint)`
- `FL2.EstimateCompressMemoryUsage` 各重载
- `Compressor.Compress(byte[])` （无参重载）
- `FL2.FindCompressBound(ReadOnlySpan<byte>)`

建议启用 `<GenerateDocumentationFile>true</GenerateDocumentationFile>` 和 `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` 来强制补全。

### 5.6 考虑 `IAsyncDisposable`

`CompressStream` 和 `DecompressStream` 作为 `Stream` 子类，应实现 `IAsyncDisposable`，支持 `await using` 模式。

### 5.7 Benchmark 项目

使用 BenchmarkDotNet 建立性能基准：
- 不同压缩级别吞吐量
- 单线程 vs 多线程
- 流式 vs 一次性
- 与 System.IO.Compression (Brotli/GZip/ZLib) 对比

---

## 6. 测试改进

### 6.1 测试结构问题

| 问题 | 详情 |
|------|------|
| **构造函数中有副作用** | `SimpleTest`, `CStreamTest`, `DStreamTest` 构造函数中写文件，每个测试方法都会执行 |
| **使用 `Debug.Assert` 而非 `Assert.IsTrue`** | `CStreamTest.cs:36,54,72` 使用 `Debug.Assert`，Release 构建下不生效 |
| **缺少 Dispose 测试** | 未验证 Compressor/Decompressor dispose 后再调用是否正确抛异常 |
| **缺少边界测试** | 空数组、极大数据、损坏数据、错误压缩级别的行为 |
| **缺少并发测试** | 多线程模式无并发安全性验证 |
| **测试命名不表意** | `Union`, `TestBlocked`, `TestDFA` 不表达测试行为 |
| **未使用 Dispose 模式** | `Compressor`/`Decompressor` 实例未包裹在 `using` 中 |

### 6.2 建议补充的测试用例

```
- 空字节数组压缩/解压
- 压缩数据被截断后解压 → 应抛 FL2Exception(CorruptionDetected)
- Compress 后 Dispose 再 Compress → 应抛 ObjectDisposedException
- CompressStream 写入 0 字节
- DecompressStream 从非 seekable 流读取
- CancellationToken 取消行为验证
- 各压缩级别(1-10)往返正确性
- 大文件(>2GB)边界
```

### 6.3 测试依赖版本过旧

```xml
<PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />  <!-- 有更新版本 -->
<PackageReference Include="coverlet.collector" Version="6.0.0" />       <!-- 有更新版本 -->
```

---

## 7. 改进路线图

### Phase 1 — 安全与正确性（1-2 周）
- [ ] 修复所有 P0 项（native 分配检查、Dispose 路径、int 溢出、访问级别）
- [ ] 补充 `ObjectDisposedException` 防护
- [ ] 修复 `CompressStream.CanWrite/CanRead` 语义
- [ ] 修复 `GCHandle.Free()` 在终结器路径中缺失

### Phase 2 — API 质量（2-3 周）
- [ ] 规范化命名（`posBits` → `PosBits`，`Level` → `level` 等）
- [ ] 将内部类型标记为 `internal`
- [ ] 补全 XML 文档，启用 `GenerateDocumentationFile`
- [ ] 参数验证（null、范围、空数组）
- [ ] 添加 SourceLink

### Phase 3 — 测试加固（2-3 周）
- [ ] 替换 `Debug.Assert` 为 `Assert.IsTrue`
- [ ] 补充边界、异常、Dispose 测试
- [ ] 移除构造函数副作用
- [ ] 建立 CI 覆盖率基线（目标 ≥ 80%）

### Phase 4 — 性能与生态（持续）
- [ ] `ArrayPool<byte>` 优化热路径
- [ ] 提供 `Span<byte>` / `IBufferWriter<byte>` 目标重载
- [ ] BenchmarkDotNet 性能基准
- [ ] 多 TFM 支持
- [ ] macOS 平台支持
- [ ] `IAsyncDisposable` 实现

---

## 附录：文件级问题速查表

| 文件 | P0 | P1 | P2 |
|------|:--:|:--:|:--:|
| `NativeMethods.cs` | 可见性 | — | — |
| `FL2.cs` | int 溢出 | 参数校验 | 命名 |
| `Compressor.cs` | NULL 检查, Disposed 检查 | 线程安全文档 | ArrayPool |
| `Decompressor.cs` | NULL 检查, Disposed 检查 | 线程安全文档 | ArrayPool |
| `CompressStream.cs` | GCHandle 泄漏, Disposed 检查 | CanWrite 语义 | IAsyncDisposable |
| `DecompressStream.cs` | GCHandle 泄漏, Disposed 检查 | — | IAsyncDisposable |
| `FL2Buffer.cs` | — | 可见性 | — |
| `FL2Exception.cs` | — | 构造函数 | — |
| `CompressionParameters.cs` | — | — | readonly |
| `FL2Parameter.cs` | — | — | 命名 |
| `DirectFileAccessor.cs` | — | 读写权限 | — |
