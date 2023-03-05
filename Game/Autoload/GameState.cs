using Godot;

namespace NakamaWebRTCDemo
{
    public class GameState : Node
    {
        public bool OnlinePlay { get; set; } = false;

        public static GameState Global;
        public override void _Ready()
        {
            if (Global != null)
            {
                QueueFree();
                return;
            }
            Global = this;
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                if (Global == this)
                    Global = null;
            }
        }
    }
}
