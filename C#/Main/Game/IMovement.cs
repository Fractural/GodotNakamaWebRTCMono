using Godot;

namespace NakamaWebRTCDemo
{
    public interface IMovement
    {
        Vector2 Direction { get; set; }
        float Speed { get; set; }
    }
}
