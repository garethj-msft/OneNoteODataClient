using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace OneNoteODataClient
{
    public partial class AuthWindow : Form
    {
        private const string CallbackUrl = @"https://oauth.live.com/desktop";
        private const string LoginBase = @"https://login.live.com";
        private const string TokenPath = @"oauth20_token.srf";
        private const string AuthUrl = @"https://login.live.com/oauth20_authorize.srf";

        public string Scopes { get; set; }
        public string AccessToken { get; private set; }
        public string RefreshToken { get; private set; }

        public string ClientId { get; set; }

        public AuthWindow()
        {
            InitializeComponent();
        }

        private async void browser_Navigated(object sender, WebBrowserNavigatedEventArgs e)
        {
            this.AccessToken = string.Empty;
            string uri = e.Url.ToString();

            if (uri.Contains("error="))
            {
                this.DialogResult = DialogResult.Abort;
                this.Close();
            }
            if (uri.StartsWith(CallbackUrl))
            {
                Match accessMatch = Regex.Match(uri, "access_token=(.+?)&");
                if (accessMatch.Success)
                {
                    this.AccessToken = WebUtility.UrlDecode(accessMatch.Groups[1].Value);
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    NameValueCollection query = HttpUtility.ParseQueryString(e.Url.Query);
                    if (query.AllKeys.Contains("code"))
                    {
                        string code = query["code"];
                        bool success = await this.FetchTokens(code);
                        this.DialogResult = success ? DialogResult.OK : DialogResult.Abort;    
                        this.Close();
                    }   
                }
            }
        }

        private async Task<bool> FetchTokens(string code)
        {
            var client = new HttpClient();
            client.BaseAddress = new Uri(LoginBase);

            var tokenContent =
                new StringContent(string.Format("client_id={0}&redirect_uri={1}&code={2}&grant_type=authorization_code",
                    this.ClientId, CallbackUrl, code), Encoding.UTF8, "application/x-www-form-urlencoded");

            HttpResponseMessage response = await client.PostAsync(TokenPath, tokenContent);
            if (response.IsSuccessStatusCode)
            {
                Dictionary<string, string> oauthResponse = await response.Content.ReadAsAsync<Dictionary<string, string>>();
                this.AccessToken = oauthResponse["access_token"];
                this.RefreshToken = oauthResponse["refresh_token"];
                return true;
            }
            return false;
        }

        private void AuthWindow_Load(object sender, EventArgs e)
        {
            // Use the simpler token flow unless we need a refresh token.
            string responseType = "token";
            if (this.Scopes.Contains("wl.offline_access"))
            {
                responseType = "code";
            }
            this.browser.Url =
                new Uri(string.Format(CultureInfo.InvariantCulture,
                    "{4}?client_id={0}&scope={1}&response_type={3}&redirect_uri={2}", this.ClientId, WebUtility.HtmlEncode(this.Scopes), CallbackUrl, responseType, AuthUrl));
        }
    }
}
