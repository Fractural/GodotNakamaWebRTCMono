using Godot;

namespace NakamaWebRTCDemo
{
    public interface IMovement : IEnable
    {
        Vector2 Direction { get; set; }
        float Speed { get; set; }
    }
}
