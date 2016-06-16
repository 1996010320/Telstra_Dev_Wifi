using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Telstra_Dev_Wifi
{
    public partial class Form1 : Form
    {
        TelstraToken _token;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtLatitude.Text = "-37.81819";
            txtLongitude.Text = "145.00176";
            txtRadius.Text = "1000";
        }

        private async void btnCalculate_Click(object sender, EventArgs e)
        {
            List<TelstraWifiHotSpot> lst = new List<TelstraWifiHotSpot>();

            if (_token == null || _token.ExpiredDt < DateTime.Now)
            {
                _token = await GetAccessToken("your consumer key", "your consumer secret");
            }
            

            string latitude = txtLatitude.Text;
            string longitude = txtLongitude.Text;
            string radius = txtRadius.Text;

            string url = string.Format("https://api.telstra.com/v1/wifi/hotspots?lat={0}&long={1}&radius={2}", latitude, longitude, radius);

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token.AccessToken);

            HttpRequestMessage request = new HttpRequestMessage()
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri(url)
            };

            HttpResponseMessage responseMessage = await client.SendAsync(request);

            var message = await responseMessage.Content.ReadAsStringAsync();

            var jMsg = JsonConvert.DeserializeObject<object>(message);

            

            foreach (var j in (JArray)jMsg)
            {
                string wifiLatitude = ((JObject)j)["lat"].ToString();
                string wifiLongitude = ((JObject)j)["long"].ToString();
                string wifiAddress = ((JObject)j)["address"].ToString();
                string wifiCity = ((JObject)j)["city"].ToString();
                string wifiState = ((JObject)j)["state"].ToString();

                TelstraWifiHotSpot hotspot = new TelstraWifiHotSpot(wifiLatitude, wifiLongitude, wifiAddress, wifiCity, wifiState);
                lst.Add(hotspot);
            }


            if (lst.Count > 0)
            {
                lstHotspots.DataSource = lst;
                lstHotspots.DisplayMember = lst.ToString();
            }

        }





        internal class TelstraToken
        {
            internal string AccessToken;
            internal DateTime ExpiredDt;

            internal TelstraToken() { }

            internal TelstraToken(string _accessToken, DateTime _expiredDt)
            {
                this.AccessToken = _accessToken;
                this.ExpiredDt = _expiredDt;
            }            
        }

        internal class TelstraWifiHotSpot
        {
            internal string latitude;
            internal string longitude;
            internal string address;
            internal string city;
            internal string state;

            internal TelstraWifiHotSpot(string _latitude, string _longitude, string _address, string _city, string _state)
            {
                this.latitude = _latitude;
                this.longitude = _longitude;
                this.address = _address;
                this.city = _city;
                this.state = _state;
            }


            public override string ToString()
            {
                string fullAddress = string.IsNullOrWhiteSpace(address) ? string.Empty : address;
                fullAddress += ", ";
                fullAddress += string.IsNullOrWhiteSpace(city) ? string.Empty : city;
                fullAddress += " ";
                fullAddress += string.IsNullOrWhiteSpace(state) ? string.Empty : state;

                return fullAddress;
            }
        }

        private async Task<TelstraToken> GetAccessToken(string consumerkey, string consumersecret)
        {
            TelstraToken Token = new TelstraToken();

            string AccessUrl = @"https://api.telstra.com/v1/oauth/token";

            HttpClient authClient = new HttpClient();
            HttpContent httpContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"client_id", consumerkey},
                {"client_secret", consumersecret},
                {"grant_type", "client_credentials"},
                {"scope", "WIFI"}
            });


            HttpRequestMessage Request = new HttpRequestMessage()
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(AccessUrl),
                Content = httpContent
            };

            try
            {
                var ResponseMessage = await authClient.SendAsync(Request);
                var Response = await ResponseMessage.Content.ReadAsStringAsync();

                if (ResponseMessage.IsSuccessStatusCode)
                {
                    var AuthToken = JsonConvert.DeserializeObject<object>(Response);

                    JObject jObj = JObject.Parse(AuthToken.ToString());

                    Token.AccessToken = jObj["access_token"].ToString();
                    Token.ExpiredDt = DateTime.Now.AddSeconds(double.Parse(jObj["expires_in"].ToString()));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return Token;
        }
        
        
    }
}
