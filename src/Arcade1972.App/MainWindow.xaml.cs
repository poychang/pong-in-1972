using System.Diagnostics;
using Arcade1972.Core;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.Graphics;
using VirtualKey = Windows.System.VirtualKey;
using Rect = Windows.Foundation.Rect;

namespace Arcade1972.App;

public sealed partial class MainWindow : Window
{
    private readonly DailyFreePlayQuota freePlayQuota;
    private readonly Classic1972Rules rules = new();
    private readonly ClassicGameSimulation simulation;
    private readonly ClassicAiController aiController;
    private readonly DispatcherQueueTimer gameTimer;
    private readonly Stopwatch frameClock = new();
    private FixedStepGameLoop gameLoop;
    private bool leftUpPressed;
    private bool leftDownPressed;
    private bool rightUpPressed;
    private bool rightDownPressed;
    private bool isOnePlayer;
    private bool resumeGameAfterInformation;
    private bool isCompletingMatch;
    private FreePlayMatchSession? activeFreePlaySession;

    public MainWindow(DailyFreePlayQuota freePlayQuota)
    {
        this.freePlayQuota = freePlayQuota;
        InitializeComponent();

        simulation = new ClassicGameSimulation(rules);
        aiController = new ClassicAiController(rules);
        gameLoop = new FixedStepGameLoop(simulation, simulation.CreateInitialState());
        gameTimer = DispatcherQueue.CreateTimer();
        gameTimer.Interval = TimeSpan.FromMilliseconds(8);
        gameTimer.Tick += GameTimer_Tick;

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        AppWindow.Resize(new SizeInt32(1024, 720));

        if (AppWindowTitleBar.IsCustomizationSupported())
        {
            AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
            AppWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;
        }

        Closed += MainWindow_Closed;
    }

    private void FullScreenButton_Click(object sender, RoutedEventArgs e)
    {
        var presenter = AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen
            ? AppWindowPresenterKind.Overlapped
            : AppWindowPresenterKind.FullScreen;

        AppWindow.SetPresenter(presenter);
    }

    private void AppTitleBar_Loaded(object sender, RoutedEventArgs e)
    {
        SetTitleBarInteractiveRegion();
    }

    private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        SetTitleBarInteractiveRegion();
    }

    private void SetTitleBarInteractiveRegion()
    {
        if (!ExtendsContentIntoTitleBar || AppTitleBar.XamlRoot is null)
        {
            return;
        }

        var scale = AppTitleBar.XamlRoot.RasterizationScale;
        var transform = TitleBarActions.TransformToVisual(null);
        var bounds = transform.TransformBounds(
            new Rect(0, 0, TitleBarActions.ActualWidth, TitleBarActions.ActualHeight));
        var region = new RectInt32(
            (int)Math.Round(bounds.X * scale),
            (int)Math.Round(bounds.Y * scale),
            (int)Math.Round(bounds.Width * scale),
            (int)Math.Round(bounds.Height * scale));

        var inputSource = InputNonClientPointerSource.GetForWindowId(AppWindow.Id);
        inputSource.SetRegionRects(NonClientRegionKind.Passthrough, [region]);
    }

    private void InformationButton_Click(object sender, RoutedEventArgs e)
    {
        ShowInformation();
    }

    private void CloseInformationButton_Click(object sender, RoutedEventArgs e)
    {
        HideInformation();
    }

    private void ShowInformation()
    {
        if (InformationOverlay.Visibility == Visibility.Visible)
        {
            return;
        }

        resumeGameAfterInformation = gameTimer.IsRunning;
        gameTimer.Stop();
        ClearInput();
        InformationOverlay.Visibility = Visibility.Visible;
        CloseInformationButton.Focus(FocusState.Programmatic);
    }

    private void HideInformation()
    {
        InformationOverlay.Visibility = Visibility.Collapsed;

        if (resumeGameAfterInformation)
        {
            frameClock.Restart();
            gameTimer.Start();
            InputSink.Focus(FocusState.Programmatic);
        }
        else
        {
            StartButton.Focus(FocusState.Programmatic);
        }

        resumeGameAfterInformation = false;
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        await TryStartMatchAsync(onePlayer: false);
    }

    private async void StartOnePlayerButton_Click(object sender, RoutedEventArgs e)
    {
        await TryStartMatchAsync(onePlayer: true);
    }

    private async Task TryStartMatchAsync(bool onePlayer)
    {
        SetStartButtonsEnabled(false);
        try
        {
            var startResult = await freePlayQuota.TryStartMatchAsync();
            if (!startResult.IsAllowed)
            {
                MenuHeading.Text = "NO FREE PLAYS";
                return;
            }

            activeFreePlaySession = startResult.Session;
        }
        catch (Exception)
        {
            MenuHeading.Text = "QUOTA UNAVAILABLE";
            return;
        }
        finally
        {
            if (activeFreePlaySession is null)
            {
                SetStartButtonsEnabled(true);
            }
        }

        isOnePlayer = onePlayer;
        aiController.Reset();
        gameLoop = new FixedStepGameLoop(simulation, simulation.CreateInitialState());
        MenuOverlay.Visibility = Visibility.Collapsed;
        ClearInput();
        RenderGame();
        InputSink.Focus(FocusState.Programmatic);
        frameClock.Restart();
        gameTimer.Start();
    }

    private async void GameTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        if (isCompletingMatch)
        {
            return;
        }

        var elapsed = frameClock.Elapsed;
        frameClock.Restart();

        var input = new PaddleInput(
            Axis(leftUpPressed, leftDownPressed),
            isOnePlayer
                ? aiController.Update(gameLoop.State, elapsed.TotalSeconds)
                : Axis(rightUpPressed, rightDownPressed));
        gameLoop.Advance(elapsed, input);
        RenderGame();

        if (gameLoop.State.Phase == MatchPhase.Finished)
        {
            gameTimer.Stop();
            ClearInput();
            isCompletingMatch = true;
            await CompleteFinishedMatchAsync();
        }
    }

    private async Task CompleteFinishedMatchAsync()
    {
        try
        {
            if (activeFreePlaySession is null)
            {
                throw new InvalidOperationException("The finished match has no quota session.");
            }

            var completion = await freePlayQuota.CompleteMatchAsync(activeFreePlaySession);
            if (completion == FreePlayCompletionStatus.QuotaExhausted)
            {
                ShowMenu("FREE PLAY LIMIT REACHED", allowNewMatch: false);
                return;
            }

            activeFreePlaySession = null;
            var heading = gameLoop.State.Winner == PlayerSide.Left
                ? "LEFT PLAYER WINS"
                : "RIGHT PLAYER WINS";
            ShowMenu(heading, allowNewMatch: true);
        }
        catch (Exception)
        {
            ShowMenu("QUOTA SAVE FAILED", allowNewMatch: false);
        }
        finally
        {
            isCompletingMatch = false;
        }
    }

    private void ShowMenu(string heading, bool allowNewMatch)
    {
        MenuHeading.Text = heading;
        StartButton.Content = "PLAY AGAIN";
        SetStartButtonsEnabled(allowNewMatch);
        MenuOverlay.Visibility = Visibility.Visible;
        if (allowNewMatch)
        {
            StartButton.Focus(FocusState.Programmatic);
        }
    }

    private void SetStartButtonsEnabled(bool isEnabled)
    {
        OnePlayerButton.IsEnabled = isEnabled;
        StartButton.IsEnabled = isEnabled;
    }

    private void RenderGame()
    {
        if (GameCanvas.ActualWidth <= 0 || GameCanvas.ActualHeight <= 0)
        {
            return;
        }

        var scale = Math.Min(
            GameCanvas.ActualWidth / rules.FieldWidth,
            GameCanvas.ActualHeight / rules.FieldHeight);
        var offsetX = (GameCanvas.ActualWidth - (rules.FieldWidth * scale)) / 2;
        var offsetY = (GameCanvas.ActualHeight - (rules.FieldHeight * scale)) / 2;
        var state = gameLoop.State;

        PositionRectangle(
            LeftPaddle,
            offsetX + (rules.PaddleInset * scale),
            offsetY + (state.LeftPaddle.Y * scale),
            rules.PaddleWidth * scale,
            rules.PaddleHeight * scale);
        PositionRectangle(
            RightPaddle,
            offsetX + ((rules.FieldWidth - rules.PaddleInset - rules.PaddleWidth) * scale),
            offsetY + (state.RightPaddle.Y * scale),
            rules.PaddleWidth * scale,
            rules.PaddleHeight * scale);
        PositionRectangle(
            Ball,
            offsetX + ((state.Ball.X - (rules.BallSize / 2)) * scale),
            offsetY + ((state.Ball.Y - (rules.BallSize / 2)) * scale),
            rules.BallSize * scale,
            rules.BallSize * scale);
        PositionRectangle(
            CenterLine,
            offsetX + ((rules.FieldWidth / 2) * scale),
            offsetY,
            Math.Max(1, scale),
            rules.FieldHeight * scale);

        LeftScore.Text = state.LeftScore.ToString();
        RightScore.Text = state.RightScore.ToString();
        Canvas.SetLeft(LeftScore, offsetX + (rules.FieldWidth * scale * 0.36));
        Canvas.SetTop(LeftScore, offsetY + (12 * scale));
        Canvas.SetLeft(RightScore, offsetX + (rules.FieldWidth * scale * 0.60));
        Canvas.SetTop(RightScore, offsetY + (12 * scale));
    }

    private static void PositionRectangle(
        FrameworkElement rectangle,
        double left,
        double top,
        double width,
        double height)
    {
        rectangle.Width = width;
        rectangle.Height = height;
        Canvas.SetLeft(rectangle, left);
        Canvas.SetTop(rectangle, top);
    }

    private static double Axis(bool upPressed, bool downPressed)
    {
        return (downPressed ? 1 : 0) - (upPressed ? 1 : 0);
    }

    private void InputSink_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.I)
        {
            ShowInformation();
        }
        else
        {
            SetKeyState(e.Key, true);
        }

        e.Handled = true;
    }

    private void InputSink_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        SetKeyState(e.Key, false);
        e.Handled = true;
    }

    private void SetKeyState(VirtualKey key, bool isPressed)
    {
        switch (key)
        {
            case VirtualKey.W:
                leftUpPressed = isPressed;
                break;
            case VirtualKey.S:
                leftDownPressed = isPressed;
                break;
            case VirtualKey.Up:
                rightUpPressed = isPressed;
                break;
            case VirtualKey.Down:
                rightDownPressed = isPressed;
                break;
        }
    }

    private void GameRoot_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (MenuOverlay.Visibility == Visibility.Collapsed)
        {
            InputSink.Focus(FocusState.Programmatic);
        }
    }

    private void GameCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        RenderGame();
    }

    private void InformationOverlay_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Escape or VirtualKey.I)
        {
            HideInformation();
            e.Handled = true;
        }
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        gameTimer.Stop();
        gameTimer.Tick -= GameTimer_Tick;
    }

    private void ClearInput()
    {
        leftUpPressed = false;
        leftDownPressed = false;
        rightUpPressed = false;
        rightDownPressed = false;
    }
}