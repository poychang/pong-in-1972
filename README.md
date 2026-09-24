# 1972 Arcade

一款受 1972 年早期投幣式電子桌球遊戲啟發的 Windows 遊戲。目前名稱與視覺皆為開發階段素材；本專案與 Atari 無關，也不使用原始遊戲的商標、程式、美術、字型或錄音。

## 目前進度

- 可調整大小的 WinUI 3 自訂標題列視窗，預設不進入全螢幕。
- 本機雙人模式：`W` / `S` 與方向鍵控制球拍。
- 單人模式：具反應延遲且受相同移動速度限制的電腦對手。
- 120 Hz 固定步進核心、八段球拍反射、回擊加速與 11 分勝負。
- 全螢幕由玩家透過標題列按鈕主動切換。
- 標題列資訊按鈕會顯示 Player 1／Player 2 按鍵，遊戲中開啟時會自動暫停。

Microsoft Store 消耗型商品、每日免費三次、MSIX 封裝與正式商店素材尚未實作，現階段不會顯示或執行任何購買流程。

## 建置與執行

需求：Windows 10 22H2 或 Windows 11，以及 .NET 10 SDK。

```powershell
dotnet restore pong-in-1972.sln
dotnet build pong-in-1972.sln -c Debug -p:Platform=x64
dotnet test tests/Arcade1972.Tests/Arcade1972.Tests.csproj -c Debug
dotnet run --project src/Arcade1972.App/Arcade1972.App.csproj -c Debug -p:Platform=x64
```

目前的 App 是供開發驗證使用的 unpackaged WinUI 3 應用。正式 MSIX/Store 建置仍需要安裝 Visual Studio 的 Windows App SDK 與 MSIX 工作負載，並與 Partner Center 的應用程式識別建立關聯。

## 專案結構

- `src/Arcade1972.Core`：不依賴 Windows UI 的 deterministic 遊戲規則。
- `src/Arcade1972.App`：WinUI 3 視窗、輸入與 XAML 畫面。
- `tests/Arcade1972.Tests`：物理、勝負、固定步進與 AI 測試。
- `docs/game-reference.md`：歷史規則依據與尚待量測的參數。
