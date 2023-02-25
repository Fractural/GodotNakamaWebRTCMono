using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using GDC = Godot.Collections;
using System;
using NakamaWebRTC;

namespace NakamaWebRTCDemo
{
    public partial class ConnectionScreen : Screen
    {
        [OnReadyGet]
        private TextEdit loginEmailField;
        [OnReadyGet]
        private TextEdit loginPasswordField;

        public static readonly string CredentialsFilePath = "user://credentials.json";

        public string Email { get; set; }
        public string Password { get; set; }

        [OnReady]
        public void RealReady()
        {
            TryLoadCredentials();
        }

        private void TryLoadCredentials()
        {
            var file = new File();
            if (file.FileExists(CredentialsFilePath))
            {
                file.Open(CredentialsFilePath, File.ModeFlags.Read);
                var result = JSON.Parse(file.GetAsText());
                if (result.Result is GDC.Dictionary dict)
                {
                    Email = (string)dict["email"];
                    Password = (string)dict["password"];

                    loginEmailField.Text = Email;
                    loginPasswordField.Text = Password;
                }
                file.Close();
            }
        }

        private void SaveCredentials()
        {
            var file = new File();
            file.Open(CredentialsFilePath, File.ModeFlags.Write);
            file.StoreLine(JSON.Print(new
            {
                email = Email,
                password = Password,
            }.ToGDDict()));
            file.Close();
        }

        public override void ShowScreen(GDC.Dictionary args = null)
        {
            base.ShowScreen(args);
            if (args != null)
            {
                if ()
            }
        }
    }
}
