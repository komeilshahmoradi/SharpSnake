using Godot;
using System;

// Assuming a Grid-based movement system
public partial class Game : Node2D
{
    [Export] public int GridSize = 40;// Based on the size of the snake skin
    [Export] public Vector2I ViewportGridSize = new Vector2I(25, 15);
    [Export] public PackedScene FoodScene; // Scene for the food item (Sprite2D + optional script)
    [Export] public PackedScene SnakeScene; // Scene containing just the Snake script attached to a Node2D
    [Export] public AudioStreamPlayer EatSoundPlayer;
    [Export] public AudioStreamPlayer GameOverSoundPlayer;
    [Export] public AudioStreamPlayer BackgroundMusicPlayer;
    [Export] public Label ScoreLabel { get; private set; }
    [Export] public Label MessageLabel { get; private set; }

    // Game Objects
    private Snake _snake;
    private Node2D _food;

    // Game State
    // Enum definition moved to enums/GameState.cs
    // public enum GameState { Ready, Playing, GameOver }
    private GameState _currentState = GameState.Ready;
    private int _score = 0;

    // Input Handler instance
    private InputHandler _inputHandler;
    // UI Manager instance
    private UIManager _uiManager;

    // Timing
    private Timer _moveTimer;
    private float _moveInterval = 0.2f; // Slightly faster

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        if (SnakeScene == null) GD.PushError("SnakeScene not set in SnakeGame inspector!");
        if (FoodScene == null) GD.PushError("FoodScene not set in SnakeGame inspector!");
        // Check other exported nodes too
        if (EatSoundPlayer == null) GD.PushWarning("EatSoundPlayer not assigned in SnakeGame inspector.");
        if (GameOverSoundPlayer == null) GD.PushWarning("GameOverSoundPlayer not assigned (currently unused).");
        if (BackgroundMusicPlayer == null) GD.PushWarning("BackgroundMusicPlayer not assigned in Game inspector.");
        if (ScoreLabel == null) GD.PushWarning("ScoreLabel not assigned in Game inspector."); // Added check
        if (MessageLabel == null) GD.PushWarning("MessageLabel not assigned in Game inspector."); // Added check

        // Create the Input Handler instance
        _inputHandler = new InputHandler(); // No longer needs 'this'
        // Create the UI Manager instance
        _uiManager = new UIManager();

        _moveTimer = new Timer();
        AddChild(_moveTimer);
        _moveTimer.WaitTime = _moveInterval;
        _moveTimer.OneShot = false;
        _moveTimer.Timeout += OnMoveTimerTimeout;

        ResetGame();
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        // Can potentially update UI animations or other per-frame logic here
    }

    // --- Input Handling (Receives Action from Handler) ---
    public override void _Input(InputEvent @event)
    {
        // Get the detected action from the handler
        PlayerAction action = _inputHandler.ProcessInput(@event, _currentState);

        // Execute the action
        switch (action.Type)
        {
            case PlayerActionType.StartOrReset:
                if (_currentState == GameState.Ready) StartGame();
                else if (_currentState == GameState.GameOver) ResetGame();
                GetViewport().SetInputAsHandled();
                break;

            case PlayerActionType.Move:
                if (_currentState == GameState.Playing && _snake != null)
                {
                    _snake.SetNextDirection(action.Direction);
                    GetViewport().SetInputAsHandled();
                }
                break;

            case PlayerActionType.None:
            default:
                // Do nothing, don't handle event
                break;
        }
    }

    // --- Existing Game Logic Methods (can be private) ---
    private void StartGame()
    {
        GD.Print("Starting Game!");
        _score = 0;
        _currentState = GameState.Playing;

        // Update UI using UIManager results
        UpdateUI();

        // Ensure snake is ready (ResetGame should handle this)
        if (_snake == null)
        {
             GD.PrintErr("Snake is null at StartGame, resetting.");
             ResetGame(); // Attempt recovery
             if (_snake == null) return; // If still null, abort
        }
        // Ensure initial food is placed
        if (_food == null) SpawnFood();

        _moveTimer.Start();
    }

    private void EndGame()
    {
        GD.Print("Game Over!");
        _currentState = GameState.GameOver;
        _moveTimer.Stop();
        BackgroundMusicPlayer?.Stop(); // Stop background music
        GameOverSoundPlayer?.Play(); // Play game over sound effect (if assigned)

        // Update UI using UIManager results
        UpdateUI();
    }

    private void ResetGame()
    {
        GD.Print("Resetting Game!");
        _currentState = GameState.Ready;
        _score = 0;

        // Stop timer before resetting UI/objects
        _moveTimer.Stop();

        // Update UI to show "Ready"
        UpdateUI();

        // Remove existing food
        _food?.QueueFree();
        _food = null;

        // Remove existing snake node if it exists
        _snake?.QueueFree();
        _snake = null;

        // Instantiate and initialize a new snake
        if (SnakeScene != null)
        {
            Node snakeNode = SnakeScene.Instantiate();
            if (snakeNode is Snake snakeInstance)
            {
                _snake = snakeInstance;
                AddChild(_snake);

                // Initialize the snake and check for success (skin assignment)
                Vector2I startPos = ViewportGridSize / 2;
                bool initializedSuccessfully = _snake.Initialize(startPos, GridSize, ViewportGridSize);

                if (!initializedSuccessfully)
                {
                    GD.PrintErr("Snake failed to initialize (likely missing Skin resource). Halting game setup.");
                    _snake.QueueFree(); // Clean up the failed instance
                    _snake = null;
                    // Update UI to show error
                    if (_uiManager != null && MessageLabel != null) {
                        MessageLabel.Text = "ERROR: Missing Snake Skin resource!";
                        MessageLabel.Visible = true;
                    }
                    // Ensure game stays in a non-playable state
                    _currentState = GameState.GameOver; // Or a new Error state
                    return; // Stop ResetGame here
                }
                 GD.Print($"Snake initialized at {startPos}");
            }
            else
            {
                 GD.PrintErr("Instantiated node from SnakeScene is not a Snake type!");
                 snakeNode.QueueFree();
                 return;
            }
        }
        else
        {
             GD.PrintErr("Cannot reset: SnakeScene is not assigned!");
             return;
        }

        // Spawn the first food item (only if snake initialized successfully)
        SpawnFood();

        // Play background music if assigned
        BackgroundMusicPlayer?.Play();
    }

    private void OnMoveTimerTimeout()
    {
        if (_currentState != GameState.Playing || _snake == null) return;

        if (!_snake.Move()) // Ask snake to move, check for collision
        {
            EndGame(); // Snake reported collision
        }
        else
        {
            // Check for food collision
            if (SnakeAteFood())
            {
                _score++;
                // Update UI Score only
                if (ScoreLabel != null && _uiManager != null)
                {
                     ScoreLabel.Text = _uiManager.GetScoreText(_score);
                }
                EatSoundPlayer?.Play();
                _snake.Grow();
                _food.QueueFree(); // Remove eaten food
                _food = null;
                SpawnFood(); // Spawn new food
            }
        }
    }

    private bool SnakeAteFood()
    {
        // Need null checks
        if (_food == null || _snake == null) return false;

        // Compare the grid position of the snake's head to the grid position of the food
        return _snake.GetHeadPosition() == GetGridCoordinates(_food.Position);
    }

    private void SpawnFood()
    {
        // Remove existing food just in case
        _food?.QueueFree();
        _food = null;

        if (FoodScene == null)
        {
            GD.PrintErr("Cannot spawn food: FoodScene not assigned!");
            return;
        }

        // Find a valid random position on the grid, not on the snake
        Random random = new Random();
        Vector2I foodPosGrid;
        int attempts = 0;
        const int maxAttempts = 100; // Prevent infinite loop if grid is full
        bool isOnSnake;

        do
        {
            foodPosGrid = new Vector2I(
                random.Next(1, ViewportGridSize.X - 1),
                random.Next(1, ViewportGridSize.Y - 1)
            );
            attempts++;

            // Check if the generated position is on the snake
            isOnSnake = false;
            if (_snake != null)
            {
                isOnSnake = _snake.IsPositionOnSnake(foodPosGrid);
            }
            GD.Print($"Spawn attempt {attempts}: pos={foodPosGrid}, isOnSnake={isOnSnake}"); // Debug Print

            if (attempts > maxAttempts)
            {
                 GD.PrintErr($"Failed to find empty spot for food after {maxAttempts} attempts. Grid full?");
                 return;
            }
        } while (isOnSnake); // Loop only if it IS on the snake

        // Instantiate the Food scene and place it
        // Assuming food scene root is Node2D or inherits from it (like Sprite2D)
        Node foodNode = FoodScene.Instantiate();
        if (foodNode is Node2D foodInstance)
        {
            _food = foodInstance;
            AddChild(_food);
            // Position the food node's top-left corner at the grid coordinate
            _food.Position = GetPixelCoordinates(foodPosGrid);
             // If the food sprite itself needs centering *within* its node,
             // handle that in Food.tscn (e.g., Sprite2D Offset or parent Node2D)
             GD.Print($"Food spawned at grid position: {foodPosGrid}, pixel pos: {_food.Position}");
        }
        else
        {
             GD.PrintErr("Instantiated node from FoodScene is not a Node2D type!");
             foodNode.QueueFree(); // Clean up
        }
    }

    // Helper to get grid coordinates from pixel coordinates (top-left)
    public Vector2I GetGridCoordinates(Vector2 pixelPosition)
    {
        // Floor to handle positions precisely
        return new Vector2I(
             (int)Math.Floor(pixelPosition.X / GridSize),
             (int)Math.Floor(pixelPosition.Y / GridSize)
         );
    }

    // Helper to get pixel coordinates (top-left corner) from grid coordinates
    public Vector2 GetPixelCoordinates(Vector2I gridPosition)
    {
        return (Vector2)gridPosition * GridSize;
    }

    // --- UI Update Helper ---
    private void UpdateUI()
    {
        if (_uiManager == null) return;

        if (ScoreLabel != null)
        {
            ScoreLabel.Text = _uiManager.GetScoreText(_score);
        }
        if (MessageLabel != null)
        {
            MessageLabel.Text = _uiManager.GetMessageText(_currentState, _score);
            MessageLabel.Visible = _uiManager.GetMessageVisibility(_currentState);
        }
    }
} 