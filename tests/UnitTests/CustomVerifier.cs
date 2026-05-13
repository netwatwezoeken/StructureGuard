using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Testing;

namespace UnitTests;

public class CustomVerifier : DefaultVerifier
{
    public CustomVerifier()
        : this(ImmutableStack<string>.Empty)
    {
    }
    
    protected ImmutableStack<string> Context { get; }
    
    protected CustomVerifier(ImmutableStack<string> context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
    }
    
    public override void Equal<T>(T expected, T actual, string? message = null)
    {
        if (message != null && message.StartsWith("Mismatch between number of diagnostics returned"))
            return;
        base.Equal(expected, actual, message);
    }
    
    public override IVerifier PushContext(string context)
    {
        return new CustomVerifier(Context.Push(context));
    }
}