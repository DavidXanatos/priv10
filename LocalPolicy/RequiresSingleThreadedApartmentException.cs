using System;

namespace LocalPolicy
{
    public class RequiresSingleThreadedApartmentException : Exception
    {
        const string message = "This library requires use of a single-threaded apartment. Decorate your main method with the [STAThread] attribute. See http://msdn.microsoft.com/en-gb/library/windows/desktop/ms680112(v=vs.85).aspx for more documentation";

        public RequiresSingleThreadedApartmentException(Exception innerException)
            : base(message, innerException) { }
    }
}
