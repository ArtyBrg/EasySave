using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EasySave.Models
{
    public class BackupState : INotifyPropertyChanged
    {
        private string _name;
        public string Name { get => _name; set => SetField(ref _name, value); }

        private string _state;
        public string State { get => _state; set => SetField(ref _state, value); }

        private double _progress;
        public double Progress { get => _progress; set => SetField(ref _progress, value); }

        private string _sourcePath;
        public string SourcePath { get => _sourcePath; set => SetField(ref _sourcePath, value); }

        private string _targetPath;
        public string TargetPath { get => _targetPath; set => SetField(ref _targetPath, value); }

        private int _totalFiles;
        public int TotalFiles { get => _totalFiles; set => SetField(ref _totalFiles, value); }

        private long _totalSize;
        public long TotalSize { get => _totalSize; set => SetField(ref _totalSize, value); }

        private int _filesRemaining;
        public int FilesRemaining { get => _filesRemaining; set => SetField(ref _filesRemaining, value); }

        private long _remainingSize;
        public long RemainingSize { get => _remainingSize; set => SetField(ref _remainingSize, value); }

        private string _currentSourceFile;
        public string CurrentSourceFile { get => _currentSourceFile; set => SetField(ref _currentSourceFile, value); }

        private string _currentTargetFile;
        public string CurrentTargetFile { get => _currentTargetFile; set => SetField(ref _currentTargetFile, value); }

        private string _timestamp;
        public string Timestamp { get => _timestamp; set => SetField(ref _timestamp, value); }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
