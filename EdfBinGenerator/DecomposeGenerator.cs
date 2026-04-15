using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace EdfBinGenerator;

[Generator]
public class DecomposeGenerator : IIncrementalGenerator
{
    public const string DecomposeAttribute = "DecomposeGeneratorAttribute";
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(i => i.AddSource($"{DecomposeAttribute}.g.cs",
            $@"using System;
            namespace NetEdf;
            [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
            internal class {{SerializeAttribute}} : Attribute {{{{ }}}}""));
                }}"));
    }
}
