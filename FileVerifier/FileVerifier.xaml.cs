using Microsoft.Win32;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FileVerifier
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string sdCardFolder;
        string hardDriveFolder;
        StringComparison comp = StringComparison.OrdinalIgnoreCase;
        public MainWindow()
        {
            InitializeComponent();
            sdCardFolder = String.Empty;
            hardDriveFolder = String.Empty;
        }

        private void SDFolderSelector_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog ofd = new OpenFolderDialog
            {
                Multiselect = false,
                Title = "Select SD Card Folder"
            };
            sdCardFolder = GetFolderDetails(ofd, SDFolderLbl, SDCountTb);
        }

        private void HDFolderSelector_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog ofd = new OpenFolderDialog
            {
                Multiselect = false,
                Title = "Select Hard Drive Folder"
            };
            hardDriveFolder = GetFolderDetails(ofd, HDFolderLbl, HDCountTb);
        }

        private void CompareBtn_Click(object sender, RoutedEventArgs e)
        {
            var errorMessages = CheckFoldersSelected();
            if (errorMessages.Any())
            {
                MessageBox.Show(String.Join("\n", errorMessages), "Could Not Compare");
                return;
            }

            var missingFiles = GetMissingFiles();
            if (missingFiles.Any())
            {
                ShowMissingFilesDialog(missingFiles);
                return;
            }

            MessageBox.Show("All files on the SD card exist in the selected Hard Drive folder.");
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            var confirmation = MessageBox.Show($"You are about to delete all files from {sdCardFolder}. Continue?", "Delete Files", MessageBoxButton.YesNo);
            if (confirmation != MessageBoxResult.Yes)
                return;

            var driveTypeVerified = ValidateDriveType();
            if (!driveTypeVerified)
                return;

            DeleteFiles();
            ResetAfterDelete();
        }


        private string GetFolderDetails(OpenFolderDialog ofd, Label folderLabel, TextBox countTextbox)
        {
            string selectedFolder = "";
            if (ofd.ShowDialog() ?? false)
            {
                selectedFolder = ofd.FolderName;
                folderLabel.Visibility = Visibility.Visible;
                folderLabel.Content = selectedFolder;
                countTextbox.Text = GetFileCount(selectedFolder).ToString();
            }
            return selectedFolder;
        }

        private int GetFileCount(string folderPath)
        {
            return Directory.GetFiles(folderPath).Length;
        }

        private void ShowMissingFilesDialog(IEnumerable<string> missingFiles)
        {
            var res = MessageBox.Show("Some files are missing from the hard drive. Select OK to view the list of files, or Cancel to proceed.", "Missing Files", MessageBoxButton.OKCancel);
            if (res == MessageBoxResult.OK)
            {
                var missingFileNames = missingFiles.Select(file => System.IO.Path.GetFileName(file));
                MessageBox.Show(String.Join("\n", missingFileNames));
            }
        }

        private bool ValidateDriveType()
        {
            var dir = new DirectoryInfo(Directory.GetDirectoryRoot(sdCardFolder));
            var removableDriveTest = DriveInfo.GetDrives().Where(d => d.RootDirectory.FullName == dir.FullName);
            if (!removableDriveTest.Any() || removableDriveTest.First().DriveType != DriveType.Removable)
                return MessageBox.Show("Could not confirm that the deleting folder is an SD Card. Continue?", "Unverified Type", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
            return true;
        }

        private void ResetAfterDelete()
        {
            sdCardFolder = String.Empty;
            SDCountTb.Text = "No folder selected.";
            SDFolderLbl.Visibility = Visibility.Hidden;
            SDFolderLbl.Content = String.Empty;
        }

        private List<string> CheckFoldersSelected()
        {
            var errorMessages = new List<string>();
            if (String.IsNullOrEmpty(sdCardFolder))
                errorMessages.Add("No SD Card Folder was selected.");
            if (String.IsNullOrEmpty(hardDriveFolder))
                errorMessages.Add("No Hard Drive Folder was selected.");
            return errorMessages;
        }

        private IEnumerable<string> GetMissingFiles()
        {
            var sdCardFiles = Directory.GetFiles(sdCardFolder);
            var hardDriveFiles = Directory.GetFiles(hardDriveFolder);

            if (IgnoreGoProCb.IsChecked ?? false)
                sdCardFiles = sdCardFiles.Where(IsGoProExtraFile).ToArray();

            return sdCardFiles.Except(hardDriveFiles);
        }

        private bool IsGoProExtraFile(string path)
        {
            return (new string[] { ".THM", ".LRV" }).Contains(System.IO.Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
        }

        private void DeleteFiles()
        {
            var folderInfo = new DirectoryInfo(sdCardFolder);
            foreach (var file in folderInfo.EnumerateFiles())
            {
                file.Delete();
            }

            if (!folderInfo.GetDirectories().Any())
                folderInfo.Delete(true);
        }
    }
}