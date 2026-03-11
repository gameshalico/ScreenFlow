# Screen Flow

Unity 向けの画面ライフサイクル・ナビゲーションフレームワーク。

UniTask ベースの非同期画面遷移、キャンセル対応、複数のコンテナパターン（Slot / Stack / Tab）を提供します。

## 要件

- Unity 6000.3 以上
- [UniTask](https://github.com/Cysharp/UniTask) 2.5.10 以上

## インストール

Unity Package Manager で以下の git URL を追加:

```
https://github.com/gameshalico/screen-flow.git
```

## 基本概念

### スクリーンライフサイクル

`IScreen` を実装したクラスは以下のライフサイクルで管理されます。

```
Initialize → Enter → Run → (Exit) → Cleanup
```
※ 上位のコンテナを破棄した場合など、Exitは呼ばれずにCleanupされることがあります

| メソッド | 説明 |
|---------|------|
| `Initialize` | リソースの準備・初期化 |
| `Enter` | 表示開始（フェードイン等） |
| `Run` | 実行中のメインループ。キャンセルされるまで継続されることを想定 |
| `Exit` | 非表示処理（フェードアウト等） |
| `Cleanup` | リソース解放 |

### トランジション

`IScreenTransition` を実装した型で遷移パラメータを定義します。スクリーンが `ITransitionReceiver<T>` を実装していれば、遷移時に自動的にパラメータが適用されます。

```csharp
public sealed class FadeTransition : IScreenTransition
{
    public float Duration { get; init; } = 0.3f;
}

public sealed class MenuScreen : IScreen, ITransitionReceiver<FadeTransition>
{
    public void ApplyTransition(FadeTransition transition)
    {
        // トランジションパラメータを受け取る
    }

    // IScreen の各メソッドを実装...
}
```

### Suspend / Resume

`ISuspendableScreen` を実装すると、スタックで背面に回った際に `Suspend`、復帰時に `Resume` が呼ばれます。`Cleanup` されないためメモリを保持し、再初期化なしで高速に復帰できます。

## コンテナ

### ScreenSlot — 単一スクリーン

常に 1 つのアクティブスクリーンを保持します。`Show` で前のスクリーンを破棄して新しいスクリーンに切り替えます。

```csharp
var slot = new ScreenSlot();
slot.Register<FadeTransition>(t => new MenuScreen());

await slot.Show(new FadeTransition(), cancellationToken);
```

### ScreenStack — スタック

スクリーンを積み重ねて管理します。`Push` で新しいスクリーンを上に載せ、`Pop` で取り除きます。

```csharp
var stack = new ScreenStack();
stack.Register<FadeTransition>(t => new PauseScreen());

await stack.Push(new FadeTransition(), cancellationToken);
// 前のスクリーンは Suspend される（ISuspendableScreen 実装時）

await stack.Pop(cancellationToken);
// 前のスクリーンが Resume される
```

### ScreenTab\<TKey\> — タブ

キーでスクリーンを管理します。一度作成したタブは再利用され、切り替え時は `Enter` / `Exit` のみ実行されます。

```csharp
var tab = new ScreenTab<string>();
tab.Register<FadeTransition>(t => new SettingsScreen());

await tab.Show("settings", new FadeTransition(), cancellationToken);
await tab.Show("profile", new FadeTransition(), cancellationToken);

// 不要なタブの削除（現在アクティブなタブは削除不可）
await tab.Remove("settings", cancellationToken);
```

## ユーティリティ

### SceneScreenHelper

Unity シーンの加算ロード・アンロードを UniTask で行うヘルパーです。

```csharp
var scene = await SceneScreenHelper.Load("SceneName", cancellationToken);
await SceneScreenHelper.Unload(scene, cancellationToken);
```

### 拡張メソッド

トランジションのインスタンスを省略するための拡張メソッドが用意されています。

```csharp
// パラメータなしの new() で生成
await slot.Show<FadeTransition>(cancellationToken);
await stack.Push<FadeTransition>(cancellationToken);

// ファクトリでトランジションを無視する登録
slot.Register<FadeTransition>(() => new MenuScreen());
```

## 設計上の特徴

- **遷移のキューイング**: 複数の遷移リクエストは直列化され、順序が保証されます
- **CancellationToken 統合**: すべての非同期メソッドでキャンセルに対応

## ライセンス

[LICENSE](LICENSE) を参照してください。