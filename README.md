# Video Thumbnail Browser

WhiteBrowser ライクな動画・書庫サムネイルブラウザです。複数フォルダを監視し、ファイルをサムネイルグリッドで一覧表示します。サムネイル上でマウスを左右に動かすとシークバーのようにプレビューフレームが切り替わります。

## 主な機能

- **サムネイルスクラブ** — ホバーで複数フレームをシーク再生
- **書庫対応** — ZIP/CBZ/RAR/CBR/7z等の表紙画像をサムネイル表示
- **フォルダ監視** — 複数フォルダをリアルタイム監視、新規ファイルを自動検出
- **D&Dでフォルダ登録** — サムネイルエリアにフォルダをドラッグ＆ドロップで追加
- **タグ・評価** — 星1〜5の評価、スラッシュ区切りでタグをグループ分け（例: `アニメ/SF`）
- **一括タグ付与** — 複数ファイルをCtrl/Shiftで選択して一括でタグを付与
- **種別フィルター** — 動画のみ・書庫のみに絞り込み
- **インクリメンタルサーチ** — `tag:アニメ` `rating:>=4` で即座に絞り込み
- **形態素解析トークン** — TinySegmenter移植によりファイル名を分割、クリックで検索追加
- **並べ替え** — ファイル名・登録日時・更新日時・再生回数・評価・サイズ・動画時間・タグ数
- **詳細パネル** — クリックで登録日時・再生回数・ファイルサイズ等を表示
- **DBプロファイル切替** — 用途別に複数のライブラリを `.profile` ファイルで管理
- **ページネーション** — 20件ごとにページ分割、ページ番号クリック/直接入力でジャンプ
- **起動アプリ登録** — 動画用・書庫用それぞれ3つまで登録（ダブルクリックでソフト1、右クリックでソフト2/3）
- **ファイル操作** — 右クリックからファイル名コピー・フルパスコピー・ごみ箱削除
- **SQLiteキャッシュ** — サムネイルをキャッシュ、変更のないファイルは再生成しない
- **2段階起動** — 起動直後にキャッシュから即表示、バックグラウンドでスキャン

## 必要環境

- Windows 10 / 11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（実行時は[.NET 8 Runtime](https://dotnet.microsoft.com/download/dotnet/8.0)のみでもOK）
- **FFmpeg**（`ffmpeg.exe` と `ffprobe.exe`）— 動画サムネイル生成に必須、**同梱していません**
- 書庫（RAR/7z等）のカバー抽出には**7-Zip**推奨（ZIP/CBZは不要）

## セットアップ

### 1. FFmpeg を用意する

- 公式ビルド: <https://www.gyan.dev/ffmpeg/builds/>
- `ffmpeg.exe` と `ffprobe.exe` を `Tools/` フォルダに置く

```
VideoThumbnailBrowser.exe
Tools/
  ffmpeg.exe
  ffprobe.exe
  7z.exe        ← 任意（RAR/7z書庫のサムネイル対応）
```

または PATH が通った場所でもOKです。

### 2. ビルド

```bash
git clone https://github.com/<あなたのユーザー名>/VideoThumbnailBrowser.git
cd VideoThumbnailBrowser
dotnet build
```

Visual Studio 2022 で `.sln` を開いてビルドしても構いません。

### 3. 使い方

1. 起動後、左上の☰でフォルダパネルを開き「＋」でフォルダを追加
   または、サムネイルエリアにフォルダをドラッグ＆ドロップでも追加可能
2. 初回はサムネイル生成のためスキャンに時間がかかります
3. サムネイル上でマウスを左右に動かすとフレームが切り替わります
4. ダブルクリックで登録ソフト1で再生（未登録なら既定のアプリ）

## ファイル構成

```
VideoThumbnailBrowser.exe
Tools/
  ffmpeg.exe
  ffprobe.exe
  7z.exe                ← 任意
メイン.profile          ← プロファイルの設定（監視フォルダ・起動ソフト等）
メイン.db               ← SQLiteキャッシュ（タグ・評価・再生回数）
active.txt              ← 最後に使ったプロファイル名
Thumbnails/
  メイン/               ← サムネイル画像（プロファイル別）
  仕事用/
```

## アップデート時に引き継ぐファイル

**旧バージョンから新バージョンに更新する際は以下をコピーしてください。**

| ファイル/フォルダ | 内容 | 必須 |
|---|---|---|
| `*.profile` | 監視フォルダ・起動ソフト設定 | ✅ |
| `*.db` | タグ・評価・再生回数のキャッシュ | ✅ |
| `active.txt` | 最後に使ったプロファイル | ✅ |
| `Thumbnails/` | 生成済みサムネイル画像 | 推奨（ないと再生成） |
| `Tools/` | FFmpeg・7z等 | ✅ |

**コピー不要なもの（自動生成されます）:**
- `bin/`、`obj/` フォルダ

## 検索構文

| 入力例 | 効果 |
|---|---|
| `アニメ` | ファイル名の部分一致 |
| `tag:アクション` | タグで絞り込み |
| `tag:アニメ/SF` | グループ付きタグで絞り込み |
| `rating:5` | 星5のみ |
| `rating:>=4` | 星4以上 |
| `アニメ tag:SF rating:>=3` | AND検索 |

## タグのグループ分け

タグ名にスラッシュを使うとグループ分けできます。

例: `アニメ/SF`、`アニメ/恋愛`、`実写/洋画`

タグフィルターのドロップダウンでグループ別に表示されます。

## ライセンス

MIT License

## 謝辞

- [FFmpeg](https://ffmpeg.org/) — 動画サムネイル生成
- [SQLite](https://www.sqlite.org/) / [Microsoft.Data.Sqlite](https://docs.microsoft.com/en-us/dotnet/standard/data/sqlite/)
- [SharpZipLib](https://github.com/icsharpcode/SharpZipLib) — ZIP/CBZ書庫対応
- [TinySegmenter](http://chasen.org/~taku/software/TinySegmenter/) — 日本語形態素解析（C#移植版を内包）
