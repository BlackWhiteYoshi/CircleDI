using CircleDI.Tests.GenerateSourceText;
using System.Text;

namespace CircleDI.Tests;

/// <summary>
/// <para>Contains a single function to generate the file SourceCodeGenerationOutput.md</para>
/// <para>
/// This function is not a test, it is a utility function to generate a file.<br />
/// It just happens that creating an entire console application project for this little functionality was not worth it.
/// </para>
/// </summary>
public static class SourceCodeMarkDownGenerator {
    private static void AppendSection(this StringBuilder builder, string title, string input) {
        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        builder.Append("\n\n\n<br></br>");
        builder.Append("\n## ");
        builder.Append(title);

        builder.Append("\n\n```csharp\n");
        builder.Append(input);
        if (builder[^1] != '\n')
            builder.Append('\n');
        builder.Append("```");

        builder.Append("\n\n```csharp\n");
        builder.Append(sourceTextClass);
        if (builder[^1] != '\n')
            builder.Append('\n');
        builder.Append("```\n");
    }


    /// <summary>
    /// <para>Uncomment "[Fact]" and do "run test" on this function to generate the markdown file.</para>
    /// <para>Do not forget to comment out "[Fact]" again.</para>
    /// </summary>
    //[Fact]
    public static void CreateMarkDownFile() {
        StringBuilder builder = new(100000);

        builder.Append("# Source Code Generation Output\n");


        builder.AppendSection("Register Services", """
            using CircleDIAttributes;

            [ServiceProvider]
            [Transient<Service1, Service1>]
            [Scoped<IService2, Service2>]
            [Singleton<IService3, Service3>]
            public sealed partial class MyProvider;

            public interface IService1;
            public class Service1(IService2 service2, IService3 service3) : IService1;
            public interface IService2;
            public class Service2(IService3 service3) : IService2;
            public interface IService3;
            public class Service3 : IService3;

            """);

        builder.AppendSection("Register Circle Dependency (set-accessor)", """
            using CircleDIAttributes;

            [ServiceProvider]
            [Singleton<IMyService1, MyService1>]
            [Singleton<IMyService2, MyService2>]
            public partial class CircleExampleProvider;


            public interface IMyService1;
            public class MyService1(IMyService2 myService2) : IMyService1;

            public interface IMyService2;
            public class MyService2 : IMyService2 {
                public required IMyService1 MyService1 { private get; set; }
            }

            """);

        builder.AppendSection("Register Circle Dependency (init-accessor)", """
            using CircleDIAttributes;

            [ServiceProvider]
            [Singleton<IMyService1, MyService1>]
            [Singleton<IMyService2, MyService2>]
            public partial class CircleExampleProvider;


            public interface IMyService1;
            public class MyService1(IMyService2 myService2) : IMyService1;

            public interface IMyService2;
            public class MyService2 : IMyService2 {
                public required IMyService1 MyService1 { private get; init; }
            }

            """);

        builder.AppendSection("Register Delegate Service", """
            using CircleDIAttributes;

            [ServiceProvider]
            [Delegate<IntToString>(nameof(ConvertToString))]
            public partial class DelegateProvider {
                private static string ConvertToString(int number) => number.ToString();
            }


            public delegate string IntToString(int number);

            """);

        builder.AppendSection("Implementation Field", """
            using CircleDIAttributes;

            [ServiceProvider]
            [Singleton<IMyService, MyService>(Implementation = nameof(myService))]
            public partial class FieldProvider {
                private readonly MyService myService;

                public FieldProvider(MyService myService) {
                    this.myService = myService;
                    InitServices();
                }
            }


            public interface IMyService;
            public class MyService : IMyService;

            """);

        builder.AppendSection("Implementation Property", """
            using CircleDIAttributes;

            [ServiceProvider]
            [Singleton<IMyService, MyService>(Implementation = nameof(MyServiceImplementation)]
            public partial class PropertyProvider {
                private MyService MyServiceImplementation => new MyService();
            }


            public interface IMyService;
            public class MyService : IMyService;

            """);

        builder.AppendSection("Implementation Method", """
            using CircleDIAttributes;

            [ServiceProvider]
            [Singleton<IMyService1, MyService1>]
            [Singleton<IMyService2, MyService2>(Implementation = nameof(MyService2Implementation)]
            public partial class MethodProvider {
                private MyService2 MyService2Implementation(IMyService1 myService1) => new MyService2(myService1);
            }


            public interface IMyService1;
            public class MyService1 : IMyService1;

            public interface IMyService2;
            public class MyService2(IMyService1 myService1) : IMyService2;

            """);

        builder.AppendSection("Disposable Services", """
            using CircleDIAttributes;
            using System;
            using System.Threading.Tasks;

            [ServiceProvider]
            [Singleton<IMyService1, MyService1>]
            [Singleton<IMyService2, MyService2>]
            [Transient<IMyService3, MyService3>]
            [Transient<IMyService4, MyService4>]
            public partial class DisposingProvider;


            public interface IMyService1;
            public class MyService1 : IMyService1, IDisposable {
                public void Dispose() { }
            }

            public interface IMyService2;
            public class MyService2 : IMyService2, IAsyncDisposable {
                public ValueTask DisposeAsync() => default;
            }

            public interface IMyService3;
            public class MyService3 : IMyService3, IDisposable {
                public void Dispose() { }
            }

            public interface IMyService4;
            public class MyService4 : IMyService4, IAsyncDisposable {
                public ValueTask DisposeAsync() => default;
            }

            """);

        builder.AppendSection("Lazy Service Instantiation", """
            using CircleDIAttributes;

            [ServiceProvider(CreationTime = CreationTiming.Lazy)]
            [Singleton<IMyService, MyService>]
            public partial class LazyProvider;


            public interface IMyService;
            public class MyService : IMyService;

            """);

        builder.AppendSection("Get Method Accessor", """
            using CircleDIAttributes;

            [ServiceProvider(GetAccessor = GetAccess.Method)]
            [Singleton<IMyService, MyService>]
            public partial class MethodProvider;


            public interface IMyService;
            public class MyService : IMyService;

            """);

        builder.AppendSection("Disable Scope and disable Dispose Generation", """
            using CircleDIAttributes;

            [ServiceProvider(GenerateDisposeMethods = DisposeGeneration.NoDisposing)]
            [ScopedProvider(Generate = false)]
            [Singleton<IMyService, MyService>]
            public partial class SkipGenerationProvider;


            public interface IMyService;
            public class MyService : IMyService;

            """);

        builder.AppendSection("ThreadSafe disabled", """
            using CircleDIAttributes;
            using System;

            [ServiceProvider(ThreadSafe = false)]
            [Singleton<IMyService, MyService>(CreationTime = CreationTiming.Lazy)]
            [Transient<IDisposableService, DisposableService>]
            public partial class FastProvider;


            public interface IMyService;
            public class MyService : IMyService;

            public interface IDisposableService : IDisposable;
            public class DisposableService : IDisposableService {
                public void Dispose() { }
            }

            """);

        builder.AppendSection("Overwriting defaults", """
            using CircleDIAttributes;
            using System.Threading.Tasks;

            [ServiceProvider]
            [Singleton<IOverwritingProvider, OverwritingProvider>(Name = "Me", Implementation = "this")]
            [Scoped<IOverwritingProvider.IScope, OverwritingProvider.Scope>(Name = "MeScope", Implementation = "this")]
            public partial class OverwritingProvider {
                public OverwritingProvider() {
                    InitServices();
                }

                public void Dispose() {
                    DisposeServices();
                }

                public ValueTask DisposeAsync() {
                    return DisposeServicesAsync();
                }

                public sealed partial class Scope {
                    public Scope([Dependency] IOverwritingProvider overwritingProvider) {
                        InitServices(overwritingProvider);
                    }

                    public void Dispose() {
                        DisposeServices();
                    }

                    public ValueTask DisposeAsync() {
                        return DisposeServicesAsync();
                    }
                }
            }

            public partial interface IOverwritingProvider {
                public partial interface IScope;
            }

            """);


        File.WriteAllText("../../../SourceCodeGenerationOutput.md", builder.ToString());
    }
}
