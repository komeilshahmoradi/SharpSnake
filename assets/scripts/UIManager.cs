// Determines the text and visibility for UI elements based on game state
public class UIManager
{
    public string GetScoreText(int score)
    {
        return $"Score: {score}";
    }

    // Gets the main message text based on state
    public string GetMessageText(GameState state, int finalScore = 0)
    {
        switch (state)
        {
            case GameState.Ready:
                return "Ready! Press any key to start.";
            case GameState.GameOver:
                return $"Game Over! Score: {finalScore}\nPress any key to reset.";
            case GameState.Playing:
            default:
                return ""; // No message during play
        }
    }

    // Determines if the main message should be visible
    public bool GetMessageVisibility(GameState state)
    {
        // Only show message when Ready or Game Over
        return state == GameState.Ready || state == GameState.GameOver;
    }
} 