﻿using CircleDI.Defenitions;
using CircleDI.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.ObjectPool;
using System.Text;

namespace CircleDI.Generation;

public readonly struct CircleDIBuilder {
    private readonly ObjectPool<StringBuilder> stringBuilderPool;

    public CircleDIBuilder() => stringBuilderPool = new DefaultObjectPoolProvider().CreateStringBuilderPool(initialCapacity: 8192, maximumRetainedCapacity: 1024 * 1024);


    public void GenerateClass(SourceProductionContext context, ServiceProvider serviceProvider) {
        // check ErrorLists
        {
            bool errorReported = false;

            if (serviceProvider.ErrorList != null) {
                foreach (Diagnostic error in serviceProvider.ErrorList)
                    context.ReportDiagnostic(error);
                errorReported = true;
            }

            // serviceProvider.SortedServiceList is still empty at this point
            foreach (Service service in serviceProvider.SingletonList.Concat(serviceProvider.ScopedList).Concat(serviceProvider.TransientList).Concat(serviceProvider.DelegateList))
                if (service.ErrorList != null) {
                    foreach (Diagnostic error in service.ErrorList)
                        context.ReportDiagnostic(error);
                    errorReported = true;
                }

            if (errorReported)
                return;
        }

        // create dependency tree
        serviceProvider.CreateDependencyTree();
        if (serviceProvider.ErrorList != null) {
            foreach (Diagnostic error in serviceProvider.ErrorList)
                context.ReportDiagnostic(error);
            return;
        }


        StringBuilder builder = stringBuilderPool.Get();
        StringBuilderExtension builderExtension = new(builder, serviceProvider);

        builder.Append("""
            // <auto-generated/>
            #pragma warning disable
            #nullable enable annotations


            using System;

            """);
        if (serviceProvider.GenerateDisposeMethods.HasFlag(DisposeGeneration.DisposeAsync) || serviceProvider.GenerateDisposeMethodsScope.HasFlag(DisposeGeneration.DisposeAsync))
            builder.Append("using System.Threading.Tasks;\n");
        builder.Append('\n');

        if (serviceProvider.NameSpaceList.Count > 0) {
            builder.Append("namespace ");
            builder.AppendNamespaceList(serviceProvider.NameSpaceList);
            builder.Length--;
            builder.Append(';');
            builder.Append('\n');
            builder.Append('\n');
        }

        // containing types
        for (int i = serviceProvider.ContainingTypeList.Count - 1; i >= 0; i--) {
            builder.Append(builderExtension.indent.Sp0);
            builder.Append("partial ");
            builder.Append(serviceProvider.ContainingTypeList[i].type.AsString());
            builder.Append(' ');
            builder.Append(serviceProvider.ContainingTypeList[i].name);
            builder.Append(' ');
            builder.Append('{');
            builder.Append('\n');
            builderExtension.indent.IncreaseLevel();
        }

        // class head
        builderExtension.AppendClassSummary();
        builder.Append(builderExtension.indent.Sp0);
        foreach (string modifier in serviceProvider.Modifiers) {
            builder.Append(modifier);
            builder.Append(' ');
        }
        builder.Append("partial ");
        builder.Append(serviceProvider.Keyword.AsString());
        builder.Append(' ');
        builder.Append(serviceProvider.Name);
        builder.Append(' ');
        builder.Append(':');
        builder.Append(' ');
        if (serviceProvider.HasInterface) {
            builder.Append(serviceProvider.InterfaceName);
            builder.Append(',');
            builder.Append(' ');
        }
        builder.Append("IServiceProvider {\n");


        // constructor or InitServices()
        if (!serviceProvider.HasConstructor) {
            builder.Append(builderExtension.indent.Sp4);
            builder.Append("/// <summary>\n");
            builder.Append(builderExtension.indent.Sp4);
            builder.Append("/// Creates an instance of <see cref=\"global::");
            builder.AppendNamespaceList(serviceProvider.NameSpaceList);
            builder.AppendContainingTypeList(serviceProvider.ContainingTypeList);
            builder.Append(serviceProvider.Name);
            builder.Append("\"/> together with all <see cref=\"global::CircleDIAttributes.CreationTiming.Constructor\">non-lazy</see> singleton services.\n");
            builder.Append(builderExtension.indent.Sp4);
            builder.Append("/// </summary>\n");
            builder.Append(builderExtension.indent.Sp4);
            builder.Append("public ");
            builder.Append(serviceProvider.Name);
            builder.Append("() {\n");
        }
        else {
            builder.Append(builderExtension.indent.Sp4);
            builder.Append("/// <summary>\n");
            builder.Append(builderExtension.indent.Sp4);
            builder.Append("/// Constructs non-lazy singleton services. Should be called inside the constructor at the end.\n");
            builder.Append(builderExtension.indent.Sp4);
            builder.Append("/// </summary>\n");
            builderExtension.AppendInitServicesMemberNotNull();
            builder.Append(builderExtension.indent.Sp4);
            builder.Append("private void InitServices() {\n");
        }
        builderExtension.AppendConstructorServices();

        // "special" method CreateScope()
        if (serviceProvider.GenerateScope) {
            builderExtension.AppendCreateScopeSummary();
            builder.Append(builderExtension.indent.Sp4);
            builder.Append("public global::");
            builder.AppendNamespaceList(serviceProvider.NameSpaceList);
            builder.AppendContainingTypeList(serviceProvider.ContainingTypeList);
            if (serviceProvider.HasInterface) {
                builder.Append(serviceProvider.InterfaceName);
                builder.Append(".IScope");
            }
            else {
                builder.Append(serviceProvider.Name);
                builder.Append(".Scope");
            }
            builder.Append(" CreateScope(");
            foreach (Dependency dependency in serviceProvider.CreateScope.ConstructorDependencyList.Concat<Dependency>(serviceProvider.CreateScope.PropertyDependencyList))
                if (!dependency.HasAttribute) {
                    builder.Append("global::");
                    builder.Append(dependency.ServiceIdentifier);
                    builder.Append(' ');
                    builder.Append(dependency.Name);
                    builder.Append(',');
                    builder.Append(' ');
                }
            if (builder[^1] == ' ')
                builder.Length -= 2;
            builder.Append(") => new global::");
            builder.AppendNamespaceList(serviceProvider.NameSpaceList);
            builder.AppendContainingTypeList(serviceProvider.ContainingTypeList);
            builder.Append(serviceProvider.Name);
            builder.Append(".Scope");
            // AppendConstructorDependencyList of serviceProvider.CreateScope
            {
                builder.Append('(');
                if (serviceProvider.CreateScope.ConstructorDependencyList.Length > 0) {
                    foreach (ConstructorDependency dependency in serviceProvider.CreateScope.ConstructorDependencyList) {
                        if (!dependency.HasAttribute)
                            builder.Append(dependency.Name);
                        else {
                            if (dependency.Service!.IsRefable && !serviceProvider.Keyword.HasFlag(ClassStructKeyword.Struct))
                                builder.Append(dependency.ByRef.AsString());
                            builder.AppendServiceGetter(dependency.Service!);
                        }
                        builder.Append(',');
                        builder.Append(' ');
                    }
                    builder.Length -= 2;
                }
                builder.Append(')');
            }
            // AppendPropertyDependencyList of serviceProvider.CreateScope
            {
                if (serviceProvider.CreateScope.PropertyDependencyList.Count > 0) {
                    builder.Append(' ');
                    builder.Append('{');
                    foreach (PropertyDependency dependency in serviceProvider.CreateScope.PropertyDependencyList) {
                        builder.Append('\n');
                        builder.Append(builderExtension.indent.Sp8);
                        builder.Append(dependency.Name);
                        builder.Append(' ');
                        builder.Append('=');
                        builder.Append(' ');
                        if (!dependency.HasAttribute)
                            builder.Append(dependency.Name);
                        else
                            builder.AppendServiceGetter(dependency.Service!);
                        builder.Append(',');
                    }
                    builder.Length--;
                    builder.Append('\n');
                    builder.Append(builderExtension.indent.Sp4);
                    builder.Append('}');
                }
            }
            builder.Append(';');
            builder.Append('\n');
            builder.Append('\n');
            builder.Append('\n');
        }

        // singletons getter/getMethods
        builderExtension.AppendServicesGetter();

        // Transient services
        (bool hasDisposeList, bool hasAsyncDisposeList) = builderExtension.AppendServicesTransient();

        // Delegate services
        builderExtension.AppendServicesDelegate();

        // IServiceProvider
        builderExtension.AppendIServiceProviderNotScoped();

        // Dispose
        builderExtension.AppendDisposeMethods(hasDisposeList, hasAsyncDisposeList);

        // UnsafeAccessor methods
        builderExtension.AppendUnsafeAccessorMethods();


        // Scope
        if (serviceProvider.GenerateScope) {
            builderExtension.SetToScope();
            builder.Append('\n');

            // class head
            builderExtension.AppendClassSummary();
            builder.Append(builderExtension.indent.Sp0);
            foreach (string modifier in serviceProvider.ModifiersScope) {
                builder.Append(modifier);
                builder.Append(' ');
            }
            builder.Append("partial ");
            builder.Append(serviceProvider.KeywordScope.AsString());
            builder.Append(" Scope : ");
            if (serviceProvider.HasInterface) {
                builder.Append(serviceProvider.InterfaceName);
                builder.Append(".IScope, ");
            }
            builder.Append("IServiceProvider {\n");

            // ServiceProviderField
            {
                builder.Append(builderExtension.indent.Sp4);
                builder.Append("private ");
                builder.Append(serviceProvider.HasInterface ? serviceProvider.InterfaceName : serviceProvider.Name);
                builder.Append(" __serviceProvider;");
                builder.Append('\n');
                builder.Append('\n');
            }

            // constructor or InitServices()
            if (!serviceProvider.HasConstructorScope) {
                builderExtension.AppendCreateScopeSummary();
                builder.Append(builderExtension.indent.Sp4);
                builder.Append("/// <param name=\"serviceProvider\">An instance of the service provider this provider is the scope of.</param>\n");
                builder.Append(builderExtension.indent.Sp4);
                builder.Append("public Scope(");
            }
            else {
                builder.Append(builderExtension.indent.Sp4);
                builder.Append("/// <summary>\n");
                builder.Append(builderExtension.indent.Sp4);
                builder.Append("/// Constructs non-lazy scoped services. Should be called inside the constructor at the end.\n");
                builder.Append(builderExtension.indent.Sp4);
                builder.Append("/// </summary>\n");
                builder.Append(builderExtension.indent.Sp4);
                builder.Append("/// <param name=\"serviceProvider\">\n");
                builder.Append(builderExtension.indent.Sp4);
                builder.Append("/// The ServiceProvider this ScopedProvider is created from. Usually it is the object you get injected to your constructor parameter:<br />\n");
                builder.Append(builderExtension.indent.Sp4);
                builder.Append("/// public Scope([Dependency] ");
                builder.Append(serviceProvider.HasInterface ? serviceProvider.InterfaceName : serviceProvider.Name);
                builder.Append(" serviceProvider) { ...\n");
                builder.Append(builderExtension.indent.Sp4);
                builder.Append("/// </param>\n");
                builderExtension.AppendInitServicesMemberNotNull();
                builder.Append(builderExtension.indent.Sp4);
                builder.Append("private void InitServices(");
            }
            builder.Append(serviceProvider.HasInterface ? serviceProvider.InterfaceName : serviceProvider.Name);
            builder.Append(" serviceProvider) {\n");
            builder.Append(builderExtension.indent.Sp8);
            builder.Append("__serviceProvider = serviceProvider;\n");
            builderExtension.AppendConstructorServices();

            // scoped getter/getMethods
            builderExtension.AppendServicesGetter();

            // singleton exposing
            foreach (Service service in serviceProvider.SingletonList) {
                string refOrEmpty = (service.IsRefable && !serviceProvider.KeywordScope.HasFlag(ClassStructKeyword.Struct) && !serviceProvider.Keyword.HasFlag(ClassStructKeyword.Struct)) switch {
                    true => "ref ",
                    false => string.Empty
                };

                builderExtension.AppendServiceSummary(service);
                builder.Append(builderExtension.indent.Sp4);
                builder.Append("public ");
                builder.Append(refOrEmpty);
                builder.Append("global::");
                builder.Append(service.ServiceType);
                builder.Append(' ');
                builder.AppendServiceGetter(service);
                builder.Append(" => ");
                builder.Append(refOrEmpty);
                builder.Append("__serviceProvider.");
                builder.AppendServiceGetter(service);
                builder.Append(';');
                builder.Append('\n');
                builder.Append('\n');
            }
            builder.Append('\n');

            // Transient services
            (bool hasDisposeListScope, bool hasAsyncDisposeListScope) = builderExtension.AppendServicesTransient();

            // Delegate services
            builderExtension.AppendServicesDelegate();

            // IServiceProvider
            builderExtension.AppendIServiceProviderAllServices();

            // Dispose
            builderExtension.AppendDisposeMethods(hasDisposeListScope, hasAsyncDisposeListScope);

            // UnsafeAccessor methods
            builderExtension.AppendUnsafeAccessorMethods();

            builder.Length -= 2;
            builder.Append(builderExtension.indent.Sp0);
            builder.Append('}');
            builder.Append('\n');

            builderExtension.indent.DecreaseLevel();
        }
        else
            builder.Length -= 2;

        builder.Append(builderExtension.indent.Sp0);
        builder.Append('}');
        builder.Append('\n');

        // containing types closing
        for (int i = 0; i < serviceProvider.ContainingTypeList.Count; i++) {
            builderExtension.indent.DecreaseLevel();
            builder.Append(builderExtension.indent.Sp0);
            builder.Append('}');
            builder.Append('\n');
        }

        string hintName = serviceProvider.Name.GetFullyQualifiedName("g.cs", serviceProvider.NameSpaceList, serviceProvider.ContainingTypeList);
        string source = builder.ToString();
        context.AddSource(hintName, source);

        stringBuilderPool.Return(builder);
    }

    public void GenerateInterface(SourceProductionContext context, ServiceProvider serviceProvider) {
        if (!serviceProvider.HasInterface)
            return;

        StringBuilder builder = stringBuilderPool.Get();
        StringBuilderExtension builderExtension = new(builder, serviceProvider);

        builder.Append("""
            // <auto-generated/>
            #pragma warning disable
            #nullable enable annotations


            using System;
    
            
            """);
        if (serviceProvider.NameSpaceList.Count > 0) {
            builder.Append("namespace ");
            builder.AppendNamespaceList(serviceProvider.NameSpaceList);
            builder.Length--;
            builder.Append(';');
            builder.Append('\n');
            builder.Append('\n');
        }

        // containing types
        for (int i = serviceProvider.ContainingTypeList.Count - 1; i >= 0; i--) {
            builder.Append(builderExtension.indent.Sp0);
            builder.Append("partial ");
            builder.Append(serviceProvider.ContainingTypeList[i].type.AsString());
            builder.Append(' ');
            builder.Append(serviceProvider.ContainingTypeList[i].name);
            builder.Append(' ');
            builder.Append('{');
            builder.Append('\n');
            builderExtension.indent.IncreaseLevel();
        }

        // interface head
        builderExtension.AppendClassSummary();
        builder.Append(builderExtension.indent.Sp0);
        builder.Append("public partial interface ");
        builder.Append(serviceProvider.InterfaceName);
        builder.Append(serviceProvider.GenerateDisposeMethods switch {
            DisposeGeneration.NoDisposing => " ",
            DisposeGeneration.Dispose => " : IDisposable ",
            DisposeGeneration.DisposeAsync => " : IAsyncDisposable ",
            DisposeGeneration.GenerateBoth => " : IDisposable, IAsyncDisposable ",
            _ => throw new Exception($"Invalid DisposeGenerationEnum value: serviceProvider.GenerateDisposeMethods = {serviceProvider.GenerateDisposeMethods}")
        });
        builder.Append('{');
        builder.Append('\n');

        // "special" method CreateScope()
        if (serviceProvider.GenerateScope) {
            builderExtension.AppendCreateScopeSummary();
            builder.Append(builderExtension.indent.Sp4);
            builder.Append("global::");
            builder.AppendNamespaceList(serviceProvider.NameSpaceList);
            builder.AppendContainingTypeList(serviceProvider.ContainingTypeList);
            builder.Append(serviceProvider.InterfaceName);
            builder.Append(".IScope CreateScope(");
            foreach (Dependency dependency in serviceProvider.CreateScope.ConstructorDependencyList.Concat<Dependency>(serviceProvider.CreateScope.PropertyDependencyList))
                if (!dependency.HasAttribute) {
                    builder.Append("global::");
                    builder.Append(dependency.ServiceIdentifier);
                    builder.Append(' ');
                    builder.Append(dependency.Name);
                    builder.Append(',');
                    builder.Append(' ');
                }
            if (builder[^1] == ' ')
                builder.Length -= 2;
            builder.Append(");\n\n");
        }

        // service getter
        foreach (Service service in serviceProvider.SortedServiceList) {
            if (service.Lifetime.HasFlag(ServiceLifetime.Scoped))
                continue;
            if (service.Implementation.Type != MemberType.None && service.Implementation.IsScoped)
                continue;

            builderExtension.AppendServiceSummary(service);
            builder.Append(builderExtension.indent.Sp4);

            if (service.IsRefable && !serviceProvider.Keyword.HasFlag(ClassStructKeyword.Struct))
                builder.Append("ref ");

            builder.Append("global::");
            builder.Append(service.ServiceType);
            builder.Append(' ');
            if (service.GetAccessor == GetAccess.Property) {
                builder.Append(service.Name);
                builder.Append(" { get; }");
            }
            else {
                builder.Append("Get");
                builder.Append(service.Name);
                builder.Append("()");
                builder.Append(';');
            }
            builder.Append('\n');
            builder.Append('\n');
        }

        // Scope
        if (serviceProvider.GenerateScope) {
            builderExtension.SetToScope();
            builder.Append('\n');

            // class head
            builderExtension.AppendClassSummary();
            builder.Append(builderExtension.indent.Sp0);
            builder.Append("public partial interface IScope");
            builder.Append(serviceProvider.GenerateDisposeMethodsScope switch {
                DisposeGeneration.NoDisposing => " ",
                DisposeGeneration.Dispose => " : IDisposable ",
                DisposeGeneration.DisposeAsync => " : IAsyncDisposable ",
                DisposeGeneration.GenerateBoth => " : IDisposable, IAsyncDisposable ",
                _ => throw new Exception($"Invalid DisposeGenerationEnum value: serviceProvider.GenerateDisposeMethodsScope = {serviceProvider.GenerateDisposeMethodsScope}")
            });
            builder.Append('{');
            builder.Append('\n');

            // service getter
            foreach (Service service in serviceProvider.SortedServiceList) {
                builderExtension.AppendServiceSummary(service);
                builder.Append(builderExtension.indent.Sp4);

                bool isSingletonNotRefable = service.Lifetime == ServiceLifetime.Singleton && serviceProvider.Keyword.HasFlag(ClassStructKeyword.Struct);
                if (service.IsRefable && !serviceProvider.KeywordScope.HasFlag(ClassStructKeyword.Struct) && !isSingletonNotRefable)
                    builder.Append("ref ");

                builder.Append("global::");
                builder.Append(service.ServiceType);
                builder.Append(' ');
                if (service.GetAccessor == GetAccess.Property) {
                    builder.Append(service.Name);
                    builder.Append(" { get; }");
                }
                else {
                    builder.Append("Get");
                    builder.Append(service.Name);
                    builder.Append("()");
                    builder.Append(';');
                }
                builder.Append('\n');
                builder.Append('\n');
            }

            builder.Length--;
            builder.Append(builderExtension.indent.Sp0);
            builder.Append('}');
            builder.Append('\n');

            builderExtension.indent.DecreaseLevel();
        }
        else
            builder.Length--;

        builder.Append(builderExtension.indent.Sp0);
        builder.Append('}');
        builder.Append('\n');

        // containing types closing
        for (int i = 0; i < serviceProvider.ContainingTypeList.Count; i++) {
            builderExtension.indent.DecreaseLevel();
            builder.Append(builderExtension.indent.Sp0);
            builder.Append('}');
            builder.Append('\n');
        }

        string hintName = serviceProvider.InterfaceName.GetFullyQualifiedName("g.cs", serviceProvider.NameSpaceList, serviceProvider.ContainingTypeList);
        string source = builder.ToString();
        context.AddSource(hintName, source);

        stringBuilderPool.Return(builder);
    }
}
