using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Patternson
{
    /// <summary>
    /// PatternKnot
    /// </summary>
    public class PatternKnot<T>
    {
        public int Position;
        public T Element;
    }
        

    /// <summary>
    /// Pattern
    /// </summary>
    public class Pattern<T> : List<PatternKnot<T>>
    {
        #region public methods()
        public bool Add(int newPosition, T newElement)
        {
            foreach (var knot in this)
            {
                if ((knot.Position == newPosition) && knot.Element.Equals(newElement)) return false;
            }

            var newKnot = new PatternKnot<T>();

            newKnot.Position = newPosition;
            newKnot.Element = newElement;

            this.Add(newKnot);

            return true;
        }

        /// <summary>
        /// Prints the content of the pattern in a readable text form
        /// </summary>
        /// <returns></returns>
        public String AsText()
        {
            StringBuilder text = new StringBuilder();

            foreach (var knot in this)
            {
                text.Append("(" + knot.Position + ")" + knot.Element.ToString() + " ");
            }

            return text.ToString().TrimEnd(' ');
        }
        #endregion
    }


    /// <summary>
    /// PatternHistory
    /// </summary>
    public class PatternHistory<T> : Pattern<T>
    {
        public List<int> TimeLine { get; set; } = new List<int>();

        public string SourceLine { get; set; }

        #region public methods()
        /// <summary>
        /// Returns for a knot of the pattern the time positions it occurs in the data stream
        /// </summary>
        /// <param name="knot"></param>
        /// <returns></returns>
        public List<int> GetTimePositions(PatternKnot<T> knot)
        {
            if (!Contains(knot)) return null;

            return TimeLine.Select(timePoint => timePoint + knot.Position).ToList();
        }

        /// <summary>
        /// Prints the content of the pattern and its timeline in a readable text form
        /// </summary>
        /// <returns></returns>
        public new string AsText()
        {
            StringBuilder text = new StringBuilder();

            text.Append(base.AsText() + "\n");

            text.Append("t: ");
            for (int i = 0; i < TimeLine.Count; i++)
            {
                text.Append(TimeLine[i] + " ");
            }

            if (!string.IsNullOrEmpty(SourceLine))
            {
                text.Append("\ns: " + SourceLine);
            }

            return text.ToString().TrimEnd(' ');
        }
        #endregion
    }


    /// <summary>
    /// PatternTable: a dictionary of patterns and their timelines, source references and pedigrees.
    /// The keys are the pattern IDs. 
    /// </summary>
    public class PatternTable<T> : Dictionary<int, PatternHistory<T>>
    {
        public List<int> SourceOffset;

        public Dictionary<int, List<int>> Pedigree;

        #region public methods()
        /// <summary>
        /// Returns for a time position in the data stream the index of the source to which it belongs
        /// </summary>
        /// <param name="timePos"></param>
        /// <returns></returns>
        private int GetSource(int timePos)
        {
            for (int source = 0; source < SourceOffset.Count; source++)
            {
                if (SourceOffset[source] > timePos)
                {
                    source--;
                    return source;
                }
            }

            return (SourceOffset.Count - 1);
        }

        /// <summary>
        /// Returns for a occurrence of a pattern in the data stream to which sources the knots of the pattern belong
        /// </summary>
        /// <param name="pat"></param>
        /// <param name="timePos"></param>
        /// <returns></returns>
        public List<int> GetSources(PatternHistory<T> pat, int timePos)
        {
            if (!ContainsValue(pat)) return null;
            if (!pat.TimeLine.Contains(timePos)) return null;

            return pat.Select(knot => GetSource(timePos + knot.Position)).ToList();            
        }

        /// <summary>
        /// Adds a new pattern to the pattern table. Returns the ID assigned to the pattern
        /// </summary>
        /// <param name="patEntry"></param>
        /// <returns></returns>
        public int AddPatternEntry(PatternHistory<T> patEntry)
        {
            int patId = Count + 1;

            Add(patId, patEntry);

            return patId;
        }

        /// <summary>
        /// Adds a new timepoint for a pattern in the pattern table. Returns false, if the pattern ID does not exists or the timeline already contains that timepoint
        /// </summary>
        /// <param name="patId"></param>
        /// <param name="timePoint"></param>
        /// <returns></returns>
        public bool AddTimePoint(int patId, int timePoint)
        {
            if (!this.ContainsKey(patId)) return false;

            if (this[patId].TimeLine.Contains(timePoint)) return false;
                
            this[patId].TimeLine.Add(timePoint);
            this[patId].TimeLine.Sort();
                        
            return true;
        }

        /// <summary>
        /// Prints the content of the pattern table in a readable textform
        /// </summary>
        /// <returns></returns>
        public string AsText()
        {
            var sb = new StringBuilder();

            foreach (var pat in this)
            {
               sb.Append("ID: " + pat.Key + "\n" + pat.Value.AsText() + "\n");

                if (Pedigree == null) continue;

                if (Pedigree.ContainsKey(pat.Key))
                {
                    sb.Append("a: ");
                    foreach (var ancestor in Pedigree[pat.Key])
                    {
                        sb.Append(ancestor + " ");
                    }
                    sb.Append("\n\n");                   
                }
                else
                {
                    sb.Append("a: -\n\n");
                }
            }

            return sb.ToString().TrimEnd('\n');
        }
        #endregion
    }


    /// <summary>
    /// PatternFrequency
    /// </summary>

    public class PatternFrequency
    {
        public int PatId { get; set; }
        public int OccurCount { get; set; }
    }


    /// <summary>
    /// PatternRecognition
    /// </summary>    
    public class PatternRecognition<T>
    {
        public List<T> IgnoreData = new List<T>();

        #region SearchPattern(List<byte[]> data)
        /// <summary>
        /// Searches patterns between multiple data sources and sorts the pattern according to the sources they refer to 
        /// </summary>
        /// <param name="data"></param>
        public PatternTable<T> SearchPattern(IList<T>[] data)
        {
            if (data == null)
                throw new ArgumentNullException("SearchPattern: input data == null!");


            var chain = data.SelectMany(d => d).ToList();
                               
            var patTable = SearchPattern(chain);


            // notes the starting point for each data source in the timeline of the pattern
            var sourceOffset = new List<int>();

            for (int i = 0, offset = 0; i < data.Count(); i++ )
            {
                sourceOffset.Add(offset);
                offset += data[i].Count();
            }

            patTable.SourceOffset = sourceOffset;


            //// determine a pedigree for each pattern listed in the table
            
            //foreach (var entryA in patTable)
            //{
            //    var keyA = entryA.Key;
            //    var patA = entryA.Value;

            //    var chainA = PatternToChain(patA);

            //    foreach (var entryB in patTable)
            //    {
            //        var keyB = entryB.Key;
            //        var patB = entryB.Value;

            //        if (keyB == keyA) continue;

            //        var chainB = PatternToChain(patB);

            //        // TODO jhd: work here! 
            //    }
            //}

            
            var newPatTable = new PatternTable<T>(); 

            foreach (var entry in patTable.ToList())
            {
                var id = entry.Key;
                var pat = entry.Value;

                var diffTable = new Dictionary<string, List<int>>();

                // calculates for each occurence (t) of the pattern the shift in data source from
                // one pattern knot to the next and saves these differences in a string (diffKey).
                // The string can vary over the timeline. For each diffKey the timepoints of its occurence
                // are noted 
                foreach (var t in pat.TimeLine)
                {
                    var sources = patTable.GetSources(pat, t);

                    var diff = new StringBuilder();
                    for (int k = 1; k < sources.Count(); k++)
                    {
                        diff.Append(sources[k] - sources[k - 1] + " ");
                    }
                    
                    var diffKey = diff.ToString().TrimEnd(' ');
                    if (!diffTable.ContainsKey(diffKey))
                    {
                        diffTable.Add(diffKey, new List<int>());
                    }

                    diffTable[diffKey].Add(t);
                }

                // if the pattern has more than one diffKey, additional entries of the same pattern
                // will be created in the pattern table. However, the diffkey should occur at least twice
                // in the timeline of the pattern   
                foreach (var diff in diffTable)
                {
                    if (diff.Value.Count > 1)
                    {
                        var patHistory = new PatternHistory<T>();
                        foreach (var knot in pat.ToList()) patHistory.Add(knot);
                        patHistory.TimeLine = diff.Value;
                        patHistory.SourceLine = diff.Key;

                        newPatTable.AddPatternEntry(patHistory);
                    }
                }
            }

            newPatTable.SourceOffset = patTable.SourceOffset;

            return newPatTable;            
        }
        #endregion

        #region SearchPattern(byte[] data)
        /// <summary>
        /// Searches Pattern in a data source, removes duplicates and determines a pedigree of the found patterns
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public PatternTable<T> SearchPattern(IList<T> data)
        {
            if (data == null)
                throw new ArgumentNullException("SearchPattern: input data == null!");

            
            var patTable = SearchRepetitions(data);


            // check for duplicates among the PatternEntries and remove them
            List<int> removeKey = new List<int>();
            HashSet<int> checkedKey = new HashSet<int>();

            foreach (var entryA in patTable.ToList())
            {
                var keyA = entryA.Key;
                var patA = entryA.Value;

                checkedKey.Add(keyA);

                foreach (var entryB in patTable.ToList())
                {
                    var keyB = entryB.Key;
                    var patB = entryB.Value;

                    if (checkedKey.Contains(keyB)) continue;
                                        
                    int identical_knots = 0;

                    if (patA.Count == patB.Count)
                    {
                        for (int i = 0; i < patA.Count; i++)
                            if (patA[i].Element.Equals(patB[i].Element))
                                if (patA[i].Position == patB[i].Position)
                                    identical_knots++;
                    }

                    if (identical_knots == patA.Count)
                    {
                        for (int i = 0; i < patB.TimeLine.Count; i++)
                            if (!patTable[keyA].TimeLine.Contains(patB.TimeLine[i]))
                                patTable[keyA].TimeLine.Add(patB.TimeLine[i]);

                        patTable[keyA].TimeLine.Sort();

                        removeKey.Add(keyB);
                    }
                }
            }


            foreach (var key in removeKey)
            {
                patTable.Remove(key);
            }


            //// determine for each pattern its "ancestors".These are patterns inside the pattern which are 
            //// listed in the pattern table as well and, hence, can be considered "independent" from
            //// examined pattern
            //var ancestors = new Dictionary<int, List<int>>();

            //foreach (var entryA in patTable)
            //{
            //    var keyA = entryA.Key;
            //    var patA = entryA.Value;

            //    foreach (var entryB in patTable)
            //    {
            //        var keyB = entryB.Key;
            //        var patB = entryB.Value;

            //        if (keyB == keyA) continue;
            //        if (patB.Count > patA.Count) continue;

            //        for (int i = 0; i < (patA.Count - patB.Count + 1); i++)
            //        {
            //            if (patA[i].Element != patB[0].Element) continue;

            //            var shift = patA[i].Position - patB[0].Position;
            //            var identicalKnots = 0;

            //            for (int j = 1; j < patB.Count; j++)
            //            {
            //                for (int k = i + 1; k < patA.Count; k++)
            //                {
            //                    if (patB[j].Element == patA[k].Element)
            //                    {
            //                        if ((patB[j].Position + shift) == patA[k].Position)
            //                        {
            //                            identicalKnots++;
            //                            break;
            //                        }
            //                    }
            //                }                            
            //            }

            //            if (identicalKnots == patB.Count)
            //            {
            //                if (!ancestors.ContainsKey(keyA)) ancestors.Add(keyA, new List<int>());

            //                ancestors[keyA].Add(keyB);
            //            }                        
            //        }                       
            //    }

            //    if (ancestors.ContainsKey(keyA)) ancestors[keyA].Sort();
            //}

            //patTable.Pedigree = ancestors;
            
            return patTable;            
        }
        #endregion

        #region SearchRepetitions()
        /// <summary>
        /// Search for repetitions in raw data
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private PatternTable<T> SearchRepetitions(IList<T> data)
        {
            var patTable = new PatternTable<T>();

            for (int shift = 1; shift < data.Count(); shift++)
            {
                var patEntry = new PatternHistory<T>();

                for (int i = 0; i < (data.Count() - shift); i++)
                {
                    if (IgnoreData.Contains(data[i])) continue;

                    if (IgnoreData.Contains(data[i + shift])) continue;


                    if (data[i].Equals(data[i + shift]))
                    {
                        var knot = new PatternKnot<T>();

                        knot.Element = data[i];
                        knot.Position = i;

                        patEntry.Add(knot);

                        if (patEntry.Count == 1)
                        {
                            patEntry.TimeLine.Add(i);
                            patEntry.TimeLine.Add(i + shift);
                        }
                    }
                }

                if (patEntry.Count > 1) // pattern should have more than one knot
                {
                    for (int j = 0; j < patEntry.Count; j++) patEntry[j].Position -= patEntry.TimeLine[0];

                    patTable.AddPatternEntry(patEntry);
                }
            }

            return patTable;
        }
        #endregion

        private List<T> PatternToChain(PatternHistory<T> pat)
        {
            if (pat == null)
                throw new ArgumentNullException("pat");

            var chain = new List<T>();

            var emptyElement = IgnoreData == null ? default(T) : IgnoreData[0];

            for (int i = 0, previousPos = 0; i < pat.Count; i++)
            {
                for (int j = previousPos; j < pat[i].Position; j++) chain.Add(emptyElement);

                chain.Add(pat[i].Element);

                previousPos = pat[i].Position + 1;
            }

            return chain;
        }
    }
}
