using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Patternson
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        byte[] sourceData;

        PatternRecognition patternRecognition;

        PatternTable foundPatterns;

        ObservableCollection<int> foundPatternsCollection;

        FlowDocument foundPatternsFlowDoc;

        int patternOccurIndex;


        /// <summary>
        /// Methods of MainWindow class
        /// </summary>
        public MainWindow()
        {
            patternRecognition = new PatternRecognition();

            foundPatterns = null;

            foundPatternsCollection = new ObservableCollection<int>();

            foundPatternsFlowDoc = null;

            patternOccurIndex = 0;

            InitializeComponent();

            FoundPatternsListBox.DataContext = foundPatternsCollection;
        }

        /// <summary>
        /// EndProgramButton_Click()
        /// </summary>
        private void EndProgramButton_Click(object sender, RoutedEventArgs e)
        {
            App.Current.Shutdown();
        }

        /// <summary>
        /// OpenSourceFileButton_Click()
        /// </summary>
        private void OpenSourceFileButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                var fileName = openFileDialog.FileName;

                var fileNameWithoutPath = fileName.Substring(openFileDialog.FileName.LastIndexOf('\\') + 1);

                OpenSourceFileButton.Content = fileNameWithoutPath;


                using (FileStream fs = File.OpenRead(fileName))
                {
                    sourceData = new byte[fs.Length];

                    fs.Read(sourceData, 0, (int)fs.Length);
                }


                foundPatternsFlowDoc = new FlowDocument();

                var tr = new TextRange(
                    foundPatternsFlowDoc.ContentStart,
                    foundPatternsFlowDoc.ContentEnd);

                using (FileStream fs = File.OpenRead(fileName))
                {
                    tr.Load(fs, DataFormats.Text);
                }

                FoundPatternsFlowDocScrollViewer.Document = foundPatternsFlowDoc;

                if (foundPatterns != null) foundPatterns.Clear();

                if (foundPatternsCollection != null) foundPatternsCollection.Clear();

                NextPatternOccurrenceButton.Content = "Next";

                PatternCountLabel.Content = "Pattern count: -";

                PatternTextBlock.Text = "";
            }
        }

        /// <summary>
        /// OpenResultFileButton_Click()
        /// </summary>
        private void OpenResultFileButton_Click(object sender, RoutedEventArgs e)
        {
            if (foundPatterns == null)
            {
                MessageBox.Show("No patterns to save.");
                return;
            }

            var saveFileDialog = new SaveFileDialog();
            if (saveFileDialog.ShowDialog() == true)
            {
                var fileName = saveFileDialog.FileName;

                var fileNameWithoutPath = fileName.Substring(saveFileDialog.FileName.LastIndexOf('\\') + 1);

                OpenResultFileButton.Content = fileNameWithoutPath;


                StringBuilder output = new StringBuilder();

                output.Clear();
                output.Append("Patterns:\n\n");

                output.Append(foundPatterns.AsText());

                File.WriteAllText(saveFileDialog.FileName, output.ToString());
            }
        }

        /// <summary>
        /// SearchPatternsButton_Click()
        /// </summary>
        private void SearchPatternsButton_Click(object sender, RoutedEventArgs e)
        {
            if (sourceData == null)
            {
                MessageBox.Show("No source file selected or file empty.");
                return;
            }

            if (sourceData.Length > 0)
            {
                foundPatterns = patternRecognition.SearchPattern(sourceData);

                if (foundPatterns != null)
                {
                    var foundPatFreqList = new List<PatternFrequency>();

                    foreach (KeyValuePair<int, PatternHistory> pat in foundPatterns)
                    {
                        var patFreq = new PatternFrequency();

                        patFreq.PatId = pat.Key;
                        patFreq.OccurCount = pat.Value.TimeLine.Count;

                        foundPatFreqList.Add(patFreq);
                    }

                    foundPatFreqList = foundPatFreqList.OrderByDescending(fq => fq.OccurCount).ToList<PatternFrequency>();

                    foundPatternsCollection.Clear();

                    foreach (PatternFrequency patFreq in foundPatFreqList)
                        foundPatternsCollection.Add(patFreq.PatId);

                    PatternCountLabel.Content = "Pattern count: " + foundPatterns.Count;
                }
                else
                    MessageBox.Show("No patterns found.");
            }
            else
            {
                MessageBox.Show("Source file empty.");
            }
        }

        /// <summary>
        /// UpdateFoundPatternsFlowDoc()
        /// </summary>

        private FlowDocument UpdateFoundPatternsFlowDoc()
        {
            int id = (int)FoundPatternsListBox.SelectedValue;

            var para = new Paragraph();

            var sb = new StringBuilder();

            int timePos = foundPatterns[id].TimeLine[patternOccurIndex];

            int start = 0;

            int i = 0;


            foreach (PatternKnot knot in foundPatterns[id])
            {
                for (i = start; i < (timePos + knot.Position); i++)
                {
                    sb.Append((char)sourceData[i]);
                }

                if (sb.Length > 0)
                {
                    para.Inlines.Add(new Run(sb.ToString()));
                    sb.Clear();
                }

                sb.Append((char)sourceData[i]);
                var knotLetter = new Run(sb.ToString());
                knotLetter.Background = new SolidColorBrush(Colors.LightBlue);
                para.Inlines.Add(knotLetter);
                sb.Clear();

                start = (timePos + knot.Position) + 1;
            }


            for (i = start; i < sourceData.Length; i++)
            {
                sb.Append((char)sourceData[i]);
            }

            if (sb.Length > 0)
            {
                para.Inlines.Add(new Run(sb.ToString()));
                sb.Clear();
            }


            var fd = new FlowDocument();

            fd.Blocks.Add(para);

            return fd;
        }

        /// <summary>
        /// FoundPatternsListBox_SelectionChanged()
        /// </summary>
        private void FoundPatternsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FoundPatternsListBox.SelectedIndex != -1)
            {
                patternOccurIndex = 0;

                int id = (int)FoundPatternsListBox.SelectedValue;

                NextPatternOccurrenceButton.Content = "Next\n" +
                    (patternOccurIndex + 1) +
                    " of " +
                    foundPatterns[id].TimeLine.Count;

                FoundPatternsFlowDocScrollViewer.Document = UpdateFoundPatternsFlowDoc();

                PatternTextBlock.Text = ((Pattern)foundPatterns[id]).AsText();
            }
        }

        /// <summary>
        /// NextPatternOccurrenceButton_Click()
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void NextPatternOccurrenceButton_Click(object sender, RoutedEventArgs e)
        {
            if (FoundPatternsListBox.SelectedIndex != -1)
            {
                int id = (int)FoundPatternsListBox.SelectedValue;

                if (patternOccurIndex < (foundPatterns[id].TimeLine.Count - 1))
                    patternOccurIndex++;
                else
                    patternOccurIndex = 0;

                NextPatternOccurrenceButton.Content = "Next\n" +
                    (patternOccurIndex + 1) +
                    " of " +
                    foundPatterns[id].TimeLine.Count;

                FoundPatternsFlowDocScrollViewer.Document = UpdateFoundPatternsFlowDoc();
            }
        }

        private void AskForPatternTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (foundPatterns == null) return;


            string question = AskForPatternTextBox.Text;

            var matchingPatterns = new List<int[]>();
            var assignedToPattern = new HashSet<int>();

            bool questionAnalysisInProgress = true;

            do
            {
                int maxPatId = -1;
                int maxCoincPat = 0;
                int maxStartPat = 0;

                foreach (KeyValuePair<int, PatternHistory> pat in foundPatterns)
                {
                    int maxCoinc = 0;
                    int maxStart = 0;

                    for (int start = 0; start < question.Length; start++)
                    {
                        int numCoinc = 0;

                        bool mismatchWithQuestion = false;
                        bool fillsEmptySpotsInQuestion = false;

                        foreach (PatternKnot knot in pat.Value)
                        {
                            int index = knot.Position + start;

                            if (index >= question.Length) continue;

                            if (knot.Element == question[index])
                            {
                                numCoinc++;
                                if (!assignedToPattern.Contains(index)) fillsEmptySpotsInQuestion = true;
                            }
                            else
                            {
                                mismatchWithQuestion = true;
                                break;
                            }
                        }

                        if (mismatchWithQuestion) continue;

                        if (!fillsEmptySpotsInQuestion) continue;

                        if (numCoinc > maxCoinc)
                        {
                            maxCoinc = numCoinc;
                            maxStart = start;
                        }
                    }

                    if (maxCoinc > maxCoincPat)
                    {
                        maxCoincPat = maxCoinc;
                        maxStartPat = maxStart;
                        maxPatId = pat.Key;
                    }

                }

                if (maxPatId == -1)
                {
                    if (assignedToPattern.Count == 0)
                    {
                        PatternPredictionTextBlock.Text = "No match with available patterns.";
                        return;
                    }
                    else
                    {
                        questionAnalysisInProgress = false;
                    }
                }
                else
                {
                    matchingPatterns.Add(new int[] { maxPatId, maxStartPat });

                    foreach (PatternKnot knot in foundPatterns[maxPatId])
                        assignedToPattern.Add(maxStartPat + knot.Position);
                }

            } while (questionAnalysisInProgress);



            int answerLength = 0;

            foreach (int[] pat in matchingPatterns)
            {
                int id = pat[0];
                int start = pat[1];

                int lastPos = 0;

                foreach (PatternKnot knot in foundPatterns[id])
                    if (knot.Position > lastPos) lastPos = knot.Position;

                if (answerLength < (lastPos + start + 1)) answerLength = lastPos + start + 1;
            }



            var sb = new StringBuilder();

            for (int i = 0; i < answerLength; i++) sb.Append(' ');

            foreach (int[] pat in matchingPatterns)
            {
                int id = pat[0];
                int start = pat[1];

                foreach (PatternKnot knot in foundPatterns[id])
                    if (sb[start + knot.Position] == ' ') sb[start + knot.Position] = (char)knot.Element;
            }



            for (int i = 0; (i < question.Length) && (i < answerLength); i++) sb[i] = question[i];



            /*

            int i;
            int i0 = 0;
            int timePos = maxStartPat;

            var sb = new StringBuilder();

            foreach (PatternKnot knot in foundPatterns[maxPatId])
            {
                for (i = i0; i < (timePos + knot.Position); i++)
                {
                    if (i < question.Length)
                        sb.Append((char)question[i]);
                    else
                        sb.Append(' ');
                }

                sb.Append((char)knot.Element);
                
                i0 = (timePos + knot.Position) + 1;
            }


            for (i = i0; i < question.Length; i++)
            {
                sb.Append((char)question[i]);
            }
            
            */



            PatternPredictionTextBlock.Text = sb.ToString();



            // WORK HERE
        }
        
    }
}
