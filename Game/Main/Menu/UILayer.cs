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
        [OnReadyGet]
        private ColorRect tint;

        // Makes the back button run a specific function instead.
        //
        // Useful for when the uilayer is hidden and you want the
        // back button to go back to a specific menu.
        public Action BackButtonActionOverride { get; set; }
        public Screen CurrentScreen { get; private set; }

        public event Action<string, Node> ScreenChanged;

        [OnReady]
        public void RealReady()
        {
            foreach (var child in screenHolder.GetChildren())
                if (child is Screen screen)
                    screen.Construct(this);

            backButton.Connect("pressed", this, nameof(OnBackButonPressed));

            HideMessage();
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

            if (CurrentScreen.ParentScreen == null)
                HideBackButton();
            else
                ShowBackButton();
        }

        public void ShowScreen(string name, object args = null)
        {
            var newScreen = screenHolder.GetNode<Screen>(name);
            if (newScreen == null)
                return;
            ShowScreen(newScreen, args);
        }

        public void HideScreen()
        {
            if (CurrentScreen == null)
                return;
            CurrentScreen.Hide();
            CurrentScreen = null;
        }

        int messageIdx = -1;

        public async void ShowMessage(string text, float duration = -1)
        {
            messageLabel.Text = text;
            messageLabel.Visible = true;
            tint.Visible = true;
            messageIdx++;
            int currMessageIdx = messageIdx;
            // NOTE: If we have more than 100 messages running simulatenously,
            //       this could break, since the values loop back
            // TODO: Replace this with GDTask solution that has cancellation tokens
            if (messageIdx >= 100)
            {
                messageIdx = 0;
            }

            if (duration > 0)
            {
                await ToSignal(GetTree().CreateTimer(duration), "timeout");
                // We got interrupted! We don't want to hide the message anymore.
                if (messageIdx != currMessageIdx)
                    return;
                HideMessage();
            }
        }

        public void HideMessage()
        {
            messageLabel.Visible = false;
            tint.Visible = false;
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
            if (BackButtonActionOverride != null)
            {
                BackButtonActionOverride();
                BackButtonActionOverride = null;
            }
            else
                ShowScreen(CurrentScreen.ParentScreen);
        }
    }
}
