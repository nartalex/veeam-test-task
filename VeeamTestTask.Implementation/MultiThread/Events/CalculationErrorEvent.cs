using System;

namespace VeeamTestTask.Implementation.MultiThread.Events
{
    internal class CalculationErrorEvent
    {
        public delegate void CalculationErrorEventHandler(Exception e);

        public event CalculationErrorEventHandler OnCalculationError = delegate { };

        public void Fire(Exception e)
        {
            OnCalculationError(e);
        }
    }
}
