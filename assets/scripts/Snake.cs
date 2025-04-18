using Godot;
using System.Collections.Generic;
using System.Linq;

public partial class Snake : Node2D
{
    [Export] public SnakeSkin Skin { get; private set; }
    public Vector2I Direction { get; private set; } = Vector2I.Right;
    public Vector2I NextDirection { get; private set; } = Vector2I.Right;
    private List<Vector2I> _bodySegments = new List<Vector2I>();
    private List<Sprite2D> _bodySprites = new List<Sprite2D>();
    private bool _shouldGrow = false;
    private int _gridSize; // Needed for positioning sprites
    private Vector2I _viewportGridSize; // Needed for wall collision check


    public override void _Ready()
    {
        // Check if the Skin resource was assigned (Useful editor warning)
        if (Skin == null)
        {
            GD.PrintErr($"Snake ({this.Name}) requires a SnakeSkin resource assigned in the Inspector! Game may fail to start.");
        }
        // ... (Optional texture checks within Skin)
    }


    // Initialize now returns bool to indicate success
    public bool Initialize(Vector2I startPosition, int gridSize, Vector2I viewportGridSize)
    {
        _gridSize = gridSize;
        _viewportGridSize = viewportGridSize;
        return Reset(startPosition); // Return the success/failure of Reset
    }

    // Reset now returns bool to indicate success
    public bool Reset(Vector2I startPosition)
    {
        // CRITICAL CHECK: Ensure Skin is assigned before proceeding
        if (Skin == null)
        {
             GD.PrintErr($"Snake ({this.Name}) cannot Reset: Missing SnakeSkin resource!");
             return false; // Indicate failure
        }

        // Clear existing sprites and segments
        foreach (var sprite in _bodySprites)
        {
            sprite.QueueFree();
        }
        _bodySprites.Clear();
        _bodySegments.Clear();

        Direction = Vector2I.Right;
        NextDirection = Vector2I.Right;
        _shouldGrow = false;

        // Initial snake (e.g., 3 segments: head, body, tail) facing right
        Vector2I head = startPosition;
        Vector2I body = startPosition - new Vector2I(1, 0);
        Vector2I tail = startPosition - new Vector2I(2, 0);

        _bodySegments.Add(tail);
        _bodySegments.Add(body);
        _bodySegments.Add(head);

        // Create initial sprites using textures from Skin
        // These Get...Texture calls are now safe because we checked Skin above
        AddSegmentSprite(tail, GetTailTexture(Vector2I.Right));
        AddSegmentSprite(body, GetBodyTexture(Vector2I.Right, Vector2I.Right)); // Assuming straight horizontal initially
        AddSegmentSprite(head, GetHeadTexture(Vector2I.Right));
        UpdateSpriteTexturesAndRotations();

        return true; // Indicate success
    }

    public void SetNextDirection(Vector2I requestedDirection)
    {
        // Prevent immediate 180 turns
        if (Direction != -requestedDirection)
        {
            NextDirection = requestedDirection;
        }
    }

    // Returns true if move was successful, false if collision occurred
    public bool Move()
    {
        Direction = NextDirection;
        Vector2I currentHead = GetHeadPosition();
        Vector2I nextHead = currentHead + Direction;

        GD.Print($"Move check: nextHead={nextHead}, _viewportGridSize={_viewportGridSize}");

        // 1. Check Wall Collision
        if (nextHead.X < 0 || nextHead.X >= _viewportGridSize.X ||
            nextHead.Y < 0 || nextHead.Y >= _viewportGridSize.Y)
        {
            GD.Print("Collision: Wall");
            return false;
        }

        // 2. Check Self Collision (ignore the tail tip if not growing)
        // Need to check against the *future* position of the body parts *before* moving the tail.
        // Check against all segments except the very last one (the tail tip) which will move away.
        int checkLimit = _bodySegments.Count -1; // Check all except the current tail position
        for (int i = 0; i < checkLimit; i++)
        {
             if (_bodySegments[i] == nextHead)
             {
                 GD.Print($"Collision: Self at {nextHead} with segment {i} at {_bodySegments[i]}");
                 return false;
             }
        }


        // Move logic: Add new head, remove tail (if not growing)
        _bodySegments.Add(nextHead);
        // Add new head sprite temporarily, texture/rotation updated later
        AddSegmentSprite(nextHead, GetHeadTexture(Direction));


        if (_shouldGrow)
        {
            _shouldGrow = false; // Reset grow flag
             // Don't remove tail segment/sprite
            GD.Print($"Grew! New length: {_bodySegments.Count}");
        }
        else
        {
             // Remove tail data and sprite
             Vector2I tailPos = _bodySegments.First();
             _bodySegments.RemoveAt(0);
             Sprite2D tailSprite = _bodySprites.First();
              _bodySprites.RemoveAt(0);
             tailSprite.QueueFree();
        }

        // Update textures and rotations for head, new body segment, and new tail
        UpdateSpriteTexturesAndRotations();


        return true;
    }

    public void Grow()
    {
        _shouldGrow = true;
    }

    public Vector2I GetHeadPosition()
    {
         if (_bodySegments.Count == 0) return Vector2I.Zero;
        return _bodySegments.Last();
    }

    public bool IsPositionOnSnake(Vector2I position)
    {
        return _bodySegments.Contains(position);
    }


    // Helper to add a sprite node for a segment
    private void AddSegmentSprite(Vector2I gridPosition, Texture2D texture)
    {
        Sprite2D sprite = new Sprite2D();
        sprite.Texture = texture;
        sprite.Position = GetPixelCoordinates(gridPosition) + new Vector2(_gridSize / 2.0f, _gridSize / 2.0f);
        AddChild(sprite);
        _bodySprites.Add(sprite);
    }


    // Updates the textures and rotations of the head, tail, and adjacent body parts
    private void UpdateSpriteTexturesAndRotations()
    {
        if (_bodySegments.Count < 1) return;

        // --- Update Head ---
        Sprite2D headSprite = _bodySprites.Last();
        Vector2I headPos = _bodySegments.Last();
        headSprite.Texture = GetHeadTexture(Direction);
        headSprite.Rotation = 0; // Assuming sprites are pre-rotated
        headSprite.Position = GetPixelCoordinates(headPos) + new Vector2(_gridSize / 2.0f, _gridSize / 2.0f); // Center

        if (_bodySegments.Count < 2) return; // Only head exists, nothing more to update

        // --- Update Tail ---
        Sprite2D tailSprite = _bodySprites.First();
        Vector2I tailPos = _bodySegments[0];
        Vector2I prevToTailPos = _bodySegments[1]; // Segment before the tail
        Vector2I tailPointingDir = prevToTailPos - tailPos; // Direction tail is pointing away from body
        tailSprite.Texture = GetTailTexture(tailPointingDir);
        tailSprite.Rotation = 0; // Assuming sprites are pre-rotated
        tailSprite.Position = GetPixelCoordinates(tailPos) + new Vector2(_gridSize / 2.0f, _gridSize / 2.0f); // Center

        // --- Update Body Segments ---
        // Iterate through the actual body segments (skip head and tail)
        for (int i = 1; i < _bodySegments.Count - 1; i++)
        {
            Sprite2D bodySprite = _bodySprites[i];
            Vector2I currentPos = _bodySegments[i];
            Vector2I nextPos = _bodySegments[i + 1]; // Towards head
            Vector2I prevPos = _bodySegments[i - 1]; // Towards tail

            Vector2I dirFromPrev = currentPos - prevPos;
            Vector2I dirToNext = nextPos - currentPos;

            bodySprite.Texture = GetBodyTexture(dirFromPrev, dirToNext);
            bodySprite.Rotation = 0; // Assuming corner sprites are pre-rotated
            bodySprite.Position = GetPixelCoordinates(currentPos) + new Vector2(_gridSize / 2.0f, _gridSize / 2.0f); // Center
        }
    }


    // --- Texture Access Methods (Now assume Skin is not null) ---
    private Texture2D GetHeadTexture(Vector2I dir)
    {
        // Removed Skin null check
        if (dir == Vector2I.Up) return Skin.HeadUpTexture;
        if (dir == Vector2I.Down) return Skin.HeadDownTexture;
        if (dir == Vector2I.Left) return Skin.HeadLeftTexture;

        return Skin.HeadRightTexture; // Default/fallback
    }

    private Texture2D GetTailTexture(Vector2I dir) // Direction tail points away from body
    {
        // If tail segment is to the LEFT of the next segment (dir = Right),
        // use the tail sprite that visually points LEFT.
        if (dir == Vector2I.Right) return Skin.TailLeftTexture;

        // If tail segment is to the RIGHT of the next segment (dir = Left),
        // use the tail sprite that visually points RIGHT.
        if (dir == Vector2I.Left) return Skin.TailRightTexture;

        // If tail segment is BELOW the next segment (dir = Up),
        // use the tail sprite that visually points DOWN.
        if (dir == Vector2I.Up) return Skin.TailDownTexture;

        // If tail segment is ABOVE the next segment (dir = Down),
        // use the tail sprite that visually points UP.
        return Skin.TailUpTexture;
    }

    // Determines body texture based on direction from previous segment and to next segment
    private Texture2D GetBodyTexture(Vector2I fromDir, Vector2I toDir)
    {
        // Removed Skin null check
        if (fromDir == toDir)
        {
            // Use vertical body piece if moving up/down, horizontal otherwise
            return (fromDir.Y != 0) ? Skin.BodyVerticalTexture : Skin.BodyHorizontalTexture;
        }
        else // Corner piece
        {
            // Determine the correct corner sprite based on the turn
            if ((fromDir == Vector2I.Right && toDir == Vector2I.Up) || (fromDir == Vector2I.Down && toDir == Vector2I.Left)) return Skin.BodyTopLeftTexture; // Turn top-left
            if ((fromDir == Vector2I.Left && toDir == Vector2I.Up) || (fromDir == Vector2I.Down && toDir == Vector2I.Right)) return Skin.BodyTopRightTexture; // Turn top-right
            if ((fromDir == Vector2I.Right && toDir == Vector2I.Down) || (fromDir == Vector2I.Up && toDir == Vector2I.Left)) return Skin.BodyBottomLeftTexture; // Turn bottom-left
            if ((fromDir == Vector2I.Left && toDir == Vector2I.Down) || (fromDir == Vector2I.Up && toDir == Vector2I.Right)) return Skin.BodyBottomRightTexture; // Turn bottom-right

            // Fallback/Error case - should not happen with cardinal directions
            GD.PrintErr($"Invalid turn: from {fromDir} to {toDir}");
            return Skin.BodyHorizontalTexture; // Default fallback
        }
    }

    private Vector2 GetPixelCoordinates(Vector2I gridPosition)
    {
        return (Vector2)gridPosition * _gridSize;
    }
} 