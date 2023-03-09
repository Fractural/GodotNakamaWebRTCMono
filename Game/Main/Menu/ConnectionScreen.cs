using Fractural.GodotCodeGenerator.Attributes;
using Godot;
using GDC = Godot.Collections;
using System;
using NakamaWebRTC;
using Nakama;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using Fractural.Utils;

namespace NakamaWebRTCDemo
{
    public partial class ConnectionScreen : Screen
    {
        public class Args
        {
            public bool? Reconnect { get; set; }
            public string NextScreen { get; set; }
        }

        [OnReadyGet]
        private LineEdit loginEmailField;
        [OnReadyGet]
        private LineEdit loginPasswordField;
        [OnReadyGet]
        private LineEdit createAccountUsernameField;
        [OnReadyGet]
        private LineEdit createAccountEmailField;
        [OnReadyGet]
        private LineEdit createAccountPasswordField;
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
        private string email = "";
        private string password = "";

        private bool reconnect;
        private string nextScreen;

        [OnReady]
        public void RealReady()
        {
            loginButton.Connect("pressed", this, nameof(OnLoginButtonPressed));
            createAccountButton.Connect("pressed", this, nameof(OnCreateAccountButtonPressed));
            TryLoadCredentials();
        }

        public override void ShowScreen(object args = null)
        {
            base.ShowScreen(args);

            reconnect = false;
            nextScreen = nameof(MatchScreen);
            if (args is Args castedArgs)
            {
                if (castedArgs.Reconnect.HasValue)
                    reconnect = castedArgs.Reconnect.Value;
                if (castedArgs.NextScreen != null)
                    nextScreen = castedArgs.NextScreen;
            }

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


            await Online.Global.CallNakama(async (c) =>
            {
                ISession nakamaSession = null;
                try
                {
                    nakamaSession = await c.AuthenticateEmailAsync(email, password, create: false);
                }
                catch (ApiResponseException ex)
                {
                    Visible = true;
                    string statusCode = "";

                    uiLayer.ShowMessage($"Login failed {statusCode}:{ex.Message}", 2f);

                    // Clear fields
                    email = "";
                    password = "";

                    Online.Global.NakamaSession = null;
                    return;
                }

                SetSessionAndChangeScreen(c, nakamaSession, saveCredentials);
            });
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
                uiLayer.ShowMessage("Must provide email", 2f);
                return;
            }
            if (password == "")
            {
                uiLayer.ShowMessage("Must provide password", 2f);
                return;
            }
            if (username == "")
            {
                uiLayer.ShowMessage("Must provide username", 2f);
                return;
            }

            Visible = false;
            uiLayer.ShowMessage("Creating account...");

            await Online.Global.CallNakama(async (c) =>
            {
                ISession nakamaSession = null;
                try
                {
                    nakamaSession = await c.AuthenticateEmailAsync(email, password, username, true);
                }
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
                    uiLayer.ShowMessage(message, 2);

                    Online.Global.NakamaSession = null;
                    return;
                }

                SetSessionAndChangeScreen(c, nakamaSession, saveCredentials);
            });
        }

        private async void SetSessionAndChangeScreen(Client client, ISession session, bool saveCredentials)
        {
            if (saveCredentials)
                SaveCredentials();
            Online.Global.NakamaSession = session;
            uiLayer.HideMessage();

            try
            {
                var rpcResult = await client.RpcAsync(session, "get_ice_servers");
                var jsonParseResult = JSON.Parse(rpcResult.Payload);
                if (jsonParseResult.Error != Error.Ok)
                    throw new Exception("Could not parse get_ice_servers payload as JSON.");
                var result = (GDC.Dictionary)jsonParseResult.Result;

                OnlineMatch.Global.IceServers = result.Get<GDC.Array>("response.ice_servers");

                Console.Print("Succesfully set up ice servers: " + OnlineMatch.Global.IceServers.ToString());

                if (nextScreen != "")
                    uiLayer.ShowScreen(nextScreen);
            }
            catch (Exception e)
            {
                Console.PrintErr(e.ToString());
                uiLayer.ShowMessage("Fetching ICE servers failed.", 2);
            }
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
            file.StoreLine(JSON.Print(new GDC.Dictionary()
            {
                ["email"] = email,
                ["password"] = password
            }));
            file.Close();
        }
    }
}
