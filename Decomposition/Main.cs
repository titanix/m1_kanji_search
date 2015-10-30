using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using LayeredGraphDictionary;

namespace Decomposition
{
    public class Prototype
    {
        /// <summary>
        /// Point d'entrée de l'application.
        /// </summary>
        public static void Main()
        {
            IdsReader idsr = new IdsReader();
            LoadData(idsr);

            var testX = idsr.RealAlgo(new List<char> { '殺', '式' });

            var graph = idsr.GetDecompositionWithGraph("部");

            var dkorosu = idsr.GetDecomposition("殺").Distinct();
            var dshiki = idsr.GetDecomposition("式").Distinct();
            var test = idsr.SearchKanji(new List<char> { '殺', '式' });
#if OLD
            var set0 = FilterChar(dkorosu);
            var set1 = FilterChar(dshiki);
            idsr.__debug(set0, set1);

            // on cherche 蠅
            var test2 = idsr.SearchKanji(new List<char> { '虫', '縄' });
            var test3 = idsr.SearchKanji(new List<char> { '虫', '繩' });
#endif
            // on cherche 歸
            var test4 = idsr.SearchKanji(new List<char> { '師', '雪', '足' });
            var test5 = idsr.RealAlgo(new List<char> { '師', '雪', '足' });
        }

        public static void AppendToFile(string path, string content)
        {
            using (StreamWriter sw = new StreamWriter(path, true, Encoding.UTF8))
            {
                sw.WriteLine(content);
            }
        }

        public static void LoadData(IdsReader idsr)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = "Decomposition.ids.txt";

            using (Stream resourceStream = assembly.GetManifestResourceStream(resourceName))
            {
                idsr.AnalyzeDataFile(resourceStream);
            }
        }

        public static List<string> FilterChar(IEnumerable<string> input)
        {
            List<string> result = new List<string>();
            foreach (string item in input)
            {
                try
                {
                    Char.ConvertToUtf32(item, 0);
                    result.Add(item);
                }
                catch
                {
                }
            }
            return result;
        }
    }

    public class IdsReader
    {
        internal AdjacencyListGraph<string> DGraph = new AdjacencyListGraph<string>();
        internal AdjacencyListGraph<string> CGraph = new AdjacencyListGraph<string>();

        private void LineAction(string line)
        {
            // si la ligne contient des entités CDP on ne la traite pas
            Regex regex = new Regex("&CDP-[0-9|A-F]+;");
            if (regex.IsMatch(line))
            {
                return;
            }
            string[] parts = line.Split('\t');
            if (parts.Length < 3)
            {
                return;
            }
            // on supprime la portion 2 l'éventuelle partie entre crochets
            if (parts[2].IndexOf('[') > 0)
            {
                parts[2] = parts[2].Substring(0, parts[2].IndexOf('['));
            }

            IEnumerable<char> components = parts[2].ToCharArray().Where(c => c < '⿰' || c > '⿻');
            foreach (char c in components)
            {
                if (!parts[1].Equals(c.ToString()))
                {
                    DGraph.AddVertex(parts[1], c.ToString());
                    CGraph.AddVertex(c.ToString(), parts[1]);
                }
            }
        }

        public void AnalyzeDataFile(Stream stream)
        {
            using (StreamReader sr = new StreamReader(stream))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    LineAction(line);
                }
            }
        }

        // implémente la fonction d(k)
        public List<string> GetDecomposition(string k)
        {
            return new List<string>(GetDecomposition(DGraph, k, null, null));
        }

        // implémente la fonction rc(c)
        public List<string> GetCompounds(string k)
        {
            return new List<string>(GetDecomposition(CGraph, k, null, null));
        }

        private IEnumerable<string> GetDecomposition(AdjacencyListGraph<string> graph, string k, List<Vertex> parcoured, List<string> result)
        {
            if (parcoured == null)
            {
                parcoured = new List<Vertex>();
            }
            if (result == null)
            {
                result = new List<string>();
            }

            Vertex v = graph.GetVertex(k);
            if (v != null && !parcoured.Contains(v))
            {
                parcoured.Add(v);
                foreach (Vertex link in v.LinkedVertices)
                {
                    result.Add(link.Value);
                    GetDecomposition(graph, link.Value, parcoured, result);
                }
            }

            return result;
        }

        public Tuple<IEnumerable<string>, string> GetDecompositionWithGraph(string k)
        {
            return GetDecompositionWithGraph(CGraph, k, null, null, new StringBuilder());
        }

        private Tuple<IEnumerable<string>, string> GetDecompositionWithGraph(AdjacencyListGraph<string> graph, string k, List<Vertex> parcoured, List<string> result, StringBuilder vs)
        {
            if (parcoured == null)
            {
                parcoured = new List<Vertex>();
            }
            if (result == null)
            {
                result = new List<string>();
            }

            Vertex v = graph.GetVertex(k);
            if (v != null && !parcoured.Contains(v))
            {
                parcoured.Add(v);
                foreach (Vertex link in v.LinkedVertices)
                {
                    result.Add(link.Value);
                    vs.Append(String.Format("{0} -> {1};\r\n", v.Value, link.Value));
                    GetDecompositionWithGraph(graph, link.Value, parcoured, result, vs);
                }
            }

            return new Tuple<IEnumerable<string>, string>(result, vs.ToString());
        }

        // implémente la fonction e(k, c)
        public bool ComponentExists(string kanji, string component)
        {
            var decomp = GetDecomposition(kanji);
            return decomp.Contains(component);
        }

        public IEnumerable<string> SearchKanji(IList<char> kanjis)
        {
            if (kanjis.Count() < 1)
            {
                throw new ArgumentException("Not enough kanji in input.");
            }

            HashSet<string> initSet = GetSet(kanjis[0].ToString());
            HashSet<string> nextSet = null;

            for (int i = 1; i < kanjis.Count(); i++)
            {
                nextSet = GetSet(kanjis[i].ToString());
                initSet.IntersectWith(nextSet);
            }

            return initSet;
        }

        public IEnumerable<string> RealAlgo(IList<char> kanjis)
        {
            List<string> initStep = GetSet(kanjis[0].ToString()).ToList();
            List<string> result = initStep;

            // on itère sur les Kn d'entrées
            for (int i = 1; i < kanjis.Count; i++)
            {
                string Kn = kanjis[i].ToString();
                List<string> newResult = new List<string>();
                //var KnComp = GetDecomposition(Kn);

                foreach (string elem in result)
                {
                    bool haveCommonComponent = false;
                    foreach (var comp in GetDecomposition(elem))
                    {
                        haveCommonComponent |= ComponentExists(Kn, comp);
                    }
                    if (haveCommonComponent)
                    {
                        newResult.Add(elem);
                    }
                }

                result = newResult;
            }

            return result;
        }

        public IEnumerable<string> __debug(IEnumerable<string> set0, IEnumerable<string> set1)
        {
            HashSet<string> initSet = new HashSet<string>(set0.SelectMany(c => GetCompounds(c)));
            HashSet<string> nextSet = new HashSet<string>(set1.SelectMany(c => GetCompounds(c)));

            initSet.IntersectWith(nextSet);

            return initSet;
        }

        private HashSet<string> GetSet(string kanji)
        {
            HashSet<string> initSet = new HashSet<string>();
            var comp0 = GetDecomposition(kanji);
            foreach (string c in comp0)
            {
                foreach (string k in GetCompounds(c))
                {
                    initSet.Add(k);
                }
            }
            return initSet;
        }
    }
}
