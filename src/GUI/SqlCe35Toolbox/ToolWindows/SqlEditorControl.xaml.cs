﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using EnvDTE;
using ErikEJ.SqlCeToolbox.Dialogs;
using ErikEJ.SqlCeToolbox.Helpers;
using ExecutionPlanVisualizer;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using ICSharpCode.AvalonEdit.Search;
using Microsoft.Win32;

namespace ErikEJ.SqlCeToolbox.ToolWindows
{
    /// <summary>
    /// Interaction logic for SqlEditorControl.xaml
    /// </summary>
    public partial class SqlEditorControl
    {
        public DatabaseInfo DatabaseInfo 
        {
            get
            {
                return _dbInfo;
            }
            set
            {
                if (value != null)
                {
                    _dbInfo = value;
                    _parentWindow.Caption = _dbInfo.Caption;
                }
            }
        } 
        //This property must be set by parent window
        private readonly SqlEditorWindow _parentWindow;
        private DatabaseInfo _dbInfo;
        private string _savedFileName;
        private FontFamily fontFamiliy = new FontFamily("Consolas");
        private double fontSize = 14;
        public DTE Dte { get; private set; }
        private bool _ignoreDdlErrors;
        private bool _showResultInGrid;
        private bool _showBinaryValuesInResult;
        private bool _showNullValuesAsNull;
        private bool _useClassicGrid;

        public SqlEditorControl(SqlEditorWindow parentWindow)
        {
            InitializeComponent();
            _parentWindow = parentWindow;
        }

        //TODO For intellisense
        //public List<string> TableNames { get; set; }
        //public List<Column> Columns { get; set; }

        public ExplorerControl ExplorerControl { get; set; }

        public string SqlText
        {
            get
            {
                return SqlTextBox.Text;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    if (value.Length > 10000)
                        SqlTextBox.SyntaxHighlighting = null;
                    SqlTextBox.Text = value;
                    _isDirty = false;
                    if (value.Length <= 10000 && SqlTextBox.SyntaxHighlighting == null)
                        LoadHighlighter();                    
                    Resultspanel.Children.Clear();
                }
                else
                {
                    SqlTextBox.Clear();
                }
            }
        }

        private bool _isDirty;

        public bool IsDirty
        {
            get
            {
                return _isDirty;
            }
            set
            {
                _isDirty = value;
                if (_isDirty)
                {
                    _parentWindow.Caption = _dbInfo.Caption + "*";
                }
                else
                {
                    _parentWindow.Caption = _dbInfo.Caption;
                }
                if (!string.IsNullOrEmpty(_savedFileName))
                {
                    _parentWindow.Caption = Path.GetFileName(_savedFileName) + " - " + _parentWindow.Caption;
                }
            }
        }

        private void SqlEditorWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var overflowGrid = toolBar1.Template.FindName("OverflowGrid", toolBar1) as FrameworkElement;
                if (overflowGrid != null)
                {
                    overflowGrid.Visibility = Visibility.Collapsed;
                }
                var package = _parentWindow.Package as SqlCeToolboxPackage;
                if (package == null) return;
                Dte = package.GetServiceHelper(typeof(DTE)) as DTE;

                toolBar1.Background = toolTray.Background = VSThemes.GetCommandBackground();
                dock1.Background = VSThemes.GetWindowBackground();
                sep4.Background = VSThemes.GetToolbarSeparatorBackground();
                txtSaveAs.Foreground = VSThemes.GetWindowText();
                if (DatabaseInfo != null)
                    txtVersion.Text = DatabaseInfo.ServerVersion;
                LoadDefaultOptions();
                ConfigureOptions();
                LoadHighlighter();
                //TODO
                //formsHost.Visibility = Visibility.Collapsed;
                SqlTextBox.TextChanged += SqlTextBox_TextChanged;
                //TODO Entry point for Intellisense
                //SqlTextBox.TextArea.TextEntering += SqlTextBox_TextArea_TextEntering;
                //SqlTextBox.TextArea.TextEntered += SqlTextBox_TextArea_TextEntered;

                SqlTextBox.Focus();
            }
            catch (Exception ex)
            {
                DataConnectionHelper.SendError(ex, DatabaseInfo != null ? DatabaseInfo.DatabaseType : DatabaseType.SQLServer);
            }
        }

        private void LoadDefaultOptions()
        {
            _showResultInGrid = Properties.Settings.Default.ShowResultInGrid;
            _showBinaryValuesInResult = Properties.Settings.Default.ShowBinaryValuesInResult;
            _showNullValuesAsNull = Properties.Settings.Default.ShowNullValuesAsNULL;
            _useClassicGrid = Properties.Settings.Default.UseClassicGrid;
        }

        private List<CheckListItem> items = new List<CheckListItem>();

        private void ConfigureOptions()
        {
            items.Clear();

            items.Add(new CheckListItem
                {
                    IsChecked = _showResultInGrid,
                    Label = "Show Result in Grid",
                    Tag = "ShowResultInGrid"
                });            
            items.Add(new CheckListItem
                {
                    IsChecked = _showBinaryValuesInResult,
                    Label = "Show Binary Values in Result",
                    Tag = "ShowBinaryValuesInResult"
                });

            items.Add(new CheckListItem
            {
                IsChecked = _showNullValuesAsNull,
                Label = "Show null Values as NULL",
                Tag = "ShowNullValuesAsNULL"
            });
            items.Add(new CheckListItem
            {
                IsChecked = _ignoreDdlErrors,
                Label = "Ignore DDL Errors",
                Tag = "_ignoreDdlErrors"
            });
            items.Add(new CheckListItem
            {
                IsChecked = _useClassicGrid,
                Label = "Use classic (plain) grid",
                Tag = "_useClassicGrid"
            });
            chkOptions.ItemsSource = null;
            chkOptions.ItemsSource = items;
        }

        private void chkOptions_ItemSelectionChanged(object sender, Xceed.Wpf.Toolkit.Primitives.ItemSelectionChangedEventArgs e)
        {
            var item = e.Item as CheckListItem;
            if (item != null)
            {
                switch (item.Tag)
                {
                    case "_ignoreDdlErrors":
                        _ignoreDdlErrors = item.IsChecked;
                        break;
                    case "ShowResultInGrid":
                        _showResultInGrid = item.IsChecked;
                        break;
                    case "ShowBinaryValuesInResult":
                        _showBinaryValuesInResult = item.IsChecked;
                        break;
                    case "ShowNullValuesAsNULL":
                        _showNullValuesAsNull = item.IsChecked;
                        break;
                    case "_useClassicGrid":
                        _useClassicGrid = item.IsChecked;
                        break;
                }
            }
        }

        private void ddButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigureOptions();
        }

        void SqlTextBox_TextChanged(object sender, EventArgs e)
        {
            IsDirty = true;
        }

        private void LoadHighlighter()
        {
            MemoryStream ms = null;
            try
            {
                ms = new MemoryStream(SqlCeToolbox.Resources.SqlCeSyntax);
                SqlTextBox.SyntaxHighlighting = HighlightingLoader.Load(new XmlTextReader(ms),
                HighlightingManager.Instance);
            }
            finally
            {
                if (ms != null)
                    ms.Dispose();
            }
        }

        private void SqlEditorControl_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (DatabaseInfo != null && DatabaseInfo.DatabaseType == DatabaseType.SQLite)
            {
                ParseButton.Visibility = Visibility.Collapsed;
                ExecuteWithPlanButton.Visibility = Visibility.Collapsed;
            }
        }

        #region Toolbar Button events

        private void NewButton_Click(object sender, RoutedEventArgs e)
        {
            DataConnectionHelper.LogUsage("EditorNew");
            OpenSqlEditorToolWindow();
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            DataConnectionHelper.LogUsage("EditorOpen");
            OpenScript();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            DataConnectionHelper.LogUsage("EditorSave");
            SaveScript(false);
        }

        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            DataConnectionHelper.LogUsage("EditorSave");
            SaveScript(true);
        }

        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            ExecuteScript();
        }

        public void ExecuteScript()
        {
            if (string.IsNullOrWhiteSpace(SqlText))
                return;
            DataConnectionHelper.LogUsage("EditorExecute");
            ExecuteSqlScriptInEditor();
        }

        public static RoutedCommand ExecuteCommand = new RoutedCommand();
        public void ExecutedExecuteCommand(object sender, ExecutedRoutedEventArgs e)
        {
            ExecuteScript();
        }

        private void ExecuteWithPlanButton_Click(object sender, RoutedEventArgs e)
        {
            DataConnectionHelper.LogUsage("EditorExecuteWithPlan");
            if (string.IsNullOrWhiteSpace(SqlText))
                return;
            try
            {
                using (var repository = DataConnectionHelper.CreateRepository(DatabaseInfo))
                {
                    var sql = GetSqlFromSqlEditorTextBox();
                    string showPlan;
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    var dataset = repository.ExecuteSql(sql, out showPlan);
                    sw.Stop();
                    FormatTime(sw);
                    if (dataset != null)
                    {
                        ParseDataSetResultsToResultsBox(dataset);
                    }
                    try
                    {
                        TryLaunchSqlplan(showPlan);
                    }
                    catch (Win32Exception)
                    {
                        EnvDTEHelper.ShowError("This feature requires Visual Studio 2010 Premium / SQL Server Management Studio to be installed");
                    }
                    catch (Exception ex)
                    {
                        DataConnectionHelper.SendError(ex, DatabaseType.SQLCE35);
                    }

                }
            }
            catch (Exception ex)
            {
                ParseSqlErrorToResultsBox(DataConnectionHelper.CreateEngineHelper(DatabaseInfo.DatabaseType).FormatError(ex));
            }
        }

        private void TryLaunchSqlplan(string showPlan)
        {
            if (!string.IsNullOrWhiteSpace(showPlan))
            {
                if (DatabaseInfo.DatabaseType == DatabaseType.SQLite)
                {
                    var textBox = new TextBox
                    {
                        FontFamily = fontFamiliy,
                        FontSize = fontSize,
                        Text = showPlan
                    };
                    ClearResults();
                    Resultspanel.Children.Add(textBox);
                    tab1.Visibility = Visibility.Collapsed;
                    resultsTabControl.SelectedIndex = 1;
                }
                else
                {
                    PlanPanel.Children.Clear();
                    var formsHost = new WindowsFormsHost();
                    formsHost.Child = new QueryPlanUserControl();
                    PlanPanel.Children.Add(formsHost);

                    var qpControl = (QueryPlanUserControl) formsHost.Child;
                    if (qpControl != null)
                    {
                        var planHtml = QueryPlanVisualizer.BuildPlanHtmml(showPlan);
                        qpControl.DisplayExecutionPlanDetails(showPlan, planHtml);
                    }

                    //var fileName = System.IO.Path.GetTempFileName();
                    //fileName = fileName + ".sqlplan";
                    //System.IO.File.WriteAllText(fileName, showPlan);
                    //// If Data Dude is available
                    //var pkg = _parentWindow.Package as SqlCeToolboxPackage;
                    //if (pkg.VSSupportsSqlPlan())
                    //{
                    //    _dte.ItemOperations.OpenFile(fileName);
                    //    _dte.ActiveDocument.Activate();
                    //}
                    //else
                    //{
                    //    // Just try to start SSMS
                    //    using (RegistryKey rkRoot = Registry.ClassesRoot)
                    //    {
                    //        RegistryKey rkFileType = rkRoot.OpenSubKey(".sqlplan");
                    //        if (rkFileType != null)
                    //        {
                    //            System.Diagnostics.Process.Start(fileName);
                    //        }
                    //        else
                    //        {
                    //            EnvDTEHelper.ShowError("No application that can open .sqlplan files is installed, you could install SSMS 2012 SP1 Express");
                    //        }
                    //    }
                    //}
                }
            }
        }

        private void ParseButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SqlText))
                return;
            DataConnectionHelper.LogUsage("EditorParse");
            try
            {
                using (var repository = DataConnectionHelper.CreateRepository(DatabaseInfo))
                {
                    var textBox = new TextBox();
                    textBox.FontFamily = fontFamiliy;
                    textBox.FontSize = fontSize;
                    string sql = GetSqlFromSqlEditorTextBox();
                    repository.ParseSql(sql);
                    textBox.Text = "Statement(s) in script parsed and seems OK!";
                    ClearResults();
                    Resultspanel.Children.Add(textBox);
                    tab2.Focus();
                }
            }
            catch (Exception ex)
            {
                ParseSqlErrorToResultsBox(DataConnectionHelper.CreateEngineHelper(DatabaseInfo.DatabaseType).FormatError(ex));
            }
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
#pragma warning disable 618
            SearchPanel sPanel = new SearchPanel();
            if (SqlTextBox != null) sPanel.Attach(SqlTextBox.TextArea);
#pragma warning restore 618
        }

        private void ClearResults()
        {
            tab1.Visibility = Visibility.Visible;
            tab2.Visibility = Visibility.Visible;
            tab2.Header = "Messages";
            GridPanel.Children.Clear();
            Resultspanel.Children.Clear();
            PlanPanel.Children.Clear();
        }

        private void ShowPlanButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SqlText))
                return;
            DataConnectionHelper.LogUsage("EditorShowPlan");
            try
            {
                using (var repository = DataConnectionHelper.CreateRepository(DatabaseInfo))
                {
                    string sql = GetSqlFromSqlEditorTextBox();
                    string showPlan = repository.ParseSql(sql);
                    try
                    {
                        TryLaunchSqlplan(showPlan);
                    }
                    catch (Win32Exception)
                    {
                        EnvDTEHelper.ShowError("This feature requires Visual Studio 2010 Premium / SQL Server Management Studio to be installed");
                    }
                    catch (Exception ex)
                    {
                        DataConnectionHelper.SendError(ex, DatabaseType.SQLCE35);
                    }
                }
            }
            catch (Exception sqlException)
            {
                ParseSqlErrorToResultsBox(DataConnectionHelper.CreateEngineHelper(DatabaseInfo.DatabaseType).FormatError(sqlException));
            }
        }

        #endregion

        private void FormatTime(Stopwatch sw)
        {
            var ts = new TimeSpan(sw.ElapsedTicks);
            txtTime.Text = string.Format("Duration: {0:00}:{1:00}.{2:000}", ts.Minutes, ts.Seconds, ts.Milliseconds);
        }

        public void OpenScript()
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                Filter = "SQL Server Compact Script (*.sqlce;*.sql)|*.sqlce;*.sql|All Files(*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false,
                Title = "Select Script to Open"
            };
            if (DatabaseInfo.DatabaseType == DatabaseType.SQLite)
            {
                ofd.Filter = "SQLite Script (*.sql)|*.sql|All Files(*.*)|*.*";
            }
            if (ofd.ShowDialog() != true) return;
            SqlText = File.ReadAllText(ofd.FileName);
            _savedFileName = ofd.FileName;
            IsDirty = false;
        }

        public void SaveScript(bool promptForName)
        {
            if (promptForName || string.IsNullOrEmpty(_savedFileName))
            {
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Filter = "SQL Server Compact Script (*.sqlce;*.sql)|*.sqlce;*.sql|All Files(*.*)|*.*";
                if (DatabaseInfo.DatabaseType == DatabaseType.SQLite)
                {
                    sfd.Filter = "SQLite Script (*.sql)|*.sql|All Files(*.*)|*.*";
                }
                sfd.ValidateNames = true;
                sfd.Title = "Save script as";
                if (sfd.ShowDialog() == true)
                {
                    File.WriteAllText(sfd.FileName, SqlText, Encoding.UTF8);
                    _savedFileName = sfd.FileName;
                    IsDirty = false;
                }
            }
            else
            {
                File.WriteAllText(_savedFileName, SqlText, Encoding.UTF8);
                IsDirty = false;
            }
        }

        private void ExecuteSqlScriptInEditor()
        {
            try
            {
                using (var repository = DataConnectionHelper.CreateRepository(DatabaseInfo))
                {
                    var sql = GetSqlFromSqlEditorTextBox();
                    bool schemaChanged;
                    if (sql.Length == 0) return;
                    sql = sql.Replace("\r", " \r");
                    sql = sql.Replace("GO  \r", "GO\r");
                    sql = sql.Replace("GO \r", "GO\r");
                    var sw = new Stopwatch();
                    sw.Start();
                    var dataset = repository.ExecuteSql(sql, out schemaChanged, _ignoreDdlErrors);
                    sw.Stop();
                    FormatTime(sw);
                    if (dataset == null) return;
                    ParseDataSetResultsToResultsBox(dataset);
                    if (!schemaChanged) return;
                    if (ExplorerControl != null)
                    {
                        ExplorerControl.RefreshTables(DatabaseInfo);
                    }
                }
            }
            catch (Exception sqlException)
            {
                ParseSqlErrorToResultsBox(DataConnectionHelper.CreateEngineHelper(DatabaseInfo.DatabaseType).FormatError(sqlException));
            }
        }

        private string GetSqlFromSqlEditorTextBox()
        {
            var sql = SqlText.Trim();
            if (!string.IsNullOrWhiteSpace(SqlTextBox.SelectedText))
            {
                sql = SqlTextBox.SelectedText;
            }

            if (!sql.EndsWith("\r\nGO"))
                sql = sql + "\r\nGO";
            return sql;
        }

        private void ParseSqlErrorToResultsBox(string sqlException)
        {
            ClearResults();
            var textBox = new TextBox();
            textBox.Foreground = Brushes.Red;
            textBox.FontFamily = fontFamiliy;
            textBox.FontSize = fontSize;
            textBox.Text = sqlException;
            Resultspanel.Children.Add(textBox);
            tab1.Visibility = Visibility.Collapsed;
            resultsTabControl.SelectedIndex = 1;
        }

        private void ParseDataSetResultsToResultsBox(DataSet dataset)
        {
            ClearResults();

            foreach (DataTable table in dataset.Tables)
            {
                txtTime.Text = txtTime.Text + " / " + table.Rows.Count + " rows ";
                var textBox = new TextBox();
                textBox.FontFamily = fontFamiliy;
                textBox.FontSize = fontSize;
                textBox.Foreground = Brushes.Black;
                DockPanel.SetDock(textBox, Dock.Top);
                if (table.Rows.Count == 0)
                {
                    textBox.Text = string.Format("{0} rows affected", table.MinimumCapacity);
                    Resultspanel.Children.Add(textBox);
                    resultsTabControl.SelectedIndex = 1;
                }
                else
                {
                    if (_showResultInGrid)
                    {
                        if (_useClassicGrid)
                        {
                            var grid = BuildPlainGrid(table);
                            DockPanel.SetDock(grid, Dock.Top);
                            GridPanel.Children.Add(grid);
                        }
                        else
                        {
                            var grid = new ExtEditControl();
                            grid.SourceTable = table;
                            DockPanel.SetDock(grid, Dock.Top);
                            GridPanel.Children.Add(grid);
                        }
                        resultsTabControl.SelectedIndex = 0;
                    }
                    else
                    {
                        tab1.Visibility = Visibility.Collapsed;
                        tab2.Header = "Results";
                        resultsTabControl.SelectedIndex = 1;
                        var results = new StringBuilder();
                        foreach (var column in table.Columns)
                        {
                            results.Append(column + "\t");
                        }
                        results.Remove(results.Length - 1, 1);
                        results.Append(Environment.NewLine);

                        foreach (DataRow row in table.Rows)
                        {
                            foreach (var item in row.ItemArray)
                            {
                                if (item == DBNull.Value)
                                {
                                    if (_showNullValuesAsNull)
                                    {
                                        results.Append("NULL\t");
                                    }
                                    else
                                    {
                                        results.Append("\t");
                                    }
                                }
                                //This formatting is optional (causes perf degradation)
                                else if (item.GetType() == typeof(byte[]) && _showBinaryValuesInResult)
                                {
                                    var buffer = (byte[])item;
                                    results.Append("0x");
                                    for (var i = 0; i < buffer.Length; i++)
                                    {
                                        results.Append(buffer[i].ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
                                    }
                                    results.Append("\t");
                                }
                                else if (item is DateTime)
                                {
                                    results.Append(((DateTime)item).ToString("O") + "\t");
                                }
                                else if (item is double || item is float)
                                {
                                    string intString = Convert.ToDouble(item).ToString("R", System.Globalization.CultureInfo.InvariantCulture);
                                    results.Append(intString + "\t");
                                }
                                else
                                {
                                    results.Append(item + "\t");
                                }
                            }
                            results.Remove(results.Length - 1, 1);
                            results.Append(Environment.NewLine);
                        }
                        textBox.Text = results.ToString();
                        Resultspanel.Children.Add(textBox);
                    }
                }
            }

            if (_showResultInGrid && GridPanel.Children.Count > 0)
            {
                resultsTabControl.SelectedIndex = 0;
            }
        }

        private DataGrid BuildPlainGrid(DataTable table)
        {
            var grid = new DataGrid
            {
                AutoGenerateColumns = true,
                IsReadOnly = true,
                FontSize = fontSize,
                FontFamily = fontFamiliy,
                ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader,
                ItemsSource = ((IListSource) table).GetList()
            };
            grid.AutoGeneratingColumn += grid_AutoGeneratingColumn;
            return grid;
        }

        void grid_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {   
            var pos = e.PropertyName.IndexOf("_", StringComparison.Ordinal);   
            if (pos > 0 && e.Column.Header != null)   
            {   
                e.Column.Header = e.Column.Header.ToString().Replace("_", "__");   
            }   
            if (_showNullValuesAsNull)   
            {   
                ((DataGridBoundColumn)e.Column).Binding.TargetNullValue = "NULL";   
            }   
        }  

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (GridPanel.Children.Count > 0)
                {
                    var dataGrid = FindDataGrid();
                    if (dataGrid == null) return;
                    var sfd = new SaveFileDialog();
                    sfd.Filter = "CSV file (*.csv)|*.csv|All Files(*.*)|*.*";
                    sfd.ValidateNames = true;
                    sfd.Title = "Save result as CSV";
                    if (sfd.ShowDialog() == true)
                    {
                        dataGrid.SelectAllCells();
                        dataGrid.ClipboardCopyMode = DataGridClipboardCopyMode.IncludeHeader;
                        ApplicationCommands.Copy.Execute(null, dataGrid);
                        dataGrid.UnselectAllCells();
                        var result = (string)Clipboard.GetData(DataFormats.CommaSeparatedValue);
                        Clipboard.Clear();
                        File.WriteAllText(sfd.FileName, result);
                    }
                    return;
                }
                if (Resultspanel.Children.Count > 0)
                {
                    var textBox = Resultspanel.Children[0] as TextBox;
                    if (textBox == null) return;
                    var sfd = new SaveFileDialog();
                    sfd.Filter = "CSV file (*.csv)|*.csv|All Files(*.*)|*.*";
                    sfd.ValidateNames = true;
                    sfd.Title = "Save result as CSV";
                    if (sfd.ShowDialog() == true)
                    {
                        var separator = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ListSeparator;
                        var result = textBox.Text.Replace("\t", separator);
                        File.WriteAllText(sfd.FileName, result);
                    }
                }
            }
            catch (Exception ex)
            {
                DataConnectionHelper.SendError(ex, DatabaseInfo.DatabaseType);
            }
        }

        private DataGrid FindDataGrid()
        {
            var dataGrid = GridPanel.Children[0] as DataGrid;
            if (dataGrid != null)
            {
                return dataGrid;
            }
            var control = GridPanel.Children[0] as ExtEditControl;
            if (control == null) return null;
            var grid = control.FindName("masterGrid") as Grid;
            if (grid == null) return null;
            return grid.Children[0] as DataGrid;
        }

        public void OpenSqlEditorToolWindow()
        {
            if (DatabaseInfo == null)  return;
            if (ExplorerControl == null) return;

            try
            {
                var pkg = _parentWindow.Package as SqlCeToolboxPackage;
                Debug.Assert(pkg != null, "Package property of the Explorere Tool Window should never be null, have you tried to create it manually and not through FindToolWindow()?");
                var sqlEditorWindow = pkg.CreateWindow<SqlEditorWindow>();
                var control = sqlEditorWindow.Content as SqlEditorControl;
                if (control != null)
                {
                    control.DatabaseInfo = DatabaseInfo;
                    control.ExplorerControl = ExplorerControl;
                }
            }
            catch (Exception ex)
            {
                DataConnectionHelper.SendError(ex, DatabaseInfo.DatabaseType);
            }
        }

        private void Options_Click(object sender, RoutedEventArgs e)
        {
            var package = _parentWindow.Package as SqlCeToolboxPackage;
            if (package == null) return;
            package.ShowOptionPage(typeof(OptionsPageGeneral));
            DataConnectionHelper.LogUsage("ToolbarOptions");
        }
    }
}