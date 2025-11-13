using CircleDI.Defenitions;
using CircleDI.Generation;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace CircleDI.Extensions;

/// <summary>
/// Extension methods on <see cref="StringBuilder"/>.
/// </summary>
public static class StringBuilderExtensions {
    /// <summary>
    /// Appends <see cref="Indent.Level"/> of copies of <see cref="Indent.CHAR"/>.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="indent"></param>
    public static StringBuilder AppendIndent(this StringBuilder builder, Indent indent) => builder.Append(Indent.CHAR, indent.Level);

    extension(string str) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.IFirstLower)"/> to <see cref="AppendFirstLower"/>.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.IFirstLower AsFirstLower => Unsafe.As<StringBuilderInterpolationHandler.IFirstLower>(str);
    }
    /// <summary>
    /// Appends the given string whereat the first character will be appended as lowercase.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="str"></param>
    public static StringBuilder AppendFirstLower(this StringBuilder builder, string str) {
        if (str.Length == 0)
            return builder;

        return builder.Append(char.ToLower(str[0]))
            .Append(str, 1, str.Length - 1);
    }

    extension(IEnumerable<string> list) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.StringJoin)"/> to <see cref="AppendStringJoin"/>.
        /// </summary>
        /// <param name="list"></param>
        /// <param name="join"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.StringJoin AsStringJoin(string join) => new(list, join);
    }
    /// <summary>
    /// Similar to <see cref="string.Join(string, IEnumerable{string})"/>, except the result gets appended to the StringBuilder instead of creating a new string.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="list"></param>
    /// <param name="join"></param>
    public static StringBuilder AppendStringJoin(this StringBuilder builder, IEnumerable<string> list, string join) {
        foreach (string item in list)
            builder.AppendInterpolation($"{item}{join}");
        builder.Length -= join.Length;

        return builder;
    }


    extension(Service service) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.IServiceField)"/> to <see cref="AppendServiceField"/>.
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.IServiceField AsServiceField => Unsafe.As<StringBuilderInterpolationHandler.IServiceField>(service);
    }
    /// <summary>
    /// <para>If implementation is field, it appends <see cref="AppendImplementationName"/>.</para>
    /// <para>Otherwise it appends <i><see cref="Service.Name"/></i>.</para>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="service"></param>
    public static StringBuilder AppendServiceField(this StringBuilder builder, Service service)
        => service.Implementation.Type switch {
            MemberType.Field => builder.AppendImplementationName(service),
            _ => builder.AppendInterpolation($"_{service.Name.AsFirstLower}")
        };

    extension(Service service) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.IServiceGetter)"/> to <see cref="AppendServiceGetter"/>.
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.IServiceGetter AsServiceGetter => Unsafe.As<StringBuilderInterpolationHandler.IServiceGetter>(service);
    }
    /// <summary>
    /// <para>If GetAccessor is a property, it appends the <see cref="Service.Name"/>.</para>
    /// <para>Otherwise it appends "Get<i><see cref="Service.Name"/></i>()".</para>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="service"></param>
    public static StringBuilder AppendServiceGetter(this StringBuilder builder, Service service)
        => service.GetAccessor switch {
            GetAccess.Property => builder.Append(service.Name),
            _ => builder.AppendInterpolation($"Get{service.Name}()")
        };

    extension(Service service) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.IImplementationName)"/> to <see cref="AppendImplementationName"/>.
        /// </summary>
        /// <param name="service"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.IImplementationName AsImplementationName => Unsafe.As<StringBuilderInterpolationHandler.IImplementationName>(service);
    }
    /// <summary>
    /// <para>If the service is not declared at a module, it just appends <see cref="ImplementationMember.Name"/>.</para>
    /// <para>
    /// If the service is decalred at a module, the module identifier is appended before appending <see cref="ImplementationMember.Name"/>.<br />
    /// Depending on the <see cref="Service.ImportMode"/>, the identifier is either<br />
    /// - <see cref="ImportMode.Static"/> -> FullQualifiedName<br />
    /// - <see cref="ImportMode.Service"/> -> ModuleName<br />
    /// - <see cref="ImportMode.Parameter"/> -> _moduleName
    /// </para>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="service"></param>
    public static StringBuilder AppendImplementationName(this StringBuilder builder, Service service) {
        Debug.Assert((service.ImportMode is ImportMode.Auto && service.Module is null)
            || (service.ImportMode is not ImportMode.Auto && service.Module is not null));

        return (service.ImportMode, service.Implementation.IsStatic) switch {
            (ImportMode.Static, _)
            or (ImportMode.Service, true)
            or (ImportMode.Parameter, true) => builder.AppendInterpolation($"global::{service.Module!.AsClosedFullyQualified}{(service.Implementation.IsScoped ? ".Scope" : "")}.{service.Implementation.Name}"),
            (ImportMode.Service, false) => builder.AppendInterpolation($"{service.Module!.Name}{(service.Implementation.IsScoped ? "Scope" : "")}.{service.Implementation.Name}"),
            (ImportMode.Parameter, false) => builder.AppendInterpolation($"_{service.Module!.Name.AsFirstLower}{(service.Implementation.IsScoped ? "Scope" : "")}.{service.Implementation.Name}"),
            _ => builder.Append(service.Implementation.Name),
        };
    }


    extension(List<string> namespaceList) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.INamespace)"/> to <see cref="AppendNamespace"/>.
        /// </summary>
        /// <param name="namespaceList"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.INamespace AsNamespace => Unsafe.As<StringBuilderInterpolationHandler.INamespace>(namespaceList);
    }
    /// <summary>
    /// <para>
    /// Appdends a string beginning with "namespace" and ending with ";\n\n":<br />
    /// "namespace {name1}.{name2}.{name3}.[...].{itemN};\n\n"
    /// </para>
    /// <para>If namespaceList is empty, nothing is appended.</para>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="namespaceList"></param>
    public static StringBuilder AppendNamespace(this StringBuilder builder, List<string> namespaceList) {
        if (namespaceList.Count > 0) {
            builder.Append("namespace ");

            for (int i = namespaceList.Count - 1; i > 0; i--)
                builder.AppendInterpolation($"{namespaceList[i]}.");
            builder.Append(namespaceList[0]);

            builder.Append(";\n\n");
        }

        return builder;
    }


    #region TypeName

    /// <summary>
    /// Creates the fully qualified name, but '&lt;', '&gt;' and ':' are replaced with '{', '}' and ':' and the given extension is appended.
    /// </summary>
    /// <param name="builder">buffer to create the string, the content is cleared.</param>
    /// <param name="typeName">the class/struct</param>
    /// <param name="extension">ending of the hintName, typically it is ".g.cs"</param>
    /// <returns></returns>
    public static string CreateHintName(this StringBuilder builder, TypeName typeName, string extension) {
        builder.Clear();

        builder.AppendClosedFullyQualified(typeName)
            .Replace('<', '{')
            .Replace('>', '}')
            .Replace(':', '.')
            .Append(extension);

        return builder.ToString();
    }

    extension(TypeName typeName) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.IClosedFullyQualifiedXMLSummary)"/> to <see cref="AppendClosedFullyQualifiedXMLSummary"/>.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.IClosedFullyQualifiedXMLSummary AsClosedFullyQualifiedXMLSummary => Unsafe.As<StringBuilderInterpolationHandler.IClosedFullyQualifiedXMLSummary>(typeName);
    }
    /// <summary>
    /// Creates the fully qualified name, but '&lt;' and '&gt;' are replaced with '{' and '}'.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="typeName"></param>
    /// <returns></returns>
    public static StringBuilder AppendClosedFullyQualifiedXMLSummary(this StringBuilder builder, TypeName typeName) {
        int startIndex = builder.Length;
        return builder.AppendClosedFullyQualified(typeName)
            .Replace('<', '{', startIndex, builder.Length - startIndex)
            .Replace('>', '}', startIndex, builder.Length - startIndex);
    }


    extension(TypeName typeName) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.IClosedFullyQualified)"/> to <see cref="AppendClosedFullyQualified"/>.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.IClosedFullyQualified AsClosedFullyQualified => Unsafe.As<StringBuilderInterpolationHandler.IClosedFullyQualified>(typeName);
    }
    /// <summary>
    /// Appends fully qualified type:<br />
    /// "{namespace1}.{namespaceN}.{containingType1}.{containingTypeN}.{name}&lt;{T1}.{TN}&gt;"
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="typeName"></param>
    public static StringBuilder AppendClosedFullyQualified(this StringBuilder builder, TypeName typeName)
        => builder.AppendInterpolation($"{typeName.AsNamespaceList}{typeName.AsClosedContainingTypeList}{typeName.AsClosedName}");

    extension(TypeName typeName) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.IOpenFullyQualified)"/> to <see cref="AppendOpenFullyQualified"/>.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.IOpenFullyQualified AsOpenFullyQualified => Unsafe.As<StringBuilderInterpolationHandler.IOpenFullyQualified>(typeName);
    }
    /// <summary>
    /// Appends fully qualified type:<br />
    /// "{namespace1}.{namespaceN}.{containingType1}.{containingTypeN}.{name}&lt;{T1}.{TN}&gt;"
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="typeName"></param>
    public static StringBuilder AppendOpenFullyQualified(this StringBuilder builder, TypeName typeName)
        => builder.AppendInterpolation($"{typeName.AsNamespaceList}{typeName.AsOpenContainingTypeList}{typeName.AsOpenName}");


    extension(TypeName typeName) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.INamespaceList)"/> to <see cref="AppendNamespaceList"/>.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.INamespaceList AsNamespaceList => Unsafe.As<StringBuilderInterpolationHandler.INamespaceList>(typeName);
    }
    /// <summary>
    /// Appends namespace with trailing dot
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="typeName"></param>
    public static StringBuilder AppendNamespaceList(this StringBuilder builder, TypeName typeName) {
        for (int i = typeName.NameSpaceList.Count - 1; i >= 0; i--)
            builder.AppendInterpolation($"{typeName.NameSpaceList[i]}.");

        return builder;
    }


    extension(TypeName typeName) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.IClosedContainingTypeList)"/> to <see cref="AppendClosedContainingTypeList"/>.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.IClosedContainingTypeList AsClosedContainingTypeList => Unsafe.As<StringBuilderInterpolationHandler.IClosedContainingTypeList>(typeName);
    }
    /// <summary>
    /// Appends containing types with trailing dot
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="typeName"></param>
    public static StringBuilder AppendClosedContainingTypeList(this StringBuilder builder, TypeName typeName) {
        for (int i = typeName.ContainingTypeList.Count - 1; i >= 0; i--)
            builder.AppendInterpolation($"{typeName.ContainingTypeList[i].AsClosedName}.");

        return builder;
    }

    extension(TypeName typeName) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.IOpenContainingTypeList)"/> to <see cref="AppendOpenContainingTypeList"/>.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.IOpenContainingTypeList AsOpenContainingTypeList => Unsafe.As<StringBuilderInterpolationHandler.IOpenContainingTypeList>(typeName);
    }
    /// <summary>
    /// Appends containing types with trailing dot
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="typeName"></param>
    public static StringBuilder AppendOpenContainingTypeList(this StringBuilder builder, TypeName typeName) {
        for (int i = typeName.ContainingTypeList.Count - 1; i >= 0; i--)
            builder.AppendInterpolation($"{typeName.ContainingTypeList[i].AsOpenName}.");

        return builder;
    }


    extension(TypeName typeName) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.IClosedName)"/> to <see cref="AppendClosedName"/>.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.IClosedName AsClosedName => Unsafe.As<StringBuilderInterpolationHandler.IClosedName>(typeName);
    }
    /// <summary>
    /// Appends "{Name}&lt;{T1}, {T2}, {TN}&gt;"
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="typeName"></param>
    public static StringBuilder AppendClosedName(this StringBuilder builder, TypeName typeName)
        => builder.AppendInterpolation($"{typeName.Name}{(typeName.Nullable ? "?" : "")}{typeName.AsClosedGenerics}");

    extension(TypeName typeName) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.IOpenName)"/> to <see cref="AppendOpenName"/>.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.IOpenName AsOpenName => Unsafe.As<StringBuilderInterpolationHandler.IOpenName>(typeName);
    }
    /// <summary>
    /// Appends "{Name}&lt;{T1}, {T2}, {TN}&gt;"
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="typeName"></param>
    public static StringBuilder AppendOpenName(this StringBuilder builder, TypeName typeName)
        => builder.AppendInterpolation($"{typeName.Name}{(typeName.Nullable ? "?" : "")}{typeName.AsOpenGenerics}");


    extension(TypeName typeName) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.IClosedGenerics)"/> to <see cref="AppendClosedGenerics"/>.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.IClosedGenerics AsClosedGenerics => Unsafe.As<StringBuilderInterpolationHandler.IClosedGenerics>(typeName);
    }
    /// <summary>
    /// Appends: "&lt;{T1}, {T2}, {TN}&gt;"<br />
    /// If <see cref="TypeName.TypeArgumentList"/> empty, nothing is appended.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="typeName"></param>
    public static StringBuilder AppendClosedGenerics(this StringBuilder builder, TypeName typeName) {
        if (typeName.TypeArgumentList.Count == 0)
            return builder;

        builder.Append('<');
        for (int i = 0; i < typeName.TypeArgumentList.Count; i++)
            if (typeName.TypeArgumentList[i] is null)
                // is open generic
                builder.AppendInterpolation($"{typeName.TypeParameterList[i]}, ");
            else
                // is closed generic
                builder.AppendInterpolation($"global::{typeName.TypeArgumentList[i]!.AsClosedFullyQualified}, ");
        builder.Length -= 2;
        builder.Append('>');

        return builder;
    }

    extension(TypeName typeName) {
        /// <summary>
        /// Creates a type to map method <see cref="StringBuilderInterpolationHandler.AppendFormatted(StringBuilderInterpolationHandler.IOpenGenerics)"/> to <see cref="AppendOpenGenerics"/>.
        /// </summary>
        /// <param name="typeName"></param>
        /// <returns></returns>
        public StringBuilderInterpolationHandler.IOpenGenerics AsOpenGenerics => Unsafe.As<StringBuilderInterpolationHandler.IOpenGenerics>(typeName);
    }
    /// <summary>
    /// Appends: "&lt;{T1}, {T2}, {TN}&gt;"<br />
    /// If <see cref="TypeName.TypeArgumentList"/> empty, nothing is appended.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="typeName"></param>
    public static StringBuilder AppendOpenGenerics(this StringBuilder builder, TypeName typeName) {
        if (typeName.TypeParameterList.Count == 0)
            return builder;

        return builder.AppendInterpolation($"<{typeName.TypeParameterList.AsStringJoin(", ")}>");
    }

    #endregion


    /// <summary>
    /// The same as <see cref="StringBuilder.Append(string)"/>, but only for interpolated strings: $"..."<br />
    /// It constructs the string directly in the builder, so no unnecessary string memory allocations.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="handler"></param>
    /// <returns></returns>
    public static StringBuilder AppendInterpolation(this StringBuilder builder, [InterpolatedStringHandlerArgument("builder")] StringBuilderInterpolationHandler handler) => builder;

    [InterpolatedStringHandler]
    public readonly ref partial struct StringBuilderInterpolationHandler {
        private readonly StringBuilder builder;

        public StringBuilderInterpolationHandler(int literalLength, int formattedCount, StringBuilder builder) => this.builder = builder;

        public void AppendLiteral(string str) => builder.Append(str);

        public void AppendFormatted<T>(T item) => builder.Append(item);



        public void AppendFormatted(Indent indent) => builder.AppendIndent(indent);

        public interface IFirstLower;
        public void AppendFormatted(IFirstLower firstLower) => builder.AppendFirstLower(Unsafe.As<string>(firstLower));

        public readonly record struct StringJoin(IEnumerable<string> List, string Join);
        public void AppendFormatted(StringJoin stringJoin) => builder.AppendStringJoin(stringJoin.List, stringJoin.Join);


        public interface IServiceField;
        public void AppendFormatted(IServiceField serviceField) => builder.AppendServiceField(Unsafe.As<Service>(serviceField));

        public interface IServiceGetter;
        public void AppendFormatted(IServiceGetter serviceGetter) => builder.AppendServiceGetter(Unsafe.As<Service>(serviceGetter));

        public interface IImplementationName;
        public void AppendFormatted(IImplementationName implementationName) => builder.AppendImplementationName(Unsafe.As<Service>(implementationName));


        public interface INamespace;
        public void AppendFormatted(INamespace @namespace) => builder.AppendNamespace(Unsafe.As<List<string>>(@namespace));


        #region TypeName

        public interface IClosedFullyQualifiedXMLSummary;
        public void AppendFormatted(IClosedFullyQualifiedXMLSummary closedFullyQualifiedXMLSummary) => builder.AppendClosedFullyQualifiedXMLSummary(Unsafe.As<TypeName>(closedFullyQualifiedXMLSummary));


        public interface IClosedFullyQualified;
        public void AppendFormatted(IClosedFullyQualified closedFullyQualified) => builder.AppendClosedFullyQualified(Unsafe.As<TypeName>(closedFullyQualified));

        public interface IOpenFullyQualified;
        public void AppendFormatted(IOpenFullyQualified openFullyQualified) => builder.AppendOpenFullyQualified(Unsafe.As<TypeName>(openFullyQualified));


        public interface INamespaceList;
        public void AppendFormatted(INamespaceList namespaceList) => builder.AppendNamespaceList(Unsafe.As<TypeName>(namespaceList));


        public interface IClosedContainingTypeList;
        public void AppendFormatted(IClosedContainingTypeList closedContainingTypeList) => builder.AppendClosedContainingTypeList(Unsafe.As<TypeName>(closedContainingTypeList));

        public interface IOpenContainingTypeList;
        public void AppendFormatted(IOpenContainingTypeList openContainingTypeList) => builder.AppendOpenContainingTypeList(Unsafe.As<TypeName>(openContainingTypeList));


        public interface IClosedName;
        public void AppendFormatted(IClosedName closedName) => builder.AppendClosedName(Unsafe.As<TypeName>(closedName));

        public interface IOpenName;
        public void AppendFormatted(IOpenName openName) => builder.AppendOpenName(Unsafe.As<TypeName>(openName));


        public interface IClosedGenerics;
        public void AppendFormatted(IClosedGenerics closedGenerics) => builder.AppendClosedGenerics(Unsafe.As<TypeName>(closedGenerics));

        public interface IOpenGenerics;
        public void AppendFormatted(IOpenGenerics openGenerics) => builder.AppendOpenGenerics(Unsafe.As<TypeName>(openGenerics));

        #endregion
    }
}
