using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using System;

namespace NakamaWebRTCDemo
{
    public partial class Console : Control
    {
        public static Console Global { get; private set; }
        
        [OnReadyGet]
        private ColorRect tintRect;
        [OnReadyGet]
        private Button toggleButton;
        [OnReadyGet]
        private RichTextLabel outputLabel;

        // Called when the node enters the scene tree for the first time.
        [OnReady]
        public void RealReady()
        {
            if (Global != null)
            {
                QueueFree();
                return;
            }
            Global = this;
            toggleButton.Connect("toggled", this, nameof(OnButtonToggled));
            outputLabel.BbcodeText = "";
            OnButtonToggled(false);
        }

        public override void _Notification(int what)
        {
            if (what == NotificationPredelete)
            {
                if (Global == this)
                    Global = null;
            }
        }

        public static void Print(string text = "") 
        {
            GD.Print(text);
            Global.outputLabel.AppendBbcode($"{text}\n");
        }

        public static void PrintErr(string text = "")
        {
            GD.PrintErr(text);
            Global.outputLabel.AppendBbcode($"[color=red]{text}[/color]\n");
        }

        private void OnButtonToggled(bool toggled)
        {
            outputLabel.Visible = toggled;
            tintRect.Visible = toggled;
        }
    }
}