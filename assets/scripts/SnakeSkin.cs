using Godot;

// Defines a collection of textures for a snake's appearance.
// The [GlobalClass] attribute makes this visible in the Godot editor's
[GlobalClass]
public partial class SnakeSkin : Resource
{
    [Export] public Texture2D HeadUpTexture { get; set; }
    [Export] public Texture2D HeadDownTexture { get; set; }
    [Export] public Texture2D HeadLeftTexture { get; set; }
    [Export] public Texture2D HeadRightTexture { get; set; }
    [Export] public Texture2D TailUpTexture { get; set; }
    [Export] public Texture2D TailDownTexture { get; set; }
    [Export] public Texture2D TailLeftTexture { get; set; }
    [Export] public Texture2D TailRightTexture { get; set; }
    [Export] public Texture2D BodyVerticalTexture { get; set; }
    [Export] public Texture2D BodyHorizontalTexture { get; set; }
    [Export] public Texture2D BodyTopLeftTexture { get; set; }    
    [Export] public Texture2D BodyTopRightTexture { get; set; }   
    [Export] public Texture2D BodyBottomLeftTexture { get; set; } 
    [Export] public Texture2D BodyBottomRightTexture { get; set; }

    public SnakeSkin()
    {
    }
} 