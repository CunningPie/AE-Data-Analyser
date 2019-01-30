using AEDataAnalyzer.Correlation;
using AEDataAnalyzer.User_Interface;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AEDataAnalyzer
{
    public partial class MainView : Form
    {
        DataAnalysis Analyser;
        List<Wave> Waves;
        DataGridView DataGrid;
        TabControl Tabs;
        RFunctions RFunc;

        List<Dictionary<KeyValuePair<Wave, Wave>, double>> CorrelationResults;
        List<double> Thresholds;

        string CurrentFileName;

        public MainView()
        {
            InitializeComponent();

            RFunc = new RFunctions();
            CorrelationResults = new List<Dictionary<KeyValuePair<Wave, Wave>, double>>();
            Thresholds = new List<double>();
        }

        public void ConstructTable(ref DataGridView DataGrid, Dictionary<string, int> Columns)
        {
            var Number = new DataGridViewColumn();
            Number.HeaderText = "№";
            Number.ReadOnly = true;
            Number.Frozen = true;
            Number.CellTemplate = new DataGridViewTextBoxCell();

            DataGrid.Columns.Add(Number);

            List<string> Titles = (from KeyValuePair<string, int> pair in Columns orderby pair.Value select pair.Key).ToList();

            foreach (string title in Titles)
            {
                var column = new DataGridViewColumn();
                column.HeaderText = title;
                column.ReadOnly = true;
                column.Frozen = true;
                column.CellTemplate = new DataGridViewTextBoxCell();

                DataGrid.Columns.Add(column);
            }

        }

        private void Menu_File_Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                this.Text = ofd.FileName;
                CurrentFileName = Path.GetFileNameWithoutExtension(ofd.FileName);

                if (Tabs == null)
                    Tabs = new TabControl();

                TabPage page = new TabPage(CurrentFileName);

                DataGrid = new DataGridView();
                DataGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                DataGrid.Dock = DockStyle.Fill;

                Analyser = new DataAnalysis(ofd.FileName);

                ConstructTable(ref DataGrid, Analyser.Columns);

                int i = 1;

                foreach (SensorInfo si in Analyser.Data)
                {
                    var ColumnsData = new List<string>();
                    ColumnsData.Add((i++).ToString());
                    ColumnsData.AddRange(si.Params);

                    DataGrid.Rows.Add(ColumnsData.ToArray());
                }

                page.Controls.Add(DataGrid);

                Tabs.Controls.Add(page);
                Tabs.Dock = DockStyle.Fill;
                Tabs.Anchor = AnchorStyles.Top | AnchorStyles.Left;

                ContextMenu contextMenu = new ContextMenu();
                contextMenu.MenuItems.Add(new MenuItem("Закрыть вкладку", ContextMenu_Close_Click));
                contextMenu.MenuItems.Add("-");
                contextMenu.MenuItems.Add(new MenuItem("Поиск волн", Menu_Tools_FindWaves_Click));
                contextMenu.MenuItems.Add(new MenuItem("Построить график", Menu_Tools_Plot_Click));
                contextMenu.MenuItems.Add(new MenuItem("Корреляция", Menu_Tools_Correlation_Click));

                Tabs.ContextMenu = contextMenu;

                this.Controls.Add(Tabs);
                Tabs.BringToFront();

                Menu_Tools_Plot.Enabled = false;
                Menu_Tools_Correlation.Enabled = false;

                Menu_Tools_FindWaves.Enabled = true;

                CorrelationResults.Clear();
                Thresholds.Clear();
            }
        }

        private void ContextMenu_Close_Click(object sender, EventArgs e)
        {
            Tabs.Controls.Remove(Tabs.SelectedTab);
        }

        private void Menu_Tools_FindWaves_Click(object sender, EventArgs e)
        {
            if (Analyser != null && Analyser.Data.Count > 0)
            {
                Waves = new List<Wave>();
                int wave_num = 0;

                for (int i = 0; i < Analyser.Data.Count; i++)
                {
                    if (Analyser.Data[i].SensorType == "LE")
                    {
                        var NewWave = new List<SensorInfo>();

                        NewWave.Add(Analyser.Data[i++]);

                        while (i < Analyser.Data.Count() && Analyser.Data[i].SensorType == "Ht" )//Analyser.Data[i].SensorType != "LE" && (double)(Analyser.Data[i].Time.Seconds - NewWave[0].Time.Seconds) * 1000 + Analyser.Data[i].MSec - NewWave[0].MSec < 1000)
                            NewWave.Add(Analyser.Data[i++]);

                        Waves.Add(new Wave(NewWave, wave_num++));
                        --i;
                    }
                }

                DataGridView WavesDataGrid = new DataGridView();
                WavesDataGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                WavesDataGrid.Dock = DockStyle.Fill;

                Dictionary<string, int> Titles = new Dictionary<string, int>() { { "Id", 1 }, { "Channel", 2 }, { "Time", 3 }, { "MSec", 4 }, { "Amplitude", 5 }, { "Energy", 6 }, { "Duration", 7 } };
                ConstructTable(ref WavesDataGrid, Titles);

                int k = 1;

                foreach (Wave w in Waves)
                {

                    foreach (SensorInfo si in w.Events)
                    {
                        var ColumnsData = new List<string>();
                        ColumnsData.Add((k).ToString());
                        ColumnsData.AddRange(si.Params);

                        WavesDataGrid.Rows.Add(ColumnsData.ToArray());
                    }

                    k++;
                }

                TabPage page = new TabPage(CurrentFileName + "_Waves");
                page.Controls.Add(WavesDataGrid);
                Tabs.Controls.Add(page);

                Menu_Tools_Correlation.Enabled = true;
            }
        }

        private void Menu_Tools_Plot_Click(object sender, EventArgs e)
        {
            PlotAttributes pa = new PlotAttributes();

            if (pa.ShowDialog() == DialogResult.OK)
            {
                if (Waves != null)
                {
                    string DirectoryName = "F:/Научная Статья/Графики/Waves_" + CurrentFileName;
                    if (!Directory.Exists(DirectoryName))
                        Directory.CreateDirectory(DirectoryName);
                    else
                    {
                        DirectoryName = DirectoryName + "_" + DateTime.Now.TimeOfDay.ToString().Replace(":", "-");
                        Directory.CreateDirectory(DirectoryName);
                    }

                    for (int i = 0; i < Waves.Count(); i++)
                    {
                        int threshold_num = 0;

                        foreach (var coeffs in CorrelationResults)
                        {
                            List<Wave> CorrelatedWaves = (from KeyValuePair<KeyValuePair<Wave, Wave>, double> pair in coeffs where (Waves.FindIndex(w => w == pair.Key.Key) == i && pair.Value >= Thresholds[threshold_num]) select pair.Key.Value).ToList();

                            if (CorrelatedWaves.Count() > 1)
                            {
                                foreach (string Param in pa.Params)
                                {
                                    Directory.CreateDirectory(DirectoryName + "/" + Param);

                                    RFunc.PlotWavesCollection(CorrelatedWaves, DirectoryName + "/" + Param + "/Wave" + Param + i + ".png", Param);

                                    TabPage page = new TabPage("Wave" + Param + i);

                                    PictureBox Plot = new PictureBox();
                                    Plot.Load(DirectoryName + "/" + Param + "/Wave" + Param + i + ".png");
                                    Plot.Dock = DockStyle.Fill;
                                    Plot.SizeMode = PictureBoxSizeMode.Zoom;
                                    page.Controls.Add(Plot);
                                    Tabs.Controls.Add(page);
                                }
                            }

                            threshold_num++;
                        }
                    }
                }
            }
        }

        private void Menu_Tools_Correlation_Click(object sender, EventArgs e)
        {
            CorrelationOptions co = new CorrelationOptions();

            if (co.ShowDialog() == DialogResult.OK)
            {
                CorrelationResults = CorrelationFunction(Waves, co.Params, co.Op, co.CorrelationTypes);

                string FileNameModifier = "";

                foreach (string s in co.Params)
                    FileNameModifier += s;

                foreach (string s in co.CorrelationTypes)
                    FileNameModifier += s;

                FileNameModifier += co.Op;

                ConstructCorrelationPairTable(FileNameModifier);
                ConstructCorrelationMatrix(FileNameModifier);
            }
        }

        private void ConstructCorrelationPairTable(string FileNameModifier)
        {
            foreach (Dictionary<KeyValuePair<Wave, Wave>, double> CorrTable in CorrelationResults)
            {
                TabPage page = new TabPage(CurrentFileName + "_CorrelationCoeffs" + FileNameModifier);
                DataGridView dataGrid = new DataGridView();
                dataGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                dataGrid.Dock = DockStyle.Fill;

                ConstructTable(ref dataGrid, new Dictionary<string, int>() { { "Пара", 1 }, { "Коэффициент", 2 } });

                int k = 1;

                foreach (KeyValuePair<KeyValuePair<Wave, Wave>, double> pair in CorrTable)
                {
                    int i = Waves.FindIndex(w => w == pair.Key.Key), j = Waves.FindIndex(w => w == pair.Key.Value);

                    dataGrid.Rows.Add(k++, i + ", " + j, Math.Round(pair.Value, 4));
                }

                page.Controls.Add(dataGrid);
                Tabs.Controls.Add(page);

                Menu_Tools_Plot.Enabled = true;
            }
        }

        private void ConstructCorrelationMatrix(string FileNameModifier)
        {
            foreach (Dictionary<KeyValuePair<Wave, Wave>, double> CorrTable in CorrelationResults)
            {
                TabPage page = new TabPage(CurrentFileName + "_CorrelationCoeffs" + FileNameModifier);
                DataGridView dataGrid = new DataGridView();
                dataGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                dataGrid.Dock = DockStyle.Fill;

                int w_num = 0;

                var Number = new DataGridViewColumn();
                Number.HeaderText = "№";
                Number.ReadOnly = true;
                Number.Frozen = true;
                Number.CellTemplate = new DataGridViewTextBoxCell();

                dataGrid.Columns.Add(Number);

                foreach (Wave w in Waves)
                {
                    var WaveColumn = new DataGridViewColumn();
                    WaveColumn.HeaderText = w_num.ToString();
                    WaveColumn.ReadOnly = true;
                    WaveColumn.Frozen = true;
                    WaveColumn.CellTemplate = new DataGridViewTextBoxCell();

                    dataGrid.Columns.Add(WaveColumn);
                    w_num++;
                }

                for (int i = 0; i < Waves.Count(); i++)
                {
                    double[] coeffs = (from KeyValuePair<KeyValuePair<Wave, Wave>, double> pair in CorrTable where pair.Key.Key == Waves[i] select pair.Value).ToArray();

                    DataGridViewRow row = (DataGridViewRow)dataGrid.Rows[0].Clone();

                    int cell = i + 1;
                    row.Cells[0].Value = i;

                    foreach (double d in coeffs)
                    {
                        row.Cells[cell++].Value = Math.Round(d, 4);
                    }

                    dataGrid.Rows.Add(row);
                }

                page.Controls.Add(dataGrid);
                Tabs.Controls.Add(page);

                Menu_Tools_Plot.Enabled = true;
            }
        }

        private List<Dictionary<KeyValuePair<Wave, Wave>, double>> CorrelationFunction(List<Wave> Waves, IEnumerable<String> ParamTypes, string Op, IEnumerable<String> CorrCoeffs)
        {
            var ListCoeffs = new List<Dictionary<KeyValuePair<Wave, Wave>, double>>();
            var ResultList = new List<Dictionary<KeyValuePair<Wave, Wave>, double>>();

            foreach (string Coeff in CorrCoeffs)
            {
                var Result = new Dictionary<KeyValuePair<Wave, Wave>, double>();
                double threshold = 0;

                if (Op == "Mult")
                    threshold = 1;

                foreach (string param in ParamTypes)
                {
                    Dictionary<KeyValuePair<Wave, Wave>, double> CorrelatedCoeffs = new Dictionary<KeyValuePair<Wave, Wave>, double>();

                    List<double> ValuesX = new List<double>(), ValuesY = new List<double>();

                    if (Coeff == "Pearson")
                    {
                        if (Op == "Mult")
                            switch (param)
                            {
                                case "Time":
                                    threshold *= 0.95;
                                    break;
                                case "Amplitude":
                                    threshold *= 0.85;
                                    break;
                                case "Energy":
                                    threshold *= 0.99;
                                    break;
                                default:
                                    threshold *= 0.8;
                                    break;
                            }
                        else if (Op == "Sum")
                            switch (param)
                            {
                                case "Time":
                                    threshold += 0.95;
                                    break;
                                case "Amplitude":
                                    threshold += 0.85;
                                    break;
                                case "Energy":
                                    threshold += 0.99;
                                    break;
                                default:
                                    threshold += 0.8;
                                    break;
                            }
                    }
                    else if (Coeff == "Fechner")
                    {
                        if (Op == "Mult")
                            threshold *= 0.7;
                        else if (Op == "Sum")
                            threshold += 0.7;
                    }

                    for (int i = 0; i < Waves.Count(); i++)
                    {
                        switch (param)
                        {
                            case "Time":
                                ValuesX = (from SensorInfo si in Waves[i].Events select si.MSec).ToList();
                                break;
                            case "Energy":
                                ValuesX = (from SensorInfo si in Waves[i].Events select si.Energy).ToList();
                                break;
                            case "Amplitude":
                                ValuesX = (from SensorInfo si in Waves[i].Events select si.Amplitude).ToList();
                                break;
                        }

                        foreach (Wave w in Waves.Skip(i))
                        {
                            var pair = new KeyValuePair<Wave, Wave>(Waves[i], w);

                            Wave wave = w;
                            /*
                            if (Waves[i].Events.Count > w.Events.Count)
                                switch (param)
                                {
                                    case "Time":
                                        ValuesX = (from SensorInfo si in SupportFunctions.PointSelection(w, Waves[i]).Events select si.MSec).ToList();
                                        break;
                                    case "Energy":
                                        ValuesX = (from SensorInfo si in SupportFunctions.PointSelection(w, Waves[i]).Events select si.Energy).ToList();
                                        break;
                                    case "Amplitude":
                                        ValuesX = (from SensorInfo si in SupportFunctions.PointSelection(w, Waves[i]).Events select si.Amplitude).ToList();
                                        break;
                                }
                            else if (Waves[i].Events.Count < w.Events.Count)
                                wave = SupportFunctions.PointSelection(Waves[i], w);
*/
                            switch (param)
                            {
                                case "Time":
                                    ValuesY = (from SensorInfo si in wave.Events select si.MSec).ToList();
                                    break;
                                case "Energy":
                                    ValuesY = (from SensorInfo si in wave.Events select si.Energy).ToList();
                                    break;
                                case "Amplitude":
                                    ValuesY = (from SensorInfo si in wave.Events select si.Amplitude).ToList();
                                    break;
                            }

                            switch (Coeff)
                            {
                                case "Pearson":
                                    CorrelatedCoeffs.Add(pair, PearsonCorrelation.Coefficient(ValuesX, ValuesY));
                                    break;
                                case "Fechner":
                                    CorrelatedCoeffs.Add(pair, FechnerCorrelation.Coefficient(ValuesX, ValuesY));
                                    break;
                            }
                        }
                    }

                    ListCoeffs.Add(CorrelatedCoeffs);
                }

                for (int i = 0; i < Waves.Count(); i++)
                    for (int j = i; j < Waves.Count(); j++)
                    {
                        double r = 0;

                        switch (Op)
                        {
                            case "Mult":
                                r = 1;

                                foreach (var coeffs in ListCoeffs)
                                    r *= coeffs[new KeyValuePair<Wave, Wave>(Waves[i], Waves[j])];

                                Result.Add(new KeyValuePair<Wave, Wave>(Waves[i], Waves[j]), r);
                                break;
                            case "Sum":
                                foreach (var coeffs in ListCoeffs)
                                    r += coeffs[new KeyValuePair<Wave, Wave>(Waves[i], Waves[j])];

                                break;
                        }

                        Result.Add(new KeyValuePair<Wave, Wave>(Waves[i], Waves[j]), r);
                    }

                Thresholds.Add(threshold);
                ResultList.Add(Result);
            }

            return ResultList;
        }
    }
}
