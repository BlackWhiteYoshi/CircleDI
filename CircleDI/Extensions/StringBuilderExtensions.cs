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

    /// <summary>
    /// Appends the given string whereat the first character will be appended as lowercase.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="str"></param>
    public static StringBuilder AppendFirstLower(this StringBuilder builder, string str) {
        if (str.Length == 0)
            return builder;

        builder.Append(char.ToLower(str[0]));
        builder.Append(str, 1, str.Length - 1);

        return builder;
    }


    /// <summary>
    /// <para>If implementation is field, it appends <i><see cref="Service.Implementation.Name"/></i>.</para>
    /// <para>Otherwise it appends <i><see cref="Service.Name"/></i>.</para>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="service"></param>
    public static StringBuilder AppendServiceField(this StringBuilder builder, Service service) {
        if (service.Implementation.Type == MemberType.Field)
            builder.AppendImplementationName(service);
        else
            builder.Append('_').AppendFirstLower(service.Name);

        return builder;
    }

    /// <summary>
    /// <para>If GetAccessor is a property, it appends the <see cref="Service.Name"/>.</para>
    /// <para>Otherwise it appends "Get<i><see cref="Service.Name"/></i>()".</para>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="service"></param>
    public static StringBuilder AppendServiceGetter(this StringBuilder builder, Service service) {
        if (service.GetAccessor == GetAccess.Property)
            builder.Append(service.Name);
        else
            builder.AppendInterpolation($"Get{service.Name}()");

        return builder;
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
        switch (service.ImportMode) {
            case ImportMode.Static: {
                Debug.Assert(service.Module is not null);
                builder.Append("global::");
                builder.AppendClosedFullyQualified(service.Module!);
                if (service.Implementation.IsScoped)
                    builder.Append(".Scope");
                builder.Append('.');
                break;
            }
            case ImportMode.Service: {
                if (service.Implementation.IsStatic)
                    goto case ImportMode.Static;

                Debug.Assert(service.Module is not null);
                builder.Append(service.Module!.Name);
                if (service.Implementation.IsScoped)
                    builder.Append("Scope");
                builder.Append('.');
                break;
            }
            case ImportMode.Parameter: {
                if (service.Implementation.IsStatic)
                    goto case ImportMode.Static;

                Debug.Assert(service.Module is not null);
                builder.Append('_');
                builder.AppendFirstLower(service.Module!.Name);
                if (service.Implementation.IsScoped)
                    builder.Append("Scope");
                builder.Append('.');
                break;
            }
            default:
                Debug.Assert(service.Module is null);
                break;
        }

        builder.Append(service.Implementation.Name);

        return builder;
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
    /// Appends fully qualified type:<br />
    /// "{namespace1}.{namespaceN}.{containingType1}.{containingTypeN}.{name}&lt;{T1}.{TN}&gt;"
    /// </summary>
    /// <param name="builder"></param>
    public static StringBuilder AppendClosedFullyQualified(this StringBuilder builder, TypeName typeName)
        => builder.AppendNamespaceList(typeName)
            .AppendClosedContainingTypeList(typeName)
            .Append(typeName.Name)
            .AppendClosedGenerics(typeName);

    /// <summary>
    /// Appends fully qualified type:<br />
    /// "{namespace1}.{namespaceN}.{containingType1}.{containingTypeN}.{name}&lt;{T1}.{TN}&gt;"
    /// </summary>
    /// <param name="builder"></param>
    public static StringBuilder AppendOpenFullyQualified(this StringBuilder builder, TypeName typeName)
        => builder.AppendNamespaceList(typeName)
            .AppendOpenContainingTypeList(typeName)
            .Append(typeName.Name)
            .AppendOpenGenerics(typeName);

    /// <summary>
    /// Creates the fully qualified name, but '&lt;', '&gt;' and ':' are replaced with '{', '}' and ':' and the given extension is appended.
    /// </summary>
    /// <param name="builder">buffer to create the string, the content is cleared.</param>
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



    /// <summary>
    /// Appends namespace with trailing dot
    /// </summary>
    /// <param name="builder"></param>
    public static StringBuilder AppendNamespaceList(this StringBuilder builder, TypeName typeName) {
        for (int i = typeName.NameSpaceList.Count - 1; i >= 0; i--)
            builder.AppendInterpolation($"{typeName.NameSpaceList[i]}.");

        return builder;
    }


    /// <summary>
    /// Appends containing types with trailing dot
    /// </summary>
    /// <param name="builder"></param>
    public static StringBuilder AppendClosedContainingTypeList(this StringBuilder builder, TypeName typeName) {
        for (int i = typeName.ContainingTypeList.Count - 1; i >= 0; i--)
            builder.AppendClosedName(typeName.ContainingTypeList[i]).Append('.');

        return builder;
    }

    /// <summary>
    /// Appends containing types with trailing dot
    /// </summary>
    /// <param name="builder"></param>
    public static StringBuilder AppendOpenContainingTypeList(this StringBuilder builder, TypeName typeName) {
        for (int i = typeName.ContainingTypeList.Count - 1; i >= 0; i--)
            builder.AppendOpenName(typeName.ContainingTypeList[i]).Append('.');

        return builder;
    }


    /// <summary>
    /// Appends "{Name}<{T1}, {T2}, {TN}>"
    /// </summary>
    /// <param name="builder"></param>
    public static StringBuilder AppendClosedName(this StringBuilder builder, TypeName typeName)
        => builder.Append(typeName.Name).AppendClosedGenerics(typeName);

    /// <summary>
    /// Appends "{Name}<{T1}, {T2}, {TN}>"
    /// </summary>
    /// <param name="builder"></param>
    public static StringBuilder AppendOpenName(this StringBuilder builder, TypeName typeName)
        => builder.Append(typeName.Name).AppendOpenGenerics(typeName);


    /// <summary>
    /// Appends: "&lt;{T1}, {T2}, {TN}&gt;"<br />
    /// If <see cref="TypeArgumentList"/> empty, nothing is appended.
    /// </summary>
    /// <param name="builder"></param>
    public static StringBuilder AppendClosedGenerics(this StringBuilder builder, TypeName typeName) {
        if (typeName.TypeArgumentList.Count == 0)
            return builder;

        builder.Append('<');

        for (int i = 0; i < typeName.TypeArgumentList.Count; i++) {
            if (typeName.TypeArgumentList[i] is null)
                // is open generic
                builder.Append(typeName.TypeParameterList[i]);
            else
                // is closed generic
                builder.Append("global::").AppendClosedFullyQualified(typeName.TypeArgumentList[i]!);
            builder.Append(", ");
        }
        builder.Length -= 2;

        builder.Append('>');

        return builder;
    }

    /// <summary>
    /// Appends: "&lt;{T1}, {T2}, {TN}&gt;"<br />
    /// If <see cref="TypeArgumentList"/> empty, nothing is appended.
    /// </summary>
    /// <param name="builder"></param>
    public static StringBuilder AppendOpenGenerics(this StringBuilder builder, TypeName typeName) {
        if (typeName.TypeParameterList.Count == 0)
            return builder;

        builder.Append('<');

        foreach (string parameter in typeName.TypeParameterList)
            builder.AppendInterpolation($"{parameter}, ");
        builder.Length -= 2;

        builder.Append('>');

        return builder;
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
    public readonly ref struct StringBuilderInterpolationHandler {
        private readonly StringBuilder builder;

        public StringBuilderInterpolationHandler(int literalLength, int formattedCount, StringBuilder builder) => this.builder = builder;

        public void AppendLiteral(string str) => builder.Append(str);

        public void AppendFormatted<T>(T item) => builder.Append(item);
    }
}
