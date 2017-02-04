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
        public int Source;
    }


    /// <summary>
    /// PatternInterval
    /// </summary>
    public class PatternInterval
    {
        public byte A;
        public byte B;
        public int Interval;
        public int PositionOfA;
    }


    /// <summary>
    /// Pattern
    /// </summary>
    public class Pattern : List<PatternKnot>
    {
        public int ContainsIntervalsOf(Pattern pat)
        {
            if (pat.Count == 0) return 0;

            PatternIntervalIterator iteratorA = new PatternIntervalIterator(this);
            PatternIntervalIterator iteratorB = new PatternIntervalIterator(pat);

            PatternInterval intervalA;
            PatternInterval intervalB;

            int shared_intervals = 0;

            while ((intervalB = iteratorB.NextInterval()) != null)
            {
                Boolean intervalB_present_in_patA = false;

                while (((intervalA = iteratorA.NextInterval()) != null) && (intervalB_present_in_patA != true))
                {
                    if (intervalA.A == intervalB.A)
                        if (intervalA.B == intervalB.B)
                            if (intervalA.Interval == intervalB.Interval)
                                intervalB_present_in_patA = true;
                }

                iteratorA.Reset();

                if (intervalB_present_in_patA) shared_intervals++;
            }

            return shared_intervals;
        }

        public Boolean Add(int new_position, byte new_element)
        {
            foreach (PatternKnot knot in this)
                if ((knot.Position == new_position) && (knot.Element == new_element)) return false;


            PatternKnot new_knot = new PatternKnot();

            new_knot.Position = new_position;
            new_knot.Element = new_element;

            this.Add(new_knot);

            return true;
        }

        public Pattern MakeCopy()
        {
            Pattern target = new Pattern();

            foreach (PatternKnot knot in this) target.Add(knot.Position, knot.Element);

            return target;
        }

        public String AsText()
        {
            StringBuilder text = new StringBuilder();

            text.Clear();
            
            foreach (PatternKnot knot in this)
            {
                text.Append("(" + knot.Source + "," + knot.Position + ")" + (char)knot.Element + " ");
            }

            return text.ToString().TrimEnd(' ');
        }

    }


    /// <summary>
    /// PatternHistory
    /// </summary>
    public class PatternHistory : Pattern
    {
        public List<int> TimeLine { get; set; } = new List<int>();


        public new String AsText()
        {
            StringBuilder text = new StringBuilder();

            text.Clear();

            text.Append(base.AsText() + "\n");

            text.Append("t: ");

            for (int i = 0; i < this.TimeLine.Count; i++)
            {
                text.Append(this.TimeLine[i] + " ");
            }

            return text.ToString().TrimEnd(' ');
        }

    }


    /// <summary>
    /// PatternTable
    /// </summary>
    public class PatternTable : Dictionary<int, PatternHistory> // make singleton?
    {
        public int AddPatternEntry(PatternHistory patEntry)
        {
            int patId = this.Count + 1;

            if (this.ContainsKey(patId)) return -1;

            this.Add(patId, patEntry);

            return patId;
        }

            
        public bool AddTimePoint(int patId, int timePoint)
        {
            if (this.ContainsKey(patId) == false) return false;

            if (this[patId].TimeLine.Contains(timePoint)) return false;
                
            this[patId].TimeLine.Add(timePoint);
            this[patId].TimeLine.Sort();
                        
            return true;
        }

        public string AsText()
        {
            var sb = new StringBuilder();

            sb.Clear();

            foreach (KeyValuePair<int, PatternHistory> pat in this)
            {
                sb.Append("ID: " + pat.Key + "\n" + pat.Value.AsText() + "\n\n");
            }

            return sb.ToString().TrimEnd('\n');
        }
    }


    /// <summary>
    /// PatternIntervalIterator
    /// </summary>
    public class PatternIntervalIterator
    {
        private int current_i = 0;
        private int current_j = 0;

        private Pattern pat;


        public PatternIntervalIterator(Pattern pat)
        {
            this.pat = pat;
        }

        public PatternInterval NextInterval()
        {
            PatternInterval next_interval = new PatternInterval();

            current_j++;

            if (current_j > (pat.Count - 1))
            {
                current_i++;

                if (current_i > (pat.Count - 1)) return null;

                current_j = current_i + 1;

                if (current_j > (pat.Count - 1)) return null;
            }

            PatternKnot knotA = pat[current_i];
            PatternKnot knotB = pat[current_j];

            int interval = knotB.Position - knotA.Position;

            if (interval >= 0)
            {
                next_interval.A = knotA.Element;
                next_interval.B = knotB.Element;
                next_interval.Interval = interval;
                next_interval.PositionOfA = knotA.Position;
            }
            else
            {
                next_interval.A = knotB.Element;
                next_interval.B = knotA.Element;
                next_interval.Interval = interval * (-1);
                next_interval.PositionOfA = knotB.Position;
            }

            return next_interval;
        }

        public void Reset()
        {
            current_i = 0;
            current_j = 0;
        }
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
        
        /// <summary>
        /// SearchPattern(byte[] dataA, byte[] dataB)
        /// </summary>
        /// <param name="dataA"></param>
        /// <param name="dataB"></param>
        
        public PatternTable SearchPattern(byte[] dataA, byte[] dataB)
        {
            var dataC = new byte[dataA.Length + dataB.Length];

            dataA.CopyTo(dataC, 0);
            dataB.CopyTo(dataC, dataA.Length);

            var patTable = SearchPattern(dataC);


            var patKeyToDelete = new HashSet<int>();

            
            for (int i = 0; i < patTable.Count; i++)
            {
                // assign each knot of the pattern at minTime to a certain source

                var id = patTable.ToList()[i].Key;
                var pat = patTable.ToList()[i].Value;

                var minTime = pat.TimeLine.Min();

                foreach (PatternKnot knot in pat)
                {
                    int absPos = knot.Position + minTime;

                    knot.Source = (absPos < dataA.Length) ? 0 : 1; // 0 => dataA, 1 => dataB
                }

                
                // each knot of a pattern should be part of the same source at any time
                // if not the knot will be deleted 

                foreach (int time in pat.TimeLine)
                {
                    var knotsToDelete = new List<PatternKnot>();

                    foreach (PatternKnot knot in pat)
                    {
                        int absPos = knot.Position + time;

                        int src = (absPos < dataA.Length) ? 0 : 1; // 0 => dataA, 1 => dataB
                        
                        if (knot.Source != src) knotsToDelete.Add(knot);
                    }                  

                    foreach (PatternKnot knot in knotsToDelete) pat.Remove(knot);

                }

                // patTable[id] = pat; ... not necessary, pat is a reference to patTable[id]

                if (pat.Count < 2) patKeyToDelete.Add(id);

            }

            foreach (int key in patKeyToDelete) patTable.Remove(key); 

            return patTable;
        }
        
        
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

            foreach (KeyValuePair<int, PatternHistory> pat_entryA in pat_table)
            {
                if (!checked_key.Add(pat_entryA.Key)) continue;

                foreach (KeyValuePair<int, PatternHistory> pat_entryB in pat_table)
                {
                    if (checked_key.Contains(pat_entryB.Key)) continue;

                    if (pat_entryA.Key == pat_entryB.Key) continue;


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

    }
}
