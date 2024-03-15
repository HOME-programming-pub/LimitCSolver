using Microsoft.Win32;
using System;
using System.Collections.Generic;
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
using System.IO;
using ProtokollResolver.ViewModels;

namespace ProtokollResolver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Error(string str)
        {
            MessageBox.Show(str);
        }

        private void LoadTaskFromFile_OnClick(object sender, EventArgs eas)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Json Files (.lct.json)|*.lct.json|Alle Dateien (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == false)
                return;
            
            var filePath = openFileDialog.FileName;

            if (!File.Exists(filePath))
            {
                MessageBox.Show("Datei wurde nicht gefunden!");
                return;
            }

            (this.DataContext as MainWindowViewModel)?.LoadTaskFromFile(filePath);
        }

        private void SaveTaskToFile_OnClick(object sender, EventArgs eas)
        {
            if (string.IsNullOrWhiteSpace((this.DataContext as MainWindowViewModel)?.CurrentConfig.Code))
            {
                Error("Kein Programm vorhanden!");
                return;
            }

            try
            {
                var dialog = new SaveFileDialog();
                dialog.FileName = $"Aufgabenstellung_{(this.DataContext as MainWindowViewModel)?.CurrentConfig.Name.Replace(" ", "_")}";
                dialog.DefaultExt = ".json";
                dialog.Filter = "Json Files (.lct.json)|*.lct.json|Alle Dateien (*.*)|*.*";

                bool? result = dialog.ShowDialog();

                if (result != true)
                    return;

                string filename = dialog.FileName;

                (this.DataContext as MainWindowViewModel)?.SaveTaskToFile(filename);

            }
            catch (Exception e)
            {
                MessageBox.Show("Eine Exception ist aufgetreten");
                MessageBox.Show(e.Message);
                Console.WriteLine(e);
            }
        }

        private void LoadProtocol_OnClick(object sender, EventArgs eas)
        {
            var openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Json Files (.lcp.json)|*.lcp.json|Alle Dateien (*.*)|*.*";
            openFileDialog.RestoreDirectory = true;

            if (openFileDialog.ShowDialog() == false)
                return;

            var filePath = openFileDialog.FileName;

            if (!File.Exists(filePath))
            {
                MessageBox.Show("Datei wurde nicht gefunden!");
                return;
            }

            (this.DataContext as MainWindowViewModel)?.LoadProtocolFromFile(filePath);
        }


        private void SaveProtocol_OnClick(object sender, EventArgs eas)
        {
            if((this.DataContext as MainWindowViewModel)?.HasEmptyFields() == true)
            {
                var res = MessageBox.Show("Mindestens ein Feld ist nicht ausgefüllt, trotzdem exportieren?", "leeres Feld", MessageBoxButton.YesNo);
                if (res != MessageBoxResult.Yes)
                    return;
            }
           

            var dialog = new SaveFileDialog();
            dialog.FileName = $"Protokoll_{(this.DataContext as MainWindowViewModel)?.CurrentConfig.Name}";
            dialog.DefaultExt = ".json";
            dialog.Filter = "Json Files (.lcp.json)|*.lcp.json|Alle Dateien (*.*)|*.*";

            bool? result = dialog.ShowDialog();

            if (result != true)
                return;

            string filename = dialog.FileName;

            (this.DataContext as MainWindowViewModel)?.SaveProtocolToFile(filename);

        }
    }
}
