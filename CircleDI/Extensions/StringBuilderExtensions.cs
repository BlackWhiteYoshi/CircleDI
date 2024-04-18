using CircleDI.Defenitions;
using CircleDI.Generation;
using System.Diagnostics;
using System.Text;

namespace CircleDI.Extensions;

/// <summary>
/// Extension methods on <see cref="StringBuilder"/>.
/// </summary>
public static class StringBuilderExtensions {
    /// <summary>
    /// Appends the given string whereat the first character will be appended as lowercase.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="str"></param>
    public static void AppendFirstLower(this StringBuilder builder, string str) {
        if (str.Length == 0)
            return;

        builder.Append(char.ToLower(str[0]));
        builder.Append(str, 1, str.Length - 1);
    }

    /// <summary>
    /// <para>If implementation is field, it appends <i><see cref="Service.Implementation.Name"/></i>.</para>
    /// <para>Otherwise it appends <i><see cref="Service.Name"/></i>.</para>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="service"></param>
    public static void AppendServiceField(this StringBuilder builder, Service service) {
        if (service.Implementation.Type == MemberType.Field) {
            builder.AppendImplementationName(service);
            return;
        }
        
        if (!service.Lifetime.HasFlag(ServiceLifetime.Transient))
            builder.Append('_');
        builder.AppendFirstLower(service.Name);
    }

    /// <summary>
    /// <para>If GetAccessor is a property, it appends the <see cref="Service.Name"/>.</para>
    /// <para>Otherwise it appends "Get<i><see cref="Service.Name"/></i>()".</para>
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="service"></param>
    public static void AppendServiceGetter(this StringBuilder builder, Service service) {
        if (service.GetAccessor == GetAccess.Property)
            builder.Append(service.Name);
        else {
            builder.Append("Get");
            builder.Append(service.Name);
            builder.Append("()");
        }
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
    public static void AppendImplementationName(this StringBuilder builder, Service service) {
        switch (service.ImportMode) {
            case ImportMode.Static: {
                Debug.Assert(service.Module is not null);
                builder.Append("global::");
                builder.AppendOpenFullyQualified(service.Module!);
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
    public static void AppendNamespace(this StringBuilder builder, List<string> namespaceList) {
        if (namespaceList.Count > 0) {
            builder.Append("namespace ");
            for (int i = namespaceList.Count - 1; i > 0; i--) {
                builder.Append(namespaceList[i]);
                builder.Append(".");
            }
            builder.Append(namespaceList[0]);
            builder.Append(";\n\n");
        }
    }


    #region TypeName

    /// <summary>
    /// Appends fully qualified type:<br />
    /// "{namespace1}.{namespaceN}.{containingType1}.{containingTypeN}.{name}&lt;{T1}.{TN}&gt;"
    /// </summary>
    /// <param name="builder"></param>
    public static void AppendClosedFullyQualified(this StringBuilder builder, TypeName typeName) {
        builder.AppendNamespaceList(typeName);
        builder.AppendClosedContainingTypeList(typeName);
        builder.Append(typeName.Name);
        builder.AppendClosedGenerics(typeName);
    }

    /// <summary>
    /// Appends fully qualified type:<br />
    /// "{namespace1}.{namespaceN}.{containingType1}.{containingTypeN}.{name}&lt;{T1}.{TN}&gt;"
    /// </summary>
    /// <param name="builder"></param>
    public static void AppendOpenFullyQualified(this StringBuilder builder, TypeName typeName) {
        builder.AppendNamespaceList(typeName);
        builder.AppendOpenContainingTypeList(typeName);
        builder.Append(typeName.Name);
        builder.AppendOpenGenerics(typeName);
    }

    /// <summary>
    /// Creates the fully qualified name, but '&lt;', '&gt;' and ':' are replaced with '{', '}' and ':' and the given extension is appended.
    /// </summary>
    /// <param name="builder">buffer to create the string, the content is cleared.</param>
    /// <param name="extension">ending of the hintName, typically it is ".g.cs"</param>
    /// <returns></returns>
    public static string CreateHintName(this StringBuilder builder, TypeName typeName, string extension) {
        builder.Clear();

        builder.AppendClosedFullyQualified(typeName);
        builder.Replace('<', '{');
        builder.Replace('>', '}');
        builder.Replace(':', '.');
        builder.Append(extension);

        return builder.ToString();
    }



    /// <summary>
    /// Appends namespace with trailing dot
    /// </summary>
    /// <param name="builder"></param>
    public static void AppendNamespaceList(this StringBuilder builder, TypeName typeName) {
        for (int i = typeName.NameSpaceList.Count - 1; i >= 0; i--) {
            builder.Append(typeName.NameSpaceList[i]);
            builder.Append('.');
        }
    }


    /// <summary>
    /// Appends containing types with trailing dot
    /// </summary>
    /// <param name="builder"></param>
    public static void AppendClosedContainingTypeList(this StringBuilder builder, TypeName typeName) {
        for (int i = typeName.ContainingTypeList.Count - 1; i >= 0; i--) {
            builder.AppendClosedContainingType(typeName.ContainingTypeList[i]);
            builder.Append('.');
        }
    }

    /// <summary>
    /// Appends containing types with trailing dot
    /// </summary>
    /// <param name="builder"></param>
    public static void AppendOpenContainingTypeList(this StringBuilder builder, TypeName typeName) {
        for (int i = typeName.ContainingTypeList.Count - 1; i >= 0; i--) {
            builder.AppendOpenContainingType(typeName.ContainingTypeList[i]);
            builder.Append('.');
        }
    }


    /// <summary>
    /// Appends "{Name}<{T1}, {T2}, {TN}>"
    /// </summary>
    /// <param name="builder"></param>
    public static void AppendClosedContainingType(this StringBuilder builder, TypeName typeName) {
        builder.Append(typeName.Name);
        builder.AppendClosedGenerics(typeName);
    }

    /// <summary>
    /// Appends "{Name}<{T1}, {T2}, {TN}>"
    /// </summary>
    /// <param name="builder"></param>
    public static void AppendOpenContainingType(this StringBuilder builder, TypeName typeName) {
        builder.Append(typeName.Name);
        builder.AppendOpenGenerics(typeName);
    }


    /// <summary>
    /// Appends: "&lt;{T1}, {T2}, {TN}&gt;"<br />
    /// If <see cref="TypeArgumentList"/> empty, nothing is appended.
    /// </summary>
    /// <param name="builder"></param>
    public static void AppendClosedGenerics(this StringBuilder builder, TypeName typeName) {
        if (typeName.TypeArgumentList.Count == 0)
            return;

        builder.Append('<');

        for (int i = 0; i < typeName.TypeArgumentList.Count; i++) {
            if (typeName.TypeArgumentList[i] is null)
                // is open generic
                builder.Append(typeName.TypeParameterList[i]);
            else {
                // is closed generic
                builder.Append("global::");
                builder.AppendClosedFullyQualified(typeName.TypeArgumentList[i]!);
            }
            builder.Append(", ");
        }
        builder.Length -= 2;

        builder.Append('>');
    }

    /// <summary>
    /// Appends: "&lt;{T1}, {T2}, {TN}&gt;"<br />
    /// If <see cref="TypeArgumentList"/> empty, nothing is appended.
    /// </summary>
    /// <param name="builder"></param>
    public static void AppendOpenGenerics(this StringBuilder builder, TypeName typeName) {
        if (typeName.TypeParameterList.Count == 0)
            return;

        builder.Append('<');

        for (int i = 0; i < typeName.TypeParameterList.Count; i++) {
            builder.Append(typeName.TypeParameterList[i]);
            builder.Append(", ");
        }
        builder.Length -= 2;

        builder.Append('>');
    }

    #endregion
}
