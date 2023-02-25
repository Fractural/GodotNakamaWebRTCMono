﻿using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using GDC = Godot.Collections;
using System;
using NakamaWebRTC;
using Nakama;

namespace NakamaWebRTCDemo
{
    public partial class ConnectionScreen : Screen
    {
        [OnReadyGet]
        private TextEdit loginEmailField;
        [OnReadyGet]
        private TextEdit loginPasswordField;
        [OnReadyGet]
        private TextEdit createAccountUsernameField;
        [OnReadyGet]
        private TextEdit createAccountEmailField;
        [OnReadyGet]
        private TextEdit createAccountPasswordField;
        [OnReadyGet]
        private Button createAccountButton;
        [OnReadyGet]
        private Button loginButton;
        [OnReadyGet]
        private CheckBox saveCredentialsCheckBox;
        [OnReadyGet]
        private CheckBox createAccountSaveCredentialsCheckBox;
        [OnReadyGet]
        private TabContainer tabContainer;

        public static readonly string CredentialsFilePath = "user://credentials.json";

        // Saved credentials
        private string email;
        private string password;

        private bool reconnect;
        private string nextScreen;

        [OnReady]
        public void RealReady()
        {
            loginButton.Connect("pressed", this, nameof(OnLoginButtonPressed));
            createAccountButton.Connect("pressed", this, nameof(OnCreateAccountButtonPressed));
            TryLoadCredentials();
        }

        public override void ShowScreen(GDC.Dictionary args)
        {
            base.ShowScreen(args);
            reconnect = args.Get<bool>("reconnect", false);
            nextScreen = args.Get<string>("nextScreen", "MatchScreen");

            tabContainer.CurrentTab = 0;

            if (email != "" && password != "")
                Login();
        }

        private async void Login(bool saveCredentials = false)
        {
            Visible = false;

            if (reconnect)
            {
                uiLayer.ShowMessage("Session expired! Reconnecting...");
            }
            else
            {
                uiLayer.ShowMessage("Logging in...");
            }

            ISession nakamaSession;
            try { nakamaSession = await Online.Global.NakamaClient.AuthenticateEmailAsync(email, password, create: false); }
            catch (ApiResponseException ex)
            {
                Visible = true;
                uiLayer.ShowMessage($"Login failed {ex.StatusCode}:{ex.Message}");

                // Clear fields
                email = "";
                password = "";

                Online.Global.NakamaSession = null;
                return;
            }

            SetSessionAndChangeScreen(nakamaSession, saveCredentials);
        }

        private void OnLoginButtonPressed()
        {
            email = loginEmailField.Text.StripEdges();
            password = loginPasswordField.Text.StripEdges();
            Login(saveCredentialsCheckBox.Pressed);
        }

        private async void OnCreateAccountButtonPressed()
        {
            email = createAccountEmailField.Text.StripEdges();
            password = createAccountPasswordField.Text.StripEdges();
            string username = createAccountUsernameField.Text.StripEdges();
            bool saveCredentials = createAccountSaveCredentialsCheckBox.Pressed;

            if (email == "")
            {
                uiLayer.ShowMessage("Must provide email");
                return;
            }
            if (password == "")
            {
                uiLayer.ShowMessage("Must provide password");
                return;
            }
            if (username == "")
            {
                uiLayer.ShowMessage("Must provide username");
                return;
            }

            Visible = false;
            uiLayer.ShowMessage("Creating account...");

            ISession nakamaSession;
            try { nakamaSession = await Online.Global.NakamaClient.AuthenticateEmailAsync(email, password, username, true); }
            catch (ApiResponseException ex)
            {
                Visible = true;

                string message = ex.Message;
                if (ex.Message == "Invalid credentials.")
                {
                    message = "E-mail already in use.";
                }
                else if (message == "")
                {
                    message = "Unable to create account.";
                }
                uiLayer.ShowMessage(message);

                Online.Global.NakamaSession = null;
                return;
            }

            SetSessionAndChangeScreen(nakamaSession, saveCredentials);
        }

        private void SetSessionAndChangeScreen(ISession session, bool saveCredentials)
        {
            if (saveCredentials)
                SaveCredentials();
            Online.Global.NakamaSession = session;
            uiLayer.HideMessage();

            if (nextScreen != "")
                uiLayer.ShowScreen(nextScreen);
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
                    email = (string)dict["email"];
                    password = (string)dict["password"];

                    loginEmailField.Text = email;
                    loginPasswordField.Text = password;
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
                email = email,
                password = password,
            }.ToGDDict()));
            file.Close();
        }
    }
}
