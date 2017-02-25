using System;
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
    public class PatternKnot
    {
        public int Position;
        public byte Element;
    }
        

    /// <summary>
    /// Pattern
    /// </summary>
    public class Pattern : List<PatternKnot>
    {
        #region public methods()
        public bool Add(int newPosition, byte newElement)
        {
            foreach (PatternKnot knot in this)
            {
                if ((knot.Position == newPosition) && (knot.Element == newElement)) return false;
            }

            PatternKnot newKnot = new PatternKnot();

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

            foreach (PatternKnot knot in this)
            {
                text.Append("(" + knot.Position + ")" + (char)knot.Element + " ");
            }

            return text.ToString().TrimEnd(' ');
        }
        #endregion
    }


    /// <summary>
    /// PatternHistory
    /// </summary>
    public class PatternHistory : Pattern
    {
        public List<int> TimeLine { get; set; } = new List<int>();

        public string SourceLine { get; set; }

        #region public methods()
        /// <summary>
        /// Returns for a knot of the pattern the time positions it occurs in the data stream
        /// </summary>
        /// <param name="knot"></param>
        /// <returns></returns>
        public List<int> GetTimePositions(PatternKnot knot)
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
    /// PatternTable. A dictionary of patterns and their timelines. The keys are the pattern IDs 
    /// </summary>
    public class PatternTable : Dictionary<int, PatternHistory>
    {
        public List<int> SourceOffset = new List<int>();

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
        public List<int> GetSources(PatternHistory pat, int timePos)
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
        public int AddPatternEntry(PatternHistory patEntry)
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
                sb.Append("ID: " + pat.Key + "\n" + pat.Value.AsText() + "\n\n");
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
    public class PatternRecognition
    {
        public List<byte> IgnoreData = new List<byte>();

        #region SearchPattern(List<byte[]> data)
        /// <summary>
        /// SearchPattern(List<byte[]> data)
        /// </summary>
        /// <param name="data"></param>
        public PatternTable SearchPattern(List<byte[]> data)
        {
            if (data == null)
                throw new ArgumentNullException("SearchPattern: input data == null!");

            // glues all data sources together to one data chain
            var length = data.Select(d => d.Length).Sum();
            var chain = new byte[length];

            int copyOffset = 0;
            foreach (var d in data)
            {
                d.CopyTo(chain, copyOffset);
                copyOffset += d.Length;
            }


            var patTable = SearchPattern(chain);

            // notes the starting point for each data source in the timeline of the pattern
            for (int i = 0, offset = 0; i < data.Count(); i++ )
            {
                patTable.SourceOffset.Add(offset);
                offset += data[i].Length;
            }


            var newPatTable = new PatternTable(); 

            foreach (var entry in patTable)
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
                    for (int k = 1; k < sources.Count; k++)
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
                        var patHistory = new PatternHistory();
                        foreach (var knot in pat) patHistory.Add(knot);
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
        /// SearchPattern(byte[] data)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public PatternTable SearchPattern(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException("SearchPattern: input data == null!");
            
            // *********************************************************
            // search patterns in raw data
            // *********************************************************

            PatternTable pat_table = new PatternTable();

            
            for (int shift = 1; shift < data.Length; shift++)
            {
                var pat_entry = new PatternHistory();

                for (int i = 0; i < (data.Length - shift); i++)
                {
                    if (IgnoreData.Contains(data[i])) continue;

                    if (IgnoreData.Contains(data[i + shift])) continue;
                    
                    
                    if (data[i] == data[i + shift])
                    {
                        PatternKnot knot = new PatternKnot();

                        knot.Element = data[i];
                        knot.Position = i;

                        pat_entry.Add(knot);

                        if (pat_entry.Count == 1)
                        {
                            pat_entry.TimeLine.Add(i);
                            pat_entry.TimeLine.Add(i + shift);
                        }
                    }
                }

                if (pat_entry.Count > 1) // pattern should have more than one knot
                {
                    for (int j = 0; j < pat_entry.Count; j++) pat_entry[j].Position -= pat_entry.TimeLine[0];

                    pat_table.AddPatternEntry(pat_entry);
                }
            }


            // check for duplicates among the PatternEntries and remove them

            List<int> remove_key = new List<int>();
            HashSet<int> checked_key = new HashSet<int>();

            foreach (var pat_entryA in pat_table)
            {
                checked_key.Add(pat_entryA.Key);

                foreach (var pat_entryB in pat_table)
                {
                    if (checked_key.Contains(pat_entryB.Key)) continue;

                    
                    int identical_knots = 0;

                    if (pat_entryA.Value.Count == pat_entryB.Value.Count)
                    {
                        for (int i = 0; i < pat_entryA.Value.Count; i++)
                            if (pat_entryA.Value[i].Element == pat_entryB.Value[i].Element)
                                if (pat_entryA.Value[i].Position == pat_entryB.Value[i].Position)
                                    identical_knots++;
                    }

                    if (identical_knots == pat_entryA.Value.Count)
                    {
                        for (int i = 0; i < pat_entryB.Value.TimeLine.Count; i++)
                            if (!pat_table[pat_entryA.Key].TimeLine.Contains(pat_entryB.Value.TimeLine[i]))
                                pat_table[pat_entryA.Key].TimeLine.Add(pat_entryB.Value.TimeLine[i]);

                        pat_table[pat_entryA.Key].TimeLine.Sort();

                        remove_key.Add(pat_entryB.Key);
                    }
                }
            }


            foreach (int key in remove_key)
            {
                pat_table.Remove(key);
            }


            // WORK HERE: build inheritance tree

            return pat_table;            
        }
        #endregion
    }
}
