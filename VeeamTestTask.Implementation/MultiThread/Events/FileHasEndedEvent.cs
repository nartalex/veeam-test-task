namespace VeeamTestTask.Implementation.MultiThread.Events
{
    internal class FileHasEndedEvent
    {
        public delegate void FileHasEndedEventHandler();

        public event FileHasEndedEventHandler OnFileHasEnded = delegate { };

        public void Fire()
        {
            OnFileHasEnded();
        }
    }
}
