using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace Finbourne.Sdk.Extensions.Tests.Unit;

public class ArchitectureTests
{
    private String[] allowReferencesToModules =
    {
        "Newtonsoft.Json.dll",
        "Polly.dll",
        "Microsoft.Extensions.Configuration.Abstractions.dll"
    };

    [Test]
    public void VerifyPubliclyExposedTypes_FromAllowedPackageList()
    {
        var assembly = Assembly.GetAssembly(typeof(ApiClient));
        var errors = new List<string>();
        foreach (var type in GetNonPrivateTypesAndReferencedTypes(assembly))
        {
            foreach (var referencedType in type.Value)
            {
                if (referencedType.Assembly == assembly 
                    || referencedType.Namespace == "System"
                    || referencedType.Namespace.StartsWith("System.")
                    || allowReferencesToModules.Contains(referencedType.Module.ScopeName))
                {
                    continue;
                }
                errors.Add($"\t- {referencedType.FullName ?? $"{referencedType.Namespace}.{referencedType.Name}"} from {referencedType.Module.ScopeName} was referenced in {type.Key.FullName}");
            }
        }

        if (errors.Any())
        {
            Assert.Fail($"Detected one or more leaks of the following assemblies: {Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    private bool NotVisibleOutsideOfAssembly(FieldInfo field) =>
        field.IsAssembly || field.IsPrivate || field.IsFamilyAndAssembly;
    
    private bool NotVisibleOutsideOfAssembly(MethodBase method) =>
        method.IsAssembly || method.IsPrivate || method.IsFamilyAndAssembly;

    /// <summary>
    /// Retrieves all referenced types in any non-private member of non-private classes.
    /// </summary>
    /// <param name="assembly">The assembly to reflect on.</param>
    /// <returns>A dictionary where the key is the class and the value is a list of referenced types.</returns>
    private Dictionary<Type, HashSet<Type>> GetNonPrivateTypesAndReferencedTypes(Assembly assembly)
    {
        var typeReferences = new Dictionary<Type, HashSet<Type>>();

        // Get all non-private types (public, internal, or protected)
        var nonPrivateTypes = assembly.GetTypes().Where(t => t.IsPublic || t.IsNestedPublic || t.IsVisible || t.IsNestedFamily || t.IsNestedFamORAssem).ToHashSet();

        foreach (var type in nonPrivateTypes)
        {
            var referencedTypes = new HashSet<Type>();

            // Get all non-private fields and their types
            foreach (FieldInfo field in type.GetFields())
            {
                if (NotVisibleOutsideOfAssembly(field)) continue;
                referencedTypes.Add(field.FieldType);
            }

            // Get all non-private properties and their types
            foreach (var property in type.GetProperties())
            {
                var method = property.GetGetMethod(true) ?? property.GetSetMethod(true);
                if (method == null || NotVisibleOutsideOfAssembly(method))
                {
                    continue;
                }
                referencedTypes.Add(property.PropertyType);
            }
            
            // Get all non-private methods and their parameter/return types
            foreach (var constructorInfo in type.GetConstructors())
            {
                if (NotVisibleOutsideOfAssembly(constructorInfo)) continue;

                // Parameter types
                foreach (var parameter in constructorInfo.GetParameters())
                {
                    referencedTypes.Add(parameter.ParameterType);
                    foreach (var genericTypeArgument in parameter.ParameterType.GenericTypeArguments)
                    {
                        referencedTypes.Add(genericTypeArgument);
                    }
                }
            }

            // Get all non-private methods and their parameter/return types
            foreach (var method in type.GetMethods())
            {
                if (NotVisibleOutsideOfAssembly(method)) continue;

                // Return type
                referencedTypes.Add(method.ReturnType);

                // Parameter types
                foreach (var parameter in method.GetParameters())
                {
                    referencedTypes.Add(parameter.ParameterType);
                }
            }

            // Get non-private events and their handler types
            foreach (var eventInfo in type.GetEvents())
            {
                var method = eventInfo.GetAddMethod(true) ?? eventInfo.GetRemoveMethod(true);
                if (method == null || NotVisibleOutsideOfAssembly(method))
                {
                    continue;
                }
                referencedTypes.Add(eventInfo.EventHandlerType);
            }

            if (referencedTypes.Count > 0)
            {
                typeReferences[type] = referencedTypes;
            }
        }

        return typeReferences;
    }
    
    /// <summary>
    /// Gets all types that are publicly accessible from outside the project.
    /// </summary>
    /// <param name="assembly">The assembly to reflect on.</param>
    /// <returns>A list of public types in the assembly.</returns>
    static ISet<Type> GetPublicTypes(Assembly assembly)
    {
        Type[] types = assembly.GetTypes();
        return types.Where(t => t.IsPublic || t.IsNestedPublic || t.IsVisible).ToHashSet();
    }
}