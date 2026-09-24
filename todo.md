# TODO

依照順序一次處理一個項目。每完成一項：

1. 執行該項目的測試或建置驗證。
2. 將項目從「進行中／待辦」移到「已完成」，並整理失效或重複的內容。
3. 將程式碼、測試、文件與本檔案放在同一個 Git commit。
4. 除非明確要求，不自動 push。

## 進行中

- [ ] 在主選單顯示今日剩餘次數與 UTC 重置資訊。

## 待辦

### 免費遊玩額度

- [ ] 額度耗盡時停用開局並顯示購買入口占位畫面。

### MSIX 與 Microsoft Store 前置

- [ ] 安裝並驗證 Visual Studio Windows App SDK、MSIX Packaging 與 Windows SDK 工作負載。
- [ ] 將目前 unpackaged WinUI 3 App 轉為 Packaged MSIX 專案。
- [ ] 建立正式 Package identity、圖示、啟動畫面與 zh-TW／en-US 資源。
- [ ] 在 Partner Center 建立 App identity，並關聯本機封裝專案。
- [ ] 建立 1 次與 10 次 Store-managed consumable 測試商品。

### Store 購買與扣次

- [ ] 定義 `IStoreGateway` 與 fake gateway，測試商品查詢、購買及餘額錯誤狀態。
- [ ] 實作 `StoreContext` adapter，顯示 Store 回傳的商品名稱與 formatted price。
- [ ] 實作購買防重入，以及取消、離線、未登入與伺服器錯誤介面。
- [ ] 免費額度耗盡時，付費局開始前連線確認 Store 餘額。
- [ ] 實作賽後 consumable fulfillment 與固定 tracking ID。
- [ ] 實作 pending fulfillment journal、重啟恢復與冪等重試。
- [ ] 透過 Partner Center private flight 驗證 1／10 次商品、跨裝置餘額與 Microsoft 禮品卡結帳。

### 遊戲與體驗

- [ ] 加入自行合成的球拍、牆面與得分音效，以及靜音／音量設定。
- [ ] 保存並還原視窗大小、位置與玩家明確選擇的顯示模式。
- [ ] 加入 Xbox 控制器支援與裝置拔除自動暫停。
- [ ] 視窗失焦或最小化時自動暫停並清除按鍵狀態。
- [ ] 加入遊戲內暫停、返回主選單與放棄比賽流程。
- [ ] 完成鍵盤導覽、高對比與 Narrator 可讀性驗證。
- [ ] 量測可信的原機參考資料，更新 `Classic1972Rules` 與 golden regression tests。

### 上架準備

- [ ] 建立隱私權政策、支援頁、IARC 分級與非 Atari 官方產品聲明。
- [ ] 製作 Store 圖示、螢幕截圖、商品文案及價格／市場設定。
- [ ] 建立 Windows CI：Release build、tests 與 MSIX bundle artifact。
- [ ] 在 Windows 10 22H2、Windows 11 與 100／150／200% DPI 執行驗收。
- [ ] 執行 Windows App Certification Kit 與至少 2 小時 soak test。

## 已完成

- [x] 將免費額度接到單人與雙人開局及賽果流程；開局建立 session，完成比賽才扣除。
- [x] 實作 Windows LocalState 額度儲存，使用原子替換並隔離損毀或無效的 JSON 狀態。
- [x] 實作每日免費 3 次的純核心模型，涵蓋 UTC 重置、完成才扣、跨日歸屬、回撥保護與冪等完成。
- [x] 建立 repository 專用 `AGENTS.md`，固定架構、產品、驗證與 Git 工作流程。
- [x] 建立 .NET solution、遊戲核心、WinUI 3 App 與 xUnit 測試專案。
- [x] 實作 120 Hz 固定步進、11 分勝負、八段反射、回擊加速與失分重設。
- [x] 實作本機雙人模式與具反應延遲的單人 AI。
- [x] 建立預設非全螢幕的自訂標題列視窗，以及玩家主動切換全螢幕功能。
- [x] 加入資訊按鈕、Player 1／Player 2 操作說明與閱讀時自動暫停。
- [x] 完成 Debug／Release 建置、核心測試與 WinUI 視窗啟動驗證。