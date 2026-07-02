# Video Thumbnail Browser（WhiteBrowser風サムネイル動画ブラウザ）

複数フォルダを監視し、動画ファイルをサムネイル一覧で表示するWindows専用デスクトップアプリです。
サムネイル上でマウスを左右に動かすと、シークバーのようにプレビューフレームが切り替わります。

## 主な機能

- 複数フォルダの登録・監視（サブフォルダ再帰、新規ファイルの自動検出）
- 動画1本につき複数枚（既定10枚）のサムネイルを自動生成し、ホバーでスクラブプレビュー
- SQLiteによるキャッシュ（ファイルサイズ・更新日時が変わらない限り再生成しない）
- 数千〜数万ファイルでも軽く動くよう、行グルーピング＋WPF標準仮想化で一覧表示
- ファイル名検索、ダブルクリックで既定プレーヤー再生、右クリックでフォルダーを開く

## 必要環境

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Visual Studio 2022（任意。`dotnet build` でも可）
- **FFmpeg**（`ffmpeg.exe` と `ffprobe.exe`）— サムネイル生成に必須

## セットアップ手順

1. **FFmpegを用意する**
   - https://www.gyan.dev/ffmpeg/builds/ などから Windows用ビルドを取得
   - `ffmpeg.exe` と `ffprobe.exe` を、実行ファイルと同じ階層の `Tools` フォルダーに置く
     ```
     VideoThumbnailBrowser/
       VideoThumbnailBrowser.exe
       Tools/
         ffmpeg.exe
         ffprobe.exe
     ```
   - もしくはシステムのPATHに `ffmpeg`/`ffprobe` を追加していればそれを使います（`Tools`フォルダーが優先されます）
   - 見つからない場合、起動後のステータスバーに警告が表示されます

2. **ビルド**
   - Visual Studio: `VideoThumbnailBrowser.sln` を開いてビルド・実行（NuGetが自動復元されます）
   - コマンドライン:
     ```
     cd VideoThumbnailBrowser
     dotnet build
     dotnet run
     ```

3. **使い方**
   - 左サイドバーの「＋ フォルダーを追加」で監視フォルダを登録
   - 初回はサムネイル生成のためスキャンに時間がかかります（ステータスバーに進捗表示）
   - 以降は登録フォルダへの新規ファイル追加を自動検出し、サムネイルを生成します
   - サムネイル上でマウスを左右に動かすとプレビューが切り替わります
   - ダブルクリックで再生、右クリックで「フォルダーを開く」

## アーキテクチャ概要

```
Models/      VideoItem, WatchedFolder         … 純粋なデータ
Services/
  VideoFileTypes          拡張子による動画判定
  VideoScanner             フォルダ再帰スキャン
  FolderWatcherService     FileSystemWatcherラッパー（書き込み完了待ちを含む）
  FfmpegThumbnailGenerator ffmpeg/ffprobeを呼んでサムネイル生成（同時実行数を制限）
  ThumbnailCacheDb         SQLiteキャッシュ（サイズ・更新日時で再生成スキップ判定）
  AppSettings              監視フォルダ等をJSONで永続化
ViewModels/
  MainViewModel            全体の制御（フォルダ管理・スキャン・検索・行の再構築）
  VideoItemViewModel       1本の動画のUI状態（カバー画像＋スクラブ用フレーム）
  RowViewModel             列数ぶんをまとめた「1行」（仮想化のための単位）
Controls/
  ScrubThumbnailControl    ホバーでスクラブするサムネイルUI
```

### なぜ「行」でグルーピングしているか

WPF標準の `VirtualizingStackPanel` は縦方向のスタックしか仮想化できず、
タイル状のグリッドをそのまま仮想化する標準コントロールはありません。
そこで `ColumnCount` 列ぶんのアイテムを1つの `RowViewModel` にまとめ、
それを `VirtualizingStackPanel` で縦に並べることで、画面外の行は実体化されず、
数万ファイルでもスクロールが軽くなるようにしています。

### サムネイルのスクラブ表示

動画ごとに等間隔で複数枚（既定10枚）を事前生成し、ディスクとSQLiteにキャッシュします。
一覧表示時は「カバー画像（1枚目）」のみ読み込み、実際にマウスが乗ったときに
残りのフレームを遅延読み込みします。マウスのX座標に応じて表示フレームを切り替えるだけなので、
リアルタイムでの動画デコードは発生せず、軽快に動作します。

## 既知の制約・拡張のヒント

- フォルダ削除時、`FileSystemWatcher` を1つずつ解除する仕組みは未実装で、全フォルダの監視を作り直しています。フォルダ数が非常に多い場合は個別解除に変更すると効率的です。
- 検索やスキャンは現状アプリ内メモリで行っており、データベース化（フルテキスト検索等）すれば数十万ファイル規模にも耐えられます。
- ネットワークドライブ上のフォルダを監視する場合、`FileSystemWatcher` の挙動が不安定になることがあります。定期的なポーリングと併用すると安定します。
- サムネイル枚数・幅は `MainViewModel` 内の `FfmpegThumbnailGenerator` 初期化部分で調整できます。
- 現状はffmpeg.exeをプロセス起動で呼んでいるため、GPUデコード（`-hwaccel`オプション等）を追加すると大量ファイルのスキャンがさらに高速化できます。
