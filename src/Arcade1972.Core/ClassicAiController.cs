namespace Arcade1972.Core;

public sealed class ClassicAiController(
    Classic1972Rules rules,
    double reactionIntervalSeconds = 0.12,
    double aimOffset = 0)
{
    private double elapsedSinceReaction = double.MaxValue;
    private double currentAxis;

    public double Update(GameState state, double elapsedSeconds)
    {
        elapsedSinceReaction += Math.Max(0, elapsedSeconds);
        if (elapsedSinceReaction < reactionIntervalSeconds)
        {
            return currentAxis;
        }

        elapsedSinceReaction = 0;
        var paddleCenter = state.RightPaddle.Y + (rules.PaddleHeight / 2);
        var targetY = rules.FieldHeight / 2;

        if (state.Ball.VelocityX > 0)
        {
            var paddleX = rules.FieldWidth - rules.PaddleInset - rules.PaddleWidth;
            var secondsToPaddle = Math.Max(0, (paddleX - state.Ball.X) / state.Ball.VelocityX);
            targetY = ReflectWithinField(state.Ball.Y + (state.Ball.VelocityY * secondsToPaddle));
        }

        var error = targetY + aimOffset - paddleCenter;
        currentAxis = Math.Abs(error) < 1 ? 0 : Math.Sign(error);
        return currentAxis;
    }

    public void Reset()
    {
        elapsedSinceReaction = double.MaxValue;
        currentAxis = 0;
    }

    private double ReflectWithinField(double y)
    {
        var radius = rules.BallSize / 2;
        var span = rules.FieldHeight - (2 * radius);
        var period = 2 * span;
        var position = (y - radius) % period;
        if (position < 0)
        {
            position += period;
        }

        return radius + (position <= span ? position : period - position);
    }
}