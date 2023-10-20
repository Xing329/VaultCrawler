using Autodesk.Connectivity.WebServices;
using Autodesk.Connectivity.WebServicesTools;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;
using File = Autodesk.Connectivity.WebServices.File;

namespace VaultDataCrawlerNF
{
    //public class ObservableStringCollection : ObservableCollection<string> { }
    internal class FileRequestViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private FileRequestModel model = new FileRequestModel();

        public FileRequestViewModel()
        {
            StartDate = DateTime.Now;
            EndDate = DateTime.Now;
            MyVault = AuthWindows.loginWindows();
            FolderProjects = MyVault.DocumentService.GetFoldersByParentId(1, false).Where(folder => folder.CreateUserId != 0).ToArray();
            ConsoleOutputText = "Bitte wählen Sie die richtigen Suchkriterien und geben Sie Suchbegriff ein";

            NumberList = new ObservableCollection<int>();
            for (int i = 1; i <= 20; i++)
            {
                NumberList.Add(i);
            }
            NumberOfFiles = NumberList[0];
            OpenFolderCommand = new RelayCommand(OpenFolder);
            ExitCommand = new RelayCommand(Exit);

            FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "VaultFiles");

            if (!Directory.Exists(FolderPath))
            {
                Directory.CreateDirectory(FolderPath);
            }


        }


        #region Properties
        private Folder[] folderProjects;
        public Folder[] FolderProjects
        {
            get { return folderProjects; }
            set
            {
                if (folderProjects != value)
                {
                    folderProjects = value;
                    OnPropertyChanged("FolderProjects");
                }
            }
        }




        public string FolderPath { get; set; }

        private void OpenFolder()
        {

            Process.Start(FolderPath);

        }

        private void Exit()
        {
            MyVault.Dispose();
            Application.Current.Shutdown();
        }
        //protected void OnPropertyChanged(string propertyName)
        //{
        //    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        //}

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            Debug.WriteLine("PropertyChanged invoked for property: " + propertyName);
        }

        private string consoleOutputText;
        public string ConsoleOutputText
        {
            get { return consoleOutputText; }
            set
            {
                consoleOutputText = value;
                OnPropertyChanged("ConsoleOutputText");
            }
        }

        //public async Task AppendToConsole(string text)
        //{
        //    await Application.Current.Dispatcher.InvokeAsync(() =>
        //    {
        //        ConsoleOutputText += Environment.NewLine + text;
        //    });
        //}

        public void AppendToConsole(string text)
        {
            ConsoleOutputText += Environment.NewLine + text;
        }



        private ObservableCollection<int> numberList;
        public ObservableCollection<int> NumberList
        {
            get { return numberList; }
            set
            {
                numberList = value;
                OnPropertyChanged("NumberList");

            }
        }


        private int numberOfFiles;
        public int NumberOfFiles
        {
            get { return numberOfFiles; }
            set
            {
                if (numberOfFiles != value)
                {
                    numberOfFiles = value;
                    OnPropertyChanged("NumberOfFiles");
                }
            }
        }

        private WebServiceManager myVault;

        public WebServiceManager MyVault
        {
            get { return myVault; }
            set { myVault = value; }
        }


        private DateTime startDate;
        public DateTime StartDate
        {
            get { return startDate; }
            set
            {
                if (startDate != value)
                {
                    startDate = value;
                    OnPropertyChanged("StartDate");
                }
            }
        }

        private DateTime endDate;
        public DateTime EndDate
        {
            get { return endDate; }
            set
            {
                if (endDate != value)
                {
                    endDate = new DateTime(value.Year, value.Month, value.Day, 23, 0, 0);
                    OnPropertyChanged("EndDate");
                }
            }
        }

        public class TimeRange
        {
            public DateTime StartTime { get; set; }
            public DateTime EndTime { get; set; }

            public override string ToString()
            {
                return $"Zeitspanne von {StartTime.ToString("dd-MM-yyyy_HH-mm")} bis {EndTime.ToString("dd-MM-yyyy_HH-mm")}";
            }
        }

        private List<TimeRange> timeRanges;
        public List<TimeRange> TimeRanges
        {
            get { return timeRanges; }
            set
            {
                timeRanges = value;
                OnPropertyChanged("TimeRanges");
            }
        }

        private string searchText;

        public string SearchText
        {
            get { return searchText; }
            set
            {
                searchText = value;
                OnPropertyChanged("SearchText");
            }
        }


        private ICommand downloadFilesCommand;

        public ICommand DownloadFilesCommand
        {
            get
            {
                if (downloadFilesCommand == null)
                {
                    downloadFilesCommand = new RelayCommand(async () => await DownloadFiles());
                }
                return downloadFilesCommand;
            }
        }

        private ICommand downloadPapierkorbCommand;

        public ICommand DownloadPapierkorbCommand
        {
            get
            {
                if (downloadPapierkorbCommand == null)
                {
                    downloadPapierkorbCommand = new RelayCommand(async () => await DownloadPapierkorb());
                }
                return downloadPapierkorbCommand;
            }

        }

        public ICommand OpenFolderCommand { get; set; }
        public ICommand ExitCommand { get; set; }

        #endregion
        private async Task DownloadFiles()
        {

            AppendToConsole("Start");


            List<TimeRange> timeranges = DivideIntoSegments(startDate, endDate, numberOfFiles);
            List<Task> tasks = new List<Task>();
            foreach (var timeRange in timeranges)
            {
                tasks.Add(Task.Run(() => GetDataFromApi(timeRange)));
            }

            await Task.WhenAll(tasks.ToArray());


            AppendToConsole("Der gesamte Vorgang ist abgeschlossen. Bitte überprüfen Sie den Ordner.");
            AppendToConsole("Ende");

        }

        private async Task DownloadPapierkorb()
        {

            List<Task> tasks = new List<Task>();
            AppendToConsole("Start");
            foreach (var Folder in FolderProjects.Where(f => f.Cloaked == true))
            {
                if (Folder.Cloaked == true)
                {
                    tasks.Add(Task.Run(() => GetFolderDataFromApi(Folder)));
                }
            }

            await Task.WhenAll(tasks.ToArray());

            //await Task.Run(() => AppendToConsole("Der gesamte Vorgang ist abgeschlossen. Bitte überprüfen Sie den Ordner."));
            AppendToConsole("Der gesamte Vorgang ist abgeschlossen. Bitte überprüfen Sie den Ordner.");
            AppendToConsole("Ende");

        }
        // RelayCommand implementation for ICommand
        private class RelayCommand : ICommand
        {
            private readonly Action execute;

            public RelayCommand(Action execute)
            {
                this.execute = execute;
            }

            public event EventHandler CanExecuteChanged;

            public bool CanExecute(object parameter)
            {
                return true;
            }

            public void Execute(object parameter)
            {
                execute();
            }
        }

        public async Task GetDataFromApi(TimeRange timerange)
        {
            //DateTime startDate = new DateTime(2023, month - 1, DateTime.DaysInMonth(2023, month - 1), 23, 0, 0);
            //DateTime endDate = new DateTime(2023, month, DateTime.DaysInMonth(2023, month), 23, 0, 0);
            string startDate = timerange.StartTime.ToString("MM/dd/yyyy HH:mm:ss");
            string endDate = timerange.EndTime.ToString("MM/dd/yyyy HH:mm:ss");
            //Console.WriteLine(startDate.ToString("MM/dd/yyyy HH:mm:ss") + "to" + endDate.ToString("MM/dd/yyyy HH:mm:ss"));
            //AppendToConsole(startDate + "to" + endDate);

            SrchCond srchCondHidden = new SrchCond(); //search condition: checked out before. In App.config.
            {
                srchCondHidden.PropDefId = 17;
                srchCondHidden.SrchOper = 10;
                srchCondHidden.SrchTxt = "1";
                srchCondHidden.PropTyp = PropertySearchType.SingleProperty;
                srchCondHidden.SrchRule = SearchRuleType.Must;
            }

            SrchCond srchCondStart = new SrchCond(); //search condition: checked out before. In App.config.
            {
                srchCondStart.PropDefId = 23;
                srchCondStart.SrchOper = 7;
                //srchCondStart.SrchTxt = "09.30.2023 23:00:00";
                srchCondStart.SrchTxt = startDate;
                //srchCondStart.SrchTxt = startDate.ToString("MM/dd/yyyy HH:mm:ss");
                srchCondStart.PropTyp = PropertySearchType.SingleProperty;
                srchCondStart.SrchRule = SearchRuleType.Must;
            }
            SrchCond srchCondEnd = new SrchCond(); //search condition: checked out before. In App.config.
            {
                srchCondEnd.PropDefId = 23;
                srchCondEnd.SrchOper = 8;
                //srchCondEnd.SrchTxt = endDate.ToString("MM/dd/yyyy HH:mm:ss");
                //srchCondEnd.SrchTxt = "10.02.2023 23:00:00";
                srchCondEnd.SrchTxt = endDate;
                srchCondEnd.PropTyp = PropertySearchType.SingleProperty;
                srchCondEnd.SrchRule = SearchRuleType.Must;

            }

            SrchSort srchSort = new SrchSort();
            {
                srchSort.PropDefId = 23;
                srchSort.SortAsc = false;
            }


            string bookmark = string.Empty;
            SrchStatus status = null;
            List<File> totalResults = new List<File>();
            AppendToConsole($"{startDate}-{endDate}, Dateien wird abgerufen");
            //Application.Current.Dispatcher.Invoke(() => { AppendToConsole($"{timerange.StartTime}-{timerange.StartTime}, Dateien wird abgerufen"); });
            while (status == null || totalResults.Count < status.TotalHits) //Loop until all file information is obtained.
            {
                File[] results = MyVault.DocumentService.FindFilesBySearchConditions(new SrchCond[] { srchCondHidden, srchCondStart, srchCondEnd }, new SrchSort[] { srchSort }, null, true, true, ref bookmark, out status);

                if (results != null)
                {
                    totalResults.AddRange(results);

                    AppendToConsole($"{startDate}-{endDate} Anzahl der erhaltenen Dateien:{totalResults.Count} insgesamt:{status.TotalHits}");
                    //Application.Current.Dispatcher.Invoke(() => { AppendToConsole($"{timerange.StartTime}-{timerange.StartTime} Anzahl der erhaltenen Dateien:{totalResults.Count} insgesamt:{status.TotalHits}"); });

                }
                else { break; }

            }

            XmlHelper exporter = new VaultDataCrawlerNF.XmlHelper();
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fileName = timerange.ToString();
            string filePath = Path.Combine(FolderPath, fileName + ".xml");
            //Console.WriteLine(filePath);
            // Application.Current.Dispatcher.Invoke(() => { AppendToConsole(filePath); });
            //Console.WriteLine("XML export completed successfully.");
            exporter.ExportToXml(totalResults, filePath);
            AppendToConsole(filePath + " :XML Datei wurde erfolgreich erstellt.");
            //Application.Current.Dispatcher.Invoke(() => { AppendToConsole("XML export completed successfully."); });
            long[] fileIdList = totalResults.Select(file => file.Id).ToArray();
            //PropInst[] PropList = mVault.PropertyService.GetProperties("FILE", fileIdList, new long[] { 23 });
            //foreach (var prop in PropList)
            //{
            //    Console.WriteLine(prop.EntityId+"= "+prop.Val);
            //}

            Task<PropInst[]> task1 = GetPropertyAsync("FILE", fileIdList, new long[] { 23 });
            Task<PropInst[]> task2 = GetPropertyAsync("FILE", fileIdList, new long[] { 27 });
            Task<PropInst[]> task3 = GetPropertyAsync("FILE", fileIdList, new long[] { 31 });


            var tasks = new List<Task>() { task1, task2, task3 };

            await Task.WhenAll(tasks.ToArray());
            //Console.WriteLine(task1.IsCompleted);
            //Console.WriteLine(task2.IsCompleted);

            PropInst[] PropResultDate = task1.Result;
            PropInst[] PropResultPath = task2.Result;
            PropInst[] PropResultFileEx = task3.Result;

            //foreach (PropInst p in PropResultDate)
            //{
            //    Console.WriteLine(p.EntityId + "= " + p.Val);
            //}
            XDocument docXml = XDocument.Load(filePath);

            foreach (XElement fileElement in docXml.Descendants("File"))
            {
                long fileId = long.Parse(fileElement.Attribute("Id").Value);

                // Find the corresponding PropInst for this fileId
                PropInst propInstDate = PropResultDate.FirstOrDefault(prop => prop.EntityId == fileId);

                PropInst propInstPath = PropResultPath.FirstOrDefault(prop => prop.EntityId == fileId);

                PropInst propInstFileEx = PropResultFileEx.FirstOrDefault(prop => prop.EntityId == fileId);


                if (propInstDate.Val != null && propInstDate.Val.ToString() != null)
                {
                    string formattedDate = DateTime.ParseExact(propInstDate.Val.ToString(), "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                        .ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
                    fileElement.SetAttributeValue("OrigCreateDate", formattedDate);
                }
                //if (propInstDate != null)
                //{
                //    // Update the valField in the XML with the corresponding value
                //    string formattedDate = DateTime.ParseExact(propInstDate.Val?.ToString(), "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
                //    fileElement.SetAttributeValue("OrigCreateDate", propInstDate.Val != null ? formattedDate : "null");

                //}
                else
                {
                    fileElement.SetAttributeValue("OrigCreateDate", "Null");
                }

                if (propInstPath != null)
                {
                    // Update the valField in the XML with the corresponding value
                    fileElement.SetAttributeValue("Path", propInstPath.Val != null ? propInstPath.Val.ToString() : "null");
                }
                else
                {
                    fileElement.SetAttributeValue("Path", "File not found");
                }

                if (propInstFileEx != null)
                {
                    // Update the valField in the XML with the corresponding value
                    fileElement.SetAttributeValue("Extension", propInstFileEx.Val != null ? propInstFileEx?.Val.ToString() : "null");
                }
                else
                {
                    fileElement.SetAttributeValue("Extension", "File not found");
                }

            }
            string fileNameEx = filePath.Replace(".xml", "+Attributes.xml");


            docXml.Save(fileNameEx);

            AppendToConsole(fileNameEx + " :XML Datei mit zusätzliche Attributte wurde erfolgreich expotiert.");
            //Application.Current.Dispatcher.Invoke(() => { AppendToConsole("XML file updated and saved successfully."); });

            Task<PropInst[]> GetPropertyAsync(string entityClassId, long[] arrayId, long[] propertyDefIds)
            {
                try
                {
                    return Task.Run(() => MyVault.PropertyService.GetProperties(entityClassId, arrayId, propertyDefIds));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    //Application.Current.Dispatcher.Invoke(() => { AppendToConsole("Error in GetPropertyAsync: " + ex.Message); });
                    //Console.WriteLine("Error in GetPropertyAsync: " + ex.Message);
                    throw;  // Re-throw the exception to propagate it further if needed
                }

                // Call the PropertyService.GetProperties method

            }
        }

        public List<TimeRange> DivideIntoSegments(DateTime startTime, DateTime endTime, int n)
        {
            List<TimeRange> timeSegments = new List<TimeRange>();

            // Calculate the duration of each segment
            TimeSpan segmentDuration = new TimeSpan((endTime - startTime).Ticks / n);

            // Iterate to create segments
            DateTime segmentStart = startTime;
            DateTime segmentEnd = segmentStart.Add(segmentDuration);

            for (int i = 0; i < n; i++)
            {
                // Ensure the last segment ends at the specified end time
                if (i == n - 1)
                    segmentEnd = endTime;

                TimeRange segment = new TimeRange
                {
                    StartTime = segmentStart,
                    EndTime = segmentEnd
                };

                timeSegments.Add(segment);

                // Update segment start and end for the next iteration
                segmentStart = segmentEnd;
                segmentEnd = segmentStart.Add(segmentDuration);
            }

            return timeSegments;
        }

        public async Task GetFolderDataFromApi(Folder foler)
        {
            SrchCond srchCondHidden = new SrchCond(); //search condition: checked out before. In App.config.
            {
                srchCondHidden.PropDefId = 17;
                srchCondHidden.SrchOper = 10;
                srchCondHidden.SrchTxt = "1";
                srchCondHidden.PropTyp = PropertySearchType.SingleProperty;
                srchCondHidden.SrchRule = SearchRuleType.Must;
            }
            SrchCond srchCondPK = new SrchCond(); //search condition: checked out before. In App.config.
            {
                srchCondPK.PropDefId = 27;
                srchCondPK.SrchOper = 1;
                srchCondPK.SrchTxt = searchText;
                srchCondPK.PropTyp = PropertySearchType.SingleProperty;
                srchCondPK.SrchRule = SearchRuleType.Must;
            }
            SrchSort srchSort = new SrchSort(); //Sort by file name.
            {
                srchSort.PropDefId = 27;
                srchSort.SortAsc = true;
            }

            string bookmark = string.Empty;
            SrchStatus status = null;
            List<File> totalResults = new List<File>();
            Console.WriteLine("Dateien werden abgerufen.");
            AppendToConsole(foler.Name + ": Dateien werden abgerufen.");
            while (status == null || totalResults.Count < status.TotalHits) //Loop until all file information is obtained.
            {
                File[] results = MyVault.DocumentService.FindFilesBySearchConditions(new SrchCond[] { srchCondHidden, srchCondPK }, new SrchSort[] { srchSort }, new long[] { foler.Id }, true, true, ref bookmark, out status);

                if (results != null)
                {
                    totalResults.AddRange(results);
                    //Console.WriteLine($"Anzahl der erhaltenen Dateien:{totalResults.Count}, insgesamt: {status.TotalHits}");
                    AppendToConsole($"Anzahl der erhaltenen Dateien:{totalResults.Count} insgesamt:{status.TotalHits}");
                }
                else { break; }

            }
            //string fileName = "Papierkorb01_02_03.xml";
            //string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            //XmlExporter exporter = new XmlExporter();
            //string filePath = Path.Combine(desktopPath, fileName);
            //Console.WriteLine(filePath);
            //exporter.ExportToXml(totalResults, filePath);
            //Console.WriteLine("XML export completed successfully.");

            XmlHelper exporter = new VaultDataCrawlerNF.XmlHelper();
            string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fileName = searchText + "_from_" + foler.Name + ".xml";
            string filePath = Path.Combine(FolderPath, fileName);
            //Console.WriteLine(filePath);
            // Application.Current.Dispatcher.Invoke(() => { AppendToConsole(filePath); });
            //Console.WriteLine("XML export completed successfully.");
            exporter.ExportToXml(totalResults, filePath);
            AppendToConsole(filePath + " :XML Datei wurde erfolgreich erstellt.");
            //Application.Current.Dispatcher.Invoke(() => { AppendToConsole("XML export completed successfully."); });
            long[] fileIdList = totalResults.Select(file => file.Id).ToArray();
            //PropInst[] PropList = mVault.PropertyService.GetProperties("FILE", fileIdList, new long[] { 23 });
            //foreach (var prop in PropList)
            //{
            //    Console.WriteLine(prop.EntityId+"= "+prop.Val);
            //}

            Task<PropInst[]> task1 = GetPropertyAsync("FILE", fileIdList, new long[] { 23 });
            Task<PropInst[]> task2 = GetPropertyAsync("FILE", fileIdList, new long[] { 27 });
            Task<PropInst[]> task3 = GetPropertyAsync("FILE", fileIdList, new long[] { 31 });

            Task<PropInst[]> GetPropertyAsync(string entityClassId, long[] arrayId, long[] propertyDefIds)
            {
                try
                {
                    return Task.Run(() => MyVault.PropertyService.GetProperties(entityClassId, arrayId, propertyDefIds));
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    //Application.Current.Dispatcher.Invoke(() => { AppendToConsole("Error in GetPropertyAsync: " + ex.Message); });
                    //Console.WriteLine("Error in GetPropertyAsync: " + ex.Message);
                    throw;  // Re-throw the exception to propagate it further if needed
                }

                // Call the PropertyService.GetProperties method

            }
            var tasks = new List<Task>() { task1, task2, task3 };

            await Task.WhenAll(tasks.ToArray());
            //Console.WriteLine(task1.IsCompleted);
            //Console.WriteLine(task2.IsCompleted);

            PropInst[] PropResultDate = task1.Result;
            PropInst[] PropResultPath = task2.Result;
            PropInst[] PropResultFileEx = task3.Result;

            //foreach (PropInst p in PropResultDate)
            //{
            //    Console.WriteLine(p.EntityId + "= " + p.Val);
            //}
            XDocument docXml = XDocument.Load(filePath);

            foreach (XElement fileElement in docXml.Descendants("File"))
            {
                long fileId = long.Parse(fileElement.Attribute("Id").Value);

                // Find the corresponding PropInst for this fileId
                PropInst propInstDate = PropResultDate.FirstOrDefault(prop => prop.EntityId == fileId);

                PropInst propInstPath = PropResultPath.FirstOrDefault(prop => prop.EntityId == fileId);

                PropInst propInstFileEx = PropResultFileEx.FirstOrDefault(prop => prop.EntityId == fileId);


                if (propInstDate.Val != null && propInstDate.Val.ToString() != null)
                {
                    string formattedDate = DateTime.ParseExact(propInstDate.Val.ToString(), "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture)
                        .ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
                    fileElement.SetAttributeValue("OrigCreateDate", formattedDate);
                }
                //if (propInstDate != null)
                //{
                //    // Update the valField in the XML with the corresponding value
                //    string formattedDate = DateTime.ParseExact(propInstDate.Val?.ToString(), "dd.MM.yyyy HH:mm:ss", CultureInfo.InvariantCulture).ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
                //    fileElement.SetAttributeValue("OrigCreateDate", propInstDate.Val != null ? formattedDate : "null");

                //}
                else
                {
                    fileElement.SetAttributeValue("OrigCreateDate", "Null");
                }

                if (propInstPath != null)
                {
                    // Update the valField in the XML with the corresponding value
                    fileElement.SetAttributeValue("Path", propInstPath.Val != null ? propInstPath.Val.ToString() : "null");
                }
                else
                {
                    fileElement.SetAttributeValue("Path", "File not found");
                }

                if (propInstFileEx != null)
                {
                    // Update the valField in the XML with the corresponding value
                    fileElement.SetAttributeValue("Extension", propInstFileEx.Val != null ? propInstFileEx?.Val.ToString() : "null");
                }
                else
                {
                    fileElement.SetAttributeValue("Extension", "File not found");
                }

            }

            string fileNameEx = filePath.Replace(".xml", "+Attributes.xml");
            docXml.Save(fileNameEx);
            Debug.WriteLine("End Ex art");
            AppendToConsole(fileNameEx + " :XML Datei mit zusätzliche Attributte wurde erfolgreich expotiert.");


            //Application.Current.Dispatcher.Invoke(() => { AppendToConsole("XML file updated and saved successfully."); });


        }


    }
}
