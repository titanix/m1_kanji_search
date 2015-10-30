using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace LayeredGraphDictionary
{
    class Vertex
    {
        /// <summary>
        /// Identifiant unique utilisé pour la sérialisation du graph.
        /// </summary>
        private uint id;

        private static uint idGenerator;

        /// <summary>
        /// Lien vers d'autres noeuds du graph.
        /// </summary>
        private List<Vertex> linkedVertices = new List<Vertex>();

        internal void AddVertexLink(Vertex v)
        {
            if (!linkedVertices.Contains(v))
            {
                linkedVertices.Add(v);
            }
        }

        public Vertex()
        {
            id = idGenerator++;
        }

        /// <summary>
        /// Chaîne de caractère associée au noeud.
        /// </summary>
        public string Value { get; internal set; }

        public uint ID { get { return id; } }

        public IReadOnlyList<Vertex> LinkedVertices { get { return linkedVertices.AsReadOnly(); } }
    }

    class AdjacencyListGraph<T>
    {
        Dictionary<T, Vertex> vertexList = new Dictionary<T, Vertex>();

        internal void AddVertex(T newVertex, params T[] linkedVertex)
        {
            Vertex v = GetVertexHelper(newVertex);
            foreach (T n in linkedVertex)
            {
                Vertex lv = GetVertexHelper(n);
                v.AddVertexLink(lv);
            }
        }

        internal void Serialize(StreamWriter output)
        {
            foreach (KeyValuePair<T, Vertex> pair in vertexList)
            {
                Vertex v = pair.Value;
                output.Write(String.Format("{0}|{1}|", v.ID, v.Value));

                StringBuilder linkList = new StringBuilder();
                foreach (Vertex link in v.LinkedVertices)
                {
                    linkList.Append(link.ID);
                    linkList.Append(";");
                }

                output.Write(linkList.ToString());
                output.WriteLine();
            }
        }

        /// <summary>
        /// Cherche si un Vertex correspondant au Node passé en paramètre existe dans le graph.
        /// Si oui, ce Vertex est renvoyé.
        /// Si non, une nouvelle instance de Vertex est crée, ajouté au graph et renvoyée.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private Vertex GetVertexHelper(T n)
        {
            Vertex v = null;
            if (vertexList.ContainsKey(n))
            {
                v = vertexList[n];
            }
            else
            {
                v = CreateVertex(n);
            }
            return v;
        }

        public Vertex GetVertex(T n)
        {
            if (vertexList.ContainsKey(n))
            {
                return vertexList[n];
            }
            return null;
        }

        /// <summary>
        /// Crée une nouvelle instance de Vertex via un Node donné.
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private Vertex CreateVertex(T n)
        {
            Vertex v = new Vertex();
            v.Value = n.ToString();
            
            if (v != null)
            {
                vertexList.Add(n, v);
                return v;
            }
            else
            {
                throw new ArgumentException();
            }
        }
    }
}
