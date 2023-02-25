using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using System;
using GDC = Godot.Collections;

namespace NakamaWebRTCDemo
{
    public partial class UILayer : CanvasLayer
    {
        [OnReadyGet]
        private Control screenHolder;
        [OnReadyGet]
        private Label messageLabel;
        [OnReadyGet]
        private Button backButton;

        public Control CurrentScreen { get; private set; }

        public event Action<string, Node> OnScreenChanged;
        public event Action OnBackButtonPressed;

        [OnReady]
        public void RealReady()
        {
            foreach (var child in screenHolder.GetChildren())
                if (child is Screen screen)
                    screen.Construct(this);
        }

        public void ShowScreen(string name, GDC.Dictionary args = null)
        {
            if (args == null)
                args = new GDC.Dictionary();
            var screen = screenHolder.GetNode<Screen>(name);
            if (screen == null)
                return;

            if (CurrentScreen != null)
                HideScreen();
            screen.ShowScreen(args);
            CurrentScreen = screen;

            OnScreenChanged?.Invoke(name, CurrentScreen);
        }

        public void HideScreen()
        {
            if (CurrentScreen == null)
                return;
            CurrentScreen.Hide();
            CurrentScreen = null;
        }

        public void ShowMessage(string text)
        {
            messageLabel.Text = text;
            messageLabel.Visible = true;
        }

        public void HideMessage()
        {
            messageLabel.Visible = false;
        }
    }
}
