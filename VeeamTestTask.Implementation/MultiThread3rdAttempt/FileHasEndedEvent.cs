namespace VeeamTestTask.Implementation.MultiThread3rdAttempt
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
