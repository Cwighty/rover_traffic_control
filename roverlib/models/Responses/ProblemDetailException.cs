using System.Runtime.Serialization;

namespace Roverlib.Models.Responses;

[Serializable]
internal class ProblemDetailException : Exception
{
    public ProblemDetail Problem { get; set; }

    public ProblemDetailException(ProblemDetail problem)
    {
        this.Problem = problem;
    }

    public ProblemDetailException(string message)
        : base(message) { }

    public ProblemDetailException(string message, Exception innerException)
        : base(message, innerException) { }

    protected ProblemDetailException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}
