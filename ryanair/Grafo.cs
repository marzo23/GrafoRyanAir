using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ryanair
{
    class Grafo<T> where T:IElement
    {
        public List<Node<T>> NodeList { get; set; }
        public Node<T> DijkstraRoot { get; set; }
        public void SetDjikstraRootById(int id)
        {
            DijkstraRoot = NodeList.Find(n => n.Element.id == id);
        }


        public Grafo(){
            NodeList = new List<Node<T>>();
            DijkstraRoot = null;
        }

        void ResetDijkstraLbl()
        {
            for (int i = 0; i < NodeList.Count; i++)
            {
                NodeList[i].dijkstraLbl = null;
            }
        }

        public List<Node<T>> GetShortestRoute(Node<T> node)
        {
            List<Node<T>> aux = new List<Node<T>>();
            while (node != DijkstraRoot && node != null)
            {
                if (node.dijkstraLbl == null || node.dijkstraLbl.previousNode == null)
                    return null;
                aux.Add(node);
                Console.WriteLine(node.Element.ToString());
                node = node.dijkstraLbl.previousNode;
            }
            return aux;
        }

        public void Dijkstra(Node<T> root, double[,] aristas, List<Node<T>> evaluated)
        {
            if(evaluated == null)
            {
                evaluated = new List<Node<T>>();
                ResetDijkstraLbl();
                if (DijkstraRoot == null && root != null)
                    DijkstraRoot = root;
                else if (DijkstraRoot != null)
                    root = DijkstraRoot;
            }            
            if (root != null)
            {
                evaluated.Add(root);
                Node<T> min = null;
                for (int i = 0; i < root.ElementList.Count; i++)
                {
                    if(!evaluated.Contains(root.ElementList[i]))
                    if(aristas[root.Element.id, root.ElementList[i].Element.id] != 0)
                    {
                        if (root.ElementList[i].dijkstraLbl == null)
                            root.ElementList[i].dijkstraLbl = new DijkstraLbl<T> { previousNode = root, value = aristas[root.Element.id, root.ElementList[i].Element.id]+(root.dijkstraLbl==null?0: root.dijkstraLbl.value) };
                        else
                            if (root.ElementList[i].dijkstraLbl.value > aristas[root.Element.id, root.ElementList[i].Element.id] + (root.dijkstraLbl == null ? 0 : root.dijkstraLbl.value))
                                root.ElementList[i].dijkstraLbl = new DijkstraLbl<T> { previousNode = root, value = aristas[root.Element.id, root.ElementList[i].Element.id] + (root.dijkstraLbl == null ? 0 : root.dijkstraLbl.value) };
                        if (min == null)
                            min = root.ElementList[i];
                        else
                            if(min.dijkstraLbl.value > root.ElementList[i].dijkstraLbl.value)
                                min = root.ElementList[i];
                    }
                }
                if (min != null)
                    Dijkstra(min, aristas, evaluated);
            }            
        }

        public void GenerateGrafoByListAndRel(List<T> list, bool[,] relations)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Node<T> pivotNode = this.NodeList.Find(n => n.Element.id == i);
                if (pivotNode == null)
                {
                    pivotNode = new Node<T> { Element = list[i], ElementList = new List<Node<T>>() };
                    this.NodeList.Add(pivotNode);
                }
                for (int j = 0; j < list.Count; j++)
                {
                    if (relations[i, j])
                    {
                        Node<T> aux = this.NodeList.Find(n => n.Element.id == j);
                        if (aux == null)
                        {
                            this.NodeList.Add(new Node<T> { Element = list[j], ElementList = new List<Node<T>>() });
                            pivotNode.ElementList.Add(this.NodeList.Last());
                        }
                        else
                            pivotNode.ElementList.Add(aux);
                    }

                }
            }
        }
    }

    class DijkstraLbl<T>
    {
        public double value { get; set; }
        public Node<T> previousNode { get; set; }
    }

    public interface IElement
    {
        int id { get; set; }
    }

    class Node<T>
    {
        public T Element { get; set; }
        public List<Node<T>> ElementList { get; set; }
        public DijkstraLbl<T> dijkstraLbl { get; set; }
        public Node() {
            ElementList = new List<Node<T>>();
            Element = default(T);
            dijkstraLbl = null;
        }
    }
}
