using System.Reflection;
using Microsoft.Azure.Functions.Worker;
using SnapApp.Svc.Middleware;

namespace SnapApp.Svc.Extensions;

public static class FunctionDefinitionExtensions
{
    public static FunctionAuthorizeAttribute? GetFunctionAuthorizeAttribute(this FunctionDefinition fnDef)
    {
        Type? assemblyType = Type.GetType(fnDef.EntryPoint[..fnDef.EntryPoint.LastIndexOf('.')]);
        MethodInfo? methodInfo = assemblyType?.GetMethod(fnDef.EntryPoint[(fnDef.EntryPoint.LastIndexOf('.') + 1)..]);

        return methodInfo?.GetCustomAttribute<FunctionAuthorizeAttribute>(false);
    }
}
