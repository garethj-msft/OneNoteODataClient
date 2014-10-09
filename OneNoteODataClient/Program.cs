using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.OneNote.Api;

namespace OneNoteODataClient
{
    internal class Program
    {
        private static string accessToken = string.Empty;

        private static string refreshToken = string.Empty;
      
        private const string clientId = "000000004C12423B";

        private const string scopes = "wl.signin wl.offline_access office.onenote";

        [STAThread]
        private static void Main(string[] args)
        {
            var container = new OneNoteApi(new Uri("https://www.onenote.com/api/beta/"));
            container.BuildingRequest += container_BuildingRequest;
            
            var authWindow = new AuthWindow();
            authWindow.ClientId = clientId;
            authWindow.Scopes = scopes;
            
            DialogResult result = authWindow.ShowDialog();
            if (result == DialogResult.Cancel)
            {
                return;
            }
            accessToken = authWindow.AccessToken;
            refreshToken = authWindow.RefreshToken;

            var notebooks = from n in container.Notebooks.Expand("sections")
                            where n.Id == "0-205B05507D5809C1!372"
                            select n;

            string id = "";
            Notebook notebook = null;

            foreach (Notebook item in notebooks)
            {
                // Can't use .First(), because library transmutes this to $top, which is unsupported :-(
                id = item.Id;
                notebook = item;
                break;
            }

            Section section = null;
            foreach (Section item in notebook.Sections)
            {
                section = item;
                break;
            }

            Console.WriteLine("id: " + notebook.Id);
            Console.WriteLine("name: " + notebook.Name);
            Console.WriteLine("section name: " + section.Name);
            Console.WriteLine("section count: " + notebook.Sections.Count());
            Console.ReadLine();
        }

        private static void container_BuildingRequest(object sender,
            global::Microsoft.OData.Client.BuildingRequestEventArgs e)
        {
            var headerValue = new AuthenticationHeaderValue("Bearer", accessToken);
            e.Headers.Add("Authorization", headerValue.ToString());

        }
    }
}
