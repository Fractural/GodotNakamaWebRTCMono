using Fractural.GodotCodeGenerator.Attributes;
using Godot;

namespace NakamaWebRTCDemo
{
    /// <summary>
    /// In charge of input for a player as well
    /// as synchronization when this node is a mere 
    /// puppet.
    /// </summary>
    public partial class PlayerInput : Node, IEnable
    {
        public enum ModeEnum
        {
            Control,
            Synchronize,
        }


        [OnReadyGet(OrNull = true)]
        private GamePlayer player;

        [Export]
        public bool Enabled { get; set; } = true;
        [Export]
        public ModeEnum Mode { get; set; } = ModeEnum.Control;
        [Export]
        public string InputPrefix { get; set; } = "";

        // NOTE: Synchronization is handled by IMovement and IAttack individually
        public override void _Process(float delta)
        {
            if (Mode != ModeEnum.Control || !Enabled)
                return;

            player.Movement.Direction = new Vector2(
                Input.GetActionStrength(InputPrefix + "right") - Input.GetActionStrength(InputPrefix + "left"),
                Input.GetActionStrength(InputPrefix + "down") - Input.GetActionStrength(InputPrefix + "up")
                ).Normalized();

            if (Input.IsActionJustReleased(InputPrefix + "attack"))
                player.Attack.Use();
        }
    }
}
