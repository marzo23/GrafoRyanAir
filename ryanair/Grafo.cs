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

        public Node<T> Kruscal(double[,] aristas)
        {
            List<Node<T>> treeList = new List<Node<T>>();
            List<int[]> aristasSorted = SortAristas(aristas);
            foreach (int[] arista in aristasSorted)
            {
                if (treeList.Count <= 0)
                {
                    Node<T> aux = this.NodeList.Find(n => n.Element.id == arista[0]);
                    if (aux != null)
                    {
                        treeList.Add(new Node<T> { Element = aux.Element });
                    }
                }
                if (treeList.Count > 0)
                {
                    Node<T> node = null;
                    Node<T> next = null;
                    int nodeTreeNum = -1;
                    int nextTreeNum = -1;
                    for (int i = 0; i < treeList.Count; i++)
                    {
                        if (node == null)
                        {
                            node = FindTreeNodeById(treeList[i], arista[0], new List<Node<T>>());
                            nodeTreeNum = i;
                        }
                        if (next == null)
                        {
                            next = FindTreeNodeById(treeList[i], arista[1], new List<Node<T>>());
                            nextTreeNum = i;
                        }
                        if (node != null && next != null)
                            break;
                    }
                    
                    if (node != null && next != null)
                    {
                        if (nodeTreeNum == nextTreeNum)
                            continue;
                        else
                        {
                            node.ElementList.Add(next);
                            next.ElementList.Add(node);
                            treeList.Remove(treeList.ElementAt(nextTreeNum));
                        }
                    }
                    else
                    {
                        if (node == null && next == null)
                        {
                            node = this.NodeList.Find(n => n.Element.id == arista[0]);
                            next = this.NodeList.Find(n => n.Element.id == arista[1]);
                            if (node != null && next != null)
                            {
                                treeList.Add(new Node<T> { Element = node.Element });
                                treeList.Last().ElementList.Add(new Node<T> { Element = next.Element });
                                treeList.Last().ElementList.Last().ElementList.Add(treeList.Last());
                            }
                        }
                        else
                        {
                            if (node == null)
                            {
                                node = this.NodeList.Find(n => n.Element.id == arista[0]);
                                next.ElementList.Add(new Node<T> { Element = node.Element });
                                next.ElementList.Last().ElementList.Add(next);
                            }
                            else
                            {
                                next = this.NodeList.Find(n => n.Element.id == arista[1]);
                                node.ElementList.Add(new Node<T> { Element = next.Element });
                                node.ElementList.Last().ElementList.Add(node);
                            }
                        }
                    }
                        
                }                 
            }
            if (treeList.Count == 1)
                return treeList[0];
            else
                return null;
        }

        public List<int[]> SortAristas(double[,] aristasMatrix)
        {
            List<int[]> aristas = new List<int[]>();
            for (int i = 0; i < aristasMatrix.GetLength(0); i++)
            {
                for (int j = i+1; j < aristasMatrix.GetLength(1); j++)
                {
                    if (aristasMatrix[i, j] != 0)
                        aristas.Add(new int[] { i, j });
                }
            }

            QuickSort(aristasMatrix, ref aristas, 0, aristas.Count-1);

            return aristas;
        }

        public void QuickSort(double[,] aristasMatrix, ref List<int[]> aristas, int min, int max)
        {
            int p = (max + min) / 2;
            double pivot = aristasMatrix[aristas[p][0], aristas[p][1]];
            int i = min;
            int j = max;
            do
            {
                while (aristasMatrix[aristas[i][0], aristas[i][1]] < pivot) i++;
                while (aristasMatrix[aristas[j][0], aristas[j][1]] > pivot) j--;

                if(i<=j)
                {
                    int[] aux = aristas[i];
                    aristas[i] = aristas[j];
                    aristas[j] = aux;
                    i++;
                    j--;
                }

            } while (i <= j);
            if (min < j)
                QuickSort(aristasMatrix, ref aristas, min, j);
            if (max > i)
                QuickSort(aristasMatrix, ref aristas, i, max);
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
            if (node == DijkstraRoot)
                aux.Add(node);
            return aux;
        }

        public void Prim(Node<T> treeRoot, double[,] aristas, List<Node<T>> evaluated)
        {
            if (treeRoot != null)
            {
                if (evaluated == null)
                {
                    evaluated = new List<Node<T>>();
                    Node<T> aux = this.NodeList.Find(n => n.Element.id == treeRoot.Element.id);
                    if (aux != null)
                        evaluated.Add(aux);
                }
                Node<T> min = null;
                Node<T> previousNode = null;
                for (int j = 0; j < evaluated.Count; j++)
                {
                    for (int i = 0; i < evaluated[j].ElementList.Count; i++)
                    {
                        if (!evaluated.Contains(evaluated[j].ElementList[i]))
                            if (aristas[evaluated[j].Element.id, evaluated[j].ElementList[i].Element.id] != 0) //////VALIDACION INNECESARIA?
                            {
                                if (min == null && previousNode == null)
                                {
                                    previousNode = evaluated[j];
                                    min = evaluated[j].ElementList[i];
                                }                                    
                                else if (aristas[evaluated[j].Element.id, evaluated[j].ElementList[i].Element.id]< aristas[previousNode.Element.id, min.Element.id])
                                {
                                    previousNode = evaluated[j];
                                    min = evaluated[j].ElementList[i];
                                }
                            }
                    }
                }
                if (min != null && previousNode != null)
                {
                    Node<T> aux = FindTreeNodeById(treeRoot, previousNode.Element.id, new List<Node<T>>());
                    if (aux != null)
                    {
                        evaluated.Add(min);
                        aux.ElementList.Add(new Node<T> { Element = min.Element });
                        aux.ElementList.Last().ElementList.Add(aux);
                        Prim(treeRoot, aristas, evaluated);
                    }                 
                }
            }
        }

        public Node<T> FindTreeNodeById(Node<T> treeRoot, int id, List<Node<T>> procesed)
        {
            if (treeRoot.Element.id == id)
                return treeRoot;
            else
            {
                procesed.Add(treeRoot);
                for (int i = 0; i < treeRoot.ElementList.Count; i++)
                {
                    if (!procesed.Contains(treeRoot.ElementList[i]))
                    {
                        Node<T> aux = FindTreeNodeById(treeRoot.ElementList[i], id, procesed);
                        if (aux != null)
                            return aux;
                    }                    
                }
            }
            return null;
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
                for (int j = 0; j < evaluated.Count; j++)
                {
                    for (int i = 0; i < evaluated[j].ElementList.Count; i++)
                    {
                        if (!evaluated.Contains(evaluated[j].ElementList[i]))
                            if (aristas[evaluated[j].Element.id, evaluated[j].ElementList[i].Element.id] != 0)
                            {
                                if (evaluated[j].ElementList[i].dijkstraLbl == null)
                                    evaluated[j].ElementList[i].dijkstraLbl = new DijkstraLbl<T> { previousNode = evaluated[j], value = aristas[evaluated[j].Element.id, evaluated[j].ElementList[i].Element.id] + (evaluated[j].dijkstraLbl == null ? 0 : evaluated[j].dijkstraLbl.value) };
                                else
                                    if (evaluated[j].ElementList[i].dijkstraLbl.value > aristas[evaluated[j].Element.id, evaluated[j].ElementList[i].Element.id] + (evaluated[j].dijkstraLbl == null ? 0 : evaluated[j].dijkstraLbl.value))
                                    evaluated[j].ElementList[i].dijkstraLbl = new DijkstraLbl<T> { previousNode = evaluated[j], value = aristas[evaluated[j].Element.id, evaluated[j].ElementList[i].Element.id] + (evaluated[j].dijkstraLbl == null ? 0 : evaluated[j].dijkstraLbl.value) };
                                if (min == null)
                                    min = evaluated[j].ElementList[i];
                                else
                                    if (min.dijkstraLbl.value > evaluated[j].ElementList[i].dijkstraLbl.value)
                                    min = evaluated[j].ElementList[i];
                            }
                    }
                }
                /*for (int i = 0; i < root.ElementList.Count; i++)
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
                }*/
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
