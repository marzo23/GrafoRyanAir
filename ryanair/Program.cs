using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ryanair
{
    class Program
    {
        public static string airportJsonFile = Path.Combine(Directory.GetCurrentDirectory(), "airports.json");
        public static string apiUrl = "https://api.ryanair.com/aggregate/4/common?embedded=airports";
        public static string priceApiUrl = //"https://ota.ryanair.com/flagg/api/v4/es-es/availability?DateOut=2018-06-14&FlexDaysBeforeOut=1&RoundTrip=false&Destination={1}&Origin={0}";
                                            "https://desktopapps.ryanair.com/v4/es-es/availability?DateOut=2018-06-20&Destination={1}&Origin={0}&RoundTrip=false&ToUs=AGREED&currency=USD";

        static void Main(string[] args)
        {

            Console.WriteLine(Directory.GetCurrentDirectory());
            List<Airport> airports = new List<Airport>();
            if (File.ReadAllText(airportJsonFile).Length<=0)
            {
                airports = GetAirportsList(apiUrl);
                saveData(airports);
            }
            else
                foreach (string json in File.ReadAllLines(airportJsonFile))
                    airports.Add(JsonConvert.DeserializeObject<Airport>(json));
            bool[,] relations = GetRelations(airports);
            double[,] distances = GetDistances(airports, relations);
            double[,] prices = GetPrices(airports, relations);
            Grafo<Airport> grafo = new Grafo<Airport>();//GenerateGrafoByListAndRel(airports, relations);
            grafo.GenerateGrafoByListAndRel(airports, relations);
            grafo.Dijkstra(grafo.NodeList[0], distances, null);
            List<Node<Airport>> shortestRout = grafo.GetShortestRoute(grafo.NodeList[49]);

            Console.ReadLine();
        }


        public static Grafo<Airport> GenerateGrafoByListAndRel(List<Airport> airports, bool[,] relations)
        {
            Grafo<Airport> grafo = new Grafo<Airport>();

            for (int i = 0; i < airports.Count; i++)
            {
                Node<Airport> pivotNode = grafo.NodeList.Find(n => n.Element.id == i);
                if (pivotNode == null)
                {
                    pivotNode = new Node<Airport> { Element = airports[i], ElementList = new List<Node<Airport>>() };
                    grafo.NodeList.Add(pivotNode);
                }
                for (int j = 0; j < airports.Count; j++)
                {
                    if (relations[i, j])
                    {
                        Node<Airport> aux = grafo.NodeList.Find(n => n.Element.id == j);
                        if (aux == null)
                        {
                            grafo.NodeList.Add(new Node<Airport> { Element = airports[j], ElementList = new List<Node<Airport>>() });
                            pivotNode.ElementList.Add(grafo.NodeList.Last());
                        }
                        else
                            pivotNode.ElementList.Add(aux);
                    }

                }
            }
            return grafo;
        }

        public static double[,] GetPrices(List<Airport> airports, bool[,] relations)
        {
            List<string> curr = new List<string>();
            double[,] prices = new double[airports.Count, airports.Count];
            for (int i = 0; i < airports.Count; i++)
            {
                for (int j = 0; j < airports.Count; j++)
                {
                    if (relations[i, j])
                    {
                        string url = string.Format(priceApiUrl, airports[i].iataCode, airports[j].iataCode);
                        string responseStr = GetJson(url);
                        if (responseStr == null)
                            continue;
                        JObject jobj = JObject.Parse(responseStr);
                        string currency = (string)jobj["currency"];
                        if (currency == null)
                            continue;
                        if (currency.Equals("USD"))
                        {
                            string aux = (string)jobj["trips"]["dates"]["flights"]["fares"]["amount"];
                            if (aux != null)
                                prices[i, j] = double.Parse(aux);
                        }
                        else
                            if (!curr.Contains(currency))
                                curr.Add(currency);
                    }
                    else
                        prices[i, j] = 0;
                }
            }
            foreach (string item in curr)
            {
                Console.WriteLine(item);
            }
            return prices;
        }

        public static double[,] GetDistances(List<Airport> airports, bool[,]relations)
        {
            double[,] distances = new double[airports.Count, airports.Count];
            for (int i = 0; i < airports.Count; i++)
            {
                for (int j = 0; j < airports.Count; j++)
                {
                    if (relations[i, j])
                    {

                        distances[i, j] = DistanceByHarvesine(airports[i].coordinates, airports[j].coordinates);
                    }
                    else
                        distances[i, j] = 0;
                }
            }
            return distances;
        }

        public static double DistanceByHarvesine(Coordinates p1, Coordinates p2)
        {
            int r = 6378;
            double x = Convert.ToSingle(Math.PI / 180) * float.Parse(p1.latitude) - float.Parse(p2.latitude);
            double y = Convert.ToSingle(Math.PI / 180) * float.Parse(p1.longitude) - float.Parse(p2.longitude);
            double a = Math.Pow(Math.Sin(x / 2), 2) + Math.Cos(float.Parse(p1.latitude)) * Math.Cos(float.Parse(p2.latitude)) * Math.Pow(Math.Sin(y / 2), 2);
            return Math.Abs(2 * r * Math.Asin(Math.Sqrt(a)));
        }

        public static void saveData(List<Airport> airports)
        {
            Thread.Sleep(2000);
            foreach (Airport airport in airports)
            {
                File.AppendAllText(airportJsonFile, JsonConvert.SerializeObject(airport) + "\n");
            }
           
        }

        public static bool[,] GetRelations(List<Airport> airports)
        {
            bool[,] relations = new bool[airports.Count, airports.Count];

            for (int i = 0; i < airports.Count; i++)
                for (int j = 0; j < airports.Count; j++)
                    relations[i, j] = false;

            foreach (Airport airport in airports)
            {
                foreach (string route in airport.routes)
                {
                    Airport airportAux = airports.Find(a => a.iataCode.Equals(route));
                    if (airportAux != null)
                        relations[airport.id, airportAux.id] = true;
                }
            }
            return relations;
        }

        public static List<Airport> GetAirportsList(string url)
        {
            string responseStr = GetJson(url);

            JObject jobj = JObject.Parse(responseStr);

            IList<JToken> results = jobj["airports"].Children().ToList();

            List<Airport> airportList = new List<Airport>();
            foreach (JToken result in results)
            {
                Airport airport = result.ToObject<Airport>();
                List<string> auxRoutes = new List<string>();
                foreach (string route in airport.routes)
                {
                    if (route.Contains("airport"))
                        auxRoutes.Add(route.Replace("airport:", ""));
                }
                airport.routes = auxRoutes;
                airportList.Add(airport);
            }
            for (int i = 0; i < airportList.Count; i++)
                airportList[i].id = i;
            return airportList;
        }

        public static string GetJson(string URL)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(URL) as HttpWebRequest;
                using (HttpWebResponse response = request.GetResponse() as HttpWebResponse)
                {
                    StreamReader reader = new StreamReader(response.GetResponseStream());
                    return reader.ReadToEnd();
                }
            }
            catch (Exception) { return null; }
        }
    }





    public class Airport : IElement
    {
        public int id { get; set; }
        public string iataCode { get; set; }
        public string name { get; set; }
        public Coordinates coordinates { get; set; }
        public List<string> routes { get; set; }
        public override string ToString()
        {
            return this.name + "- Latitude: " + this.coordinates.latitude + "; Longitude: " + this.coordinates.longitude + ";";
        }
    }

    public class Coordinates
    {
        public string latitude { get; set; }
        public string longitude { get; set; }
    }

}
