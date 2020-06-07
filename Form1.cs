using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;


namespace AMFC2
{
    public partial class Form1 : Form
    {
        // Code voor ophalen en weergeven van openstreetmap (deels) door Kevin Wijnen, thanks buddy.
        // C# functies voor Tile en Latitude/Longitude berekeningen van de OpenStreetsWiki (https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#C.2FC.2B.2B), verder nog PERL code omgezet voor boundary box (https://wiki.openstreetmap.org/wiki/Slippy_map_tilenames#Lon..2Flat._to_bbox)
        // Degrees to Radians code door Stormconsultancy.co.uk (https://stormconsultancy.co.uk/blog/storm-news/convert-an-angle-in-degrees-to-radians-in-c/)
        //Het mapelement is ontwikkeld door Leon Rodriguez
        int xplaneReceiveport = 49000; //de poort waaruit de data van xplane het programma binnenkomt
        int xplaneSendport = 49001; //de poort waarnaar we nieuwe data voor xplane verzenden
        FlightDataAccessLayer FDAL = new FlightDataAccessLayer();
        int refreshrate = 1000;
        int mapzoom = 11;

        private System.Threading.Timer threadingTimer; //<<Deze ook renamen naar threadingTimer als line 'testtimer = ...' uncommented wordt.
        //private System.Threading.Timer systemTestThread; //speciale thread alleen bedoeld voor tijdelijke system test

        public Form1()
        {
            Activate();
            WindowState = FormWindowState.Maximized;
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            //Hiermee is het venster op volledig scherm gezet
            //refreshrate = 1000; //milliseconden tot volgende refresh van gegevens
            InitializeComponent();
            //label4.Text = trackBar1.Value.ToString(); //niet echt nodig
            //de 'read data' functie uitvoeren, 1 seconde wachten
            //de 'send data functie moet aangeroepen worden vanuit een button-press


            //timer instellen en read-sequence starten
            //System.Threading.Timer threadingTimer = new System.Threading.Timer(callFdalUpdate, 10, 1, refreshrate); //als volgende regel uncommented, deze regel wel commenten!
 //>>           //threadingTimer = new System.Threading.Timer(callFdalUpdate, 10, 1, refreshrate); //<<deze uncommenten en renamen naar threadingTimer om zo het 'garbage-collecten' van het systeem naar dit soort functies op te lossen
            //(bron: "https://stackoverflow.com/questions/2196825/why-does-system-threading-timer-stop-on-its-own#:~:text=The%20reason%20the%20timer%20%22stops,GC%20knows%20it's%20still%20needed"
 //>>           //Verplaatst naar de startProcesstimer functie(!)

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            
        }

        private void startProcesstimer(object sender, EventArgs e) //wanneer op de knop start + retrieve word geklikt
        {
            threadingTimer = new System.Threading.Timer(callFdalUpdate, 10, 1, refreshrate);
        }

        private void button1_Click(object sender, EventArgs e)
        {

        }

        public void updateGmap()
        {
            //gmap
            int forwardHeading = Int32.Parse(FDAL.headingMagnetic);
            //van het vliegtuig altijd naar boven op de kaart wordt weergegeven
            gmap.MapProvider = GMap.NET.MapProviders.OpenStreetMapProvider.Instance;
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerOnly;
            double latitude = Convert.ToDouble(FDAL.latitudeDegrees);
            double longitude = Convert.ToDouble(FDAL.longitudeDegrees);
            gmap.Position = new GMap.NET.PointLatLng(latitude, longitude);
            gmap.Zoom = mapzoom;
            gmap.Bearing = forwardHeading;
        }

        private void mapZoomIn_Click(object sender, EventArgs e)
        {
            gmap.Zoom += 1;
        }

        private void mapZoomOut_Click(object sender, EventArgs e)
        {
            if (gmap.Zoom > 1)
            {
                gmap.Zoom -= 1;
            }
        }

        public string returnOpenStreetMap(float _flat, float _flon, int _zoom)
        {
            float flat = _flat;
            float flon = _flon;
            int z = _zoom;
            //Kevin's methode
            double lat = Convert.ToDouble(flat);
            double lon = Convert.ToDouble(flon);
            string bbox = "";

            // Boundary Box berekenen en ook omzetten naar een bruikbare URL.
            bbox = LonLat_to_bbox(lat, lon, z);
            string[] bboxCoords = bbox.Split('/');
            string embed_url = "https://www.openstreetmap.org/export/embed.html?bbox=" + bboxCoords[0] + "%2C" + bboxCoords[1] + "%2C" + bboxCoords[2] + "%2C" + bboxCoords[3];
            return (embed_url);
        }

        public string returnGoogleMap(float poslon, float poslat, string zoom, string mode="web")
        {
            string stringposlat = poslat.ToString();
            string stringposlon = poslon.ToString();
            string newstringposlat = stringposlat.Replace(",", ".");
            string newstringposlon = stringposlon.Replace(",", ".");
            string url = "https://www.google.com/maps/@" + newstringposlat + "," + newstringposlon + "," + zoom + "z";
            string embedUrl = url + "http://maps.google.com/maps?output=embed&ll=" + newstringposlat + "," + newstringposlon + "&z=" + zoom;
            if (mode == "embed")
            {
                return (embedUrl);
            }
            else
            {
                return (url);
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            
        }

        //Openstreetmap stuff used from Kevin's code
        static string LonLat_to_bbox(double lat, double lon, int z)
        {
            int width = 1920; // STEL BREEDTE IN VAN HET KAARTGEBIED!
            int height = 979; // STEL HOOGTE IN VAN HET KAARTGEBIED!
            int tile_size = 256;

            //X en Y tile ophalen
            double x_tile = long2tilex(lon, z);
            double y_tile = lat2tiley(lat, z);

            // X/Y tiles ophalen voor Zuid en Oost door ((variabel * tile_size - width.2)/tilesize) en met height voor Y tile.

            double x_tile_south = (x_tile * tile_size - width / 2) / tile_size;
            double y_tile_south = (y_tile * tile_size - height / 2) / tile_size;
            double x_tile_east = (x_tile * tile_size + width / 2) / tile_size;
            double y_tile_east = (y_tile * tile_size + height / 2) / tile_size;

            double lon_south = tilex2long((int)x_tile_south, z);
            double lat_south = tiley2lat((int)y_tile_south, z);
            double lon_east = tilex2long((int)x_tile_east, z);
            double lat_east = tiley2lat((int)y_tile_east, z);
            // Coordinaten berekenen met X Tile, Y tile en Zoom, beide tiles zuid. Daarna zelfde doen voor oostelijke X/Y tiles.
            // Dus eerst ZUID lon/lan, dan OOST lon/lan

            // Daarna bbox outputten door coördinaten te combineren
            //lon_s, lat_s, lon_e, last_e

            string bbox = Convert.ToString(lon_south) + "/" + Convert.ToString(lat_south) + "/" + Convert.ToString(lon_east) + "/" + Convert.ToString(lat_east);
            bbox = bbox.Replace(',', '.');

            return bbox;
        }

        public static double ToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }

        static int long2tilex(double lon, int z)
        {
            return (int)(Math.Floor((lon + 180.0) / 360.0 * (1 << z)));
        }

        static int lat2tiley(double lat, int z)
        {
            return (int)Math.Floor((1 - Math.Log(Math.Tan(ToRadians(lat)) + 1 / Math.Cos(ToRadians(lat))) / Math.PI) / 2 * (1 << z));
        }

        static double tilex2long(int x, int z)
        {
            return x / (double)(1 << z) * 360.0 - 180;
        }

        static double tiley2lat(int y, int z)
        {
            double n = Math.PI - 2.0 * Math.PI * y / (double)(1 << z);
            return 180.0 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //FlightDataAccessLayer fdal = new FlightDataAccessLayer();
            //byte[] response = fdal.listenToUDP(49001);
            //label6.Text = response;
        }

        public async void callFdalUpdate(object state) //wordt aangeroepen via de threadingTimer om de fdal te laten updaten
        {
            //await Task.Run(new Action(() => FDAL.updateFDAL()));
            //await Task.Run(new Action(() => updateAllUI()));
            FDAL.updateFDAL(xplaneReceiveport); //nieuwe gegevens ophalen van de fdal
            updateAllUI(); //alle GUI-elementen updaten
            updateGmap(); //
        }

        public void callFdalTest() //public async void callFdalTest(object state)
        {
            FDAL.simulateSytemTestdata();
            updateAllUI();
            updateGmap();
        }

        //voor de grafische rendering van de gegevens zorgen
        public void updateAllUI() //om alle nieuwe gegevens weer uit de fdal te halen en in het window te plaatsen
        {
            labelKias.Invoke((MethodInvoker)(() => labelKias.Text = FDAL.kias));
            labelKtgs.Invoke((MethodInvoker)(() => labelKtgs.Text = FDAL.ktgs));
            labelMach.Invoke((MethodInvoker)(() => labelMach.Text = FDAL.mach));
            labelThrottle.Invoke((MethodInvoker)(() => labelThrottle.Text = FDAL.throttleCommanded));
            labelAltitude.Invoke((MethodInvoker)(() => labelAltitude.Text = FDAL.altitudeFtmsl));
            labelVerticalspeed.Invoke((MethodInvoker)(() => labelVerticalspeed.Text = FDAL.VVI));
            labelWindspeed.Invoke((MethodInvoker)(() => labelWindspeed.Text = FDAL.windspd));
            labelWinddirection.Invoke((MethodInvoker)(() => labelWinddirection.Text = FDAL.winddir));
            labelComfrequency.Invoke((MethodInvoker)(() => labelComfrequency.Text = FDAL.COMfrequency));
            labelComstandby.Invoke((MethodInvoker)(() => labelComstandby.Text = FDAL.COMStandby));
            labelNavfrequency.Invoke((MethodInvoker)(() => labelNavfrequency.Text = FDAL.NAVfrequency));
            labelNavstandby.Invoke((MethodInvoker)(() => labelNavstandby.Text = FDAL.NAVstandby));
            labelNAVOBS.Invoke((MethodInvoker)(() => labelNAVOBS.Text = FDAL.NAVobs));
            labelRudderpos.Invoke((MethodInvoker)(() => labelRudderpos.Text = FDAL.rudderPos));
            labelElevatorpos1.Invoke((MethodInvoker)(() => labelElevatorpos1.Text = FDAL.elevatorPos));
            //labelElevatorpos2.Invoke((MethodInvoker)(() => labelElevatorpos2.Text = FDAL.elevatorPos));
            labelAileronpos1.Invoke((MethodInvoker)(() => labelAileronpos1.Text = FDAL.aileronPos));
            labelAileronpos2.Invoke((MethodInvoker)(() => labelAileronpos2.Text = FDAL.aileronPos)); //moet wss inverted worden
            labelNwheelpos.Invoke((MethodInvoker)(() => labelNwheelpos.Text = FDAL.nosewheelPos));
            labelSpeedbrakeL.Invoke((MethodInvoker)(() => labelSpeedbrakeL.Text = FDAL.speedbrakePos));
            labelSpeedbrakeR.Invoke((MethodInvoker)(() => labelSpeedbrakeR.Text = FDAL.speedbrakePos));
            labelFlapsL.Invoke((MethodInvoker)(() => labelFlapsL.Text = FDAL.flapPos));
            labelFlapsR.Invoke((MethodInvoker)(() => labelFlapsR.Text = FDAL.flapPos)); //moet wss inverted worden
            trackBar4.Invoke((MethodInvoker)(() => trackBar4.Value = Int32.Parse(FDAL.elevatorPos)));
            trackBar2.Invoke((MethodInvoker)(() => trackBar2.Value = Int32.Parse(FDAL.nosewheelPos)));
            trackBar3.Invoke((MethodInvoker)(() => trackBar3.Value = Int32.Parse(FDAL.rudderPos)));
            trackBar5.Invoke((MethodInvoker)(() => trackBar5.Value = Int32.Parse(FDAL.aileronPos)));
            trackBar6.Invoke((MethodInvoker)(() => trackBar6.Value = Int32.Parse(FDAL.aileronPos))); //moet wss inverted worden
            trackBar7.Invoke((MethodInvoker)(() => trackBar7.Value = Int32.Parse(FDAL.speedbrakePos)));
            trackBar8.Invoke((MethodInvoker)(() => trackBar8.Value = Int32.Parse(FDAL.speedbrakePos)));
            trackBar9.Invoke((MethodInvoker)(() => trackBar9.Value = Int32.Parse(FDAL.flapPos)));
            trackBar10.Invoke((MethodInvoker)(() => trackBar10.Value = Int32.Parse(FDAL.flapPos)));
        }

        private void exitApplication_Click(object sender, EventArgs e)
        {
            //afsluiten applicatie
            Application.Exit();
        }

        private void button8_Click(object sender, EventArgs e) //system test button
        {
            //systemTestThread = new System.Threading.Timer(callFdalTest, 10, 1, refreshrate);
            callFdalTest();
        }
    }
}
