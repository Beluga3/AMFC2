using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
//het kan zijn dat IPAddress.Any niet werkt en ik daadwerkelijk het netwerk ip-adres van de x-plane computer moet registreren.
namespace AMFC2
{
    class FlightDataAccessLayer
    {
        //hier komen alle gegevens via UDP van xplane11 binnen en worden uit het UDP-pakket in deze class gezet
        public string kias;                //onder 3
        public string ktgs;                //onder 3
        public string mach;                //onder 4
        public string VVI;                 //onder 4
        public string windspd;             //onder 5
        public string winddir;             //onder 5
        public string elevatorPos;         //onder 11
        public string aileronPos;          //onder 11
        public string rudderPos;           //onder 11
        public string nosewheelPos;        //onder 11
        public string flapPos;             //onder 13
        public string speedbrakePos;       //onder 13
        public string headingMagnetic;     //onder 17
        public string latitudeDegrees;     //onder 20
        public string longitudeDegrees;    //onder 20
        public string altitudeFtmsl;       //onder 20
        public string throttleCommanded;   //onder 25
        public string COMfrequency;        //onder 96
        public string COMStandby;          //onder 96
        public string NAVfrequency;        //onder 97
        public string NAVstandby;          //onder 97
        public string NAVobs;              //onder 98
        public string transponderSetting;  //onder 104
        public string FPLNlegnr;           //onder 128
        public string FPLNlegtype;         //onder 128
        public string FPLNleglatitude;     //onder 128
        public string FPLNleglongitude;    //onder 128

        public byte[] listenToUDP(int xplanePort)
        {
            //luistert naar UDP-packages die binnenkomen van enig ip-adres in het netwerk
            //string UdpString = "testing";
            //udp-package ophalen van een ip-adres dat broadcast op poort [port]
            byte[] data = new byte[1024];
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, xplanePort);
            UdpClient newsock = new UdpClient(ipep);
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            data = newsock.Receive(ref sender);
            /*for (int index = 0; index < data.Length; index++)
            {
                Console.Write("{0},", data[index]); //dit schrijft gewoon alle waardes achter elkaar in de console
            } */
            //UdpString = Encoding.UTF8.GetString(data, 0, data.Length);
            //of UdpString = Encoding.ASCII.GetStrings(data);
            newsock.Close();
            return (data);
        }

        public List<byte[]> listenToUDPUntilStop(int xplanePort)
        {
            ASCIIEncoding ASCII = new ASCIIEncoding();
            List<byte[]> fullResponse = new List<byte[]>();
            //
            //string response = ""; //not used (?)
            string strData = "";
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, xplanePort);
            UdpClient udpClient = new UdpClient(ipep);
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            while (!strData.Equals("Over")) //zolang het udp-pakket nog niet ten einde is
            {
                Byte[] udpSentence = udpClient.Receive(ref ipep);
                fullResponse.Add(udpSentence);  //de inkomende udp-sentence toevoegen aan een lijst van sentences
                strData = ASCII.GetString(udpSentence); //strData updaten naar de laatste line
            }
            udpClient.Close();
            return (fullResponse); //om zo het volledige udp-pakket binnen te halen
        }

        public List<float> UDPunpack(byte[] udpsentence)
        {
            //de UDP-message decoden naar waarden en deze naar de desbetreffende variabele schrijven
            //(this.coordinates = ...) bijvoorbeeld
            //deze functie moet [refreshrate] keer per seconden worden aangeroepen. de refreshrate staat
            //gedefinieerd in Form1.cs bovenaan de code.
            float xplaneIndex = BitConverter.ToSingle(udpsentence, 5); //start position is 5 want de xplane-index zijn bytes 5,6,7 en 8
            float value1 = BitConverter.ToSingle(udpsentence, 9);
            float value2 = BitConverter.ToSingle(udpsentence, 13);
            float value3 = BitConverter.ToSingle(udpsentence, 17);
            float value4 = BitConverter.ToSingle(udpsentence, 21);
            float value5 = BitConverter.ToSingle(udpsentence, 25);
            float value6 = BitConverter.ToSingle(udpsentence, 29);
            float value7 = BitConverter.ToSingle(udpsentence, 33);
            float value8 = BitConverter.ToSingle(udpsentence, 37);
            List<float> datavalues = new List<float>();
            float[] floatarray = { xplaneIndex, value1, value2, value3, value4, value5, value6, value7, value8 };
            datavalues.AddRange(floatarray);
            /*
             * dit zijn alleen de datavalues van één enkele xplane-index / udp-sentence
            */
            return datavalues;
        }

        //functie maken om alle udpSentences in het resultaat van listenToUDPUntilStop door UDPUnpack te laten gaan
        public List<float[]> UDPunpackAll(List<byte[]> fullUDPresponse)
        {
            List<float[]> UDPFloatValues = new List<float[]>();
            float[] emptyFloatArray = new float[1024];
            //ieder item in de lijst 'fullUDPresponse' is een byte-array van één sentence van het udp-datagram.
            //hiervoor gebruik ik gewoon bovenstaande functie om een sentence om te zetten naar class-properties
            for (int i = 0; i < fullUDPresponse.Count; i++)
            {
                emptyFloatArray = UDPunpack(fullUDPresponse[i]).ToArray();
                if (emptyFloatArray.Length != 0)
                {
                    UDPFloatValues.Add(emptyFloatArray);
                }
            }
            return (UDPFloatValues);
        }


        public void enterDatavalues(List<float> datavalues) //neemt een float-list van één udp-sentence/line en voert de gegevens in
        {
            if ((int)datavalues[0] == 3) // als de message over speed gaat
            {
                this.kias = datavalues[1].ToString();
                this.ktgs = datavalues[4].ToString();
            }
            else if ((int)datavalues[0] == 4) // als de message over mach/vertical speed gaat
            {
                this.mach = datavalues[1].ToString();
                this.VVI = datavalues[2].ToString();
            }
            else if ((int)datavalues[0] == 5) // als de message over weather gaat
            {
                this.windspd = datavalues[3].ToString();
                this.winddir = datavalues[4].ToString();
            }
            else if ((int)datavalues[0] == 6) // aircraft
            {
                //niets van deze categorie geïmplementeerd
            }
            else if ((int)datavalues[0] == 11) // flightcontrols
            {
                this.elevatorPos = datavalues[1].ToString();
                this.aileronPos = datavalues[2].ToString();
                this.rudderPos = datavalues[3].ToString();
                this.nosewheelPos = datavalues[4].ToString();
            }
            else if ((int)datavalues[0] == 13) // trim-flaps-brakes
            {
                this.flapPos = datavalues[5].ToString();
                this.speedbrakePos = datavalues[8].ToString();
            }
            else if ((int)datavalues[0] == 17) // pitch-roll-heading
            {
                this.headingMagnetic = datavalues[4].ToString();
            }
            else if ((int)datavalues[0] == 20) // latitude-longitude-altitude
            {
                this.latitudeDegrees = datavalues[1].ToString();
                this.longitudeDegrees = datavalues[2].ToString();
                this.altitudeFtmsl = datavalues[3].ToString();
            }
            else if ((int)datavalues[0] == 25) // throttle-command
            {
                this.throttleCommanded = datavalues[1].ToString();
            }
            else if ((int)datavalues[0] == 96) // com1-com2-freq
            {
                this.COMfrequency = datavalues[1].ToString();
                this.COMStandby = datavalues[2].ToString();
            }
            else if ((int)datavalues[0] == 97) // nav1-nav2-freq
            {
                this.NAVfrequency = datavalues[1].ToString();
                this.NAVstandby = datavalues[2].ToString();
            }
            else if ((int)datavalues[0] == 98) // navobs
            {
                this.NAVobs = datavalues[1].ToString();
            }
            else if ((int)datavalues[0] == 104) //transponder
            {
                this.transponderSetting = datavalues[2].ToString();
            }
            else if ((int)datavalues[0] == 128) //Flightplan
            {
                this.FPLNlegnr = datavalues[1].ToString();
                this.FPLNlegtype = datavalues[2].ToString();
                this.FPLNleglatitude = datavalues[3].ToString();
                this.FPLNleglongitude = datavalues[4].ToString();
            }
        }

        public void enterAllDataValues(List<float[]> floatvalueArray)
        {
            foreach (float[] item in floatvalueArray)
            {
                List<float> floatvalueList = item.ToList();
                enterDatavalues(floatvalueList);
            }
        }

        public string testUDPConnection(int xplanePort) //beta? :P
        {
            string status = "No connection";
            byte[] data = new byte[1024];
            IPEndPoint ipep = new IPEndPoint(IPAddress.Any, xplanePort);
            UdpClient newsock = new UdpClient(ipep);
            IPEndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            data = newsock.Receive(ref sender);
            if (data.Length != 0)
            {
                status = "connection established";
            }
            return (status);
        }

        public void updateFDAL(int xplaneReceiveport)
        {
            //de juiste sequence van functies uitvoeren
            List<Byte[]> fullUdp = listenToUDPUntilStop(xplaneReceiveport);
            List<float[]> unpacked = UDPunpackAll(fullUdp);
            enterAllDataValues(unpacked);
        }

        public void simulateSytemTestdata() //alleen voor testen van software, dus zonder verbinding met xplane
        {
            Random random = new Random();
            float[] valueSpeed = { float.Parse("3"), float.Parse(random.Next(1, 250).ToString()), 0, 0, float.Parse(random.Next(1, 250).ToString())}; //speed gebruikt 0, 1 en 4
            float[] machVertical = { float.Parse("4"), float.Parse(random.NextDouble().ToString()), float.Parse(random.Next(-2000, 2000).ToString())}; //mach-vvi gebruikt 0, 1 en 2
            float[] weather = { float.Parse("5"), 0, 0, float.Parse(random.Next(0, 20).ToString()), float.Parse(random.Next(0, 359).ToString())};//weather gebruikt 0, 3 en 4
            float[] controls = { float.Parse("11"), float.Parse(random.Next(-50, 50).ToString()), float.Parse(random.Next(-50, 50).ToString()), float.Parse(random.Next(-50, 50).ToString()), float.Parse(random.Next(-50, 50).ToString())};//flightcontrols gebruikt 1, 2, 3 en 4
            float[] flapsBrakes = { float.Parse("13"), 0, 0, 0, 0, float.Parse(random.Next(0, 100).ToString()), 0, 0, float.Parse(random.Next(0, 100).ToString()) }; //0, 5 en 8
            float[] heading = { float.Parse("17"), 0, 0, 0, float.Parse(random.Next(0, 395).ToString()) }; // 4
            float[] latlonalt = { float.Parse("20"), float.Parse(random.Next(-90, 90).ToString()), float.Parse(random.Next(-90, 90).ToString()), float.Parse(random.Next(500, 35000).ToString()) }; //0, 1, 2 en 3
            float[] throttle = { float.Parse("25"), float.Parse(random.Next(0, 100).ToString()) }; //0 en 1
            float[] coms = { float.Parse("96"), float.Parse(random.Next(101, 129).ToString()), float.Parse(random.Next(101, 129).ToString()) }; //0, 1 en 2
            float[] navs = { float.Parse("97"), float.Parse(random.Next(101, 129).ToString()), float.Parse(random.Next(101, 129).ToString()) }; //0, 1 en 2
            float[] navobs = { float.Parse("98"), float.Parse(random.Next(0, 10).ToString()) }; //0 en 1
            float[] transponder = { float.Parse("104"), 0, float.Parse(random.Next(1000, 9999).ToString()) }; //0 en 2
            float[] fltpln = { float.Parse("128"), float.Parse(random.Next(0, 10).ToString()), float.Parse("1"), float.Parse(random.Next(-90, 90).ToString()), float.Parse(random.Next(-90, 90).ToString()) };
            List<float[]> testdatalist = new List<float[]>() { valueSpeed, machVertical, weather, controls, flapsBrakes, heading, latlonalt, throttle, coms, navs, navobs, transponder, fltpln };
            enterAllDataValues(testdatalist);
        }
    }
}
