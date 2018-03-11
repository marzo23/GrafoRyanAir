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
            //MatrixToHTML(relations, "relations.html", airports);
            double[,] distances = GetDistances(airports, relations);
            //MatrixToHTML(distances, "distances.html", airports);
            //double[,] prices = GetPrices(airports, relations);
            Grafo<Airport> grafo = new Grafo<Airport>();
            grafo.GenerateGrafoByListAndRel(airports, relations);
            grafo.Dijkstra(grafo.NodeList[0], distances, null);
            List<Node<Airport>> shortestRout = grafo.GetShortestRoute(grafo.NodeList[49]);
            Node<Airport> treeRootPrim = new Node<Airport> { Element = grafo.NodeList[0].Element };
            grafo.Prim(treeRootPrim, distances, null);
            Node<Airport> treeRootKruscal = grafo.Kruscal(distances);
            Console.ReadLine();
        }

        public static void MatrixToHTML(double[,] matrix, string htmlName, List<Airport> airport)
        {
            string html = "<style> td{width:20px;}table, th, td {border: 1px solid black;} </style><table>";
            for (int i = -1; i < matrix.GetLength(0); i++)
            {
                html += "<tr>";
                for (int j = -1; j < matrix.GetLength(1); j++)
                {
                    if (i < 0)
                    {
                        if (j >= 0)
                            html += "<td title='"+ airport[j].ToString()+ "'>" + airport[j].iataCode + "</td>";
                        else
                            html += "<td></td>";
                    }
                    else
                    {
                        if(j<0)
                            html += "<td title='" + airport[i].ToString() + "'>" + airport[i].iataCode + "</td>";
                        else
                            html += "<td>" + matrix[i, j].ToString() + "</td>";
                    }
                }
                html += "</tr>";
            }
            html += "</table>";
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), htmlName), html);
        }

        public static void MatrixToHTML(bool[,] matrix, string htmlName, List<Airport> airport)
        {
            string html = "<style> td{width:20px;}table, th, td {border: 1px solid black;} </style><table>";
            for (int i = -1; i < matrix.GetLength(0); i++)
            {
                html += "<tr>";
                for (int j = -1; j < matrix.GetLength(1); j++)
                {
                    if (i < 0)
                    {
                        if (j >= 0)
                            html += "<td title='" + airport[j].ToString() + "'>" + airport[j].iataCode + "</td>";
                        else
                            html += "<td></td>";
                    }
                    else
                    {
                        if (j < 0)
                            html += "<td title='" + airport[i].ToString() + "'>" + airport[i].iataCode + "</td>";
                        else
                            html += "<td>" + matrix[i, j].ToString() + "</td>";
                    }
                }
                html += "</tr>";
            }
            html += "</table>";
            File.WriteAllText(Path.Combine(Directory.GetCurrentDirectory(), htmlName), html);
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
            double rad = Convert.ToSingle(Math.PI / 180);
            double p1x = rad * float.Parse(p1.latitude);
            double p1y = rad * float.Parse(p1.longitude);
            double p2x = rad * float.Parse(p2.latitude);
            double p2y = rad * float.Parse(p2.longitude);

            double x = Math.Abs(p1x-p2x);
            double y = Math.Abs(p1y-p2y);
            double a = Math.Pow(Math.Sin(x / 2), 2) + Math.Cos(p1x) * Math.Cos(p2x) * Math.Pow(Math.Sin(y / 2), 2);
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
                    if (route.Contains("airport:"))
                        auxRoutes.Add(route.Substring(8,3));
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
            catch (Exception e) {return null;}
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
