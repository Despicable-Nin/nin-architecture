
using FluentValidation;

namespace espasyo.Domain.Exceptions;

public class ApplicationDomainException : Exception
{
    public ApplicationDomainException() : base()
    {

    }

    public ApplicationDomainException(string message) : base(message)
    {
        
    }
    
    public ApplicationDomainException(string message, ValidationException exception) : base(message, exception)
    {
        
    }
}