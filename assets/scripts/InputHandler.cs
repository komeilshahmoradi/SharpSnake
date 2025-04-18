using Godot;
using System;

// Enum definition moved to enums/PlayerActionType.cs
// public enum PlayerActionType { ... }

// Struct to hold the detected action details
public readonly struct PlayerAction
{
    public PlayerActionType Type { get; }
    public Vector2I Direction { get; } // Only relevant for Move type

    public PlayerAction(PlayerActionType type, Vector2I direction = default)
    {
        Type = type;
        Direction = direction;
    }

    // Static factory methods for convenience
    public static PlayerAction None => new PlayerAction(PlayerActionType.None);
    public static PlayerAction StartOrReset => new PlayerAction(PlayerActionType.StartOrReset);
    public static PlayerAction Move(Vector2I direction) => new PlayerAction(PlayerActionType.Move, direction);
}

// Processes player input and returns the intended action
public class InputHandler // Does not inherit from Node
{
    // No longer needs Game instance
    // private Game _gameInstance;

    // No constructor needed, or a parameterless one
    public InputHandler()
    {
    }

    // Method called by Game._Input
    // Now takes the current state and returns the detected action
    public PlayerAction ProcessInput(InputEvent @event, GameState currentState)
    {
        // --- Start/Restart Input ---
        if ((currentState == GameState.Ready || currentState == GameState.GameOver) && @event is InputEventKey keyEvent && keyEvent.Pressed && !keyEvent.Echo)
        {
            return PlayerAction.StartOrReset;
        }

        // --- Direction Input (only during Playing state) ---
        if (currentState != GameState.Playing) return PlayerAction.None; // No action if not playing

        Vector2I requestedDirection = Vector2I.Zero;

        // Check mapped actions
        if (@event.IsActionPressed("move_up"))
        {
             requestedDirection = Vector2I.Up;
        }
        else if (@event.IsActionPressed("move_down"))
        {
             requestedDirection = Vector2I.Down;
        }
        else if (@event.IsActionPressed("move_left"))
        {
             requestedDirection = Vector2I.Left;
        }
        else if (@event.IsActionPressed("move_right"))
        {
            requestedDirection = Vector2I.Right;
        }

        // If a valid direction was requested, return a Move action
        if (requestedDirection != Vector2I.Zero)
        {
            return PlayerAction.Move(requestedDirection);
        }

        // No relevant input detected
        return PlayerAction.None;
    }
} 