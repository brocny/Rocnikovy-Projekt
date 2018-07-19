using System;
using System.IO;
using System.Windows.Forms;
using App.Properties;
using Core.Face;

namespace App
{
    public class DatabaseHelper<T>
    {
        public DatabaseHelper(IFaceDatabase<T> faceDatabase)
        {
            FaceDatabase = faceDatabase;
        }

        public string FileFilter { get; set; } = Settings.Default.FileFilter;
        private readonly string _defaultSerializePath = 
            string.IsNullOrWhiteSpace(Settings.Default.DefaultSerializePath) ?
                ".\\FaceDB" :
                Settings.Default.DefaultSerializePath;

        public IFaceDatabase<T> Open()
        {
            var dialog = new OpenFileDialog
            {
                InitialDirectory = SerializeFile == null ? _defaultSerializePath : Path.GetDirectoryName(SerializeFile),
                DefaultExt = "xml",
                Filter = FileFilter,
                Title = "Select file containing saved face database"
            };

            var result = dialog.STAShowDialog();
            // make a backup of the current database in case something goes wrong
            var backup = FaceDatabase.Backup();
            if (result == DialogResult.OK)
                try
                {
                    using (var fs = dialog.OpenFile())
                    {
                        FaceDatabase.Deserialize(fs);
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show(
                        $"Error: An error occured while loading the database from {dialog.FileName}: {Environment.NewLine}{exc}");
                    // something went wrong -> revert
                    FaceDatabase.Restore(backup);
                }

            SerializeFile = dialog.FileName;

            return FaceDatabase;
        }

        public string SaveAs()
        {
            if (!Directory.Exists(_defaultSerializePath)) Directory.CreateDirectory(_defaultSerializePath);

            var dialog = new SaveFileDialog
            {
                InitialDirectory = SerializeFile == null ? _defaultSerializePath : Path.GetDirectoryName(SerializeFile),
                DefaultExt = "xml",
                Filter = FileFilter
            };

            var result = dialog.STAShowDialog();
            if (result == DialogResult.OK)
            {
                try
                {
                    using (var fs = dialog.OpenFile())
                    {
                        FaceDatabase.Serialize(fs);
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show(
                        $"Error: An error occured while saving the database to {dialog.FileName}:{Environment.NewLine}{exc}");
                }

                SerializeFile = dialog.FileName;
            }

            return SerializeFile;
        }

        public string Save()
        {
            if (string.IsNullOrEmpty(SerializeFile))
            {
                return SaveAs();
            }
            try
            {
                using (var fs = File.OpenWrite(SerializeFile))
                {
                    FaceDatabase.Serialize(fs);
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    $"Error: An error occured while saving the database to {SerializeFile}:{Environment.NewLine}{exc}");
            }

            return SerializeFile;
        }

        public void SaveBeforeClose()
        {

            if (string.IsNullOrWhiteSpace(SerializeFile))
                return;

            var dialogResult = MessageBox.Show("Do you wish to save database before exiting?", "Save database", MessageBoxButtons.YesNo);
            if (dialogResult == DialogResult.Yes)
            {
                using (var stream = File.OpenWrite(SerializeFile))
                {
                    FaceDatabase.Serialize(stream);
                }
            }
        }

        public IFaceDatabase<T> FaceDatabase { get; }
        public string SerializeFile { get; private set; }
    }
}
