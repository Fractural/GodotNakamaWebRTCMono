using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using System;
using System.Collections.Generic;
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

        public Screen CurrentScreen { get; private set; }

        public event Action<string, Node> ScreenChanged;

        [OnReady]
        public void RealReady()
        {
            foreach (var child in screenHolder.GetChildren())
                if (child is Screen screen)
                    screen.Construct(this);

            backButton.Connect("pressed", this, nameof(OnBackButonPressed));

            ShowScreen(nameof(TitleScreen));
        }

        public void ShowScreen(Screen screen, object args = null)
        {
            if (screen == null)
                return;

            if (CurrentScreen != null)
                HideScreen();
            screen.ShowScreen(args);
            CurrentScreen = screen;

            ScreenChanged?.Invoke(screen.Name, CurrentScreen);
        }

        public void ShowScreen(string name, object args = null)
        {
            var newScreen = screenHolder.GetNode<Screen>(name);
            if (newScreen == null)
                return;
            ShowScreen(newScreen);
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

        public void ShowBackButton()
        {
            backButton.Visible = true;
        }

        public void HideBackButton()
        {
            backButton.Visible = false;
        }

        public void HideAll()
        {
            HideScreen();
            HideMessage();
            HideBackButton();
        }

        private void OnBackButonPressed()
        {
            ShowScreen(CurrentScreen.ParentScreen);
        }
    }
}
